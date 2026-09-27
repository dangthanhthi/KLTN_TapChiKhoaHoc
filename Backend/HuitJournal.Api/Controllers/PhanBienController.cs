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
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
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
    [Authorize(Roles = "Chuyên gia phản biện")]
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

    /// <summary>
    /// Chuyên gia phản biện nộp Phiếu nhận xét và đánh giá BM-04
    /// </summary>
    [HttpPost("evaluate")]
    [Authorize(Roles = "Chuyên gia phản biện")]
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
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
    public async Task<IActionResult> MakeDecision([FromBody] QuyetDinhBienTapDto dto)
    {
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
    public async Task<IActionResult> DownloadManuscript(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var isEditorOrAdmin = User.IsInRole("Quản trị hệ thống") || User.IsInRole("Ban biên tập");
        var (success, message, physicalPath, fileName, contentType) =
            await _phanBienService.GetManuscriptForReviewerAsync(id, userId, isEditorOrAdmin);

        if (!success || physicalPath == null || fileName == null || contentType == null)
        {
            return BadRequest(new { message });
        }

        return PhysicalFile(physicalPath, contentType, fileName);
    }
}
