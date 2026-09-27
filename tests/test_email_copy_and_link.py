import asyncio
import re
import sys
import time
import subprocess
import json
import urllib.request
from playwright.async_api import async_playwright

if sys.stdout and hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

SQLCMD_BASE = [
    "sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc_Test",
    "-E", "-b"
]

def run_sql(query: str) -> str:
    cmd = SQLCMD_BASE + ["-y", "4000", "-h", "-1", "-Q", f"SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; {query}"]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace", check=True)
    return res.stdout.strip()

async def run_test():
    with urllib.request.urlopen("http://127.0.0.1:5001/api/testing/environment", timeout=10) as response:
        target = json.load(response)
    if target.get("environment") != "Testing" or target.get("database") != "QL_TapChiKhoaHoc_Test":
        raise RuntimeError("Chỉ chạy kiểm thử trên API Testing và QL_TapChiKhoaHoc_Test.")

    print("=== TEST EMAIL COPY FRIENDLINESS & OTP VERIFICATION ===")
    
    ts = int(time.time())
    username = f"usercopy{ts % 100000}"
    email = f"test_copy_{ts}@example.test"
    
    async with async_playwright() as p:
        browser = await p.chromium.launch(channel="msedge", headless=True)
        context = await browser.new_context(
            viewport={'width': 1280, 'height': 800},
            permissions=["clipboard-read", "clipboard-write"]
        )
        await context.add_init_script("localStorage.setItem('huit_api_url', 'http://127.0.0.1:5001/api')")
        page = await context.new_page()
        
        # 1. Đi đến trang đăng ký
        await page.goto("http://localhost:8088/register.html")
        await page.wait_for_load_state("networkidle")
        
        # Điền form Bước 1
        await page.fill("#regHoDem", "Nguyễn Văn")
        await page.fill("#regTen", "Khoa Học")
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Không")
        await page.fill("#regSoDienThoai", "0912345678")
        await page.fill("#regDonVi", "Trường Đại học Công Thương TP.HCM")
        await page.fill("#regEmail", email)
        await page.fill("#regTenDangNhap", username)
        await page.fill("#regPassword", "HuitJournal@2026")
        await page.fill("#regConfirmPassword", "HuitJournal@2026")
        await page.check("#regTerms")
        
        # Bấm Đăng ký
        await page.click("button[type='submit']")
        
        # Chờ chuyển sang Bước 2
        await page.wait_for_selector("#register-step-2", state="visible", timeout=10000)
        print("✓ Đã chuyển sang Bước 2 (Xác thực Email)")
        
        # Kiểm tra nút "Dán mã từ bộ nhớ tạm"
        paste_btn = page.locator("#btnPasteOtp")
        is_visible = await paste_btn.is_visible()
        assert is_visible, "Nút #btnPasteOtp phải hiển thị trực quan!"
        btn_text = await paste_btn.inner_text()
        print("✓ Nút #btnPasteOtp hiển thị trực quan:", btn_text.strip())
        
        # Chụp ảnh Step 2 với nút Dán mã
        await page.screenshot(path="tests/shot_step2_paste_button.png")
        print("✓ Đã lưu ảnh chụp: tests/shot_step2_paste_button.png")
        
        # 2. Kiểm tra bản ghi Email trong CSDL (EmailOutbox)
        await asyncio.sleep(1)
        text_body = run_sql(f"SELECT TOP 1 NoiDungText FROM EmailOutbox WHERE NguoiNhan = '{email.lower()}' ORDER BY TaoLucUtc DESC;")
        html_body = run_sql(f"SELECT TOP 1 NoiDungHtml FROM EmailOutbox WHERE NguoiNhan = '{email.lower()}' ORDER BY TaoLucUtc DESC;")
        status = run_sql(f"SELECT TOP 1 TrangThai FROM EmailOutbox WHERE NguoiNhan = '{email.lower()}' ORDER BY TaoLucUtc DESC;")
        print(f"✓ EmailOutbox tìm thấy | Trạng thái gửi: {status}")
        
        # Kiểm tra tính thân thiện sao chép trong PlainText
        assert "[" not in text_body and "]" not in text_body, "PlainText không được chứa ngoặc vuông [ ] bao quanh mã OTP!"
        assert "MÃ XÁC NHẬN BẢO MẬT (Nhấp đúp chuột vào dãy số để sao chép):" in text_body, "PlainText phải có hướng dẫn nhấp đúp sao chép!"
        assert "register.html?regId=" in text_body and "&code=" not in text_body, "Liên kết chỉ được chứa mã hồ sơ, không chứa OTP!"
        print("✓ PlainText Body: KHÔNG chứa ngoặc vuông, mã tách biệt hoàn toàn trên một dòng, sẵn sàng nhấp đúp sao chép!")
        
        # Kiểm tra tính thân thiện sao chép trong HTML
        assert "user-select: all" in html_body, "HTML Body phải có CSS user-select: all để 1 chạm chọn hết mã OTP!"
        assert "Mở trang nhập mã xác nhận" in html_body, "HTML Body phải có liên kết mở trang nhập mã!"
        print("✓ HTML Body: Có user-select: all và liên kết mở trang nhập mã!")
        
        # Lấy MaDangKy và mã OTP 6 số
        reg_id_raw = run_sql(f"SELECT TOP 1 MaDangKy FROM DangKyChoXacNhan WHERE EmailGoc = '{email}' ORDER BY TaoLucUtc DESC;")
        reg_id = re.search(r'[0-9a-fA-F-]{36}', reg_id_raw).group(0)
        match = re.search(r'\b\d{6}\b', text_body)
        assert match is not None, "Không tìm thấy mã OTP 6 số trong text body!"
        plain_code = match.group(0)
        print("✓ Trích xuất được mã OTP 6 số từ hộp thư mô phỏng")
        
        # 3. Liên kết mở trang nhập mã, không truyền OTP qua URL
        activation_url = f"http://localhost:8088/register.html?regId={reg_id}"
        print("-> Đang mở trang nhập mã từ liên kết email")
        
        page2 = await context.new_page()
        await page2.goto(activation_url)
        await page2.wait_for_selector("#register-step-2", state="visible", timeout=10000)
        for index, digit in enumerate(plain_code):
            await page2.locator(".otp-digit").nth(index).fill(digit)
        
        # Chờ chuyển hướng đến profile.html
        await page2.wait_for_url("**/profile.html*", timeout=10000)
        print("✓ Nhập mã từ email đã kích hoạt tài khoản và chuyển hướng đến profile.html!")
        
        await page2.screenshot(path="tests/shot_activated_via_link.png")
        print("✓ Đã lưu ảnh chụp: tests/shot_activated_via_link.png")
        
        # 4. Test tính năng Dán mã (Paste) trên Step 2
        ts2 = ts + 1
        username2 = f"userpaste{ts2 % 100000}"
        email2 = f"test_paste_{ts2}@example.test"
        
        page3 = await context.new_page()
        await page3.goto("http://localhost:8088/register.html")
        await page3.fill("#regHoDem", "Trần Thị")
        await page3.fill("#regTen", "Thử Nghiệm")
        await page3.select_option("#regHocVi", "Tiến sĩ")
        await page3.select_option("#regHocHam", "Không")
        await page3.fill("#regSoDienThoai", "0987654321")
        await page3.fill("#regDonVi", "Trường Đại học Công Thương TP.HCM")
        await page3.fill("#regEmail", email2)
        await page3.fill("#regTenDangNhap", username2)
        await page3.fill("#regPassword", "HuitJournal@2026")
        await page3.fill("#regConfirmPassword", "HuitJournal@2026")
        await page3.check("#regTerms")
        await page3.click("button[type='submit']")
        
        await page3.wait_for_selector("#register-step-2", state="visible", timeout=10000)
        
        # Lấy OTP của user 2 từ text body
        text_body2 = run_sql(f"SELECT TOP 1 NoiDungText FROM EmailOutbox WHERE NguoiNhan = '{email2.lower()}' ORDER BY TaoLucUtc DESC;")
        code2_match = re.search(r'\b\d{6}\b', text_body2)
        plain_code2 = code2_match.group(0)
        print(f"✓ User 2 OTP: {plain_code2}")
        
        # Thao tác dán mã vào Step 2 thông qua bấm nút #btnPasteOtp
        await page3.evaluate(f"navigator.clipboard.writeText('{plain_code2}')")
        await page3.click("#btnPasteOtp")
        
        # Chờ các ô OTP được điền đầy đủ 6 số
        await page3.wait_for_function("getEnteredOtpCode().length === 6", timeout=5000)
        digits_val = await page3.evaluate("getEnteredOtpCode()")
        print(f"✓ Giá trị 6 ô OTP sau khi bấm nút Dán mã: {digits_val}")
        assert digits_val == plain_code2, f"Kỳ vọng {plain_code2}, nhận được {digits_val}"
        
        # Chờ chuyển hướng đến profile.html
        await page3.wait_for_url("**/profile.html*", timeout=10000)
        print("✓ Bấm nút 'Dán mã từ bộ nhớ tạm' thành công và tự động kích hoạt tài khoản thành công!")
        
        await browser.close()
        
    print("=== TẤT CẢ KIỂM THỬ SAO CHÉP & KÍCH HOẠT ĐỀU THÀNH CÔNG 100%! ===")

if __name__ == "__main__":
    asyncio.run(run_test())
