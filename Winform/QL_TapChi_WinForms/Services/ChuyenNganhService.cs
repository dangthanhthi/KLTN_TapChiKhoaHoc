using System.Collections.Generic;
using QL_TapChi_WinForms.Models;

namespace QL_TapChi_WinForms.Services
{
    public class ChuyenNganhService
    {
        public static List<ChuyenNganh> GetAllChuyenNganh() =>
            JournalApiClient.Read<List<ChuyenNganh>>("/api/desktop-editorial/categories") ?? new();

        public static bool SaveChuyenNganh(ChuyenNganh cn, out string? error)
        {
            var result = JournalApiClient.Post("/api/desktop-editorial/categories/save", new {
                cn.MaChuyenNganh, cn.TenChuyenNganh, cn.MoTa
            });
            error = result is null ? JournalApiClient.LastError : null;
            if (result is { } json && json.TryGetProperty("maChuyenNganh", out var id)) cn.MaChuyenNganh = id.GetInt32();
            return result != null;
        }

        public static bool DeleteChuyenNganh(int maChuyenNganh, out string? error)
        {
            var result = JournalApiClient.Post($"/api/desktop-editorial/categories/{maChuyenNganh}/delete", new { });
            error = result is null ? JournalApiClient.LastError : null;
            return result != null;
        }
    }
}
