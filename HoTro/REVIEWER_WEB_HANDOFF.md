# Bàn làm việc phản biện — phạm vi Web và bàn giao API

Ngày 24/09/2026. Đặng Thành Thi phụ trách lập trình Web. Đợt này chỉ sửa phân hệ Web, không sửa WinForms, Backend, SQL hoặc đề cương của GVHD.

## Công nghệ và lựa chọn tối ưu

Giữ HTML5, CSS3 và JavaScript thuần, phù hợp Web tĩnh hiện tại. Trang mới tách CSS, quy tắc thuần và phần tương tác; tải script bằng `defer`, không thêm framework hoặc thư viện runtime. Không cần đổi sang React/Vue chỉ để xây một trang nghiệp vụ. Sau này có thể thêm TypeScript/Vite cho kiểm tra kiểu, build và quản lý module nếu cả Web cần mở rộng; thay đổi frontend phải giữ hợp đồng API dùng chung.

Đây là Web nhiều trang kết nối API, không mặc định là SPA hoặc đồng bộ offline. Dữ liệu localStorage ở demo không được đồng bộ đến SQL Server. Phân quyền trình duyệt phục vụ điều hướng; API phải tự xác thực từng thao tác.

## Những gì đã làm

- `reviewer.html`: trang riêng, không công khai dữ liệu khi chưa kiểm tra quyền.
- `reviewer.css`: giao diện học thuật HUIT, danh sách bên trái và hồ sơ bên phải, responsive, hỗ trợ bàn phím và dialog native.
- `reviewer-core.js`: kiểm tra vai trò, lọc/sắp xếp, thời hạn và kiểm tra điểm.
- `reviewer.js`: API adapter không fallback, danh sách, lịch sử của chính reviewer, bản nháp, phiếu, lịch ICS và xuất TXT.
- Menu tài khoản và trang cá nhân có liên kết dành riêng cho vai trò phản biện. Đăng nhập hỗ trợ quay về `reviewer.html` bằng allowlist, không nhận URL chuyển hướng tùy ý.

## Hành vi

Phiên online gọi `/auth/profile` để xác minh vai trò trước khi tải công việc. Có vai trò `Chuyên gia phản biện` mới phù hợp role hiện tại của API; frontend cũng nhận các tên tương đương để hỗ trợ dữ liệu cũ. Người chỉ có vai trò biên tập/admin không tự động có quyền reviewer. Máy chủ quyết định quyền cuối cùng.

Danh sách API rỗng hiển thị rỗng. API 400/401/403/500 hoặc lỗi mạng không tạo dữ liệu demo. Phiên `standalone_token_` hiện hữu được nhận diện là demo, có nhãn trên trang và nút chủ động nạp mẫu. Lưu demo theo tài khoản trong khóa `huit-reviewer:demo:<identity>:assignments`, không đọc kho mẫu dùng chung cũ. Chấp nhận/từ chối lời mời hiện chỉ mô phỏng ở demo.

“Lịch sử đánh giá” là các bài chính reviewer đã chấm, không phải phiếu của reviewer khác. Có thể xem các vòng khác của chính mình trên cùng bài. Backend hiện trả điểm và kiến nghị; nội dung đầy đủ cần endpoint bổ sung bên dưới. Không tự tạo nhận xét cho hồ sơ thật.

BM-04 sử dụng đúng DTO hiện có: bốn điểm thành phần, điểm trung bình và hai vùng nhận xét, cùng bốn kiến nghị theo SQL. Không tự thêm “6 tiêu chí” khi Backend chưa có trường tương ứng. Phiếu không điền sẵn điểm hoặc kiến nghị. Sau khi server xác nhận, giao diện khóa sửa phiếu đã gửi và đưa vào lịch sử.

Nháp lưu trong sessionStorage theo chế độ, tài khoản, phân công và vòng. Nháp khôi phục khi đóng/mở dialog hoặc tải lại tab; không đồng bộ thiết bị và không phải phiếu đã gửi. Đăng xuất/xóa phiên ở tab khác xóa nháp. Không lưu toàn bộ phiếu online lâu dài trong localStorage. Sau khi gửi chỉ lưu bản ghi trong phiên để người dùng xem lại; danh sách phân công vẫn lấy từ server.

Lịch ICS chỉ chứa mã bài/vòng và thời hạn, không đưa tiêu đề hoặc nhận xét bí mật vào lịch cá nhân. TXT không chứa nhận xét riêng cho Ban biên tập. Dữ liệu người dùng hiển thị bằng DOM `textContent`, không ghép HTML/inline onclick.

## API đã có và đang sử dụng

| Phương thức | Đường dẫn dưới `/api` | Ghi chú |
|---|---|---|
| GET | `/auth/profile` | Trả `maNguoiDung`, `hoTen`, `vaiTros` từ token hợp lệ |
| GET | `/phanbien/my-assignments` | Chỉ công việc của reviewer đang đăng nhập; trả mảng, rỗng là `[]` |
| POST | `/phanbien/evaluate` | JSON theo `PhieuDanhGiaDto`; thành công phải có `{ "success": true }` |

Các endpoint này đã tồn tại trong mã C# khi rà soát. Bộ kiểm thử mới mô phỏng phản hồi để kiểm tra frontend; chưa chứng minh kết nối với database thật.

## Hai endpoint cần thành viên Backend bổ sung

### GET `/api/phanbien/assignments/{maPhanCong}/evaluation`

Chỉ reviewer sở hữu phân công được xem phiếu của chính mình. Trả 401 khi chưa đăng nhập, 403/404 khi không có quyền, 404 khi chưa có phiếu. JSON đề xuất:

```json
{
  "maPhanCong": 123,
  "diemTinhMoi": 8,
  "diemPhuongPhap": 7,
  "diemKetQua": 8,
  "diemTrinhBay": 7,
  "diemTongKet": 7.5,
  "nhanXetChoTacGia": "Nội dung nhận xét",
  "nhanXetBaoMat": "Chỉ Ban biên tập và người viết phiếu xem",
  "kienNghi": "Chỉnh sửa nhỏ"
}
```

Frontend kiểm tra `maPhanCong` trả về khớp công việc. Nếu endpoint chưa có, chỉ hiển thị tóm tắt và thông báo thiếu nội dung, không giả lập phiếu online.

### GET `/api/phanbien/assignments/{maPhanCong}/manuscript`

Kiểm tra token, vai trò, quyền sở hữu phân công và điều kiện được tải. Trả tệp ẩn danh đúng vòng dưới dạng PDF/DOCX/DOC, với `Content-Type` đúng và `Cache-Control: private, no-store`. Cần làm sạch metadata nhận diện tác giả từ tệp. Không trả đường dẫn tùy ý để frontend tải bằng token và không redirect sang máy chủ khác.

Trang gửi Bearer token, kiểm tra MIME phản hồi và tải blob; không tự lấy bản thảo từ endpoint chi tiết của tác giả. Chưa có endpoint thì báo chưa khả dụng. Demo không cung cấp PDF mẫu giả dưới tên bản thảo thật.

## Các điều kiện Backend nên bảo đảm

1. POST đánh giá kiểm tra quyền sở hữu, trạng thái/vòng hợp lệ, tài khoản còn hoạt động và không cho sửa phiếu đã khóa.
2. Chống nộp trùng, kiểm soát cập nhật đồng thời và timeout. Frontend ngăn bấm hai lần nhưng không thay thế idempotency phía server.
3. Tính/kiểm tra điểm tổng phía server; không tin điểm tổng từ client.
4. Không trả tên tác giả cho reviewer hoặc tên reviewer cho tác giả qua file, metadata, lịch sử hay nhận xét.
5. Nếu mở rộng lời mời online, thống nhất endpoint chấp nhận/từ chối/gia hạn và cập nhật contract; hiện UI không tuyên bố những thao tác này đã chạy thật.

## Đọc lại bản tổng hợp cập nhật

Đã đối chiếu nội dung `Web/journal-interactions.js`, `Web/profile.html`, `Web/journal-layout.js` trong TXT cập nhật với tệp thực tế: khớp tại thời điểm thực hiện. TXT là ảnh chụp nguồn trước thay đổi này; source Web sau nâng cấp là bản chạy mới.

Một số nhận định trong báo cáo cập nhật cần điều chỉnh: `btoa(...)` có tiền tố `bcr_sha_` là Base64 đảo ngược được, không phải hash mật khẩu; chức năng tải cũ trỏ đến một PDF mẫu; nhánh lấy công việc cũ có thể trả mẫu khi API lỗi/rỗng. Trang mới xử lý đúng các giới hạn của module reviewer, nhưng không tuyên bố đã sửa hết bảo mật/xác thực chung của hệ thống. Tích hợp API và kiểm thử dữ liệu thật cần phối hợp thành viên Backend.

## Kiểm thử

Chạy kiểm tra cú pháp:

```powershell
node --check reviewer.js
node --check reviewer-core.js
node --check journal-interactions.js
```

Bộ kiểm thử `tests/test_reviewer_workspace.cjs` dùng Node và Playwright, Edge headless hoặc Chromium. Cài Playwright trong môi trường phát triển nếu chưa có rồi chạy `node tests/test_reviewer_workspace.cjs`. Có thể đặt `PLAYWRIGHT_MODULE` là đường dẫn module Playwright có sẵn và `REVIEWER_ARTIFACT_DIR` cho ảnh/log. Test tự chạy static server tạm, mock API, không ghi vào DB thật.

Kiểm tra tiếp với nhóm: tạo tài khoản reviewer thật → phân công từ WinForms → mở Web và thấy bài → nộp phiếu → kiểm tra WinForms nhận được → tải lại Web xem lịch sử; sau khi bổ sung endpoint, kiểm tra tải đúng bản ẩn danh và xem phiếu cũ trên thiết bị khác.
