# Hoàn thiện và triển khai luồng Web/API — 01/10/2026

## Bản online

Web: https://kltn-tap-chi-khoa-hoc.vercel.app. API: https://huit-journal-api.runasp.net. API trả phiên bản `2026.10.01.workflows.1`; endpoint quản trị xác nhận môi trường Production, database db70314. Web đã triển khai qua GitHub/Vercel. API triển khai bằng gói delta `dist/monsterasp/api-workflows-20261001-final-delta.zip`, hosting báo Done, 4 files. Cấu hình IIS web.config đã lưu giới hạn request 125 MiB; API vẫn trả HTTP 200 sau cập nhật.

Trước migration đã tạo backup `db70314_custom_01.10.2026_639264643220945186.bak` (9 MB, Done). Migration `HoTro/Migration_20261001_CompleteWebWorkflows.sql` chỉ bổ sung JournalWorkflowRecord và JournalWorkflowFile, không thay thế dữ liệu bài báo hiện có. Không sửa WinForms trong đợt này.

## Các luồng đã hoàn thiện

| Luồng | Kết quả |
|---|---|
| Nộp bài | Bắt buộc bản thảo, kiểm tra đồng tác giả và JSON lồng nhau, lưu cam đoan/siêu dữ liệu, BM-02 và phụ lục riêng tư. Transaction và mã yêu cầu tránh lưu một phần hoặc tạo bài trùng khi gửi lại. |
| Nháp | Lưu văn bản và tệp lên server; khóa theo tài khoản; kiểm tra phiên bản tránh ghi đè nháp mới; khôi phục tệp và danh mục chuyên ngành. |
| Sơ duyệt | Lưu báo cáo trùng lặp và kết luận theo đúng bản thảo; yêu cầu sửa trả hồ sơ về tác giả và tạo thư thông báo. Hồ sơ nộp mới phải có kết quả đạt trước phân công. |
| Phản biện | Mời, nhận/từ chối, nhắc hạn, hết hạn, thay người chưa hoàn thành; giữ dấu vết phân công. Quyết định chỉ mở sau các phiếu BM-04 cần thiết. |
| Tác giả sửa bài | Nhận góp ý công khai; nộp bản sạch và BM-03 theo vòng; giữ lịch sử phiên bản. Sửa hình thức quay về sơ duyệt; sửa sau phản biện quay về quyết định. |
| Đồng tác giả | MaNguoiDung có thể null; liên kết email sau xác minh tài khoản, giữ snapshot. Chỉ xem bài có mình là đồng tác giả, vẫn có thể nộp bài mới của mình. Không thấy góp ý nội bộ hoặc danh tính người phản biện. |
| Rút hồ sơ | Tác giả gửi lý do; tòa soạn xét duyệt; kiểm tra lại điều kiện và gửi kết quả. |
| Bản bông | Tòa soạn gửi đúng PDF mới nhất; tác giả xác nhận hoặc yêu cầu sửa. PDF mới làm mất hiệu lực xác nhận cũ. |
| Phát hành | Chặn PDF thiếu, bông chưa duyệt, trang chồng lấn, phát hành sớm. Hỗ trợ hẹn lịch, thông báo tác giả và đồng tác giả liên kết, tránh phát hành trùng. |
| Sau công bố | Đính chính/rút bài bằng thông báo công khai có dấu vết, giữ hồ sơ gốc. |
| Liên hệ | Lưu yêu cầu thật; tòa soạn trả lời; chỉ báo thành công sau API xác nhận. |
| Quên mật khẩu | Mã 6 số, thời hạn/lượt thử, không dùng lại; mật khẩu hash và vô hiệu phiên đăng nhập trước khi khôi phục. |

Trang mới: `/editorial-workflow.html`, `/article-workflow.html?id=<mã bài>`, `/forgot-password.html`. Trang cá nhân và menu tài khoản có liên kết phù hợp. Admin và Tổng biên tập thấy các công cụ phát hành/quyết định cuối; biên tập viên không thấy phần quyền cuối.

## Bằng chứng kiểm thử

SQL Server local QL_TapChiKhoaHoc_Test: WorkflowInvariantTests **92/92**, bao gồm nhiều vòng tác giả–phản biện, liên kết đồng tác giả, rollback/idempotency, nháp, rút bài, khôi phục mật khẩu, sơ duyệt, bông và phát hành. Fixture được dọn; SMTP mô phỏng trong bộ này.

Giao diện Web với API mô phỏng: tác giả **21/21**, phản biện **20/20**, các biểu mẫu mới **28/28**. Biểu mẫu mới kiểm tra 320, 375, 414, 768 và 1024 px; không lỗi JavaScript. API Release build thành công. Console harness có cảnh báo NU1900 do không tải được dữ liệu kiểm tra lỗ hổng NuGet; không có lỗi biên dịch.

Production: admin đăng nhập qua giao diện, trang tòa soạn tải inbox thật. HTTP qua proxy Vercel kiểm tra admin/biên tập viên/phản biện/tác giả đều đăng nhập 200; inbox staff 200, tác giả/phản biện 403. Tổng biên tập đăng nhập và inbox 200, endpoint quản trị hệ thống 403. Nháp test tạo 200, tài khoản khác bị chặn 403, phiên bản cũ 409, xóa mềm 200. Không thay đổi trạng thái bài báo thật trong kiểm tra production này.

Production SMTP báo simulateDeliveryInDev=false, enableBackgroundDispatcher=true, chu kỳ 5 giây; outbox Pending=0, Sent=7, Failed=0 tại thời điểm kiểm tra. Đây là trạng thái server gửi thư, chưa phải bằng chứng đã nhận trong hộp thư. Chưa chạy một hồ sơ mới xuyên suốt tới công bố trên production.

Ảnh giao diện online: `scratch/workflow-online-20261001.jpg`. Chi tiết HTTP nháp/quyền: `scratch/workflow-production-check.json` (không token/mật khẩu).

## Giới hạn còn cần vận hành bên ngoài

MonsterASP Free có thể ngủ; tác vụ hẹn lịch và nhắc hạn xử lý khi ứng dụng hoạt động trở lại, không bảo đảm đúng từng phút khi app pool ngừng. DOI chỉ công khai là DOI thật sau khi đăng ký/xác thực; không tích hợp tự động Crossref hoặc Turnitin. Báo cáo trùng lặp hiện do tòa soạn tải lên. Bản tin tùy chọn chưa kết nối và giao diện nói rõ điều này.

Báo cáo này thay thế hiện trạng thiếu trong WORKFLOW_GAPS_20261001.md; báo cáo cũ giữ làm lịch sử chẩn đoán.
