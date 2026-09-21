import asyncio
import sys
from playwright.async_api import async_playwright

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

async def test_draft():
    async with async_playwright() as p:
        try:
            browser = await p.chromium.launch(headless=True, channel="msedge")
        except Exception:
            browser = await p.chromium.launch(headless=True, channel="chrome")
        page = await browser.new_page()
        
        print("1. Truy cập register.html...")
        await page.goto("http://localhost:5000/register.html")
        
        print("2. Nhập dữ liệu thử nghiệm...")
        await page.fill("#regHoDem", "Dang Thanh")
        await page.fill("#regTen", "Thi")
        await page.select_option("#regHocVi", "Thạc sĩ")
        await page.fill("#regSoDienThoai", "0984012345")
        await page.fill("#regEmail", "dangthanhthi_draft@gmail.com")
        await page.fill("#regTenDangNhap", "dangthanhthi_draft")
        await page.fill("#regDonVi", "Khoa CNTT - HUIT")
        await page.fill("#regSoTaiKhoan", "123456789")
        await page.fill("#regChuTaiKhoan", "DANG THANH THI")
        await page.fill("#regNganHang", "Vietcombank")
        
        # Chờ 500ms để autosave ghi vào storage
        await page.wait_for_timeout(500)
        
        print("3. Tải lại trang (F5 / reload)...")
        await page.reload()
        await page.wait_for_timeout(1000)
        
        # Kiểm tra xem dữ liệu có được khôi phục không
        ho_dem = await page.input_value("#regHoDem")
        ten = await page.input_value("#regTen")
        hoc_vi = await page.input_value("#regHocVi")
        so_dien_thoai = await page.input_value("#regSoDienThoai")
        email = await page.input_value("#regEmail")
        ten_dn = await page.input_value("#regTenDangNhap")
        don_vi = await page.input_value("#regDonVi")
        so_tk = await page.input_value("#regSoTaiKhoan")
        chu_tk = await page.input_value("#regChuTaiKhoan")
        ngan_hang = await page.input_value("#regNganHang")
        
        print(f"   Họ đệm sau reload: {ho_dem}")
        print(f"   Tên sau reload: {ten}")
        print(f"   Học vị sau reload: {hoc_vi}")
        print(f"   Email sau reload: {email}")
        print(f"   Đơn vị sau reload: {don_vi}")
        print(f"   Số TK sau reload: {so_tk}")
        
        assert ho_dem == "Dang Thanh", "Họ đệm không khớp!"
        assert ten == "Thi", "Tên không khớp!"
        assert hoc_vi == "Thạc sĩ", "Học vị không khớp!"
        assert email == "dangthanhthi_draft@gmail.com", "Email không khớp!"
        assert don_vi == "Khoa CNTT - HUIT", "Đơn vị không khớp!"
        assert so_tk == "123456789", "Số TK không khớp!"
        
        alert_text = await page.inner_text("#draft-alert-box")
        print(f"   Notice text: {alert_text}")
        assert "Đã tự động khôi phục dữ liệu" in alert_text
        
        print("4. Bấm 'Xóa bản nháp'...")
        await page.click("#draft-alert-box button")
        await page.wait_for_timeout(500)
        
        ho_dem_cleared = await page.input_value("#regHoDem")
        email_cleared = await page.input_value("#regEmail")
        print(f"   Họ đệm sau khi xóa: '{ho_dem_cleared}'")
        print(f"   Email sau khi xóa: '{email_cleared}'")
        assert ho_dem_cleared == "", "Họ đệm chưa được xóa!"
        assert email_cleared == "", "Email chưa được xóa!"
        
        print("[✓] KIỂM THỬ TÍNH NĂNG GHI NHỚ VÀ KHÔI PHỤC BẢN NHÁP THÀNH CÔNG 100%!")
        await browser.close()

if __name__ == "__main__":
    asyncio.run(test_draft())
