using System.Text.Json;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;
using HuitJournal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

public partial class JournalWorkflowController
{
    public sealed record ReplaceReviewerRequest(int ReviewerId, DateTime ResponseDue, DateTime CompletionDue, string Reason);
    [HttpPost("assignments/{id:int}/replace"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public Task<IActionResult> ReplaceReviewer(int id, ReplaceReviewerRequest dto, [FromServices] IPhanBienService reviews) => Run(async () =>
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 10 || dto.Reason.Length > 2000)
            throw new ArgumentException("Nêu lý do thay phản biện từ 10 đến 2000 ký tự.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var assignment = await db.PhanCongPhanBiens.Include(p => p.BaiBao).SingleOrDefaultAsync(p => p.MaPhanCong == id) ?? throw new KeyNotFoundException();
        await WorkflowTools.LockAsync(db, "Journal:Article:" + assignment.MaBaiBao);
        await db.Entry(assignment).ReloadAsync();
        await db.Entry(assignment.BaiBao).ReloadAsync();
        if (assignment.BaiBao.TrangThai is not ("Đang phản biện" or "Chờ sơ duyệt" or "Chờ quyết định") ||
            assignment.TrangThai == "Đã đánh giá" || await db.PhieuDanhGias.AnyAsync(p => p.MaPhanCong == id) || assignment.MaNguoiDung == dto.ReviewerId)
            throw new ArgumentException("Không thể thay phân công đã đánh giá, thay bằng chính người cũ hoặc thay ở bước hiện tại.");
        var oldReviewer = assignment.MaNguoiDung;
        assignment.TrangThai = "Từ chối phản biện";
        await db.SaveChangesAsync();
        var result = await reviews.AssignReviewerAsync(UserId, new PhanCongRequestDto { MaBaiBao = assignment.MaBaiBao, MaNguoiDungReviewer = dto.ReviewerId,
            SoVong = assignment.SoVong, HanPhanHoi = dto.ResponseDue, HanHoanThanh = dto.CompletionDue, LyDo = dto.Reason });
        if (!result.Success) { await tx.RollbackAsync(); db.ChangeTracker.Clear(); throw new ArgumentException(result.Message); }
        db.WorkflowRecords.Add(new WorkflowRecord { ArticleId = assignment.MaBaiBao, UserId = UserId, Kind = "ReviewerReplacement", State = "Completed",
            Payload = JsonSerializer.Serialize(new { OldAssignment = id, NewAssignment = result.MaPhanCong, dto.Reason }) });
        var user = await db.NguoiDungs.FindAsync(oldReviewer);
        if (user?.TrangThai == true) db.EmailOutboxes.Add(WorkflowTools.Mail("ThayPhanBien", user.Email, "[HUIT Journal] Cập nhật phân công phản biện #" + id,
            "Tòa soạn đã kết thúc phân công này. " + dto.Reason));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return new { success = true, result.MaPhanCong };
    });

    [HttpPost("contact/{id:guid}/reply"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public Task<IActionResult> Reply(Guid id, ReasonRequest dto) => Run(async () =>
    {
        JournalWorkflowService.Validate(dto);
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:Contact:" + id);
        var record = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == id && r.Kind == "Contact") ?? throw new KeyNotFoundException();
        if (record.State != "Pending") throw new ArgumentException("Yêu cầu đã được trả lời.");
        var request = JsonSerializer.Deserialize<ContactRequest>(record.Payload)!;
        db.EmailOutboxes.Add(WorkflowTools.Mail("PhanHoiLienHe", request.Email, "[HUIT Journal] Phản hồi yêu cầu " + id.ToString("N")[..8], dto.Reason));
        record.State = "Replied"; record.UpdatedUtc = DateTime.UtcNow;
        record.Payload = JsonSerializer.Serialize(new { request.Name, request.Email, request.ArticleCode, request.Subject, request.Message, Reply = dto.Reason, EditorId = UserId });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return new { success = true };
    });

    [HttpPost("article/{id:int}/publication-notice"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public Task<IActionResult> PublicationNotice(int id, PublicationNoticeRequest dto) => Run(async () =>
    {
        JournalWorkflowService.Validate(dto);
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:Article:" + id);
        var article = await db.BaiBaos.Include(b => b.TacGia).Include(b => b.DongTacGias).ThenInclude(d => d.NguoiDung)
            .SingleOrDefaultAsync(b => b.MaBaiBao == id && b.TrangThai == "Đã xuất bản") ?? throw new ArgumentException("Chỉ lập thông báo cho bài đã xuất bản.");
        db.WorkflowRecords.Add(new WorkflowRecord { ArticleId = id, UserId = UserId, Kind = "PublicationNotice", State = "Published",
            Payload = JsonSerializer.Serialize(new { dto.Kind, dto.Text }) });
        var label = dto.Kind == "Retraction" ? "Thu hồi bài đã xuất bản" : "Đính chính bài đã xuất bản";
        db.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao { MaBaiBao = id, MaNguoiThucHien = UserId,
            TrangThaiCu = article.TrangThai, TrangThaiMoi = article.TrangThai, NgayChuyen = WorkflowTools.VietnamNow,
            GhiChu = label, ThongBaoChoTacGia = dto.Text });
        article.NgayCapNhat = WorkflowTools.VietnamNow;
        foreach (var user in article.DongTacGias.Where(d => d.NguoiDung != null && d.NguoiDung.TrangThai).Select(d => d.NguoiDung!)
            .Append(article.TacGia).Where(u => u.TrangThai).DistinctBy(u => u.Email.Trim().ToLowerInvariant()))
            db.EmailOutboxes.Add(WorkflowTools.Mail("ThongBaoDinhChinh", user.Email, "[HUIT Journal] " + label + " #" + id, dto.Text));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return new { success = true };
    });

    [HttpGet("article/{id:int}/publication-notices"), AllowAnonymous]
    public Task<IActionResult> Notices(int id) => Run(async () =>
    {
        if (!await db.BaiBaos.AnyAsync(b => b.MaBaiBao == id && b.TrangThai == "Đã xuất bản")) throw new KeyNotFoundException();
        var rows = await db.WorkflowRecords.AsNoTracking().Where(r => r.ArticleId == id && r.Kind == "PublicationNotice" && r.State == "Published")
            .OrderByDescending(r => r.CreatedUtc).ToListAsync();
        return rows.Select(r => new { r.CreatedUtc, data = JsonSerializer.Deserialize<JsonElement>(r.Payload) });
    });

    public sealed record ScheduleRequest(DateTimeOffset PublishAt);
    [HttpPost("issue/{id:int}/schedule"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public Task<IActionResult> Schedule(int id, ScheduleRequest dto, [FromServices] IssuePublicationService publisher) => Run(async () =>
    {
        if (dto.PublishAt.UtcDateTime <= DateTime.UtcNow.AddMinutes(1)) throw new ArgumentException("Chọn thời điểm phát hành trong tương lai, cách hiện tại ít nhất một phút.");
        var validation = await publisher.Publish(id, UserId, validateOnly: true);
        if (validation.Status != 200) throw new ArgumentException(JsonSerializer.Serialize(validation.Result));
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:IssueSchedule:" + id);
        var key = JsonSerializer.Serialize(new { IssueId = id });
        var previous = await db.WorkflowRecords.Where(r => r.Kind == "IssueSchedule" && r.Payload == key && r.State == "Pending").ToListAsync();
        foreach (var p in previous) { p.State = "Superseded"; p.UpdatedUtc = DateTime.UtcNow; }
        var record = new WorkflowRecord { Kind = "IssueSchedule", State = "Pending", UserId = UserId, Payload = key,
            CreatedUtc = DateTime.UtcNow, UpdatedUtc = dto.PublishAt.UtcDateTime };
        db.WorkflowRecords.Add(record);
        var issue = await db.SoTapChis.FindAsync(id) ?? throw new KeyNotFoundException();
        if (issue.TrangThai == "Đã xuất bản" || issue.TrangThai == "Đã phát hành") throw new ArgumentException("Số tạp chí đã phát hành.");
        issue.NgayPhatHanh = TimeZoneInfo.ConvertTime(dto.PublishAt, WorkflowTools.VietnamZone).DateTime;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return new { success = true, record.Id };
    });
}
