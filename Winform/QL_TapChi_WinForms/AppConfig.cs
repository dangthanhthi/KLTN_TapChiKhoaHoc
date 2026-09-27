using System;

namespace QL_TapChi_WinForms
{
    public static class AppConfig
    {
        public static string AppName => "TẠP CHÍ KHOA HỌC ĐẠI HỌC CÔNG THƯƠNG";
        public static string ShortName => "JST";
        public static string Version => "1.0.0 (KLCN-2026)";

        // Nghiệp vụ WinForms và Web đều dùng Backend API. Cấu hình URL khi triển khai nhiều máy.
        public static string ApiBaseUrl =>
            (Environment.GetEnvironmentVariable("HUIT_JOURNAL_API_URL") ?? "http://localhost:5000").TrimEnd('/');

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
