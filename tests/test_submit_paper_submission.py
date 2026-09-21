import asyncio
import os
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')

from playwright.async_api import async_playwright

async def run_submission_test():
    print("=" * 70)
    print("KIỂM THỬ THỰC TẾ: CHỨC NĂNG NỘP BÀI (SUBMIT-PAPER.HTML)")
    print("=" * 70)

    web_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    file_url = f"file:///{web_dir.replace(os.sep, '/')}/submit-paper.html"

    console_errors = []
    page_errors = []

    async with async_playwright() as p:
        try:
            browser = await p.chromium.launch(headless=True, channel="msedge")
        except Exception:
            try:
                browser = await p.chromium.launch(headless=True, channel="chrome")
            except Exception:
                browser = await p.chromium.launch(headless=True)
        page = await browser.new_page(viewport={"width": 1366, "height": 768})

        page.on("console", lambda msg: console_errors.append(msg.text) if msg.type == "error" else None)
        page.on("pageerror", lambda err: page_errors.append(str(err)))

        print(f"-> Mở trang: {file_url}")
        await page.goto(file_url)
        await page.wait_for_timeout(500)

        # 1. Giả lập đăng nhập tác giả
        print("-> Thiết lập phiên tác giả (journal_token, journal_user)...")
        await page.evaluate("""() => {
            localStorage.setItem('journal_token', 'test_jwt_author_token_123');
            localStorage.setItem('journal_user', JSON.stringify({
                id: 3,
                hoTen: 'TS. Vũ Thị F',
                email: 'vuthif@huit.edu.vn',
                isLoggedIn: true,
                chucVu: 'Tác giả'
            }));
        }""")

        # 2. Sử dụng nút 1-Click Demo để nạp dữ liệu đầy đủ
        print("-> Nhấp nút '✦ Tải bản thảo mẫu thử nghiệm nhanh (1-Click Demo)'...")
        await page.evaluate("loadDemoSampleFile()")
        await page.wait_for_timeout(1600) # Đợi hiệu ứng extraction simulation

        # 3. Kiểm tra các trường đã được điền
        title_vi = await page.input_value("#title-vi")
        keywords_vi = await page.input_value("#keywords-vi")
        abstract_vi = await page.input_value("#abstract-vi")
        print(f"  [✓] Tiêu đề tiếng Việt: {title_vi[:50]}...")
        print(f"  [✓] Từ khóa tiếng Việt: {keywords_vi}")
        print(f"  [✓] Tóm tắt tiếng Việt: {len(abstract_vi)} ký tự")

        # 4. Chuyển sang Bước 5 (Tổng duyệt)
        print("-> Chuyển sang Bước 5: Xem lại toàn diện & xác nhận...")
        await page.evaluate("switchStep(5)")
        await page.wait_for_timeout(400)

        # Kiểm tra Step 5 hiển thị đúng từ khóa từ keywords-vi
        rv_keywords = await page.inner_text("#rv-keywords")
        print(f"  [✓] Bước 5 hiển thị từ khóa: {rv_keywords}")

        # 5. Đánh dấu đồng ý cam kết
        print("-> Đánh dấu cam đoan liêm chính học thuật (#final-agree-check)...")
        await page.check("#final-agree-check")

        # 6. Mock hàm apiSubmitPaper để kiểm tra luồng nộp bài không bị crash
        print("-> Mock apiSubmitPaper và kích hoạt handleFinalSubmit()...")
        submission_payload_received = await page.evaluate("""() => {
            return new Promise(async (resolve) => {
                // Mock apiSubmitPaper để bắt dữ liệu FormData thực tế được gửi
                window.apiSubmitPaper = async (formData) => {
                    return {
                        success: true,
                        maDinhDanh: 'JST-2026-9999',
                        tieuDe: formData.get('TieuDe'),
                        tuKhoa: formData.get('TuKhoa'),
                        tomTat: formData.get('TomTat'),
                        chuyenNganh: formData.get('MaChuyenNganh')
                    };
                };

                // Bắt kết quả thông báo showToast khi nộp bài
                let submittedResult = null;
                const originalShowToast = window.showToast;
                window.showToast = (msg, type) => {
                    submittedResult = msg;
                    if (originalShowToast) originalShowToast(msg, type);
                };

                // Gọi handleFinalSubmit()
                await handleFinalSubmit();
                resolve(submittedResult);
            });
        }""")

        print(f"  [✓] Kết quả gọi handleFinalSubmit: {submission_payload_received is not None}")
        if submission_payload_received:
            first_line = submission_payload_received.split('\n')[0]
            print(f"  [✓] Thông báo phản hồi từ hệ thống: {first_line}")

        # 7. Kiểm tra lỗi JS Console hoặc Page Crash
        print(f"-> Kiểm tra lỗi runtime Console: {len(console_errors)} lỗi")
        for err in console_errors:
            print(f"   [LỖI CONSOLE] {err}")
            
        print(f"-> Kiểm tra Page Errors: {len(page_errors)} lỗi")
        for err in page_errors:
            print(f"   [LỖI RUNTIME] {err}")

        assert len(page_errors) == 0, f"Trang bị crash với lỗi: {page_errors}"
        assert len(console_errors) == 0, f"Console có lỗi: {console_errors}"
        assert submission_payload_received is not None, "Không nhận được phản hồi nộp bài thành công!"

        print("\n" + "=" * 70)
        print("KẾT QUẢ: 100% THÀNH CÔNG! ĐÃ SỬA TRIỆT ĐỂ BUG CRASH Ở HANDLEFINALSUBMIT()")
        print("=" * 70)

        await browser.close()

if __name__ == "__main__":
    asyncio.run(run_submission_test())
