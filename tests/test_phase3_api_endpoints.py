import urllib.request
import json
import uuid
import time
import sys
import os

sys.stdout.reconfigure(encoding='utf-8', line_buffering=True)
sys.stderr.reconfigure(encoding='utf-8', line_buffering=True)

API_ROOT = os.environ.get("JOURNAL_TEST_BASE_URL", "http://localhost:5001/api").rstrip("/")
BASE_URL = f"{API_ROOT}/auth"

def post_json(endpoint, data, token=None):
    url = f"{BASE_URL}/{endpoint}"
    req = urllib.request.Request(
        url,
        data=json.dumps(data).encode('utf-8'),
        headers={
            'Content-Type': 'application/json',
            **({'Authorization': f'Bearer {token}'} if token else {})
        },
        method='POST'
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return resp.getcode(), json.loads(resp.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        body = e.read().decode('utf-8')
        try:
            return e.code, json.loads(body)
        except Exception:
            return e.code, {"raw": body}

def get_json(endpoint, token=None):
    url = f"{BASE_URL}/{endpoint}"
    req = urllib.request.Request(
        url,
        headers={
            'Content-Type': 'application/json',
            **({'Authorization': f'Bearer {token}'} if token else {})
        },
        method='GET'
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return resp.getcode(), json.loads(resp.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        body = e.read().decode('utf-8')
        try:
            return e.code, json.loads(body)
        except Exception:
            return e.code, {"raw": body}

def run_tests():
    with urllib.request.urlopen(f"{API_ROOT}/testing/environment", timeout=10) as response:
        target = json.load(response)
    if target.get("environment") != "Testing" or target.get("database") != "QL_TapChiKhoaHoc_Test":
        raise RuntimeError("Kiểm thử chỉ được chạy trên API Testing và QL_TapChiKhoaHoc_Test.")
    print("=======================================================================")
    print("  BỘ KIỂM THỬ GIAI ĐOẠN 3: API ĐĂNG KÝ 202, VERIFY-EMAIL & RESEND")
    print(f"  Backend API: {BASE_URL} (Testing Profile)")
    print("=======================================================================\n")

    total_tests = 0
    passed_tests = 0

    def assert_true(cond, name, detail=None):
        nonlocal total_tests, passed_tests
        total_tests += 1
        if cond:
            passed_tests += 1
            print(f"  [PASS] {name}")
            if detail:
                print(f"         Chi tiết: {detail}")
        else:
            print(f"  [FAIL] {name}")
            if detail:
                print(f"         Chi tiết lỗi: {detail}")
            raise Exception(f"Kiểm thử thất bại: {name}")

    print("--- NHÓM 1: KIỂM THỬ CÁC RÀNG BUỘC UNHAPPY PATHS (POST /api/auth/register) ---")

    # 1.1 Mật khẩu yếu
    status, res = post_json("register", {
        "hoTen": "Trần Văn A",
        "email": f"unhappy_{uuid.uuid4().hex[:6]}@huit.edu.vn",
        "password": "password",
        "quocGia": "Vietnam"
    })
    assert_true(status == 400 and not res.get("success"),
        "Mật khẩu quá đơn giản (password) bị từ chối với HTTP 400",
        f"Status: {status} | Msg: {res.get('message')}")

    # 1.2 Số điện thoại không hợp lệ theo quốc gia Việt Nam
    status, res = post_json("register", {
        "hoTen": "Trần Văn A",
        "email": f"unhappy_{uuid.uuid4().hex[:6]}@huit.edu.vn",
        "password": "StrongPassword@2026",
        "quocGia": "Vietnam",
        "soDienThoai": "12345"
    })
    assert_true(status == 400 and not res.get("success"),
        "Số điện thoại Việt Nam sai định dạng bị từ chối với HTTP 400",
        f"Status: {status} | Msg: {res.get('message')}")

    # 1.3 Tổ hợp học hàm GS nhưng học vị Cử nhân (vi phạm quy chế học thuật)
    status, res = post_json("register", {
        "hoTen": "Trần Văn A",
        "email": f"unhappy_{uuid.uuid4().hex[:6]}@huit.edu.vn",
        "password": "StrongPassword@2026",
        "hocHam": "Giáo sư",
        "hocVi": "Cử nhân"
    })
    assert_true(status == 400 and not res.get("success"),
        "Học hàm Giáo sư với học vị Cử nhân bị từ chối (Academic Integrity)",
        f"Status: {status} | Msg: {res.get('message')}")

    # 1.4 Bộ 3 ngân hàng không trọn vẹn
    status, res = post_json("register", {
        "hoTen": "Trần Văn A",
        "email": f"unhappy_{uuid.uuid4().hex[:6]}@huit.edu.vn",
        "password": "StrongPassword@2026",
        "soTaiKhoan": "123456789",
        "nganHang": ""
    })
    assert_true(status == 400 and not res.get("success"),
        "Điền thiếu thông tin trong bộ 3 ngân hàng bị từ chối với HTTP 400",
        f"Status: {status} | Msg: {res.get('message')}")

    print("\n--- NHÓM 2: KHỞI TẠO ĐĂNG KÝ HỢP LỆ (HTTP 202 ACCEPTED) ---")

    test_suffix = uuid.uuid4().hex[:8]
    test_username = f"user_{test_suffix}"
    test_email = f"author_{test_suffix}@gmail.com"
    test_orcid = f"0000-0002-{int(time.time())%9000+1000:04d}-8888"

    happy_data = {
        "tenDangNhap": test_username,
        "hoDem": "Nguyễn Thành",
        "ten": "Đạt",
        "hoTen": "Nguyễn Thành Đạt",
        "email": test_email,
        "password": "SecurePassword@2026",
        "hocVi": "Tiến sĩ",
        "hocHam": "Không",
        "gioiTinh": "Nam",
        "quocGia": "Vietnam",
        "ngonNgu": "Tiếng Việt",
        "soDienThoai": "0987654321",
        "donVi": "Trường Đại học Công Thương TP.HCM",
        "diaChi": "140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM",
        "soTaiKhoan": "999888777666",
        "chuTaiKhoan": "NGUYEN THANH DAT",
        "nganHang": "Vietcombank",
        "maORCID": f"https://orcid.org/{test_orcid}",
        "chuyenNganhId": 1,
        "dangKyPhanBien": True
    }

    status, reg_res = post_json("register", happy_data)
    assert_true(status == 202, "Đăng ký thành công trả về HTTP 202 Accepted chuẩn xác thực 2 bước", f"Status: {status}")
    assert_true(reg_res.get("success") == True, "Trường success = True trong phản hồi")
    assert_true(reg_res.get("requiresVerification") == True, "Trường requiresVerification = True")
    reg_id = reg_res.get("registrationId")
    assert_true(bool(reg_id), "Phản hồi chứa registrationId", f"registrationId = {reg_id}")
    masked_email = reg_res.get("maskedEmail")
    assert_true("***" in masked_email and "@gmail.com" in masked_email,
        "Địa chỉ email được che giấu an toàn", f"maskedEmail = {masked_email}")
    assert_true(reg_res.get("resendAfterSeconds") == 60, "Cooldown gửi lại mặc định 60 giây")
    assert_true("token" not in reg_res, "Tuyệt đối không cấp JWT token trước khi xác thực email")

    print("\n--- NHÓM 3: GỬI LẠI MÃ & RATE LIMIT (POST /api/auth/resend-verification) ---")

    # 3.1 Gửi lại ngay lập tức -> Phải bị chặn bởi Cooldown 60s
    status, resend_res = post_json("resend-verification", { "registrationId": reg_id })
    assert_true(status == 400 or not resend_res.get("success"),
        "Chặn yêu cầu gửi lại mã ngay lập tức (Cooldown 60s)",
        f"Status: {status} | Msg: {resend_res.get('message')} | ResendAfter: {resend_res.get('resendAfterSeconds')}s")

    print("\n--- NHÓM 4: XÁC THỰC MÃ EMAIL (POST /api/auth/verify-email) ---")

    # 4.1 Thử nhập sai mã
    status, verify_wrong = post_json("verify-email", {
        "registrationId": reg_id,
        "verificationCode": "000000"
    })
    assert_true(status == 400 and not verify_wrong.get("success"),
        "Nhập sai mã xác nhận bị từ chối với HTTP 400",
        f"Msg: {verify_wrong.get('message')}")

    # 4.2 Lấy mã hợp lệ từ EmailOutbox trong CSDL (mô phỏng người dùng mở hộp thư đọc mã)
    import subprocess
    import re
    sql_cmd = f"SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SELECT TOP 1 NoiDungText FROM EmailOutbox WHERE NguoiNhan = '{test_email}' ORDER BY TaoLucUtc DESC;"
    res_sql = subprocess.check_output([
        "sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc_Test", "-y", "1000", "-h", "-1", "-Q", f"SET NOCOUNT ON; {sql_cmd}"
    ], text=True, encoding="utf-8")

    match = re.search(r'\b(\d{6})\b', res_sql)
    matched_code = match.group(1) if match else None

    assert_true(matched_code is not None, "Đã tìm thấy mã OTP trong thư mô phỏng")

    # 4.3 Gửi mã xác nhận hợp lệ
    status, verify_ok = post_json("verify-email", {
        "registrationId": reg_id,
        "verificationCode": matched_code
    })
    assert_true(status == 200 and verify_ok.get("success"),
        "Xác thực mã hợp lệ thành công với HTTP 200 OK",
        f"Message: {verify_ok.get('message')}")
    token = verify_ok.get("token")
    assert_true(bool(token) and len(token) > 50, "Cấp JWT Token thành công sau khi xác thực email", f"Token length = {len(token)}")
    user_dto = verify_ok.get("user", {})
    assert_true("Tác giả" in user_dto.get("vaiTros", []), "Người dùng được cấp vai trò Tác giả")
    assert_true("Độc giả" in user_dto.get("vaiTros", []), "Người dùng được cấp vai trò Độc giả")
    assert_true(user_dto.get("email") == test_email, "Email tài khoản khớp chính xác")

    # 4.4 Sử dụng JWT Token truy cập GET /api/auth/profile
    status, profile_res = get_json("profile", token=token)
    assert_true(status == 200, "JWT Token sử dụng truy cập hồ sơ thành công (GET /api/auth/profile)",
        f"HoTen: {profile_res.get('hoTen')} | Email: {profile_res.get('email')}")

    # 4.5 Thử dùng lại mã đã xác thực -> Phải bị từ chối
    status, verify_reuse = post_json("verify-email", {
        "registrationId": reg_id,
        "verificationCode": matched_code
    })
    assert_true(status == 400 and not verify_reuse.get("success"),
        "Mã đã sử dụng hoặc hồ sơ đã kích hoạt bị từ chối tái sử dụng (One-Time Passcode)",
        f"Msg: {verify_reuse.get('message')}")

    # Dọn dẹp tài khoản kiểm thử
    print("\n--- DỌN DẸP DỮ LIỆU KIỂM THỬ ---")
    cleanup_sql = f"SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; DECLARE @Uid INT = (SELECT MaNguoiDung FROM NguoiDung WHERE Email = '{test_email}'); IF @Uid IS NOT NULL BEGIN DELETE FROM NguoiDung_VaiTro WHERE MaNguoiDung = @Uid; DELETE FROM NguoiDung_ChuyenMon WHERE MaNguoiDung = @Uid; DELETE FROM DonDangKyPhanBien WHERE MaNguoiDung = @Uid; DELETE FROM NguoiDung WHERE MaNguoiDung = @Uid; END; DELETE FROM EmailOutbox WHERE NguoiNhan = '{test_email}'; DELETE FROM MaXacNhanEmail WHERE MaDangKy = '{reg_id}'; DELETE FROM DangKyChoXacNhan WHERE MaDangKy = '{reg_id}';"
    try:
        subprocess.run(["sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc_Test", "-Q", cleanup_sql], timeout=10, check=True)
        print("  Đã dọn dẹp sạch tài khoản thử nghiệm khỏi cơ sở dữ liệu.")
    except Exception as ex:
        print(f"  Cảnh báo dọn dẹp: {ex}")

    print("\n=======================================================================")
    print(f"  KẾT QUẢ TỔNG HỢP: {passed_tests}/{total_tests} KIỂM THỬ THÀNH CÔNG (100% PASS)")
    print("  GIAI ĐOẠN 3 HOÀN TẤT XUẤT SẮC!")
    print("=======================================================================\n")

if __name__ == "__main__":
    run_tests()

