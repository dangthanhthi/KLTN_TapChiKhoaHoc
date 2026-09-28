using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Data;

namespace HuitJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChuyenNganhController : ControllerBase
{
    private readonly QLTapChiKhoaHocContext _context;

    public ChuyenNganhController(QLTapChiKhoaHocContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách chuyên ngành thời gian thực (Real-time) kèm số lượng bài báo khoa học đã xuất bản
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedCategories()
    {
        var categories = await _context.ChuyenNganhs
            .AsNoTracking()
            .Select(c => new
            {
                c.MaChuyenNganh,
                c.TenChuyenNganh,
                c.MoTa,
                TongSoBaiBao = c.BaiBaos.Count(b => b.TrangThai == "Đã xuất bản"
                    && b.SoTapChi != null
                    && (b.SoTapChi.TrangThai == "Đã xuất bản" || b.SoTapChi.TrangThai == "Đã phát hành")
                    && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                        && f.TenThuMuc.EndsWith(".pdf")))
            })
            .OrderByDescending(c => c.TongSoBaiBao)
            .ThenBy(c => c.MaChuyenNganh)
            .ToListAsync();

        return Ok(categories);
    }
}
