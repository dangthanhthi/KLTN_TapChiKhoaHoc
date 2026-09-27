# TỔNG HỢP TOÀN BỘ QUY TẮC, KỸ NĂNG, LIÊN KẾT & HƯỚNG DẪN DỰ ÁN
# KHÓA LUẬN TỐT NGHIỆP: HỆ THỐNG QUẢN LÝ TẠP CHÍ KHOA HỌC (JOURNAL MANAGER)

> **Tài liệu bàn giao dự án:** Đọc trước khi tiếp tục làm việc trên máy trạm mới hoặc môi trường khác.  
> **Ngày cập nhật:** 19/09/2026  
> **Nhóm thực hiện đề tài & Phân công nhiệm vụ:**  
> - Đặng Thành Thi (Nhóm trưởng - **Phụ trách lập trình cả 2 phân hệ: Web Portal và Backend API C# .NET 9**)  
> - Trần Xuân Hướng (Phụ trách lập trình phân hệ Desktop WinForms)  
> - Lưu Đức Linh (Phụ trách phân hệ Desktop WinForms và tài liệu)  
> **Giảng viên hướng dẫn:** ThS. Lâm Thị Họa Mi  
> **Đơn vị:** Khoa Công nghệ Thông tin, Trường Đại học Công Thương TP. Hồ Chí Minh (HUIT)

---

## 1. TỔNG QUAN HỆ THỐNG & CÁC LIÊN KẾT QUAN TRỌNG (LINKS)

### 1.1. Thông tin định danh Tạp chí chính thức (HUIT Journal Identity)
- **Tên cơ quan:** Tạp chí Khoa học Đại học Công Thương (Huit Journal of Science)
- **Cơ quan chủ quản:** Trường Đại học Công Thương TP. Hồ Chí Minh
- **Giấy phép hoạt động báo chí:** Số **53/GP-BTTTT** ngày 06/3/2024 do **Bộ Thông tin và Truyền thông** cấp
- **Mã số định danh quốc tế chuẩn hóa:**
  - Bản in (Print): **p-ISSN 3030-4113**
  - Bản trực tuyến (Online Open Access): **e-ISSN 3030-413X**
- **Ban lãnh đạo:**
  - **Tổng biên tập:** TS. Bùi Hồng Đăng (Chủ tịch Hội đồng trường HUIT)
  - **Chủ tịch Hội đồng biên tập:** PGS. TS. Nguyễn Xuân Hoàn (Hiệu trưởng HUIT)
- **Trụ sở & Văn phòng Tòa soạn:** Phòng C101, Tòa nhà C, Trường ĐH Công Thương TP.HCM, 140 Lê Trọng Tấn, P. Tây Thạnh, TP. Hồ Chí Minh
- **Điện thoại:** 028.38163318 - ext.112  ·  **Email:** journal@huit.edu.vn
- **Trang thông tin trực tuyến:** https://huitjournal.vn | https://journal.huit.edu.vn

### 1.2. Liên kết mã nguồn và môi trường vận hành (Repositories & Deployments)
- **GitHub Repository chính thức:** https://github.com/dangthanhthi/KLTN_TapChiKhoaHoc (Nhánh `main`)
- **Nền tảng xuất bản Web thử nghiệm:** Vercel Hosting (cấu hình tại `Web/vercel.json`)
- **Môi trường chạy cục bộ (Local Web Server):** http://localhost:8088/UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html (khởi chạy tự động bằng `Chay_Web.bat`, tránh xung đột cổng 8080 với Laragon)
- **Môi trường ứng dụng Ban biên tập (Desktop Client):** WinForms C# (`HuitJournal.exe`), khởi chạy bằng `Chay_WinForms.bat`.
- **Cơ sở dữ liệu Microsoft SQL Server 2022:** Máy chủ `.\CSSQL22`, cơ sở dữ liệu `QL_TapChiKhoaHoc` (11 bảng chuẩn 3NF).

---

## 2. HỆ THỐNG 4 KỸ NĂNG CỐT LÕI (AGENT SKILLS)

Dự án được cấu hình 4 nhóm kỹ năng chuyên biệt đặt tại thư mục .agents/skills/:

### 2.1. Kỹ năng UI/UX & Frontend: hallmark (.agents/skills/hallmark/SKILL.md)
- **Mục đích:** Thiết kế và xây dựng giao diện người dùng Desktop (WinForms C# .NET 10) và Web (HTML/CSS/JS) chuẩn mực, phong cách Tạp chí học thuật (Editorial/Academic).
- **Quy chuẩn Anti-AI-slop (Chống thiết kế rập khuôn):**
  1. *Đa dạng cấu trúc layout (Structural Variety):* Không dùng mẫu 3-card rập khuôn, áp dụng bố cục Editorial split, Spec-sheet, Bento grid hoặc Masthead báo chí.
  2. *Khóa bảng màu & phông chữ (Locked Design Tokens):* 
     - Màu nhận diện thương hiệu chủ đạo: Xanh da trời #1da1f2 (Primary Azure Blue).
     - Bảng màu bổ trợ: --ink-900: #16202B, --ink-700: #3D4A57, --ink-500: #6B7684, --line: #E1E9F1, --sky-050: #F3F8FD.
     - Phông chữ Serif học thuật: Source Serif 4 (Georgia).
     - Phông chữ Sans-serif hiện đại: Inter (hệ thống sans-serif).
  3. *Không in nghiêng tiêu đề (Typographic Purity):* Mọi tiêu đề (h1, h2, h3, title) luôn viết đứng (
ormal), chỉ in nghiêng thuật ngữ khoa học hoặc trích dẫn.
  4. *Nội dung trung thực (Honest Copy):* Dữ liệu bám sát tôn chỉ tạp chí khoa học HUIT, không dùng chữ vô nghĩa (Lorem Ipsum).
  5. *Không vẽ chrome/khung trình duyệt giả:* Tránh mockup viền giả làm rối mắt.
  6. *Chuẩn High-DPI & Responsive:* Hỗ trợ PerMonitorV2 trên WinForms và hiển thị hoàn hảo từ 320px đến màn hình 4K.

### 2.2. Kỹ năng Ghi nhớ dài hạn & Quản lý tri thức: mem0 (.agents/skills/mem0/SKILL.md)
- **Mục đích:** Duy trì tính nhất quán kiến trúc, thói quen lập trình, các quyết định ADRs qua nhiều phiên làm việc.
- **Tệp lưu trữ tri thức:** .agents/memory/project_memory.json.
- **Nguyên tắc then chốt:**
  - Nhớ rõ 5 vai trò người dùng chuẩn mực (Quản trị, Ban biên tập, Tác giả, Phản biện viên, Độc giả).
  - Nhớ quy trình xuất bản 6 giai đoạn cốt lõi của tạp chí.
  - Không tự ý thay đổi cấu trúc bảng CSDL khi chưa đối chiếu các ràng buộc học thuật.

### 2.3. Kỹ năng Tự động hóa & Kiểm thử trình duyệt: rowser-use (.agents/skills/browser-use/SKILL.md)
- **Mục đích:** Tự động hóa duyệt web, kiểm thử E2E giao diện, kiểm tra console logs, xác thực quy trình nộp bài trực tuyến, đo lường độ phản hồi DOM trên các kích thước chuẩn (320px, 375px, 768px, 1024px, 1366px).

### 2.4. Kỹ năng Tích hợp công cụ MCP: modelcontextprotocol (.agents/skills/modelcontextprotocol/SKILL.md)
- **Mục đích:** Kết nối các máy chủ Model Context Protocol để tương tác tệp tin hệ thống an toàn, truy vấn cơ sở dữ liệu SQLite/SQL Server và cập nhật đồ thị tri thức.

---

## 3. TOÀN BỘ QUY TẮC DỰ ÁN (PROJECT RULES)

1. **Quy tắc thiết kế UI mặc định (.agents/rules/ui-design.md):**
   - Áp dụng triệt để bộ nhận diện Hallmark.
   - Khoảng cách Header/Navbar và Body phải có khoảng thở (margin-top: 28px), dải bóng ox-shadow: 0 4px 16px rgba(29, 161, 242, 0.15).
   - Độ rộng khung nội dung chuẩn mực: max-width: 1360px (.wrap).
   - Khóa màu chữ menu Dropdown #1f2937 để không bị trùng màu trắng trên nền trắng.
2. **Quy tắc bảo toàn tài liệu học thuật (Document Integrity):**
   - Khi cập nhật báo cáo khóa luận KLTN_011.docx, phải bảo toàn 100% định dạng: Font Times New Roman, Size 13pt (bảng biểu 10-11pt), giãn dòng 1.15 lines, giãn đoạn efore: 2pt, after: 2pt, tiêu đề in đậm căn giữa.
   - Thống nhất tên thực thể và thuộc tính giữa CSDL SQL (SQLKLCN.sql) và Từ điển dữ liệu trong Chương 3 Khóa luận.

---

## 4. QUY TRÌNH NGHIỆP VỤ XUẤT BẢN 6 GIAI ĐOẠN (BUSINESS WORKFLOW)

Hệ thống được thiết kế theo quy trình khép kín chuẩn COPE & OJS:
1. **Giai đoạn 1 - Nộp bản thảo trực tuyến (Submission):** Tác giả nộp qua Stepper Wizard 5 bước (Thông tin chung, Tác giả & Đồng tác giả, Chuyên ngành, Đính kèm tệp ẩn danh, Rà soát & Gửi).
2. **Giai đoạn 2 - Tiếp nhận & Sơ duyệt (Initial Screening):** Thư ký / Ban biên tập kiểm tra thể thức IMRaD, quy cách trích dẫn IEEE, tỷ lệ trùng lặp (Turnitin/Kiểm tra đạo văn < 20%).
3. **Giai đoạn 3 - Phản biện chuyên môn kín hai chiều (Double-Blind Peer Review):** Mời tối thiểu 02 chuyên gia độc lập theo đúng chuyên ngành. Chuyên gia có 3 ngày xác nhận và 15-20 ngày hoàn thành phiếu nhận xét theo 5 tiêu chí thang điểm 10. Nếu 2 ý kiến trái ngược, Ban biên tập mời chuyên gia thứ 3 (Trọng tài khoa học).
4. **Giai đoạn 4 - Quyết định biên tập & Chỉnh sửa (Editorial Decision):** Ban biên tập ra quyết định (Chấp nhận, Sửa nhỏ, Sửa lớn phản biện lại, hoặc Từ chối). Tác giả nộp bản giải trình và tệp chỉnh sửa có đánh dấu (Track Changes).
5. **Giai đoạn 5 - Biên tập kỹ thuật & Đọc bông (Copyediting & Proofreading):** Hiệu đính ngữ pháp, chuyển đổi định dạng 2 cột, kiểm tra tài liệu tham khảo, gửi bản đọc bông (Galley Proof) cho tác giả xác nhận lần cuối.
6. **Giai đoạn 6 - Xuất bản & Phát hành (Publication):** Gán bài vào Tập, Số tạp chí, cấp chỉ số DOI vĩnh viễn (Crossref), xuất bản ấn bản điện tử PDF trên Cổng thông tin và đồng bộ chỉ mục Google Scholar.

---

## 5. CẤU TRÚC DỮ LIỆU & CSDL SQL SERVER (12 BẢNG CHUẨN 3NF)

CSDL lưu trữ tại SQLKLCN.sql và tài liệu chi tiết Thiet_Ke_CSDL_JournalManager.docx:
1. VaiTro: Danh mục 5 vai trò hệ thống (Quản trị, Ban biên tập, Tác giả, Phản biện viên, Độc giả).
2. NguoiDung: Hồ sơ tài khoản, email duy nhất, mật khẩu mã hóa BCrypt ($2a$11$), học vị, mã ORCID quốc tế.
3. NguoiDung_VaiTro: Bảng phân quyền đa năng (N:N), cho phép một người dùng đảm nhận nhiều vai trò.
4. ChuyenNganh: 5 Lĩnh vực nghiên cứu cốt lõi của HUIT (CNTT & AI, Cơ khí - Tự động hóa, Môi trường & Nông nghiệp, Kinh tế & QTKD, Hóa học & CN Thực phẩm).
5. NguoiDung_ChuyenMon: Bảng nối chuyên môn người dùng (N:N), xác định chuyên môn chính phục vụ nộp bài và phân công phản biện.
6. SoTapChi: Quản lý các số phát hành định kỳ (Tập, Số, Năm, Ngày phát hành, Trạng thái).
7. BaiBao: Thực thể trung tâm lưu siêu dữ liệu bài báo (Tiêu đề, Tóm tắt tiếng Việt/Anh, Từ khóa, Trạng thái, DOI, Ngày gửi, Khóa ngoại tới tác giả, chuyên ngành, số tạp chí, số trang).
8. DongTacGia: Danh sách đồng tác giả (snapshot danh tính độc lập, email duy nhất trong bài, hỗ trợ tác giả khách và liên kết tài khoản).
9. ThuMucBaiBao: Lưu trữ lịch sử phiên bản tệp tin (Bản thảo gốc, File ẩn danh, Bản chỉnh sửa, Phụ lục).
10. PhanCongPhanBien: Theo dõi tiến trình phân công phản biện kín, số vòng, hạn phản hồi, hạn hoàn thành.
11. PhieuDanhGia: Phiếu chấm điểm 5 tiêu chí (Tính mới, Phương pháp, Kết quả, Trình bày, Điểm tổng kết 0-10) kèm nhận xét công khai và bảo mật.
12. LichSuTrangThaiBaiBao: Bảng lưu vết kiểm toán (Audit Trail) lịch sử chuyển đổi trạng thái của từng bài báo khoa học.

---

## 6. DANH MỤC TỆP TIN TRỌNG TÂM CẦN NẮM VỮNG

| Thư mục / Tệp | Chức năng & Ý nghĩa |
| :--- | :--- |
| KLTN_011.docx | **Thuyết minh Khóa luận Tốt nghiệp hoàn chỉnh** (Chương 1: Khảo sát, Chương 2: Phân tích hướng đối tượng BCE/Use Case, Chương 3: Thiết kế CSDL & Kiến trúc). |
| Thiet_Ke_CSDL_JournalManager.docx | Tài liệu đặc tả kỹ thuật CSDL chi tiết tách riêng, chuẩn format đồ án A4. |
| SQLKLCN.sql | Kịch bản lệnh T-SQL khởi tạo đầy đủ 12 bảng, 3 triggers, 2 stored procedures và dữ liệu mẫu (chạy bằng `sqlcmd -f 65001`). |
| Backend/HuitJournal.Api | **Backend RESTful Web API (.NET 9)**: Entity Framework Core 9.0.2, BCrypt.Net, JWT Bearer Token, kết nối trực tiếp SQL Server `QL_TapChiKhoaHoc`. |
| Web/ | Thư mục mã nguồn Website Cổng thông tin Tạp chí khoa học (12 trang HTML, CSS tương tác, JavaScript kết nối trực tiếp Backend API). |
| Winform/ | Ứng dụng Quản lý Tòa soạn Windows Forms viết bằng C# .NET 10. |
| docs/so_do/ | Toàn bộ ảnh PNG/SVG sơ đồ quy trình chuẩn (Quy trình tổng thể, Quy trình phản biện, Sơ đồ mời chuyên gia thứ 3). |
| Chay_Backend.bat | Script 1-click khởi chạy Backend Web API và Web Portal tại cổng 5000. |
| Chay_Web.bat | Script 1-click khởi chạy máy chủ Python static tại cổng 8088 (dự phòng). |
| Chay_WinForms.bat | Script 1-click chạy trực tiếp ứng dụng WinForms của Ban biên tập. |
| AGENTS.md & GEMINI.md | Bộ chỉ dẫn định hướng và tiêu chuẩn vận hành bắt buộc cho AI Agent. |

---

## 7. HƯỚNG DẪN TIẾP TỤC LÀM VIỆC TẠI MÔI TRƯỜNG MỚI

1. **Giải nén gói lưu trữ:**
   - Sử dụng WinRAR hoặc 7-Zip giải nén tệp Khoa_Luan_Tot_Nghiep_Backup_Full.rar vào thư mục làm việc trên máy mới.
2. **Triển khai Cơ sở dữ liệu:**
   - Mở Microsoft SQL Server Management Studio (SSMS).
   - Mở tệp SQLKLCN.sql và nhấn Execute (F5), hoặc chạy lệnh: `sqlcmd -S .\CSSQL22 -E -f 65001 -i SQLKLCN.sql` để tạo lập cơ sở dữ liệu `QL_TapChiKhoaHoc`.
3. **Chạy ứng dụng Backend Web API & Web Portal:**
   - Nhấp đúp vào `Chay_Backend.bat` hoặc chạy: `dotnet run --project Backend/HuitJournal.Api`.
   - Hệ thống sẽ tự động khởi chạy Web API và mở Cổng thông tin tại: `http://localhost:5000/`.
4. **Chạy ứng dụng WinForms:**
   - Cần máy cài đặt .NET 9/.NET 10 SDK hoặc Desktop Runtime.
   - Nhấp đúp vào `Chay_WinForms.bat` hoặc mở solution trong Visual Studio 2022 để Debug.
5. **Cập nhật tài liệu Word:**
   - Chỉnh sửa trực tiếp trên KLTN_011.docx bằng Microsoft Word hoặc WPS Office, giữ nguyên Style định dạng học thuật.
