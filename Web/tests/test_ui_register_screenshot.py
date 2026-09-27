import asyncio
import sys
from playwright.async_api import async_playwright

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

async def main():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True, channel="msedge")
        page = await browser.new_page(viewport={"width": 1280, "height": 900})
        
        # Mở trang đăng ký
        await page.goto("http://localhost:8088/register.html")
        await page.wait_for_timeout(1000)
        
        # Điền thông tin thử nghiệm để kích hoạt các validator giao diện:
        # 1. Chọn học vị Cử nhân + tích phản biện -> kiểm tra alert học thuật
        await page.select_option("#regHocVi", "Cử nhân")
        
        # 2. Nhập tên đăng nhập hợp lệ
        await page.fill("#regTenDangNhap", "nguyenvana_huit")
        
        # 3. Nhập mật khẩu mạnh -> kiểm tra password strength meter
        await page.fill("#regPassword", "HuitJournal@2026Secure")
        await page.fill("#regConfirmPassword", "HuitJournal@2026Secure")
        
        # 4. Nhập tài khoản ngân hàng và kiểm tra viết hoa
        await page.fill("#regSoTaiKhoan", "1903888999")
        await page.fill("#regChuTaiKhoan", "nguyen van a") # oninput sẽ viết hoa
        await page.fill("#regNganHang", "Vietcombank")
        
        # 5. Nhập ORCID dạng url
        await page.fill("#regOrcid", "https://orcid.org/0000-0002-1825-0097")
        await page.eval_on_selector("#regOrcid", "el => el.blur()")
        
        await page.wait_for_timeout(1000)
        
        # Chụp ảnh toàn cảnh form đăng ký sau nâng cấp
        shot_path = r"C:\Users\MSIIIIII\.gemini\antigravity\brain\2d768ef8-fc1d-49c3-94fc-8635ba4a8e61\shot_register_constraints.png"
        await page.screenshot(path=shot_path, full_page=True)
        print(f"Screenshot saved to: {shot_path}")
        
        await browser.close()

if __name__ == "__main__":
    asyncio.run(main())
