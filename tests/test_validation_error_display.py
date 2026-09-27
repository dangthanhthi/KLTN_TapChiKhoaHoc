import asyncio
import os
import sys
from playwright.async_api import async_playwright

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

ARTIFACT_DIR = r"C:\Users\MSIIIIII\.gemini\antigravity\brain\2d768ef8-fc1d-49c3-94fc-8635ba4a8e61"

async def test_validation_error_display():
    print("=== BẮT ĐẦU KIỂM THỬ GIAO DIỆN HIỂN THỊ CẢNH BÁO LỖI (HALLMARK ANTI-AI-SLOP) ===")
    async with async_playwright() as p:
        browser = await p.chromium.launch(channel="msedge", headless=True)
        context = await browser.new_context(viewport={"width": 1280, "height": 900})
        page = await context.new_page()

        # 1. Truy cập trang đăng ký
        await page.goto("http://localhost:8088/register.html", wait_until="networkidle")
        await page.evaluate("() => { sessionStorage.clear(); localStorage.clear(); }")
        await page.reload(wait_until="networkidle")

        print("1. Kiểm tra ban đầu: Banner cảnh báo lỗi và viền đỏ phải ẩn")
        summary_box = page.locator("#register-error-summary-box")
        assert not await summary_box.is_visible(), "Lỗi: Summary box không được hiển thị khi vừa mở trang"

        # 2. Nhấn submit khi chưa điền thông tin -> Phải hiển thị lỗi trực quan đa trường
        print("2. Thử submit form trống -> Kiểm tra gom cụm lỗi và hiển thị trực quan")
        await page.click('button[type="submit"]')
        await page.wait_for_timeout(500)

        # Kiểm tra Summary Box hiển thị
        assert await summary_box.is_visible(), "Lỗi: Summary box phải hiển thị khi có lỗi validation"
        summary_text = await summary_box.inner_text()
        print(f"Summary Box text:\n{summary_text}")
        assert "Vui lòng kiểm tra và sửa lại các mục sau đây" in summary_text
        assert "•" not in summary_text and "*" not in summary_text, "Lỗi Hallmark: Không được dùng bullet points!"
        assert "1." in summary_text and "2." in summary_text, "Lỗi: Danh sách phải được đánh số thứ tự 1, 2, ..."

        # Kiểm tra các trường bị lỗi có class .input-has-error và inline message
        ho_dem_input = page.locator("#regHoDem")
        ho_dem_error = page.locator("#regHoDemError")
        assert "input-has-error" in (await ho_dem_input.get_attribute("class") or "")
        assert await ho_dem_error.is_visible()
        print(f"regHoDemError text: {await ho_dem_error.inner_text()}")

        # 3. Điền thông tin theo đúng kịch bản của người dùng (screenshot media_1790488146167.png):
        # Số điện thoại 9 chữ số: '123456789'
        print("3. Điền dữ liệu theo kịch bản người dùng (SĐT 9 số: 123456789)")
        await page.fill("#regHoDem", "Đặng")
        await page.fill("#regTen", "Thành Thi")
        await page.select_option("#regHocVi", "Cử nhân")
        await page.select_option("#regHocHam", "Không")
        await page.select_option("#regQuocGia", "Vietnam")
        await page.fill("#regSoDienThoai", "123456789")
        await page.fill("#regDonVi", "Khoa CNTT - Đại học Công Thương TP.HCM")
        await page.fill("#regEmail", "dangthanhthi1005@gmail.com")
        await page.fill("#regTenDangNhap", "dangthanhthi1005")
        await page.fill("#regPassword", "Thanhthi@123")
        await page.fill("#regConfirmPassword", "Thanhthi@123")
        await page.check("#regTerms")

        # Submit form
        await page.click('button[type="submit"]')
        await page.wait_for_timeout(500)

        # Kiểm tra: Chỉ còn lỗi Số điện thoại
        assert await summary_box.is_visible()
        sdt_input = page.locator("#regSoDienThoai")
        sdt_error = page.locator("#regSoDienThoaiError")
        assert "input-has-error" in (await sdt_input.get_attribute("class") or ""), "Lỗi: regSoDienThoai phải có viền đỏ input-has-error"
        assert await sdt_error.is_visible(), "Lỗi: regSoDienThoaiError phải hiển thị ngay dưới ô điện thoại"
        sdt_err_msg = await sdt_error.inner_text()
        print(f"Thông báo lỗi SĐT dưới ô input: '{sdt_err_msg}'")
        assert "Số điện thoại Việt Nam không hợp lệ" in sdt_err_msg

        # Chụp ảnh minh chứng lỗi trực quan theo yêu cầu người dùng
        shot_path = os.path.join(ARTIFACT_DIR, "shot_error_indication_visible.png")
        await page.screenshot(path=shot_path, full_page=False)
        print(f"Đã chụp ảnh minh chứng: {shot_path}")

        # 4. Kiểm tra xóa lỗi realtime khi người dùng chỉnh sửa SĐT
        print("4. Nhập lại SĐT hợp lệ (0989012345) -> Kiểm tra tự động xóa viền đỏ và ẩn lỗi")
        await page.fill("#regSoDienThoai", "0989012345")
        await page.wait_for_timeout(300)
        assert "input-has-error" not in (await sdt_input.get_attribute("class") or ""), "Lỗi: Khi sửa SĐT đúng thì phải xóa input-has-error"
        assert not await sdt_error.is_visible(), "Lỗi: Khi sửa SĐT đúng thì phải ẩn thông báo lỗi"

        # Chụp ảnh sau khi sửa xong
        shot_path_fixed = os.path.join(ARTIFACT_DIR, "shot_error_cleared_on_correction.png")
        await page.screenshot(path=shot_path_fixed, full_page=False)
        print(f"Đã chụp ảnh sau sửa lỗi: {shot_path_fixed}")

        await browser.close()
        print("=== TẤT CẢ KIỂM THỬ HIỂN THỊ CẢNH BÁO LỖI ĐÃ ĐẠT 100%! ===")

if __name__ == "__main__":
    asyncio.run(test_validation_error_display())
