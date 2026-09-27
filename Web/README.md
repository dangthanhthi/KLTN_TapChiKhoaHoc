# Hệ thống Tạp chí Khoa học & Xuất bản Học thuật (Scientific Journal System)

Website chính thức và Cổng thông tin điện tử của Tạp chí Khoa học, phục vụ quy trình xuất bản học thuật trực tuyến: giới thiệu, công bố bài báo, kho lưu trữ, nộp bản thảo và bàn làm việc tác giả.

## 🚀 Các trang tính năng chính
- **Trang chủ Tạp chí (`UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html`)**: Ấn phẩm mới phát hành, tiêu điểm nghiên cứu, bài báo mới nhất, tra cứu chuyên ngành.
- **Giới thiệu Tạp chí (`about.html`)**: Tôn chỉ mục đích, mã chuẩn quốc tế ISSN / e-ISSN, định danh DOI CrossRef, chỉ mục dữ liệu Google Scholar, VCI, ACI.
- **Chính sách xuất bản & Đạo đức (`publishing-policy.html`)**: Bản quyền truy cập mở Open Access CC-BY, quy định liêm chính, chống đạo văn, bình duyệt kín 2 chiều.
- **Hội đồng biên tập (`editorial-board.html`)**: Danh sách Tổng biên tập, Phó Tổng biên tập, Thư ký tòa soạn và Hội đồng chuyên ngành.
- **Quy trình & Biểu mẫu (`guidelines.html`)**: 6 giai đoạn nộp - biên tập - phản biện - xuất bản; tải biểu mẫu bản thảo mẫu Word (`.docx`).
- **Kho lưu trữ chuyên ngành (`archives.html`)**: Tra cứu toàn bộ các số đã xuất bản qua các năm theo từng chuyên ngành.
- **Chi tiết bài báo (`article-detail.html`)**: Tải PDF, trích dẫn học thuật tự động (BibTeX, RIS, APA), thông tin tác giả, chỉ số trích dẫn.
- **Bàn làm việc Tác giả (`profile.html`)**: Theo dõi tiến trình 6 giai đoạn xử lý bản thảo thời gian thực, gửi bản thảo chỉnh sửa, cập nhật ORCID.
- **Nộp bài trực tuyến (`submit-paper.html`)**: Biểu mẫu nộp bài 5 bước chuẩn hóa.
- **Đăng nhập (`login.html`) & Đăng ký (`register.html`)**: Quản lý phiên làm việc tác giả và bạn đọc.

## 🛠️ Công nghệ sử dụng
- **Frontend**: HTML5, CSS3 hiện đại, Vanilla JavaScript (Không phụ thuộc thư viện nặng).
- **Thiết kế**: Chuẩn mực Typography học thuật (`Source Serif 4` & `Inter`), responsive hoàn hảo, hỗ trợ High-DPI.
- **Triển khai (Deployment)**: Vercel Static Hosting.

## 👤 Phân công thực hiện
- **Đặng Thành Thi:** Phụ trách toàn diện lập trình phân hệ Web (Frontend HTML/CSS/JS, trải nghiệm người dùng, cơ chế Hybrid Dual-Engine và kiểm thử tự động Web E2E).
- Các phân hệ khác (WinForms Desktop) do thành viên khác trong nhóm phụ trách độc lập.


## Bàn làm việc phản biện riêng (24/09/2026)

Trang `reviewer.html` dành cho tài khoản có vai trò phản biện. Truy cập qua menu tài khoản hoặc trang cá nhân. Có danh sách công việc, tìm kiếm/bộ lọc, hạn xử lý, lịch sử đánh giá, phiếu BM-04, nháp trong phiên, xuất bản ghi TXT và lịch ICS.

Phiên thật xác minh vai trò bằng API, không tự chuyển sang dữ liệu mẫu khi lỗi hoặc danh sách rỗng. Phiên demo có nhãn riêng và phải chủ động nạp dữ liệu mẫu; dữ liệu tách theo tài khoản. Không coi phân quyền trên trình duyệt là bảo mật thay thế máy chủ.

Phạm vi của Đặng Thành Thi: lập trình phân hệ Web; không sửa WinForms, SQL hoặc Backend trong đợt này. Công nghệ Web được chọn theo khả năng bảo trì và hợp đồng API, không bắt buộc dùng cùng framework với Desktop. Chi tiết ở `REVIEWER_WEB_HANDOFF.md`.
