using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Services;

namespace HuitJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhanBienController : ControllerBase
{
    private readonly IPhanBienService _phanBienService;

    public PhanBienController(IPhanBienService phanBienService)
    {
        _phanBienService = phanBienService;
    }

    /// <summary>
    /// Ban biên tập phân công chuyên gia phản biện kín (Tự động kích hoạt Trigger TRG_PhanCongPhanBien_KiemTraChuyenMon)
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public async Task<IActionResult> AssignReviewer([FromBody] PhanCongRequestDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        var (success, message, maPhanCong) = await _phanBienService.AssignReviewerAsync(userId, dto);
        if (!success)
        {
            return BadRequest(new { message });
        }

        return Ok(new
        {
            success = true,
            message,
            maPhanCong
        });
    }

    /// <summary>
    /// Chuyên gia phản biện xem danh sách các bài báo được phân công
    /// </summary>
    [HttpGet("my-assignments")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> GetMyAssignments()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        var assignments = await _phanBienService.GetMyReviewAssignmentsAsync(userId);
        return Ok(assignments);
    }

    [HttpPost("assignments/{id:int}/respond")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> RespondToAssignment(int id, [FromBody] PhanBienResponseDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        var (success, message) = await _phanBienService.RespondToAssignmentAsync(id, userId, dto.Accept!.Value);
        return success ? Ok(new { success = true, message }) : BadRequest(new { message });
    }

    [HttpGet("assignments/{id:int}/evaluation")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> GetMyEvaluation(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        var evaluation = await _phanBienService.GetMyEvaluationAsync(id, userId);
        return evaluation == null ? NotFound(new { message = "Không tìm thấy phiếu đánh giá của bạn." }) : Ok(evaluation);
    }

    [HttpGet("assignments/{id:int}/evaluation-draft")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> GetMyEvaluationDraft(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        var draft = await _phanBienService.GetMyEvaluationDraftAsync(id, userId);
        return draft == null ? NoContent() : Ok(draft);
    }

    [HttpGet("my-evaluation-drafts")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> GetMyEvaluationDrafts()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        return Ok(await _phanBienService.GetMyEvaluationDraftsAsync(userId));
    }

    [HttpPut("assignments/{id:int}/evaluation-draft")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> SaveMyEvaluationDraft(int id, [FromBody] PhieuDanhGiaBanNhapDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        var result = await _phanBienService.SaveMyEvaluationDraftAsync(id, userId, dto);
        return result.Success ? Ok(new { success = true, savedAtUtc = result.SavedAtUtc }) : BadRequest(new { message = result.Message });
    }

    [HttpDelete("assignments/{id:int}/evaluation-draft")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> DeleteMyEvaluationDraft(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        var result = await _phanBienService.DeleteMyEvaluationDraftAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(new { message = result.Message });
    }

    /// <summary>
    /// Chuyên gia phản biện nộp Phiếu nhận xét và đánh giá BM-04
    /// </summary>
    [HttpPost("evaluate")]
    [Authorize(Roles = "Quản trị hệ thống,Chuyên gia phản biện")]
    public async Task<IActionResult> SubmitEvaluation([FromBody] PhieuDanhGiaDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        var (success, message) = await _phanBienService.SubmitEvaluationAsync(userId, dto);
        if (!success)
        {
            return BadRequest(new { message });
        }

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Ban biên tập ra quyết định biên tập (Chấp nhận, Yêu cầu chỉnh sửa, hoặc Từ chối)
    /// </summary>
    [HttpPost("decision")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> MakeDecision([FromBody] QuyetDinhBienTapDto dto)
    {
        var finalStatus = dto.TrangThaiMoi?.Trim();
        if ((string.Equals(finalStatus, "Đã chấp nhận", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(finalStatus, "Từ chối", StringComparison.OrdinalIgnoreCase)) &&
            !(User.IsInRole("Tổng biên tập") || User.IsInRole("Quản trị hệ thống")))
            return Forbid();
        if (string.Equals(finalStatus, "Đã xuất bản", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Chỉ phát hành bài qua quy trình phát hành số tạp chí." });
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        var (success, message) = await _phanBienService.MakeEditorialDecisionAsync(userId, dto);
        if (!success)
        {
            return BadRequest(new { message });
        }

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Chuyên gia phản biện tải tệp bản thảo khoa học ẩn danh đã được phân công
    /// </summary>
    [HttpGet("assignments/{id}/manuscript")]
    [Authorize]
    public async Task<IActionResult> DownloadManuscript(int id, [FromQuery] bool inline = false)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var isEditorOrAdmin = User.IsInRole("Quản trị hệ thống") || User.IsInRole("Tổng biên tập") || User.IsInRole("Ban biên tập");
        var (success, message, physicalPath, fileName, contentType) =
            await _phanBienService.GetManuscriptForReviewerAsync(id, userId, isEditorOrAdmin);

        if (!success || physicalPath == null || fileName == null || contentType == null)
        {
            return BadRequest(new { message });
        }

        if (inline && (contentType == "application/pdf" || contentType.StartsWith("image/")))
        {
            return PhysicalFile(physicalPath, contentType);
        }

        return PhysicalFile(physicalPath, contentType, fileName);
    }
}
