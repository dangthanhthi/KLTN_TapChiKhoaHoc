# Triển khai Backend, SQL Server và xác nhận email trên VPS

Thư mục này chuẩn bị cấu hình Docker Compose cho API .NET 9, SQL Server 2022 và Caddy HTTPS. Cần VPS Linux có Docker Compose, DNS `API_DOMAIN` trỏ về VPS, cổng 80/443 và dung lượng đủ cho SQL Server. Chỉ Vercel phục vụ giao diện Web; API và SQL ở VPS. SQL port 1433 không công khai.

## Chuẩn bị

1. Rà soát giấy phép SQL Server phù hợp mục đích sử dụng và chọn `MSSQL_PID`. Không dùng Developer Edition cho hoạt động thương mại.
2. Tạo `deploy/vps/.env` từ `example.env.template` trên VPS, điền `API_DOMAIN`, mật khẩu SQL, hai khóa ngẫu nhiên độc lập (JWT và HMAC, tối thiểu 32 byte mỗi khóa), SMTP thật và xác nhận EULA sau khi đọc điều khoản. `.env` không được đưa lên Git.
3. Dùng một tên miền gửi thư mà bạn có quyền sử dụng. Xác minh SPF/DKIM/DMARC tại nhà cung cấp gửi thư. Không dùng mật khẩu Gmail cá nhân trong mã nguồn.
4. Chuyển backup CSDL và thư mục `Uploads` từ máy hiện tại qua kênh riêng được mã hóa; không tải chúng lên GitHub. Khôi phục CSDL trên SQL Server ở VPS. Nếu tạo CSDL mới, chạy schema nền và migration email (`HoTro/Migration_20260927_EmailVerificationSchema.sql`) trước khi mở đăng ký. Kiểm tra bản sao lưu và quyền truy cập dữ liệu theo yêu cầu của dự án.

## Khởi động

Từ thư mục `deploy/vps` chạy `docker compose up -d --build` sau khi `.env`, DNS, SQL và dữ liệu đã sẵn sàng. Kiểm tra `https://<API_DOMAIN>/api/chuyennganh` nhận JSON; kiểm tra đăng ký với hộp thư thử của bạn để chắc chắn thư tới thật và mã chỉ dùng một lần. Không kiểm thử bằng dữ liệu người dùng thật trước khi hoàn tất kiểm soát truy cập và sao lưu.

API chỉ nhận origin Vercel đã khai báo trong `Cors__AllowedOrigins__0`; Caddy kết thúc HTTPS, còn API chỉ lắng nghe mạng Docker nội bộ. Khi thay đổi tên miền Web, cập nhật origin cho đúng. `ASPNETCORE_FORWARDEDHEADERS_ENABLED` chỉ an toàn khi cổng API không được public trực tiếp và chỉ Caddy kết nối vào mạng nội bộ.

## Kết nối Vercel

Trong Project Settings → Environment Variables của dự án Vercel đặt `HUIT_API_ORIGIN=https://<API_DOMAIN>` (không thêm `/api`). Redeploy. `vercel.mjs` sẽ chuyển `/api/:path*` và ảnh đại diện `/uploads/avatars/:path*` sang API HTTPS. Kiểm tra trực tiếp tại `https://kltn-tap-chi-khoa-hoc.vercel.app/api/chuyennganh`, sau đó kiểm tra đăng ký, nhận email, xác nhận mã, đăng nhập, ảnh đại diện và WinForms cùng CSDL. Nếu endpoint không có JSON thật, không công bố rằng bản Vercel đã đồng bộ.

## Vận hành

Sao lưu SQL và `Uploads` theo lịch; kiểm tra phục hồi định kỳ. Theo dõi thư gửi lỗi, số hồ sơ chờ hết hạn và dung lượng hai volume. Khóa SMTP cũ đã từng nằm trong tệp cấu hình cục bộ cần được thay trước khi dùng môi trường công khai. Không đưa JWT/HMAC/SMTP hoặc file `.env` vào commit, issue hoặc tài liệu công khai.
