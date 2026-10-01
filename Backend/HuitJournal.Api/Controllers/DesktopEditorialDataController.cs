using System.Security.Claims;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using HuitJournal.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

public sealed class EditorialArticleRequest
{
    public string TieuDe { get; set; } = "";
    public string? TieuDeTiengAnh { get; set; }
    public string? TomTat { get; set; }
    public string? TuKhoa { get; set; }
    public int MaChuyenNganh { get; set; }
    public int MaTacGia { get; set; }
}

public sealed class EditorialCategoryRequest
{
    public int MaChuyenNganh { get; set; }
    public string TenChuyenNganh { get; set; } = "";
    public string? MoTa { get; set; }
}

public sealed class ReviewerRoleRequest
{
    public int MaNguoiDung { get; set; }
}

[ApiController]
[Route("api/desktop-editorial")]
[Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
public sealed class DesktopEditorialDataController : ControllerBase
{
    private readonly QLTapChiKhoaHocContext _db;
    private readonly IWebHostEnvironment _env;
    public DesktopEditorialDataController(QLTapChiKhoaHocContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    [HttpGet("articles")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> Articles([FromQuery] string? keyword, [FromQuery] string? trangThai,
        [FromQuery] int? maChuyenNganh, [FromQuery] bool chuaGanSo = false)
    {
        var query = _db.BaiBaos.AsNoTracking().Where(b => b.TrangThai != "Đã rút");
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(b => b.TieuDe.Contains(term) || (b.TuKhoa != null && b.TuKhoa.Contains(term)) ||
                b.TacGia.HoTen.Contains(term) || b.MaBaiBao.ToString().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "Tất cả") query = query.Where(b => b.TrangThai == trangThai);
        if (maChuyenNganh > 0) query = query.Where(b => b.MaChuyenNganh == maChuyenNganh);
        if (chuaGanSo) query = query.Where(b => b.MaSoTapChi == null &&
            (b.TrangThai == "Đang chế bản" || b.TrangThai == "Chấp nhận đăng" ||
             b.TrangThai == "Đã chấp nhận" || b.TrangThai == "Sẵn sàng xuất bản"));
        return Ok(await query.OrderByDescending(b => b.NgayCapNhat).ThenByDescending(b => b.MaBaiBao)
            .Select(b => new {
                b.MaBaiBao, b.TieuDe, b.TieuDeTiengAnh, b.TomTat, b.TomTatTiengAnh, b.TuKhoa,
                b.TrangThai, b.MaDOI, b.NgayGui, b.NgayCapNhat, b.MaNguoiDung, b.MaChuyenNganh,
                b.MaSoTapChi, b.TrangBatDau, b.TrangKetThuc,
                TenTacGia = b.TacGia.HoTen, EmailTacGia = b.TacGia.Email, DonViTacGia = b.TacGia.DonVi,
                TenChuyenNganh = b.ChuyenNganh.TenChuyenNganh, TenSoTapChi = b.SoTapChi == null ? null : b.SoTapChi.TenSo,
                SoPhanBienDaGiao = b.PhanCongPhanBiens.Count,
                SoPhanBienDaDanhGia = b.PhanCongPhanBiens.Count(p => p.TrangThai == "Đã đánh giá")
            }).ToListAsync());
    }

    [HttpGet("articles/{id:int}")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> Article(int id)
    {
        var article = await _db.BaiBaos.AsNoTracking().Where(b => b.MaBaiBao == id && b.TrangThai != "Đã rút")
            .Select(b => new {
                b.MaBaiBao, b.TieuDe, b.TieuDeTiengAnh, b.TomTat, b.TomTatTiengAnh, b.TuKhoa,
                b.TrangThai, b.MaDOI, b.NgayGui, b.NgayCapNhat, b.MaNguoiDung, b.MaChuyenNganh,
                b.MaSoTapChi, b.TrangBatDau, b.TrangKetThuc,
                TenTacGia = b.TacGia.HoTen, EmailTacGia = b.TacGia.Email, DonViTacGia = b.TacGia.DonVi,
                TenChuyenNganh = b.ChuyenNganh.TenChuyenNganh, TenSoTapChi = b.SoTapChi == null ? null : b.SoTapChi.TenSo,
                SoPhanBienDaGiao = b.PhanCongPhanBiens.Count,
                SoPhanBienDaDanhGia = b.PhanCongPhanBiens.Count(p => p.TrangThai == "Đã đánh giá")
            }).FirstOrDefaultAsync();
        return article == null ? NotFound(new { message = "Không tìm thấy bài báo." }) : Ok(article);
    }

    [HttpGet("articles/{id:int}/coauthors")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> Coauthors(int id) => Ok(await _db.DongTacGias.AsNoTracking()
        .Where(c => c.MaBaiBao == id).OrderBy(c => c.ThuTu)
        .Select(c => new { c.MaDongTacGia, c.HoTen, c.Email, c.DonVi, c.ThuTu, c.MaBaiBao }).ToListAsync());

    [HttpGet("articles/{id:int}/files")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> Files(int id) => Ok(await _db.ThuMucBaiBaos.AsNoTracking()
        .Where(f => f.MaBaiBao == id).OrderByDescending(f => f.SoVong).ThenByDescending(f => f.NgayTaiLen)
        .Select(f => new { MaTapTin = f.MaThuMuc, TenTapTin = f.TenThuMuc, f.DuongDan,
            LoaiTapTin = f.LoaiThuMuc, f.KichThuoc, f.SoVong, f.NgayTaiLen, f.MaBaiBao }).ToListAsync());

    [HttpGet("files/{id:int}/download")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var record = await _db.ThuMucBaiBaos.AsNoTracking().FirstOrDefaultAsync(f => f.MaThuMuc == id);
        if (record == null) return NotFound(new { message = "Không tìm thấy tệp." });
        var normalized = record.DuongDan.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized) || normalized.Split(Path.DirectorySeparatorChar).Any(part => part == ".."))
            return BadRequest(new { message = "Đường dẫn tệp không hợp lệ." });
        var path = UploadStoragePaths.ResolveExistingFile(_env.ContentRootPath, record.DuongDan);
        if (path == null) return NotFound(new { message = "Tệp không tồn tại trên máy chủ." });
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch { ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword", _ => "application/octet-stream" };
        return PhysicalFile(path, contentType, Path.GetFileName(record.TenThuMuc));
    }

    [HttpGet("articles/{id:int}/history")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> History(int id) => Ok(await _db.LichSuTrangThais.AsNoTracking()
        .Where(h => h.MaBaiBao == id).OrderByDescending(h => h.NgayChuyen).ThenByDescending(h => h.MaLichSu)
        .Select(h => new { h.MaLichSu, h.MaBaiBao, h.TrangThaiCu, h.TrangThaiMoi, h.NgayChuyen,
            h.MaNguoiThucHien, TenNguoiThucHien = h.NguoiThucHien == null ? "Hệ thống" : h.NguoiThucHien.HoTen,
            h.GhiChu }).ToListAsync());

    [HttpPost("articles")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> CreateArticle([FromBody] EditorialArticleRequest dto)
    {
        var invalid = await ValidateArticle(dto, true);
        if (invalid != null) return BadRequest(new { message = invalid });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Unauthorized();
        var now = DateTime.Now;
        var article = new BaiBao { TieuDe = dto.TieuDe.Trim(), TieuDeTiengAnh = dto.TieuDeTiengAnh?.Trim(),
            TomTat = dto.TomTat?.Trim(), TuKhoa = dto.TuKhoa?.Trim(), MaChuyenNganh = dto.MaChuyenNganh,
            MaNguoiDung = dto.MaTacGia, TrangThai = "Chờ sơ duyệt", NgayGui = now, NgayCapNhat = now };
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.BaiBaos.Add(article);
        await _db.SaveChangesAsync();
        _db.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao { MaBaiBao = article.MaBaiBao,
            TrangThaiMoi = article.TrangThai, NgayChuyen = now, MaNguoiThucHien = actorId,
            GhiChu = "Tòa soạn tiếp nhận hồ sơ bản thảo." });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { success = true, maBaiBao = article.MaBaiBao });
    }

    [HttpPost("articles/{id:int}/edit")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> EditArticle(int id, [FromBody] EditorialArticleRequest dto)
    {
        var article = await _db.BaiBaos.FindAsync(id);
        if (article == null) return NotFound(new { message = "Không tìm thấy bài báo." });
        if (article.TrangThai == "Đã xuất bản" || article.TrangThai == "Đã rút")
            return BadRequest(new { message = "Không thể sửa bài đã phát hành hoặc đã rút." });
        if (dto.MaTacGia != article.MaNguoiDung)
            return BadRequest(new { message = "Không thể đổi tác giả chính qua thao tác sửa thông tin bài báo." });
        var invalid = await ValidateArticle(dto, false);
        if (invalid != null) return BadRequest(new { message = invalid });
        article.TieuDe = dto.TieuDe.Trim(); article.TieuDeTiengAnh = dto.TieuDeTiengAnh?.Trim();
        article.TomTat = dto.TomTat?.Trim(); article.TuKhoa = dto.TuKhoa?.Trim();
        article.MaChuyenNganh = dto.MaChuyenNganh; article.NgayCapNhat = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("articles/{id:int}/withdraw")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> WithdrawArticle(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Unauthorized();
        var article = await _db.BaiBaos.FindAsync(id);
        if (article == null) return NotFound(new { message = "Không tìm thấy bài báo." });
        if (article.TrangThai != "Chờ sơ duyệt" && article.TrangThai != "Chờ sửa hình thức")
            return BadRequest(new { message = "Chỉ được rút hồ sơ trước khi bắt đầu phản biện." });
        if (await _db.PhanCongPhanBiens.AnyAsync(p => p.MaBaiBao == id))
            return BadRequest(new { message = "Bài đã phân công phản biện, không thể rút hồ sơ." });
        var old = article.TrangThai;
        article.TrangThai = "Đã rút"; article.NgayCapNhat = DateTime.Now;
        _db.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao { MaBaiBao = id, TrangThaiCu = old,
            TrangThaiMoi = article.TrangThai, NgayChuyen = article.NgayCapNhat,
            MaNguoiThucHien = actorId, GhiChu = "Tòa soạn rút hồ sơ trước phản biện." });
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    private async Task<string?> ValidateArticle(EditorialArticleRequest dto, bool validateAuthor)
    {
        if (string.IsNullOrWhiteSpace(dto.TieuDe) || dto.TieuDe.Length > 500 ||
            dto.TieuDeTiengAnh?.Length > 500 || dto.TuKhoa?.Length > 255)
            return "Tiêu đề hoặc từ khóa không hợp lệ.";
        if (!await _db.ChuyenNganhs.AnyAsync(c => c.MaChuyenNganh == dto.MaChuyenNganh))
            return "Chuyên ngành không tồn tại.";
        if (validateAuthor && !await _db.NguoiDungs.AnyAsync(u => u.MaNguoiDung == dto.MaTacGia && u.TrangThai))
            return "Tác giả không tồn tại hoặc đã bị khóa.";
        return null;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories() => Ok(await _db.ChuyenNganhs.AsNoTracking()
        .OrderBy(c => c.MaChuyenNganh).Select(c => new { c.MaChuyenNganh, c.TenChuyenNganh,
            c.MoTa, SoBaiBao = c.BaiBaos.Count }).ToListAsync());

    [HttpPost("categories/save")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> SaveCategory([FromBody] EditorialCategoryRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenChuyenNganh) || dto.TenChuyenNganh.Length > 255 || dto.MoTa?.Length > 500)
            return BadRequest(new { message = "Tên hoặc mô tả chuyên ngành không hợp lệ." });
        if (await _db.ChuyenNganhs.AnyAsync(c => c.TenChuyenNganh == dto.TenChuyenNganh.Trim() && c.MaChuyenNganh != dto.MaChuyenNganh))
            return BadRequest(new { message = "Tên chuyên ngành đã tồn tại." });
        var category = dto.MaChuyenNganh == 0 ? new ChuyenNganh() : await _db.ChuyenNganhs.FindAsync(dto.MaChuyenNganh);
        if (category == null) return NotFound(new { message = "Không tìm thấy chuyên ngành." });
        category.TenChuyenNganh = dto.TenChuyenNganh.Trim(); category.MoTa = dto.MoTa?.Trim();
        if (dto.MaChuyenNganh == 0) _db.ChuyenNganhs.Add(category);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, maChuyenNganh = category.MaChuyenNganh });
    }

    [HttpPost("categories/{id:int}/delete")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.ChuyenNganhs.FindAsync(id);
        if (category == null) return NotFound(new { message = "Không tìm thấy chuyên ngành." });
        if (await _db.BaiBaos.AnyAsync(b => b.MaChuyenNganh == id) ||
            await _db.NguoiDungChuyenMons.AnyAsync(c => c.MaChuyenNganh == id))
            return BadRequest(new { message = "Chuyên ngành đang được bài báo hoặc chuyên gia sử dụng." });
        _db.ChuyenNganhs.Remove(category); await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("issues")]
    public async Task<IActionResult> Issues() => Ok(await _db.SoTapChis.AsNoTracking()
        .OrderByDescending(s => s.Nam).ThenByDescending(s => s.Tap).ThenByDescending(s => s.So)
        .Select(s => new { s.MaSoTapChi, s.TenSo, s.Tap, s.So, s.Nam, s.NgayPhatHanh,
            s.TrangThai, SoLuongBai = s.BaiBaos.Count }).ToListAsync());

    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics()
    {
        var articleCounts = await _db.BaiBaos.AsNoTracking().Where(b => b.TrangThai != "Đã rút")
            .GroupBy(b => b.TrangThai).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        int Count(string status) => articleCounts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;
        return Ok(new {
            TongBaiBao = articleCounts.Sum(c => c.Count), ChoSoDuyet = Count("Chờ sơ duyệt"),
            DangPhanBien = Count("Đang phản biện"),
            CanChinhSua = Count("Chờ sửa hình thức") + Count("Chờ chỉnh sửa"),
            ChoQuyetDinh = Count("Chờ quyết định"), DaXuatBan = Count("Đã xuất bản"),
            TongNguoiDung = await _db.NguoiDungs.CountAsync(),
            TongPhanBien = await _db.NguoiDungs.CountAsync(u => u.NguoiDungVaiTros.Any(r => r.VaiTro.TenVaiTro == "Chuyên gia phản biện")),
            TongSoTapChi = await _db.SoTapChis.CountAsync()
        });
    }

    [HttpGet("metrics/by-status")]
    public async Task<IActionResult> CountsByStatus() => Ok(await _db.BaiBaos.AsNoTracking()
        .Where(b => b.TrangThai != "Đã rút")
        .GroupBy(b => b.TrangThai).Select(g => new { g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Key, x => x.Count));

    [HttpGet("metrics/by-category")]
    public async Task<IActionResult> CountsByCategory() => Ok(await _db.ChuyenNganhs.AsNoTracking()
        .Select(c => new { c.TenChuyenNganh, Count = c.BaiBaos.Count(b => b.TrangThai != "Đã rút") })
        .ToDictionaryAsync(x => x.TenChuyenNganh, x => x.Count));

    [HttpGet("reviewers")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> Reviewers() => Ok(await _db.NguoiDungs.AsNoTracking()
        .Where(u => u.TrangThai && u.NguoiDungVaiTros.Any(r => r.VaiTro.TenVaiTro == "Chuyên gia phản biện"))
        .OrderBy(u => u.HoTen).Select(u => new { u.MaNguoiDung, u.HoTen, u.Email, u.HocVi, u.DonVi }).ToListAsync());

    [HttpGet("reviewer-candidates")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public async Task<IActionResult> ReviewerCandidates([FromQuery] string? keyword)
    {
        var query = _db.NguoiDungs.AsNoTracking().Where(u => u.TrangThai &&
            !u.NguoiDungVaiTros.Any(r => r.VaiTro.TenVaiTro == "Chuyên gia phản biện") &&
            (u.HocVi == "Thạc sĩ" || u.HocVi == "Tiến sĩ" || u.HocVi == "TSKH" ||
             u.HocHam == "Phó giáo sư" || u.HocHam == "Giáo sư"));
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(u => u.HoTen.Contains(term) || u.Email.Contains(term));
        }
        return Ok(await query.OrderBy(u => u.HoTen).Take(100)
            .Select(u => new { u.MaNguoiDung, u.HoTen, u.Email, u.HocVi, u.HocHam, u.DonVi })
            .ToListAsync());
    }

    [HttpPost("reviewers/grant")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập")]
    public async Task<IActionResult> GrantReviewer([FromBody] ReviewerRoleRequest dto)
    {
        if (dto.MaNguoiDung <= 0) return BadRequest(new { message = "Mã người dùng không hợp lệ." });
        var user = await _db.NguoiDungs.Include(u => u.NguoiDungVaiTros)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == dto.MaNguoiDung && u.TrangThai);
        if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản đang hoạt động." });
        if (user.HocVi is not ("Thạc sĩ" or "Tiến sĩ" or "TSKH") &&
            user.HocHam is not ("Phó giáo sư" or "Giáo sư"))
            return BadRequest(new { message = "Ứng viên cần có học vị tối thiểu Thạc sĩ hoặc học hàm tương đương." });
        var role = await _db.VaiTros.FirstOrDefaultAsync(r => r.TenVaiTro == "Chuyên gia phản biện");
        if (role == null) return StatusCode(503, new { message = "CSDL chưa có vai trò Chuyên gia phản biện." });
        if (user.NguoiDungVaiTros.Any(r => r.MaVaiTro == role.MaVaiTro))
            return Ok(new { success = true, message = "Tài khoản đã có quyền phản biện." });
        _db.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = user.MaNguoiDung, MaVaiTro = role.MaVaiTro });
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = $"Đã cấp quyền phản biện cho {user.HoTen}." });
    }

    [HttpGet("assignments")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> Assignments([FromQuery] int? maBaiBao, [FromQuery] string? trangThai)
    {
        var query = _db.PhanCongPhanBiens.AsNoTracking().AsQueryable();
        if (maBaiBao > 0) query = query.Where(p => p.MaBaiBao == maBaiBao);
        if (trangThai == "Quá hạn") query = query.Where(p => p.TrangThai != "Đã đánh giá" &&
            p.TrangThai != "Từ chối phản biện" && p.HanHoanThanh < DateTime.Now);
        else if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "Tất cả") query = query.Where(p => p.TrangThai == trangThai);
        return Ok(await query.OrderByDescending(p => p.NgayPhanCong).Select(p => new {
            p.MaPhanCong, p.SoVong, p.NgayPhanCong, p.HanPhanHoi, p.HanHoanThanh, p.TrangThai,
            p.MaBaiBao, p.MaNguoiDung, TenPhanBien = p.ChuyenGia.HoTen, EmailPhanBien = p.ChuyenGia.Email,
            DonViPhanBien = p.ChuyenGia.DonVi, TieuDeBaiBao = p.BaiBao.TieuDe,
            DiemTongKet = p.PhieuDanhGia == null ? null : p.PhieuDanhGia.DiemTongKet,
            KienNghi = p.PhieuDanhGia == null ? null : p.PhieuDanhGia.KienNghi
        }).ToListAsync());
    }

    [HttpGet("assignments/{id:int}/evaluation")]
    [Authorize(Roles = "Quản trị hệ thống,Tổng biên tập,Ban biên tập")]
    public async Task<IActionResult> Evaluation(int id)
    {
        var evaluation = await _db.PhieuDanhGias.AsNoTracking().Where(p => p.MaPhanCong == id)
            .Select(p => new { p.MaPhieu, p.DiemTinhMoi, p.DiemPhuongPhap, p.DiemKetQua,
                p.DiemTrinhBay, p.DiemTongKet, p.NhanXetChoTacGia, p.NhanXetBaoMat,
                p.KienNghi, p.NgayDanhGia, p.MaPhanCong }).FirstOrDefaultAsync();
        return evaluation == null ? NotFound(new { message = "Chưa có phiếu đánh giá." }) : Ok(evaluation);
    }
}
