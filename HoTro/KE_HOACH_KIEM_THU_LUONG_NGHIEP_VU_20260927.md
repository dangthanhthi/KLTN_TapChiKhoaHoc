# Quy trình kiểm thử luồng nghiệp vụ HUIT Journal

Ngày lập: 27/09/2026. Phạm vi: Web Portal, Backend API, SQL Server, WinForms và trang công khai. Đây là kế hoạch kiểm thử và biên bản kết quả ban đầu, **không phải chứng nhận hệ thống đã sẵn sàng bán ra thị trường**.

## 1. Phạm vi và tiêu chí kết luận

Năm vai trò: khách/độc giả, tác giả, chuyên gia phản biện, ban biên tập, quản trị hệ thống. Một tài khoản có thể mang nhiều vai trò; mọi quyền phải được kiểm tra ở API, không chỉ ẩn nút trên Web/WinForms.

Sáu giai đoạn cần đi xuyên suốt: (1) đăng ký, xác nhận email và nộp bản thảo; (2) sơ duyệt hình thức; (3) tạo bản ẩn danh và mời ít nhất hai phản biện; (4) nhận lời, thẩm định và nộp BM-04; (5) quyết định, tác giả nộp bản sửa/BM-03 và phản biện lại nếu cần; (6) chấp nhận, chế bản, xếp số, phát hành và công bố PDF. Nhánh từ chối, rút bài, phản biện từ chối và sửa nhiều vòng cũng là luồng chính cần kiểm thử.

P0: mất dữ liệu, lộ danh tính/tệp riêng, sai quyền, sai trạng thái, công bố sai hoặc không thể hoàn tất luồng. P1: sai nội dung/thống kê/thông báo, lỗi phục hồi, sai đồng bộ Web–WinForms. P2: hiển thị và tiện dụng. Một ca đạt khi có bằng chứng **API + CSDL + giao diện** phù hợp; riêng ca tích hợp WinForms cần chứng cứ trên hai máy hoặc hai phiên độc lập.

## 2. Môi trường và dữ liệu thử

1. Chỉ dùng API cục bộ `http://127.0.0.1:5001/api` với `ASPNETCORE_ENVIRONMENT=Testing`. Trước mọi lệnh POST/PUT/DELETE, `GET /api/testing/environment` phải trả chính xác `Testing`, `QL_TapChiKhoaHoc_Test`, `dataSource="."`. Khi một giá trị khác, dừng ngay. Không chạy E2E có ghi dữ liệu trên Vercel/MonsterASP hoặc CSDL chính.
2. Tắt `EmailVerification__EnableBackgroundDispatcher` khi chạy nhóm kiểm thử API không kiểm tra email. Nhóm OTP dùng outbox mô phỏng trong Testing; tuyệt đối không gửi mã tới địa chỉ thật. Chỉ bài thử SMTP riêng trên staging mới dùng hộp thư thử do nhóm kiểm soát.
3. Lập run ID theo UTC, ví dụ `QA-20260927-001`, gắn vào email `@example.test`, tiêu đề bài, tên file và số nháp. Lưu manifest gồm ID đối tượng được tạo và đường dẫn file. Chụp số lượng/kiểu dữ liệu ban đầu, backup riêng CSDL thử trước khi chạy toàn bộ E2E.
4. Bộ fixture cần ít nhất 1 khách, 2 tác giả (trong đó 1 đồng tác giả), 3 chuyên gia cùng chuyên ngành, 1 chuyên gia khác chuyên ngành, 1 biên tập viên, 1 quản trị viên; 2 số nháp, 1 số đã phát hành; PDF hợp lệ, PDF giả, DOCX hợp lệ, tệp quá kích thước, tệp có tên độc hại, bản gốc có danh tính, bản ẩn danh, BM-03 và PDF thành phẩm.
5. Chỉ dọn những bản ghi có run ID và ID trong manifest; đối chiếu quan hệ trước khi xóa. Chỉ xóa file nằm dưới thư mục dành riêng cho run ID đã được kiểm tra bằng đường dẫn tuyệt đối. Nếu không thể xác định quyền sở hữu file, giữ lại và báo cáo. Không dùng cleanup rộng trong hai script E2E cũ.

## 3. Ma trận ca kiểm thử

Ký hiệu lớp: U = unit/service, A = API + SQL Testing, B = browser thật, D = WinForms, M = thao tác tay có ghi bằng chứng. Những ca ghi dữ liệu chỉ chạy sau cổng an toàn ở mục 2.

| ID | P | Lớp | Đầu vào / hành động | Kết quả bắt buộc |
|---|---|---|---|---|
| ENV-01 | P0 | A | API Testing và DB đúng danh tính | Cho phép chạy; ghi lại ba trường môi trường |
| ENV-02 | P0 | A | API Production/DB khác/URL không phải loopback | Dừng trước request ghi đầu tiên |
| ENV-03 | P0 | A | Chạy lại cùng fixture sau cleanup | Không còn bản ghi/file thử; dữ liệu nền không đổi |
| AU-01 | P0 | A,B | Đăng ký đủ trường, email chưa dùng | Hồ sơ chờ, chưa tạo quyền đăng nhập; outbox có 1 email mã |
| AU-02 | P0 | A | Email/tên đăng nhập trùng, rỗng, Unicode, quá dài | 400 rõ trường lỗi, không tạo hồ sơ dư |
| AU-03 | P0 | A | OTP đúng 6 số trước hạn | Kích hoạt đúng một tài khoản; đăng nhập được |
| AU-04 | P0 | A | OTP sai 5 lần, mã hết hạn, mã đã dùng | Bị chặn; không tạo tài khoản; không lộ mã |
| AU-05 | P0 | A | Gửi lại mã trước cooldown, vượt hạn giờ/ngày | Rate limit đúng; mã cũ hết giá trị nếu chính sách yêu cầu |
| AU-06 | P0 | A | Hai request xác nhận mã đồng thời | Tối đa một lần kích hoạt; không sinh tài khoản trùng |
| AU-07 | P0 | A,B | Sai mật khẩu, tài khoản khóa, JWT hết hạn, logout | Không truy cập dữ liệu riêng; thông báo đúng |
| AU-08 | P1 | A,B | Đổi mật khẩu, cập nhật hồ sơ, avatar, xóa avatar | DB và Web đồng nhất; token cũ xử lý theo chính sách |
| AU-09 | P0 | A,B,D | Một người có nhiều vai trò | Quyền theo role thực, không lấy từ học vị/nhãn UI |
| AU-10 | P0 | A,B,D | Tác giả thử tự xin vai trò phản biện; Tổng biên tập cấp/thu hồi vai trò qua WinForms | Web/API tự phục vụ bị chặn; chỉ tài khoản quản trị có quyền cấp vai trò |
| SUB-01 | P0 | A,B | Tác giả gửi bản thảo đủ metadata/đồng tác giả/PDF | Tạo 1 bài `Chờ sơ duyệt`, file và lịch sử đầy đủ |
| SUB-02 | P0 | A | Thiếu tiêu đề, tóm tắt, chuyên ngành, file | 400; không có bài/file mồ côi |
| SUB-03 | P0 | A | `.exe`, PDF giả/thiếu EOF/chứa hành động nguy hiểm | Từ chối; không lưu file |
| SUB-04 | P0 | A | Tệp quá lớn, tên path traversal, upload đứt mạng | Từ chối an toàn; không ghi dở |
| SUB-05 | P0 | A | Hai lần bấm Nộp hoặc gửi đồng thời | Không sinh hai bài ngoài ý định; UI chống gửi lặp |
| SUB-06 | P0 | A | Tác giả khác/khách xem chi tiết hoặc tải bản gốc | 401/403/404 theo hợp đồng; không trả dữ liệu |
| SUB-07 | P0 | A | Đồng tác giả đã liên kết xem bài; email giả mạo | Chỉ tài khoản đã xác thực/khớp quyền được xem |
| PRE-01 | P0 | A,D | Biên tập sơ duyệt: đạt / yêu cầu sửa thể thức / từ chối | Trạng thái, lịch sử, thông báo tác giả đúng |
| PRE-02 | P0 | A | Tác giả/chuyên gia tự đổi trạng thái qua API | 403; DB không đổi |
| PRE-03 | P1 | A,B,D | Ghi chú nội bộ và thông báo tác giả | Tác giả chỉ thấy phần được phép, không thấy tên người phản biện |
| REV-01 | P0 | A,D | Chưa có bản ẩn danh đúng vòng mà mời chuyên gia | Bị chặn |
| REV-02 | P0 | A | Phân công khác chuyên ngành, tác giả/đồng tác giả, tài khoản khóa | Bị chặn ở API/SQL |
| REV-03 | P0 | A | Mời cùng chuyên gia hai lần cùng vòng | Bị chặn; không có phân công trùng |
| REV-04 | P0 | A,B | Hai lời mời chờ phản hồi | Trạng thái hiển thị chính xác; chưa được BM-04/tải file trước nhận lời |
| REV-05 | P0 | A,B | Nhận lời một lần rồi nhận/từ chối lại | Lần đầu ghi nhận, lần sau bị chặn |
| REV-06 | P0 | A,B | Từ chối lời mời, mời người thay thế | Chỉ lời mời hoạt động tính vào ngưỡng; không bế tắc quy trình |
| REV-07 | P0 | A | Hạn phản hồi qua ngày, hạn hoàn thành qua ngày | Áp dụng chính sách hạn rõ ràng; biên tập biết việc quá hạn |
| REV-08 | P0 | A,B | Chuyên gia tải bản thảo vòng N | Chỉ bản ẩn danh đúng vòng; không trả bản gốc/BM-03/tác giả |
| REV-09 | P0 | A | Chuyên gia A tải phân công B; khách đoán URL | Từ chối, không lộ tên file hoặc đường dẫn vật lý |
| BM4-01 | P0 | A,B | Điểm 0, 10, thập phân hợp lệ; nhận xét/kiến nghị đủ | API tự tính tổng, lưu đúng 1 phiếu |
| BM4-02 | P0 | A | Điểm thiếu/âm/>10, kiến nghị lạ, nhận xét rỗng/quá dài | 400; không có phiếu |
| BM4-03 | P0 | A,B | Chưa nhận lời, đã từ chối, sai vòng, bài đã kết thúc | Không nộp BM-04 |
| BM4-04 | P0 | A | Nộp lại phiếu hoặc hai POST cùng lúc | Không ghi đè và không tạo hai phiếu |
| BM4-05 | P0 | A,B,D | Phiếu bí mật của phản biện A | Chỉ A và biên tập được xem; tác giả không thấy nhận xét mật |
| DEC-01 | P0 | A,D | Quyết định khi 0/2 và 1/2 BM-04 | Bị chặn; DB giữ trạng thái cũ |
| DEC-02 | P0 | A,D | Đủ tối thiểu 2 BM-04 cùng vòng | Cho quyết định hợp lệ, lưu người thực hiện và thông báo |
| DEC-03 | P0 | A | Thử nhảy trạng thái, lặp quyết định, quyết định từ vai trò sai | Bị chặn; lịch sử không sai |
| DEC-04 | P1 | A | Hai biên tập viên quyết định đồng thời | Chỉ một quyết định hợp lệ theo phiên bản/trạng thái |
| DEC-05 | P0 | A | Bài đã công bố bị chuyển `Từ chối` | Phải có chính sách đính chính/rút bài công khai; không âm thầm biến mất |
| BM3-01 | P0 | A,B | `Chờ chỉnh sửa`: tác giả chính nộp BM-03 và bản sạch/track changes | Tăng đúng vòng; lưu đủ file và lịch sử |
| BM3-02 | P0 | A | Người khác nộp thay, thiếu BM-03/file, bài sai trạng thái | Bị chặn; không tạo file dở |
| BM3-03 | P0 | A,D | Vòng 2 tạo bản ẩn danh mới rồi mời phản biện | Không trả bản vòng 1; yêu cầu đủ BM-04 vòng hiện hành |
| PUB-01 | P0 | A,D | Chấp nhận nhưng chưa xếp số/chưa PDF/chưa trang | Không công bố |
| PUB-02 | P0 | A,D | Trang bắt đầu/kết thúc đảo, chồng trang, DOI trùng | Bị chặn hoặc có quy trình phê duyệt rõ |
| PUB-03 | P0 | A,D | Xếp bài, tải PDF thành phẩm, phát hành số | Bài và số đồng bộ `Đã xuất bản`; lịch sử đầy đủ |
| PUB-04 | P0 | A,B | Khách xem số/bài/PDF sau phát hành | API 200; nội dung PDF thật, đúng bài và số |
| PUB-05 | P0 | A,B | Bài/số nháp trước phát hành | Không xuất hiện trên trang chủ/kho lưu trữ/search; PDF ẩn |
| PUB-06 | P1 | A,B | Đếm số bài dropdown, kho lưu trữ, chi tiết số | Cùng một tập bài công khai, số đếm khớp |
| PUB-07 | P1 | A,B | Tệp PDF thành phẩm bị thiếu trên disk | Không quảng bá link chết; báo lỗi vận hành có thể xử lý |
| PUB-08 | P1 | A,B | DOI, tác giả, ORCID, ngày, số trang, trích dẫn | Metadata đúng nguồn và nhất quán giữa API/Web/PDF |
| PUB-09 | P1 | A | Rút bài sau công bố/đính chính | Dùng trạng thái và trang thông báo công khai, giữ audit trail |
| SYNC-01 | P0 | A,B,D | WinForms phân công; Web chuyên gia làm mới | Lời mời xuất hiện đúng người và vòng |
| SYNC-02 | P0 | A,B,D | Web nhận/từ chối và gửi BM-04; WinForms làm mới | Trạng thái/điểm/phiếu giống API, không cần SQL trực tiếp |
| SYNC-03 | P0 | A,B,D | WinForms quyết định/xếp số/phát hành; Web làm mới | Tác giả và trang công khai cập nhật đúng |
| SYNC-04 | P0 | D | WinForms trên máy khác qua HTTPS, mất mạng rồi kết nối lại | Báo lỗi thật, không ghi cục bộ giả; tải lại từ API |
| SYNC-05 | P0 | D | WinForms cấu hình nhầm URL API/DB backup khác | Từ chối backup/restore sai môi trường; nghiệp vụ thường vẫn qua API |
| SYNC-06 | P1 | A,B,D | Hai phiên Web/WinForms sửa cùng đối tượng | Không mất cập nhật; có cảnh báo xung đột hoặc kiểm tra phiên bản |
| SEC-01 | P0 | A,B | JWT sai/hết hạn, IDOR mọi ID tài nguyên | 401/403/404; không lộ dữ liệu qua body/headers |
| SEC-02 | P0 | A | XSS trong tiêu đề, nhận xét, tên file; SQL injection | Hiển thị dạng text, truy vấn tham số hóa; không thực thi |
| SEC-03 | P0 | A,B | CORS từ origin lạ, HTTPS, cookie/token storage | Chỉ origin cho phép; không truyền token qua URL |
| SEC-04 | P1 | A | Rate limit đăng nhập/upload/OTP, request size, log | Không spam; log không chứa mật khẩu, OTP, JWT |
| RES-01 | P0 | B,D | API 503, timeout, mất internet lúc gửi POST | Không báo thành công giả; có cách kiểm tra đã ghi hay chưa trước khi thử lại |
| RES-02 | P1 | A,D | Backup/restore thử nghiệm có đối chiếu server + DB | Restore đúng dữ liệu và file; không thay DB chính |
| UI-01 | P1 | B | 320/375/768/1024/1366px, zoom 200%, bàn phím | Không vỡ form; dialog/validation rõ; không lỗi console |
| UI-02 | P2 | B | Ngôn ngữ, ngày/giờ, thứ tự tab, nhãn đọc màn hình | Dễ hiểu và dùng được trên bàn phím/thiết bị nhỏ |

## 4. Trình tự thực hiện chuẩn

**Đợt A — cổng an toàn và kiểm tra không ghi:** build Backend/WinForms, unit/service, browser với API mô phỏng, `tests/test_business_workflow_readonly.cjs`; lưu phiên bản commit, cấu hình không bí mật và báo cáo lỗi. Dừng nếu target khác Testing.

**Đợt B — tài khoản và nộp bài:** chạy AU và SUB bằng fixture riêng, kiểm tra SQL sau mỗi bước và ảnh giao diện ở 375px + 1366px. Mọi ca POST lỗi phải xác minh **không** tạo bản ghi hoặc file.

**Đợt C — biên tập và phản biện:** PRE → REV → BM4 → DEC → BM3, cả nhánh nhận lời/từ chối, 0/1/2 phiếu và nhiều vòng. Kiểm tra double-blind bằng phản hồi API và nội dung tệp, không chỉ nhìn UI.

**Đợt D — công bố:** PUB trên số nháp riêng, so đếm API/Web, thử PDF thật, thử link hỏng, chứng cứ truy cập của khách. Không phát hành trên dữ liệu công khai đang dùng.

**Đợt E — WinForms và độ bền:** cùng API Testing trên máy thứ hai hoặc VM, chạy SYNC và RES; so trạng thái trước/sau làm mới, mất mạng, yêu cầu đồng thời. Kiểm tra backup/restore trên bản sao riêng.

**Đợt F — hồi quy và nghiệm thu:** chạy lại P0/P1 sau sửa, lưu biên bản ID–kết quả–HTTP–ảnh/log–ID bản ghi–người kiểm, cleanup theo manifest và đối chiếu baseline. Chỉ đề nghị đưa lên staging khi 100% P0 đạt, không còn lỗi mất dữ liệu/lộ thông tin và toàn luồng Web–API–SQL–WinForms–public đạt trên môi trường riêng.

## 5. Kết quả đã kiểm hôm nay

| Nhóm | Kết quả | Giới hạn |
|---|---|---|
| Build Backend .NET 9 | PASS, 0 warning/0 error | Chỉ xác nhận biên dịch |
| Build WinForms .NET 8 | PASS, 0 warning/0 error | Chưa chạy UI trên máy thứ hai |
| Browser reviewer qua API mô phỏng | PASS 15/15 | Chưa chứng minh SQL và phân quyền backend thật |
| API mất kết nối và profile 503 | PASS | API mô phỏng |
| API Testing + SQL thật, không ghi | PASS 8/8: 23 số, 230 liên kết bài và 230 PDF tải được, 5 endpoint riêng tư 401 | Chưa kiểm E2E có ghi |
| Đối chiếu số liệu SQL Testing | PHÁT HIỆN: 240 bài/224 người dùng; 235 bài `Đã xuất bản`, trong đó 5 bài thiếu bản ghi PDF thành phẩm | 5 bài ID 1–5 không xuất hiện trong 230 bài công khai; cần phân loại dữ liệu mẫu hay bài thật trước khi xử lý |
| Bất biến phản biện và quyết định với SQL Testing | PASS 27/27: nhận/từ chối, mời thay thế, hai người nhận đồng thời, BM-04, quyết định 0/1/2 phiếu, BM-03 thiếu/hợp lệ, lịch sử ẩn danh, chặn từ chối bài đã công bố; fixture QA và tệp đã dọn | Chưa thay thế E2E từ OTP đến phát hành |
| Tự xin vai trò phản biện | PASS: `POST /api/auth/request-reviewer` trả 404 trên API Testing | Cấp/thu hồi vai trò từ WinForms quản trị cần kiểm thử trên giao diện |
| E2E 6 giai đoạn có ghi | CHƯA CHẠY | Script cũ không còn đúng luồng OTP/nhận lời, cleanup nguy hiểm |
| WinForms ↔ Web ↔ SQL cùng lúc | CHƯA CHẠY | Cần fixture và phiên WinForms riêng |
| Production Vercel/MonsterASP | CHƯA CHẠY | Người dùng đang hoãn triển khai, không ghi dữ liệu production |

## 6. Rủi ro và cải tiến ưu tiên từ rà soát mã

1. **P0 — bộ E2E cũ không còn an toàn để chạy nguyên trạng.** `Web/tests/test_live_backend_6_stages_integration.py` chờ token ngay sau đăng ký và cho phản biện tải/nộp BM-04 trước khi nhận lời; `tests/test_e2e_publishing_lifecycle.py` cũng thiếu bước nhận lời. Các script có cleanup xóa tệp và bản ghi bằng ID; script cũ còn giả định baseline đúng 10 bài/10 người, trong khi DB Testing hiện chứa dữ liệu phát hành lịch sử. Cả hai đã được chặn ở lệnh chạy trực tiếp; cần viết lại runner có run manifest, cleanup chỉ đối tượng của run và cập nhật OTP/nhận lời.
2. **P0 — chuyển `Đã xuất bản` sang `Từ chối`: đã chặn cục bộ và kiểm thử SQL thật.** Bài công bố hiện từ chối mọi quyết định trạng thái thông thường. Quy trình đính chính/rút bài công khai, giữ DOI và audit trail vẫn cần thiết kế trước khi bán.
3. **P1 — bắt đầu phản biện sau hai lời mời: đã sửa cục bộ.** Bài chỉ chuyển `Đang phản biện` khi hai chuyên gia cùng vòng đã nhận lời. Hai phản hồi đồng thời được tuần tự hóa theo bài; ca SQL thật đã đạt. Hàng đợi thay thế người từ chối/quá hạn trên WinForms vẫn cần kiểm tra tiện dụng.
4. **P1 — chính sách hạn phản hồi/hạn BM-04 chưa được thực thi trong hai API nhận lời/nộp phiếu.** Cần quyết định cho phép nộp muộn có lý do hay tự khóa; nếu khóa phải có cơ chế biên tập gia hạn và ghi vết.
5. **P1 — trang công khai hiện dựa trên bản ghi PDF, không xác minh tệp vật lý khi lập danh sách.** Trong lần kiểm này cả 230/230 PDF của kho Testing tải được và có chữ ký `%PDF-`. Nên thêm giám sát định kỳ để phát hiện link chết sau khi triển khai hoặc di chuyển tệp.
6. **P1 — đồng thời/idempotency mới được xác minh cho hai lời mời nhận cùng lúc.** Cần tiếp tục AU-06, SUB-05, BM4-04, DEC-04 và SYNC-06; cân nhắc khóa lạc quan/unique constraint theo từng quy tắc.
7. **P1 — dữ liệu Testing có 5 bài đánh dấu `Đã xuất bản` nhưng thiếu bản ghi PDF thành phẩm (ID 1–5).** API hiện loại chúng khỏi kho công khai nên số 230/235 là nhất quán theo quy tắc hiển thị. Cần xác định đó là seed/demo hay bài thật, rồi sửa trạng thái hoặc bổ sung PDF hợp lệ; chưa xóa/sửa trong đợt kiểm thử này.
8. **P1 — EF Core cảnh báo truy vấn nạp nhiều collection trong cùng một câu SQL khi đọc dữ liệu công khai.** Hiện 23 số phản hồi được, nhưng khi kho lớn hơn có nguy cơ chậm do nhân bản hàng. Đo thời gian/tổng số truy vấn với số báo lớn; cân nhắc `AsSplitQuery()` hoặc projection gọn thay vì nhiều `Include`.
9. **P0 — tự xin vai trò phản biện trên Web trái quy tắc dự án: đã đóng cục bộ.** Route, phương thức service và DTO tự đăng ký đã bị loại bỏ. Các hồ sơ cũ chỉ quản trị viên được duyệt/từ chối; cần kiểm thử màn cấp vai trò WinForms với tài khoản Tổng biên tập.
10. **P0 — nộp lại khi thiếu bản sửa/BM-03 trước đây vẫn chuyển bài sang `Chờ quyết định`: đã sửa cục bộ.** DTO và service đều yêu cầu giải trình, tệp BM-03 và bản thảo sạch; bài thử SQL xác nhận yêu cầu thiếu không đổi trạng thái/file, yêu cầu đủ tạo hai tệp vòng 2 và chuyển trạng thái đúng.

## 7. Lệnh chạy phần đã an toàn

Mở API Testing ở cổng 5001 với dispatcher tắt. Trước khi chạy, tự xác nhận `/api/testing/environment`; bộ `test_business_workflow_readonly.cjs` cũng dừng nếu danh tính sai. Sau đó chạy:

```powershell
node tests/test_business_workflow_readonly.cjs
node Web/tests/test_reviewer_workspace.cjs
node Web/tests/test_online_api_fail_closed.cjs
node Web/tests/test_online_profile_error_playwright.cjs
dotnet build Backend/HuitJournal.Api/HuitJournal.Api.csproj --no-restore
dotnet build Winform/QL_TapChi_WinForms/QL_TapChi_WinForms.csproj --no-restore
```

Các lệnh trên không gửi email thật, không tạo bài và không xóa dữ liệu. Không chạy hai script E2E có ghi cho đến khi sửa các điểm ở mục 6. Bằng chứng tổng kết phải ghi rõ ngày, nhánh/commit, tên DB, số ca PASS/FAIL/SKIP và lý do SKIP.

Ca tích hợp có ghi riêng tại `tests/WorkflowInvariantTests/` tự xác minh `QL_TapChiKhoaHoc_Test/.`, tạo bài mang mã `QA-REVIEW-*`, dọn chỉ bài đó và đối chiếu số lượng trước/sau. Trên máy hiện có runtime .NET 10 (không có runtime 9), chạy với `$env:DOTNET_ROLL_FORWARD='Major'` trước lệnh `dotnet run --project tests/WorkflowInvariantTests/WorkflowInvariantTests.csproj --no-build --no-restore`. Chỉ chạy khi SQL Testing cục bộ sẵn sàng; không đổi URL sang DB chính.
