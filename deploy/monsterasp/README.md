# Triển khai thử API .NET + SQL Server trên MonsterASP.NET Free

## Triển khai nhanh bằng Web Deploy

Máy đã có Microsoft Web Deploy. Trong MonsterASP, website `site94240` → Visual Studio access cần bật WebDeploy, rồi tải **Download publish profile**. File đó chứa mật khẩu; giữ ngoài repository. Cấu hình một lần:

```powershell
.\deploy\monsterasp\configure-webdeploy.ps1 -PublishSettings 'C:\duong-dan-rieng\site94240.publishSettings'
```

Script lưu mật khẩu được Windows DPAPI mã hóa tại `%LOCALAPPDATA%\HuitJournal\MonsterASP`, chỉ tài khoản Windows hiện tại đọc lại được. Không gửi profile cho người khác, không commit profile. Nếu đổi mật khẩu WebDeploy trên hosting, chạy lại bước cấu hình bằng profile mới.

Xem trước các file sẽ cập nhật (không ghi vào website):

```powershell
.\deploy\monsterasp\deploy.ps1
```

Triển khai API bằng một lệnh:

```powershell
.\deploy\monsterasp\deploy.ps1 -Apply
```

Script publish Release, kiểm tra gói không chứa bí mật hoặc Uploads, bỏ bước nén ZIP, đối chiếu kích thước và thời gian sửa để đồng bộ những file thay đổi. Lệnh `-Apply` tạm dừng API bằng AppOffline trong lúc cập nhật, rồi kiểm tra endpoint chuyên ngành. File trên máy chủ không bị xóa; thư mục Uploads/logs, tất cả appsettings JSON và biến môi trường được giữ. Script không triển khai database hoặc frontend Vercel. Khi cần sửa cấu hình công khai appsettings, thực hiện riêng có rà soát; lệnh này chủ ý bỏ qua chúng. Không bỏ qua kiểm tra chứng chỉ TLS.

Chỉ build và kiểm tra local, chưa cần tài khoản WebDeploy:

```powershell
.\deploy\monsterasp\deploy.ps1 -PrepareOnly
```

Nếu WebDeploy thất bại, không tự chạy lặp lại. Xem thông báo và log trên MonsterASP; nếu website đang trả 503, kiểm tra file `app_offline.htm`. Chỉ báo triển khai thành công sau khi đồng bộ thành công và API trả dữ liệu JSON. Thời gian build, đồng bộ và tổng thời gian được in sau mỗi lượt để đo thực tế.

Nguồn: [Web Deploy dòng lệnh của MonsterASP](https://help.monsterasp.net/websites/deploy/command-line).

## Cập nhật production ngày 30/09/2026

Đã tạo bản sao mới của `db70314`, áp dụng `Migration_20260930_EditorInChiefRole.sql` idempotent và xác nhận vai trò `Tổng biên tập` có mã 6. Đóng gói API Release `dist/monsterasp/api-20260930-193204.zip` (SHA256 `c8268fd3fb8bde75b2fb3422384de9b471a18ad467f6bc4708bf125dbfeee3df`), tải lên website `huit-journal-api.runasp.net` và giải nén ghi đè tại `/wwwroot` với tùy chọn khởi động lại application pool. Tác vụ ZIP của MonsterASP báo `Done`, 69 tệp / 16,5 MB.

Admin có quyền API quản trị và nghiệp vụ biên tập/phản biện/học thuật. Tổng biên tập có quyền phân công phản biện, duyệt quyết định học thuật và phát hành số; không truy cập API quản trị tài khoản hoặc môi trường. Biên tập viên xử lý nghiệp vụ biên tập nhưng không phân công phản biện hay phát hành số. Phản biện viên chỉ xem phân công/đánh giá của mình; Tác giả không được vào vùng biên tập/quản trị. Backup production được tạo trước khi cập nhật.

Smoke test production ngày 30/09: API MonsterASP và proxy Vercel cùng trả 200, cùng 5 chuyên ngành. Đăng nhập và ma trận GET theo 5 vai trò qua Vercel đã kiểm tra; các quyền ngoài vai trò trả 403. Đã tạo tài khoản kiểm thử Tổng biên tập `tongbientap.test@example.com` và xác nhận login/API. WinForms Release self-contained `dist/winforms/HUIT-Journal-Desktop-win-x64-20260930-194718.zip` (SHA256 `45E8C912971BB9C33ED899CB7177C7890F50F713E337B2A6C0DFCC6994E7BCA2`) trỏ API HTTPS production. Audit mode WinForms xác thực qua `AuthService` và tải/renders các view với Admin lẫn Tổng biên tập; chưa tự động nhập liệu bằng cách gõ/click vào form đăng nhập do desktop automation service không khả dụng trong phiên.

Web công khai vẫn nằm trên Vercel. Hosting này chạy API .NET 9 và SQL Server, nên không cần chuyển CSDL sang PostgreSQL. Đây là hosting dùng chung cho bản thử nghiệm, không phải VPS hay hạ tầng có cam kết vận hành.

## Trạng thái chuẩn bị ngày 27/09/2026

Đã đóng gói API Release thành công và kiểm tra gói không chứa cấu hình máy, file tải lên hoặc khóa bí mật. Gói mới nhất tại `dist/monsterasp/api-20260927-170735.zip` (SHA256 `bdd9e036059157779d6f57adc67de7cf4d330d8da281600852fe3b35597ea313`). Đã tạo website Free `huit-journal-api.runasp.net` (`site94240`), SQL Server 2025 Free `db70314` cùng vùng EU, tải và giải nén API vào `/wwwroot`, cấu hình biến môi trường Production rồi khởi động lại ứng dụng. Đã phục hồi bản `.bak` vào `db70314`; trang quản trị hiển thị 17 bảng. Ngày 27/09/2026, Vercel đã đặt `HUIT_API_ORIGIN` cho Production và redeploy thành công từ commit `78c4ec4`. Access log MonsterASP ghi nhận `/api/chuyennganh` trả HTTP 200; trang đăng ký Vercel tải được năm lĩnh vực từ API.

HTTPS của website Free đã được bật bằng chứng chỉ Let's Encrypt tự gia hạn; hạn hiện hiển thị là 22/12/2026. Đã gửi phiếu `L32A-F32V-M17C` ngày 27/09/2026. HTTPS redirect đã được bật. Đã tải và giải nén `Uploads` vào `/wwwroot/Uploads`: tác vụ báo `Done`, 254 tệp, 181,3 MB, có các thư mục `avatars`, `Published`, `revisions`, `Submissions`. Hai gói ZIP triển khai đã được chuyển từ `/wwwroot` sang `/private`. Smoke test live ngày 27/09/2026: trang đăng ký mở, danh sách chuyên ngành có dữ liệu, gửi form trống bị chặn và hiện summary + lỗi từng trường, không có lỗi JavaScript trong console. Cần sửa câu báo lỗi chuyên ngành khi chưa chọn mục (danh sách đã tải nhưng thông báo hiện nói chưa xác nhận được chuyên ngành). API chưa gửi thử OTP Gmail thật; vì vậy luồng gửi email và xác nhận tài khoản còn cần kiểm tra bằng hộp thư của chủ tài khoản.

Đã tạo bản sao CSDL hiện hành `HoTro/QL_TapChiKhoaHoc_before_monsterasp_20260927_165126.bak` bằng `COPY_ONLY, COMPRESSION, CHECKSUM` và `RESTORE VERIFYONLY` thành công; CSDL hiện tại 16 MB, dưới hạn mức 1 GB của gói Free. Bốn đối tượng cần cho xác nhận email (`DangKyChoXacNhan`, `MaXacNhanEmail`, `EmailOutbox`, `sp_DongBoDongTacGia_TheoEmail`) đều tồn tại; hiện có 10 người dùng và 10 bài báo. Công cụ SQL trong sandbox từng báo lỗi TLS, nhưng truy cập trực tiếp ngoài sandbox hoạt động; không cần hạ cấu hình mã hóa SQL Server. Bản `.bak` là dữ liệu riêng, được Git bỏ qua và không được đưa lên GitHub. Các bản `.bak` cũ tạo trước bước xác nhận email không thay thế bản này.

## Giới hạn cần biết trước

Gói Free hiện có 256 MB RAM cho ứng dụng, 1 GB SQL Server và 5 GB tệp website. IIS worker ngủ sau 30 phút không có request; khi đó tác vụ nền quét EmailOutbox cũng dừng. Chứng chỉ Let's Encrypt của website hiện được Control Panel báo tự gia hạn. Gói Free không có sao lưu tự động. Hạn mức và chính sách có thể đổi, nên đối chiếu lại với bảng giá và tài liệu của nhà cung cấp trước khi vận hành.

Nguồn: [bảng gói Free](https://www.monsterasp.net/Pricing/), [thời gian ngủ của IIS](https://forum.monsterasp.net/d/104-aspnet-core-background-services-support), [gia hạn HTTPS](https://help.monsterasp.net/books/https/page/how-to-activate-https-with-lets-encrypt-certificate).

## 1. Chuẩn bị tài khoản và bản sao dữ liệu

1. Tạo tài khoản tại [MonsterASP.NET](https://www.monsterasp.net/), chọn **Free Hosting**, tạo một website và một CSDL MS SQL Server. Tài khoản hiện tại được cấp SQL Server 2025. Không đưa tài khoản hoặc mật khẩu vào GitHub hay tài liệu dự án.
2. Ghi lại URL website HTTPS, tên SQL Server, tên CSDL, tài khoản SQL và mật khẩu SQL trong nơi lưu bí mật riêng. Với website Free hiện tại, HTTPS đã được bật theo phiếu `L32A-F32V-M17C`; xác minh chứng chỉ còn hiệu lực trong Control Panel.
3. Sao lưu CSDL `QL_TapChiKhoaHoc` đang chạy và giữ bản gốc trên máy. Dùng mục **Databases → Backups → Upload Backup / Restore** để khôi phục bản sao lên hosting. Nếu bản sao chưa có ba bảng xác nhận email, chạy `HoTro/Migration_20260927_EmailVerificationSchema.sql` trên CSDL thử. Kiểm tra số lượng hàng ở các bảng chính và bảo đảm dữ liệu kiểm thử không chứa tài khoản/mật khẩu mặc định công khai.
   **Quan trọng:** bản CSDL hiện hành có 10 tài khoản mẫu từng dùng chung mật khẩu `123456`. Bản API Production đã chặn mật khẩu này và mật khẩu cũ dạng văn bản, nên các tài khoản đó không thể đăng nhập qua Web. Trước khi mở quyền quản trị công khai, tạo tài khoản thật qua luồng xác nhận email rồi cấp vai trò cần thiết trên bản CSDL hosting; không khôi phục hay đặt lại mật khẩu mẫu yếu trên hosting.
4. `Backend/HuitJournal.Api/Uploads` là dữ liệu riêng, hiện có thể lớn hơn 150 MB. Sao chép thư mục này vào cùng thư mục gốc ứng dụng trên hosting qua kênh quản trị riêng. Giữ quyền truy cập tệp bản thảo chỉ qua API có JWT; không đặt `Uploads` trong web root tĩnh. Sao lưu thư mục này riêng vì bản sao SQL không chứa nội dung tệp.

Nhà cung cấp có [hướng dẫn khôi phục `.bak`](https://help.monsterasp.net/books/databases/page/restore-database) và [truy cập SQL từ SSMS](https://help.monsterasp.net/books/databases/page/sql-server-management-studio-ssms). Chỉ bật quyền truy cập SQL từ xa trong thời gian cần quản trị, rồi tắt lại.

## 2. Đóng gói và tải API

Tại gốc dự án, chạy PowerShell:

```powershell
.\deploy\monsterasp\prepare.ps1
```

Script tạo `dist/monsterasp/api-<thời gian>.zip` và kiểm tra gói không chứa `appsettings.Local.json`, cấu hình Testing hoặc `Uploads`. Trong Control Panel, mở website → **Files (File Manager)**, tải ZIP lên rồi chọn **Unzip** vào `/wwwroot`; nếu cập nhật bản đang chạy, bật ghi đè và khởi động lại application pool. [Hướng dẫn triển khai ZIP của MonsterASP.NET](https://help.monsterasp.net/books/deploy/page/how-to-deploy-website-content-from-zip-file). **Không** tải nguyên thư mục dự án hay file `.bak` lên web root.

## 3. Cấu hình bí mật trong Control Panel

Vào **Websites → Manage website → Scripting → Environment Variables**. Tạo các biến dưới đây bằng giá trị thật của môi trường thử; cú pháp `__` ánh xạ vào cấu hình ASP.NET Core. Không tạo file có chứa giá trị thật trong Git.

| Biến | Giá trị |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Chuỗi SQL nội bộ của hosting, gồm server, database, user, password |
| `Jwt__Key` | Khóa riêng ngẫu nhiên tối thiểu 32 byte, khác khóa OTP |
| `EmailVerification__HmacSecretKey` | Khóa OTP riêng ngẫu nhiên tối thiểu 32 byte |
| `EmailVerification__SmtpHost` | `smtp.gmail.com` nếu dùng Gmail App Password |
| `EmailVerification__SmtpPort` | `587` |
| `EmailVerification__EnableSsl` | `true` |
| `EmailVerification__SmtpUsername` | Địa chỉ hộp thư gửi mã |
| `EmailVerification__SmtpPassword` | App Password của hộp thư gửi mã, không phải mật khẩu đăng nhập Gmail |
| `EmailVerification__SenderEmail` | Cùng địa chỉ hộp thư gửi mã |
| `EmailVerification__PublicWebBaseUrl` | `https://kltn-tap-chi-khoa-hoc.vercel.app` |
| `EmailVerification__SimulateDeliveryInDev` | `false` |
| `Cors__AllowedOrigins__0` | `https://kltn-tap-chi-khoa-hoc.vercel.app` |

Nếu nhà cung cấp cấp host SQL nội bộ, dùng host nội bộ cho API. Sau khi cấu hình, khởi động lại website và xem log. API Production từ chối khóa mặc định, SMTP giả lập và chuỗi kết nối SQL cục bộ. [Hướng dẫn Environment Variables](https://help.monsterasp.net/books/development/page/environment-variables-as-configuration-store); [Gmail SMTP](https://help.monsterasp.net/books/e-mails/page/sending-e-mails-from-your-applications-using-gmail).

## 4. Kiểm thử API

1. Mở `https://<subdomain>/api/chuyennganh`; cần nhận JSON thật từ CSDL, không phải lỗi 500 hoặc HTML.
2. Gửi thử đăng ký bằng địa chỉ email của chính bạn, nhận mã thật, xác nhận, đăng nhập và kiểm tra hồ sơ. Mã sai/hết hạn phải bị từ chối.
3. Nộp một bài thử kèm PDF, tải lại qua API, kiểm tra tệp vẫn còn sau khi ứng dụng khởi động lại. Thử WinForms bằng `HUIT_JOURNAL_API_URL=https://<subdomain>` trên máy quản trị.
4. Kiểm tra RAM, log lỗi, dung lượng SQL và tệp. Nếu 256 MB không đủ hoặc bản đăng ký/email không tin cậy, dừng ở môi trường thử, chưa đổi website công khai.

WinForms dùng API cho nghiệp vụ thường ngày. Công cụ **Backup/Restore SQL cục bộ** của WinForms không phù hợp với SQL được quản lý trên hosting; dùng Control Panel để sao lưu/phục hồi CSDL hosting. Không đặt `HUIT_JOURNAL_DB_CONNECTION` cho người dùng WinForms thông thường.

## 5. Trạng thái kết nối Web Vercel

Trong **Vercel Project Settings → Environment Variables**, biến Production `HUIT_API_ORIGIN` hiện trỏ tới `https://huit-journal-api.runasp.net`; Vercel đã redeploy và trạng thái là Ready. `vercel.mjs` chuyển `/api/:path*` và `/uploads/avatars/:path*` tới API HTTPS. Access log MonsterASP xác nhận `/api/chuyennganh` trả HTTP 200 và giao diện đăng ký hiển thị dữ liệu chuyên ngành. Smoke test form rỗng xác nhận frontend validation hoạt động, nhưng phát hiện thông báo chuyên ngành khi chưa chọn mục chưa chính xác. Còn phải kiểm chứng gửi OTP Gmail thật, nhập mã và đăng nhập; chưa kiểm tra đầy đủ tải tệp, quy trình WinForms và độ ổn định khi IIS thức dậy sau thời gian ngủ.

Chỉ dùng bản Free để thử nghiệm/đồ án. Vercel Hobby giới hạn mục đích cá nhân, phi thương mại; trước khi bán sản phẩm phải chuyển sang gói/hạ tầng phù hợp. [Vercel Hobby](https://vercel.com/docs/plans/hobby).

