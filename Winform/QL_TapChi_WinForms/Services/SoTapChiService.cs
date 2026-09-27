using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using QL_TapChi_WinForms.Models;

namespace QL_TapChi_WinForms.Services
{
    public class SoTapChiService
    {
        public static List<SoTapChi> GetAllSoTapChi()
        {
            return JournalApiClient.Read<List<SoTapChi>>("/api/desktop-editorial/issues") ?? new();
        }

        public static List<ChuyenNganh> GetAllChuyenNganh()
        {
            return ChuyenNganhService.GetAllChuyenNganh();
        }

        public static bool SaveSoTapChi(SoTapChi so)
        {
            var result = JournalApiClient.Post("/api/sotapchi/draft/save", new
            {
                maSoTapChi = so.MaSoTapChi,
                tenSo = so.TenSo,
                tap = so.Tap,
                so = so.So,
                nam = so.Nam,
                ngayPhatHanh = so.NgayPhatHanh,
                trangThai = so.TrangThai
            });
            if (result is System.Text.Json.JsonElement json && json.TryGetProperty("maSoTapChi", out var id))
                so.MaSoTapChi = id.GetInt32();
            return result != null;
        }

        public static bool XoaSoTapChi(int maSoTapChi, out string? error)
        {
            var result = JournalApiClient.Post($"/api/sotapchi/{maSoTapChi}/draft/delete", new { });
            error = result is null ? JournalApiClient.LastError : null;
            return result != null;
        }
        public static bool PhatHanhSoBao(int maSoTapChi, out string? error)
        {
            var result = JournalApiClient.Post($"/api/sotapchi/{maSoTapChi}/publish", new { });
            error = result is null ? JournalApiClient.LastError : null;
            return result != null;
        }
    }
}

