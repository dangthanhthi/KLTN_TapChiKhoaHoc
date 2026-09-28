using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Services;

namespace HuitJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaiBaoController : ControllerBase
{
    private readonly IBaiBaoService _baiBaoService;
    private readonly ISoTapChiService _soTapChiService;

    public BaiBaoController(IBaiBaoService baiBaoService, ISoTapChiService soTapChiService)
    {
        _baiBaoService = baiBaoService;
        _soTapChiService = soTapChiService;
    }

    /// <summary>
    /// Nộp bài báo trực tuyến 5 bước (Bao gồm upload tệp Word/PDF và khai báo nhóm đồng tác giả)
    /// </summary>
    [HttpPost("submit")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitPaper([FromForm] BaiBaoSubmitDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var (success, message, maBaiBao, maDinhDanh) = await _baiBaoService.SubmitPaperAsync(userId, dto);
        if (!success)
        {
            return BadRequest(new { message });
        }

        return Ok(new
        {
            success = true,
            message,
            maBaiBao,
            maDinhDanh
        });
    }

    /// <summary>
    /// Lấy danh sách các bài báo đã nộp của tác giả đang đăng nhập
    /// </summary>
    [HttpGet("my-submissions")]
    [Authorize]
    public async Task<IActionResult> GetMySubmissions()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        var submissions = await _baiBaoService.GetMySubmissionsAsync(userId);
        return Ok(submissions);
    }

    /// <summary>
    /// Lấy chi tiết hồ sơ bản thảo (Dành cho tác giả hoặc Ban biên tập)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetSubmissionDetail(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        var isEditorOrAdmin = User.IsInRole("Quản trị hệ thống") || User.IsInRole("Ban biên tập");
        var detail = await _baiBaoService.GetSubmissionDetailAsync(id, userId, isEditorOrAdmin);

        if (detail == null)
        {
            return NotFound(new { message = "Không tìm thấy bài báo hoặc bạn không có quyền truy cập." });
        }

        return Ok(detail);
    }

    /// <summary>
    /// Đọc bài báo công khai đã xuất bản (Dành cho độc giả tự do)
    /// </summary>
    [HttpGet("public/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicArticle(int id)
    {
        var article = await _baiBaoService.GetPublicArticleAsync(id);
        if (article == null)
        {
            return NotFound(new { message = "Không tìm thấy bài báo đã xuất bản." });
        }

        return Ok(article);
    }

    /// <summary>
    /// Lấy bài báo công khai; limit=0 trả về đầy đủ bài đã xuất bản để đồng bộ kho lưu trữ.
    /// </summary>
    [HttpGet("public/latest")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLatestArticles([FromQuery] int limit = 10)
    {
        var articles = await _soTapChiService.GetLatestArticlesAsync(limit);
        return Ok(articles);
    }

    /// <summary>
    /// Tác giả nộp bản thảo chỉnh sửa & giải trình BM-03 sau phản biện
    /// </summary>
    [HttpPost("{id}/resubmit")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ResubmitPaper(int id, [FromForm] BaiBaoResubmitDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var (success, message) = await _baiBaoService.ResubmitPaperAsync(id, userId, dto);
        if (!success)
        {
            return BadRequest(new { message });
        }

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Tác giả hoặc Ban biên tập tải tệp bản thảo khoa học của bài báo (Có phân quyền)
    /// </summary>
    [HttpGet("{id}/manuscript")]
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
            await _baiBaoService.GetManuscriptForAuthorAsync(id, userId, isEditorOrAdmin);

        if (!success || physicalPath == null || fileName == null || contentType == null)
        {
            return BadRequest(new { message });
        }

        return PhysicalFile(physicalPath, contentType, fileName);
    }

    /// <summary>
    /// Độc giả tải tệp PDF chính thức của bài báo đã xuất bản công khai
    /// </summary>
    [HttpGet("public/{id}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadPublicPdf(int id)
    {
        var (success, message, physicalPath, fileName, contentType) =
            await _baiBaoService.GetPublicArticlePdfAsync(id);

        if (!success || physicalPath == null || fileName == null || contentType == null)
        {
            return NotFound(new { message });
        }

        return PhysicalFile(physicalPath, contentType, fileName);
    }

    /// <summary>
    /// POST /api/baibao/{id}/upload-anonymous-manuscript
    /// Ban biên tập tải lên bản thảo ẩn danh theo vòng phản biện
    /// </summary>
    [HttpPost("{id}/upload-anonymous-manuscript")]
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
    public async Task<IActionResult> UploadAnonymousManuscript(int id, IFormFile file, [FromQuery] int? soVong = null)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var (success, message, duongDan) = await _baiBaoService.UploadAnonymousManuscriptAsync(id, file, soVong, userId);
        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message, duongDan });
    }

    /// <summary>
    /// POST /api/baibao/{id}/upload-published-pdf
    /// Ban biên tập tải lên tệp PDF xuất bản thành phẩm
    /// </summary>
    [HttpPost("{id}/upload-published-pdf")]
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
    public async Task<IActionResult> UploadPublishedPdf(int id, IFormFile file)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var (success, message, duongDan) = await _baiBaoService.UploadPublishedPdfAsync(id, file, userId);
        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message, duongDan });
    }

    /// <summary>
    /// POST /api/baibao/{id}/assign-issue
    /// Ban biên tập xếp bài báo vào số tạp chí
    /// </summary>
    [HttpPost("{id}/assign-issue")]
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
    public async Task<IActionResult> AssignToIssue(int id, [FromBody] AssignIssueDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
        }

        var (success, message) = await _baiBaoService.AssignToIssueAsync(id, dto, userId);
        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message });
    }
}
