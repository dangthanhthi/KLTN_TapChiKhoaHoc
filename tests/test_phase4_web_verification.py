import asyncio
import re
import sys
import time
import subprocess
import json
from playwright.async_api import async_playwright
from test_target import API_ROOT, require_testing_api

if sys.stdout and hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

SQLCMD_BASE = [
    "sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc_Test",
    "-E", "-b"
]

def query_otp_from_email_outbox(email: str) -> str:
    query = f"SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SELECT TOP 1 NoiDungText FROM EmailOutbox WHERE NguoiNhan = '{email}' ORDER BY TaoLucUtc DESC;"
    cmd = SQLCMD_BASE + ["-y", "1000", "-h", "-1", "-Q", query]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace", check=True)
    out = res.stdout
    match = re.search(r'\b(\d{6})\b', out)
    if match:
        return match.group(1)
    raise ValueError(f"Could not find 6-digit OTP code in outbox text for {email}: {out}")

async def run_phase4_e2e_tests():
    require_testing_api()
    print("=" * 70)
    print("PHASE 4 E2E TEST: Web Portal 2-Step Registration with OTP Email Verification")
    print("=" * 70)

    ts = int(time.time())
    test_user = f"p4user_{ts}"
    test_email = f"p4user_{ts}@huit.edu.vn"

    async with async_playwright() as p:
        browser = await p.chromium.launch(channel="msedge", headless=True)
        context = await browser.new_context(viewport={"width": 1280, "height": 900})
        await context.add_init_script(f"localStorage.setItem('huit_api_url', {json.dumps(API_ROOT)})")
        page = await context.new_page()

        console_errors = []
        page.on("console", lambda msg: console_errors.append(msg.text) if msg.type == "error" else None)

        print("\n[TEST 1] Loading register.html...")
        await page.goto("http://localhost:8088/register.html", wait_until="networkidle")
        assert await page.title() == "Đăng ký tài khoản người dùng · Tạp chí Khoa học & Công nghệ"
        print("  ✓ Page loaded successfully with correct title.")

        # Check initial Stepper state
        step1_active = await page.eval_on_selector("#stepIndicator1", "el => el.classList.contains('active')")
        step2_active = await page.eval_on_selector("#stepIndicator2", "el => el.classList.contains('active')")
        step1_visible = await page.is_visible("#register-step-1")
        step2_visible = await page.is_visible("#register-step-2")
        assert step1_active is True, "Step 1 indicator should be active initially"
        assert step2_active is False, "Step 2 indicator should NOT be active initially"
        assert step1_visible is True, "Step 1 container should be visible"
        assert step2_visible is False, "Step 2 container should be hidden"
        print("  ✓ Initial Stepper & containers verified (Step 1 visible, Step 2 hidden).")

        print(f"\n[TEST 2] Filling Step 1 Registration Form for {test_user} ({test_email})...")
        await page.fill("#regHoDem", "Nguyễn Hoàng")
        await page.fill("#regTen", "Nam")
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Không")
        await page.select_option("#regGioiTinh", "Nam")
        await page.select_option("#regNgonNgu", "Tiếng Việt")
        await page.select_option("#regQuocGia", "Vietnam")
        await page.fill("#regSoDienThoai", "0934567890")
        await page.fill("#regDonVi", "Trường Đại học Công Thương TP.HCM")
        await page.fill("#regDiaChi", "140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM")
        await page.fill("#regSoTaiKhoan", "1902837465")
        await page.fill("#regChuTaiKhoan", "NGUYEN HOANG NAM")
        await page.fill("#regNganHang", "Vietcombank")
        await page.select_option("#regChuyenNganh", "1")
        await page.fill("#regTenDangNhap", test_user)
        await page.fill("#regEmail", test_email)
        await page.fill("#regPassword", "HuitSecure@2026")
        await page.fill("#regConfirmPassword", "HuitSecure@2026")
        await page.check("#regTerms")
        print("  ✓ Form inputs populated according to HUIT publication standards.")

        print("\n[TEST 3] Submitting Step 1 and waiting for HTTP 202 Accepted transition...")
        async with page.expect_response(lambda r: "/api/auth/register" in r.url and r.status == 202) as response_info:
            await page.click("button.btn-auth-submit")
        res = await response_info.value
        reg_json = await res.json()
        assert reg_json["success"] is True
        assert reg_json["requiresVerification"] is True
        reg_id = reg_json["registrationId"]
        masked_email = reg_json["maskedEmail"]
        print(f"  ✓ Received HTTP 202 Accepted. RegistrationId: {reg_id}, Masked: {masked_email}")

        # Wait for Step 2 UI transition
        await page.wait_for_selector("#register-step-2", state="visible")
        step1_disp = await page.is_visible("#register-step-1")
        step2_disp = await page.is_visible("#register-step-2")
        assert step1_disp is False, "Step 1 must be hidden"
        assert step2_disp is True, "Step 2 must be visible"

        # Check Stepper in Step 2
        step1_done = await page.eval_on_selector("#stepIndicator1", "el => el.classList.contains('completed')")
        step2_active = await page.eval_on_selector("#stepIndicator2", "el => el.classList.contains('active')")
        assert step1_done is True, "Step 1 should now be completed"
        assert step2_active is True, "Step 2 should now be active"

        # Check displayed masked email
        displayed_email = await page.text_content("#verifyMaskedEmail")
        assert masked_email in displayed_email, f"Displayed masked email should match {masked_email}"

        # Check resend button cooldown
        resend_btn_disabled = await page.is_disabled("#btnResendOtp")
        assert resend_btn_disabled is True, "Resend OTP button must be disabled during 60s cooldown"
        resend_text = await page.text_content("#resendOtpBtnText")
        assert "Gửi lại mã sau" in resend_text, f"Expected cooldown countdown, got {resend_text}"
        print(f"  ✓ Step 2 UI rendered correctly: Masked email '{displayed_email}', Resend button: '{resend_text}'.")

        # Save Step 2 screenshot
        await page.screenshot(path="C:/Users/MSIIIIII/.gemini/antigravity/brain/2d768ef8-fc1d-49c3-94fc-8635ba4a8e61/shot_step2_otp_view.png")
        print("  ✓ Screenshot saved: shot_step2_otp_view.png")

        print("\n[TEST 4] Testing Session Persistence across page reload (F5)...")
        await page.reload(wait_until="networkidle")
        await page.wait_for_selector("#register-step-2", state="visible")
        reload_step1_disp = await page.is_visible("#register-step-1")
        reload_step2_disp = await page.is_visible("#register-step-2")
        assert reload_step1_disp is False, "Step 1 must remain hidden after reload"
        assert reload_step2_disp is True, "Step 2 must automatically restore after reload"
        reload_step2_active = await page.eval_on_selector("#stepIndicator2", "el => el.classList.contains('active')")
        assert reload_step2_active is True, "Step 2 indicator must remain active after reload"
        print("  ✓ Session persistence verified! Step 2 seamlessly restored from sessionStorage.")

        print("\n[TEST 5] Testing Unhappy Path: Wrong 6-digit OTP code (000000)...")
        otp_inputs = await page.query_selector_all(".otp-digit")
        assert len(otp_inputs) == 6, f"Expected 6 OTP digit inputs, found {len(otp_inputs)}"
        for i in range(6):
            await otp_inputs[i].fill("0")

        async with page.expect_response(lambda r: "/api/auth/verify-email" in r.url and r.status == 400) as err_resp_info:
            await page.click("#btnVerifySubmit")
        err_res = await err_resp_info.value
        err_json = await err_res.json()
        assert err_json["success"] is False
        assert "remainingAttempts" in err_json
        rem_attempts = err_json["remainingAttempts"]
        print(f"  ✓ HTTP 400 received as expected for wrong code. Remaining attempts: {rem_attempts}")

        await page.wait_for_selector("#otpStatusAlert", state="visible")
        alert_text = await page.text_content("#otpStatusAlert")
        assert "không chính xác" in alert_text or "lần thử" in alert_text
        print(f"  ✓ Error alert displayed clearly to user: '{alert_text.strip()}'.")

        await page.screenshot(path="C:/Users/MSIIIIII/.gemini/antigravity/brain/2d768ef8-fc1d-49c3-94fc-8635ba4a8e61/shot_step2_otp_error.png")
        print("  ✓ Screenshot saved: shot_step2_otp_error.png")

        print("\n[TEST 6] Querying correct OTP code from database EmailOutbox...")
        real_otp = query_otp_from_email_outbox(test_email)
        print(f"  ✓ Retrieved real OTP code from SQL Server EmailOutbox: {real_otp}")

        print("\n[TEST 7] Testing Happy Path: Entering correct OTP code and completing registration...")
        # Since entering the 6th digit auto-submits, we expect verify-email response while filling the digits
        async with page.expect_response(lambda r: "/api/auth/verify-email" in r.url and r.status == 200) as success_resp_info:
            for i, digit in enumerate(real_otp):
                await otp_inputs[i].fill(digit)
        succ_res = await success_resp_info.value
        succ_json = await succ_res.json()
        assert succ_json["success"] is True
        assert "token" in succ_json and succ_json["token"]
        assert "user" in succ_json
        print(f"  ✓ HTTP 200 OK! Verification successful! Token generated: {succ_json['token'][:25]}...")

        # Wait for redirect to profile.html
        await page.wait_for_url("**/profile.html", timeout=6000)
        print(f"  ✓ Automatically redirected to: {page.url}")

        # Check localStorage token & user
        stored_token = await page.evaluate("() => localStorage.getItem('journal_token')")
        stored_user = await page.evaluate("() => localStorage.getItem('journal_user')")
        assert stored_token is not None and len(stored_token) > 20
        assert "Nguyễn Hoàng Nam" in stored_user
        print(f"  ✓ LocalStorage authenticated user verified: {stored_user}")

        await page.screenshot(path="C:/Users/MSIIIIII/.gemini/antigravity/brain/2d768ef8-fc1d-49c3-94fc-8635ba4a8e61/shot_step2_profile_redirect.png")
        print("  ✓ Screenshot saved: shot_step2_profile_redirect.png")

        print("\n[TEST 8] Testing 'Sửa thông tin hoặc đổi email' navigation...")
        # Start a new registration flow
        await page.evaluate("() => { localStorage.clear(); sessionStorage.clear(); }")
        await page.goto("http://localhost:8088/register.html", wait_until="networkidle")

        ts2 = int(time.time()) + 1
        test_user2 = f"p4edit_{ts2}"
        test_email2 = f"p4edit_{ts2}@huit.edu.vn"

        await page.fill("#regHoDem", "Trần Đình")
        await page.fill("#regTen", "Trọng")
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Không")
        await page.fill("#regSoDienThoai", "0987654321")
        await page.fill("#regDonVi", "HUIT")
        await page.fill("#regDiaChi", "TP.HCM")
        await page.fill("#regTenDangNhap", test_user2)
        await page.fill("#regEmail", test_email2)
        await page.fill("#regPassword", "HuitSecure@2026")
        await page.fill("#regConfirmPassword", "HuitSecure@2026")
        await page.check("#regTerms")

        async with page.expect_response(lambda r: "/api/auth/register" in r.url and r.status == 202):
            await page.click("button.btn-auth-submit")

        await page.wait_for_selector("#register-step-2", state="visible")
        print("  ✓ Reached Step 2. Now clicking 'Sửa thông tin hoặc đổi email'...")

        await page.click("button.btn-back-step1")
        await page.wait_for_selector("#register-step-1", state="visible")
        back_step1_disp = await page.is_visible("#register-step-1")
        back_step2_disp = await page.is_visible("#register-step-2")
        assert back_step1_disp is True, "Step 1 should be visible after clicking back"
        assert back_step2_disp is False, "Step 2 should be hidden after clicking back"

        # Check preserved draft
        email_val = await page.input_value("#regEmail")
        assert email_val == test_email2, f"Expected {test_email2}, got {email_val}"
        print(f"  ✓ Step 1 restored with intact draft value: email = '{email_val}'.")

        print("\n[TEST 9] Testing Mobile Viewport Responsiveness (375x812)...")
        await page.set_viewport_size({"width": 375, "height": 812})
        # Submit to Step 2 again to test mobile view of OTP screen
        async with page.expect_response(lambda r: "/api/auth/register" in r.url and r.status == 202):
            await page.click("button.btn-auth-submit")
        await page.wait_for_selector("#register-step-2", state="visible")
        await page.eval_on_selector(".auth-box", "el => el.scrollIntoView()")
        await page.screenshot(path="C:/Users/MSIIIIII/.gemini/antigravity/brain/2d768ef8-fc1d-49c3-94fc-8635ba4a8e61/shot_step2_mobile_view.png")
        print("  ✓ Screenshot saved: shot_step2_mobile_view.png")

        print("\n" + "=" * 70)
        print("ALL 9 PHASE 4 E2E TEST SCENARIOS PASSED 100%!")
        print("=" * 70)

        await browser.close()

if __name__ == "__main__":
    asyncio.run(run_phase4_e2e_tests())
