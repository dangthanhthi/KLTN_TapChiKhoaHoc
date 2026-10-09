using System.Net;
using System.Security.Claims;
using System.Text.Json;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using HuitJournal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

[ApiController, Route("api/editorial-articles/{id}/doi")]
[Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
public class DoiController(QLTapChiKhoaHocContext db) : ControllerBase
{
    public sealed class DoiRequest { public string? MaDOI { get; set; } }

    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var article = await db.BaiBaos.FindAsync(id);
        if (article == null) return NotFound();
        var record = await db.WorkflowRecords.AsNoTracking().Where(r => r.ArticleId == id && r.Kind == "DOI")
            .OrderByDescending(r => r.UpdatedUtc).FirstOrDefaultAsync();
        return Ok(new { article.MaDOI, state = record?.Payload == DoiTools.Payload(article.MaDOI) ? record.State : (article.MaDOI == null ? "Unassigned" : "Pending"), checkedUtc = record?.UpdatedUtc });
    }

    [HttpPost]
    public async Task<IActionResult> Save(int id, DoiRequest request)
    {
        string? doi;
        try { doi = DoiTools.Normalize(request.MaDOI); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var article = await db.BaiBaos.FindAsync(id);
        if (article == null) return NotFound();
        // Once verified, an identifier must remain stable, including after publication.
        if (await db.WorkflowRecords.AnyAsync(r => r.ArticleId == id && r.Kind == "DOI" && r.State == "Verified"))
            return Conflict(new { message = "DOI đã xác thực không được thay thế. Liên hệ đơn vị đăng ký để xử lý metadata." });
        if (doi != null && await db.BaiBaos.AnyAsync(b => b.MaBaiBao != id && b.MaDOI == doi))
            return Conflict(new { message = "DOI đã được gán cho một bài khác." });
        article.MaDOI = doi;
        article.NgayCapNhat = DateTime.Now;
        db.WorkflowRecords.Add(new WorkflowRecord { ArticleId = id, UserId = UserId, Kind = "DOI", State = doi == null ? "Unassigned" : "Pending", Payload = DoiTools.Payload(doi) });
        Audit(id, "Cập nhật DOI: " + (doi ?? "Chưa cấp"), article.TrangThai);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = "Đã lưu. Mã chỉ được công khai sau khi xác thực đăng ký Crossref và đúng bài.", state = doi == null ? "Unassigned" : "Pending" });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(int id, CancellationToken cancellationToken)
    {
        var article = await db.BaiBaos.Include(b => b.TacGia).Include(b => b.DongTacGias).FirstOrDefaultAsync(b => b.MaBaiBao == id, cancellationToken);
        if (article == null) return NotFound();
        string? doi;
        try { doi = DoiTools.Normalize(article.MaDOI); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        if (doi == null) return BadRequest(new { message = "Chưa nhập DOI." });
        if (await db.BaiBaos.AnyAsync(b => b.MaBaiBao != id && b.MaDOI == doi, cancellationToken))
            return Conflict(new { message = "DOI đang được gán cho bài khác; kiểm tra dữ liệu trước khi xác thực." });
        var originalTitle = article.TieuDe;
        var originalEnglishTitle = article.TieuDeTiengAnh;
        var state = "Failed";
        var message = "Không xác thực được DOI.";
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HuitJournal/1.0");
        try
        {
            using var metadata = await client.GetAsync("https://api.crossref.org/works/" + Uri.EscapeDataString(doi), cancellationToken);
            if (metadata.StatusCode == HttpStatusCode.NotFound) message = "DOI chưa đăng ký tại Crossref. Đơn vị xuất bản cần đăng ký metadata trước.";
            else if (!metadata.IsSuccessStatusCode) { state = "Pending"; message = "Crossref tạm thời không phản hồi. Giữ trạng thái chờ; thử lại sau."; }
            else
            {
                using var json = JsonDocument.Parse(await metadata.Content.ReadAsStringAsync(cancellationToken));
                var work = json.RootElement.GetProperty("message");
                var titles = work.GetProperty("title").EnumerateArray().Select(t => DoiTools.TitleKey(t.GetString() ?? "")).ToArray();
                var matches = work.GetProperty("DOI").GetString()?.Equals(doi, StringComparison.OrdinalIgnoreCase) == true &&
                    titles.Any(t => t.Length > 0 && (t == DoiTools.TitleKey(article.TieuDe) || t == DoiTools.TitleKey(article.TieuDeTiengAnh ?? "")));
                var localNames = article.DongTacGias.Select(a => DoiTools.NameKey(a.HoTen)).Append(DoiTools.NameKey(article.TacGia.HoTen)).ToHashSet();
                var authorMatches = work.TryGetProperty("author", out var authors) && authors.EnumerateArray().Any(a =>
                    localNames.Contains(DoiTools.NameKey((a.TryGetProperty("given", out var given) ? given.GetString() : "") + " " + (a.TryGetProperty("family", out var family) ? family.GetString() : ""))));
                if (!matches || !authorMatches) message = "DOI đã tồn tại nhưng metadata không khớp tiêu đề hoặc nhóm tác giả. Không được gán DOI của bài khác.";
                else
                {
                    using var resolution = await client.GetAsync("https://doi.org/" + doi, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    var location = resolution.Headers.Location;
                    if ((int)resolution.StatusCode is >= 300 and < 400 && location?.IsAbsoluteUri == true && location.Scheme is "https" or "http")
                    { state = "Verified"; message = "DOI đã đăng ký, khớp tiêu đề bài và đã hoạt động tại doi.org."; }
                    else message = "Metadata đã có nhưng DOI chưa chuyển tới trang bài. Kiểm tra URL đăng ký rồi xác thực lại.";
                }
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        { state = "Pending"; message = "Chưa kết nối được dịch vụ xác thực DOI. Giữ trạng thái chờ; thử lại sau."; }
        // Reject a stale verification if another editor changed the identifier/title during the request.
        await db.Entry(article).ReloadAsync(cancellationToken);
        if (DoiTools.Normalize(article.MaDOI) != doi || article.TieuDe != originalTitle || article.TieuDeTiengAnh != originalEnglishTitle)
            return Conflict(new { message = "DOI hoặc tiêu đề đã thay đổi; hãy xác thực lại." });
        var previous = await db.WorkflowRecords.Where(r => r.ArticleId == id && r.Kind == "DOI" && r.State == "Verified").ToListAsync(cancellationToken);
        foreach (var r in previous) { r.State = "Superseded"; r.UpdatedUtc = DateTime.UtcNow; }
        article.MaDOI = doi;
        db.WorkflowRecords.Add(new WorkflowRecord { ArticleId = id, UserId = UserId, Kind = "DOI", State = state, Payload = DoiTools.Payload(doi) });
        Audit(id, message, article.TrangThai);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { state, message });
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private void Audit(int id, string note, string status) => db.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
    { MaBaiBao = id, MaNguoiThucHien = UserId, TrangThaiCu = status, TrangThaiMoi = status, GhiChu = note, NgayChuyen = DateTime.Now });
}
