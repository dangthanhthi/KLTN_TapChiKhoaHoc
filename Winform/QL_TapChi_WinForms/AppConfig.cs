using System;
using System.IO;
using System.Text.Json;

namespace QL_TapChi_WinForms
{
    public static class AppConfig
    {
        public static string AppName => "TẠP CHÍ KHOA HỌC ĐẠI HỌC CÔNG THƯƠNG";
        public static string ShortName => "JST";
        public static string Version => "1.0.0 (KLCN-2026)";

        // Máy tòa soạn dùng cùng API với Web; desktop-settings.json chỉ chứa URL công khai, không chứa bí mật.
        // Biến môi trường ưu tiên hơn tệp để hỗ trợ môi trường phát triển và triển khai.
        private static readonly Lazy<string> ConfiguredApiBaseUrl = new(LoadApiBaseUrl);
        public static string ApiBaseUrl => ConfiguredApiBaseUrl.Value;

        private static string LoadApiBaseUrl()
        {
            var raw = Environment.GetEnvironmentVariable("HUIT_JOURNAL_API_URL");
            if (string.IsNullOrWhiteSpace(raw))
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "desktop-settings.json");
                if (File.Exists(configPath))
                {
                    try
                    {
                        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
                        raw = document.RootElement.GetProperty("ApiBaseUrl").GetString();
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException)
                    {
                        throw new InvalidOperationException("Tệp desktop-settings.json không hợp lệ. Hãy kiểm tra ApiBaseUrl.", ex);
                    }
                }
            }

            raw = string.IsNullOrWhiteSpace(raw) ? "http://localhost:5000" : raw.Trim().TrimEnd('/');
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) ||
                uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
            {
                throw new InvalidOperationException("ApiBaseUrl phải là origin HTTPS công khai hoặc HTTP localhost, không có đường dẫn /api.");
            }
            return uri.GetLeftPart(UriPartial.Authority);
        }

        // Chỉ dùng cho công cụ sao lưu/phục hồi cục bộ của quản trị viên.
        public static string[] ConnectionStrings =
            Environment.GetEnvironmentVariable("HUIT_JOURNAL_DB_CONNECTION") is string configured && !string.IsNullOrWhiteSpace(configured)
            ? new[] { configured }
            : new[]
        {
            "Server=.;Database=QL_TapChiKhoaHoc;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;",
            "Server=.\\CSSQL22;Database=QL_TapChiKhoaHoc;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;",
            "Server=.\\SQLEXPRESS;Database=QL_TapChiKhoaHoc;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;",
            "Server=(localdb)\\MSSQLLocalDB;Database=QL_TapChiKhoaHoc;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;"
        };

        public static string CurrentConnectionString { get; set; } = ConnectionStrings[0];
    }
}
