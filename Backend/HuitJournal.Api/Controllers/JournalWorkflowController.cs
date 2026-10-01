using System.Security.Claims;
using System.Text.Json;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Infrastructure;
using HuitJournal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

[ApiController, Route("api/workflows"), Authorize]
public partial class JournalWorkflowController(JournalWorkflowService service, QLTapChiKhoaHocContext db, IWebHostEnvironment env) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Staff => User.IsInRole("Quản trị hệ thống") || User.IsInRole("Tổng biên tập") || User.IsInRole("Ban biên tập");
    private async Task<IActionResult> Run(Func<Task<object?>> action)
    {
        try { return Ok(await action()); }
        catch (UnauthorizedAccessException) { return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này." }); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Không tìm thấy hồ sơ." }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "Hồ sơ đã được cập nhật. Vui lòng tải lại." }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (JsonException) { return BadRequest(new { message = "Dữ liệu hồ sơ không hợp lệ." }); }
    }
    [HttpGet("status"), AllowAnonymous]
    public object Status() => new { version = "2026.10.01.workflows.2" };
    [HttpGet("article/{id:int}")]
    public Task<IActionResult> Article(int id) => Run(() => service.Article(id, UserId, Staff)!);
    [HttpGet("submission-draft")]
    public Task<IActionResult> Draft() => Run(() => service.Draft(UserId));
    [HttpPost("submission-draft"), RequestSizeLimit(125*1024*1024), RequestFormLimits(MultipartBodyLengthLimit = 125*1024*1024)]
    public Task<IActionResult> SaveDraft([FromForm] SubmissionDraftRequest dto) => Run(async () => await service.SaveDraft(UserId, dto));
    [HttpDelete("submission-draft/{id:guid}")]
    public Task<IActionResult> DeleteDraft(Guid id) => Run(async () => { await service.DeleteDraft(id, UserId); return new { success = true }; });
    [HttpPost("contact"), AllowAnonymous]
    public Task<IActionResult> Contact(ContactRequest dto) => Run(async () => new { success = true, id = await service.Contact(dto), message = "Tòa soạn đã nhận yêu cầu." });
    [HttpPost("password-reset/request"), AllowAnonymous]
    public Task<IActionResult> Reset(ResetRequest dto) => Run(async () => new { id = await service.RequestReset(dto), message = "Nếu email thuộc tài khoản đang hoạt động, hệ thống sẽ gửi mã khôi phục." });
    [HttpPost("password-reset/confirm"), AllowAnonymous]
    public Task<IActionResult> ConfirmReset(ResetConfirm dto) => Run(async () => new { success = await service.ConfirmReset(dto) });
    [HttpPost("article/{id:int}/withdrawal")]
    public Task<IActionResult> Withdrawal(int id, ReasonRequest dto) => Run(async () => new { success = true, id = await service.RequestWithdrawal(id, UserId, dto) });
    [HttpPost("withdrawal/{id:guid}/review"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public Task<IActionResult> ReviewWithdrawal(Guid id, ActionReviewRequest dto) => Run(async () => { await service.ReviewWithdrawal(id, UserId, dto); return new { success = true }; });
    [HttpPost("article/{id:int}/proof"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public Task<IActionResult> SendProof(int id) => Run(async () => new { success = true, id = await service.SendProof(id, UserId) });
    [HttpPost("proof/{id:guid}/review")]
    public Task<IActionResult> ReviewProof(Guid id, ActionReviewRequest dto) => Run(async () => { await service.ReviewProof(id, UserId, dto); return new { success = true }; });
    [HttpPost("article/{id:int}/screening"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public Task<IActionResult> Screening(int id, [FromForm] ScreeningRequest dto) => Run(async () => { await service.Screening(id, UserId, dto); return new { success = true }; });
    [HttpGet("inbox"), Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public Task<IActionResult> Inbox() => Run(async () => await db.WorkflowRecords.AsNoTracking().Where(r => r.Kind == "Contact" || r.Kind == "Withdrawal" || r.Kind == "Proof")
        .OrderByDescending(r => r.UpdatedUtc).Take(200).Select(r => new { r.Id, r.ArticleId, r.Kind, r.State, r.Payload, r.UpdatedUtc }).ToListAsync());
    [HttpGet("files/{id:guid}")]
    public async Task<IActionResult> File(Guid id)
    {
        var file = await db.WorkflowFiles.Include(f => f.Record).SingleOrDefaultAsync(f => f.Id == id);
        if (file == null) return NotFound();
        bool allowed = file.ArticleId.HasValue
            ? await service.CanRead(file.ArticleId.Value, UserId, Staff) && (file.Kind != "SimilarityReport" || Staff)
            : file.UserId == UserId;
        if (!allowed) return Forbid();
        var path = UploadStoragePaths.ResolveExistingFile(env.ContentRootPath, file.Path);
        if (path == null) return NotFound();
        return PhysicalFile(path, file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/octet-stream", file.Name, true);
    }
    [HttpGet("proof/{id:guid}/pdf")]
    public async Task<IActionResult> ProofPdf(Guid id)
    {
        var record = await db.WorkflowRecords.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.Kind == "Proof");
        if (record?.ArticleId == null) return NotFound();
        if (!await service.CanRead(record.ArticleId.Value, UserId, Staff)) return Forbid();
        var fileId = JsonDocument.Parse(record.Payload).RootElement.GetProperty("FileId").GetInt32();
        var file = await db.ThuMucBaiBaos.SingleOrDefaultAsync(f => f.MaThuMuc == fileId && f.MaBaiBao == record.ArticleId);
        var path = file == null ? null : UploadStoragePaths.ResolveExistingFile(env.ContentRootPath, file.DuongDan);
        return path == null ? NotFound() : PhysicalFile(path, "application/pdf", enableRangeProcessing: true);
    }
}
