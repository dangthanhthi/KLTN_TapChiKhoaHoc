# HUIT Journal of Science — Khóa luận tốt nghiệp

Mã nguồn gồm `Web/` (HTML/CSS/JS), `Backend/HuitJournal.Api/` (ASP.NET Core .NET 9), `Winform/` (ứng dụng tòa soạn), các script SQL và tài liệu trong `HoTro/`.

## Chạy trên máy phát triển

1. Khôi phục CSDL `QL_TapChiKhoaHoc` từ bản sao lưu **cục bộ** hoặc các script SQL phù hợp. Áp dụng `HoTro/Migration_20260927_EmailVerificationSchema.sql` cho luồng xác nhận email.
2. Tạo `Backend/HuitJournal.Api/appsettings.Local.json` trên máy, đặt `Jwt:Key`, `EmailVerification:HmacSecretKey`, SMTP và chuỗi kết nối nếu khác mặc định. Tệp này bị Git bỏ qua. Có thể bắt đầu từ `Backend/HuitJournal.Api/appsettings.json`; không chép bí mật vào file được theo dõi.
3. Chạy `Chay_Backend.bat`, `Chay_Web.bat` hoặc chạy Web trực tiếp từ Backend ở `http://localhost:5000`.

## Triển khai

GitHub chứa mã nguồn, script và tài liệu dự án. Tệp `.bak`, `.zip`, thư mục Uploads và cấu hình bí mật được giữ ngoài Git. Vercel xuất bản thư mục `dist/` do `scripts/build-web.mjs` tạo từ các tài nguyên Web cần thiết, không đưa SQL/test lên website; API .NET và SQL Server cần máy chủ riêng. Khi API có HTTPS, đặt biến môi trường Vercel `HUIT_API_ORIGIN=https://api.ten-mien-cua-ban` rồi redeploy. Cấu hình sẽ chuyển `/api/*` và ảnh đại diện công khai `/uploads/avatars/*` từ Vercel đến máy chủ API; Web vẫn gọi `/api` cùng origin.

Hướng dẫn máy chủ tại `deploy/vps/README.md`. Bản Web Vercel chưa thể dùng dữ liệu thật hoặc gửi mã email cho đến khi máy chủ API và CSDL được triển khai, tên miền HTTPS hoạt động, dữ liệu đã được chuyển an toàn và `HUIT_API_ORIGIN` được cấu hình.
