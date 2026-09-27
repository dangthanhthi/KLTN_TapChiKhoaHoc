import asyncio
import json
import os
import sys
import time
import urllib.request
import urllib.error
import uuid

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')

from playwright.async_api import async_playwright

API_BASE = "http://localhost:5000/api/auth"
WEB_BASE = "http://localhost:8088"

def make_post(endpoint, data):
    url = f"{API_BASE}/{endpoint}"
    req = urllib.request.Request(
        url,
        data=json.dumps(data).encode('utf-8'),
        headers={'Content-Type': 'application/json'},
        method='POST'
    )
    with urllib.request.urlopen(req) as resp:
        return resp.getcode(), json.loads(resp.read().decode('utf-8'))

def test_api_happy_paths():
    print("=" * 80)
    print("PHẦN 1: KIỂM THỬ BACKEND API THẬT - CÁC KỊCH BẢN ĐĂNG KÝ HỢP LỆ (HAPPY PATHS)")
    print("=" * 80)

    # -------------------------------------------------------------------------
    # KỊCH BẢN 1: Tác giả tiêu chuẩn (Thạc sĩ, chuyên ngành CNTT, có tài khoản ngân hàng)
    # -------------------------------------------------------------------------
    print("\n[KỊCH BẢN 1] Đăng ký Tác giả chuẩn (Thạc sĩ, CNTT & AI, đầy đủ tài khoản ngân hàng)...")
    uid1 = uuid.uuid4().hex[:8]
    data1 = {
        "tenDangNhap": f"author_{uid1}",
        "hoDem": "Nguyễn Hoàng",
        "ten": "Minh",
        "hoTen": "Nguyễn Hoàng Minh",
        "email": f"minh.nh_{uid1}@huit.edu.vn",
        "password": "Password123@",
        "hocVi": "Thạc sĩ",
        "hocHam": "Không",
        "gioiTinh": "Nam",
        "quocGia": "Vietnam",
        "ngonNgu": "Tiếng Việt",
        "soDienThoai": "0981234567",
        "donVi": "Khoa Công nghệ Thông tin, Đại học Công Thương TP.HCM",
        "diaChi": "140 Lê Trọng Tấn, Tân Phú, TP.HCM",
        "soTaiKhoan": "1029384756",
        "chuTaiKhoan": "NGUYEN HOANG MINH",
        "nganHang": "Vietcombank Tân Bình",
        "chuyenNganhId": 1,
        "dangKyPhanBien": False
    }
    code1, res1 = make_post("register", data1)
    print(f"  -> HTTP Code: {code1} | Success: {res1.get('success')}")
    assert code1 == 200 and res1.get("success"), f"Thất bại Kịch bản 1: {res1}"
    assert res1.get("token"), "Phải có JWT Token"
    user1 = res1.get("user", {})
    assert "Tác giả" in user1.get("vaiTros", []), "Phải có vai trò Tác giả"
    assert "Độc giả" in user1.get("vaiTros", []), "Phải có vai trò Độc giả"
    assert user1.get("hocVi") == "Thạc sĩ"
    u_id1 = user1.get("maNguoiDung") or user1.get("id")
    print(f"  [✓] Đăng ký thành công Tác giả ID #{u_id1} - Token: {res1.get('token')[:20]}...")

    # -------------------------------------------------------------------------
    # KỊCH BẢN 2: Chuyên gia cấp cao (Tiến sĩ - Phó Giáo Sư, Cơ khí, có ORCID đầy đủ URL)
    # -------------------------------------------------------------------------
    print("\n[KỊCH BẢN 2] Đăng ký Chuyên gia (Tiến sĩ - PGS, Cơ khí, URL ORCID)...")
    uid2 = uuid.uuid4().hex[:8]
    orcid_tail = f"{int(time.time()) % 9000 + 1000:04d}"
    data2 = {
        "tenDangNhap": f"pgs_{uid2}",
        "hoDem": "Lê Quang",
        "ten": "Đức",
        "hoTen": "Lê Quang Đức",
        "email": f"duc.lq_{uid2}@huit.edu.vn",
        "password": "SecurePassword123@",
        "hocVi": "Tiến sĩ",
        "hocHam": "Phó giáo sư",
        "gioiTinh": "Nam",
        "quocGia": "Vietnam",
        "ngonNgu": "Tiếng Việt",
        "soDienThoai": "+84903456789",
        "donVi": "Khoa Cơ khí, Trường Đại học Bách Khoa TP.HCM",
        "diaChi": "268 Lý Thường Kiệt, Q.10, TP.HCM",
        "soTaiKhoan": "0071001234567",
        "chuTaiKhoan": "LE QUANG DUC",
        "nganHang": "Vietcombank TP.HCM",
        "chuyenNganhId": 2,
        "maORCID": f"https://orcid.org/0000-0002-{orcid_tail}-1234",
        "dangKyPhanBien": True
    }
    code2, res2 = make_post("register", data2)
    print(f"  -> HTTP Code: {code2} | Success: {res2.get('success')}")
    assert code2 == 200 and res2.get("success"), f"Thất bại Kịch bản 2: {res2}"
    user2 = res2.get("user", {})
    assert user2.get("hocHam") == "Phó giáo sư"
    assert user2.get("hocVi") == "Tiến sĩ"
    assert user2.get("maORCID") == f"0000-0002-{orcid_tail}-1234", "Mã ORCID phải được chuẩn hóa bỏ prefix https://orcid.org/"
    u_id2 = user2.get("maNguoiDung") or user2.get("id")
    print(f"  [✓] Đăng ký thành công PGS.TS ID #{u_id2} - Chuẩn hóa ORCID: {user2.get('maORCID')}")

    # -------------------------------------------------------------------------
    # KỊCH BẢN 3: Nhà khoa học cấp Giáo sư (Tiến sĩ - Giáo sư, Khoa học Môi trường)
    # -------------------------------------------------------------------------
    print("\n[KỊCH BẢN 3] Đăng ký Nhà khoa học cấp cao (Tiến sĩ - Giáo sư, Môi trường)...")
    uid3 = uuid.uuid4().hex[:8]
    data3 = {
        "tenDangNhap": f"prof_{uid3}",
        "hoDem": "Phạm Thị",
        "ten": "Hoa",
        "hoTen": "Phạm Thị Hoa",
        "email": f"hoa.pt_{uid3}@huit.edu.vn",
        "password": "Password789!",
        "hocVi": "Tiến sĩ",
        "hocHam": "Giáo sư",
        "gioiTinh": "Nữ",
        "quocGia": "Vietnam",
        "ngonNgu": "Tiếng Việt",
        "soDienThoai": "0918765432",
        "donVi": "Viện Tài nguyên & Môi trường, Đại học Quốc gia TP.HCM",
        "diaChi": "Khu đô thị ĐHQG-HCM, Dĩ An, Bình Dương",
        "soTaiKhoan": "1234567890123",
        "chuTaiKhoan": "PHAM THI HOA",
        "nganHang": "BIDV Đông Sài Gòn",
        "chuyenNganhId": 3,
        "dangKyPhanBien": True
    }
    code3, res3 = make_post("register", data3)
    print(f"  -> HTTP Code: {code3} | Success: {res3.get('success')}")
    assert code3 == 200 and res3.get("success"), f"Thất bại Kịch bản 3: {res3}"
    user3 = res3.get("user", {})
    assert user3.get("hocHam") == "Giáo sư"
    u_id3 = user3.get("maNguoiDung") or user3.get("id")
    print(f"  [✓] Đăng ký thành công Giáo sư ID #{u_id3}: {user3.get('hoTen')}")

    # -------------------------------------------------------------------------
    # KỊCH BẢN 4: Tác giả trẻ / Sinh viên / Kỹ sư (Cử nhân, Kinh tế, để trống ngân hàng tùy chọn)
    # -------------------------------------------------------------------------
    print("\n[KỊCH BẢN 4] Đăng ký Tác giả trẻ (Cử nhân, Kinh tế, các trường tùy chọn để trống hợp lệ)...")
    uid4 = uuid.uuid4().hex[:8]
    data4 = {
        "tenDangNhap": f"young_{uid4}",
        "hoDem": "Trần Bảo",
        "ten": "An",
        "hoTen": "Trần Bảo An",
        "email": f"an.tb_{uid4}@gmail.com",
        "password": "Password2026@",
        "hocVi": "Cử nhân",
        "hocHam": "Không",
        "gioiTinh": "Nam",
        "quocGia": "Vietnam",
        "ngonNgu": "Tiếng Việt",
        "donVi": "Khoa Quản trị Kinh doanh - HUIT",
        "chuyenNganhId": 4,
        "dangKyPhanBien": False
    }
    code4, res4 = make_post("register", data4)
    print(f"  -> HTTP Code: {code4} | Success: {res4.get('success')}")
    assert code4 == 200 and res4.get("success"), f"Thất bại Kịch bản 4: {res4}"
    user4 = res4.get("user", {})
    assert user4.get("hocVi") == "Cử nhân"
    assert user4.get("soTaiKhoan") is None
    u_id4 = user4.get("maNguoiDung") or user4.get("id")
    print(f"  [✓] Đăng ký thành công Tác giả trẻ ID #{u_id4}: {user4.get('hoTen')}")

    # -------------------------------------------------------------------------
    # KỊCH BẢN 5: Nhà nghiên cứu Quốc tế (English language, United States, Hóa học - Thực phẩm)
    # -------------------------------------------------------------------------
    print("\n[KỊCH BẢN 5] Đăng ký Nhà nghiên cứu Quốc tế (United States, English, Thực phẩm)...")
    uid5 = uuid.uuid4().hex[:8]
    data5 = {
        "tenDangNhap": f"john_{uid5}",
        "hoDem": "John",
        "ten": "Smith",
        "hoTen": "John Smith",
        "email": f"john.smith_{uid5}@stanford.edu",
        "password": "IntlResearcher99#",
        "hocVi": "Tiến sĩ",
        "hocHam": "Không",
        "gioiTinh": "Nam",
        "quocGia": "United States",
        "ngonNgu": "Tiếng Anh",
        "donVi": "Department of Chemical Engineering, Stanford University",
        "diaChi": "Stanford, CA 94305, USA",
        "chuyenNganhId": 5,
        "dangKyPhanBien": True
    }
    code5, res5 = make_post("register", data5)
    print(f"  -> HTTP Code: {code5} | Success: {res5.get('success')}")
    assert code5 == 200 and res5.get("success"), f"Thất bại Kịch bản 5: {res5}"
    user5 = res5.get("user", {})
    assert user5.get("quocGia") == "United States"
    assert user5.get("ngonNgu") == "Tiếng Anh"
    u_id5 = user5.get("maNguoiDung") or user5.get("id")
    print(f"  [✓] Đăng ký thành công Nhà nghiên cứu Quốc tế ID #{u_id5}: {user5.get('hoTen')}")


async def test_ui_e2e_happy_path():
    print("\n" + "=" * 80)
    print("PHẦN 2: KIỂM THỬ GIAO DIỆN WEB E2E BẰNG TRÌNH DUYỆT (PLAYWRIGHT BROWSER)")
    print("=" * 80)

    reg_url = f"{WEB_BASE}/register.html"
    print(f"-> Truy cập trang đăng ký: {reg_url}")

    async with async_playwright() as p:
        try:
            browser = await p.chromium.launch(headless=True, channel="msedge")
        except Exception:
            try:
                browser = await p.chromium.launch(headless=True, channel="chrome")
            except Exception:
                browser = await p.chromium.launch(headless=True)

        page = await browser.new_page(viewport={"width": 1366, "height": 850})

        console_logs = []
        page.on("console", lambda msg: console_logs.append(f"[{msg.type}] {msg.text}"))

        await page.goto(reg_url)
        await page.wait_for_timeout(600)

        # Chuẩn bị dữ liệu đăng ký người dùng mới trên UI
        uid = uuid.uuid4().hex[:6]
        username = f"dr_huit_{uid}"
        email = f"dr.huit_{uid}@huit.edu.vn"
        full_name = "TS. Đặng Thanh Thi"
        import random
        orcid_val = f"0000-0002-{int(time.time())%9000+1000:04d}-{random.randint(1000, 9999):04d}"

        print(f"-> Điền biểu mẫu đăng ký cho: {username} ({email}) với ORCID {orcid_val}...")
        await page.fill("#regHoDem", "Đặng Thanh")
        await page.fill("#regTen", "Thi")
        await page.select_option("#regHocVi", "Tiến sĩ")
        await page.select_option("#regHocHam", "Không")
        await page.select_option("#regGioiTinh", "Nam")
        await page.select_option("#regNgonNgu", "Tiếng Việt")
        await page.select_option("#regQuocGia", "Vietnam")
        await page.fill("#regSoDienThoai", "0908123456")
        await page.fill("#regDonVi", "Khoa Công nghệ Thông tin, Trường Đại học Công Thương TP.HCM")
        await page.fill("#regDiaChi", "140 Lê Trọng Tấn, P. Tây Thạnh, Q. Tân Phú, TP.HCM")
        await page.fill("#regSoTaiKhoan", "012345678999")
        await page.fill("#regChuTaiKhoan", "DANG THANH THI")
        await page.fill("#regNganHang", "Vietcombank Chi nhánh Tây Sài Gòn")
        await page.select_option("#regChuyenNganh", "1") # CNTT & AI
        
        await page.fill("#regEmail", email)
        await page.fill("#regTenDangNhap", username)
        await page.fill("#regPassword", "HuitPassword2026@")
        await page.fill("#regConfirmPassword", "HuitPassword2026@")
        await page.fill("#regOrcid", orcid_val)

        # Đồng ý điều khoản
        await page.check("#regTerms")

        # Chụp ảnh trước khi nhấn nút Đăng ký
        shot_before = "Web/tests/shot_register_happy_filled.png"
        await page.screenshot(path=shot_before, full_page=False)
        print(f"  [✓] Đã chụp ảnh biểu mẫu đã điền: {shot_before}")

        # Nhấn nút Đăng ký
        print("-> Nhấn nút 'Đăng ký'...")
        submit_btn = await page.query_selector('button[type="submit"]')
        await submit_btn.scroll_into_view_if_needed()

        async with page.expect_response(lambda r: "/api/auth/register" in r.url and r.status == 200, timeout=10000) as response_info:
            await submit_btn.click()

        response = await response_info.value
        res_json = await response.json()
        print(f"  [✓] Nhận phản hồi API 200 OK từ Backend thật: {res_json.get('message')}")
        assert res_json.get("success") is True, f"API phản hồi không thành công: {res_json}"
        assert res_json.get("token") is not None, "API không trả về JWT Token!"

        # Chờ điều hướng tự động sang profile.html
        print("-> Chờ chuyển hướng tự động sang profile.html...")
        await page.wait_for_url("**/profile.html**", timeout=8000)
        await page.wait_for_timeout(1000)

        current_url = page.url
        print(f"  [✓] URL hiện tại sau đăng ký: {current_url}")
        assert "profile.html" in current_url, f"Không chuyển hướng sang profile.html! URL: {current_url}"

        # Kiểm tra thông tin hiển thị trên trang profile.html
        page_content = await page.content()
        assert "Đặng Thanh Thi" in page_content or username in page_content, "Thông tin người dùng mới không hiển thị trên Profile!"
        print("  [✓] Hồ sơ người dùng mới đã được nạp chính xác trên trang cá nhân Profile!")

        # Chụp ảnh sau khi đăng ký thành công và vào profile.html
        shot_after = "Web/tests/shot_register_happy_profile.png"
        await page.screenshot(path=shot_after, full_page=False)
        print(f"  [✓] Đã chụp ảnh minh chứng chuyển hướng Profile: {shot_after}")

        await browser.close()


def main():
    test_api_happy_paths()
    asyncio.run(test_ui_e2e_happy_path())
    print("\n" + "=" * 80)
    print("🎉 TOÀN BỘ CÁC BÀI KIỂM THỬ ĐĂNG KÝ (HAPPY PATHS) ĐÃ ĐẠT 100% THÀNH CÔNG!")
    print("=" * 80)

if __name__ == "__main__":
    main()
