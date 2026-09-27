using System;
using System.Collections.Generic;
using QL_TapChi_WinForms.Models;

namespace QL_TapChi_WinForms.Services
{
    public class NguoiDungService
    {
        public static List<NguoiDung> GetAllUsers(string? keyword = null, string? roleFilter = null)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(keyword)) query.Add("keyword=" + Uri.EscapeDataString(keyword.Trim()));
            if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "Tất cả")
                query.Add("role=" + Uri.EscapeDataString(roleFilter));
            var path = "/api/desktop-admin/users" + (query.Count == 0 ? "" : "?" + string.Join("&", query));
            return JournalApiClient.Read<List<NguoiDung>>(path) ?? new();
        }

        public static List<NguoiDung> GetReviewers() =>
            JournalApiClient.Read<List<NguoiDung>>("/api/desktop-editorial/reviewers") ?? new();

        public static List<VaiTro> GetAllRoles() =>
            JournalApiClient.Read<List<VaiTro>>("/api/desktop-admin/roles") ?? new();

        public static bool SaveUser(NguoiDung u, List<int> selectedRoleIds)
        {
            var result = JournalApiClient.Post("/api/desktop-admin/users/save", new {
                maNguoiDung = u.MaNguoiDung, hoTen = u.HoTen, email = u.Email,
                matKhauMoi = u.MaNguoiDung == 0 ? u.MatKhau : null,
                soDienThoai = u.SoDienThoai, donVi = u.DonVi, hocVi = u.HocVi,
                maORCID = u.MaORCID, trangThai = u.TrangThai, vaiTroIds = selectedRoleIds
            });
            if (result is System.Text.Json.JsonElement json && json.TryGetProperty("maNguoiDung", out var id))
                u.MaNguoiDung = id.GetInt32();
            return result != null;
        }

        public static bool ToggleUserStatus(int maNguoiDung, bool newStatus) =>
            JournalApiClient.Post($"/api/desktop-admin/users/{maNguoiDung}/status", new { trangThai = newStatus }) != null;

        public static bool DeleteUser(int maNguoiDung, out string message, out bool isSoftDeleted)
        {
            var result = JournalApiClient.Post($"/api/desktop-admin/users/{maNguoiDung}/archive", new { });
            isSoftDeleted = result != null;
            message = result is System.Text.Json.JsonElement json
                ? JournalApiClient.ReadMessage(json) ?? "Đã khóa tài khoản."
                : JournalApiClient.LastError ?? "Không thể khóa tài khoản.";
            return result != null;
        }
    }
}
