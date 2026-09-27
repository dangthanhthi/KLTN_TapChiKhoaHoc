using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

public sealed class DesktopUserRequest
{
    public int MaNguoiDung { get; set; }
    [Required, MaxLength(150)] public string HoTen { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    public string? MatKhauMoi { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DonVi { get; set; }
    public string? HocVi { get; set; }
    public string? MaORCID { get; set; }
    public bool TrangThai { get; set; } = true;
    public List<int> VaiTroIds { get; set; } = new();
}

[ApiController]
[Route("api/desktop-admin")]
[Authorize(Roles = "Quản trị hệ thống")]
public class DesktopAdminController : ControllerBase
{
    private readonly QLTapChiKhoaHocContext _context;
    public DesktopAdminController(QLTapChiKhoaHocContext context) => _context = context;

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] string? keyword, [FromQuery] string? role)
    {
        var query = _context.NguoiDungs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(u => u.HoTen.Contains(term) || u.Email.Contains(term) ||
                (u.DonVi != null && u.DonVi.Contains(term)) || (u.MaORCID != null && u.MaORCID.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(role) && role != "Tất cả")
            query = query.Where(u => u.NguoiDungVaiTros.Any(r => r.VaiTro.TenVaiTro == role));
        return Ok(await query.OrderByDescending(u => u.MaNguoiDung).Select(u => new {
            u.MaNguoiDung, u.HoTen, u.Email, u.SoDienThoai, u.DonVi, u.HocVi,
            u.MaORCID, u.TrangThai, u.NgayTao,
            DanhSachVaiTro = u.NguoiDungVaiTros.Select(r => r.VaiTro.TenVaiTro).ToList()
        }).ToListAsync());
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles() => Ok(await _context.VaiTros.AsNoTracking()
        .OrderBy(r => r.MaVaiTro).Select(r => new { r.MaVaiTro, r.TenVaiTro, r.MoTa }).ToListAsync());

    [HttpGet("dashboard/role-distribution")]
    public async Task<IActionResult> RoleDistribution() => Ok(await _context.VaiTros.AsNoTracking()
        .OrderBy(r => r.MaVaiTro)
        .Select(r => new { r.TenVaiTro, SoLuong = r.NguoiDungVaiTros.Count })
        .ToListAsync());

    [HttpGet("dashboard/recent-history")]
    public async Task<IActionResult> RecentHistory() => Ok(await _context.LichSuTrangThais.AsNoTracking()
        .OrderByDescending(h => h.NgayChuyen).ThenByDescending(h => h.MaLichSu).Take(15)
        .Select(h => new { h.NgayChuyen, HoTen = h.NguoiThucHien == null ? "Hệ thống" : h.NguoiThucHien.HoTen,
            h.TrangThaiMoi, h.GhiChu }).ToListAsync());

    [HttpPost("users/save")]
    public async Task<IActionResult> SaveUser([FromBody] DesktopUserRequest dto)
    {
        var actorId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : 0;
        if (actorId == 0) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        if (dto.VaiTroIds.Count == 0 || dto.VaiTroIds.Distinct().Count() != dto.VaiTroIds.Count)
            return BadRequest(new { message = "Chọn ít nhất một vai trò hợp lệ." });
        var validRoleCount = await _context.VaiTros.CountAsync(r => dto.VaiTroIds.Contains(r.MaVaiTro));
        if (validRoleCount != dto.VaiTroIds.Count)
            return BadRequest(new { message = "Danh sách vai trò không hợp lệ." });
        if (await _context.NguoiDungs.AnyAsync(u => u.Email == dto.Email && u.MaNguoiDung != dto.MaNguoiDung))
            return BadRequest(new { message = "Email đã được sử dụng." });

        var isNew = dto.MaNguoiDung == 0;
        if (isNew && (string.IsNullOrWhiteSpace(dto.MatKhauMoi) || dto.MatKhauMoi.Length < 8))
            return BadRequest(new { message = "Mật khẩu tài khoản mới phải có ít nhất 8 ký tự." });
        if (!isNew && !string.IsNullOrEmpty(dto.MatKhauMoi))
            return BadRequest(new { message = "Dùng quy trình đặt lại mật khẩu riêng; không sửa mật khẩu khi cập nhật hồ sơ." });

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var user = isNew ? new NguoiDung { MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi!, 11) }
            : await _context.NguoiDungs.Include(u => u.NguoiDungVaiTros).FirstOrDefaultAsync(u => u.MaNguoiDung == dto.MaNguoiDung);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });
        if (dto.MaNguoiDung == actorId && (!dto.TrangThai || !dto.VaiTroIds.Contains(1)))
            return BadRequest(new { message = "Không thể tự khóa hoặc tự bỏ quyền quản trị." });
        if (!isNew && user.NguoiDungVaiTros.Any(r => r.MaVaiTro == 1) && (!dto.TrangThai || !dto.VaiTroIds.Contains(1)))
        {
            var activeAdmins = await _context.NguoiDungs.CountAsync(u => u.TrangThai && u.NguoiDungVaiTros.Any(r => r.MaVaiTro == 1));
            if (activeAdmins <= 1) return BadRequest(new { message = "Không thể khóa quản trị viên cuối cùng." });
        }

        user.HoTen = dto.HoTen.Trim();
        user.Email = dto.Email.Trim();
        user.SoDienThoai = dto.SoDienThoai;
        user.DonVi = dto.DonVi;
        user.HocVi = dto.HocVi ?? "Không";
        user.MaORCID = dto.MaORCID;
        user.TrangThai = dto.TrangThai;
        if (isNew) _context.NguoiDungs.Add(user);
        await _context.SaveChangesAsync();

        var oldRoles = user.NguoiDungVaiTros.Select(v => v.MaVaiTro).ToHashSet();
        _context.NguoiDungVaiTros.RemoveRange(user.NguoiDungVaiTros.Where(v => !dto.VaiTroIds.Contains(v.MaVaiTro)));
        foreach (var roleId in dto.VaiTroIds.Where(id => !oldRoles.Contains(id)))
            _context.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = user.MaNguoiDung, MaVaiTro = roleId });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { success = true, maNguoiDung = user.MaNguoiDung });
    }

    [HttpPost("users/{id}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] System.Text.Json.JsonElement body)
    {
        if (!body.TryGetProperty("trangThai", out var status) || status.ValueKind is not (System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False))
            return BadRequest(new { message = "Trạng thái không hợp lệ." });
        var user = await _context.NguoiDungs.FindAsync(id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });
        if (!status.GetBoolean())
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
                return BadRequest(new { message = "Không thể tự khóa tài khoản đang đăng nhập." });
            if (await _context.NguoiDungVaiTros.AnyAsync(v => v.MaNguoiDung == id && v.MaVaiTro == 1) &&
                await _context.NguoiDungs.CountAsync(u => u.TrangThai && u.NguoiDungVaiTros.Any(v => v.MaVaiTro == 1)) <= 1)
                return BadRequest(new { message = "Không thể khóa quản trị viên cuối cùng." });
        }
        user.TrangThai = status.GetBoolean();
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("users/{id}/archive")]
    public async Task<IActionResult> ArchiveUser(int id)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return BadRequest(new { message = "Không thể tự khóa tài khoản đang đăng nhập." });
        var user = await _context.NguoiDungs.FindAsync(id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });
        if (await _context.NguoiDungVaiTros.AnyAsync(v => v.MaNguoiDung == id && v.MaVaiTro == 1) &&
            await _context.NguoiDungs.CountAsync(u => u.TrangThai && u.NguoiDungVaiTros.Any(v => v.MaVaiTro == 1)) <= 1)
            return BadRequest(new { message = "Không thể khóa quản trị viên cuối cùng." });
        user.TrangThai = false;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã khóa tài khoản; dữ liệu học thuật được bảo toàn." });
    }
}
