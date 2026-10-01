using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuitJournal.Api.Models;

[Table("VaiTro")]
public class VaiTro
{
    [Key]
    public int MaVaiTro { get; set; }

    [Required]
    [MaxLength(100)]
    public string TenVaiTro { get; set; } = null!;

    [MaxLength(500)]
    public string? MoTa { get; set; }

    public virtual ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
}

[Table("NguoiDung")]
public class NguoiDung
{
    [Key]
    public int MaNguoiDung { get; set; }

    [MaxLength(100)]
    public string? TenDangNhap { get; set; }

    [MaxLength(100)]
    public string? HoDem { get; set; }

    [MaxLength(50)]
    public string? Ten { get; set; }

    [Required]
    [MaxLength(150)]
    public string HoTen { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string MatKhau { get; set; } = null!;

    [MaxLength(50)]
    public string HocVi { get; set; } = "Không";

    [MaxLength(50)]
    public string HocHam { get; set; } = "Không";

    [MaxLength(10)]
    public string GioiTinh { get; set; } = "Nam";

    [MaxLength(50)]
    public string NgonNgu { get; set; } = "Tiếng Việt";

    [MaxLength(100)]
    public string QuocGia { get; set; } = "Vietnam";

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    [MaxLength(255)]
    public string? DonVi { get; set; }

    [MaxLength(255)]
    public string? DiaChi { get; set; }

    [MaxLength(50)]
    public string? SoTaiKhoan { get; set; }

    [MaxLength(150)]
    public string? ChuTaiKhoan { get; set; }

    [MaxLength(150)]
    public string? NganHang { get; set; }

    [MaxLength(50)]
    public string? MaORCID { get; set; }

    [MaxLength(500)]
    public string? AnhDaiDien { get; set; }

    public bool TrangThai { get; set; } = true;

    public DateTime NgayTao { get; set; } = DateTime.Now;

    public virtual ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
    public virtual ICollection<NguoiDungChuyenMon> NguoiDungChuyenMons { get; set; } = new List<NguoiDungChuyenMon>();
    public virtual ICollection<BaiBao> BaiBaos { get; set; } = new List<BaiBao>();
    public virtual ICollection<DongTacGia> DongTacGias { get; set; } = new List<DongTacGia>();
    public virtual ICollection<PhanCongPhanBien> PhanCongPhanBiens { get; set; } = new List<PhanCongPhanBien>();
    public virtual ICollection<PhanBienDeXuat> PhanBienDeXuats { get; set; } = new List<PhanBienDeXuat>();
    public virtual ICollection<DonDangKyPhanBien> DonDangKyPhanBiens { get; set; } = new List<DonDangKyPhanBien>();
}

[Table("NguoiDung_VaiTro")]
public class NguoiDungVaiTro
{
    public int MaNguoiDung { get; set; }
    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public int MaVaiTro { get; set; }
    public virtual VaiTro VaiTro { get; set; } = null!;
}

[Table("ChuyenNganh")]
public class ChuyenNganh
{
    [Key]
    public int MaChuyenNganh { get; set; }

    [Required]
    [MaxLength(255)]
    public string TenChuyenNganh { get; set; } = null!;

    [MaxLength(500)]
    public string? MoTa { get; set; }

    public virtual ICollection<NguoiDungChuyenMon> NguoiDungChuyenMons { get; set; } = new List<NguoiDungChuyenMon>();
    public virtual ICollection<BaiBao> BaiBaos { get; set; } = new List<BaiBao>();
}

[Table("NguoiDung_ChuyenMon")]
public class NguoiDungChuyenMon
{
    public int MaNguoiDung { get; set; }
    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public int MaChuyenNganh { get; set; }
    public virtual ChuyenNganh ChuyenNganh { get; set; } = null!;

    public bool LaChuyenMonChinh { get; set; } = true;

    [MaxLength(255)]
    public string? GhiChu { get; set; }

    public DateTime NgayDangKy { get; set; } = DateTime.Now;
}

[Table("SoTapChi")]
public class SoTapChi
{
    [Key]
    public int MaSoTapChi { get; set; }

    [Required]
    [MaxLength(255)]
    public string TenSo { get; set; } = null!;

    public int Tap { get; set; }
    public int So { get; set; }
    public int Nam { get; set; }

    public DateTime? NgayPhatHanh { get; set; }

    [MaxLength(50)]
    public string TrangThai { get; set; } = "Đang biên tập";

    public virtual ICollection<BaiBao> BaiBaos { get; set; } = new List<BaiBao>();
}

[Table("BaiBao")]
public class BaiBao
{
    [Key]
    public int MaBaiBao { get; set; }

    [Required]
    [MaxLength(500)]
    public string TieuDe { get; set; } = null!;

    [MaxLength(500)]
    public string? TieuDeTiengAnh { get; set; }

    public string? TomTat { get; set; }
    public string? TomTatTiengAnh { get; set; }

    [MaxLength(255)]
    public string? TuKhoa { get; set; }

    [MaxLength(50)]
    public string TrangThai { get; set; } = "Chờ sơ duyệt";

    [MaxLength(100)]
    public string? MaDOI { get; set; }

    public DateTime NgayGui { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    public int MaNguoiDung { get; set; }
    [ForeignKey(nameof(MaNguoiDung))]
    public virtual NguoiDung TacGia { get; set; } = null!;

    public int MaChuyenNganh { get; set; }
    [ForeignKey(nameof(MaChuyenNganh))]
    public virtual ChuyenNganh ChuyenNganh { get; set; } = null!;

    public int? MaSoTapChi { get; set; }
    [ForeignKey(nameof(MaSoTapChi))]
    public virtual SoTapChi? SoTapChi { get; set; }

    public int? TrangBatDau { get; set; }
    public int? TrangKetThuc { get; set; }

    public virtual ICollection<DongTacGia> DongTacGias { get; set; } = new List<DongTacGia>();
    public virtual ICollection<ThuMucBaiBao> ThuMucBaiBaos { get; set; } = new List<ThuMucBaiBao>();
    public virtual ICollection<PhanCongPhanBien> PhanCongPhanBiens { get; set; } = new List<PhanCongPhanBien>();
    public virtual ICollection<PhanBienDeXuat> PhanBienDeXuats { get; set; } = new List<PhanBienDeXuat>();
    public virtual ICollection<LichSuTrangThaiBaiBao> LichSuTrangThais { get; set; } = new List<LichSuTrangThaiBaiBao>();
}

[Table("DongTacGia")]
public class DongTacGia
{
    [Key]
    public int MaDongTacGia { get; set; }

    [Required]
    [MaxLength(100)]
    public string HoTen { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [MaxLength(255)]
    public string? DonVi { get; set; }

    [MaxLength(50)]
    public string? MaORCID { get; set; }

    public bool LaTacGiaLienHe { get; set; } = false;
    public int ThuTu { get; set; } = 1;

    public int MaBaiBao { get; set; }
    [ForeignKey(nameof(MaBaiBao))]
    public virtual BaiBao BaiBao { get; set; } = null!;

    public int? MaNguoiDung { get; set; }
    [ForeignKey(nameof(MaNguoiDung))]
    public virtual NguoiDung? NguoiDung { get; set; }
}

[Table("ThuMucBaiBao")]
public class ThuMucBaiBao
{
    [Key]
    public int MaThuMuc { get; set; }

    [Required]
    [MaxLength(255)]
    public string TenThuMuc { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string DuongDan { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LoaiThuMuc { get; set; } = null!;

    public long KichThuoc { get; set; }
    public int SoVong { get; set; } = 1;
    public DateTime NgayTaiLen { get; set; } = DateTime.Now;

    public int MaBaiBao { get; set; }
    [ForeignKey(nameof(MaBaiBao))]
    public virtual BaiBao BaiBao { get; set; } = null!;
}

[Table("PhanCongPhanBien")]
public class PhanCongPhanBien
{
    [Key]
    public int MaPhanCong { get; set; }

    public int SoVong { get; set; } = 1;
    public DateTime NgayPhanCong { get; set; } = DateTime.Now;
    public DateTime? HanPhanHoi { get; set; }
    public DateTime? HanHoanThanh { get; set; }

    [MaxLength(50)]
    public string TrangThai { get; set; } = "Chờ phản hồi";

    public int MaBaiBao { get; set; }
    [ForeignKey(nameof(MaBaiBao))]
    public virtual BaiBao BaiBao { get; set; } = null!;

    public int MaNguoiDung { get; set; }
    [ForeignKey(nameof(MaNguoiDung))]
    public virtual NguoiDung ChuyenGia { get; set; } = null!;

    public virtual PhieuDanhGia? PhieuDanhGia { get; set; }
    public virtual PhieuDanhGiaBanNhap? PhieuDanhGiaBanNhap { get; set; }
}

[Table("PhieuDanhGiaBanNhap")]
public class PhieuDanhGiaBanNhap
{
    [Key, ForeignKey(nameof(PhanCongPhanBien))]
    public int MaPhanCong { get; set; }
    [Column(TypeName = "decimal(3,1)")] public decimal? DiemTinhMoi { get; set; }
    [Column(TypeName = "decimal(3,1)")] public decimal? DiemPhuongPhap { get; set; }
    [Column(TypeName = "decimal(3,1)")] public decimal? DiemKetQua { get; set; }
    [Column(TypeName = "decimal(3,1)")] public decimal? DiemTrinhBay { get; set; }
    [Column(TypeName = "nvarchar(max)")] public string? NhanXetChoTacGia { get; set; }
    [Column(TypeName = "nvarchar(max)")] public string? NhanXetBaoMat { get; set; }
    [MaxLength(255)] public string? KienNghi { get; set; }
    public DateTimeOffset NgayCapNhatUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = null!;
    public virtual PhanCongPhanBien PhanCongPhanBien { get; set; } = null!;
}

[Table("PhieuDanhGia")]
public class PhieuDanhGia
{
    [Key]
    public int MaPhieu { get; set; }

    [Column(TypeName = "decimal(3,1)")]
    public decimal? DiemTinhMoi { get; set; }

    [Column(TypeName = "decimal(3,1)")]
    public decimal? DiemPhuongPhap { get; set; }

    [Column(TypeName = "decimal(3,1)")]
    public decimal? DiemKetQua { get; set; }

    [Column(TypeName = "decimal(3,1)")]
    public decimal? DiemTrinhBay { get; set; }

    [Column(TypeName = "decimal(3,1)")]
    public decimal? DiemTongKet { get; set; }

    public string? NhanXetChoTacGia { get; set; }
    public string? NhanXetBaoMat { get; set; }

    [MaxLength(255)]
    public string? KienNghi { get; set; }

    public DateTime NgayDanhGia { get; set; } = DateTime.Now;

    public int MaPhanCong { get; set; }
    [ForeignKey(nameof(MaPhanCong))]
    public virtual PhanCongPhanBien PhanCongPhanBien { get; set; } = null!;
}

[Table("LichSuTrangThaiBaiBao")]
public class LichSuTrangThaiBaiBao
{
    [Key]
    public int MaLichSu { get; set; }

    public int MaBaiBao { get; set; }
    [ForeignKey(nameof(MaBaiBao))]
    public virtual BaiBao BaiBao { get; set; } = null!;

    [MaxLength(50)]
    public string? TrangThaiCu { get; set; }

    [Required]
    [MaxLength(50)]
    public string TrangThaiMoi { get; set; } = null!;

    public DateTime NgayChuyen { get; set; } = DateTime.Now;

    public int? MaNguoiThucHien { get; set; }
    [ForeignKey(nameof(MaNguoiThucHien))]
    public virtual NguoiDung? NguoiThucHien { get; set; }

    [MaxLength(500)]
    public string? GhiChu { get; set; }

    [MaxLength(2000)]
    public string? ThongBaoChoTacGia { get; set; }
}

[Table("PhanBienDeXuat")]
public class PhanBienDeXuat
{
    [Key]
    public int MaDeXuat { get; set; }

    [Required]
    [MaxLength(150)]
    public string HoTen { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [MaxLength(255)]
    public string? DonVi { get; set; }

    [MaxLength(255)]
    public string? LinhVuc { get; set; }

    public bool LaChuyenGiaHeThong { get; set; } = false;

    public int MaBaiBao { get; set; }
    [ForeignKey(nameof(MaBaiBao))]
    public virtual BaiBao BaiBao { get; set; } = null!;

    public int? MaNguoiDung { get; set; }
    [ForeignKey(nameof(MaNguoiDung))]
    public virtual NguoiDung? NguoiDung { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;
}

[Table("DonDangKyPhanBien")]
public class DonDangKyPhanBien
{
    [Key]
    public int MaDon { get; set; }

    public int MaNguoiDung { get; set; }
    [ForeignKey(nameof(MaNguoiDung))]
    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public DateTime NgayDangKy { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? GhiChu { get; set; }

    [Required]
    [MaxLength(50)]
    public string TrangThai { get; set; } = "Chờ duyệt"; // 'Chờ duyệt', 'Đã duyệt', 'Từ chối'

    public int? MaNguoiDuyet { get; set; }
    [ForeignKey(nameof(MaNguoiDuyet))]
    public virtual NguoiDung? NguoiDuyet { get; set; }

    public DateTime? NgayDuyet { get; set; }

    [MaxLength(500)]
    public string? LyDoTuChoi { get; set; }
}

[Table("DangKyChoXacNhan")]
public class DangKyChoXacNhan
{
    [Key]
    public Guid MaDangKy { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    public string EmailGoc { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string EmailSoSanh { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string TenDangNhapSoSanh { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string MatKhauHash { get; set; } = null!;

    [MaxLength(50)]
    public string? HoDem { get; set; }

    [Required]
    [MaxLength(30)]
    public string Ten { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string HoTen { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string HocVi { get; set; } = "Không";

    [Required]
    [MaxLength(30)]
    public string HocHam { get; set; } = "Không";

    [Required]
    [MaxLength(10)]
    public string GioiTinh { get; set; } = "Nam";

    [Required]
    [MaxLength(50)]
    public string QuocGia { get; set; } = "Vietnam";

    [Required]
    [MaxLength(30)]
    public string NgonNgu { get; set; } = "Tiếng Việt";

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    [MaxLength(255)]
    public string? DonVi { get; set; }

    [MaxLength(255)]
    public string? DiaChi { get; set; }

    [MaxLength(30)]
    public string? SoTaiKhoan { get; set; }

    [MaxLength(100)]
    public string? ChuTaiKhoan { get; set; }

    [MaxLength(100)]
    public string? NganHang { get; set; }

    [MaxLength(30)]
    public string? MaORCID { get; set; }

    public int? ChuyenNganhId { get; set; }

    public bool DangKyPhanBien { get; set; } = false;

    public DateTime TaoLucUtc { get; set; } = DateTime.UtcNow;

    public DateTime HetHanHoSoUtc { get; set; }

    [Required]
    [MaxLength(20)]
    public string TrangThai { get; set; } = "Pending"; // 'Pending', 'Verified', 'Expired', 'Cancelled'

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<MaXacNhanEmail> MaXacNhanEmails { get; set; } = new List<MaXacNhanEmail>();
}

[Table("MaXacNhanEmail")]
public class MaXacNhanEmail
{
    [Key]
    public Guid MaId { get; set; } = Guid.NewGuid();

    public Guid MaDangKy { get; set; }

    [ForeignKey(nameof(MaDangKy))]
    public virtual DangKyChoXacNhan DangKyChoXacNhan { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string MaHash { get; set; } = null!;

    public DateTime TaoLucUtc { get; set; } = DateTime.UtcNow;

    public DateTime HetHanUtc { get; set; }

    public int SoLanNhapSai { get; set; } = 0;

    public DateTime? DaDungLucUtc { get; set; }

    public DateTime LanGuiCuoiUtc { get; set; } = DateTime.UtcNow;
}

[Table("EmailOutbox")]
public class EmailOutbox
{
    [Key]
    public Guid MaOutbox { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string LoaiThu { get; set; } = "XacNhanDangKy";

    [Required]
    [MaxLength(150)]
    public string NguoiNhan { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string TieuDe { get; set; } = null!;

    public string? NoiDungHtml { get; set; }

    public string? NoiDungText { get; set; }

    [Required]
    [MaxLength(20)]
    public string TrangThai { get; set; } = "Pending"; // 'Pending', 'Sent', 'Failed'

    public int SoLanThuLai { get; set; } = 0;

    [MaxLength(1000)]
    public string? LoiKyThuat { get; set; }

    public DateTime TaoLucUtc { get; set; } = DateTime.UtcNow;

    public DateTime? GuiLucUtc { get; set; }
}
