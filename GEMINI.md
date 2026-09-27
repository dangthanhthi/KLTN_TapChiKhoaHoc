# Quy định hệ thống kỹ năng & Tiêu chuẩn vận hành mặc định (Agent Skills & Standards)

Dự án này áp dụng 4 kỹ năng cốt lõi cho các nhóm tác vụ chuyên biệt:

---

## 1. UI/UX Design & Frontend Development: Hallmark Skill (`hallmark`)
- **Phạm vi áp dụng:** Toàn bộ tác vụ thiết kế, xây dựng giao diện người dùng WinForms C# (.NET 10), Web, HTML/CSS, Dashboard tòa soạn.
- **Tiêu chuẩn:** Tuân thủ triệt để nguyên tắc **Anti-AI-slop** của `hallmark` (`.agents/skills/hallmark/SKILL.md`).
- **Nguyên tắc then chốt:**
  1. Đa dạng cấu trúc layout (không dùng mẫu 3-card rập khuôn).
  2. Khóa bảng màu và kích thước (Locked Design Tokens).
  3. Tiêu đề luôn viết chữ đứng (`normal`), không in nghiêng.
  4. Nội dung trung thực, đúng quy trình xuất bản 6 giai đoạn của HUIT.
  5. Không vẽ chrome/khung trình duyệt giả.
  6. Hỗ trợ High-DPI (`PerMonitorV2`) và chuẩn responsive đa màn hình.
  7. Tuyệt đối không dùng bullet point trên giao diện web (thay bằng đoạn văn liền mạch, đánh số hoặc phân dòng rõ ràng).

---

## 2. Quản lý tri thức & Ghi nhớ dài hạn: Mem0 Skill (`mem0`)
- **Phạm vi áp dụng:** Lưu giữ ngữ cảnh dự án, thói quen người dùng, các quyết định kiến trúc (ADRs), và sự nhất quán qua nhiều phiên làm việc.
- **Tiêu chuẩn:** Tuân thủ cấu trúc của `mem0` (`.agents/skills/mem0/SKILL.md`).
- **Nguyên tắc then chốt:**
  1. Tự động đọc và đối chiếu tri thức đã lưu tại `.agents/memory/project_memory.json`.
  2. Ghi nhận các quyết định công nghệ quan trọng để tránh hỏi lại hoặc đi ngược lại hướng đi đã chốt.
  3. Duy trì tính nhất quán về vai trò người dùng (5 vai trò chuẩn) và quy trình nghiệp vụ tòa soạn.

---

## 3. Tự động hóa & Kiểm thử trình duyệt: Browser-Use Skill (`browser-use`)
- **Phạm vi áp dụng:** Tự động hóa thao tác web, kiểm thử giao diện web (E2E), điền biểu mẫu nộp bài, trích xuất dữ liệu tài liệu khoa học, kiểm tra DOM và tính responsive.
- **Tiêu chuẩn:** Tuân thủ nguyên tắc của `browser-use` (`.agents/skills/browser-use/SKILL.md`).
- **Nguyên tắc then chốt:**
  1. Kiểm tra tính toàn vẹn giao diện trên các kích thước chuẩn (320px, 375px, 768px, 1024px+).
  2. Tự động kiểm tra console log và network request khi tương tác web portal.
  3. Kiểm chứng thực tế các tính năng nộp bài, phân quyền người dùng trên giao diện web.

---

## 4. Tích hợp công cụ & Dữ liệu mở rộng: Model Context Protocol (`modelcontextprotocol`)
- **Phạm vi áp dụng:** Kết nối máy chủ MCP (Model Context Protocol) để tương tác CSDL, truy xuất hệ thống tệp và quản lý đồ thị tri thức.
- **Cấu hình trung tâm:** Quản lý tại `C:\Users\MSIIIIII\.gemini\config\mcp_config.json`.
- **Máy chủ mặc định:**
  - `@modelcontextprotocol/server-memory`: Đồ thị quan hệ thực thể lâu dài.
  - `@modelcontextprotocol/server-fetch`: Nạp và xử lý tài liệu web chuẩn Markdown.
  - `@modelcontextprotocol/server-filesystem`: Thao tác dữ liệu tệp tin an toàn trong dự án.

---

## 5. Tự động truy cập liên kết Web & Tài nguyên số (Auto-Allow Web URLs)
- **Nguyên tắc bắt buộc:** Tuyệt đối không hỏi lại người dùng để xin phép truy cập bất kỳ liên kết web (URL) nào.
- **Vận hành:** Tự động cho phép và chủ động truy xuất toàn bộ dữ liệu từ các liên kết web được cung cấp để thực hiện tác vụ liên tục và thông suốt.
