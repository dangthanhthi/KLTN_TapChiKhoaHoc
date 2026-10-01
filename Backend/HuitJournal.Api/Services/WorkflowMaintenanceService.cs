using System.Text.Json;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Services;

public class WorkflowMaintenanceService(QLTapChiKhoaHocContext db, IConfiguration config)
{
    public async Task RunAsync(int? articleId = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:ReviewMaintenance");
        var today = WorkflowTools.VietnamNow.Date;
        var pending = await db.PhanCongPhanBiens.Include(p => p.ChuyenGia).Include(p => p.BaiBao)
            .Where(p => (!articleId.HasValue || p.MaBaiBao == articleId.Value) &&
                (p.BaiBao.TrangThai == "Đang phản biện" || p.BaiBao.TrangThai == "Chờ sơ duyệt") &&
                (p.TrangThai == "Chờ phản hồi" || p.TrangThai == "Đồng ý phản biện" || p.TrangThai == "Đang đánh giá") &&
                p.SoVong == db.PhanCongPhanBiens.Where(x => x.MaBaiBao == p.MaBaiBao).Max(x => x.SoVong)).ToListAsync();
        foreach (var p in pending)
        {
            if (p.TrangThai == "Chờ phản hồi" && p.HanPhanHoi?.Date < today)
            {
                p.TrangThai = "Từ chối phản biện";
                db.WorkflowRecords.Add(new WorkflowRecord { ArticleId = p.MaBaiBao, UserId = p.MaNguoiDung, Kind = "InvitationExpiry", State = "Expired",
                    Payload = JsonSerializer.Serialize(new { AssignmentId = p.MaPhanCong, Reason = "Quá hạn phản hồi lời mời." }) });
                continue;
            }
            var due = p.TrangThai == "Chờ phản hồi" ? p.HanPhanHoi : p.HanHoanThanh;
            if (due == null || due.Value.Date > today.AddDays(3) || !p.ChuyenGia.TrangThai) continue;
            // One reminder per assignment each Vietnam calendar day, even with multiple API instances.
            var key = $"{p.MaPhanCong}:{today:yyyyMMdd}";
            if (await db.WorkflowRecords.AnyAsync(r => r.Kind == "ReviewReminder" && r.Payload == keyPayload(key))) continue;
            db.WorkflowRecords.Add(new WorkflowRecord { ArticleId = p.MaBaiBao, UserId = p.MaNguoiDung, Kind = "ReviewReminder", State = "Queued", Payload = keyPayload(key) });
            db.EmailOutboxes.Add(WorkflowTools.Mail("NhacHanPhanBien", p.ChuyenGia.Email, "[HUIT Journal] Nhắc hạn phản biện bài #" + p.MaBaiBao,
                $"Hạn: {due:dd/MM/yyyy}. Trạng thái: {p.TrangThai}. Vui lòng mở {config["EmailVerification:PublicWebBaseUrl"]}/reviewer.html để xử lý."));
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task PublishScheduledAsync(IssuePublicationService publisher)
    {
        var schedules = await db.WorkflowRecords.Where(r => r.Kind == "IssueSchedule" && r.State == "Pending" && r.UpdatedUtc <= DateTime.UtcNow).Take(10).ToListAsync();
        foreach (var schedule in schedules)
        {
            var issueId = JsonDocument.Parse(schedule.Payload).RootElement.GetProperty("IssueId").GetInt32();
            var authorized = await db.NguoiDungs.AnyAsync(u => u.MaNguoiDung == schedule.UserId && u.TrangThai &&
                u.NguoiDungVaiTros.Any(v => v.VaiTro.TenVaiTro == "Quản trị hệ thống" || v.VaiTro.TenVaiTro == "Tổng biên tập"));
            var result = authorized ? await publisher.Publish(issueId, schedule.UserId!.Value) : (403, (object)new { message = "Tài khoản lập lịch không còn quyền phát hành." });
            schedule.State = result.Item1 == 200 ? "Published" : "Failed";
            schedule.UpdatedUtc = DateTime.UtcNow;
            schedule.Payload = JsonSerializer.Serialize(new { IssueId = issueId, Result = result.Item2 });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { db.Entry(schedule).State = EntityState.Detached; }
        }
    }
    private static string keyPayload(string key) => JsonSerializer.Serialize(new { Key = key });
}
