# Kết quả cải tiến phân hệ Web — 24/09/2026

Đã đọc lại TXT cập nhật trong thư mục khóa luận và đối chiếu các tệp Web liên quan. Phạm vi công việc: Đặng Thành Thi phụ trách lập trình Web; phần Backend, SQL và WinForms thuộc phối hợp nhóm.

Đã cài trực tiếp 10 tệp vào phân hệ Web của dự án trên Desktop, kiểm tra SHA256 toàn bộ tệp khớp bản đã kiểm thử. Các tệp cũ được sao lưu trong thư mục làm việc trước khi cập nhật.

## Trang mới

Truy cập **Bàn làm việc phản biện** từ menu tài khoản hoặc trang cá nhân sau khi đăng nhập bằng tài khoản có vai trò phản biện. URL trang: `reviewer.html` trong cùng thư mục Web.

Chức năng:

1. Danh sách công việc được phân công, tìm kiếm theo tiêu đề/mã/chuyên ngành, lọc hạn và sắp xếp, phân trang.
2. Tổng số đang xử lý, sắp đến hạn, quá hạn và đã đánh giá, tính từ dữ liệu đang tải.
3. Lịch sử đánh giá của chính mình, liên kết các vòng trên cùng bài, hiển thị điểm/kiến nghị và nội dung phiếu khi có.
4. Phiếu BM-04 theo DTO thật: bốn điểm thành phần, trung bình, nhận xét gửi tác giả, nhận xét riêng cho biên tập và kiến nghị.
5. Nháp riêng trong phiên theo tài khoản/phân công/vòng; giữ nháp nếu server từ chối gửi.
6. Xuất hạn vào lịch ICS, xuất bản ghi TXT không kèm nhận xét bảo mật.
7. Phiên online kiểm tra quyền bằng API, không đổi lỗi/rỗng thành dữ liệu mẫu. Phiên demo được gắn nhãn, chủ động nạp và tách dữ liệu theo tài khoản.
8. Điều hướng dành cho reviewer; hỗ trợ trở về trang này sau đăng nhập, desktop/mobile và bàn phím.

## Giới hạn cần phối hợp Backend

API hiện có hỗ trợ danh sách và gửi đánh giá. Hai điểm kết nối mới đã được chuẩn bị phía Web nhưng **chưa có trong Backend hiện tại**: tải bản thảo ẩn danh theo phân công và đọc đầy đủ phiếu cũ. Khi chưa có endpoint, trang thông báo chưa khả dụng; lịch sử vẫn hiển thị điểm/kiến nghị có sẵn. Chấp nhận/từ chối lời mời hiện chỉ có ở demo.

Nháp không phải lưu trữ lâu dài: đóng tab/đăng xuất xóa nháp. Phân quyền cuối cùng phải được thực thi phía API. Không tuyên bố có đồng bộ offline hoặc đã hoàn thiện toàn bộ bảo mật của dự án.

## Công nghệ

Giữ HTML/CSS/JavaScript thuần, chia riêng module phản biện, không thêm framework runtime. Có thể thêm TypeScript/Vite sau khi cần mở rộng quản lý toàn bộ Web; không cần theo framework của WinForms. Chi tiết hợp đồng API và phạm vi kỹ thuật xem `REVIEWER_WEB_HANDOFF.md`.

## Xác minh

12 nhóm kiểm thử tự động đã đạt với Edge headless và API mô phỏng: quyền truy cập, lỗi API/danh sách rỗng, lọc/lịch sử, nháp/gửi thất bại/gửi thành công, dữ liệu chứa HTML, phân trang/tải tệp có token/lịch, hết hạn phiên, demo riêng tài khoản, responsive ở 320/375/768/1024/1366px, và điều hướng.

Đã kiểm tra cú pháp JavaScript và xem ảnh desktop/mobile. Đây là kiểm thử frontend với phản hồi mô phỏng, **chưa phải E2E với SQL Server và WinForms thật**. Ảnh giao diện đi kèm sử dụng dữ liệu kiểm thử.

## Những nhận định trong tài liệu cập nhật cần sửa

`passwordHash` được tạo bằng `btoa` là Base64 có thể giải mã, không phải băm mật khẩu. Hàm tải phản biện cũ trỏ một PDF mẫu; API lấy công việc cũ có thể fallback sang mẫu khi rỗng/lỗi. Vì vậy, các câu “an toàn tuyệt đối” hoặc “đã hoàn thiện toàn diện” cần được thay bằng phạm vi kiểm chứng cụ thể. Đợt này xử lý module reviewer mới, không thay đổi toàn bộ xác thực chung.

Gói `reviewer-web-upgrade.zip` chứa 10 tệp Web mới/thay đổi và kết quả kiểm thử để bàn giao. Tài liệu gốc DOCX và file tổng hợp TXT được giữ nguyên; TXT vẫn là ảnh chụp trước đợt thay đổi này.
