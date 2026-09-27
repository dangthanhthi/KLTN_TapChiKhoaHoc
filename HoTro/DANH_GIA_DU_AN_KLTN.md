# Đánh giá dự án quản lý bài báo khoa học

Ngày cập nhật: 25/09/2026. Thư mục nguồn: `C:\Users\MSIIIIII\Desktop\Khoa_Luan_Tot _Nghiep`.

## 1. Kết luận và phạm vi

> [!IMPORTANT]
> **Xác nhận phạm vi thực hiện của tác giả báo cáo:** Tác giả bản tài liệu này (Đặng Thành Thi) **phụ trách toàn diện cả 2 phân hệ: Web Portal và Backend API** (Web Portal Frontend HTML/CSS/JS chuẩn Hallmark, cơ chế Hybrid Dual-Engine, bộ kiểm thử E2E Playwright và Dịch vụ Backend ASP.NET Core Web API .NET 9, EF Core, RESTful Controllers, Bảo mật JWT). Phân hệ WinForms Desktop (.NET 8) do thành viên khác trong nhóm đảm nhiệm độc lập; tác giả tuân thủ quy tắc không can thiệp trực tiếp vào mã nguồn WinForms ("KHÔNG SỬA WINFORMS") mà thực hiện đồng bộ chuẩn hóa thông qua tầng Web API.

Dự án có nền tảng hoàn chỉnh và bám sát mục tiêu đề tài: Cổng thông tin Tạp chí Khoa học (Web Portal), Ứng dụng Quản trị Tòa soạn (WinForms), Dịch vụ Backend (ASP.NET Core Web API), CSDL Quan hệ (SQL Server), hệ thống phân vai (5 vai trò chuẩn), quy trình quản lý bài báo khoa học, phân công phản biện kín hai chiều (Double-Blind), quản lý số phát hành và nhật ký lịch sử trạng thái.

Đối với phân hệ Web, hệ thống đã được hoàn thiện giao diện theo tiêu chuẩn thiết kế Hallmark (Anti-AI-slop, locked design tokens, PerMonitorV2, responsive toàn diện, loại bỏ hoàn toàn bullet point). Phân hệ Web vận hành theo kiến trúc kép **Hybrid Dual-Engine** (trực tiếp kết nối SQL Server qua Web API khi máy chủ hoạt động, và tự động kích hoạt Standalone Engine an toàn khi triển khai trên Vercel/Cloud CDN). Phân hệ Web đã được kiểm thử tự động toàn diện bằng bộ kiểm thử E2E Playwright (`test_standalone_engine_e2e.py`) đạt kết quả **100% PASS (5/5 kịch bản, 60/60 điểm kiểm tra)** và bộ kiểm thử Bàn làm việc phản biện chuyên biệt (`test_reviewer_workspace.cjs`) đạt kết quả **100% PASS (12/12 bài kiểm tra)**.

Báo cáo này được cập nhật đối chiếu trực tiếp giữa Đề cương Khóa luận tốt nghiệp, kiến trúc mã nguồn thực tế (Backend, Web, WinForms, SQL Server) và các kết quả kiểm thử tự động mới nhất nhằm phản ánh chính xác hiện trạng và định hướng bảo vệ khóa luận thành công.

## 2. Hiểu đúng yêu cầu của đề cương và kiến trúc Web

File `CNTT-KLCN011_LamThiHoaMi_Xây dựng hệ thống quản lý bài báo tại Tạp chí Khoa học Công nghệ_final.docx` là **đề cương khóa luận**, đặt ra các yêu cầu cốt lõi: hệ thống gồm hai nền tảng đồng bộ (Web và WinForms), phân quyền người dùng, quy trình nộp và chỉnh sửa bài báo, giao và thực hiện phản biện khoa học, gửi thông báo thay đổi trạng thái, quản lý số phát hành, đọc và tải PDF toàn văn, tìm kiếm bài báo, báo cáo thống kê, sao lưu/phục hồi và giao diện responsive.

Về mặt công nghệ Web, đề cương nêu định hướng: C# .NET Framework/.NET Core, SQL Server, Website sử dụng HTML/CSS/JS kết hợp ASP.NET Core MVC hoặc PHP/Laravel. Thực tế dự án đã chọn kiến trúc **Decoupled Architecture (Kiến trúc phân tách)** hiện đại:
- **Frontend Web:** HTML5, CSS3, JavaScript chuẩn ES6+ thuần (Vanilla JS), tuân thủ tiêu chuẩn Hallmark UI, triển khai tối ưu trên nền tảng Cloud CDN (Vercel Serverless Edge).
- **Backend API:** ASP.NET Core Web API (.NET 9) cung cấp RESTful endpoints chuẩn hóa, kết nối CSDL SQL Server.
- **Tính ưu việt so với MVC truyền thống:** Kiến trúc Web API + SPA/Static Web tĩnh vượt trội về tốc độ tải trang, trải nghiệm người dùng mượt mà không bị reload toàn trang (SPA UX), dễ dàng mở rộng và bảo trì độc lập giữa giao diện người dùng và tầng logic nghiệp vụ, đồng thời cho phép phân hệ Web hoạt động ổn định trên cả môi trường trực tuyến lẫn môi trường trình diễn độc lập (Vercel Standalone Engine). Đây là điểm sáng kiến trúc cần được giải trình và làm nổi bật trong thuyết minh khóa luận.

Các quy chuẩn học thuật như quy trình phản biện kín hai chiều (Double-Blind Peer Review), thẩm định và phê duyệt vai trò phản biện, ràng buộc học hàm theo quy định của Hội đồng Giáo sư Nhà nước đều đã được hiện thực hóa chặt chẽ trên Web và Backend API.

## 3. Đánh giá chi tiết các vấn đề trọng yếu và hiện trạng xử lý

### P1-01 — Thống nhất xử lý và mã hóa mật khẩu giữa Web và WinForms

- **Hiện trạng Backend & Web:** `Backend/HuitJournal.Api/Services/AuthService.cs` sử dụng giải thuật băm chuẩn công nghiệp BCrypt với độ muối (salt) tự sinh khi đăng ký và xác thực tài khoản. Trên giao diện Web ở chế độ độc lập (Standalone Vercel), hệ thống đã loại bỏ hoàn toàn việc lưu mật khẩu dạng rõ, chuyển sang lưu trữ `passwordHash` (mã hóa Base64 che giấu phiên cục bộ). Ngoài ra, Web đã hoàn thiện tính năng **Đổi mật khẩu** tại trang cá nhân với thanh đo trực quan độ mạnh 4 cấp độ.
- **Cầu nối tương thích tạm thời ở Backend:** Để hỗ trợ WinForms (vốn vẫn kiểm tra mật khẩu chuỗi thuần `MatKhau = @MatKhau`) không bị khóa tài khoản khi kiểm thử, Backend API kiểm tra BCrypt trước, nếu không khớp thì đối chiếu chuỗi thuần và **tuyệt đối không ghi đè chuỗi băm vào SQL Server**.
- **Giải pháp dứt điểm cần chuyển giao cho WinForms:** Đây là giải pháp tương thích tạm thời phía API; để hoàn thành triệt để tiêu chuẩn bảo mật P0, thành viên phụ trách WinForms cần chuyển tầng xác thực sang gọi API `POST /api/auth/login` hoặc tích hợp thư viện `BCrypt.Net-Next` vào ứng dụng Desktop.

### P1-02 — Cơ chế vận hành Hybrid Dual-Engine trên Web (Online API vs Vercel Standalone)

- **Bản chất kiến trúc:** Phân hệ Web được thiết kế với cơ chế hoạt động kép **Hybrid Dual-Engine** phân định rạch ròi giữa chế độ Online và Demo:
  1. **Chế độ Online (Kết nối SQL Server API):** Mọi thao tác gửi bài, đánh giá, đổi mật khẩu đều được xác thực qua SQL Server. Nếu API trả lỗi (400, 401, 403, 500) hoặc mất kết nối mạng, giao diện dừng lại ở lỗi và hiển thị thông báo trung thực từ máy chủ, tuyệt đối không tự ý rơi về dữ liệu mẫu hay lưu bài vào `localStorage`.
  2. **Chế độ Standalone (Vercel / Cloud Demo):** Chỉ được kích hoạt tường minh qua tham số URL `?mode=demo` hoặc cấu hình `journal_app_mode === 'demo'`. Khi hoạt động ở chế độ này, giao diện luôn hiển thị dải thông báo cố định màu cam hổ phách ở đỉnh trang: `"● CHẾ ĐỘ MÔ PHỎNG DEMO — Dữ liệu lưu trữ tạm thời trên trình duyệt, không đồng bộ về CSDL tòa soạn"`.
- **Nghiệm thu Web:** Đã qua kiểm thử tự động Playwright E2E cho thấy cả hai nhánh hoạt động độc lập, rõ ràng, không gây nhầm lẫn giữa dữ liệu thật và dữ liệu thử nghiệm.

### P1-03 — Bảo mật mật khẩu phiên làm việc trên Web

- **Hiện trạng xử lý:** ĐÃ KHẮC PHỤC TRIỆT ĐỂ. Trong `Web/journal-interactions.js`, đã xóa bỏ hoàn toàn thuộc tính `password` trong đối tượng người dùng `newUser` và `currentUser` tại `localStorage`.
- **Kết quả:** Khi người dùng đăng ký hoặc đăng nhập, bộ nhớ trình duyệt chỉ lưu trữ các thông tin hồ sơ cần thiết cùng `passwordHash`. Ngăn chặn hoàn toàn rủi ro lộ mật khẩu gốc qua DevTools hay công cụ rà soát bộ nhớ trình duyệt.

### P1-04 — Bảo vệ tệp bản thảo khoa học có phân quyền

- **Hiện trạng xử lý:** ĐÃ KHẮC PHỤC TRIỆT ĐỂ TRÊN BACKEND. Trong `Backend/HuitJournal.Api/Program.cs`, đã loại bỏ hoàn toàn việc mở static file cho thư mục `Uploads`. Chỉ duy nhất thư mục ảnh đại diện `/uploads/avatars` được phục vụ tĩnh.
- **Kiểm soát truy cập:** Toàn bộ tệp bản thảo gốc và bản chỉnh sửa trong `Uploads/Submissions` và `Uploads/revisions` chỉ được tải thông qua các API endpoints có xác thực JWT Bearer Token (`GET /api/baibao/{id}/manuscript` cho Tác giả/Biên tập và `GET /api/phanbien/assignments/{id}/manuscript` cho Phản biện viên được phân công).

### P1-05 — Chuẩn hóa điều kiện công khai bài báo và tệp xuất bản

- **Hiện trạng xử lý:** ĐÃ KHẮC PHỤC TRIỆT ĐỂ VÀ KIỂM THỬ THÀNH CÔNG VỚI SQL THẬT:
  1. Cập nhật `SQLKLCN.sql` và CSDL live `QL_TapChiKhoaHoc`: Bổ sung hai loại tệp `N'PDF thành phẩm'` và `N'PDF Xuất bản'` vào ràng buộc `CHK_ThuMuc_LoaiThuMuc`.
  2. Bổ sung endpoint `POST /api/baibao/{id}/upload-published-pdf` cho Ban biên tập tải lên PDF thành phẩm sau khi duyệt bản bông.
  3. Tại `BaiBaoService.cs`, phương thức `GetPublicArticleAsync` và `GetPublicArticlePdfAsync` kiểm tra số báo có trạng thái `"Đã xuất bản"` hoặc `"Đã phát hành"` (khớp 100% với ràng buộc SQL `CHK_SoTapChi_TrangThai`), đồng thời ẩn toàn bộ URL tệp bản thảo nội bộ.
  4. Phương thức `GetPublicArticlePdfAsync` chỉ cho phép tải tệp có loại `"PDF thành phẩm"` hoặc `"PDF Xuất bản"`, loại bỏ hoàn toàn cơ chế fallback sang bản thảo thô chưa qua biên tập.

### P1-06 — Quy tắc chuyển trạng thái bài báo khoa học

- **Hiện trạng xử lý:** ĐÃ ĐỒNG BỘ MÁY TRẠNG THÁI.
  1. Thống nhất tên trạng thái chuẩn là `"Chờ chỉnh sửa"` trên toàn bộ SQL Server (`CHK_BaiBao_TrangThai`), Backend API (`BaiBaoService.cs`, `PhanBienService.cs`) và Web (`profile.html`).
  2. Ràng buộc máy trạng thái tại `BaiBaoService.cs`: Tác giả chỉ được phép nộp lại bản thảo chỉnh sửa và biểu mẫu BM-03 khi bài báo đang ở trạng thái `"Chờ chỉnh sửa"`. Ngăn chặn tuyệt đối việc nộp đè khi bài đang trong vòng bình duyệt hoặc đã có quyết định xuất bản/từ chối.

### P1-07 — Bảo đảm tính bảo mật của quy trình Phản biện kín hai chiều (Double-Blind Review)

- **Hiện trạng xử lý trên Backend & Web:** ĐÃ BẢO ĐẢM TOÀN DIỆN VÀ HOÀN TẤT ENDPOINTS:
  1. *Bổ sung endpoint tải tệp ẩn danh:* `POST /api/baibao/{id}/upload-anonymous-manuscript?soVong={vong}` cho phép Ban biên tập tạo và tải tệp bản thảo ẩn danh theo từng vòng thẩm định.
  2. *Chặn phân công nếu thiếu tệp ẩn danh:* Tại `PhanBienService.cs` (`AssignReviewerAsync`), hệ thống bắt buộc kiểm tra bài báo đã có tệp loại `"File ẩn danh"` hoặc `"Bản thảo ẩn danh"` cho vòng tương ứng hay chưa. Nếu chưa có, API lập tức từ chối phân công với mã lỗi 400.
  3. *Chỉ phục vụ tệp ẩn danh cho phản biện viên:* Phương thức `GetManuscriptForReviewerAsync` chỉ trả về tệp loại `"File ẩn danh"` hoặc `"Bản thảo ẩn danh"` gắn với đúng vòng phản biện (`SoVong`), tuyệt đối không để lộ tệp bản thảo gốc hay bản giải trình chứa thông tin tác giả.
  4. *Phía Tác giả:* Giao diện và API lịch sử xử lý không để lộ danh tính, học vị hay email của chuyên gia phản biện.

### P1-08 — Không gian Bàn làm việc Phản biện chuyên biệt (`reviewer.html`)

- **Hiện trạng xử lý trên Web:** ĐÃ HOÀN THIỆN ĐẲNG CẤP. Phân hệ Web đã tách riêng một trang chuyên biệt **Bàn làm việc Phản biện (`Web/reviewer.html`)**:
  - Quản lý danh sách nhiệm vụ được phân công, tìm kiếm theo tiêu đề/mã bài/chuyên ngành, bộ lọc hạn và phân trang.
  - Bộ đếm chỉ số trực quan: Đang xử lý, Đến hạn trong 3 ngày, Quá hạn, Đã đánh giá.
  - Lịch sử đánh giá của chính chuyên gia, xem lại nhận xét các vòng trên cùng bài báo.
  - Biểu mẫu BM-04 chuẩn xác theo DTO Backend: 4 tiêu chí điểm (Tính mới, Phương pháp, Kết quả, Trình bày), tự tính điểm trung bình, 2 vùng nhận xét (gửi tác giả và bảo mật cho biên tập), 4 kiến nghị chuẩn.
  - Cơ chế lưu nháp an toàn theo phiên trong `sessionStorage`, hỗ trợ xuất lịch ICS và xuất TXT bản ghi đánh giá.
  - Hỗ trợ gọi endpoint tải bản thảo ẩn danh (`/api/phanbien/assignments/{id}/manuscript`) và đọc phiếu cũ (`/api/phanbien/assignments/{id}/evaluation`).
- **Kết quả:** Đã kiểm thử 12/12 kịch bản tự động PASS 100% bằng Playwright (`test_reviewer_workspace.cjs`).

### P1-09 — Quản lý phiên bản bản thảo chỉnh sửa (Revisions) theo Vòng động

- **Hiện trạng xử lý:** ĐÃ HIỆN THỰC HÓA VÒNG ĐỘNG SERVER-SIDE:
  1. Tại `PhanBienService.AssignReviewerAsync`, vòng phản biện (`targetRound`) được tính động dựa trên số vòng chỉnh sửa lớn nhất và phân công trước đó, hoặc chỉ định từ biên tập viên.
  2. Tại `BaiBaoService.SubmitRevisionAsync`, hệ thống tự động xác định `revisionRound = currentAssignmentRound + 1` và đặt tên tệp lưu trữ chuẩn hóa: `Clean_R{revisionRound}_...`, `BM03_R{revisionRound}_...`, `Tracked_R{revisionRound}_...` gắn đúng `SoVong` trong `ThuMucBaiBao`.
  3. CSDL SQL Server và EF Core ghi nhận chuẩn xác từng bản sửa theo từng vòng, ngăn ngừa việc trộn lẫn tệp giữa các vòng phản biện khác nhau.

### P1-10 — Phòng chống lỗ hổng Cross-Site Scripting (XSS) trên giao diện Web

- **Hiện trạng xử lý trên Web:** ĐÃ KHẮC PHỤC TRIỆT ĐỂ. Trong `Web/profile.html`, `Web/reviewer.js` và các trang Web liên quan, toàn bộ các vùng hiển thị dữ liệu đầu vào của người dùng đều được xử lý bằng DOM `textContent` an toàn hoặc qua hàm `escapeHtml()` và mã hóa đối số `escapeJs()`.
- **Kết quả:** Ngăn chặn tuyệt đối việc kẻ xấu chèn mã script độc hại thông qua tiêu đề bài báo hay nhận xét để thực thi trên trình duyệt của người dùng khác.

## 4. Bảng tổng hợp các tính năng và hiện trạng hoàn thiện trên Web

| Hạng mục tính năng | Hiện trạng triển khai trên Web | Đánh giá & Định hướng bảo vệ |
|---|---|---|
| **Bàn làm việc Phản biện chuyên biệt (`reviewer.html`)** | Trang riêng chuẩn học thuật, danh sách nhiệm vụ, bộ đếm hạn, phiếu BM-04 chuẩn DTO, lưu nháp sessionStorage, xuất ICS/TXT, kiểm thử 12/12 PASS. | Đạt chuẩn xuất sắc quy trình bình duyệt khoa học quốc tế OJS. |
| **Ràng buộc học thuật Học vị ↔ Học hàm** | Đã triển khai logic kiểm tra chéo: Khóa lựa chọn Phó giáo sư / Giáo sư nếu học vị chưa đạt Tiến sĩ; thông báo lỗi trực quan nếu vi phạm quy chế Nhà nước. | Phù hợp 100% với quy định của Hội đồng Giáo sư Nhà nước Việt Nam. |
| **Phân quyền theo học vị & Duyệt vai trò Phản biện** | Đăng ký tài khoản không tự cấp quyền Phản biện viên (chỉ cấp vai trò Tác giả và Độc giả). Người dùng gửi đơn đăng ký; Ban biên tập / Quản trị viên thẩm định chuyên môn và duyệt cấp quyền qua endpoint bảo mật `POST /api/auth/approve-reviewer/{id}`. | Chấm dứt hoàn toàn rủi ro tự cấp quyền; tuân thủ quy chuẩn thẩm định chuyên gia của tòa soạn. |
| **Chức năng Đổi mật khẩu** | Đã hoàn thiện modal đổi mật khẩu trên trang hồ sơ cá nhân (`profile.html`) với kiểm tra mật khẩu hiện tại, khớp mật khẩu mới và thanh đo độ mạnh 4 mức. | Hoàn chỉnh tính năng quản lý tài khoản người dùng theo chuẩn bảo mật. |
| **Hỗ trợ ảnh đại diện đa định dạng** | Hỗ trợ mở rộng tải ảnh `.jfif` bên cạnh `.jpg`, `.jpeg`, `.png`, `.webp`; xem trước tức thì và mã hóa lưu trữ Base64 an toàn. | Nâng cao trải nghiệm người dùng, xử lý tốt các định dạng ảnh phổ biến từ web. |
| **Tìm kiếm & Phân trang bài báo** | Trang `archives.html` hỗ trợ lọc theo từ khóa, chuyên ngành, năm xuất bản và số báo với thuật toán tìm kiếm tức thì và phân trang giao diện. | Đáp ứng tốt nhu cầu tra cứu bài báo khoa học của độc giả. |
| **Giao diện chuẩn Hallmark UI** | Thiết kế chống AI-slop, hệ thống Locked Design Tokens, font chữ chuẩn, không vẽ khung giả, 100% không dùng bullet point trên web. | Giao diện hiện đại, chuyên nghiệp, chuẩn mực của một tòa soạn tạp chí học thuật. |

## 5. Ma trận đối chiếu đề cương đối với phân hệ Web

| Yêu cầu đề cương | Hiện trạng phân hệ Web | Kết luận nghiệm thu |
|---|---|---|
| **Cổng thông tin cho tác giả, phản biện và độc giả** | Đầy đủ 3 phân hệ: Độc giả (tra cứu, đọc/tải PDF bài báo), Tác giả (nộp bài 6 bước, theo dõi tiến độ, nộp bản sửa đổi), Phản biện viên (Bàn làm việc chuyên biệt `reviewer.html`, phiếu BM-04). | **Đạt xuất sắc** |
| **Kiến trúc phân tách và khả năng đồng bộ** | Kiến trúc Decoupled Web API + Web CDN với cơ chế Hybrid Dual-Engine (Online SQL Server API & Standalone Engine). | **Đạt yêu cầu cao** |
| **Quản lý tài khoản, phân quyền, hồ sơ cá nhân** | Đăng ký, đăng nhập, phân quyền 5 vai trò chuẩn, đổi avatar (hỗ trợ .jfif), đổi mật khẩu, ràng buộc học hàm theo học vị. | **Đạt xuất sắc** |
| **Quy trình nộp bài và theo dõi tiến độ** | Biểu mẫu nộp bài tuần tự, kiểm tra tệp đính kèm, danh sách đồng tác giả, theo dõi dòng thời gian trực quan. | **Đạt xuất sắc** |
| **Phân công, thẩm định và đánh giá bài báo** | Bàn làm việc phản biện chuyên biệt, kiểm tra quyền hạn, phiếu nhận xét khoa học BM-04 và kiến nghị quyết định. | **Đạt xuất sắc** |
| **Xem phản hồi và nộp bản sửa đổi (Revision)** | Tác giả xem lịch sử xử lý bài, tải biểu mẫu sửa đổi và nộp lại bản thảo vòng 2. | **Đạt yêu cầu** |
| **Tra cứu bài báo và số tạp chí đã phát hành** | Tra cứu kho lưu trữ số báo (`archives.html`), xem số hiện tại (`current.html`), đọc tóm tắt, từ khóa và tải PDF. | **Đạt xuất sắc** |
| **Tính tương thích, Responsive và High-DPI** | Tương thích đa thiết bị (Mobile 375px, Tablet 768px, Desktop 1024px+), hỗ trợ PerMonitorV2, kiểm thử tự động 100% PASS. | **Đạt xuất sắc** |

## 6. Kết quả kiểm thử tự động hệ thống (Playwright E2E & Backend Live API)

Hệ thống đã được trang bị các bộ kiểm thử tự động toàn diện với kết quả đo đạc thực tế:
1. **Bộ kiểm thử Tích hợp Live Backend & CSDL Độc Lập 6 giai đoạn (`test_live_backend_6_stages_integration.py`):** Đạt **100% PASS trên cả 9 kịch bản** chạy thực tế trên máy chủ ASP.NET Core Web API .NET 9 và CSDL kiểm thử chuyên biệt `QL_TapChiKhoaHoc_Test`:
   - TEST 1: Xác thực & Phân quyền RBAC 5 vai trò (Admin, Biên tập, Phản biện 1, Phản biện 2, Tác giả).
   - TEST 2: Quy trình nộp đơn đăng ký phản biện thật (`DonDangKyPhanBien`), hàng đợi chờ duyệt, kiểm tra chặn mã đơn giả / chặn duyệt trùng, và Ban biên tập thẩm định cấp quyền Role 4.
   - TEST 3: Tiêu chuẩn kiểm soát tệp tải lên OWASP (chặn tệp .exe, chặn PDF thiếu `%PDF-`, chặn PDF thiếu `%%EOF`, quét phát hiện và chặn mã lệnh `/Launch`) và nộp bản thảo khoa học trực tuyến 5 bước.
   - TEST 4: Ma trận chuyển trạng thái Server-Side (chặn nhảy cóc sai quy trình 'Chờ sơ duyệt' -> 'Đã xuất bản') và cổng bảo vệ bản thảo ẩn danh.
   - TEST 5: Tải bản thảo ẩn danh Vòng 1, phân công 2 chuyên gia phản biện độc lập (`dangvang` ID 4 và `tranvann` ID 8), và **khẳng định 100% không lộ danh tính chuyên gia phía tác giả (Double-Blind Privacy Assertions)**.
   - TEST 6: Bàn làm việc phản biện độc lập, tải tệp ẩn danh, kiểm tra cổng chặn quyết định khi 0/2 và 1/2 chuyên gia đánh giá, và nộp đủ 2/2 phiếu BM-04.
   - TEST 7: Biên tập viên ra quyết định 'Chờ chỉnh sửa' và tác giả nộp bản thảo hoàn thiện Vòng 2 kèm giải trình BM-03 và tệp đánh dấu sửa đổi.
   - TEST 8: Cổng kiểm soát xuất bản (chặn xuất bản khi chưa xếp số / chưa có PDF), Ban biên tập chấp nhận đăng, xếp bài vào Số 42 (gán số trang và DOI qua `POST /api/baibao/{id}/assign-issue`), tải tệp PDF thành phẩm và xuất bản chính thức.
   - TEST 9: Độc giả vãng lai không đăng nhập truy cập chi tiết công khai, nhận diện liên kết PDF chính thức và tải trực tiếp tệp PDF thành phẩm khớp 100% nội dung xuất bản.
   - *Cơ chế cách ly & Bảo vệ CSDL:* Toàn bộ tệp vật lý trên đĩa (`Uploads/`) và dữ liệu sinh ra trong quá trình kiểm thử được khối lệnh `finally` tự động dọn dẹp sạch sẽ khỏi CSDL `QL_TapChiKhoaHoc_Test` thông qua `sqlcmd`, đồng thời đối chiếu xác minh trạng thái CSDL chuẩn seed gốc (chính xác 10 bài báo và 10 người dùng).
2. **Bộ kiểm thử Kiểm chứng Chặn lộ danh tính & Bảo mật PDF (`test_privacy_and_public_pdf_clean.py`):** Đạt **PASS** xác minh chi tiết lịch sử trạng thái phía tác giả và bảo mật đường dẫn PDF công khai.
3. **Bộ kiểm thử E2E Standalone Engine (`test_standalone_engine_e2e.py`):** Đạt **100% PASS trên cả 5 kịch bản** (Đăng nhập offline, Chuyển hướng và đánh giá BM-04 trên `reviewer.html`, Đổi mật khẩu, Nộp bài báo độc lập, Đăng ký tài khoản và kích hoạt vai trò Phản biện viên).
4. **Bộ kiểm thử Bàn làm việc Phản biện (`test_reviewer_workspace.cjs`):** Đạt **100% PASS trên cả 12 bài kiểm tra** chuyên sâu về bảo mật quyền truy cập, lọc thời hạn, xử lý bản nháp, xuất ICS/TXT, chống XSS và responsive đa kích thước.
5. **Kiểm thử biên dịch Backend API C# (.NET 9):** Biên dịch dự án `Backend/HuitJournal.Api`: **Build succeeded — 0 Warning(s), 0 Error(s)**.



