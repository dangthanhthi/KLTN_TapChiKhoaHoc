using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using HuitJournal.Api.Configuration;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HuitJournal.Api.Services;

public sealed class OrcidOAuthService(
    QLTapChiKhoaHocContext db,
    IHttpClientFactory clients,
    IOptions<OrcidSettings> options,
    ILogger<OrcidOAuthService> logger)
{
    private const string FlowKind = "OrcidOAuth";
    private static readonly TimeSpan FlowLifetime = TimeSpan.FromMinutes(10);
    private OrcidSettings Settings => options.Value;

    public async Task<(Guid State, string AuthorizationUrl)> StartAsync(int? userId, string returnBase, CancellationToken ct)
    {
        EnsureConfigured();
        var state = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var record = new WorkflowRecord
        {
            Id = state,
            UserId = userId,
            Kind = FlowKind,
            State = "Pending",
            CreatedUtc = now,
            UpdatedUtc = now,
            Payload = JsonSerializer.Serialize(new { expiresAtUtc = now.Add(FlowLifetime), returnBase })
        };
        db.WorkflowRecords.Add(record);
        await db.SaveChangesAsync(ct);

        // Best-effort bounded cleanup of old, unused OAuth attempts.
        var expired = await db.WorkflowRecords.Where(r => (r.Kind == FlowKind || r.Kind == "OrcidLink") && r.State != "Active" && r.CreatedUtc < now.AddDays(-2))
            .OrderBy(r => r.CreatedUtc).Take(100).ToListAsync(ct);
        if (expired.Count > 0) { db.WorkflowRecords.RemoveRange(expired); await db.SaveChangesAsync(ct); }

        var origin = Settings.UseSandbox ? "https://sandbox.orcid.org" : "https://orcid.org";
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = Settings.ClientId,
            ["response_type"] = "code",
            ["scope"] = "/authenticate",
            ["redirect_uri"] = Settings.RedirectUri,
            ["state"] = state.ToString("D")
        };
        return (state, Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString($"{origin}/oauth/authorize", query));
    }

    public async Task<string> CompleteAsync(string code, Guid state, CancellationToken ct)
    {
        EnsureConfigured();
        var flow = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == state && r.Kind == FlowKind, ct)
            ?? throw new InvalidOperationException("Phiên liên kết ORCID không tồn tại hoặc đã hết hạn.");
        if (flow.State != "Pending" || DateTime.UtcNow - flow.CreatedUtc > FlowLifetime)
            throw new InvalidOperationException("Phiên liên kết ORCID đã được sử dụng hoặc đã hết hạn.");

        // Claim the state before the external token exchange to prevent parallel code replay.
        flow.State = "Exchanging";
        flow.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        try
        {
            var origin = Settings.UseSandbox ? "https://sandbox.orcid.org" : "https://orcid.org";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{origin}/oauth/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = Settings.ClientId,
                    ["client_secret"] = Settings.ClientSecret,
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = Settings.RedirectUri
                })
            };
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await clients.CreateClient("OrcidOAuth").SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("ORCID chưa hoàn tất xác thực. Hãy thử liên kết lại.");
            using var token = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = token.RootElement;
            var orcid = NormalizeAndValidate(root.GetProperty("orcid").GetString());
            var name = root.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;

            if (await db.NguoiDungs.AnyAsync(u => u.MaORCID == orcid && (flow.UserId == null || u.MaNguoiDung != flow.UserId), ct))
                throw new InvalidOperationException("ORCID này đã liên kết với tài khoản khác trong tạp chí.");
            if (await db.DangKyChoXacNhans.AnyAsync(r => r.MaORCID == orcid && r.TrangThai == "Pending" && r.HetHanHoSoUtc > DateTime.UtcNow, ct))
                throw new InvalidOperationException("ORCID này đang được dùng trong một hồ sơ đăng ký khác.");

            flow.Payload = JsonSerializer.Serialize(new { orcid, name, expiresAtUtc = flow.CreatedUtc.Add(FlowLifetime) });
            flow.State = "Completed";
            flow.UpdatedUtc = DateTime.UtcNow;
            if (flow.UserId is int userId)
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                var user = await db.NguoiDungs.SingleOrDefaultAsync(u => u.MaNguoiDung == userId && u.TrangThai, ct)
                    ?? throw new InvalidOperationException("Tài khoản không còn hoạt động.");
                var links = await db.WorkflowRecords.Where(r => r.Kind == "OrcidLink" && r.UserId == userId && r.State == "Active").ToListAsync(ct);
                foreach (var link in links) { link.State = "Revoked"; link.UpdatedUtc = DateTime.UtcNow; }
                user.MaORCID = orcid;
                flow.Kind = "OrcidLink";
                flow.State = "Active";
                flow.UpdatedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            else
            {
                await db.SaveChangesAsync(ct);
            }
            return orcid;
        }
        catch (Exception ex)
        {
            flow.State = "Failed";
            flow.Payload = "{}";
            flow.UpdatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning(ex, "ORCID OAuth flow {FlowId} failed", state);
            throw;
        }
    }

    public async Task<(string Orcid, string? Name)> GetRegistrationResultAsync(Guid state, CancellationToken ct)
    {
        var flow = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == state && r.Kind == FlowKind && r.UserId == null && r.State == "Completed", ct)
            ?? throw new InvalidOperationException("Chưa có kết quả ORCID hợp lệ. Hãy liên kết ORCID lại.");
        if (DateTime.UtcNow - flow.CreatedUtc > FlowLifetime) throw new InvalidOperationException("Phiên liên kết ORCID đã hết hạn.");
        using var payload = JsonDocument.Parse(flow.Payload);
        return (payload.RootElement.GetProperty("orcid").GetString()!, payload.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null);
    }

    public async Task<bool> IsVerifiedAsync(int userId, string? orcid, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(orcid)) return false;
        var records = await db.WorkflowRecords.AsNoTracking()
            .Where(r => r.Kind == "OrcidLink" && r.UserId == userId && r.State == "Active")
            .Select(r => r.Payload).ToListAsync(ct);
        return records.Any(payload =>
        {
            try { using var doc = JsonDocument.Parse(payload); return doc.RootElement.TryGetProperty("orcid", out var value) && value.GetString() == orcid; }
            catch (JsonException) { return false; }
        });
    }

    public async Task UnlinkAsync(int userId, CancellationToken ct)
    {
        var user = await db.NguoiDungs.SingleOrDefaultAsync(u => u.MaNguoiDung == userId && u.TrangThai, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        user.MaORCID = null;
        var links = await db.WorkflowRecords.Where(r => r.Kind == "OrcidLink" && r.UserId == userId && r.State == "Active").ToListAsync(ct);
        foreach (var link in links) { link.State = "Revoked"; link.UpdatedUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
    }

    public static string NormalizeAndValidate(string? value)
    {
        var id = (value ?? "").Trim();
        if (id.StartsWith("https://orcid.org/", StringComparison.OrdinalIgnoreCase)) id = id["https://orcid.org/".Length..];
        else if (id.StartsWith("http://orcid.org/", StringComparison.OrdinalIgnoreCase)) id = id["http://orcid.org/".Length..];
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$"))
            throw new InvalidOperationException("ORCID trả về mã định danh sai định dạng.");
        var digits = id.Replace("-", "");
        var total = 0;
        for (var i = 0; i < 15; i++) total = (total + (digits[i] - '0')) * 2;
        var remainder = total % 11;
        var result = (12 - remainder) % 11;
        var check = result == 10 ? 'X' : (char)('0' + result);
        if (char.ToUpperInvariant(digits[15]) != check) throw new InvalidOperationException("Mã ORCID không vượt qua kiểm tra checksum.");
        return id;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(Settings.ClientId) || string.IsNullOrWhiteSpace(Settings.ClientSecret) ||
            !Uri.TryCreate(Settings.RedirectUri, UriKind.Absolute, out var redirect) || redirect.Scheme != Uri.UriSchemeHttps ||
            !Uri.TryCreate(Settings.PublicWebBaseUrl, UriKind.Absolute, out var web) || web.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Liên kết ORCID chưa được cấu hình cho website này.");
    }
}
