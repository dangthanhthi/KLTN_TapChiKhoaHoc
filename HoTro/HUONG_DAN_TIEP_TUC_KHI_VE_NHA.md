# HƯỚNG DẪN BÀN GIAO & TIẾP TỤC DỰ ÁN TẠI NHÀ (KLTN HUIT)

> **Đề tài KLTN:** Xây dựng Hệ thống Quản trị Tòa soạn và Xuất bản Tạp chí Khoa học Điện tử  
> **Đơn vị đào tạo:** Khoa Công nghệ Thông tin, Trường Đại học Công Thương TP. Hồ Chí Minh (HUIT)  
> **Nhóm thực hiện:** Đặng Thành Thi (Nhóm trưởng) · Trần Xuân Hướng · Lưu Đức Linh  
> **Giảng viên hướng dẫn:** ThS. Lâm Thị Họa Mi  
> **Repository GitHub:** [dangthanhthi/KLTN_TapChiKhoaHoc](https://github.com/dangthanhthi/KLTN_TapChiKhoaHoc) (Nhánh `main`)  
> **Tệp sao lưu mới nhất:** `KLTN_HUIT_Journal_Backup_Full_20260919.zip` (Cập nhật ngày 19/09/2026)  
> **Nguyên tắc đóng băng:** Phân hệ Desktop WinForms C# (.NET 10) trong thư mục `Winform/` đã hoàn chỉnh và được **ĐÓNG BĂNG TUYỆT ĐỐI**.

---

## I. TỔNG QUAN HỆ THỐNG ĐÃ HOÀN TẤT (TUẦN 1 ĐẾN TUẦN 8)

Dự án đã hoàn thành toàn diện việc chuyển đổi từ Mockup sang **Hệ thống Full-Stack thật 100%**:

1. **Cơ sở dữ liệu Microsoft SQL Server 2022 (`QL_TapChiKhoaHoc`):**
   - 12 bảng thực thể và 1 bảng nối chuyên môn chuẩn 3NF.
   - 3 Database Triggers kiểm soát nghiêm ngặt:
     - `TRG_BaiBao_NgayCapNhat`: Tự động cập nhật thời gian chỉnh sửa bài báo.
     - `TRG_BaiBao_KiemTraChuyenMonTacGia`: Ràng buộc tác giả chỉ được gửi bài thuộc lĩnh vực chuyên môn của mình.
     - `TRG_PhanCongPhanBien_KiemTraChuyenMon`: Chặn tuyệt đối xung đột lợi ích (chống tác giả chính hoặc đồng tác giả tự phản biện bài của mình).
   - 2 Stored Procedures:
     - `sp_DongBoDongTacGia_TheoEmail`: Tự động liên kết bài báo lịch sử khi đồng tác giả đăng ký tài khoản mới.
     - `sp_KiemTraDongTacGiaChuaLienKet`: Tra cứu bài báo cần xác nhận tác quyền (Author Claiming).
   - Đã băm mật khẩu 100% tài khoản mẫu sang chuẩn an toàn **BCrypt** (Mật khẩu dùng chung: `123456`).

2. **Backend ASP.NET Core Web API (.NET 9):**
   - Chạy tại: `http://localhost:5000/` (Kiêm Web Server phục vụ thư mục `Web/` qua Static Files).
   - OpenAPI / Swagger tài liệu hóa tại: `http://localhost:5000/openapi/v1.json`.
   - **Module Xác thực (`AuthController` & `AuthService`):** Đăng nhập JWT Bearer Token, Đăng ký liên kết tác quyền tự động, Cập nhật hồ sơ cá nhân, Đổi mật khẩu BCrypt, Danh mục chuyên ngành, Tra cứu người dùng theo email (`GET /api/auth/lookup`).
   - **Module Bài báo (`BaiBaoController` & `BaiBaoService`):** Nộp bài trực tuyến multipart form kèm tệp `.docx`/`.pdf`, lưu trữ tệp vào `Uploads/Submissions/YYYY/MM/`, lưu bảng `ThuMucBaiBao`, `DongTacGia`, ghi vết `LichSuTrangThaiBaiBao`, xử lý triệt để lỗi Trigger EF Core 9 bằng cấu hình `HasTrigger()`.
   - **Module Số Tạp chí (`SoTapChiController` & `SoTapChiService`):** Danh mục số tạp chí đã phát hành, chi tiết số tạp chí kèm mục lục bài báo, API lấy danh sách bài báo mới nhất kèm danh sách tác giả (`GET /api/baibao/public/latest`).
   - **Module Phản biện (`PhanBienController` & `PhanBienService`):** Phân công phản biện kín, Chuyên gia xem danh sách bài phân công, Chuyên gia nộp phiếu chấm điểm BM-04 theo 5 tiêu chí, Ban biên tập ra quyết định biên tập (`Đã chấp nhận`, `Chờ chỉnh sửa`, `Từ chối`).

3. **Giao diện Web Portal (`Web/`):**
   - **Trang chủ (`UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html`):** Gọi `apiGetLatestArticles()` hiển thị trực tiếp danh sách bài báo mới nhất từ SQL Server.
   - **Nộp bài 5 bước (`submit-paper.html`):** Kết nối `apiSubmitPaper()` gửi tệp và dữ liệu thật, auto-match email đồng tác giả thời gian thực từ CSDL qua `apiLookupUser()`.
   - **Bàn làm việc cá nhân (`profile.html`):** Hiển thị tiến trình 6 giai đoạn các bài đã nộp từ CSDL; hiển thị mục "Nhiệm vụ Phản biện" và mở Modal chấm điểm BM-04 trực tiếp trên Web.
   - **Kho lưu trữ (`archives.html`) & Chi tiết số (`issue-detail.html`):** Nạp dữ liệu số tạp chí và mục lục từ CSDL.
   - **Chi tiết bài báo (`article-detail.html`):** Đọc bài báo, xem thông tin tác giả, tóm tắt, từ khóa, tải tệp toàn văn PDF.
   - **Đăng nhập (`login.html`) & Đăng ký (`register.html`):** Kết nối API xác thực thật, lưu JWT token.

---

## II. HƯỚNG DẪN THIẾT LẬP KHI VỀ NHÀ TIẾP TỤC LÀM VIỆC

### Bước 1: Chuẩn bị môi trường máy tính ở nhà
Đảm bảo máy tính ở nhà đã cài đặt:
1. **.NET 9.0 SDK** (hoặc mới hơn): Kiểm tra bằng lệnh `dotnet --version`.
2. **Microsoft SQL Server** (Bản Developer, Express hoặc Standard) kèm **SSMS (SQL Server Management Studio)** hoặc Azure Data Studio.
3. **Trình duyệt web hiện đại** (Google Chrome, Microsoft Edge, hoặc Firefox).
4. *(Tùy chọn)* Python 3.x nếu muốn chạy Web tĩnh độc lập qua cổng 8088.

### Bước 2: Giải nén mã nguồn
1. Tải tệp `KLTN_HUIT_Journal_Backup_Full_20260919.zip` về máy tính.
2. Giải nén vào thư mục làm việc, ví dụ: `D:\KLTN` hoặc `C:\Users\<TenBan>\Desktop\KLTN`.

### Bước 3: Cài đặt Cơ sở dữ liệu SQL Server
1. Mở **SSMS** và kết nối vào SQL Server trên máy nhà (ví dụ: `.` hoặc `localhost` hoặc `.\SQLEXPRESS`).
2. Mở tệp `SQLKLCN.sql` (tại thư mục gốc của dự án).
3. Nhấn **F5** (hoặc bấm **Execute**) để khởi tạo CSDL `QL_TapChiKhoaHoc`. Toàn bộ 12 bảng, 3 triggers, 2 stored procedures và dữ liệu mẫu chuẩn sẽ được tạo mới hoàn chỉnh.

### Bước 4: Cấu hình chuỗi kết nối Database trong Backend
Mở tệp `Backend/HuitJournal.Api/appsettings.json`, kiểm tra chuỗi `ConnectionStrings:DefaultConnection`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\CSSQL22;Database=QL_TapChiKhoaHoc;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```
- Nếu SQL Server trên máy nhà của bạn có tên khác (ví dụ `localhost`, `.` hoặc `.\SQLEXPRESS`), hãy sửa lại tên `Server=...` cho khớp với máy nhà (ví dụ: `Server=localhost;Database=QL_TapChiKhoaHoc;...`).

### Bước 5: Khởi chạy Hệ thống
Có 2 cách khởi chạy cực kỳ tiện lợi:

#### Cách 1: Khởi chạy Full-Stack nhanh (Khuyên dùng)
- Nhấp đúp chuột vào tệp `Chay_Backend.bat` tại thư mục gốc.
- Hệ thống sẽ tự động:
  1. Biên dịch và khởi chạy Backend ASP.NET Core Web API tại `http://localhost:5000`.
  2. Tự động phục vụ luôn toàn bộ giao diện Web Portal.
  3. Tự động mở trình duyệt truy cập: `http://localhost:5000/UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html`.

#### Cách 2: Khởi chạy bằng lệnh dòng lệnh (Terminal)
Mở PowerShell hoặc Command Prompt tại thư mục dự án:
```powershell
dotnet run --project Backend/HuitJournal.Api
```
Truy cập trình duyệt tại: `http://localhost:5000/`.

---

## III. DANH SÁCH TÀI KHOẢN THỬ NGHIỆM ĐẦY ĐỦ CÁC VAI TRÒ

Toàn bộ tài khoản mẫu đã được cấu hình mật khẩu dùng chung là: **`123456`**

| Vai trò | Tên đăng nhập | Email | Họ và tên | Chuyên môn |
|---|---|---|---|---|
| **Ban biên tập** | `txhuong` | `editor@huit.edu.vn` | TS. Trần Xuân Hướng | CNTT & AI, Tự động hóa |
| **Quản trị hệ thống** | `admin` | `admin@huit.edu.vn` | Quản trị Tạp chí | CNTT & Mạng máy tính |
| **Tác giả** | `vuthif` | `vuthif@huit.edu.vn` | TS. Vũ Thị F | CNTT & AI |
| **Chuyên gia phản biện** | `dangvang` | `dangvang@vnuhcm.edu.vn` | PGS. TS. Đặng Văn G | Cơ khí & Tự động hóa |
| **Tác giả & Phản biện** | `tranthih` | `tranthih@ctub.edu.vn` | TS. Trần Thị H | Khoa học Môi trường |
| **Chuyên gia phản biện** | `nguyenvank` | `nguyenvank@hcmut.edu.vn` | TS. Nguyễn Văn K | Cơ khí & Chế tạo máy |
| **Tác giả & Phản biện** | `tranvann` | `tranvann@huit.edu.vn` | TS. Trần Văn N | CNTT & AI |

---

## IV. KỊCH BẢN DEMO KIỂM THỬ GHI ĐIỂM CAO TRƯỚC HỘI ĐỒNG

Khi demo hoặc phát triển tiếp, bạn có thể thực hiện theo quy trình 4 bước chuẩn sau:

### Kịch bản 1: Tác giả nộp bản thảo mới (Form 5 bước)
1. Đăng nhập với tài khoản Tác giả: `vuthif` / `123456` tại `http://localhost:5000/login.html`.
2. Bấm "Nộp bản thảo mới" (`submit-paper.html`).
3. Điền tiêu đề, tóm tắt, chọn tệp Word/PDF.
4. Tại bước 3 (Đồng tác giả), thử nhập email `tranvann@huit.edu.vn` $\rightarrow$ Hệ thống tự động kích hoạt `apiLookupUser()` tra cứu CSDL thật và điền tự động tên `TS. Trần Văn N`.
5. Bấm nộp bài $\rightarrow$ Hệ thống sinh mã bản thảo (ví dụ: `JST-2026-SUB0011`), lưu tệp vào `Uploads/Submissions/`, và đưa vào trạng thái "Chờ sơ duyệt".

### Kịch bản 2: Bàn làm việc & Theo dõi tiến trình 6 giai đoạn
1. Truy cập `http://localhost:5000/profile.html`.
2. Danh sách bản thảo của tác giả hiển thị đầy đủ kèm thanh trạng thái 6 giai đoạn sinh động (1. Nộp bài $\rightarrow$ 2. Sơ duyệt $\rightarrow$ 3. Phản biện $\rightarrow$ 4. Quyết định $\rightarrow$ 5. Chế bản $\rightarrow$ 6. Xuất bản).

### Kịch bản 3: Chuyên gia phản biện nhận xét & Chấm điểm BM-04
1. Đăng nhập tài khoản Chuyên gia phản biện: `dangvang` / `123456`.
2. Mở trang Bàn làm việc (`profile.html`) $\rightarrow$ Khu vực **"Nhiệm vụ Phản biện độc lập kín"** tự động hiển thị các bài báo được phân công.
3. Bấm nút **"Đánh giá bản thảo (BM-04)"** $\rightarrow$ Modal chấm điểm 5 tiêu chí xuất hiện.
4. Chấm điểm (Tính mới, Phương pháp, Kết quả, Trình bày), nhập góp ý cho tác giả, chọn kết luận `Chấp nhận đăng` và bấm gửi $\rightarrow$ Phiếu được lưu vào bảng `PhieuDanhGia` trong SQL Server.

### Kịch bản 4: Ban biên tập ra quyết định xuất bản
1. Ban biên tập phê duyệt bài báo $\rightarrow$ Trạng thái chuyển sang `Đã chấp nhận` hoặc `Đã xuất bản`.
2. Quay lại Trang chủ (`UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html`), bài báo vừa xuất bản tự động xuất hiện trên danh mục bài mới nhất!

---

## V. CÁC TỆP TIN CỐT LÕI CỦA DỰ ÁN

| Đường dẫn tệp | Mô tả chức năng |
|---|---|
| `TONG_QUAN_DU_AN_KLTN.txt` | Bản mô tả tổng quan toàn bộ dự án, kiến trúc, phân công và kịch bản bảo vệ trước hội đồng |
| `TOAN_BO_DU_AN_VA_SOURCE_CODE_KLTN.txt` | Toàn bộ mã nguồn dự án được gom vào 1 file văn bản duy nhất (64 files, ~945 KB) |
| `SQLKLCN.sql` | Kịch bản tạo mới và cấu hình CSDL SQL Server 2022 |
| `Backend/HuitJournal.Api/` | Toàn bộ mã nguồn Backend ASP.NET Core Web API (.NET 9) |
| `Web/` | Toàn bộ mã nguồn Frontend Web Portal (HTML5, CSS3, ES6 JS) |
| `Chay_Backend.bat` | Kịch bản khởi chạy hệ thống Full-Stack 1-click |
| `Chay_WinForms.bat` | Kịch bản khởi chạy ứng dụng Desktop Ban biên tập |
| `Day_Len_Github.bat` | Kịch bản đẩy mã nguồn lên GitHub tự động |

---

*Chúc bạn tiếp tục hoàn thiện xuất sắc dự án Khóa luận tốt nghiệp tại nhà!*
