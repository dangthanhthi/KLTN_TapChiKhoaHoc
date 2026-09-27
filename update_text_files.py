import os
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')

ROOT_DIR = os.path.dirname(os.path.abspath(__file__))
TONG_QUAN_PATH = os.path.join(ROOT_DIR, "TONG_QUAN_DU_AN_KLTN.txt")
TOAN_BO_PATH = os.path.join(ROOT_DIR, "TOAN_BO_DU_AN_VA_SOURCE_CODE_KLTN.txt")

# 1. CẬP NHẬT TONG_QUAN_DU_AN_KLTN.txt
SECTION_UPDATE = """

================================================================================
10. CÁC TÍNH NĂNG MỚI HOÀN THIỆN (TUẦN 10) & KẾT QUẢ KIỂM THỬ CHẤT LƯỢNG
================================================================================

10.1. Hoàn thiện triệt để chức năng Đăng xuất (Logout & Clear Session)
- Cơ chế vận hành:
  + Hàm handleUserLogout() trong Web/journal-interactions.js thực hiện xóa sạch localStorage.removeItem('journal_token'), localStorage.removeItem('journal_user') và sessionStorage.clear().
  + Lập tức gọi renderAuthNavbar() để chuyển trạng thái Navbar về Guest (hiển thị 2 nút Đăng nhập / Đăng ký).
  + Tự động đóng dropdown menu tài khoản và điều hướng an toàn về Trang chủ nếu đang ở các trang yêu cầu quyền tác giả (profile.html, submit-paper.html).
- Bổ sung giao diện trực quan:
  + Trên profile.html: Thêm nút "Đăng xuất tài khoản" màu đỏ trực quan tại thẻ Lý lịch tác giả (Author Spec Card).
  + Trên login.html: Bổ sung khối nhận diện phiên làm việc. Nếu người dùng đã đăng nhập, trang hiển thị tên và email tài khoản kèm 2 nút "Vào Bàn làm việc ->" và "Đăng xuất ngay".

10.2. Thêm giao diện Tìm kiếm bài báo công khai trên archives.html
- Bộ công cụ tìm kiếm đa tiêu chí:
  + Ô nhập từ khóa: Tìm kiếm theo tiêu đề bài báo, tên tác giả, tóm tắt hoặc từ khóa khoa học.
  + Dropdown Lĩnh vực chuyên môn: Lọc theo 5 ngành đào tạo mũi nhọn của HUIT (CNTT & AI, Cơ khí - Tự động hóa, Môi trường - Nông nghiệp, Kinh tế - QTKD, Hóa học - Thực phẩm).
  + Dropdown Số phát hành / Ấn phẩm: Lọc theo từng tập/số cụ thể (Tập 26 Số 3E, Số 2E, Số 1E, Tập 26 Số 3, Tập 25...).
  + Nút "Tìm kiếm" và nút "Đặt lại".
- Hệ thống 2 Tabs chuyển đổi chế độ xem:
  + Tab 1: Các số phát hành (Issues View) - Hiển thị 10 số phát hành kèm ảnh bìa, mô tả và mục lục theo chuẩn OJS, phân trang 5 số/trang.
  + Tab 2: Tìm kiếm bài báo (Articles Search View) - Tự động kích hoạt khi lọc. Hiển thị thẻ bài báo chuẩn học thuật gồm: Tiêu đề, Tác giả, Chuyên ngành, Số báo, Ngày đăng, DOI, Tóm tắt (Abstract), Nút xem chi tiết và Nút tải toàn văn PDF.
- Tuân thủ triệt để tiêu chuẩn Anti-AI-slop (Hallmark): Không dùng bullet point, tiêu đề viết đứng (normal), khóa bảng màu #1da1f2.

10.3. Giao diện Nộp bản chỉnh sửa & Biểu mẫu BM-03 trên profile.html
- Khối nhận xét phản biện trực quan:
  + Khi bản thảo ở trạng thái "Chờ chỉnh sửa" hoặc "Chờ sửa hình thức", hệ thống hiển thị khối thông báo viền màu hổ phách chứa ý kiến góp ý cụ thể từ Chuyên gia phản biện 1 & 2 kèm thời hạn nộp bản sửa (14 ngày).
- Hành động & Modal nộp bản sửa (#modal-revision):
  + Nút nổi bật: "Nộp bản chỉnh sửa & Giải trình (BM-03) ->".
  + Modal tiếp nhận đầy đủ 4 hạng mục bắt buộc theo quy trình xuất bản HUIT:
    1. Ô nhập nội dung giải trình tiếp thu tóm tắt (Response to Reviewers).
    2. Tệp đính kèm: Biểu mẫu giải trình tiếp thu ý kiến phản biện (BM-03) (.docx / .pdf) kèm đường dẫn tải mẫu chuẩn.
    3. Tệp đính kèm: Bản thảo đã chỉnh sửa hoàn thiện (Clean Manuscript .docx / .pdf).
    4. Tệp đính kèm: Bản thảo có đánh dấu sửa đổi Track Changes (Marked Manuscript .docx / .pdf).
  + Tương tác gửi bản sửa mượt mà, hiển thị toast thông báo thành công và cập nhật tức thì trên bàn làm việc của tác giả.

10.4. Global Responsive Engine & Kết quả kiểm thử tự động toàn diện
- Cập nhật Web/journal-interactions.css:
  + Thiết lập cơ chế chống tràn ngang toàn cục (max-width: 100% !important; overflow-x: hidden !important;).
  + Navbar và Header tự động chuyển sang chế độ vuốt cuộn ngang mượt mà trên mobile (< 768px).
  + Khối Banner ISSN căn chỉnh lại vị trí linh hoạt trên mobile.
  + Các bảng Stepper nộp bài và layout 2 cột tự động chuyển thành 1 cột trên màn hình nhỏ.
- Kịch bản kiểm thử tự động Playwright (Web/tests/test_responsive_web.py):
  + Kiểm tra toàn bộ 12 trang web tại 5 kích thước chuẩn (320px, 375px, 768px, 1024px, 1366px).
  + Kết quả: 60/60 bài kiểm tra ĐẠT CHUẨN 100% (PASS).
  + 0 lỗi JavaScript Console trên toàn bộ 12 trang web.
  + 0 hiện tượng tràn ngang (ScrollWidth == ClientWidth trên mọi màn hình từ 320px đến 1366px).
  + Vượt qua cả 4 bài kiểm thử chức năng chuyên sâu: Đăng xuất xóa token, Tìm kiếm bài báo trên archives.html, Nộp bản chỉnh sửa BM-03 trên profile.html, và Nộp bài báo chính thức trên submit-paper.html.

10.5. Tinh chỉnh an toàn & Chuẩn hóa dữ liệu trước Hội đồng bảo vệ
- Xử lý nút tải "Mục lục toàn bộ số (PDF)" trên issue-detail.html:
  + Loại bỏ việc gán nhầm sang file ảnh bìa (cover .jpg/.svg).
  + Cơ chế an toàn: Tự động ẩn nút tải mục lục nếu số báo chưa có tệp PDF mục lục thực tế; chỉ hiển thị khi có thuộc tính pdfMucLuc hợp lệ.
- Dọn dẹp form đăng nhập login.html:
  + Xóa sạch dữ liệu mẫu hardcoded (value="vuthif@huit.edu.vn", value="123456").
  + Thay thế bằng placeholder hướng dẫn chuẩn mực.
- Dọn dẹp form nộp bài submit-paper.html:
  + Xóa toàn bộ dữ liệu bài mẫu hardcoded (tiêu đề, tóm tắt, từ khóa "Transformer...", file đính kèm).
  + Đưa toàn bộ 5 bước về trạng thái trống nguyên bản.
  + Bảo lưu nút tiện ích "✦ Tải bản thảo mẫu thử nghiệm nhanh (1-Click Demo)" để phục vụ demo nhanh khi cần.

10.6. Sửa triệt để Bug chặn chức năng nộp bài (handleFinalSubmit Crash)
- Nguyên nhân: Trong submit-paper.html, hàm handleFinalSubmit() gọi document.getElementById('keywords').value, nhưng form chỉ có keywords-vi và keywords-en, dẫn đến lỗi TypeError: Cannot read properties of null và dừng toàn bộ tiến trình nộp bài.
- Khắc phục:
  + Đọc đúng document.getElementById('keywords-vi')?.value || document.getElementById('keywords-en')?.value.
  + Sửa hàm chuyển bước lỗi từ goToStep(2) sang switchStep(2) và gắn alias window.goToStep = switchStep.
- Kiểm thử tự động Playwright (Web/tests/test_submit_paper_submission.py và test_responsive_web.py): 100% PASS, 0 lỗi runtime Console, bài báo gửi dữ liệu thành công.

10.7. Sửa triệt để lỗi xung đột CHECK constraint 'CHK_NguoiDung_HocVi' và mã hóa Tiếng Việt khi Đăng ký tài khoản
- Hiện tượng: Khi người dùng điền form Đăng ký trên register.html với học vị 'Thạc sĩ' (hoặc giá trị tiếng Việt), hệ thống báo lỗi popup: 'Không thể kết nối đến máy chủ Backend (http://localhost:5000)...'.
- Nguyên nhân:
  1. CSDL SQL Server trước đó được import bằng công cụ đọc ANSI làm toàn bộ ràng buộc CHECK (CHK_NguoiDung_HocVi, CHK_NguoiDung_HocHam, CHK_NguoiDung_GioiTinh, CHK_ThuMuc_LoaiThuMuc...) và dữ liệu bị lỗi font mojibake (ví dụ: 'Tháº¡c sÄ©' thay vì 'Thạc sĩ').
  2. Khi frontend gửi UTF-8 'Thạc sĩ', SQL Server báo lỗi 547 vi phạm CHECK constraint 'CHK_NguoiDung_HocVi'.
  3. Backend ném DbUpdateException trả về lỗi làm frontend nhảy vào khối catch chung.
- Khắc phục triệt để:
  1. Tái nạp CSDL SQLKLCN.sql với chuẩn mã hóa UTF-8 hoàn chỉnh. Toàn bộ 12 bảng và 12 CHECK constraint đạt chuẩn tiếng Việt Unicode có dấu 100%.
  2. Lưu file SQLKLCN.sql với UTF-8 BOM đảm bảo SSMS/sqlcmd luôn nhận diện chính xác UTF-8.
  3. Mở rộng RegisterRequest và AuthService.RegisterAsync: lưu đầy đủ Địa chỉ, Số tài khoản, Ngân hàng, và tự động cấp thêm vai trò Chuyên gia phản biện khi người dùng đăng ký.
  4. Cải tiến apiRegister và register.html xử lý phản hồi máy chủ an toàn, báo lỗi chi tiết thay vì câu thông báo mất kết nối giả. Đã kiểm thử API đăng ký thành công 100%.

10.8. Tính năng Ghi nhớ & Tự động khôi phục dữ liệu bản nháp khi tải lại trang (Draft Autosave & Restore)
- Nhu cầu thực tế: Người dùng đang điền form Đăng ký nhiều trường thông tin (Họ tên, chuyên ngành, tài khoản ngân hàng...), nếu vô tình bấm F5, tải lại trang hoặc rớt mạng thì dữ liệu đã điền bị mất hết.
- Giải pháp hoàn thiện trên Web/register.html:
  1. Tự động lắng nghe sự kiện nhập liệu ('input' và 'change') trên toàn bộ form đăng ký.
  2. Lưu trữ bản nháp tức thời (Real-time Autosave) vào cả sessionStorage và localStorage theo khóa 'huit_journal_register_draft' để đảm bảo an toàn tuyệt đối ngay cả khi đóng/mở lại trình duyệt.
  3. Tự động phục hồi nguyên vẹn tất cả 20 trường dữ liệu (Họ đệm, Tên, Học vị, Học hàm, Giới tính, Ngôn ngữ, Quốc gia, Điện thoại, Đơn vị, Địa chỉ, Số tài khoản, Chủ tài khoản, Ngân hàng, Chuyên ngành, Checkbox chuyên gia phản biện, Email, Tên đăng nhập, Mật khẩu, ORCID, Điều khoản) ngay khi trang được tải lại.
  4. Hiển thị thông báo trạng thái trang nhã: '✦ Đã tự động khôi phục dữ liệu...' kèm nút tiện ích 'Xóa bản nháp' khi muốn làm mới lại từ đầu.
  5. Tự động xóa sạch bản nháp khi đăng ký tài khoản thành công để bảo mật thông tin.
  6. Đã kiểm thử tự động Playwright (Web/tests/test_register_draft.py) đạt chuẩn 100% PASS.

10.9. Dọn dẹp Code chết & Thay thế toàn bộ alert() sang hệ thống Toast Notification (showToast)
- Xóa bỏ hoàn toàn code chết trong Web/submit-paper.html:
  + Loại bỏ hàm applyCoverLetterTemplate() không còn phần tử #cover-letter-text trong form 5 bước.
  + Loại bỏ hàm checkAllCommitments() tham chiếu đến các checkbox cũ (#c1, #c2, #c3) đã được gộp thành #final-agree-check.
- Chuẩn hóa trải nghiệm người dùng (UX) theo tiêu chuẩn Hallmark Anti-AI-slop:
  + Nâng cấp toàn diện Web/journal-interactions.css và journal-interactions.js hỗ trợ 4 phân loại toast: Success (xanh ngọc #10b981), Error (đỏ viền đỏ #ef4444), Warning (vàng hổ phách #f59e0b), Info (xanh dương OJS #1da1f2).
  + Hỗ trợ icon SVG sắc nét, nút đóng nhanh (×), tự động ngắt dòng đa đoạn (\\n thành <br>), và responsive mobile tự động căn giữa đáy màn hình.
  + Rà soát và thay thế toàn bộ 100% (32/32 vị trí) native browser alert() trên các trang login.html, register.html, profile.html, submit-paper.html sang showToast / showSuccessToast / showErrorToast / showWarningToast / showInfoToast.
  + Cập nhật các kịch bản kiểm thử Playwright tự động thích ứng với toast notification, đạt tỉ lệ pass 100%.

10.10. Tích hợp & Chuẩn hóa phân hệ Desktop WinForms mới (QL_TapChi_WinForms)
- Loại bỏ hoàn toàn mã nguồn WinForms cũ (được sao lưu an toàn tại Winform_Old_Backup/).
- Triển khai phân hệ WinForms mới hoàn chỉnh (QL_TapChi_WinForms) trên nền tảng .NET 8.0 Windows (net8.0-windows):
  + 14 Forms nghiệp vụ: FrmMain, FrmLogin, FrmChiTietBaiBao, FrmBaiBaoDialog, FrmPhanCongPhanBien, FrmPhieuDanhGiaDialog, FrmSoTapChiDialog, FrmChuyenNganhDialog, FrmNguoiDungDialog, FrmSuaTrangVaDoiDialog, FrmChonBaiVaoSoDialog, FrmDoiMatKhauDialog, FrmKiemTraDaoVanDialog.
  + 9 UserControls quản trị & Dashboard: UcAdminDashboard, UcDashboard, UcQuanLyBaiBao, UcQuanLyChuyenNganh, UcQuanLyNguoiDung, UcQuanLyPhanBien, UcQuanLySoTapChi, UcSaoLuuPhucHoi, UcThongKeBaoCao.
  + 10 Services dữ liệu: AuthService, BackupRestoreService, BaiBaoService, ChuyenNganhService, DatabaseHelper, LanguageService (hỗ trợ đa ngôn ngữ), NguoiDungService, PhanBienService, SoTapChiService, ThongKeService.
  + Hệ thống UI Theme hiện đại (UITheme.cs) và các custom control (MetricCardControl, ModernButton, WorkflowStepperControl).
  + Kết nối CSDL SQL Server trực tiếp qua AppConfig.cs (tương thích 100% với CSDL QL_TapChiKhoaHoc).
10.11. Hoàn thiện Chu trình Lưu trữ Chuyên gia Phản biện đề xuất (PhanBienDeXuat) & Ghi nhớ bản nháp nộp bài
- Khắc phục điểm đứt gãy dữ liệu (Data Gap) ở chức năng Đề xuất cán bộ thẩm định:
  + Trước đây: Modal #add-reviewer-modal chỉ chèn tạm <tr> vào DOM bảng hiển thị, không lưu vào biến mảng, handleFinalSubmit() không gửi lên server, CSDL không có bảng lưu trữ dẫn đến thất thoát dữ liệu đề xuất của tác giả.
  + Bổ sung Bảng CSDL PhanBienDeXuat: Gồm các trường MaDeXuat, HoTen, Email, DonVi, LinhVuc, LaChuyenGiaHeThong, MaBaiBao, MaNguoiDung, NgayTao với các ràng buộc khóa ngoại chặt chẽ (FK với BaiBao, NguoiDung).
  + Mở rộng Backend ASP.NET Core API (.NET 9):
    * Cập nhật Entities.cs (entity PhanBienDeXuat, quan hệ navigation trong BaiBao và NguoiDung).
    * Cập nhật QLTapChiKhoaHocContext.cs (DbSet PhanBienDeXuats).
    * Cập nhật BaiBaoDtos.cs (PhanBienDeXuatSubmitDto, PhanBienDeXuatDto, PhanBienDeXuatJson trong BaiBaoSubmitDto, PhanBienDeXuats trong BaiBaoDetailDto).
    * Cập nhật BaiBaoService.cs (giải mã chuỗi JSON PhanBienDeXuatJson, tự động tra cứu đối chiếu tài khoản qua email để liên kết MaNguoiDung, lưu trữ vào bảng PhanBienDeXuat, và nạp Include(x => x.PhanBienDeXuats) trong GetSubmissionDetailAsync).
  + Hoàn thiện Frontend Web/submit-paper.html:
    * Quản lý dữ liệu thông qua biến mảng proposedReviewers.
    * Kiểm tra chống xung đột lợi ích (không tự phản biện bài của mình) và chống trùng lặp email.
    * Hiển thị bảng đề xuất động (renderReviewersTable) và hỗ trợ xóa hàng (removeProposedReviewer).
    * Nạp tự động 2 chuyên gia mẫu khi dùng nút 1-Click Demo (loadDemoSampleFile).
    * Đồng bộ bảng tổng duyệt ở Bước 5 trực tiếp từ proposedReviewers.
    * Đóng gói formData.append('PhanBienDeXuatJson', JSON.stringify(proposedReviewers)) khi nộp bài.
    * Tích hợp tính năng Ghi nhớ bản nháp (Draft Persistence qua localStorage) tự động lưu và khôi phục khi reload/F5 trang.
  + Hoàn thiện Frontend Web/profile.html:
    * Bổ sung Modal xem chi tiết toàn diện hồ sơ bản thảo nộp (#modal-submission-detail).
    * Hiển thị trực quan thông tin bài báo, nhóm tác giả, danh sách chuyên gia phản biện do tác giả đề xuất (đối soát khớp CSDL / mời ngoài), thư mục tài liệu đính kèm và lịch sử phê duyệt.
  + Tuân thủ nghiêm ngặt chỉ đạo: Tuyệt đối không chỉnh sửa bất kỳ tệp tin nào thuộc phân hệ Desktop WinForms.
  + Kiểm thử tự động Playwright (Web/tests/test_e2e_proposed_reviewers.py): Đạt 100% PASS, kiểm chứng trọn vẹn luồng từ Web -> Backend API -> SQL Server -> Web Profile Modal.

10.12. Nâng cấp Toàn diện Bộ Ràng buộc Chức năng Đăng ký Tài khoản (Register Validation & Academic Constraints)
- Ràng buộc Học thuật Tòa soạn (Academic Integrity):
  + Quy chuẩn hóa điều kiện cấp quyền Chuyên gia phản biện (Vai trò 4): Hệ thống kiểm tra trình độ học thuật thực tế của người dùng. Chỉ người dùng có học vị tối thiểu từ Thạc sĩ, Tiến sĩ, Tiến sĩ Khoa học (TSKH) hoặc học hàm Phó giáo sư, Giáo sư mới được cấp vai trò Phản biện viên (Role 4).
  + Người dùng có học vị Cử nhân hoặc Không khi tích chọn phản biện vẫn được cấp tài khoản thành công với 2 vai trò Tác giả (3) & Độc giả (5), kèm thông báo quy chế học thuật của Tòa soạn (Ban biên tập sẽ thẩm định bổ sung sau).
  + Giao diện Web/register.html tự động hiển thị khối cảnh báo học thuật tương tác khi người dùng chọn học vị Cử nhân/Không mà vẫn tích chọn phản biện, tuân thủ nguyên tắc minh bạch thông tin.
- Ràng buộc Toàn vẹn Dữ liệu & CSDL (Data Integrity & Storage):
  + Bổ sung QuocGia và NgonNgu vào RegisterRequest DTO và ánh xạ lưu trực tiếp vào thực thể NguoiDung trong CSDL SQL Server.
  + Chuẩn hóa và kiểm soát mã định danh tác giả ORCID (MaORCID): Tự động bóc tách tiền tố URL 'https://orcid.org/', kiểm tra định dạng regex 16 ký tự phân tách bởi 3 dấu gạch ngang (^\\d{4}-\\d{4}-\\d{4}-\\d{3}[\\dX]$), và kiểm tra tính duy nhất trên CSDL nhằm ngăn chặn vi phạm ràng buộc UQ_NguoiDung_ORCID.
  + Ràng buộc Bộ ba thông tin tài khoản ngân hàng (SoTaiKhoan, ChuTaiKhoan, NganHang): Nếu người dùng nhập bất kỳ trường nào thì bắt buộc phải điền đầy đủ cả 3 trường để phục vụ chi trả thù lao phản biện / nhuận bút; tự động viết hoa tên Chủ tài khoản theo quy chuẩn thanh toán ngân hàng.
- Ràng buộc Bảo mật & Định danh (Security & Authentication):
  + Chuẩn hóa định dạng Tên đăng nhập (TenDangNhap): Cho phép từ 3 đến 30 ký tự (chữ cái tiếng Anh không dấu, số, ., _, -). Ngăn chặn khoảng trắng và ký tự tiếng Việt.
  + Minh bạch hóa kiểm tra trùng lặp: Xóa bỏ cơ chế âm thầm đổi tên đăng nhập thành username_123; nếu tên đăng nhập đã được sử dụng, hệ thống phản hồi lỗi 400 rõ ràng để người dùng chủ động chọn tên mới.
  + Kiểm tra định dạng số điện thoại chuẩn quốc tế (^\\+?[0-9]{9,15}$).
  + Bổ sung thanh đo độ an toàn mật khẩu (Password Strength Meter) trực quan theo phong cách thiết kế Hallmark Anti-AI-slop.
- Kết quả kiểm thử tự động toàn diện:
  + Kịch bản Python Web/tests/test_register_constraints.py kiểm thử trọn vẹn 6/6 kịch bản (Username sai định dạng, Trùng username, Trùng ORCID, Bóc tách URL ORCID, Bộ ba ngân hàng thiếu, Cử nhân tích phản biện) đạt tỉ lệ PASS 100%.
  + Kịch bản Playwright Web/tests/test_ui_register_screenshot.py kiểm chứng giao diện form thực tế trên Microsoft Edge, xuất ảnh minh chứng shot_register_constraints.png đạt chuẩn 100%.
  + Tuân thủ nghiêm ngặt nguyên tắc: Tuyệt đối không chỉnh sửa phân hệ Desktop WinForms.

10.13. Hoàn thiện Chức năng Tải lên (Upload), Xem trước & Thay đổi Ảnh đại diện (Avatar Management)
- Đáp ứng tiến độ Đề cương Khóa luận tốt nghiệp (Tuần 5-6):
  + Bổ sung chức năng Upload ảnh đại diện, thay đổi thông tin cá nhân và quản lý hồ sơ tác giả/chuyên gia trên Web Portal.
- Cơ sở Dữ liệu SQL Server:
  + Cập nhật bảng NguoiDung trong SQLKLCN.sql và CSDL QL_TapChiKhoaHoc: Bổ sung trường AnhDaiDien NVARCHAR(500) NULL lưu trữ đường dẫn URL tệp tin ảnh đại diện.
- Phân hệ Backend ASP.NET Core API (.NET 9):
  + Cập nhật Models/Entities.cs: Bổ sung trường AnhDaiDien trong entity NguoiDung.
  + Cập nhật DTOs/AuthDtos.cs: Thêm AnhDaiDien vào UserProfileDto và UpdateProfileRequest.
  + Mở rộng Services/AuthService.cs và IAuthService.cs:
    * Triển khai UploadAvatarAsync(): Kiểm tra định dạng hợp lệ (.jpg, .jpeg, .png, .webp, .gif), kiểm soát dung lượng tối đa 5MB, lưu trữ tệp tin vào Uploads/avatars/ với tên định danh duy nhất (avatar_{maNguoiDung}_{ticks}{ext}), và cập nhật đường dẫn vào thực thể NguoiDung.
    * Triển khai DeleteAvatarAsync(): Hỗ trợ xóa ảnh đại diện quay về avatar mặc định theo tên viết tắt (Initials).
    * Ánh xạ tự động AnhDaiDien trong MapToProfileDto() và hỗ trợ cập nhật qua UpdateProfileAsync().
  + Cập nhật Controllers/AuthController.cs: Bổ sung 2 endpoint bảo mật [HttpPost("upload-avatar")] và [HttpDelete("avatar")] có xác thực token [Authorize].
- Phân hệ Frontend Web Portal (profile.html, login.html, journal-interactions.js, journal-layout.js):
  + Tích hợp API helper apiUploadAvatar(file), apiDeleteAvatar(), và getAvatarUrl(path) xử lý đường dẫn tương đối/tuyệt đối linh hoạt.
  + Thiết kế Thẻ Hồ sơ Tác giả & Chuyên gia (side-spec-card) trên profile.html: Khung ảnh đại diện tròn lớn 96px có viền trắng đổ bóng tinh tế, nút camera overlay chọn ảnh nhanh, nút "Tải ảnh lên" và nút "Xóa ảnh" trực quan.
  + Cập nhật Bàn làm việc Tác giả (Workbench Header): Hiển thị avatar tròn 64px cá nhân hóa đồng bộ cạnh tên tác giả.
  + Cập nhật Modal Cập nhật hồ sơ (#modal-edit-profile): Tích hợp hàng xem trước avatar và nút đổi ảnh đại diện.
  + Đồng bộ toàn diện Thanh điều hướng (Navbar): Tự động hiển thị ảnh đại diện tròn tinh tế trên thanh menu Header chung và dropdown menu tài khoản (thay thế avatar chữ cái ban đầu).
  + Cập nhật trang login.html: Hiển thị avatar của tài khoản trong khối thông báo phiên làm việc đã đăng nhập.
- Tuân thủ nghiêm ngặt nguyên tắc cốt lõi:
  + Tuyệt đối không chỉnh sửa phân hệ Desktop WinForms (thư mục Winform/ giữ nguyên trạng thái đóng băng 100%).
  + Tiêu chuẩn thiết kế Hallmark Anti-AI-slop: Không dùng bullet points, typography chữ đứng chuẩn xác, locked token màu sắc #1da1f2.
- Kiểm thử tự động toàn diện:
  + Kịch bản Python Web/tests/test_avatar_upload.py kiểm thử trọn vẹn luồng API upload avatar, ghi tệp tin lên đĩa máy chủ và xác nhận dữ liệu trả về đạt 100% PASS.
  + Kịch bản Playwright E2E Web/tests/test_ui_avatar_e2e.py kiểm chứng thực tế toàn bộ hành vi người dùng trên trình duyệt Microsoft Edge, xuất ảnh minh chứng shot_avatar_profile.png và shot_avatar_modal.png đạt chuẩn 100%.

10.14. Đồng bộ Chuẩn hóa Nghiệp vụ Tòa soạn & 5 Cải tiến Cốt lõi (Web + Backend API)
- Đồng bộ máy trạng thái sửa bài khớp CSDL:
  + Thống nhất giá trị trạng thái là 'Chờ chỉnh sửa' trên toàn bộ SQL Server (CHK_BaiBao_TrangThai), Backend API (BaiBaoService.cs, PhanBienService.cs) và Web (profile.html).
  + Ràng buộc máy trạng thái server-side: Chỉ chấp nhận tác giả nộp lại bản thảo chỉnh sửa và BM-03 khi bài báo đang ở trạng thái 'Chờ chỉnh sửa'.
  + PhanBienService.cs (MakeEditorialDecisionAsync) chuẩn hóa quyết định chuyển trạng thái sang 'Chờ chỉnh sửa', loại bỏ hoàn toàn lỗi vi phạm ràng buộc CHECK constraint trên SQL Server thật.
- Bảo đảm phản biện kín hai chiều thực thụ (Double-Blind Peer Review):
  + Tại PhanBienService.cs (AssignReviewerAsync), bắt buộc kiểm tra bài báo đã có tệp loại 'File ẩn danh' hoặc 'Bản thảo ẩn danh' trước khi cho phép phân công chuyên gia; chặn đứng việc gửi bản thảo có tên tác giả cho chuyên gia thẩm định.
  + Phương thức GetManuscriptForReviewerAsync lọc nghiêm ngặt chỉ trả về tệp loại 'File ẩn danh' hoặc 'Bản thảo ẩn danh' gắn với đúng vòng phản biện (SoVong), tuyệt đối không để lộ bản gốc hay bản giải trình chứa thông tin tác giả.
- Thẩm định và duyệt cấp vai trò Phản biện viên (Reviewer Role Approval):
  + Chấm dứt việc tự động cấp quyền phản biện khi đăng ký. Người dùng mới chỉ được cấp vai trò Tác giả (3) và Độc giả (5).
  + Khi người dùng gửi đơn đăng ký làm phản biện, hệ thống chuyển về trạng thái chờ duyệt (Pending).
  + Bổ sung endpoint bảo mật POST /api/auth/approve-reviewer/{id} dành riêng cho Ban biên tập và Quản trị viên để thẩm định và phê duyệt cấp vai trò.
- Hoàn thiện xuất bản và bảo vệ tệp PDF chính thức:
  + Bổ sung loại tệp 'PDF thành phẩm' và 'PDF Xuất bản' vào ràng buộc CHK_ThuMuc_LoaiThuMuc trong SQL Server.
  + Phương thức GetPublicArticleAsync và GetPublicArticlePdfAsync tại BaiBaoService.cs chỉ công bố bài báo khi ở trạng thái 'Đã xuất bản' và số báo đã phát hành; ẩn toàn bộ URL tệp bản thảo nội bộ; chỉ cho phép tải tệp 'PDF thành phẩm'.
- Tách biệt tường minh chế độ Demo và Online trên Web:
  + Chế độ Standalone Demo chỉ kích hoạt khi có tham số '?mode=demo' hoặc cấu hình 'journal_app_mode === demo', kèm dải banner cảnh báo màu hổ phách ở đỉnh trang: '● CHẾ ĐỘ MÔ PHỎNG DEMO'.
  + Chế độ Online khi gặp lỗi API hoặc rớt mạng sẽ dừng lại ở lỗi thật và thông báo trung thực, tuyệt đối không tự ý rơi về dữ liệu mẫu hay localStorage.
- Cầu nối tương thích xác thực WinForms:
  + Backend API hỗ trợ kiểm tra BCrypt trước, nếu không khớp đối chiếu chuỗi văn bản thuần nhưng tuyệt đối không ghi đè vào SQL Server, giúp ứng dụng Desktop WinForms không bị khóa tài khoản khi kiểm thử chéo.

10.16. Xử lý triệt để 5 Điểm nghẽn nghiệp vụ và Kiểm thử Tích hợp 6 Giai đoạn (Live Backend API .NET 9 & SQL Server)
- Hoàn thiện 5 điểm nghẽn nghiệp vụ:
  1. Đồng bộ điều kiện công bố số báo: GetPublicArticleAsync và GetPublicArticlePdfAsync tại BaiBaoService.cs đồng bộ kiểm tra SoTapChi.TrangThai == "Đã xuất bản" || SoTapChi.TrangThai == "Đã phát hành" (khớp 100% với ràng buộc SQL CHK_SoTapChi_TrangThai).
  2. Tạo luồng tải lên tệp ẩn danh và PDF thành phẩm: Bổ sung 2 endpoints mới POST /api/baibao/{id}/upload-anonymous-manuscript (theo từng vòng soVong) và POST /api/baibao/{id}/upload-published-pdf cho Ban biên tập.
  3. Đồng bộ schema CSDL bằng Migration: Hoàn tất Migration_20260925_UpdateSchema.sql trên CSDL SQL Server QL_TapChiKhoaHoc, mở rộng ràng buộc CHK_ThuMuc_LoaiThuMuc và tạo bảng DonDangKyPhanBien.
  4. Lưu đơn xin làm phản biện thật: Lưu trữ hồ sơ ứng tuyển vào bảng DonDangKyPhanBien, bổ sung hàng đợi GET /api/auth/pending-reviewers và endpoint phê duyệt POST /api/auth/approve-reviewer/{id}, từ chối POST /api/auth/reject-reviewer/{id}.
  5. Chốt vòng phản biện động: Tính toán targetRound và revisionRound linh hoạt, ràng buộc tệp ẩn danh đúng vòng, lưu trữ tệp sửa đổi Clean_R{vong}_... và BM03_R{vong}_... khớp 100% CSDL.
- Kết quả kiểm thử tích hợp toàn diện:
  + Kịch bản Web/tests/test_live_backend_6_stages_integration.py chạy trên API Testing cổng 5001 và CSDL độc lập QL_TapChiKhoaHoc_Test; xác minh môi trường trước khi ghi dữ liệu.
  + Đạt 9/9 nhóm kiểm thử tích hợp, sau đó dọn tệp và bản ghi thử nghiệm về trạng thái seed ban đầu.
"""

def update_tong_quan():
    print("-> Đang cập nhật TONG_QUAN_DU_AN_KLTN.txt...")
    if os.path.exists(TONG_QUAN_PATH):
        with open(TONG_QUAN_PATH, "r", encoding="utf-8") as f:
            content = f.read()

        # Cập nhật phân công vai trò trong phần đầu file
        role_search = "* PHÂN CÔNG VAI TRÒ & PHẠM VI THỰC HIỆN CỦA TÁC GIẢ BÁO CÁO:"
        if role_search in content:
            r_start = content.find(role_search)
            r_end = content.find("\n\n2. KIẾN TRÚC CÔNG NGHỆ", r_start)
            if r_end != -1:
                new_role_text = """* PHÂN CÔNG VAI TRÒ & PHẠM VI THỰC HIỆN CỦA TÁC GIẢ BÁO CÁO:
  - VAI TRÒ XÁC ĐỊNH: TÁC GIẢ (ĐẶNG THÀNH THI) PHỤ TRÁCH CẢ 2 PHÂN HỆ: WEB PORTAL VÀ BACKEND API (.NET 9) (Web Portal Frontend HTML5/CSS3/JavaScript ES6+ Vanilla, thiết kế giao diện học thuật chuẩn Hallmark, Dịch vụ Backend ASP.NET Core Web API .NET 9, EF Core, RESTful Controllers, bảo mật JWT, kết nối SQL Server và toàn bộ các bộ kiểm thử tự động Playwright E2E).
  - Phân hệ Desktop C# WinForms (Thư mục Winform/): Do thành viên khác trong nhóm phụ trách và triển khai độc lập. TUYỆT ĐỐI ĐÓNG BĂNG, KHÔNG THUỘC PHẠM VI LẬP TRÌNH CỦA TÔI."""
                content = content[:r_start] + new_role_text + content[r_end:]
        
        # Nếu chưa có mục 10 thì thêm vào cuối
        if "10. CÁC TÍNH NĂNG MỚI HOÀN THIỆN" not in content:
            content += SECTION_UPDATE
            with open(TONG_QUAN_PATH, "w", encoding="utf-8") as f:
                f.write(content)
            print("  [✓] Đã bổ sung Mục 10 vào TONG_QUAN_DU_AN_KLTN.txt thành công!")
        else:
            # Cập nhật lại mục 10
            idx = content.find("10. CÁC TÍNH NĂNG MỚI HOÀN THIỆN")
            header_idx = content.rfind("================================================================================", 0, idx)
            content = content[:header_idx].strip() + SECTION_UPDATE
            with open(TONG_QUAN_PATH, "w", encoding="utf-8") as f:
                f.write(content)
            print("  [✓] Đã cập nhật lại Mục 10 trong TONG_QUAN_DU_AN_KLTN.txt thành công!")

# 2. CẬP NHẬT TOAN_BO_DU_AN_VA_SOURCE_CODE_KLTN.txt
IGNORE_DIRS = {
    ".git", ".vs", "bin", "obj", "node_modules", "__pycache__", ".system_generated", "logs", "tasks", "brain",
    "web_source_legacy_archive", "Winform_Old_Backup", "Sao_Luu"
}
IGNORE_EXTS = {
    ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".webp",
    ".zip", ".rar", ".7z", ".tar", ".gz",
    ".dll", ".exe", ".pdb", ".bin",
    ".pdf", ".docx", ".xlsx", ".pptx",
    ".pyc"
}

INCLUDED_EXTS = {
    ".html", ".css", ".js", ".cs", ".sql", ".json", ".md", ".txt", ".bat", ".ps1", ".csproj", ".sln", ".py"
}

def generate_full_source_code_file():
    print("\n-> Đang quét toàn bộ thư mục và tạo tệp TOAN_BO_DU_AN_VA_SOURCE_CODE_KLTN.txt...")
    
    files_to_export = []
    for root, dirs, files in os.walk(ROOT_DIR):
        # Lọc thư mục bị bỏ qua
        dirs[:] = [d for d in dirs if d not in IGNORE_DIRS and not d.startswith(".")]
        
        for file in sorted(files):
            ext = os.path.splitext(file)[1].lower()
            if ext in IGNORE_EXTS:
                continue
            if ext not in INCLUDED_EXTS:
                continue
            
            # Tránh tự đọc file output
            if file == "TOAN_BO_DU_AN_VA_SOURCE_CODE_KLTN.txt":
                continue
            
            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, ROOT_DIR)
            files_to_export.append((rel_path, full_path))

    print(f"  Đã tìm thấy {len(files_to_export)} tệp mã nguồn và cấu hình hợp lệ.")

    header = """====================================================================================================
TỔNG HỢP TOÀN BỘ NỘI DUNG DỰ ÁN, CƠ SỞ DỮ LIỆU & TOÀN BỘ MÃ NGUỒN (SOURCE CODE)
ĐỀ TÀI: XÂY DỰNG HỆ THỐNG QUẢN LÝ TÒA SOẠN & XUẤT BẢN TẠP CHÍ KHOA HỌC (HUIT JOURNAL)
ĐƠN VỊ: TRƯỜNG ĐẠI HỌC CÔNG THƯƠNG TP. HỒ CHÍ MINH (HUIT)

LƯU Ý QUAN TRỌNG VỀ PHÂN CÔNG NHIỆM VỤ & PHẠM VI THỰC HIỆN CỦA TÁC GIẢ BÁO CÁO:
- TÁC GIẢ BÁO CÁO NÀY (ĐẶNG THÀNH THI) PHỤ TRÁCH CẢ 2 PHÂN HỆ: WEB PORTAL VÀ BACKEND API (C# .NET 9):
  + Phụ trách toàn diện lập trình Web Portal: Frontend HTML5, CSS3, JavaScript (ES6+ Vanilla), thiết kế giao diện học thuật chuẩn Hallmark.
  + Toàn bộ logic giao diện Web, tương tác người dùng, quản lý phiên làm việc, Stepper nộp bài báo khoa học.
  + Bàn làm việc Tác giả & Phản biện viên trên Web, nộp bản sửa đổi (BM-03), tải bản thảo ẩn danh (Double-Blind), đổi mật khẩu, avatar.
  + Kiến trúc Hybrid Dual-Engine (kết nối trực tiếp Web API hoặc chạy độc lập trên Vercel Cloud CDN khi máy chủ cá nhân tắt).
  + Phụ trách toàn diện Backend API: ASP.NET Core Web API .NET 9, Entity Framework Core 9, CSDL SQL Server 2022.
  + Bảo mật JWT, giải thuật băm BCrypt, quản lý phân quyền 5 vai trò, máy trạng thái bài báo khoa học, phân công phản biện kín.
  + Toàn bộ các bộ kiểm thử tự động Web E2E bằng Playwright Python, Node.js và Kiểm thử tích hợp Backend API.
- PHÂN HỆ DESKTOP WINFORMS VÀ CÁC PHẦN KHÁC:
  + Phân hệ WinForms gốc do thành viên khác trong nhóm phụ trách.
  + Theo yêu cầu tích hợp ngày 25/09/2026, các điểm nối WinForms–Backend API đã được bổ sung cho đăng nhập, phân quyền, phân công, quyết định biên tập, số báo và quản lý người dùng.
  + Các thao tác WinForms còn dùng SQL trực tiếp được ghi rõ trong HoTro/KetNoi_WinForms_Web.md; chưa thể coi là đã chuyển toàn bộ Desktop sang API.

====================================================================================================
BÁO CÁO KẾT QUẢ THỰC HIỆN & KIỂM THỬ TOÀN DIỆN LUỒNG 6 GIAI ĐOẠN XUẤT BẢN (NGÀY 25/09/2026)
====================================================================================================

I. KẾT QUẢ XỬ LÝ 5 ĐIỂM NGHẼN NGHIỆP VỤ HỆ THỐNG:
1. SỬA ĐIỀU KIỆN CÔNG BỐ SỐ BÁO:
   - Hiện tượng: GetPublicArticleAsync và GetPublicArticlePdfAsync chỉ kiểm tra SoTapChi.TrangThai == "Đã phát hành", làm cho bài báo thuộc số có trạng thái "Đã xuất bản" (theo ràng buộc SQL CHK_SoTapChi_TrangThai và WinForms) không thể đọc hoặc tải PDF.
   - Khắc phục: Sửa BaiBaoService.cs đồng bộ kiểm tra (x.SoTapChi.TrangThai == "Đã xuất bản" || x.SoTapChi.TrangThai == "Đã phát hành"). Độc giả tra cứu công khai và tải PDF thành phẩm thành công.

2. TẠO LUỒNG TẢI LÊN TỆP ẨN DANH VÀ PDF THÀNH PHẨM:
   - Hiện tượng: API kiểm tra tệp ẩn danh và PDF thành phẩm nhưng chưa có endpoint để Ban biên tập tải lên các tệp này.
   - Khắc phục: Bổ sung 2 endpoints mới tại BaiBaoController.cs:
     + POST /api/baibao/{id}/upload-anonymous-manuscript?soVong={vong}: Ban biên tập tải lên bản thảo ẩn danh theo từng vòng thẩm định phục vụ phản biện kín Double-Blind.
     + POST /api/baibao/{id}/upload-published-pdf: Ban biên tập tải lên tệp PDF thành phẩm xuất bản sau khi duyệt bản bông.

3. ĐỒNG BỘ SCHEMA CSDL BẰNG MIGRATION:
   - Hiện tượng: CSDL SQL Server thiếu bảng lưu trữ hồ sơ đăng ký phản biện thật và ràng buộc CHK_ThuMuc_LoaiThuMuc thiếu loại tệp xuất bản.
   - Khắc phục: Thực thi kịch bản HoTro/Migration_20260925_UpdateSchema.sql với cờ sqlcmd -f 65001 trên CSDL thật QL_TapChiKhoaHoc:
     + Cập nhật ràng buộc CHK_ThuMuc_LoaiThuMuc bổ sung đầy đủ: 'PDF thành phẩm', 'PDF Xuất bản', 'Bản thảo ẩn danh', 'File ẩn danh', 'Bản chỉnh sửa', 'Bản giải trình BM-03', 'Bản đánh dấu sửa đổi'.
     + Tạo mới bảng DonDangKyPhanBien (MaDon, MaNguoiDung, NgayDangKy, GhiChu, TrangThai, MaNguoiDuyet, NgayDuyet, LyDoTuChoi) với các khóa ngoại và ràng buộc CHECK chuẩn tiếng Việt UTF-8.

4. LƯU ĐƠN XIN LÀM PHẢN BIỆN THẬT:
   - Hiện tượng: Đăng ký phản biện chưa lưu vào CSDL, thiếu hàng đợi chờ duyệt của Ban biên tập.
   - Khắc phục: Thêm thực thể DonDangKyPhanBien trong Entities.cs và QLTapChiKhoaHocContext.cs. Thêm 3 endpoints tại AuthController.cs:
     + POST /api/auth/request-reviewer: Người dùng nộp đơn xin tham gia Hội đồng phản biện (lưu vào CSDL với trạng thái 'Chờ duyệt').
     + GET /api/auth/pending-reviewers: Ban biên tập / Quản trị viên lấy danh sách đơn đăng ký chờ thẩm định.
     + POST /api/auth/approve-reviewer/{id}: Ban biên tập duyệt cấp vai trò Chuyên gia phản biện (Role 4) vào NguoiDung_VaiTro.
     + POST /api/auth/reject-reviewer/{id}: Ban biên tập từ chối đơn có lý do giải trình.

5. CHỐT VÒNG PHẢN BIỆN & BẢN SỬA ĐỘNG:
   - Hiện tượng: Chưa gắn chặt số vòng phản biện với tệp bản thảo ẩn danh và bản sửa đổi, dễ gây sai lệch hồ sơ.
   - Khắc phục:
     + Tại PhanBienService.AssignReviewerAsync: Tính toán targetRound động, kiểm tra bắt buộc phải có tệp ẩn danh của đúng vòng targetRound mới cho phép phân công chuyên gia.
     + Tại BaiBaoService.SubmitRevisionAsync: Tự động tính revisionRound = currentAssignmentRound + 1, lưu tệp chuẩn hóa Clean_R{revisionRound}_... và BM03_R{revisionRound}_... gắn đúng SoVong trong ThuMucBaiBao.

II. TÌNH TRẠNG KIỂM CHỨNG CỦA PHIÊN BẢN MÃ NGUỒN NÀY:
1. Backend API .NET 9: biên dịch thành công vào thư mục bin/Verify với 0 cảnh báo và 0 lỗi.
2. Bộ kiểm thử tích hợp Web/tests/test_live_backend_6_stages_integration.py gồm 9 nhóm và đã đạt 9/9 trên API Testing cổng 5001 với QL_TapChiKhoaHoc_Test. Bộ kiểm thử xác minh môi trường/CSDL trước khi ghi dữ liệu, sau đó dọn tệp và bản ghi về 10 bài báo, 10 người dùng gốc.
3. API tách GhiChu nội bộ khỏi ThongBaoChoTacGia, chỉ bắt đầu phản biện sau hai phân công cùng vòng và kiểm tra tệp ẩn danh đúng vòng.
4. Migration HoTro/Migration_20260925_AuthorVisibleNote.sql bổ sung trường thông báo cho tác giả vào CSDL hiện có.
5. Luồng kết nối WinForms–Web: cả Backend API và WinForms build sạch; bài xuất bản qua endpoint phát hành số đã xuất hiện trên Web và tải được PDF trong 9/9 nhóm kiểm thử.

NGÀY CẬP NHẬT: 25/09/2026 - TÌNH TRẠNG ĐƯỢC GHI THEO KIỂM CHỨNG THỰC TẾ
====================================================================================================

"""

    with open(TOAN_BO_PATH, "w", encoding="utf-8") as out_f:
        out_f.write(header)
        
        for rel_path, full_path in files_to_export:
            try:
                size = os.path.getsize(full_path)
                with open(full_path, "r", encoding="utf-8", errors="replace") as in_f:
                    code_content = in_f.read()
                
                out_f.write("=" * 100 + "\n")
                out_f.write(f"TỆP TIN: {rel_path}\n")
                out_f.write(f"ĐƯỜNG DẪN ĐẦY ĐỦ: {full_path}\n")
                out_f.write(f"KÍCH THƯỚC: {size:,} bytes\n")
                out_f.write("=" * 100 + "\n\n")
                out_f.write(code_content + "\n\n")
                print(f"  + Đã xuất: {rel_path} ({size:,} bytes)")
            except Exception as e:
                print(f"  [!] Bỏ qua {rel_path}: {e}")

    total_size = os.path.getsize(TOAN_BO_PATH)
    print(f"\n[✓] HOÀN TẤT! Tệp TOAN_BO_DU_AN_VA_SOURCE_CODE_KLTN.txt đã được tạo thành công.")
    print(f"    Tổng dung lượng: {total_size / (1024*1024):.2f} MB ({total_size:,} bytes).")

if __name__ == "__main__":
    update_tong_quan()
    generate_full_source_code_file()
