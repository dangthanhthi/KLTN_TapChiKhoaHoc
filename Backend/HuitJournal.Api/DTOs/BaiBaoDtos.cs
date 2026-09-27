using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HuitJournal.Api.DTOs;

public class DongTacGiaSubmitDto
{
    [Required]
    [MaxLength(100)]
    public string HoTen { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [MaxLength(255)]
    public string? DonVi { get; set; }

    [MaxLength(50)]
    public string? MaORCID { get; set; }

    public bool LaTacGiaLienHe { get; set; } = false;

    public int ThuTu { get; set; } = 1;
}

public class PhanBienDeXuatSubmitDto
{
    [Required]
    [MaxLength(150)]
    public string HoTen { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [MaxLength(255)]
    public string? DonVi { get; set; }

    [MaxLength(255)]
    public string? LinhVuc { get; set; }

    public bool LaChuyenGiaHeThong { get; set; } = false;

    public int? MaNguoiDung { get; set; }
}

public class BaiBaoSubmitDto
{
    [Required(ErrorMessage = "Tiêu đề tiếng Việt là bắt buộc.")]
    [MaxLength(500)]
    public string TieuDe { get; set; } = null!;

    [MaxLength(500)]
    public string? TieuDeTiengAnh { get; set; }

    [Required(ErrorMessage = "Tóm tắt bài báo là bắt buộc.")]
    public string TomTat { get; set; } = null!;

    public string? TomTatTiengAnh { get; set; }

    [Required(ErrorMessage = "Từ khóa là bắt buộc.")]
    [MaxLength(255)]
    public string TuKhoa { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn chuyên ngành phù hợp.")]
    public int MaChuyenNganh { get; set; }

    /// <summary>
    /// Chuỗi JSON chứa danh sách đồng tác giả (nếu gửi qua FormData)
    /// </summary>
    public string? DongTacGiaJson { get; set; }

    /// <summary>
    /// Chuỗi JSON chứa danh sách chuyên gia phản biện do tác giả đề xuất
    /// </summary>
    public string? PhanBienDeXuatJson { get; set; }

    /// <summary>
    /// File bản thảo Word (.docx) hoặc PDF (.pdf)
    /// </summary>
    public IFormFile? TapTinBanThao { get; set; }
}

public class BaiBaoListItemDto
{
    public int MaBaiBao { get; set; }
    public string MaDinhDanh { get; set; } = null!; // Ví dụ: JST-2026-SUB{id}
    public string TieuDe { get; set; } = null!;
    public string? TieuDeTiengAnh { get; set; }
    public string ChuyenNganh { get; set; } = null!;
    public int MaChuyenNganh { get; set; }
    public string TrangThai { get; set; } = null!;
    public DateTime NgayGui { get; set; }
    public DateTime NgayCapNhat { get; set; }
    public int SoDongTacGia { get; set; }
    public string? TapTinGoc { get; set; }
}

public class BaiBaoDetailDto
{
    public int MaBaiBao { get; set; }
    public string MaDinhDanh { get; set; } = null!;
    public string TieuDe { get; set; } = null!;
    public string? TieuDeTiengAnh { get; set; }
    public string? TomTat { get; set; }
    public string? TomTatTiengAnh { get; set; }
    public string? TuKhoa { get; set; }
    public string TrangThai { get; set; } = null!;
    public string? MaDOI { get; set; }
    public DateTime NgayGui { get; set; }
    public DateTime NgayCapNhat { get; set; }

    public int MaNguoiDung { get; set; }
    public string TacGiaChinh { get; set; } = null!;
    public string EmailTacGiaChinh { get; set; } = null!;

    public int MaChuyenNganh { get; set; }
    public string TenChuyenNganh { get; set; } = null!;

    public int? MaSoTapChi { get; set; }
    public string? TenSoTapChi { get; set; }
    public int? TrangBatDau { get; set; }
    public int? TrangKetThuc { get; set; }

    public List<DongTacGiaDetailDto> DongTacGias { get; set; } = new();
    public List<PhanBienDeXuatDto> PhanBienDeXuats { get; set; } = new();
    public List<ThuMucBaiBaoDto> TapTins { get; set; } = new();
    public List<LichSuTrangThaiDto> LichSuTrangThais { get; set; } = new();
}

public class PhanBienDeXuatDto
{
    public int MaDeXuat { get; set; }
    public string HoTen { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? DonVi { get; set; }
    public string? LinhVuc { get; set; }
    public bool LaChuyenGiaHeThong { get; set; }
    public int? MaNguoiDung { get; set; }
    public DateTime NgayTao { get; set; }
}

public class DongTacGiaDetailDto
{
    public int MaDongTacGia { get; set; }
    public string HoTen { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? DonVi { get; set; }
    public string? MaORCID { get; set; }
    public bool LaTacGiaLienHe { get; set; }
    public int ThuTu { get; set; }
    public int? MaNguoiDung { get; set; }
    public bool DaLienKetTaiKhoan => MaNguoiDung.HasValue;
}

public class ThuMucBaiBaoDto
{
    public int MaThuMuc { get; set; }
    public string TenThuMuc { get; set; } = null!;
    public string DuongDan { get; set; } = null!;
    public string LoaiThuMuc { get; set; } = null!;
    public long KichThuoc { get; set; }
    public int SoVong { get; set; }
    public DateTime NgayTaiLen { get; set; }
}

public class LichSuTrangThaiDto
{
    public int MaLichSu { get; set; }
    public string? TrangThaiCu { get; set; }
    public string TrangThaiMoi { get; set; } = null!;
    public DateTime NgayChuyen { get; set; }
    public string? NguoiThucHien { get; set; }
    public string? GhiChu { get; set; }
}

public class BaiBaoPublicDto
{
    public int MaBaiBao { get; set; }
    public int? MaSoTapChi { get; set; }
    public string TieuDe { get; set; } = null!;
    public string? TieuDeTiengAnh { get; set; }
    public string? TomTat { get; set; }
    public string? TomTatTiengAnh { get; set; }
    public string? TuKhoa { get; set; }
    public string? MaDOI { get; set; }
    public DateTime NgayGui { get; set; }
    public DateTime? NgayPhatHanh { get; set; }
    public string ChuyenNganh { get; set; } = null!;
    public string? TenSoTapChi { get; set; }
    public int? Tap { get; set; }
    public int? So { get; set; }
    public int? Nam { get; set; }
    public int? TrangBatDau { get; set; }
    public int? TrangKetThuc { get; set; }
    public string? FilePdfUrl { get; set; }

    public List<DongTacGiaDetailDto> TacGias { get; set; } = new();
}

public class BaiBaoResubmitDto
{
    [Required(ErrorMessage = "Vui lòng nhập nội dung giải trình tiếp thu ý kiến phản biện.")]
    public string GiaiTrinh { get; set; } = null!;

    public IFormFile? FileBm03 { get; set; }
    public IFormFile? FileClean { get; set; }
    public IFormFile? FileTracked { get; set; }
}

public class AssignIssueDto
{
    [Required(ErrorMessage = "Mã số tạp chí là bắt buộc.")]
    public int MaSoTapChi { get; set; }
    public int? TrangBatDau { get; set; }
    public int? TrangKetThuc { get; set; }
    public string? MaDOI { get; set; }
}

