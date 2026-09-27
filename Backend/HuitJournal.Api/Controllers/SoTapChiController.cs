using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using HuitJournal.Api.Services;

namespace HuitJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SoTapChiController : ControllerBase
{
    public sealed class SaveDraftIssueRequest
    {
        public int MaSoTapChi { get; set; }
        public string TenSo { get; set; } = "";
        public int Tap { get; set; }
        public int So { get; set; }
        public int Nam { get; set; }
        public DateTime? NgayPhatHanh { get; set; }
        public string TrangThai { get; set; } = "Đang biên tập";
    }
    private readonly ISoTapChiService _soTapChiService;
    private readonly QLTapChiKhoaHocContext _context;
    private readonly IWebHostEnvironment _env;

    public SoTapChiController(ISoTapChiService soTapChiService, QLTapChiKhoaHocContext context, IWebHostEnvironment env)
    {
        _soTapChiService = soTapChiService;
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các số tạp chí đã xuất bản (Phục vụ trang archives.html)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedIssues()
    {
        var issues = await _soTapChiService.GetPublishedIssuesAsync();
        return Ok(issues);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một số tạp chí và danh sách bài báo trong số đó (Phục vụ trang issue-detail.html)
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetIssueDetail(int id)
    {
        var issue = await _soTapChiService.GetIssueDetailAsync(id);
        if (issue == null)
        {
            return NotFound(new { message = "Không tìm thấy số tạp chí yêu cầu." });
        }

        return Ok(issue);
    }

    [HttpPost("draft/save")]
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftIssueRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenSo) || dto.TenSo.Length > 255 ||
            dto.Tap < 1 || dto.So < 1 || dto.Nam < 1900 ||
            (dto.TrangThai != "Đang biên tập" && dto.TrangThai != "Đã đóng"))
            return BadRequest(new { message = "Thông tin số tạp chí hoặc trạng thái bản nháp không hợp lệ." });

        var issue = dto.MaSoTapChi == 0 ? new SoTapChi() :
            await _context.SoTapChis.FindAsync(dto.MaSoTapChi);
        if (issue == null) return NotFound(new { message = "Không tìm thấy số tạp chí." });
        if (issue.TrangThai == "Đã xuất bản" || issue.TrangThai == "Đã phát hành")
            return BadRequest(new { message = "Không thể sửa số tạp chí đã phát hành." });
        if (await _context.SoTapChis.AnyAsync(s => s.Tap == dto.Tap && s.So == dto.So && s.Nam == dto.Nam && s.MaSoTapChi != dto.MaSoTapChi))
            return BadRequest(new { message = "Tập, số và năm đã tồn tại." });

        issue.TenSo = dto.TenSo.Trim();
        issue.Tap = dto.Tap;
        issue.So = dto.So;
        issue.Nam = dto.Nam;
        issue.NgayPhatHanh = dto.NgayPhatHanh;
        issue.TrangThai = dto.TrangThai;
        if (dto.MaSoTapChi == 0) _context.SoTapChis.Add(issue);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, maSoTapChi = issue.MaSoTapChi });
    }

    [HttpPost("{id}/draft/delete")]
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        var issue = await _context.SoTapChis.FindAsync(id);
        if (issue == null) return NotFound(new { message = "Không tìm thấy số tạp chí." });
        if (issue.TrangThai == "Đã xuất bản" || issue.TrangThai == "Đã phát hành")
            return BadRequest(new { message = "Không thể xóa số tạp chí đã phát hành." });
        if (await _context.BaiBaos.AnyAsync(b => b.MaSoTapChi == id))
            return BadRequest(new { message = "Hãy gỡ các bài báo khỏi số trước khi xóa bản nháp." });
        _context.SoTapChis.Remove(issue);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/publish")]
    [Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
    public async Task<IActionResult> PublishIssue(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var editorId))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var issue = await _context.SoTapChis.FirstOrDefaultAsync(s => s.MaSoTapChi == id);
        if (issue == null) return NotFound(new { message = "Không tìm thấy số tạp chí." });
        if (issue.TrangThai == "Đã xuất bản" || issue.TrangThai == "Đã phát hành")
            return Ok(new { success = true, message = "Số tạp chí đã được phát hành trước đó." });

        var articles = await _context.BaiBaos.Include(b => b.ThuMucBaiBaos)
            .Where(b => b.MaSoTapChi == id).ToListAsync();
        if (articles.Count == 0)
            return BadRequest(new { message = "Không thể phát hành số tạp chí chưa có bài báo." });

        foreach (var article in articles)
        {
            if (article.TrangThai != "Sẵn sàng xuất bản")
                return BadRequest(new { message = $"Bài #{article.MaBaiBao} chưa được duyệt ở trạng thái Sẵn sàng xuất bản." });
            if (!article.TrangBatDau.HasValue || !article.TrangKetThuc.HasValue || article.TrangBatDau < 1 || article.TrangKetThuc < article.TrangBatDau)
                return BadRequest(new { message = $"Bài #{article.MaBaiBao} chưa có khoảng trang hợp lệ." });
            if (!article.ThuMucBaiBaos.Any(f => HasPhysicalPdf(f)))
                return BadRequest(new { message = $"Bài #{article.MaBaiBao} chưa có PDF thành phẩm thực tế trên máy chủ." });
        }

        var now = DateTime.Now;
        issue.TrangThai = "Đã xuất bản";
        issue.NgayPhatHanh ??= now;
        foreach (var article in articles)
        {
            var oldStatus = article.TrangThai;
            article.TrangThai = "Đã xuất bản";
            article.NgayCapNhat = now;
            _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
            {
                MaBaiBao = article.MaBaiBao,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = "Đã xuất bản",
                NgayChuyen = now,
                MaNguoiThucHien = editorId,
                GhiChu = $"Phát hành số tạp chí {issue.TenSo}."
            });
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { success = true, message = $"Đã phát hành {issue.TenSo} với {articles.Count} bài báo." });
    }

    private bool HasPhysicalPdf(ThuMucBaiBao file)
    {
        if ((file.LoaiThuMuc != "PDF thành phẩm" && file.LoaiThuMuc != "PDF Xuất bản") ||
            !file.TenThuMuc.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return false;
        var relative = file.DuongDan.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar).Any(part => part == "..")) return false;
        var root = Path.GetFullPath(_env.ContentRootPath);
        return new[] { Path.Combine(root, relative), Path.Combine(root, "wwwroot", relative) }
            .Select(Path.GetFullPath)
            .Any(path => path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                System.IO.File.Exists(path));
    }
}
