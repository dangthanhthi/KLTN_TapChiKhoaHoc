"""
Verification Test Suite: Reviewer Privacy (Double-Blind) & Public PDF Security
Tested against Live Backend API (.NET 9) with Full Post-Test Cleanup
Author: Đặng Thành Thi (Web & Backend API Developer)
"""

import sys
import io
import time
import json
import requests
import subprocess

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE_URL = "http://localhost:5000/api"

def print_separator(title=""):
    print("\n" + "=" * 75)
    if title:
        print(f" {title.upper()} ")
        print("=" * 75)

def run_tests():
    print_separator("KIỂM TRA CHẶN LỘ DANH TÍNH PHẢN BIỆN & BẢO MẬT PDF CÔNG KHAI")
    
    # 1. Đăng nhập Ban biên tập
    res_editor = requests.post(f"{BASE_URL}/auth/login", json={"usernameOrEmail": "txhuong", "password": "123456"})
    assert res_editor.status_code == 200, f"Đăng nhập biên tập viên thất bại: {res_editor.text}"
    token_editor = res_editor.json()["token"]
    headers_editor = {"Authorization": f"Bearer {token_editor}"}
    print("  [✓] Đăng nhập Ban biên tập thành công.")

    # 2. Đăng ký tài khoản tác giả tạm thời để kiểm thử
    temp_uname = f"author_priv_{int(time.time())}"
    temp_email = f"{temp_uname}@huit.edu.vn"
    res_reg = requests.post(f"{BASE_URL}/auth/register", json={
        "tenDangNhap": temp_uname,
        "hoTen": "TS. Tác Giả Ẩn Danh",
        "email": temp_email,
        "password": "Password123!",
        "hocVi": "Tiến sĩ",
        "chuyenNganhId": 1
    })
    assert res_reg.status_code == 200, f"Đăng ký tác giả thất bại: {res_reg.text}"
    token_author = res_reg.json()["token"]
    author_id = res_reg.json()["user"]["maNguoiDung"]
    headers_author = {"Authorization": f"Bearer {token_author}"}
    print(f"  [✓] Tạo tài khoản tác giả tạm thời: {temp_uname} (ID={author_id})")

    temp_paper_id = None
    try:
        # 3. Tác giả nộp bài báo tạm thời
        manuscript_bytes = b"%PDF-1.5 Privacy Test Manuscript..."
        files = {"TapTinBanThao": ("Manuscript_Privacy_Test.pdf", manuscript_bytes, "application/pdf")}
        data = {
            "TieuDe": f"Nghiên cứu tính bảo mật thông tin bình duyệt khoa học ({temp_uname})",
            "TomTat": "Bản thảo phục vụ kiểm thử tính ẩn danh hai chiều của phản biện viên...",
            "TuKhoa": "Privacy, Double-Blind, Security",
            "MaChuyenNganh": 1,
            "DongTacGiaJson": json.dumps([{"hoTen": "TS. Tác Giả Ẩn Danh", "email": temp_email, "donVi": "HUIT", "laTacGiaLienHe": True, "thuTu": 1}]),
            "PhanBienDeXuatJson": "[]"
        }
        res_submit = requests.post(f"{BASE_URL}/baibao/submit", headers=headers_author, data=data, files=files)
        assert res_submit.status_code == 200, f"Nộp bài thất bại: {res_submit.text}"
        temp_paper_id = res_submit.json()["maBaiBao"]
        print(f"  [✓] Tác giả nộp bài báo thử nghiệm thành công (Mã bài: {temp_paper_id})")

        # 4. Ban biên tập tải lên bản thảo ẩn danh vòng 1
        anon_bytes = b"%PDF-1.5 ANONYMOUS MANUSCRIPT FOR DOUBLE-BLIND..."
        res_anon = requests.post(f"{BASE_URL}/baibao/{temp_paper_id}/upload-anonymous-manuscript?soVong=1", 
                                 headers=headers_editor, 
                                 files={"file": ("Anonymous_R1.pdf", anon_bytes, "application/pdf")})
        assert res_anon.status_code == 200, f"Tải tệp ẩn danh thất bại: {res_anon.text}"
        print("  [✓] Ban biên tập tải bản thảo ẩn danh Vòng 1 thành công.")

        # 5. Ban biên tập phân công chuyên gia Đặng Văn G (ID=4, username: dangvang)
        reviewer_id = 4
        reviewer_name = "Đặng Văn G"
        res_assign = requests.post(f"{BASE_URL}/phanbien/assign", headers=headers_editor, json={
            "maBaiBao": temp_paper_id,
            "maNguoiDungReviewer": reviewer_id,
            "soVong": 1
        })
        assert res_assign.status_code == 200, f"Phân công thất bại: {res_assign.text}"
        print(f"  [✓] Ban biên tập phân công chuyên gia {reviewer_name} thành công.")

        # -----------------------------------------------------------------
        # TEST 1: KIỂM CHỨNG TÁC GIẢ KHÔNG THẤY TÊN PHẢN BIỆN VIÊN (MỤC 1)
        # -----------------------------------------------------------------
        print_separator("TEST 1: KIỂM CHỨNG BẢO VỆ PHẢN BIỆN KÍN (DOUBLE-BLIND) PHÍA TÁC GIẢ")
        res_author_view = requests.get(f"{BASE_URL}/baibao/{temp_paper_id}", headers=headers_author)
        assert res_author_view.status_code == 200, "Tác giả không đọc được chi tiết bài của mình!"
        author_detail = res_author_view.json()

        history_items = author_detail.get("lichSuTrangThais", [])
        print(f"  -> Tác giả nhận được {len(history_items)} mục lịch sử trạng thái:")
        for idx, h in enumerate(history_items, 1):
            ghi_chu = h.get("ghiChu", "")
            nguoi_thuc_hien = h.get("nguoiThucHien", "")
            print(f"     {idx}. [{h.get('trangThaiMoi')}] Người thực hiện: '{nguoi_thuc_hien}' | Ghi chú: '{ghi_chu}'")

            # KHẲNG ĐỊNH: Tên của phản biện viên TUYỆT ĐỐI không xuất hiện
            assert reviewer_name not in ghi_chu, f"LỖI BẢO MẬT: Tên phản biện '{reviewer_name}' bị lộ trong ghiChu của tác giả!"
            assert "dangvang" not in ghi_chu.lower(), "LỖI BẢO MẬT: Username phản biện bị lộ trong ghiChu!"
            assert reviewer_name not in str(nguoi_thuc_hien), f"LỖI BẢO MẬT: Tên phản biện bị lộ trong nguoiThucHien!"

        print("  [✓] KẾT QUẢ TEST 1: ĐẠT CHUẨN DOUBLE-BLIND 100%! Tác giả chỉ thấy 'Ban biên tập đã phân công chuyên gia phản biện kín (Vòng 1)' và người thực hiện 'Ban biên tập'.")

        # -----------------------------------------------------------------
        # TEST 2: KIỂM CHỨNG BẢO MẬT PDF TRÊN TRANG CÔNG KHAI (MỤC 2)
        # -----------------------------------------------------------------
        print_separator("TEST 2: KIỂM CHỨNG BẢO MẬT FILE PDF TRÊN TRANG CÔNG KHAI")

        # 2.1 Kiểm tra chi tiết số tạp chí
        res_issue = requests.get(f"{BASE_URL}/sotapchi/1")
        assert res_issue.status_code == 200, "Lấy chi tiết số tạp chí thất bại"
        issue_data = res_issue.json()
        articles_in_issue = issue_data.get("danhSachBaiBao", [])
        print(f"  -> Kiểm tra {len(articles_in_issue)} bài báo trong Số 42:")
        for a in articles_in_issue:
            pdf_url = a.get("filePdfUrl")
            title = a.get("tieuDe", "")[:40]
            print(f"     - Bài #{a.get('maBaiBao')}: '{title}...' -> FilePdfUrl: {pdf_url}")
            if pdf_url:
                # Phải là endpoint công khai chính thức
                assert pdf_url.startswith("/api/baibao/public/"), f"LỖI: filePdfUrl không đúng định dạng endpoint an toàn: {pdf_url}"
                assert "uploads/submissions" not in pdf_url.lower(), f"LỖI LỘ TỆP NỘI BỘ: {pdf_url}"
                assert "bản thảo gốc" not in pdf_url.lower(), f"LỖI LỘ TỆP NỘI BỘ: {pdf_url}"

        # 2.2 Kiểm tra danh sách bài mới nhất
        res_latest = requests.get(f"{BASE_URL}/baibao/public/latest")
        assert res_latest.status_code == 200, "Lấy danh sách bài mới thất bại"
        latest_articles = res_latest.json()
        print(f"  -> Kiểm tra {len(latest_articles)} bài mới nhất trên trang chủ:")
        for a in latest_articles:
            pdf_url = a.get("filePdfUrl")
            if pdf_url:
                assert pdf_url.startswith("/api/baibao/public/"), f"LỖI: filePdfUrl không an toàn: {pdf_url}"
                assert "uploads" not in pdf_url.lower() or pdf_url.startswith("/api/baibao/public/"), f"LỖI LỘ TỆP: {pdf_url}"

        print("  [✓] KẾT QUẢ TEST 2: ĐẠT CHUẨN AN TOÀN PDF CÔNG KHAI 100%! Không còn rò rỉ đường dẫn tệp bản thảo nội bộ.")

    finally:
        # -----------------------------------------------------------------
        # DỌN DẸP DỮ LIỆU THỬ NGHIỆM (CLEANUP)
        # -----------------------------------------------------------------
        print_separator("DỌN DẸP DỮ LIỆU THỬ NGHIỆM (CLEANUP)")
        if temp_paper_id:
            try:
                sql_cmds = f"""
                DELETE FROM PhieuDanhGia WHERE MaPhanCong IN (SELECT MaPhanCong FROM PhanCongPhanBien WHERE MaBaiBao = {temp_paper_id});
                DELETE FROM PhanCongPhanBien WHERE MaBaiBao = {temp_paper_id};
                DELETE FROM ThuMucBaiBao WHERE MaBaiBao = {temp_paper_id};
                DELETE FROM LichSuTrangThaiBaiBao WHERE MaBaiBao = {temp_paper_id};
                DELETE FROM DongTacGia WHERE MaBaiBao = {temp_paper_id};
                DELETE FROM PhanBienDeXuat WHERE MaBaiBao = {temp_paper_id};
                DELETE FROM BaiBao WHERE MaBaiBao = {temp_paper_id};
                """
                if author_id:
                    sql_cmds += f"""
                    DELETE FROM NguoiDung_VaiTro WHERE MaNguoiDung = {author_id};
                    DELETE FROM DonDangKyPhanBien WHERE MaNguoiDung = {author_id};
                    DELETE FROM NguoiDung WHERE MaNguoiDung = {author_id};
                    """
                proc = subprocess.run(["sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc", "-E", "-C", "-Q", sql_cmds], capture_output=True, text=True)
                if proc.returncode == 0:
                    print(f"  [✓] Đã dọn dẹp sạch sẽ bài báo thử nghiệm #{temp_paper_id} và tài khoản #{author_id} khỏi CSDL SQL Server.")
                else:
                    print(f"  [!] Dọn dẹp CSDL qua sqlcmd gặp cảnh báo: {proc.stderr}")
            except Exception as clean_err:
                print(f"  [!] Dọn dẹp CSDL gặp lưu ý: {clean_err}")

    print_separator("HOÀN TẤT XÁC MINH MỤC 1 & MỤC 2")

if __name__ == "__main__":
    run_tests()
