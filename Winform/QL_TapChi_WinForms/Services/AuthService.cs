using System;
using System.Collections.Generic;
using System.Text.Json;
using QL_TapChi_WinForms.Models;

namespace QL_TapChi_WinForms.Services
{
    public static class AuthService
    {
        public static NguoiDung? CurrentUser { get; private set; }
        public static string? LastError { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null && !string.IsNullOrWhiteSpace(JournalApiClient.Token);

        public static bool Login(string email, string password)
        {
            Logout();
            var result = JournalApiClient.Post("/api/auth/login",
                new { usernameOrEmail = email.Trim(), password }, requireToken: false);
            if (result is not JsonElement json)
            {
                LastError = JournalApiClient.LastError;
                return false;
            }

            if (!json.TryGetProperty("success", out var success) || !success.GetBoolean() ||
                !json.TryGetProperty("token", out var token) || token.ValueKind != JsonValueKind.String ||
                !json.TryGetProperty("user", out var profile) || profile.ValueKind != JsonValueKind.Object)
            {
                LastError = JournalApiClient.ReadMessage(json) ?? "API trả về dữ liệu đăng nhập không hợp lệ.";
                return false;
            }

            var roles = new List<string>();
            if (profile.TryGetProperty("vaiTros", out var roleValues) && roleValues.ValueKind == JsonValueKind.Array)
                foreach (var role in roleValues.EnumerateArray())
                    if (role.ValueKind == JsonValueKind.String && role.GetString() is string name) roles.Add(name);

            if (!roles.Contains("Ban biên tập") && !roles.Contains("Quản trị hệ thống"))
            {
                LastError = "Ứng dụng tòa soạn chỉ dành cho Ban biên tập hoặc Quản trị hệ thống.";
                return false;
            }

            CurrentUser = new NguoiDung
            {
                MaNguoiDung = profile.GetProperty("maNguoiDung").GetInt32(),
                HoTen = profile.GetProperty("hoTen").GetString() ?? "",
                Email = profile.GetProperty("email").GetString() ?? "",
                HocVi = profile.TryGetProperty("hocVi", out var degree) ? degree.GetString() : null,
                DonVi = profile.TryGetProperty("donVi", out var institution) ? institution.GetString() : null,
                SoDienThoai = profile.TryGetProperty("soDienThoai", out var phone) ? phone.GetString() : null,
                MaORCID = profile.TryGetProperty("maORCID", out var orcid) ? orcid.GetString() : null,
                DanhSachVaiTro = roles
            };
            JournalApiClient.Token = token.GetString();
            LastError = null;
            return true;
        }

        public static void Logout()
        {
            CurrentUser = null;
            JournalApiClient.Token = null;
            LastError = null;
        }

        public static bool HasRole(string roleName) =>
            CurrentUser?.DanhSachVaiTro.Exists(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase)) == true;

        public static bool IsAdminOrEditor() => HasRole("Quản trị hệ thống") || HasRole("Ban biên tập");

        public static bool DoiMatKhau(int maNguoiDung, string matKhauCu, string matKhauMoi, out string? error)
        {
            if (CurrentUser?.MaNguoiDung != maNguoiDung)
            {
                error = "Chỉ có thể đổi mật khẩu của chính mình.";
                return false;
            }
            var result = JournalApiClient.Post("/api/auth/change-password",
                new { currentPassword = matKhauCu, newPassword = matKhauMoi });
            error = result is null ? JournalApiClient.LastError : null;
            return result != null;
        }

        public static bool DatLaiMatKhau(int maNguoiDung, string matKhauMacDinh, out string? error)
        {
            error = "Chức năng đặt lại mật khẩu quản trị chưa có API an toàn. Không thể cập nhật trực tiếp trong SQL.";
            return false;
        }
    }
}
