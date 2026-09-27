# Kết nối WinForms, Backend API và Web

## Cách chạy

1. Khởi động SQL Server và CSDL `QL_TapChiKhoaHoc`.
2. Chạy `Chay_Backend.bat`. API mặc định ở `http://localhost:5000`.
3. Mở WinForms và đăng nhập bằng tài khoản Ban biên tập hoặc Quản trị hệ thống. WinForms gọi `/api/auth/login`, nhận JWT và giữ token trong bộ nhớ cho phiên hiện tại.
4. Chạy `Chay_Web.bat` nếu cần mở Web riêng tại `http://localhost:8088`. Web và WinForms cùng gọi một Backend API, Backend và các màn đọc SQL của WinForms phải trỏ cùng CSDL.

Nếu Backend ở địa chỉ khác, đặt `HUIT_JOURNAL_API_URL` trước khi mở WinForms. Nếu WinForms đọc CSDL khác máy/chủng loại SQL mặc định, đặt `HUIT_JOURNAL_DB_CONNECTION` bằng đúng chuỗi kết nối mà Backend dùng (`ConnectionStrings__DefaultConnection`). Khi đã đặt `HUIT_JOURNAL_DB_CONNECTION`, WinForms chỉ thử chuỗi đó và không tự chuyển sang CSDL khác.

## Luồng đã đi qua API

| WinForms | Backend API | Web thấy kết quả |
| --- | --- | --- |
| Đăng nhập, đổi mật khẩu | `/api/auth/login`, `/api/auth/change-password` | Cùng tài khoản và vai trò |
| Phân công phản biện | `/api/phanbien/assign` | Sau đủ hai chuyên gia cùng vòng, bài chuyển `Đang phản biện` |
| Gia hạn/hủy phân công, nộp BM-04 | `/api/editorial-articles/assignments/...`, `/api/phanbien/evaluate` | Chỉ chuyên gia được phân công và có vai trò phù hợp mới nộp phiếu |
| Quyết định biên tập | `/api/phanbien/decision` | Trạng thái và lịch sử đúng các quy tắc nghiệp vụ |
| Tạo/sửa số nháp, xếp bài | `/api/sotapchi/draft/save`, `/api/baibao/{id}/assign-issue` | Chưa công khai cho đến khi phát hành |
| Sửa trang/DOI, gỡ bài khỏi số | `/api/editorial-articles/{id}/pages`, `/api/editorial-articles/{id}/unassign` | Bị chặn sau khi số đã phát hành |
| Phát hành số | `/api/sotapchi/{id}/publish` | Số, bài và PDF thành phẩm xuất hiện công khai sau khi mọi bài đều sẵn sàng |
| Lưu/khóa người dùng | `/api/desktop-admin/users/...` | Tài khoản mới được băm mật khẩu và phân quyền dùng chung |

API phát hành kiểm tra số có bài, từng bài ở trạng thái `Sẵn sàng xuất bản`, có khoảng trang hợp lệ và tệp PDF thành phẩm. Tất cả thay đổi trạng thái của số và bài được lưu trong cùng giao dịch. Màn sửa số không thể tự đặt `Đã xuất bản`; số đã phát hành không thể được thêm bài hoặc sửa lại như bản nháp.

## Phần còn dùng SQL trực tiếp

Các màn cũ vẫn đọc danh sách, thống kê và một số dữ liệu chi tiết từ SQL. Một số thao tác biên tập phụ (chuyên ngành, tạo/sửa/xóa bài trực tiếp, sao lưu) vẫn dùng SQL trực tiếp. Trước khi triển khai ra thị trường, cần chuyển các thao tác này sang API có phân quyền và bỏ quyền ghi CSDL khỏi máy khách. BM-04 nay đi qua API và chỉ tài khoản của chuyên gia được phân công mới nộp được; chuyên gia hiện dùng giao diện Web để thực hiện.

Chức năng đặt lại mật khẩu quản trị cũ đã bị chặn vì chưa có quy trình xác minh đủ an toàn. Không dùng tài khoản/mật khẩu seed trong môi trường triển khai thật.

## Kiểm chứng

Build `Backend/HuitJournal.Api/HuitJournal.Api.csproj` và `Winform/QL_TapChi_WinForms/QL_TapChi_WinForms.csproj`. Bộ `Web/tests/test_live_backend_6_stages_integration.py` chạy trên API `Testing` và CSDL `QL_TapChiKhoaHoc_Test`; script xác minh đúng môi trường trước khi ghi dữ liệu, chạy luồng 6 giai đoạn, kiểm tra phát hành số qua API và dọn dữ liệu kiểm thử.
