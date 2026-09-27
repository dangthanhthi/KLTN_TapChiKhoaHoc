using System;
using System.Collections.Generic;
using System.Linq;
using QL_TapChi_WinForms.Models;

namespace QL_TapChi_WinForms.Services
{
    public class BaiBaoService
    {
        private const string Root = "/api/desktop-editorial/articles";
        private static string Q(string value) => Uri.EscapeDataString(value);

        public static List<BaiBao> GetAllBaiBao(string? keyword = null, string? trangThai = null,
            int? maChuyenNganh = null, IEnumerable<string>? danhSachTrangThai = null)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(keyword)) query.Add($"keyword={Q(keyword.Trim())}");
            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "Tất cả") query.Add($"trangThai={Q(trangThai)}");
            if (maChuyenNganh > 0) query.Add($"maChuyenNganh={maChuyenNganh}");
            var list = JournalApiClient.Read<List<BaiBao>>(Root + (query.Count == 0 ? "" : "?" + string.Join("&", query))) ?? new();
            if (danhSachTrangThai != null)
            {
                var allowed = danhSachTrangThai.ToHashSet(StringComparer.Ordinal);
                if (allowed.Count > 0) list = list.Where(b => allowed.Contains(b.TrangThai)).ToList();
            }
            return list;
        }

        public static BaiBao? GetBaiBaoById(int maBaiBao) => JournalApiClient.Read<BaiBao>($"{Root}/{maBaiBao}");
        public static List<DongTacGia> GetDongTacGia(int maBaiBao) =>
            JournalApiClient.Read<List<DongTacGia>>($"{Root}/{maBaiBao}/coauthors") ?? new();
        public static List<TapTinBaiBao> GetTapTinBaiBao(int maBaiBao) =>
            JournalApiClient.Read<List<TapTinBaiBao>>($"{Root}/{maBaiBao}/files") ?? new();
        public static List<LichSuTrangThai> GetLichSuTrangThai(int maBaiBao) =>
            JournalApiClient.Read<List<LichSuTrangThai>>($"{Root}/{maBaiBao}/history") ?? new();

        public static bool ChuyenTrangThai(int maBaiBao, string trangThaiMoi, string ghiChu) =>
            JournalApiClient.Post("/api/phanbien/decision", new {
                maBaiBao, trangThaiMoi, ghiChu, thongBaoChoTacGia = (string?)null
            }) != null;

        public static bool GanSoTapChi(int maBaiBao, int maSoTapChi, int? trangBatDau, int? trangKetThuc, string? maDOI) =>
            JournalApiClient.Post($"/api/baibao/{maBaiBao}/assign-issue", new {
                maSoTapChi, trangBatDau, trangKetThuc, maDOI
            }) != null;

        public static List<BaiBao> GetBaiBaoChuaGanSo() =>
            JournalApiClient.Read<List<BaiBao>>(Root + "?chuaGanSo=true") ?? new();

        public static bool GoBaiKhoiSo(int maBaiBao, out string? error)
        {
            var result = JournalApiClient.Post($"/api/editorial-articles/{maBaiBao}/unassign", new { });
            error = result is null ? JournalApiClient.LastError : null;
            return result != null;
        }

        public static bool CapNhatTrangVaDOI(int maBaiBao, int? start, int? end, string? doi, out string? error)
        {
            var result = JournalApiClient.Post($"/api/editorial-articles/{maBaiBao}/pages", new {
                trangBatDau = start, trangKetThuc = end, maDOI = doi
            });
            error = result is null ? JournalApiClient.LastError : null;
            return result != null;
        }

        public static bool ThemBaiBao(string tieuDe, string? tieuDeEn, string? tomTat, string? tuKhoa,
            int maChuyenNganh, int maTacGia, out int newId)
        {
            newId = 0;
            var result = JournalApiClient.Post(Root, new { tieuDe, tieuDeTiengAnh = tieuDeEn,
                tomTat, tuKhoa, maChuyenNganh, maTacGia });
            if (result is not { } json || !json.TryGetProperty("maBaiBao", out var id)) return false;
            newId = id.GetInt32();
            return true;
        }

        public static bool CapNhatThongTinBaiBao(int maBaiBao, string tieuDe, string? tieuDeEn,
            string? tomTat, string? tuKhoa, int maChuyenNganh)
        {
            var article = GetBaiBaoById(maBaiBao);
            if (article == null) return false;
            return JournalApiClient.Post($"{Root}/{maBaiBao}/edit", new {
                tieuDe, tieuDeTiengAnh = tieuDeEn, tomTat, tuKhoa, maChuyenNganh,
                maTacGia = article.MaNguoiDung
            }) != null;
        }

        public static bool XoaBaiBao(int maBaiBao, out string? error)
        {
            var result = JournalApiClient.Post($"{Root}/{maBaiBao}/withdraw", new { });
            error = result is null ? JournalApiClient.LastError : null;
            return result != null;
        }

        public static decimal KiemTraDaoVan(int maBaiBao, out string ketLuan)
        {
            ketLuan = "Chưa tích hợp hệ thống đối sánh đạo văn. Không có kết quả kiểm tra xác thực.";
            return -1m;
        }
    }
}
