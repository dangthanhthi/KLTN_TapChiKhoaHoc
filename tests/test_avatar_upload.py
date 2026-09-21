import urllib.request
import json
import uuid
import time
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

API_BASE = "http://localhost:5000/api"

def login(email, password):
    url = f"{API_BASE}/auth/login"
    data = json.dumps({"usernameOrEmail": email, "password": password}).encode('utf-8')
    req = urllib.request.Request(url, data=data, headers={'Content-Type': 'application/json'}, method='POST')
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode('utf-8'))

def upload_avatar(token, image_bytes, filename="test_avatar.png"):
    url = f"{API_BASE}/auth/upload-avatar"
    boundary = f"----WebKitFormBoundary{uuid.uuid4().hex}"
    
    body = bytearray()
    body.extend(f"--{boundary}\r\n".encode('utf-8'))
    body.extend(f'Content-Disposition: form-data; name="file"; filename="{filename}"\r\n'.encode('utf-8'))
    body.extend(b"Content-Type: image/png\r\n\r\n")
    body.extend(image_bytes)
    body.extend(b"\r\n")
    body.extend(f"--{boundary}--\r\n".encode('utf-8'))

    req = urllib.request.Request(
        url,
        data=body,
        headers={
            'Authorization': f"Bearer {token}",
            'Content-Type': f"multipart/form-data; boundary={boundary}"
        },
        method='POST'
    )
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode('utf-8'))

def delete_avatar(token):
    url = f"{API_BASE}/auth/avatar"
    req = urllib.request.Request(
        url,
        headers={'Authorization': f"Bearer {token}"},
        method='DELETE'
    )
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode('utf-8'))

def test_avatar_api():
    print("==================================================")
    print("KIỂM THỬ API UPLOAD & QUẢN LÝ ẢNH ĐẠI DIỆN")
    print("==================================================")

    # Đăng nhập bằng tài khoản mẫu TS. Vũ Thị F
    print("\n1. Đăng nhập tài khoản vuthif@huit.edu.vn...")
    auth_res = login("vuthif@huit.edu.vn", "123456")
    assert auth_res.get("success"), f"Đăng nhập thất bại: {auth_res}"
    token = auth_res["token"]
    user_id = auth_res["user"]["maNguoiDung"]
    print(f"-> Đăng nhập thành công! User ID: {user_id}, Tên: {auth_res['user']['hoTen']}")

    # Tạo file ảnh PNG mẫu 1x1 pixel trong suốt hợp lệ
    # Header chuẩn PNG
    png_sample = bytes([
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ])

    print("\n2. Gọi API Upload Avatar...")
    up_res = upload_avatar(token, png_sample, "avatar_test.png")
    print(f"Kết quả API: {up_res}")
    assert up_res.get("success"), f"Upload thất bại: {up_res}"
    avatar_url = up_res.get("avatarUrl")
    print(f"-> Upload thành công! URL ảnh đại diện: {avatar_url}")
    assert avatar_url and avatar_url.startswith("/uploads/avatars/"), f"URL ảnh không đúng: {avatar_url}"

    # Kiểm tra file đã thực sự được ghi vào đĩa
    abs_uploaded_file = os.path.join(r"c:\Users\MSIIIIII\Desktop\Khoa_Luan_Tot _Nghiep\Backend\HuitJournal.Api", avatar_url.lstrip("/"))
    print(f"Kiểm tra tệp tin trên đĩa: {abs_uploaded_file}")
    assert os.path.exists(abs_uploaded_file), f"Tệp tin không tồn tại trên đĩa: {abs_uploaded_file}"
    print("-> Tệp tin ảnh đại diện đã được lưu trữ an toàn trên máy chủ.")

    # Lấy lại profile qua API để xác nhận trường AnhDaiDien được trả về
    print("\n3. Kiểm tra GetProfile trả về AnhDaiDien...")
    req_profile = urllib.request.Request(f"{API_BASE}/auth/profile", headers={'Authorization': f"Bearer {token}"})
    with urllib.request.urlopen(req_profile) as resp:
        profile = json.loads(resp.read().decode('utf-8'))
        print(f"Profile AnhDaiDien: {profile.get('anhDaiDien')}")
        assert profile.get("anhDaiDien") == avatar_url, "Profile không chứa đúng AnhDaiDien"
    print("-> API GetProfile trả về URL ảnh đại diện chính xác.")

    print("\n==================================================")
    print("TẤT CẢ CÁC BÀI KIỂM THỬ API AVATAR ĐÃ ĐẠT CHUẨN 100%!")
    print("==================================================")

if __name__ == "__main__":
    test_avatar_api()
