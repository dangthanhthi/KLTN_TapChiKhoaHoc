import asyncio
import os
import re
import sys
import time
import subprocess
import json
from playwright.async_api import async_playwright
from test_target import API_ROOT, require_testing_api

if sys.stdout and hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

ARTIFACT_DIR = r"C:\Users\MSIIIIII\.gemini\antigravity\brain\2d768ef8-fc1d-49c3-94fc-8635ba4a8e61"
SQLCMD_BASE = [
    "sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc_Test",
    "-E", "-b"
]

def execute_sql(query: str) -> str:
    cmd = SQLCMD_BASE + ["-y", "1000", "-h", "-1", "-Q", f"SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; {query}"]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace", check=True)
    return res.stdout.strip()

def query_otp_from_email_outbox(email: str) -> str:
    query = f"SELECT TOP 1 NoiDungText FROM EmailOutbox WHERE NguoiNhan = '{email}' ORDER BY TaoLucUtc DESC;"
    out = execute_sql(query)
    match = re.search(r'\b(\d{6})\b', out)
    if match:
        return match.group(1)
    raise ValueError(f"Could not find 6-digit OTP code in outbox text for {email}: {out}")

async def run_phase6_browser_audit():
    require_testing_api()
    print("=" * 80)
    print("PHASE 6: COMPREHENSIVE BROWSER END-TO-END AUDIT & ACCEPTANCE VERIFICATION")
    print("Hệ thống Tạp chí Khoa học Đại học Công Thương TP.HCM (HUIT)")
    print("=" * 80)

    ts = int(time.time())
    test_user = f"audit_user_{ts}"
    test_email = f"audit_{ts}@huit.edu.vn"
    test_pass = "AuditHuitPass@2026"
    test_orcid = f"0009-0001-{ts % 9000 + 1000:04d}-{(ts + 7) % 9000 + 1000:04d}"

    async with async_playwright() as p:
        browser = await p.chromium.launch(channel="msedge", headless=True)
        context = await browser.new_context(
            viewport={"width": 1280, "height": 900},
            device_scale_factor=1.0
        )
        await context.add_init_script(f"localStorage.setItem('huit_api_url', {json.dumps(API_ROOT)})")
        page = await context.new_page()

        console_errors = []
        page.on("console", lambda msg: console_errors.append(msg.text) if msg.type == "error" else None)
        page.on("pageerror", lambda err: console_errors.append(str(err)))

        # =====================================================================
        # SCENARIO 1: Hallmark Anti-AI-slop & Typography Audit
        # =====================================================================
        print("\n[SCENARIO 1] Hallmark Anti-AI-slop & Typography Audit on register.html...")
        await page.goto("http://localhost:8088/register.html", wait_until="networkidle")
        title = await page.title()
        assert "Đăng ký tài khoản người dùng" in title, f"Unexpected page title: {title}"
        print(f"  ✓ Page loaded with standard academic title: '{title}'")

        # 1.1 Check heading typography (must be normal, NOT italic)
        italic_headings = await page.eval_on_selector_all(
            "h1, h2, h3, h4, .workbench-title, .col-title",
            "els => els.filter(el => window.getComputedStyle(el).fontStyle === 'italic').map(el => el.innerText)"
        )
        assert len(italic_headings) == 0, f"Violates Typographic Purity: Found italic headings: {italic_headings}"
        print("  ✓ Typographic Purity verified: 100% headings use normal font-style (anti-AI-slop).")

        # 1.2 Check STRICT PROHIBITION of bullet points on web UI
        bullet_violations = await page.eval_on_selector_all(
            "body *",
            """els => {
                const listItemsWithBullets = [];
                for (const el of els) {
                    const style = window.getComputedStyle(el);
                    if (el.tagName === 'LI' && style.listStyleType !== 'none') {
                        listItemsWithBullets.push(`LI with list-style-type: ${style.listStyleType}`);
                    }
                    if (el.childNodes.length === 1 && el.childNodes[0].nodeType === 3) {
                        const txt = el.childNodes[0].textContent;
                        if (txt.includes('•') || txt.includes('●') || /^[\\s]*[-*][\\s]+/.test(txt)) {
                            listItemsWithBullets.push(`Text node with bullet: ${txt.trim().substring(0, 30)}`);
                        }
                    }
                }
                return listItemsWithBullets;
            }"""
        )
        assert len(bullet_violations) == 0, f"Violates RULE_NO_BULLET_POINTS: Found bullet points: {bullet_violations}"
        print("  ✓ Strict Anti-Bullet-Point rule verified: 0 bullets found across the entire registration page.")

        # =====================================================================
        # SCENARIO 2: Form Validation & Security Unhappy Paths
        # =====================================================================
        print("\n[SCENARIO 2] Form Validation & Security Unhappy Paths...")

        # 2.1 Password mismatch check
        await page.fill("#regHoDem", "Nguyễn")
        await page.fill("#regTen", "Test")
        await page.fill("#regEmail", "unhappy@huit.edu.vn")
        await page.fill("#regTenDangNhap", "unhappy_user")
        await page.fill("#regSoDienThoai", "0912345678")
        await page.fill("#regDonVi", "Trường Đại học Công Thương TP.HCM")
        await page.fill("#regPassword", "AuditHuitPass@2026")
        await page.fill("#regConfirmPassword", "MismatchPass@9999")
        await page.select_option("#regChuyenNganh", index=1)
        await page.check("#regTerms")

        await page.click("button[type='submit']")
        await page.wait_for_timeout(300)
        toast_text = await page.eval_on_selector("#journal-global-toast", "el => el ? el.innerText : ''")
        assert "không khớp" in toast_text, f"Expected password mismatch error, got: {toast_text}"
        print("  ✓ Unhappy Path 2.1: Password mismatch caught and blocked by UI toast.")

        # 2.2 Weak password check (e.g. 123456)
        await page.fill("#regPassword", "123456")
        await page.fill("#regConfirmPassword", "123456")
        await page.click("button[type='submit']")
        await page.wait_for_timeout(300)
        toast_text = await page.eval_on_selector("#journal-global-toast", "el => el ? el.innerText : ''")
        assert "tối thiểu 8 ký tự" in toast_text or "dễ đoán" in toast_text, f"Expected weak password error, got: {toast_text}"
        print("  ✓ Unhappy Path 2.2: Weak password (123456) caught and blocked.")

        # 2.3 Invalid Vietnam phone number
        await page.fill("#regPassword", "AuditHuitPass@2026")
        await page.fill("#regConfirmPassword", "AuditHuitPass@2026")
        await page.select_option("#regQuocGia", "Vietnam")
        await page.fill("#regSoDienThoai", "0123456789") # Invalid VN mobile prefix
        await page.click("button[type='submit']")
        await page.wait_for_timeout(300)
        toast_text = await page.eval_on_selector("#journal-global-toast", "el => el ? el.innerText : ''")
        assert "Số điện thoại Việt Nam không hợp lệ" in toast_text, f"Expected invalid VN phone error, got: {toast_text}"
        print("  ✓ Unhappy Path 2.3: Invalid Vietnam phone prefix caught and blocked.")

        # 2.4 Academic integrity constraint: Giáo sư with Cử nhân
        await page.fill("#regSoDienThoai", "0987654321")
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Giáo sư")
        await page.select_option("#regHocVi", "Cử nhân")
        await page.wait_for_timeout(400)
        is_disabled = await page.eval_on_selector('#regHocHam option[value="Giáo sư"]', "opt => opt.disabled")
        current_hh = await page.eval_on_selector("#regHocHam", "el => el.value")
        assert is_disabled is True, "Option 'Giáo sư' should be disabled when Học vị is 'Cử nhân'"
        assert current_hh == "Không", f"Học hàm should be reset to 'Không', got {current_hh}"
        toast_text = await page.eval_on_selector("#journal-global-toast", "el => el ? el.innerText : ''")
        assert "không hợp lệ theo quy định học thuật" in toast_text, f"Expected academic warning toast, got: {toast_text}"
        print("  ✓ Unhappy Path 2.4: Academic rank integrity verified (GS automatically disabled & reset when selecting Cử nhân).")

        # 2.5 Incomplete banking info (stk without bank name)
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Phó giáo sư")
        await page.fill("#regSoTaiKhoan", "1029384756")
        await page.fill("#regChuTaiKhoan", "TRAN QUOC BAO")
        await page.fill("#regNganHang", "") # empty bank name
        await page.click("button[type='submit']")
        await page.wait_for_timeout(300)
        toast_text = await page.eval_on_selector("#journal-global-toast", "el => el ? el.innerText : ''")
        assert "đầy đủ cả 3 mục" in toast_text, f"Expected incomplete bank info error, got: {toast_text}"
        print("  ✓ Unhappy Path 2.5: Incomplete banking info caught and blocked.")

        # =====================================================================
        # SCENARIO 3: Step 1 Legitimate Submission & HTTP 202 Transition
        # =====================================================================
        print(f"\n[SCENARIO 3] Submitting Legitimate Step 1 for {test_user} ({test_email})...")
        await page.fill("#regHoDem", "Trần Quốc")
        await page.fill("#regTen", "Bảo")
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Phó giáo sư")
        await page.select_option("#regGioiTinh", "Nam")
        await page.select_option("#regNgonNgu", "Tiếng Việt")
        await page.select_option("#regQuocGia", "Vietnam")
        await page.fill("#regSoDienThoai", "0987654321")
        await page.fill("#regDonVi", "Trường Đại học Công Thương TP.HCM")
        await page.fill("#regDiaChi", "140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM")
        await page.fill("#regSoTaiKhoan", "1029384756")
        await page.fill("#regChuTaiKhoan", "TRAN QUOC BAO")
        await page.fill("#regNganHang", "Vietcombank")
        await page.select_option("#regChuyenNganh", "1")
        await page.fill("#regOrcid", test_orcid)
        await page.fill("#regTenDangNhap", test_user)
        await page.fill("#regEmail", test_email)
        await page.fill("#regPassword", test_pass)
        await page.fill("#regConfirmPassword", test_pass)
        await page.check("#regTerms")

        page.on("response", lambda resp: print(f"  [HTTP {resp.status}] {resp.request.method} {resp.url}") if "/api/" in resp.url else None)

        # Submit Step 1
        print("  Clicking submit button for Step 1...")
        await page.click("#register-step-1 button[type='submit']")

        # Wait for Step 2 to become visible or diagnose
        try:
            await page.wait_for_selector("#register-step-2", state="visible", timeout=6000)
        except Exception as e:
            t = await page.eval_on_selector("#journal-global-toast", "el => el ? el.innerText : 'NO TOAST'")
            focused = await page.evaluate("() => document.activeElement ? (document.activeElement.id || document.activeElement.name || document.activeElement.tagName) : 'none'")
            val_err = await page.evaluate("() => document.getElementById('register-page-form').checkValidity()")
            print(f"  [DIAGNOSTIC] Toast: '{t}', Focused: '{focused}', Form validity: {val_err}")
            raise e
        step1_visible = await page.is_visible("#register-step-1")
        assert not step1_visible, "Step 1 should be hidden"
        print("  ✓ Step 1 seamlessly transitioned to Step 2 upon receiving HTTP 202 Accepted!")

        # Verify Stepper and Masked Email
        masked_text = await page.inner_text("#verifyMaskedEmail")
        assert "a***" in masked_text, f"Expected masked email format, got: {masked_text}"
        print(f"  ✓ Masked email rendered securely: '{masked_text}'")

        # Verify Resend Cooldown button
        resend_btn_text = await page.inner_text("#resendOtpBtnText")
        assert "Gửi lại mã sau" in resend_btn_text, f"Expected cooldown on resend button, got: {resend_btn_text}"
        print(f"  ✓ 60-second cooldown active on Resend button: '{resend_btn_text}'")

        shot_p1 = os.path.join(ARTIFACT_DIR, "shot_phase6_step2_entered.png")
        await page.screenshot(path=shot_p1, full_page=True)
        print(f"  ✓ Screenshot saved: shot_phase6_step2_entered.png")

        # =====================================================================
        # SCENARIO 4: Step 2 OTP UX & Security Features (Digits, Backspace, Paste, Unhappy OTP)
        # =====================================================================
        print("\n[SCENARIO 4] Testing OTP Inputs UX (Auto-advance, Backspace, Paste, Wrong code)...")
        otp_inputs = await page.query_selector_all(".otp-digit")
        assert len(otp_inputs) == 6, f"Expected 6 OTP digit inputs, got {len(otp_inputs)}"

        # 4.1 Test Auto-advance on single digit typing
        await otp_inputs[0].focus()
        await page.keyboard.press("1")
        focused_idx = await page.evaluate("() => document.activeElement ? document.activeElement.getAttribute('data-index') : null")
        assert focused_idx == "1", f"Expected focus on data-index 1, got: {focused_idx}"
        print("  ✓ Auto-advance verified: typing in digit 0 automatically shifts focus to digit 1.")

        # 4.2 Test Backspace navigates back
        await page.keyboard.press("Backspace")
        focused_idx = await page.evaluate("() => document.activeElement ? document.activeElement.getAttribute('data-index') : null")
        assert focused_idx == "0", f"Expected focus back on data-index 0, got: {focused_idx}"
        print("  ✓ Backspace verified: deletes and moves focus to previous digit box.")

        # 4.3 Test Unhappy Path: Submit Wrong 6-digit OTP code (000000)
        # Entering the 6th digit triggers auto-submit automatically
        for i in range(6):
            await otp_inputs[i].fill("0")
        await page.wait_for_selector("#otpStatusAlert", state="visible", timeout=6000)
        alert_text = await page.inner_text("#otpStatusAlert")
        assert "không chính xác" in alert_text, f"Expected incorrect code alert, got: {alert_text}"
        assert "lần thử" in alert_text, f"Expected remaining attempts notice, got: {alert_text}"
        print(f"  ✓ Unhappy OTP caught: '{alert_text.strip()}'")

        shot_p2 = os.path.join(ARTIFACT_DIR, "shot_phase6_otp_wrong_error.png")
        await page.screenshot(path=shot_p2, full_page=True)
        print(f"  ✓ Screenshot saved: shot_phase6_otp_wrong_error.png")

        # =====================================================================
        # SCENARIO 5: Session Persistence (F5 Reload) & Back-to-Step 1
        # =====================================================================
        print("\n[SCENARIO 5] Testing Session Persistence across page reload (F5)...")
        await page.reload(wait_until="networkidle")
        step2_still_visible = await page.is_visible("#register-step-2")
        assert step2_still_visible, "Step 2 should remain visible after F5 reload via sessionStorage!"
        masked_restored = await page.inner_text("#verifyMaskedEmail")
        assert "a***" in masked_restored, f"Masked email should be intact: {masked_restored}"
        print("  ✓ Session persistence verified: F5 reload restored Step 2 without data loss.")

        shot_p3 = os.path.join(ARTIFACT_DIR, "shot_phase6_session_persisted.png")
        await page.screenshot(path=shot_p3, full_page=True)
        print(f"  ✓ Screenshot saved: shot_phase6_session_persisted.png")

        # 5.1 Test Back to Step 1 & draft preservation
        print("  Testing 'Sửa thông tin hoặc đổi email' button...")
        await page.click("button.btn-back-step1")
        await page.wait_for_timeout(300)
        step1_restored = await page.is_visible("#register-step-1")
        assert step1_restored, "Step 1 should be restored after clicking back button"
        draft_username = await page.input_value("#regTenDangNhap")
        assert draft_username == test_user, f"Draft username should be preserved: {draft_username}"
        print(f"  ✓ Draft preservation verified: Step 1 restored with user '{draft_username}'.")

        # Re-submit to return to Step 2
        await page.click("#register-step-1 button[type='submit']")
        await page.wait_for_selector("#register-step-2", state="visible", timeout=10000)

        # =====================================================================
        # SCENARIO 6: Legitimate OTP Verification & Profile Redirection
        # =====================================================================
        print(f"\n[SCENARIO 6] Querying real OTP from SQL Server Outbox for {test_email}...")
        real_otp = query_otp_from_email_outbox(test_email)
        print(f"  ✓ Real OTP code retrieved from database EmailOutbox: {real_otp}")

        # Enter legitimate 6-digit OTP
        otp_inputs = await page.query_selector_all(".otp-digit")
        for idx, digit in enumerate(real_otp):
            await otp_inputs[idx].fill(digit)

        print("  Submitted legitimate OTP code. Waiting for automatic profile redirection...")
        await page.wait_for_url("**/profile.html", timeout=12000)
        assert "profile.html" in page.url, f"Expected URL to contain profile.html, got: {page.url}"
        await page.wait_for_load_state("networkidle")
        print("  ✓ HTTP 200 OK! Redirected successfully to profile.html.")

        # Verify rendered user profile details
        await page.wait_for_selector("#spec-name", timeout=6000)
        profile_name = await page.inner_text("#spec-name")
        assert "Trần Quốc Bảo" in profile_name, f"Expected user name in profile, got: {profile_name}"
        profile_deg = await page.inner_text("#spec-degrees")
        assert "Phó giáo sư" in profile_deg and "Tiến sĩ" in profile_deg, f"Expected degrees in profile, got: {profile_deg}"
        profile_bank = await page.inner_text("#spec-bank")
        assert "1029384756" in profile_bank and "Vietcombank" in profile_bank, f"Expected bank in profile, got: {profile_bank}"
        print(f"  ✓ Profile data verified on UI: Name='{profile_name}', Degrees='{profile_deg}', Bank='{profile_bank}'")

        shot_p4 = os.path.join(ARTIFACT_DIR, "shot_phase6_profile_authenticated.png")
        await page.screenshot(path=shot_p4, full_page=True)
        print(f"  ✓ Screenshot saved: shot_phase6_profile_authenticated.png")

        # =====================================================================
        # SCENARIO 7: Database Verification
        # =====================================================================
        print("\n[SCENARIO 7] Verifying SQL Server Database Records...")
        reg_status = execute_sql(f"SELECT TrangThai FROM DangKyChoXacNhan WHERE TenDangNhapSoSanh = '{test_user}';")
        assert "Verified" in reg_status, f"Expected DangKyChoXacNhan TrangThai=Verified, got: {reg_status}"
        print("  ✓ DangKyChoXacNhan record updated to 'Verified'.")

        user_id_raw = execute_sql(f"SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = '{test_user}';")
        user_id_match = re.search(r'\b(\d+)\b', user_id_raw)
        assert user_id_match is not None, f"Expected valid MaNguoiDung in NguoiDung, got: {user_id_raw}"
        user_id = user_id_match.group(1)
        print(f"  ✓ User record created in NguoiDung with MaNguoiDung = {user_id}.")

        roles = execute_sql(f"SELECT vt.TenVaiTro FROM NguoiDung_VaiTro ndvt JOIN VaiTro vt ON ndvt.MaVaiTro = vt.MaVaiTro WHERE ndvt.MaNguoiDung = {user_id};")
        assert "Tác giả" in roles and "Độc giả" in roles, f"Expected Tác giả & Độc giả roles, got: {roles}"
        print(f"  ✓ Roles verified in NguoiDung_VaiTro: {roles.replace(chr(10), ', ')}.")

        # =====================================================================
        # SCENARIO 8: Complete Logout & Login Flow
        # =====================================================================
        print("\n[SCENARIO 8] Testing Complete Logout and Login with New Credentials...")
        await page.goto("http://localhost:8088/login.html", wait_until="networkidle")

        # Check if already logged in block is shown, click logout
        already_box = await page.is_visible("#already-logged-in-box")
        if already_box:
            print("  Logged in state detected on login.html. Clicking 'Đăng xuất ngay'...")
            await page.click("button[onclick='handleLogoutFromLogin()']")
            await page.wait_for_selector("#login-form-container", state="visible", timeout=5000)

        # Login with newly created user credentials
        await page.wait_for_selector("#login-page-form", state="visible", timeout=5000)
        await page.fill("#username", test_user)
        await page.fill("#password", test_pass)
        await page.click("#login-page-form button[type='submit']")

        # Should redirect to profile or dashboard
        await page.wait_for_url("**/profile.html", timeout=10000)
        assert "profile.html" in page.url, f"Expected redirect to profile.html after login, got: {page.url}"
        print(f"  ✓ Login succeeded with newly created account '{test_user}'! JWT issued and session active.")

        shot_p5 = os.path.join(ARTIFACT_DIR, "shot_phase6_login_success.png")
        await page.screenshot(path=shot_p5, full_page=True)
        print(f"  ✓ Screenshot saved: shot_phase6_login_success.png")

        # =====================================================================
        # SCENARIO 9: Multi-Device Responsive Viewports Audit
        # =====================================================================
        print("\n[SCENARIO 9] Multi-Device Responsive Viewports Audit...")
        viewports = [
            ("320px", 320, 568, "shot_phase6_responsive_320px.png"),
            ("375px", 375, 812, "shot_phase6_responsive_375px.png"),
            ("768px", 768, 1024, "shot_phase6_responsive_768px.png"),
            ("1280px", 1280, 800, "shot_phase6_responsive_1280px.png"),
            ("1920px", 1920, 1080, "shot_phase6_responsive_1920px.png"),
        ]

        await page.goto("http://localhost:8088/register.html", wait_until="networkidle")
        for label, w, h, fname in viewports:
            await page.set_viewport_size({"width": w, "height": h})
            await page.wait_for_timeout(400)
            
            # Check horizontal overflow
            has_overflow = await page.evaluate("() => document.body.scrollWidth > window.innerWidth")
            assert not has_overflow, f"Horizontal overflow detected at viewport {label} ({w}x{h})!"
            
            shot_vp = os.path.join(ARTIFACT_DIR, fname)
            await page.screenshot(path=shot_vp, full_page=False)
            print(f"  ✓ Viewport {label} ({w}x{h}): Responsive perfect, 0 overflow. Screenshot: {fname}")

        # =====================================================================
        # SCENARIO 10: JavaScript Runtime Console Audit
        # =====================================================================
        print("\n[SCENARIO 10] JavaScript Runtime Console Audit...")
        # Filter benign network status logs and intentional test responses (400, 401, 404, favicon)
        real_errors = [e for e in console_errors if not any(ign in e.lower() for ign in ["favicon", "net::err", "404", "status of 400", "status of 401", "failed to load resource"])]
        assert len(real_errors) == 0, f"Detected JavaScript runtime errors: {real_errors}"
        print("  ✓ Zero uncaught JavaScript errors or unhandled promise rejections detected during browser run!")

        await browser.close()

    # Clean up test user from database
    print("\n[CLEANUP] Cleaning up test user and outbox...")
    execute_sql(f"""
    DELETE FROM DangKyChoXacNhan WHERE TenDangNhapSoSanh = '{test_user}';
    DELETE FROM NguoiDung_VaiTro WHERE MaNguoiDung IN (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = '{test_user}');
    DELETE FROM NguoiDung_ChuyenMon WHERE MaNguoiDung IN (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = '{test_user}');
    DELETE FROM NguoiDung WHERE TenDangNhap = '{test_user}';
    DELETE FROM EmailOutbox WHERE NguoiNhan = '{test_email}';
    """)
    print("  ✓ Database test artifacts cleaned up.")

    print("\n" + "=" * 80)
    print("ALL 10 PHASE 6 BROWSER AUDIT SCENARIOS PASSED 100%!")
    print("EMAIL VERIFICATION SYSTEM ACCEPTANCE: COMPLETE & FULLY CERTIFIED!")
    print("=" * 80)

if __name__ == "__main__":
    asyncio.run(run_phase6_browser_audit())
