using System.Security.Claims;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

[ApiController]
[Route("api/editorial-articles")]
[Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
public class EditorialArticleController : ControllerBase
{
    public sealed class ExtendAssignmentRequest
    {
        public DateTime HanHoanThanh { get; set; }
    }
    public sealed class PageMetadataRequest
    {
        public int? TrangBatDau { get; set; }
        public int? TrangKetThuc { get; set; }
        public string? MaDOI { get; set; }
    }

    private readonly QLTapChiKhoaHocContext _context;
    public EditorialArticleController(QLTapChiKhoaHocContext context) => _context = context;

    [HttpPost("{id}/pages")]
    public async Task<IActionResult> UpdatePages(int id, [FromBody] PageMetadataRequest dto)
    {
        var article = await _context.BaiBaos.Include(b => b.SoTapChi).FirstOrDefaultAsync(b => b.MaBaiBao == id);
        if (article == null) return NotFound(new { message = "Không tìm thấy bài báo." });
        if (article.MaSoTapChi == null) return BadRequest(new { message = "Bài báo chưa được xếp vào số tạp chí." });
        if (article.TrangThai == "Đã xuất bản" || article.SoTapChi?.TrangThai == "Đã xuất bản" || article.SoTapChi?.TrangThai == "Đã phát hành")
            return BadRequest(new { message = "Không thể sửa trang hoặc DOI của bài đã phát hành." });
        if (dto.TrangBatDau.HasValue != dto.TrangKetThuc.HasValue ||
            (dto.TrangBatDau.HasValue && (dto.TrangBatDau < 1 || dto.TrangKetThuc < dto.TrangBatDau)))
            return BadRequest(new { message = "Khoảng trang không hợp lệ." });
        article.TrangBatDau = dto.TrangBatDau;
        article.TrangKetThuc = dto.TrangKetThuc;
        article.MaDOI = string.IsNullOrWhiteSpace(dto.MaDOI) ? null : dto.MaDOI.Trim();
        article.NgayCapNhat = DateTime.Now;
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/unassign")]
    public async Task<IActionResult> Unassign(int id)
    {
        var article = await _context.BaiBaos.Include(b => b.SoTapChi).FirstOrDefaultAsync(b => b.MaBaiBao == id);
        if (article == null) return NotFound(new { message = "Không tìm thấy bài báo." });
        if (article.MaSoTapChi == null) return BadRequest(new { message = "Bài báo chưa thuộc số tạp chí nào." });
        if (article.TrangThai == "Đã xuất bản" || article.SoTapChi?.TrangThai == "Đã xuất bản" || article.SoTapChi?.TrangThai == "Đã phát hành")
            return BadRequest(new { message = "Không thể gỡ bài khỏi số đã phát hành." });
        var oldStatus = article.TrangThai;
        article.MaSoTapChi = null;
        article.TrangBatDau = null;
        article.TrangKetThuc = null;
        article.MaDOI = null;
        article.TrangThai = "Đang chế bản";
        article.NgayCapNhat = DateTime.Now;
        _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            MaBaiBao = id,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = article.TrangThai,
            NgayChuyen = DateTime.Now,
            MaNguoiThucHien = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            GhiChu = "Ban biên tập gỡ bài khỏi số tạp chí trước phát hành."
        });
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("assignments/{id}/extend")]
    public async Task<IActionResult> ExtendAssignment(int id, [FromBody] ExtendAssignmentRequest dto)
    {
        var assignment = await _context.PhanCongPhanBiens.Include(p => p.BaiBao).FirstOrDefaultAsync(p => p.MaPhanCong == id);
        if (assignment == null) return NotFound(new { message = "Không tìm thấy phân công phản biện." });
        if (assignment.BaiBao.TrangThai is "Đã xuất bản" or "Từ chối" or "Đã rút" ||
            assignment.TrangThai is "Đã đánh giá" or "Từ chối phản biện" ||
            dto.HanHoanThanh.Date <= DateTime.Today ||
            (assignment.HanPhanHoi.HasValue && dto.HanHoanThanh.Date < assignment.HanPhanHoi.Value.Date.AddDays(3)) ||
            (assignment.HanHoanThanh.HasValue && dto.HanHoanThanh.Date <= assignment.HanHoanThanh.Value.Date))
            return BadRequest(new { message = "Chỉ được gia hạn phân công đang xử lý với hạn mới muộn hơn hạn cũ và sau hạn phản hồi ít nhất 3 ngày." });
        assignment.HanHoanThanh = dto.HanHoanThanh.Date;
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("assignments/{id}/remove")]
    public async Task<IActionResult> RemoveAssignment(int id)
    {
        var assignment = await _context.PhanCongPhanBiens.Include(p => p.BaiBao).Include(p => p.PhieuDanhGia)
            .FirstOrDefaultAsync(p => p.MaPhanCong == id);
        if (assignment == null) return NotFound(new { message = "Không tìm thấy phân công phản biện." });
        if (assignment.PhieuDanhGia != null || assignment.BaiBao.TrangThai == "Đã xuất bản")
            return BadRequest(new { message = "Không thể xóa phân công đã có phiếu BM-04 hoặc bài đã xuất bản." });
        if (assignment.BaiBao.TrangThai == "Đang phản biện" && assignment.TrangThai != "Từ chối phản biện" &&
            await _context.PhanCongPhanBiens.CountAsync(p => p.MaBaiBao == assignment.MaBaiBao &&
                p.SoVong == assignment.SoVong && p.TrangThai != "Từ chối phản biện") <= 2)
            return BadRequest(new { message = "Không thể giảm số chuyên gia xuống dưới hai khi vòng phản biện đang mở." });
        _context.PhanCongPhanBiens.Remove(assignment);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
