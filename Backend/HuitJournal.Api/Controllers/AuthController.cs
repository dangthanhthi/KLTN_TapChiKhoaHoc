using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Services;

namespace HuitJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly QLTapChiKhoaHocContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, QLTapChiKhoaHocContext context, ILogger<AuthController> logger)
    {
        _authService = authService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "Dữ liệu đăng nhập không hợp lệ." });
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = false,
                Message = errors
            });
        }

        try
        {
            var result = await _authService.RegisterPendingAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Accepted(result); // Trả về HTTP 202 Accepted chuẩn kiến trúc xác nhận email hai bước
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi đăng ký tài khoản.");
            return StatusCode(500, new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = false,
                Message = "Máy chủ chưa thể xử lý đăng ký. Vui lòng thử lại sau."
            });
        }
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(new AuthResponse { Success = false, Message = errors });
        }

        try
        {
            var result = await _authService.VerifyEmailAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xác nhận email.");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "Máy chủ chưa thể xác nhận email. Vui lòng thử lại sau."
            });
        }
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = false,
                Message = errors
            });
        }

        try
        {
            var result = await _authService.ResendVerificationAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gửi lại mã xác nhận.");
            return StatusCode(500, new RegisterPendingResponse
            {
                Success = false,
                RequiresVerification = false,
                Message = "Máy chủ chưa thể gửi lại mã. Vui lòng thử lại sau."
            });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var profile = await _authService.GetProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ người dùng." });
        }

        return Ok(profile);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var updated = await _authService.UpdateProfileAsync(userId, request);
        if (updated == null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ người dùng để cập nhật." });
        }

        return Ok(new { success = true, message = "Cập nhật hồ sơ thành công.", profile = updated });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("MaNguoiDung")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        try
        {
            var success = await _authService.ChangePasswordAsync(userId, request);
            if (!success)
            {
                return BadRequest(new { success = false, message = "Mật khẩu hiện tại không chính xác." });
            }

            return Ok(new { success = true, message = "Đổi mật khẩu thành công." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("upload-avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn tệp tin ảnh đại diện." });
        }

        try
        {
            var updated = await _authService.UploadAvatarAsync(userId, file);
            if (updated == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy hồ sơ người dùng." });
            }

            return Ok(new { success = true, message = "Cập nhật ảnh đại diện thành công!", profile = updated, avatarUrl = updated.AnhDaiDien });
        }
        catch (ArgumentException aex)
        {
            return BadRequest(new { success = false, message = aex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi tải ảnh lên máy chủ: " + ex.Message });
        }
    }

    [HttpDelete("avatar")]
    [Authorize]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var updated = await _authService.DeleteAvatarAsync(userId);
        if (updated == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy hồ sơ người dùng." });
        }

        return Ok(new { success = true, message = "Đã xóa ảnh đại diện thành công.", profile = updated });
    }

    [HttpGet("specialties")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSpecialties()
    {
        var list = await _context.ChuyenNganhs
            .Select(c => new { c.MaChuyenNganh, c.TenChuyenNganh, c.MoTa })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("lookup")]
    [AllowAnonymous]
    public async Task<IActionResult> LookupUser([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email không được để trống." });
        }

        var cleanEmail = email.Trim().ToLower();
        var user = await _context.NguoiDungs
            .AsNoTracking()
            .Where(u => u.Email.ToLower() == cleanEmail)
            .Select(u => new
            {
                u.MaNguoiDung,
                u.HoTen,
                u.Email,
                u.DonVi,
                u.MaORCID
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng trong CSDL." });
        }

        var verifiedPayloads = await _context.WorkflowRecords.AsNoTracking()
            .Where(r => r.Kind == "OrcidLink" && r.UserId == user.MaNguoiDung && r.State == "Active")
            .Select(r => r.Payload).ToListAsync();
        var orcidVerified = !string.IsNullOrWhiteSpace(user.MaORCID) && verifiedPayloads.Any(payload =>
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(payload);
                return document.RootElement.TryGetProperty("orcid", out var id) && id.GetString() == user.MaORCID;
            }
            catch (System.Text.Json.JsonException) { return false; }
        });

        return Ok(new { user.MaNguoiDung, user.HoTen, user.Email, user.DonVi, user.MaORCID, MaORCIDDaXacThuc = orcidVerified });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        return Ok(new { success = true, message = "Đã đăng xuất khỏi hệ thống." });
    }

    /// <summary>
    /// GET /api/auth/pending-reviewers
    /// Ban biên tập / Quản trị viên lấy danh sách đơn đăng ký phản biện đang chờ thẩm định
    /// </summary>
    [HttpGet("pending-reviewers")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public async Task<IActionResult> GetPendingReviewers()
    {
        var list = await _authService.GetPendingReviewerRegistrationsAsync();
        return Ok(list);
    }

    /// <summary>
    /// POST /api/auth/approve-reviewer/{id}
    /// Quản trị viên xử lý các hồ sơ phản biện cũ còn chờ duyệt.
    /// POST /api/auth/approve-reviewer/{maDon}
    /// Ban biên tập / Quản trị viên thẩm định và chính thức phê duyệt vai trò Chuyên gia phản biện theo mã đơn
    /// </summary>
    [HttpPost("approve-reviewer/{maDon}")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public async Task<IActionResult> ApproveReviewerRole(int maDon)
    {
        var editorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("MaNguoiDung");

        if (!int.TryParse(editorIdClaim, out int editorId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

        var (success, message) = await _authService.ApproveReviewerRoleAsync(maDon, editorId);
        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// POST /api/auth/reject-reviewer/{maDon}
    /// Quản trị viên xử lý các hồ sơ phản biện cũ còn chờ duyệt.
    /// </summary>
    [HttpPost("reject-reviewer/{maDon}")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public async Task<IActionResult> RejectReviewerRole(int maDon, [FromBody] RejectReviewerRequestDto? dto = null)
    {
        var editorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("MaNguoiDung");

        if (!int.TryParse(editorIdClaim, out int editorId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

        var (success, message) = await _authService.RejectReviewerRoleAsync(maDon, editorId, dto?.LyDo);
        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }
}
