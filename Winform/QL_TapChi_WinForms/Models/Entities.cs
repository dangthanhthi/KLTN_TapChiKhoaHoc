using System;
using System.Collections.Generic;

namespace QL_TapChi_WinForms.Models
{
    public class VaiTro
    {
        public int MaVaiTro { get; set; }
        public string TenVaiTro { get; set; } = string.Empty;
        public string? MoTa { get; set; }
    }

    public class NguoiDung
    {
        public int MaNguoiDung { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? DonVi { get; set; }
        public string? HocVi { get; set; }
        public string? MaORCID { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public List<string> DanhSachVaiTro { get; set; } = new List<string>();
        public string VaiTroHienThi => DanhSachVaiTro.Count > 0 ? string.Join(", ", DanhSachVaiTro) : "Chưa phân quyền";
    }

    public class ChuyenNganh
    {
        public int MaChuyenNganh { get; set; }
        public string TenChuyenNganh { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public int SoBaiBao { get; set; } = 0;
    }

    public class SoTapChi
    {
        public int MaSoTapChi { get; set; }
        public string TenSo { get; set; } = string.Empty;
        public int Tap { get; set; }
        public int So { get; set; }
        public int Nam { get; set; }
        public DateTime? NgayPhatHanh { get; set; }
        public string TrangThai { get; set; } = "Đang biên tập";
        public int SoLuongBai { get; set; } = 0;

        public string HienThiTenSo => $"Tập {Tap}, Số {So} ({Nam}) - {TenSo}";
    }

    public class BaiBao
    {
        public int MaBaiBao { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? TieuDeTiengAnh { get; set; }
        public string? TomTat { get; set; }
        public string? TomTatTiengAnh { get; set; }
        public string? TuKhoa { get; set; }
        public string TrangThai { get; set; } = "Chờ sơ duyệt";
        public string? MaDOI { get; set; }
        public DateTime NgayGui { get; set; } = DateTime.Now;
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;
        public int MaNguoiDung { get; set; }
        public int MaChuyenNganh { get; set; }
        public int? MaSoTapChi { get; set; }
        public int? TrangBatDau { get; set; }
        public int? TrangKetThuc { get; set; }

        // Navigation properties
        public string? TenTacGia { get; set; }
        public string? EmailTacGia { get; set; }
        public string? DonViTacGia { get; set; }
        public string? TenChuyenNganh { get; set; }
        public string? TenSoTapChi { get; set; }
        public int SoPhanBienDaGiao { get; set; }
        public int SoPhanBienDaDanhGia { get; set; }

        public string MaDinhDanh => $"JST-{MaBaiBao:D4}";
    }

    public class DongTacGia
    {
        public int MaDongTacGia { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DonVi { get; set; }
        public int ThuTu { get; set; }
        public int MaBaiBao { get; set; }
    }

    public class TapTinBaiBao
    {
        public int MaTapTin { get; set; }
        public string TenTapTin { get; set; } = string.Empty;
        public string DuongDan { get; set; } = string.Empty;
        public string LoaiTapTin { get; set; } = string.Empty;
        public long KichThuoc { get; set; }
        public int SoVong { get; set; } = 1;
        public DateTime NgayTaiLen { get; set; } = DateTime.Now;
        public int MaBaiBao { get; set; }
    }

    public class PhanCongPhanBien
    {
        public int MaPhanCong { get; set; }
        public int SoVong { get; set; } = 1;
        public DateTime NgayPhanCong { get; set; } = DateTime.Now;
        public DateTime? HanPhanHoi { get; set; }
        public DateTime? HanHoanThanh { get; set; }
        public string TrangThai { get; set; } = "Chờ phản hồi";
        public int MaBaiBao { get; set; }
        public int MaNguoiDung { get; set; }

        // Join properties
        public string? TenPhanBien { get; set; }
        public string? EmailPhanBien { get; set; }
        public string? DonViPhanBien { get; set; }
        public string? TieuDeBaiBao { get; set; }
        public decimal? DiemTongKet { get; set; }
        public string? KienNghi { get; set; }
    }

    public class PhieuDanhGia
    {
        public int MaPhieu { get; set; }
        public decimal? DiemTinhMoi { get; set; }
        public decimal? DiemPhuongPhap { get; set; }
        public decimal? DiemKetQua { get; set; }
        public decimal? DiemTrinhBay { get; set; }
        public decimal? DiemTongKet { get; set; }
        public string? NhanXetChoTacGia { get; set; }
        public string? NhanXetBaoMat { get; set; }
        public string? KienNghi { get; set; }
        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
        public int MaPhanCong { get; set; }
    }

    public class LichSuTrangThai
    {
        public int MaLichSu { get; set; }
        public int MaBaiBao { get; set; }
        public string? TrangThaiCu { get; set; }
        public string TrangThaiMoi { get; set; } = string.Empty;
        public DateTime NgayChuyen { get; set; } = DateTime.Now;
        public int? MaNguoiThucHien { get; set; }
        public string? TenNguoiThucHien { get; set; }
        public string? GhiChu { get; set; }
    }
}
