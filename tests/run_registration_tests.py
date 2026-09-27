import asyncio
import json
import sys
sys.stdout.reconfigure(encoding='utf-8')
from playwright.async_api import async_playwright
from test_target import API_ROOT, require_testing_api

async def run_tests():
    require_testing_api()
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True, channel="msedge")
        context = await browser.new_context(viewport={'width': 1280, 'height': 900})
        await context.add_init_script(f"localStorage.setItem('huit_api_url', {json.dumps(API_ROOT)})")
        page = await context.new_page()

        print("[TEST] 1. Navigating to http://localhost:8088/register.html ...")
        await page.goto("http://localhost:8088/register.html?mode=online")
        await page.wait_for_load_state("networkidle")

        # Clear any draft
        await page.evaluate("clearRegistrationDraft()")
        await page.wait_for_timeout(500)

        # ---------------------------------------------------------------------
        # TEST A: Password strength meter tests
        # ---------------------------------------------------------------------
        print("[TEST] 2. Testing password strength feedback...")
        
        # Test weak password "123456"
        await page.fill("#regPassword", "123456")
        await page.wait_for_timeout(200)
        str_text = await page.text_content("#password-strength-text")
        str_detail = await page.text_content("#password-strength-detail")
        print("  Password '123456' -> Text:", str_text, "| Detail:", str_detail)
        assert "Không hợp lệ" in str_text or "Quá ngắn" in str_text or "dễ đoán" in str_detail

        # Test weak password "password"
        await page.fill("#regPassword", "password")
        await page.wait_for_timeout(200)
        str_text = await page.text_content("#password-strength-text")
        str_detail = await page.text_content("#password-strength-detail")
        print("  Password 'password' -> Text:", str_text, "| Detail:", str_detail)
        assert "dễ đoán" in str_detail or "Không hợp lệ" in str_text

        # Test short password "Ab1!"
        await page.fill("#regPassword", "Ab1!")
        await page.wait_for_timeout(200)
        str_text = await page.text_content("#password-strength-text")
        print("  Password 'Ab1!' -> Text:", str_text)
        assert "Quá ngắn" in str_text

        # Test strong password "HuitJournal@2026"
        await page.fill("#regPassword", "HuitJournal@2026")
        await page.wait_for_timeout(200)
        str_text = await page.text_content("#password-strength-text")
        str_detail = await page.text_content("#password-strength-detail")
        print("  Password 'HuitJournal@2026' -> Text:", str_text, "| Detail:", str_detail)
        assert "Rất mạnh" in str_text or "Khá an toàn" in str_text

        # ---------------------------------------------------------------------
        # TEST B: Country phone placeholder & realtime validation tests
        # ---------------------------------------------------------------------
        print("[TEST] 3. Testing country-specific phone validation...")
        
        # Default: Vietnam
        quoc_gia = await page.input_value("#regQuocGia")
        assert quoc_gia == "Vietnam"
        phone_ph = await page.get_attribute("#regSoDienThoai", "placeholder")
        print("  Vietnam placeholder:", phone_ph)
        assert "0989012345" in phone_ph

        # Invalid Vietnam phone: 0123456789
        await page.fill("#regSoDienThoai", "0123456789")
        await page.wait_for_timeout(200)
        hint_text = await page.text_content("#regSoDienThoaiHint")
        print("  Invalid VN phone hint:", hint_text)
        assert "không hợp lệ" in hint_text

        # Valid Vietnam phone: 0989012345
        await page.fill("#regSoDienThoai", "0989012345")
        await page.wait_for_timeout(200)
        hint_text = await page.text_content("#regSoDienThoaiHint")
        print("  Valid VN phone hint:", hint_text)
        assert "hợp lệ cho Vietnam" in hint_text

        # Change country to United States
        await page.select_option("#regQuocGia", "United States")
        await page.wait_for_timeout(200)
        phone_ph = await page.get_attribute("#regSoDienThoai", "placeholder")
        hint_text = await page.text_content("#regSoDienThoaiHint")
        print("  US placeholder:", phone_ph, "| Hint with VN number:", hint_text)
        assert "(202)" in phone_ph
        # With previous 0989012345, US validation should now be invalid
        assert "không hợp lệ" in hint_text

        # Enter valid US phone: +1 (202) 555-0123
        await page.fill("#regSoDienThoai", "+1 (202) 555-0123")
        await page.wait_for_timeout(200)
        hint_text = await page.text_content("#regSoDienThoaiHint")
        print("  Valid US phone hint:", hint_text)
        assert "hợp lệ cho United States" in hint_text

        # Change country to Japan
        await page.select_option("#regQuocGia", "Japan")
        await page.wait_for_timeout(200)
        await page.fill("#regSoDienThoai", "090-1234-5678")
        await page.wait_for_timeout(200)
        hint_text = await page.text_content("#regSoDienThoaiHint")
        print("  Valid Japan phone hint:", hint_text)
        assert "hợp lệ cho Japan" in hint_text

        # Reset country to Vietnam & valid phone for submission test
        await page.select_option("#regQuocGia", "Vietnam")
        await page.fill("#regSoDienThoai", "0912345678")

        # ---------------------------------------------------------------------
        # TEST C: Academic rank & degree constraint
        # ---------------------------------------------------------------------
        print("[TEST] 4. Testing Academic rank (HocHam vs HocVi) constraint...")
        await page.select_option("#regHocVi", "Cử nhân")
        await page.wait_for_timeout(200)
        pgs_opt = await page.query_selector('#regHocHam option[value="Phó giáo sư"]')
        is_disabled = await pgs_opt.is_disabled()
        print("  With 'Cử nhân', is 'Phó giáo sư' option disabled?", is_disabled)
        assert is_disabled == True

        # Set to Tiến sĩ, check that PGS becomes enabled
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.wait_for_timeout(200)
        is_disabled = await pgs_opt.is_disabled()
        print("  With 'Tiến sĩ', is 'Phó giáo sư' option disabled?", is_disabled)
        assert is_disabled == False
        await page.select_option("#regHocHam", "Phó giáo sư")

        # ---------------------------------------------------------------------
        # TEST D: Full Registration Happy Path Submission
        # ---------------------------------------------------------------------
        print("[TEST] 5. Submitting full valid registration with real backend API...")
        await page.fill("#regHoDem", "Nguyễn Minh")
        await page.fill("#regTen", "Khoa")
        await page.fill("#regDonVi", "Khoa Công nghệ Thông tin, Đại học Công Thương TP.HCM")
        await page.fill("#regDiaChi", "140 Lê Trọng Tấn, Tân Phú, TP.HCM")
        await page.fill("#regSoTaiKhoan", "19036582910")
        await page.fill("#regChuTaiKhoan", "NGUYEN MINH KHOA")
        await page.fill("#regNganHang", "Techcombank")
        await page.fill("#regEmail", "nmkhoa_test_2026@huit.edu.vn")
        await page.fill("#regTenDangNhap", "nmkhoa_test")
        await page.fill("#regPassword", "Secure@Huit2026")
        await page.fill("#regConfirmPassword", "Secure@Huit2026")
        await page.check("#regTerms")

        # Take screenshot of fully filled form
        shot_path = r"C:\Users\MSIIIIII\.gemini\antigravity\brain\2d768ef8-fc1d-49c3-94fc-8635ba4a8e61\shot_register_tested_full.png"
        await page.screenshot(path=shot_path, full_page=True)
        print("  Screenshot saved to:", shot_path)

        # Submit form
        await page.click('button[type="submit"]')
        await page.wait_for_timeout(2500)

        # Verify toast or redirection
        toast_el = await page.query_selector("#journal-global-toast")
        if toast_el:
            toast_text = await toast_el.text_content()
            print("  Toast response:", toast_text)
            assert "thành công" in toast_text.lower() or "chào mừng" in toast_text.lower()

        # Check local storage token
        token = await page.evaluate("localStorage.getItem('journal_token')")
        print("  JWT Token in localStorage:", bool(token))
        assert bool(token) == True

        # Take success screenshot
        shot_success_path = r"C:\Users\MSIIIIII\.gemini\antigravity\brain\2d768ef8-fc1d-49c3-94fc-8635ba4a8e61\shot_register_success.png"
        await page.screenshot(path=shot_success_path, full_page=True)
        print("  Success screenshot saved to:", shot_success_path)

        await browser.close()
        print("\n[ALL PLAYWRIGHT TESTS PASSED SUCCESSFULLY!]")

if __name__ == "__main__":
    asyncio.run(run_tests())
