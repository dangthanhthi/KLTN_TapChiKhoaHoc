import urllib.request
import json
import uuid
import time
import subprocess
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

BASE_URL = "http://localhost:5000/api/auth"

def make_post(endpoint, data):
    url = f"{BASE_URL}/{endpoint}"
    req = urllib.request.Request(
        url,
        data=json.dumps(data).encode('utf-8'),
        headers={'Content-Type': 'application/json'},
        method='POST'
    )
    try:
        with urllib.request.urlopen(req) as resp:
            return resp.getcode(), json.loads(resp.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        body = e.read().decode('utf-8')
        try:
            return e.code, json.loads(body)
        except:
            return e.code, {"raw": body}

def run_all_tests():
    print("==================================================")
    print("KIỂM THỬ RÀNG BUỘC CHỨC NĂNG ĐĂNG KÝ TÀI KHOẢN")
    print("==================================================")
    
    # 1. Test Username không hợp lệ (chứa khoảng trắng hoặc tiếng Việt)
    print("\n--- TEST 1: Kiểm tra username không hợp lệ ---")
    bad_username_data = {
        "tenDangNhap": "dang thanh thi",
        "hoDem": "Đặng",
        "ten": "Thi",
        "hoTen": "Đặng Thi",
        "email": f"test_{uuid.uuid4().hex[:8]}@huit.edu.vn",
        "password": "Password123@",
        "chuyenNganhId": 1
    }
    code, res = make_post("register", bad_username_data)
    print(f"HTTP Status: {code}")
    print(f"Message: {res.get('message')}")
    assert code == 400 or not res.get("success")
    assert "Tên đăng nhập không hợp lệ" in res.get("message", "")
    print("=> TEST 1 PASS: Username sai định dạng bị chặn chính xác.")

    # 2. Test Đăng ký thành công tài khoản chuẩn (Tiến sĩ, tích phản biện, bộ 3 ngân hàng, ORCID full link)
    print("\n--- TEST 2: Đăng ký chuyên gia Tiến sĩ (đủ điều kiện phản biện) ---")
    valid_username = f"dr_{uuid.uuid4().hex[:8]}"
    valid_email = f"{valid_username}@huit.edu.vn"
    valid_orcid = f"0000-0002-{int(time.time())%9000+1000:04d}-9999"
    full_orcid_url = f"https://orcid.org/{valid_orcid}"

    dr_data = {
        "tenDangNhap": valid_username,
        "hoDem": "Nguyễn Văn",
        "ten": "Tiến Sĩ",
        "hoTen": "Nguyễn Văn Tiến Sĩ",
        "email": valid_email,
        "password": "SecurePassword123@",
        "hocVi": "Tiến sĩ",
        "hocHam": "Không",
        "gioiTinh": "Nam",
        "quocGia": "United States",
        "ngonNgu": "Tiếng Anh",
        "soDienThoai": "+84912345678",
        "donVi": "Đại học Công Thương TP.HCM",
        "diaChi": "140 Lê Trọng Tấn",
        "soTaiKhoan": "19034567890",
        "chuTaiKhoan": "nguyen van tien si", # Cần được tự động viết hoa
        "nganHang": "Vietcombank Tân Bình",
        "chuyenNganhId": 1,
        "maORCID": full_orcid_url, # Cần được tự động bóc tách url
        "dangKyPhanBien": True
    }
    code, res = make_post("register", dr_data)
    print(f"HTTP Status: {code}")
    print(f"Success: {res.get('success')}")
    print(f"Message: {res.get('message')}")
    assert code == 200 and res.get("success"), f"Đăng ký thất bại: {res}"
    user = res.get("user", {})
    roles = user.get("vaiTros", [])
    print(f"Các vai trò được cấp: {roles}")
    assert "Tác giả" in roles, "Phải có vai trò Tác giả"
    assert "Độc giả" in roles, "Phải có vai trò Độc giả"
    assert "Chuyên gia phản biện" in roles, "Tiến sĩ phải được cấp vai trò Chuyên gia phản biện"
    assert user.get("quocGia") == "United States", "Quốc gia phải lưu đúng United States"
    assert user.get("ngonNgu") == "Tiếng Anh", "Ngôn ngữ phải lưu đúng Tiếng Anh"
    assert user.get("chuTaiKhoan") == "NGUYEN VAN TIEN SI", "Chủ tài khoản phải được viết hoa chuẩn mực"
    assert user.get("maORCID") == valid_orcid, f"Mã ORCID phải được bóc tách chuẩn 16 ký tự, thực tế: {user.get('maORCID')}"
    print("=> TEST 2 PASS: Đăng ký Tiến sĩ đủ điều kiện phản biện, bóc tách ORCID, viết hoa chủ TK thành công 100%.")

    # 3. Test Username trùng lặp -> Phải báo lỗi rõ ràng, KHÔNG tự động đổi tên thành username_123
    print("\n--- TEST 3: Kiểm tra trùng lặp Tên đăng nhập ---")
    dup_user_data = {
        "tenDangNhap": valid_username, # Trùng với user trên
        "hoDem": "Trần",
        "ten": "A",
        "hoTen": "Trần A",
        "email": f"diff_{uuid.uuid4().hex[:8]}@huit.edu.vn",
        "password": "Password123@",
        "chuyenNganhId": 2
    }
    code, res = make_post("register", dup_user_data)
    print(f"HTTP Status: {code}")
    print(f"Message: {res.get('message')}")
    assert code == 400 or not res.get("success")
    assert "Tên đăng nhập này đã được sử dụng" in res.get("message", "")
    print("=> TEST 3 PASS: Trùng username được thông báo lỗi minh bạch, không đổi lén.")

    # 4. Test ORCID trùng lặp
    print("\n--- TEST 4: Kiểm tra trùng lặp mã ORCID ---")
    dup_orcid_data = {
        "tenDangNhap": f"user_{uuid.uuid4().hex[:8]}",
        "hoDem": "Lê",
        "ten": "B",
        "hoTen": "Lê B",
        "email": f"diff_{uuid.uuid4().hex[:8]}@huit.edu.vn",
        "password": "Password123@",
        "maORCID": valid_orcid, # Đã dùng ở TEST 2
        "chuyenNganhId": 3
    }
    code, res = make_post("register", dup_orcid_data)
    print(f"HTTP Status: {code}")
    print(f"Message: {res.get('message')}")
    assert code == 400 or not res.get("success")
    assert "ORCID này đã được liên kết" in res.get("message", "")
    print("=> TEST 4 PASS: Trùng ORCID được ngăn chặn và báo lỗi rõ ràng.")

    # 5. Test Bộ 3 Ngân hàng không trọn vẹn (chỉ điền số tài khoản, bỏ trống ngân hàng)
    print("\n--- TEST 5: Kiểm tra thông tin ngân hàng không trọn vẹn ---")
    incomplete_bank_data = {
        "tenDangNhap": f"user_{uuid.uuid4().hex[:8]}",
        "hoDem": "Phạm",
        "ten": "C",
        "hoTen": "Phạm C",
        "email": f"bank_test_{uuid.uuid4().hex[:8]}@huit.edu.vn",
        "password": "Password123@",
        "soTaiKhoan": "1234567890",
        # Thiếu chuTaiKhoan và nganHang
        "chuyenNganhId": 4
    }
    code, res = make_post("register", incomplete_bank_data)
    print(f"HTTP Status: {code}")
    print(f"Message: {res.get('message')}")
    assert code == 400 or not res.get("success")
    assert "đầy đủ cả 3 mục" in res.get("message", "")
    print("=> TEST 5 PASS: Điền thiếu thông tin trong bộ 3 ngân hàng bị chặn chuẩn xác.")

    # 6. Test Ràng buộc học thuật: Đăng ký với học vị Cử nhân tích phản biện -> Không được cấp vai trò 4
    print("\n--- TEST 6: Ràng buộc học thuật với học vị Cử nhân tích phản biện ---")
    bachelor_username = f"bachelor_{uuid.uuid4().hex[:8]}"
    bachelor_data = {
        "tenDangNhap": bachelor_username,
        "hoDem": "Hoàng",
        "ten": "Cử Nhân",
        "hoTen": "Hoàng Cử Nhân",
        "email": f"{bachelor_username}@huit.edu.vn",
        "password": "Password123@",
        "hocVi": "Cử nhân",
        "hocHam": "Không",
        "dangKyPhanBien": True, # Tích phản biện
        "chuyenNganhId": 5
    }
    code, res = make_post("register", bachelor_data)
    print(f"HTTP Status: {code}")
    print(f"Success: {res.get('success')}")
    print(f"Message: {res.get('message')}")
    assert code == 200 and res.get("success")
    user = res.get("user", {})
    roles = user.get("vaiTros", [])
    print(f"Các vai trò được cấp cho Cử nhân: {roles}")
    assert "Tác giả" in roles, "Phải có vai trò Tác giả"
    assert "Độc giả" in roles, "Phải có vai trò Độc giả"
    assert "Chuyên gia phản biện" not in roles, "Cử nhân tuyệt đối KHÔNG được tự động cấp vai trò Chuyên gia phản biện"
    assert "Lưu ý: Vai trò Chuyên gia phản biện" in res.get("message", ""), "Phải có câu lưu ý quy chế học thuật"
    print("=> TEST 6 PASS: Ràng buộc học thuật chuẩn tòa soạn hoạt động hoàn hảo 100%.")

    print("\n==================================================")
    print("TẤT CẢ 6/6 TEST CASES RÀNG BUỘC ĐÃ PASS XUẤT SẮC!")
    print("==================================================")

if __name__ == "__main__":
    run_all_tests()
