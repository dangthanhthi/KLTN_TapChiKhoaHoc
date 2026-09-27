using System.ComponentModel.DataAnnotations;

namespace HuitJournal.Api.DTOs;

public class PhanCongRequestDto
{
    [Required]
    public int MaBaiBao { get; set; }

    [Required]
    public int MaNguoiDungReviewer { get; set; }

    public int? SoVong { get; set; }
    public DateTime? HanPhanHoi { get; set; }
    public DateTime? HanHoanThanh { get; set; }
    [MaxLength(1000)]
    public string? LyDo { get; set; }
}

public class PhieuDanhGiaDto
{
    [Required]
    public int MaPhanCong { get; set; }

    [Range(0, 10)]
    public decimal? DiemTinhMoi { get; set; }

    [Range(0, 10)]
    public decimal? DiemPhuongPhap { get; set; }

    [Range(0, 10)]
    public decimal? DiemKetQua { get; set; }

    [Range(0, 10)]
    public decimal? DiemTrinhBay { get; set; }

    [Range(0, 10)]
    public decimal? DiemTongKet { get; set; }

    public string? NhanXetChoTacGia { get; set; }
    public string? NhanXetBaoMat { get; set; }

    [Required]
    public string KienNghi { get; set; } = null!; // 'Chấp nhận đăng', 'Chỉnh sửa nhỏ', 'Chỉnh sửa lớn và phản biện lại', 'Từ chối đăng'
}

public class QuyetDinhBienTapDto
{
    [Required]
    public int MaBaiBao { get; set; }

    [Required]
    public string TrangThaiMoi { get; set; } = null!; // 'Chấp nhận', 'Chờ chỉnh sửa', 'Từ chối', etc.

    public string? GhiChu { get; set; }

    [MaxLength(2000)]
    public string? ThongBaoChoTacGia { get; set; }
}

public class PhanCongListItemDto
{
    public int MaPhanCong { get; set; }
    public int MaBaiBao { get; set; }
    public string TieuDeBaiBao { get; set; } = null!;
    public string ChuyenNganh { get; set; } = null!;
    public int SoVong { get; set; }
    public DateTime NgayPhanCong { get; set; }
    public DateTime? HanPhanHoi { get; set; }
    public DateTime? HanHoanThanh { get; set; }
    public string TrangThai { get; set; } = null!;
    public bool DaDanhGia { get; set; }
    public decimal? DiemTongKet { get; set; }
    public string? KienNghi { get; set; }
}
