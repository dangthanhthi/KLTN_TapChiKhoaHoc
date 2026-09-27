using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using QL_TapChi_WinForms.Models;

namespace QL_TapChi_WinForms.Services
{
    public class PhanBienService
    {
        public static List<PhanCongPhanBien> GetPhanCongByBaiBao(int maBaiBao)
        {
            return JournalApiClient.Read<List<PhanCongPhanBien>>($"/api/desktop-editorial/assignments?maBaiBao={maBaiBao}") ?? new();
        }

        public static List<PhanCongPhanBien> GetAllPhanCong(string? filterStatus = null)
        {
            var path = "/api/desktop-editorial/assignments";
            if (!string.IsNullOrWhiteSpace(filterStatus) && filterStatus != "Tất cả")
                path += "?trangThai=" + Uri.EscapeDataString(filterStatus);
            return JournalApiClient.Read<List<PhanCongPhanBien>>(path) ?? new();
        }

        public static PhieuDanhGia? GetPhieuDanhGiaByPhanCong(int maPhanCong)
        {
            return JournalApiClient.Read<PhieuDanhGia>($"/api/desktop-editorial/assignments/{maPhanCong}/evaluation");
        }

        public static bool PhanCongReviewer(int maBaiBao, int maNguoiDung, DateTime hanPhanHoi, DateTime hanHoanThanh, int soVong = 1)
        {
            return JournalApiClient.Post("/api/phanbien/assign", new
            {
                maBaiBao,
                maNguoiDungReviewer = maNguoiDung,
                soVong,
                hanPhanHoi = hanPhanHoi.Date,
                hanHoanThanh = hanHoanThanh.Date
            }) != null;
        }

        public static bool MoiPhanBienThuBa(int maBaiBao, int maNguoiDung, DateTime hanPhanHoi, DateTime hanHoanThanh, string lyDo, int soVong = 1)
        {
            return JournalApiClient.Post("/api/phanbien/assign", new
            {
                maBaiBao,
                maNguoiDungReviewer = maNguoiDung,
                soVong,
                hanPhanHoi = hanPhanHoi.Date,
                hanHoanThanh = hanHoanThanh.Date,
                lyDo
            }) != null;
        }

        public static bool XoaPhanCong(int maPhanCong)
        {
            return JournalApiClient.Post($"/api/editorial-articles/assignments/{maPhanCong}/remove", new { }) != null;
        }

        public static bool GiaHanPhanBien(int maPhanCong, DateTime hanHoanThanhMoi)
        {
            return JournalApiClient.Post($"/api/editorial-articles/assignments/{maPhanCong}/extend",
                new { hanHoanThanh = hanHoanThanhMoi.Date }) != null;
        }

        public static bool LuuPhieuDanhGia(int maPhanCong, decimal tinhMoi, decimal phuongPhap, decimal ketQua, decimal trinhBay, string nhanXetTacGia, string nhanXetBaoMat, string kienNghi)
        {
            decimal tongKet = Math.Round((tinhMoi + phuongPhap + ketQua + trinhBay) / 4m, 1);
            return JournalApiClient.Post("/api/phanbien/evaluate", new
            {
                maPhanCong,
                diemTinhMoi = tinhMoi,
                diemPhuongPhap = phuongPhap,
                diemKetQua = ketQua,
                diemTrinhBay = trinhBay,
                diemTongKet = tongKet,
                nhanXetChoTacGia = nhanXetTacGia,
                nhanXetBaoMat,
                kienNghi
            }) != null;
        }
        private static PhanCongPhanBien MapRowToPhanCong(DataRow r)
        {
            return new PhanCongPhanBien
            {
                MaPhanCong = Convert.ToInt32(r["MaPhanCong"]),
                SoVong = Convert.ToInt32(r["SoVong"]),
                NgayPhanCong = Convert.ToDateTime(r["NgayPhanCong"]),
                HanPhanHoi = r["HanPhanHoi"] != DBNull.Value ? Convert.ToDateTime(r["HanPhanHoi"]) : null,
                HanHoanThanh = r["HanHoanThanh"] != DBNull.Value ? Convert.ToDateTime(r["HanHoanThanh"]) : null,
                TrangThai = r["TrangThai"].ToString() ?? "Chờ phản hồi",
                MaBaiBao = Convert.ToInt32(r["MaBaiBao"]),
                MaNguoiDung = Convert.ToInt32(r["MaNguoiDung"]),
                TenPhanBien = r["TenPhanBien"]?.ToString(),
                EmailPhanBien = r["EmailPhanBien"]?.ToString(),
                DonViPhanBien = r["DonViPhanBien"]?.ToString(),
                TieuDeBaiBao = r["TieuDeBaiBao"]?.ToString(),
                DiemTongKet = r["DiemTongKet"] != DBNull.Value ? Convert.ToDecimal(r["DiemTongKet"]) : null,
                KienNghi = r["KienNghi"]?.ToString()
            };
        }
    }
}

