# Dùng WinForms trên máy Windows khác

Web trên Vercel gọi `/api` qua rewrite đến Backend. WinForms chạy cục bộ trên Windows và gọi **cùng Backend API HTTPS** trực tiếp. Hai ứng dụng chỉ đồng bộ khi cùng API và CSDL; Vercel không chạy file `.exe`.

## Đóng gói nội bộ

Từ thư mục gốc dự án:

```powershell
.\deploy\winforms\publish.ps1
```

Script tạo `dist/winforms/HUIT-Journal-Desktop-win-x64.zip` và in SHA256. Trong ZIP có ứng dụng tự chứa .NET 8 và `desktop-settings.json` với `ApiBaseUrl` là `https://huit-journal-api.runasp.net`. Giải nén **toàn bộ** vào một thư mục trên máy Windows 64-bit rồi chạy `QL_TapChi_WinForms.exe`. Máy cần Internet và tài khoản có vai trò Ban biên tập hoặc Quản trị hệ thống. Không đặt mật khẩu, JWT hoặc chuỗi kết nối SQL trong ZIP hay Git.

Nếu chưa tải được runtime Windows từ NuGet, dùng `.\deploy\winforms\publish.ps1 -FrameworkDependent`. Gói này cần cài **.NET 8 Desktop Runtime** trên máy nhận; nó vẫn có cùng `desktop-settings.json` và toàn bộ thư viện của dự án.

Để chọn API khác, chạy `publish.ps1 -ApiOrigin https://ten-mien-api-cua-ban`. Trong môi trường phát triển, biến `HUIT_JOURNAL_API_URL` trên máy ưu tiên hơn tệp cấu hình; nếu không có cả hai, ứng dụng gọi `http://localhost:5000`. Chỉ chấp nhận HTTPS cho API công khai. URL cấu hình là origin, không thêm `/api`.

## Giới hạn hiện tại

Quản lý bài báo, phân công phản biện và xuất bản đi qua Backend API. Chức năng sao lưu/phục hồi trong WinForms vẫn dùng SQL Server trực tiếp, cần quyền quản trị SQL ở **đúng máy/CSDL của Backend** và không nên mở cổng SQL cho Internet. Trên máy tòa soạn từ xa, không dùng chức năng này cho đến khi có tác vụ sao lưu phía máy chủ. Các tệp bản thảo tải lên/xuống đi qua API, nên cần kiểm tra giới hạn kích thước và thời gian của host.

Khi ổn định phiên bản, có thể đưa ZIP lên GitHub Releases và công bố liên kết tải từ website. Nếu cần cập nhật tự động, cấu hình ClickOnce/MSIX và ký phát hành sau. Chỉ phát hành cho nhân sự tòa soạn; phản biện viên dùng `reviewer.html` trên web.

Trước khi bán thương mại, nâng cả WinForms (`net8.0-windows`) và Backend (`net9.0`) lên .NET 10 LTS, sau khi xác nhận host API hỗ trợ .NET 10; hai phiên bản hiện tại hết hỗ trợ trong tháng 11/2026 theo lịch Microsoft.
