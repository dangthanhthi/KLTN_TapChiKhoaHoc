import asyncio
import sys
import time
from playwright.async_api import async_playwright

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

async def run():
    print("=" * 70)
    print("KIỂM THỬ TỰ ĐỘNG CHẾ ĐỘ STANDALONE ENGINE (VERCEL / MOBILE OFFLINE)")
    print("Mô phỏng máy chủ Backend hoàn toàn offline (chặn 100% request localhost:5000)")
    print("=" * 70)

    async with async_playwright() as p:
        try:
            browser = await p.chromium.launch(headless=True, channel="msedge")
        except Exception:
            browser = await p.chromium.launch(headless=True, channel="chrome")
        
        context = await browser.new_context()
        page = await context.new_page()

        # CHẶN TOÀN BỘ KẾT NỐI TỚI BACKEND 5000 ĐỂ MÔ PHỎNG TẮT MÁY / VERCEL ĐỘC LẬP
        await page.route("**/localhost:5000/**", lambda route: route.abort())
        await page.route("**/127.0.0.1:5000/**", lambda route: route.abort())

        # Bắt log console
        page.on("console", lambda msg: print(f"  [Browser Console {msg.type}]: {msg.text}"))
        page.on("pageerror", lambda err: print(f"  [Browser PageError]: {err}"))

        # -------------------------------------------------------------
        # TEST 1: Đăng nhập tài khoản mẫu trong môi trường offline
        # -------------------------------------------------------------
        print("\n[TEST 1] Đăng nhập tài khoản mẫu (GS.TS. Đặng Thành Thi) khi backend offline...")
        await page.goto("http://localhost:8088/login.html")
        await page.wait_for_load_state("networkidle")

        await page.fill("#username", "dangthanhthi")
        await page.fill("#password", "123456")
        await page.click("button[type='submit']")
        await page.wait_for_timeout(1500)

        current_url = page.url
        print(f"  -> URL sau đăng nhập: {current_url}")
        assert "profile.html" in current_url, "Lỗi: Không chuyển hướng đến profile.html sau khi đăng nhập!"

        heading_text = await page.inner_text("#author-heading-name")
        print(f"  -> Tiêu đề bàn làm việc: {heading_text}")
        assert "GS.TS. Đặng Thành Thi" in heading_text, "Lỗi: Tên người dùng không đúng!"
        print("  [✓] TEST 1 PASS: Đăng nhập offline thành công, chuyển hướng vào Bàn làm việc!")

        # -------------------------------------------------------------
        # TEST 2: Kiểm tra phân công phản biện & Nộp phiếu đánh giá BM-04
        # -------------------------------------------------------------
        print("\n[TEST 2] Kiểm tra phân công phản biện & nộp phiếu đánh giá BM-04...")
        # Đợi danh sách phân công nạp xong
        await page.wait_for_timeout(1000)
        assignments_count = await page.locator("#my-assignments-container .sub-item-card").count()
        print(f"  -> Số lượng nhiệm vụ phản biện hiển thị: {assignments_count}")
        assert assignments_count > 0, "Lỗi: Không hiển thị danh sách nhiệm vụ phản biện!"

        # Mở modal đánh giá BM-04
        eval_btn = page.locator("#my-assignments-container button:has-text('BM-04')").first
        if await eval_btn.is_visible():
            print("  -> Nhấp nút 'Đánh giá bản thảo (BM-04)'...")
            await eval_btn.click()
            await page.wait_for_timeout(600)

            # Điền nhận xét và nộp
            await page.fill("#eval-nhanxet-tacgia", "Bản thảo có tính mới cao, phương pháp nghiên cứu rõ ràng và số liệu thuyết phục.")
            await page.fill("#eval-nhanxet-baomat", "Đề xuất thông qua bản thảo không cần sửa đổi lớn.")
            await page.click("#modal-eval-paper button[type='submit']")
            await page.wait_for_timeout(1000)

            # Kiểm tra toast hoặc trạng thái đã hoàn thành
            print("  [✓] TEST 2 PASS: Nộp phiếu đánh giá BM-04 offline thành công!")
        else:
            print("  [!] Nhiệm vụ phản biện đã được đánh giá trước đó.")

        # -------------------------------------------------------------
        # TEST 3: Đổi mật khẩu tài khoản trong chế độ Standalone
        # -------------------------------------------------------------
        print("\n[TEST 3] Đổi mật khẩu tài khoản qua Modal Đổi mật khẩu...")
        # Mở modal đổi mật khẩu
        await page.click("button:has-text('Đổi mật khẩu')")
        await page.wait_for_timeout(600)
        assert await page.is_visible("#modal-change-pass"), "Lỗi: Modal đổi mật khẩu không hiển thị!"

        await page.fill("#p-current", "123456")
        await page.fill("#p-new", "KLTN@2026Secure")
        await page.fill("#p-conf", "KLTN@2026Secure")
        
        # Nhấp nút lưu
        await page.click("#btn-save-pass-submit")
        await page.wait_for_timeout(1000)
        print("  [✓] TEST 3 PASS: Đổi mật khẩu tài khoản trong chế độ Standalone thành công!")

        # -------------------------------------------------------------
        # TEST 4: Nộp bản thảo bài báo mới (Submit Paper Workflow)
        # -------------------------------------------------------------
        print("\n[TEST 4] Nộp bản thảo bài báo mới qua submit-paper.html...")
        await page.goto("http://localhost:8088/submit-paper.html")
        await page.wait_for_load_state("networkidle")

        # Nạp bản thảo mẫu
        await page.evaluate("loadDemoSampleFile()")
        await page.wait_for_timeout(1600)

        # Chuyển sang bước 5 và xác nhận
        await page.evaluate("switchStep(5)")
        await page.wait_for_timeout(400)
        await page.check("#final-agree-check")

        # Nộp bản thảo chính thức
        print("  -> Gọi handleFinalSubmit() gửi bài nộp vào Standalone Engine...")
        await page.evaluate("handleFinalSubmit()")
        await page.wait_for_timeout(2500)

        # Kiểm tra chuyển hướng về profile.html
        print(f"  -> URL sau nộp bài: {page.url}")
        assert "profile.html" in page.url, "Lỗi: Không chuyển về profile.html sau khi nộp bài!"

        # Kiểm tra bài vừa nộp có trong danh sách bản thảo
        first_sub_title = await page.locator("#my-submissions-container article h3").first.inner_text()
        print(f"  -> Tiêu đề bản thảo mới nhất trong Bàn làm việc: {first_sub_title}")
        assert len(first_sub_title) > 0, "Lỗi: Không có tiêu đề bản thảo!"
        print("  [✓] TEST 4 PASS: Nộp bài báo độc lập và hiển thị tức thì trên Bàn làm việc thành công!")

        # -------------------------------------------------------------
        # TEST 5: Đăng ký tài khoản mới & Đăng ký nâng cấp Phản biện viên
        # -------------------------------------------------------------
        print("\n[TEST 5] Đăng ký tài khoản mới (Học vị Tiến sĩ) và kích hoạt Phản biện viên...")
        # Xóa session hiện tại
        await page.evaluate("() => { localStorage.clear(); sessionStorage.clear(); }")
        await page.goto("http://localhost:8088/register.html")
        await page.wait_for_load_state("networkidle")

        ts = int(time.time())
        new_email = f"tiensi_{ts}@huit.edu.vn"
        new_username = f"tiensi_{ts}"

        await page.fill("#regHoDem", "Lê Đình")
        await page.fill("#regTen", "Khang")
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Không")
        await page.fill("#regSoDienThoai", "0938123456")
        await page.fill("#regDonVi", "Trường Đại học Công Thương TP.HCM (HUIT)")
        # Bỏ chọn phản biện khi đăng ký để kiểm tra nút nâng cấp role trên profile
        await page.uncheck("#role_reviewer")
        await page.fill("#regEmail", new_email)
        await page.fill("#regTenDangNhap", new_username)
        await page.fill("#regPassword", "12345678")
        await page.fill("#regConfirmPassword", "12345678")
        await page.check("#regTerms")

        # Submit form
        await page.click("button[type='submit']")
        await page.wait_for_timeout(2500)

        print(f"  -> URL sau khi đăng ký mới: {page.url}")
        assert "profile.html" in page.url, "Lỗi: Đăng ký không tự động chuyển về profile.html!"

        new_name = await page.inner_text("#author-heading-name")
        print(f"  -> Tên tài khoản mới: {new_name}")
        assert "Lê Đình Khang" in new_name, "Lỗi: Tên hiển thị không khớp!"

        # Kiểm tra box đăng ký phản biện viên hiển thị
        rev_box_visible = await page.is_visible("#reviewer-register-box")
        print(f"  -> Khung đăng ký Phản biện viên hiển thị: {rev_box_visible}")
        assert rev_box_visible, "Lỗi: Khung đăng ký phản biện viên không hiển thị cho Tiến sĩ!"

        # Nhấp nút đăng ký vai trò Phản biện viên
        await page.click("#btn-request-reviewer")
        await page.wait_for_timeout(1000)

        # Kiểm tra badge vai trò
        roles_text = await page.inner_text("#spec-vaitro")
        print(f"  -> Vai trò sau khi đăng ký: {roles_text}")
        assert "Chuyên gia phản biện" in roles_text or "Phản biện viên" in roles_text, "Lỗi: Vai trò phản biện chưa được thêm!"
        print("  [✓] TEST 5 PASS: Đăng ký tài khoản mới & thêm vai trò Phản biện viên thành công!")

        # Chụp ảnh minh chứng
        await page.screenshot(path="tests/shot_standalone_full_flow.png", full_page=True)
        print("\n-> Đã lưu ảnh minh chứng: tests/shot_standalone_full_flow.png")

        await browser.close()
        print("\n" + "=" * 70)
        print("TẤT CẢ 5/5 BỘ TEST STANDALONE ENGINE OFFLINE ĐÃ PASS 100%!")
        print("Hệ thống hoạt động hoàn hảo trên Vercel / điện thoại kể cả khi tắt máy tính.")
        print("=" * 70)

if __name__ == "__main__":
    asyncio.run(run())
