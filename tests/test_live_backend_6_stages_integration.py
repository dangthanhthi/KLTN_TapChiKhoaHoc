"""
Comprehensive End-to-End Integration Test Suite: 6-Stage Scientific Publishing Workflow
Tested against Live Backend API (.NET 9) and Dedicated Test Database (QL_TapChiKhoaHoc_Test)
Author: Đặng Thành Thi (Web Portal & Backend API Developer)

Verifies:
1. Multi-role Authentication & RBAC (Admin, Editor, Reviewer 1, Reviewer 2, Author)
2. Reviewer Application & Approval Workflow (with MaDon validation and re-approval guard)
3. OWASP File Upload Security & 5-Step Online Submission (Magic bytes, %%EOF, /Launch scanning)
4. State Machine Transition Matrix Guards & Pre-review Negative Tests
5. Double-Blind Anonymization & 2-Reviewer Assignment (with Author Privacy Leak Protection)
6. Dual Reviewer Desk & BM-04 Evaluations (Guarding decision until ALL reviewers complete)
7. Editorial Revision Decision & Author BM-03 Revision Submission (Stage 5)
8. Production Workflow Guards, Issue Assignment & Production PDF Upload (Stage 6)
9. Public Portal Publishing & Unauthenticated Reader PDF Download
10. Complete Database & Disk Cleanup with Verification Guard
"""

import sys
import io
import os
import json
import time
import requests
import subprocess
from pathlib import Path

# Configure stdout encoding to UTF-8
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE_URL = "http://localhost:5000/api"
TEST_DB = "QL_TapChiKhoaHoc_Test"

# Helper to generate structurally valid PDF bytes
def make_dummy_pdf(content_text: str) -> bytes:
    body = (
        "%PDF-1.5\n"
        "1 0 obj\n"
        "<< /Type /Catalog /Pages 2 0 R >>\n"
        "endobj\n"
        "2 0 obj\n"
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>\n"
        "endobj\n"
        "3 0 obj\n"
        f"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\n"
        "endobj\n"
        "4 0 obj\n"
        f"<< /Length {len(content_text)} >>\n"
        "stream\n"
        f"{content_text}\n"
        "endstream\n"
        "endobj\n"
        "xref\n"
        "0 5\n"
        "0000000000 65535 f \n"
        "trailer\n"
        "<< /Root 1 0 R >>\n"
        "startxref\n"
        "%%EOF\n"
    )
    return body.encode("utf-8")

def print_separator(title=""):
    print("\n" + "=" * 80)
    if title:
        print(f" {title.upper()} ")
        print("=" * 80)

def clean_database_and_disk(paper_id=None, author_id=None, uploaded_disk_files=None):
    """
    Cleans up all test database records and physical files on disk.
    If cleanup fails, raises an AssertionError to fail the test suite.
    """
    errors = []

    # 1. Clean physical files on disk
    if uploaded_disk_files:
        for file_path in uploaded_disk_files:
            try:
                p = Path(file_path)
                if p.exists() and p.is_file():
                    p.unlink()
                    print(f"  [✓] Đã xóa tệp vật lý: {p.name}")
            except Exception as e:
                errors.append(f"Không thể xóa tệp vật lý {file_path}: {e}")

    # Also clean revision folder if created
    if paper_id:
        try:
            rev_dir = Path(f"Backend/HuitJournal.Api/Uploads/revisions/paper_{paper_id}")
            if rev_dir.exists():
                for f in rev_dir.iterdir():
                    if f.is_file():
                        f.unlink()
                rev_dir.rmdir()
                print(f"  [✓] Đã xóa thư mục chỉnh sửa: {rev_dir}")
        except Exception as e:
            errors.append(f"Không thể xóa thư mục revisions paper_{paper_id}: {e}")

    # 2. Clean SQL Server records
    try:
        sql_cmds = "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;\n"
        if paper_id:
            sql_cmds += f"""
            DELETE FROM PhieuDanhGia WHERE MaPhanCong IN (SELECT MaPhanCong FROM PhanCongPhanBien WHERE MaBaiBao = {paper_id});
            DELETE FROM PhanCongPhanBien WHERE MaBaiBao = {paper_id};
            DELETE FROM ThuMucBaiBao WHERE MaBaiBao = {paper_id};
            DELETE FROM LichSuTrangThaiBaiBao WHERE MaBaiBao = {paper_id};
            DELETE FROM DongTacGia WHERE MaBaiBao = {paper_id};
            DELETE FROM PhanBienDeXuat WHERE MaBaiBao = {paper_id};
            DELETE FROM BaiBao WHERE MaBaiBao = {paper_id};
            """
        if author_id:
            sql_cmds += f"""
            DELETE FROM NguoiDung_ChuyenMon WHERE MaNguoiDung = {author_id};
            DELETE FROM NguoiDung_VaiTro WHERE MaNguoiDung = {author_id};
            DELETE FROM DonDangKyPhanBien WHERE MaNguoiDung = {author_id};
            DELETE FROM NguoiDung WHERE MaNguoiDung = {author_id};
            """

        if sql_cmds.strip():
            proc = subprocess.run(
                ["sqlcmd", "-S", ".", "-d", TEST_DB, "-E", "-I", "-C", "-f", "65001", "-Q", sql_cmds],
                capture_output=True, text=True
            )
            if proc.returncode != 0:
                errors.append(f"Lỗi SQLCMD khi xóa dữ liệu test: {proc.stderr}")
            else:
                print(f"  [✓] Dọn dẹp CSDL {TEST_DB} thành công (Bài #{paper_id}, Tác giả #{author_id}).")

            # Verify pristine state (exactly 10 articles and 10 users in seed data)
            verify_sql = "SET QUOTED_IDENTIFIER ON; SELECT COUNT(*) FROM BaiBao; SELECT COUNT(*) FROM NguoiDung;"
            v_proc = subprocess.run(
                ["sqlcmd", "-S", ".", "-d", TEST_DB, "-E", "-I", "-C", "-f", "65001", "-Q", verify_sql],
                capture_output=True, text=True
            )
            lines = [l.strip() for l in v_proc.stdout.splitlines() if l.strip().isdigit()]
            if len(lines) >= 2:
                art_count, usr_count = int(lines[0]), int(lines[1])
                print(f"  [✓] Xác minh trạng thái CSDL sau dọn dẹp: {art_count} bài báo, {usr_count} người dùng (chuẩn seed).")
                if art_count != 10 or usr_count != 10:
                    errors.append(f"Dữ liệu CSDL chưa trở về chuẩn seed ban đầu (Bài báo: {art_count}, Người dùng: {usr_count})!")

    except Exception as e:
        errors.append(f"Lỗi ngoại lệ trong quá trình dọn dẹp CSDL: {e}")

    if errors:
        error_msg = "\n".join(errors)
        raise AssertionError(f"DỌN DẸP THỬ NGHIỆM THẤT BẠI:\n{error_msg}")

def run_tests():
    print_separator("KIỂM THỬ TÍCH HỢP TOÀN DIỆN 6 GIAI ĐOẠN XUẤT BẢN KHOA HỌC (LIVE TEST)")
    print(f"Môi trường kiểm thử: Backend API .NET 9 trên {BASE_URL}")
    print(f"Cơ sở dữ liệu độc lập: {TEST_DB} (tuyệt đối không can thiệp CSDL chính)")
    print("Tiêu chuẩn bảo mật: OWASP File Validation + State Machine Guards + Double-Blind Anonymity")

    results = []
    created_paper_id = None
    created_author_id = None
    tracked_disk_files = []

    try:
        # =========================================================
        # TEST 1: Xác thực & Phân quyền RBAC (Multi-role Authentication)
        # =========================================================
        print_separator("TEST 1: Đăng nhập các vai trò hệ thống (Admin, Editor, Reviewer 1, Reviewer 2, Author)")

        # 1.1 Admin login
        res_admin = requests.post(f"{BASE_URL}/auth/login", json={"usernameOrEmail": "admin", "password": "123456"})
        assert res_admin.status_code == 200, f"Admin login failed: {res_admin.text}"
        token_admin = res_admin.json()["token"]
        print("  [✓] 1.1 Quản trị viên (admin): Đăng nhập thành công (Role 1)")

        # 1.2 Editor login
        res_editor = requests.post(f"{BASE_URL}/auth/login", json={"usernameOrEmail": "txhuong", "password": "123456"})
        assert res_editor.status_code == 200, f"Editor login failed: {res_editor.text}"
        token_editor = res_editor.json()["token"]
        headers_editor = {"Authorization": f"Bearer {token_editor}"}
        print("  [✓] 1.2 Ban biên tập (txhuong): Đăng nhập thành công (Role 2)")

        # 1.3 Reviewer 1 login (dangvang)
        res_rev1 = requests.post(f"{BASE_URL}/auth/login", json={"usernameOrEmail": "dangvang", "password": "123456"})
        assert res_rev1.status_code == 200, f"Reviewer 1 login failed: {res_rev1.text}"
        token_rev1 = res_rev1.json()["token"]
        headers_rev1 = {"Authorization": f"Bearer {token_rev1}"}
        rev1_id = res_rev1.json()["user"]["maNguoiDung"]
        print(f"  [✓] 1.3 Chuyên gia phản biện 1 (dangvang, ID={rev1_id}): Đăng nhập thành công (Role 4)")

        # 1.4 Reviewer 2 login (tranvann - Chuyên ngành 1 CNTT, Role 4)
        res_rev2 = requests.post(f"{BASE_URL}/auth/login", json={"usernameOrEmail": "tranvann", "password": "123456"})
        assert res_rev2.status_code == 200, f"Reviewer 2 login failed: {res_rev2.text}"
        token_rev2 = res_rev2.json()["token"]
        headers_rev2 = {"Authorization": f"Bearer {token_rev2}"}
        rev2_id = res_rev2.json()["user"]["maNguoiDung"]
        print(f"  [✓] 1.4 Chuyên gia phản biện 2 (tranvann, ID={rev2_id}): Đăng nhập thành công (Role 4)")

        # 1.5 Dynamic Author account registration
        ts = int(time.time())
        author_username = f"author_e2e_{ts}"
        author_email = f"{author_username}@huit.edu.vn"
        reg_payload = {
            "tenDangNhap": author_username,
            "hoTen": "TS. Đặng Thành Thi",
            "email": author_email,
            "password": "Password123!",
            "donVi": "Đại học Công Thương TP.HCM (HUIT)",
            "hocVi": "Tiến sĩ",
            "chuyenNganhId": 1
        }
        res_reg = requests.post(f"{BASE_URL}/auth/register", json=reg_payload)
        assert res_reg.status_code == 200, f"Author registration failed: {res_reg.text}"
        token_author = res_reg.json()["token"]
        created_author_id = res_reg.json()["user"]["maNguoiDung"]
        headers_author = {"Authorization": f"Bearer {token_author}"}
        print(f"  [✓] 1.5 Đăng ký mới Tác giả ({author_username}, ID={created_author_id}): Thành công")
        results.append(("TEST 1: Xác thực & Phân quyền RBAC", True))

        # =========================================================
        # TEST 2: Đơn xin làm phản biện viên & Phê duyệt cấp quyền (với kiểm tra lỗi)
        # =========================================================
        print_separator("TEST 2: Quy trình thẩm định đơn xin phản biện & Kiểm soát mã đơn (MaDon)")

        # 2.1 Tác giả gửi đơn xin làm phản biện viên
        res_apply = requests.post(f"{BASE_URL}/auth/request-reviewer", headers=headers_author, json={
            "ghiChu": "Kính gửi Ban biên tập: Tôi có 8 năm kinh nghiệm nghiên cứu AI & NLP, mong muốn tham gia phản biện."
        })
        assert res_apply.status_code == 200, f"Nộp đơn thất bại: {res_apply.text}"
        print("  [✓] 2.1 Tác giả gửi đơn đăng ký phản biện viên.")

        # 2.2 Ban biên tập lấy danh sách chờ duyệt
        res_pending = requests.get(f"{BASE_URL}/auth/pending-reviewers", headers=headers_editor)
        assert res_pending.status_code == 200
        pending_list = res_pending.json()
        my_application = next((x for x in pending_list if x["maNguoiDung"] == created_author_id), None)
        assert my_application is not None, "Không tìm thấy đơn đăng ký trong hàng đợi!"
        application_id = my_application["maDon"]
        print(f"  [✓] 2.2 Ban biên tập tìm thấy đơn #{application_id} của {my_application['hoTen']} trong hàng đợi.")

        # 2.3 NEGATIVE TEST: Thử duyệt đơn với mã đơn không tồn tại
        res_approve_fake = requests.post(f"{BASE_URL}/auth/approve-reviewer/999999", headers=headers_editor)
        assert res_approve_fake.status_code == 400, "LỖI BẢO MẬT: Cho phép duyệt đơn với MaDon không tồn tại!"
        print("  [✓] 2.3 Cổng bảo vệ: Từ chối duyệt đơn với MaDon không tồn tại (HTTP 400).")

        # 2.4 Ban biên tập phê duyệt đơn hợp lệ theo maDon
        res_approve = requests.post(f"{BASE_URL}/auth/approve-reviewer/{application_id}", headers=headers_editor)
        assert res_approve.status_code == 200, f"Duyệt đơn thất bại: {res_approve.text}"
        print(f"  [✓] 2.4 Ban biên tập phê duyệt thành công đơn #{application_id} (được gán Vai trò 4).")

        # 2.5 NEGATIVE TEST: Thử duyệt lại đơn đã xử lý
        res_approve_again = requests.post(f"{BASE_URL}/auth/approve-reviewer/{application_id}", headers=headers_editor)
        assert res_approve_again.status_code == 400, "LỖI BẢO MẬT: Cho phép duyệt lại đơn đã phê duyệt!"
        print("  [✓] 2.5 Cổng bảo vệ: Từ chối duyệt lại đơn đã được phê duyệt (HTTP 400).")
        results.append(("TEST 2: Đơn xin phản biện & Phê duyệt hồ sơ", True))

        # =========================================================
        # TEST 3: Giai đoạn 1 & 2 — OWASP File Validation & Tác giả nộp bản thảo
        # =========================================================
        print_separator("TEST 3: Giai đoạn 1 & 2 — Kiểm tra bảo mật tệp OWASP & Nộp bản thảo trực tuyến")

        co_authors = [
            {"hoTen": "TS. Đặng Thành Thi", "email": author_email, "donVi": "HUIT", "laTacGiaLienHe": True, "thuTu": 1},
            {"hoTen": "ThS. Nghiên Cứu Viên", "email": "nghiencuuvien@huit.edu.vn", "donVi": "HUIT", "laTacGiaLienHe": False, "thuTu": 2}
        ]
        proposed_reviewers = [
            {"hoTen": "GS.TS. Cố Vấn", "email": "covan@vnuhcm.edu.vn", "donVi": "ĐHQG-HCM", "linhVuc": "Trí tuệ nhân tạo", "laChuyenGiaHeThong": False}
        ]

        submit_base_data = {
            "TieuDe": f"Nghiên cứu kiến trúc phản biện kín và xuất bản số HUIT ({ts})",
            "TieuDeTiengAnh": "Research on Double-Blind Review and Digital Publishing Architecture",
            "TomTat": "Nghiên cứu phân tích các điểm kiểm soát chất lượng trong quy trình xuất bản số học thuật...",
            "TuKhoa": "Publishing, Double-Blind, Editorial, HUIT",
            "MaChuyenNganh": 1,
            "DongTacGiaJson": json.dumps(co_authors),
            "PhanBienDeXuatJson": json.dumps(proposed_reviewers)
        }

        # 3.1 NEGATIVE TEST: Tải lên tệp định dạng không được phép (.exe)
        bad_exe_file = {"TapTinBanThao": ("malware.exe", b"MZ\x90\x00\x03\x00\x00\x00", "application/x-msdownload")}
        res_bad_ext = requests.post(f"{BASE_URL}/baibao/submit", headers=headers_author, data=submit_base_data, files=bad_exe_file)
        assert res_bad_ext.status_code == 400, "LỖI BẢO MẬT: Cho phép nộp tệp .exe!"
        print("  [✓] 3.1 OWASP Guard: Chặn tải lên tệp có phần mở rộng không hợp lệ (.exe) (HTTP 400).")

        # 3.2 NEGATIVE TEST: Tải lên tệp .pdf giả mạo (thiếu magic bytes %PDF-)
        fake_pdf = {"TapTinBanThao": ("fake.pdf", b"This is not a real PDF file content", "application/pdf")}
        res_fake_pdf = requests.post(f"{BASE_URL}/baibao/submit", headers=headers_author, data=submit_base_data, files=fake_pdf)
        assert res_fake_pdf.status_code == 400, "LỖI BẢO MẬT: Cho phép nộp PDF giả mạo!"
        print("  [✓] 3.2 OWASP Guard: Chặn tệp PDF không có magic bytes '%PDF-' (HTTP 400).")

        # 3.3 NEGATIVE TEST: Tải lên tệp .pdf thiếu thẻ %%EOF
        broken_pdf = {"TapTinBanThao": ("broken.pdf", b"%PDF-1.5 Some broken stream without eof tag", "application/pdf")}
        res_broken_pdf = requests.post(f"{BASE_URL}/baibao/submit", headers=headers_author, data=submit_base_data, files=broken_pdf)
        assert res_broken_pdf.status_code == 400, "LỖI BẢO MẬT: Cho phép nộp PDF thiếu cấu trúc %%EOF!"
        print("  [✓] 3.3 OWASP Guard: Chặn tệp PDF cấu trúc không toàn vẹn (thiếu '%%EOF') (HTTP 400).")

        # 3.4 NEGATIVE TEST: Tải lên tệp .pdf chứa payload /Launch nguy hiểm
        malicious_pdf = {"TapTinBanThao": ("exploit.pdf", b"%PDF-1.5 /Launch << /Action /Launch /F (cmd.exe) >> %%EOF", "application/pdf")}
        res_mal_pdf = requests.post(f"{BASE_URL}/baibao/submit", headers=headers_author, data=submit_base_data, files=malicious_pdf)
        assert res_mal_pdf.status_code == 400, "LỖI BẢO MẬT: Cho phép tệp chứa lệnh /Launch!"
        print("  [✓] 3.4 OWASP Guard: Quét và chặn tệp PDF chứa mã lệnh nguy hiểm '/Launch' (HTTP 400).")

        # 3.5 Nộp bài hợp lệ với tệp PDF chuẩn cấu trúc
        valid_manuscript_bytes = make_dummy_pdf(f"ORIGINAL SCIENTIFIC MANUSCRIPT BY DANG THANH THI - TS {ts}")
        valid_file = {"TapTinBanThao": ("Original_Manuscript.pdf", valid_manuscript_bytes, "application/pdf")}
        res_submit = requests.post(f"{BASE_URL}/baibao/submit", headers=headers_author, data=submit_base_data, files=valid_file)
        assert res_submit.status_code == 200, f"Nộp bài thất bại: {res_submit.text}"
        submit_res = res_submit.json()
        created_paper_id = submit_res["maBaiBao"]
        print(f"  [✓] 3.5 Nộp bài thành công: Mã bài #{created_paper_id}, Định danh: {submit_res.get('maDinhDanh')}")

        # 3.6 Tác giả tải tệp bản thảo gốc của chính mình
        res_dl_manuscript = requests.get(f"{BASE_URL}/baibao/{created_paper_id}/manuscript", headers=headers_author)
        assert res_dl_manuscript.status_code == 200, "Tác giả không tải được bản thảo gốc"
        assert res_dl_manuscript.content == valid_manuscript_bytes, "Nội dung bản thảo tải về không khớp!"
        print(f"  [✓] 3.6 Tác giả tải tệp bản thảo gốc: Khớp toàn vẹn {len(res_dl_manuscript.content)} bytes.")
        results.append(("TEST 3: Giai đoạn 1 & 2 — OWASP Upload & Nộp bản thảo", True))

        # =========================================================
        # TEST 4: Cổng kiểm soát chuyển trạng thái (State Machine Guards & Negative Tests)
        # =========================================================
        print_separator("TEST 4: Kiểm tra ma trận chuyển trạng thái (State Machine Matrix Guards)")

        # 4.1 NEGATIVE TEST: Thử chuyển thẳng từ 'Chờ sơ duyệt' sang 'Đã xuất bản'
        res_jump_pub = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã xuất bản",
            "ghiChu": "Thử nhảy cóc quy trình"
        })
        assert res_jump_pub.status_code == 400, "LỖI QUY TRÌNH: Cho phép nhảy từ 'Chờ sơ duyệt' sang 'Đã xuất bản'!"
        print("  [✓] 4.1 State Machine Guard: Chặn nhảy trạng thái trái phép 'Chờ sơ duyệt' -> 'Đã xuất bản' (HTTP 400).")

        # 4.2 NEGATIVE TEST: Phân công chuyên gia khi chưa có tệp bản thảo ẩn danh
        res_assign_no_anon = requests.post(f"{BASE_URL}/phanbien/assign", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "maNguoiDungReviewer": rev1_id,
            "soVong": 1
        })
        assert res_assign_no_anon.status_code == 400, "LỖI BẢO MẬT: Cho phép phân công khi chưa có tệp ẩn danh!"
        print("  [✓] 4.2 Double-Blind Guard: Từ chối phân công khi chưa tải lên tệp ẩn danh (HTTP 400).")
        results.append(("TEST 4: Kiểm tra ma trận chuyển trạng thái", True))

        # =========================================================
        # TEST 5: Giai đoạn 3 — Bản thảo ẩn danh, Phân công 2 chuyên gia & Chặn lộ danh tính
        # =========================================================
        print_separator("TEST 5: Giai đoạn 3 — Tải tệp ẩn danh, Phân công 2 chuyên gia & Chặn lộ danh tính")

        # 5.1 Ban biên tập tải lên bản thảo ẩn danh Vòng 1
        anon_content_r1 = make_dummy_pdf(f"ANONYMOUS MANUSCRIPT ROUND 1 - DOUBLE BLIND - PAPER #{created_paper_id}")
        res_upload_anon = requests.post(
            f"{BASE_URL}/baibao/{created_paper_id}/upload-anonymous-manuscript?soVong=1",
            headers=headers_editor,
            files={"file": ("Anonymous_R1.pdf", anon_content_r1, "application/pdf")}
        )
        assert res_upload_anon.status_code == 200, f"Tải tệp ẩn danh thất bại: {res_upload_anon.text}"
        print("  [✓] 5.1 Ban biên tập tải lên tệp bản thảo ẩn danh Vòng 1.")

        # 5.2 Ban biên tập phân công Chuyên gia 1 (dangvang, ID=4)
        res_assign_1 = requests.post(f"{BASE_URL}/phanbien/assign", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "maNguoiDungReviewer": rev1_id,
            "soVong": 1
        })
        assert res_assign_1.status_code == 200, f"Phân công chuyên gia 1 thất bại: {res_assign_1.text}"
        assign_1_id = res_assign_1.json()["maPhanCong"]
        print(f"  [✓] 5.2 Phân công Chuyên gia 1 thành công (Mã phân công #{assign_1_id}).")

        # 5.3 NEGATIVE TEST: Ra quyết định khi chỉ mới có 1 chuyên gia phản biện
        res_dec_early_1rev = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Chờ chỉnh sửa",
            "ghiChu": "Thử quyết định khi mới phân công 1 người"
        })
        assert res_dec_early_1rev.status_code == 400, "LỖI QUY CHẾ: Cho phép quyết định khi mới có 1 chuyên gia!"
        print(f"  [✓] 5.3 Workflow Guard: Chặn quyết định khi chưa đủ 2 chuyên gia ({res_dec_early_1rev.json().get('message')}).")

        # 5.4 Ban biên tập phân công Chuyên gia 2 (tranthih, ID=5)
        res_assign_2 = requests.post(f"{BASE_URL}/phanbien/assign", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "maNguoiDungReviewer": rev2_id,
            "soVong": 1
        })
        assert res_assign_2.status_code == 200, f"Phân công chuyên gia 2 thất bại: {res_assign_2.text}"
        assign_2_id = res_assign_2.json()["maPhanCong"]
        print(f"  [✓] 5.4 Phân công Chuyên gia 2 thành công (Mã phân công #{assign_2_id}).")

        # 5.5 Kiểm tra tính bảo mật phản biện kín phía tác giả
        res_author_view = requests.get(f"{BASE_URL}/baibao/{created_paper_id}", headers=headers_author)
        assert res_author_view.status_code == 200
        author_detail = res_author_view.json()

        # Kiểm tra danh sách tệp: Tác giả tuyệt đối không thấy tệp ẩn danh
        tap_tins = author_detail.get("tapTins", [])
        for t in tap_tins:
            loai = t.get("loaiThuMuc", "")
            assert "ẩn danh" not in loai.lower(), f"LỖI LỘ TỆP: Tác giả nhìn thấy tệp ẩn danh '{loai}'!"

        # Kiểm tra lịch sử trạng thái: Tuyệt đối không chứa tên/username của 2 chuyên gia
        for h in author_detail.get("lichSuTrangThais", []):
            ghi_chu = h.get("ghiChu", "")
            nguoi_thuc_hien = str(h.get("nguoiThucHien", ""))
            assert "dangvang" not in ghi_chu.lower(), "LỖI LỘ DANH TÍNH: Username reviewer 1 lộ trong ghiChu tác giả!"
            assert "tranvann" not in ghi_chu.lower(), "LỖI LỘ DANH TÍNH: Username reviewer 2 lộ trong ghiChu tác giả!"
            assert "Đặng Văn G" not in ghi_chu, "LỖI LỘ DANH TÍNH: Tên reviewer 1 lộ trong ghiChu tác giả!"
            assert "Trần Văn N" not in ghi_chu, "LỖI LỘ DANH TÍNH: Tên reviewer 2 lộ trong ghiChu tác giả!"
            assert "Đặng Văn G" not in nguoi_thuc_hien, "LỖI LỘ DANH TÍNH: Tên reviewer 1 lộ trong nguoiThucHien!"
            assert "Trần Văn N" not in nguoi_thuc_hien, "LỖI LỘ DANH TÍNH: Tên reviewer 2 lộ trong nguoiThucHien!"
        print("  [✓] 5.5 Bảo mật Double-Blind: Tác giả hoàn toàn không nhìn thấy tệp ẩn danh và danh tính 2 chuyên gia.")
        results.append(("TEST 5: Giai đoạn 3 — Phân công 2 chuyên gia & Bảo mật ẩn danh", True))

        # =========================================================
        # TEST 6: Giai đoạn 4 — Thẩm định & Phiếu đánh giá BM-04 từ 2 chuyên gia
        # =========================================================
        print_separator("TEST 6: Giai đoạn 4 — Thẩm định độc lập & Nộp phiếu BM-04 từ 2 chuyên gia")

        # 6.1 NEGATIVE TEST: Ban biên tập thử ra quyết định khi 0/2 chuyên gia nộp phiếu
        res_dec_0eval = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Chờ chỉnh sửa",
            "ghiChu": "Thử quyết định khi 0/2 chuyên gia gửi phiếu"
        })
        assert res_dec_0eval.status_code == 400, "LỖI QUY CHẾ: Cho phép quyết định khi 0/2 chuyên gia gửi phiếu BM-04!"
        print("  [✓] 6.1 Workflow Guard: Chặn quyết định khi 0/2 chuyên gia gửi phiếu BM-04 (HTTP 400).")

        # 6.2 Chuyên gia 1 tải tệp ẩn danh & nộp phiếu BM-04
        res_dl_anon_1 = requests.get(f"{BASE_URL}/phanbien/assignments/{assign_1_id}/manuscript", headers=headers_rev1)
        assert res_dl_anon_1.status_code == 200
        assert res_dl_anon_1.content == anon_content_r1, "Tệp Chuyên gia 1 nhận được không khớp tệp ẩn danh!"

        eval_dto_1 = {
            "maPhanCong": assign_1_id,
            "diemTinhMoi": 8.5,
            "diemPhuongPhap": 8.0,
            "diemKetQua": 8.5,
            "diemTrinhBay": 9.0,
            "diemTongKet": 8.5,
            "nhanXetChoTacGia": "Nghiên cứu có cấu trúc tốt, cần bổ sung bảng so sánh thực nghiệm với các phương pháp trước.",
            "nhanXetBaoMat": "Tác giả có phương pháp phù hợp, đề xuất cho chỉnh sửa để nâng cao chất lượng.",
            "kienNghi": "Chỉnh sửa nhỏ"
        }
        res_eval_1 = requests.post(f"{BASE_URL}/phanbien/evaluate", headers=headers_rev1, json=eval_dto_1)
        assert res_eval_1.status_code == 200, f"Gửi phiếu BM-04 Chuyên gia 1 thất bại: {res_eval_1.text}"
        print("  [✓] 6.2 Chuyên gia 1 (dangvang): Đã thẩm định và nộp phiếu BM-04.")

        # 6.3 NEGATIVE TEST: Thử ra quyết định khi mới có 1/2 chuyên gia nộp phiếu
        res_dec_1eval = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Chờ chỉnh sửa",
            "ghiChu": "Thử quyết định khi mới 1/2 chuyên gia gửi phiếu"
        })
        assert res_dec_1eval.status_code == 400, "LỖI QUY CHẾ: Cho phép quyết định khi mới có 1/2 chuyên gia gửi phiếu!"
        print(f"  [✓] 6.3 Workflow Guard: Chặn quyết định khi chưa đủ 2/2 chuyên gia ({res_dec_1eval.json().get('message')}).")

        # 6.4 Chuyên gia 2 tải tệp ẩn danh & nộp phiếu BM-04
        res_dl_anon_2 = requests.get(f"{BASE_URL}/phanbien/assignments/{assign_2_id}/manuscript", headers=headers_rev2)
        assert res_dl_anon_2.status_code == 200
        assert res_dl_anon_2.content == anon_content_r1, "Tệp Chuyên gia 2 nhận được không khớp tệp ẩn danh!"

        eval_dto_2 = {
            "maPhanCong": assign_2_id,
            "diemTinhMoi": 9.0,
            "diemPhuongPhap": 8.5,
            "diemKetQua": 8.5,
            "diemTrinhBay": 8.5,
            "diemTongKet": 8.6,
            "nhanXetChoTacGia": "Nghiên cứu có giá trị thực tiễn cao, đề nghị bổ sung trích dẫn các nghiên cứu gần nhất năm 2025.",
            "nhanXetBaoMat": "Đề xuất chấp nhận sau khi hoàn thiện chỉnh sửa nhỏ theo nhận xét.",
            "kienNghi": "Chỉnh sửa nhỏ"
        }
        res_eval_2 = requests.post(f"{BASE_URL}/phanbien/evaluate", headers=headers_rev2, json=eval_dto_2)
        assert res_eval_2.status_code == 200, f"Gửi phiếu BM-04 Chuyên gia 2 thất bại: {res_eval_2.text}"
        print("  [✓] 6.4 Chuyên gia 2 (tranvann): Đã thẩm định và nộp phiếu BM-04.")
        results.append(("TEST 6: Giai đoạn 4 — Thẩm định & Phiếu BM-04 từ 2 chuyên gia", True))

        # =========================================================
        # TEST 7: Giai đoạn 5 — Quyết định 'Chờ chỉnh sửa' & Tác giả nộp BM-03 Vòng 2
        # =========================================================
        print_separator("TEST 7: Giai đoạn 5 — Quyết định biên tập & Tác giả nộp BM-03 Vòng 2")

        # 7.1 Ban biên tập ra quyết định 'Chờ chỉnh sửa' (giờ đây đã đủ 2/2 phiếu)
        res_dec_edit = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Chờ chỉnh sửa",
            "ghiChu": "Kính gửi tác giả: Đề nghị chỉnh sửa theo góp ý của 2 chuyên gia phản biện trong phiếu BM-04."
        })
        assert res_dec_edit.status_code == 200, f"Quyết định biên tập thất bại: {res_dec_edit.text}"
        print("  [✓] 7.1 Ban biên tập chuyển trạng thái bài báo sang 'Chờ chỉnh sửa' thành công.")

        # 7.2 Tác giả nộp bản sửa và BM-03 (cấu trúc PDF hợp lệ)
        clean_content = make_dummy_pdf("CLEAN REVISED MANUSCRIPT ROUND 2 - FULLY INCORPORATING REVIEWER FEEDBACK")
        bm03_content = make_dummy_pdf("FORM BM-03 AUTHOR EXPLANATION AND RESPONSE MATRIX ROUND 2")
        tracked_content = make_dummy_pdf("TRACK CHANGES REVISED MANUSCRIPT ROUND 2")

        res_resubmit = requests.post(
            f"{BASE_URL}/baibao/{created_paper_id}/resubmit",
            headers=headers_author,
            data={"GiaiTrinh": "Chúng tôi đã bổ sung đầy đủ bảng so sánh thực nghiệm và tài liệu trích dẫn 2025 theo góp ý của 2 chuyên gia."},
            files={
                "FileClean": ("Clean_Paper_R2.pdf", clean_content, "application/pdf"),
                "FileBm03": ("BM03_GiaiTrinh.pdf", bm03_content, "application/pdf"),
                "FileTracked": ("TrackChanges_R2.pdf", tracked_content, "application/pdf")
            }
        )
        assert res_resubmit.status_code == 200, f"Nộp lại bản sửa thất bại: {res_resubmit.text}"
        print("  [✓] 7.2 Tác giả nộp bản chỉnh sửa Vòng 2 và giải trình BM-03 thành công (Trạng thái chuyển sang 'Chờ quyết định').")
        results.append(("TEST 7: Giai đoạn 5 — Chờ chỉnh sửa & Nộp bản sửa Vòng 2", True))

        # =========================================================
        # TEST 8: Giai đoạn 6 — Chấp nhận đăng, Xếp số báo & Tải PDF thành phẩm
        # =========================================================
        print_separator("TEST 8: Giai đoạn 6 — Chấp nhận đăng, Xếp số phát hành & Tải PDF thành phẩm")

        # 8.1 Ban biên tập chấp nhận đăng bài
        res_accept = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã chấp nhận",
            "ghiChu": "Bài báo đáp ứng chuẩn khoa học, chuyển sang khâu chế bản và xếp số phát hành."
        })
        assert res_accept.status_code == 200, f"Chấp nhận bài thất bại: {res_accept.text}"
        print("  [✓] 8.1 Ban biên tập chuyển trạng thái sang 'Đã chấp nhận'.")

        # 8.2 NEGATIVE TEST: Thử xuất bản khi chưa xếp số và chưa có PDF thành phẩm
        res_pub_early = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã xuất bản",
            "ghiChu": "Thử xuất bản sớm"
        })
        assert res_pub_early.status_code == 400, "LỖI QUY TRÌNH: Cho phép xuất bản khi chưa xếp số và chưa có PDF thành phẩm!"
        print(f"  [✓] 8.2 Cổng bảo vệ: Chặn xuất bản khi chưa đủ điều kiện ({res_pub_early.json().get('message')}).")

        # 8.3 Ban biên tập xếp bài báo vào Số 42 (MaSoTapChi = 1, Đã phát hành, Trang 73-88)
        res_assign_issue = requests.post(f"{BASE_URL}/baibao/{created_paper_id}/assign-issue", headers=headers_editor, json={
            "maSoTapChi": 1,
            "trangBatDau": 73,
            "trangKetThuc": 88,
            "maDOI": f"10.59876/huit.jsc.2026.42.06_{created_paper_id}"
        })
        assert res_assign_issue.status_code == 200, f"Xếp số thất bại: {res_assign_issue.text}"
        print("  [✓] 8.3 Ban biên tập xếp bài báo vào Số 42 (Trang 73-88, có mã DOI).")

        # 8.4 NEGATIVE TEST: Thử xuất bản khi đã xếp số nhưng CHƯA có PDF thành phẩm
        res_pub_no_pdf = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã xuất bản",
            "ghiChu": "Thử xuất bản khi chưa có tệp PDF thành phẩm"
        })
        assert res_pub_no_pdf.status_code == 400, "LỖI QUY TRÌNH: Cho phép xuất bản khi chưa có PDF thành phẩm!"
        print(f"  [✓] 8.4 Cổng bảo vệ: Chặn xuất bản khi chưa có PDF thành phẩm ({res_pub_no_pdf.json().get('message')}).")

        # 8.5 Ban biên tập tải lên tệp PDF xuất bản thành phẩm
        published_pdf_bytes = make_dummy_pdf(f"OFFICIAL PUBLISHED PRODUCTION PDF FOR ARTICLE #{created_paper_id} - VOL 42")
        res_upload_pub_pdf = requests.post(
            f"{BASE_URL}/baibao/{created_paper_id}/upload-published-pdf",
            headers=headers_editor,
            files={"file": (f"Official_Published_Paper_{created_paper_id}.pdf", published_pdf_bytes, "application/pdf")}
        )
        assert res_upload_pub_pdf.status_code == 200, f"Tải PDF xuất bản thất bại: {res_upload_pub_pdf.text}"
        print("  [✓] 8.5 Ban biên tập tải lên tệp PDF xuất bản thành phẩm.")

        # 8.6 Ban biên tập chuyển trạng thái sang 'Đã xuất bản'
        res_pub_final = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã xuất bản",
            "ghiChu": "Phát hành chính thức trên Cổng thông tin Tạp chí."
        })
        assert res_pub_final.status_code == 200, f"Chuyển trạng thái xuất bản thất bại: {res_pub_final.text}"
        print("  [✓] 8.6 Ban biên tập chính thức công bố: Bài báo chuyển sang 'Đã xuất bản'.")
        results.append(("TEST 8: Giai đoạn 6 — Chấp nhận đăng, Xếp số & Xuất bản", True))

        # =========================================================
        # TEST 9: Độc giả công chúng truy cập và Tải PDF chính thức
        # =========================================================
        print_separator("TEST 9: Độc giả công chúng truy cập chi tiết & Tải PDF chính thức")

        # 9.1 Độc giả vãng lai (không đăng nhập) xem chi tiết bài báo
        res_reader_view = requests.get(f"{BASE_URL}/baibao/public/{created_paper_id}")
        assert res_reader_view.status_code == 200, f"Độc giả không xem được bài báo công khai: {res_reader_view.status_code}"
        pub_detail = res_reader_view.json()
        expected_pdf_url = f"/api/baibao/public/{created_paper_id}/pdf"
        assert pub_detail.get("filePdfUrl") == expected_pdf_url, f"filePdfUrl không chính xác: {pub_detail.get('filePdfUrl')}"
        print(f"  [✓] 9.1 Độc giả xem chi tiết bài xuất bản: Mã bài #{created_paper_id}, Số: '{pub_detail.get('tenSoTapChi')}'")
        print(f"      Đường dẫn tải PDF: '{pub_detail.get('filePdfUrl')}'")

        # 9.2 Độc giả tải trực tiếp tệp PDF xuất bản công khai
        res_reader_pdf = requests.get(f"http://localhost:5000{expected_pdf_url}")
        assert res_reader_pdf.status_code == 200, f"Tải PDF công khai thất bại: HTTP {res_reader_pdf.status_code}"
        assert res_reader_pdf.content == published_pdf_bytes, "Nội dung tệp PDF tải về không khớp tệp thành phẩm!"
        print(f"  [✓] 9.2 Độc giả tải tệp PDF công khai: HTTP 200 OK, khớp chính xác 100% {len(res_reader_pdf.content)} bytes.")

        # 9.3 Kiểm tra bài báo xuất hiện trong số tạp chí công khai
        res_issue_check = requests.get(f"{BASE_URL}/sotapchi/1")
        assert res_issue_check.status_code == 200
        issue_articles = res_issue_check.json().get("danhSachBaiBao", [])
        found_in_issue = any(a.get("maBaiBao") == created_paper_id and a.get("filePdfUrl") == expected_pdf_url for a in issue_articles)
        assert found_in_issue, "Bài báo không hiển thị trong danh sách bài của Số 42!"
        print(f"  [✓] 9.3 Số tạp chí 42 phản ánh tức thì bài báo mới với liên kết PDF bảo mật.")

        # 9.4 Kiểm tra danh sách bài mới nhất trên trang chủ
        res_latest = requests.get(f"{BASE_URL}/baibao/public/latest?limit=10")
        assert res_latest.status_code == 200
        latest_articles = res_latest.json()
        found_in_latest = any(a.get("maBaiBao") == created_paper_id for a in latest_articles)
        assert found_in_latest, "Bài báo không hiển thị trong danh sách bài mới nhất!"
        print(f"  [✓] 9.4 Danh sách bài mới nhất (public/latest): Hiển thị bài báo mới nộp #{created_paper_id}.")
        results.append(("TEST 9: Độc giả công chúng truy cập & Tải PDF thành phẩm", True))

    finally:
        # =========================================================
        # CLEANUP: Dọn dẹp CSDL và Tệp đĩa (Bắt buộc thực thi)
        # =========================================================
        print_separator("DỌN DẸP DỮ LIỆU THỬ NGHIỆM SAU KIỂM THỬ")
        # Find any files uploaded for this paper
        if created_paper_id:
            try:
                # Query ThuMucBaiBao via sqlcmd to find physical filenames
                sql_find_files = f"SET QUOTED_IDENTIFIER ON; SELECT DuongDan FROM ThuMucBaiBao WHERE MaBaiBao = {created_paper_id};"
                proc_f = subprocess.run(
                    ["sqlcmd", "-S", ".", "-d", TEST_DB, "-E", "-I", "-C", "-f", "65001", "-Q", sql_find_files],
                    capture_output=True, text=True
                )
                for line in proc_f.stdout.splitlines():
                    line = line.strip()
                    if line.startswith("/Uploads/") or line.startswith("Uploads/"):
                        rel_path = line.lstrip("/")
                        full_path = Path("Backend/HuitJournal.Api") / rel_path
                        tracked_disk_files.append(full_path)
            except Exception as e:
                print(f"  [!] Cảnh báo tìm tệp đĩa: {e}")

        clean_database_and_disk(paper_id=created_paper_id, author_id=created_author_id, uploaded_disk_files=tracked_disk_files)

    # =========================================================
    # TỔNG KẾT
    # =========================================================
    print_separator("BÁO CÁO KẾT QUẢ KIỂM THỬ TÍCH HỢP (INTEGRATION TEST REPORT)")
    all_passed = True
    for name, passed in results:
        status_text = "PASS" if passed else "FAIL"
        print(f"  [{status_text}] {name}")
        if not passed:
            all_passed = False

    print("\n" + "=" * 80)
    if all_passed and len(results) == 9:
        print(" KẾT QUẢ: 9/9 NHÓM KIỂM THỬ TÍCH HỢP ĐẠT KẾT QUẢ (PASS)!")
        print(f" Luồng 6 giai đoạn liên thông xuyên suốt trên CSDL kiểm thử độc lập {TEST_DB}.")
    else:
        print(" CẢNH BÁO: MỘT HOẶC NHIỀU BỘ KIỂM THỬ CHƯA ĐẠT!")
    print("=" * 80 + "\n")

if __name__ == "__main__":
    try:
        run_tests()
    except Exception as e:
        print(f"\n[LỖI THỰC THI]: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
