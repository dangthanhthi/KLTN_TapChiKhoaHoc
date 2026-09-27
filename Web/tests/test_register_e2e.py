import asyncio
import sys
from playwright.async_api import async_playwright

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

async def run():
    async with async_playwright() as p:
        try:
            browser = await p.chromium.launch(headless=True, channel="msedge")
        except Exception:
            browser = await p.chromium.launch(headless=True, channel="chrome")
        page = await browser.new_page()
        page.on("console", lambda msg: print("Console:", msg.text))
        page.on("pageerror", lambda err: print("PageError:", err))
        
        dialog_messages = []
        async def handle_dialog(dialog):
            dialog_messages.append(dialog.message)
            await dialog.accept()
        
        page.on("dialog", lambda d: asyncio.create_task(handle_dialog(d)))
        
        print("-> Mở trang http://localhost:5000/register.html...")
        await page.goto("http://localhost:5000/register.html")
        await page.fill("#regHoDem", "Trần Văn")
        await page.fill("#regTen", "Test")
        await page.select_option("#regHocVi", "Thạc sĩ")
        await page.select_option("#regHocHam", "Không")
        await page.select_option("#regGioiTinh", "Nam")
        await page.select_option("#regQuocGia", "Vietnam")
        await page.fill("#regSoDienThoai", "0912345678")
        await page.fill("#regDonVi", "Khoa CNTT - HUIT")
        await page.fill("#regDiaChi", "140 Lê Trọng Tấn")
        await page.fill("#regSoTaiKhoan", "999888777")
        await page.fill("#regChuTaiKhoan", "TRAN VAN TEST")
        await page.fill("#regNganHang", "Vietcombank")
        import time
        ts = int(time.time())
        u_email = f"tranvan_{ts}@gmail.com"
        u_user = f"tranvan_{ts}"
        await page.fill("#regEmail", u_email)
        await page.fill("#regTenDangNhap", u_user)
        await page.fill("#regPassword", "123456")
        await page.fill("#regConfirmPassword", "123456")
        await page.check("#regTerms")
        
        print("-> Nhấp nút Đăng ký tài khoản...")
        await page.click("button[type='submit']")
        await page.wait_for_timeout(2500)
        
        print("-> Trang hiện tại sau đăng ký:", page.url)
        assert "profile.html" in page.url, "Không chuyển hướng tới profile.html!"
        print("[✓] KIỂM THỬ E2E ĐĂNG KÝ TÀI KHOẢN HOÀN TOÀN THÀNH CÔNG!")
        await browser.close()

if __name__ == "__main__":
    asyncio.run(run())
