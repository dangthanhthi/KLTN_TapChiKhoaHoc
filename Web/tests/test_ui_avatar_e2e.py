import asyncio
import os
import sys
from playwright.async_api import async_playwright

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

# Tạo 1 file ảnh PNG avatar thử nghiệm
def create_sample_avatar_file():
    path = os.path.join(os.path.dirname(__file__), "sample_scientist_avatar.png")
    # PNG 2x2 px màu xanh đậm học thuật
    png_bytes = bytes([
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02,
        0x08, 0x02, 0x00, 0x00, 0x00, 0xFD, 0xD4, 0x9A,
        0x73, 0x00, 0x00, 0x00, 0x15, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x18, 0x05, 0xA3, 0x60,
        0x14, 0x8C, 0x82, 0x51, 0xC1, 0x28, 0x18, 0x00,
        0x2B, 0x15, 0x04, 0x01, 0x36, 0x60, 0xDA, 0xE9,
        0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
        0xAE, 0x42, 0x60, 0x82
    ])
    with open(path, "wb") as f:
        f.write(png_bytes)
    return path

async def main():
    avatar_file = create_sample_avatar_file()
    
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True, channel="msedge")
        page = await browser.new_page(viewport={"width": 1280, "height": 960})
        
        # 1. Đăng nhập qua login.html
        print("1. Đang đăng nhập...")
        await page.goto("http://localhost:8088/login.html")
        await page.wait_for_timeout(500)
        
        # Nếu đã có login session thì vào thẳng profile
        if await page.locator("a:has-text('Vào Bàn làm việc')").is_visible():
            await page.click("a:has-text('Vào Bàn làm việc')")
        else:
            await page.fill("#username", "vuthif@huit.edu.vn")
            await page.fill("#password", "123456")
            await page.click("button[type='submit']")
        
        # Chờ chuyển hướng sang profile.html
        await page.wait_for_url("**/profile.html", timeout=6000)
        await page.wait_for_timeout(1000)
        print("-> Đã vào profile.html!")
        
        # 2. Upload file ảnh đại diện
        print("2. Thực hiện tải ảnh đại diện lên qua input file...")
        file_input = page.locator("#author-avatar-file-input")
        await file_input.set_input_files(avatar_file)
        
        # Chờ phản hồi toast
        await page.wait_for_timeout(2000)
        
        # 3. Mở thử modal Cập nhật hồ sơ để xác nhận avatar preview hiển thị
        print("3. Kiểm tra Modal cập nhật hồ sơ...")
        await page.click("button:has-text('Cập nhật hồ sơ')")
        await page.wait_for_timeout(800)
        
        # Chụp ảnh modal
        modal_shot = r"C:\Users\MSIIIIII\.gemini\antigravity\brain\2d768ef8-fc1d-49c3-94fc-8635ba4a8e61\shot_avatar_modal.png"
        await page.screenshot(path=modal_shot)
        print(f"Modal shot saved to: {modal_shot}")
        
        # Đóng modal
        await page.click("#modal-edit-profile .btn-modal-cancel")
        await page.wait_for_timeout(500)
        
        # 4. Chụp ảnh toàn trang profile sau khi cập nhật avatar
        shot_path = r"C:\Users\MSIIIIII\.gemini\antigravity\brain\2d768ef8-fc1d-49c3-94fc-8635ba4a8e61\shot_avatar_profile.png"
        await page.screenshot(path=shot_path, full_page=True)
        print(f"Full profile shot saved to: {shot_path}")
        
        await browser.close()
    
    print("==================================================")
    print("KIỂM THỬ PLAYWRIGHT E2E AVATAR HOÀN TẤT THÀNH CÔNG!")
    print("==================================================")

if __name__ == "__main__":
    asyncio.run(main())
