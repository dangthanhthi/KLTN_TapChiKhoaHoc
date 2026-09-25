"""
Comprehensive End-to-End Integration Test Suite: 6-Stage Scientific Publishing Workflow
Tested against Live Backend API (.NET 9) and SQL Server Database (QL_TapChiKhoaHoc)
Author: Đặng Thành Thi (Web & Backend API Developer)

Verifies:
1. Multi-role Authentication & RBAC (Admin, Editor, Reviewer, Author)
2. Reviewer Application & Approval Workflow
3. Author Online Submission (5-step data + manuscript file)
4. Double-Blind Anonymization & Assignment (with Privacy Leak Protection Check)
5. Reviewer Desk & BM-04 Peer Review Evaluation
6. Editorial Workflow Transition Guards (Pre-review & Pre-publishing validation)
7. Author Revision Submission (BM-03 Response Form & Tracked Changes)
8. Editorial Issue Assignment & Production PDF Upload
9. Public Portal Publishing & Official PDF Download (Unauthenticated Reader)
10. Complete Post-Test Database Cleanup via sqlcmd
"""

import sys
import io
import json
import time
import requests
import subprocess

# Set stdout encoding to UTF-8
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE_URL = "http://localhost:5000/api"

def print_separator(title=""):
    print("\n" + "=" * 75)
    if title:
        print(f" {title.upper()} ")
        print("=" * 75)

def clean_database(paper_id=None, author_id=None):
    """Clean up test records from SQL Server using sqlcmd"""
    try:
        sql_cmds = ""
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
            DELETE FROM NguoiDung_VaiTro WHERE MaNguoiDung = {author_id};
            DELETE FROM DonDangKyPhanBien WHERE MaNguoiDung = {author_id};
            DELETE FROM NguoiDung WHERE MaNguoiDung = {author_id};
            """
        if sql_cmds:
            proc = subprocess.run(["sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc", "-E", "-C", "-Q", sql_cmds], capture_output=True, text=True)
            if proc.returncode == 0:
                print(f"  [✓] Dọn dẹp sạch sẽ CSDL: Bài #{paper_id}, Tác giả #{author_id}.")
            else:
                print(f"  [!] Cảnh báo dọn dẹp CSDL: {proc.stderr}")
    except Exception as e:
        print(f"  [!] Lỗi trong quá trình dọn dẹp: {e}")

def run_tests():
    print_separator("KIỂM THỬ TÍCH HỢP TOÀN DIỆN 6 GIAI ĐOẠN XUẤT BẢN KHOA HỌC (LIVE TEST)")
    print("Môi trường: Backend API C# (.NET 9) trên http://localhost:5000 & SQL Server QL_TapChiKhoaHoc")
    print("Phạm vi: Luồng liên thông nghiệp vụ từ nộp bài đến xuất bản công khai")

    results = []
    created_paper_id = None
    created_author_id = None

    try:
        # ---------------------------------------------------------
        # TEST 1: Xác thực & Phân quyền RBAC (Multi-role Authentication)
        # ---------------------------------------------------------
        print_separator("TEST 1: Đăng nhập các vai trò hệ thống (Admin, Biên tập, Phản biện, Tác giả)")
        
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

        # 1.3 Reviewer login
        res_rev = requests.post(f"{BASE_URL}/auth/login", json={"usernameOrEmail": "dangvang", "password": "123456"})
        assert res_rev.status_code == 200, f"Reviewer login failed: {res_rev.text}"
        token_rev = res_rev.json()["token"]
        headers_rev = {"Authorization": f"Bearer {token_rev}"}
        reviewer_id = res_rev.json()["user"]["maNguoiDung"]
        print(f"  [✓] 1.3 Phản biện viên (dangvang, ID={reviewer_id}): Đăng nhập thành công (Role 4)")

        # 1.4 Dynamic Author account registration
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
        print(f"  [✓] 1.4 Đăng ký mới Tác giả ({author_username}, ID={created_author_id}): Thành công")
        results.append(("TEST 1: Xác thực & Phân quyền RBAC", True))

        # ---------------------------------------------------------
        # TEST 2: Đơn xin làm phản biện viên & Phê duyệt cấp quyền
        # ---------------------------------------------------------
        print_separator("TEST 2: Quy trình thẩm định đơn xin gia nhập Hội đồng phản biện")
        
        # 2.1 Tác giả gửi đơn
        res_apply = requests.post(f"{BASE_URL}/auth/request-reviewer", headers=headers_author, json={
            "ghiChu": "Kính gửi Ban biên tập: Tôi có 8 năm kinh nghiệm nghiên cứu AI & NLP, mong muốn tham gia phản biện."
        })
        assert res_apply.status_code == 200, f"Nộp đơn thất bại: {res_apply.text}"
        print(f"  [✓] 2.1 Tác giả gửi đơn đăng ký làm phản biện viên.")

        # 2.2 Ban biên tập lấy danh sách chờ duyệt
        res_pending = requests.get(f"{BASE_URL}/auth/pending-reviewers", headers=headers_editor)
        assert res_pending.status_code == 200
        pending_list = res_pending.json()
        my_application = next((x for x in pending_list if x["maNguoiDung"] == created_author_id), None)
        assert my_application is not None, "Không tìm thấy đơn đăng ký trong hàng đợi!"
        print(f"  [✓] 2.2 Ban biên tập duyệt hàng đợi: Tìm thấy đơn #{my_application['maDon']} của {my_application['hoTen']}")

        # 2.3 Phê duyệt đơn (kiểm tra hỗ trợ cả maDon)
        res_approve = requests.post(f"{BASE_URL}/auth/approve-reviewer/{my_application['maDon']}", headers=headers_editor)
        assert res_approve.status_code == 200, f"Duyệt đơn thất bại: {res_approve.text}"
        print(f"  [✓] 2.3 Ban biên tập phê duyệt đơn đăng ký phản biện thành công.")
        results.append(("TEST 2: Đơn xin phản biện & Phê duyệt hồ sơ", True))

        # ---------------------------------------------------------
        # TEST 3: Giai đoạn 1 & 2 — Tác giả nộp bản thảo và Khởi tạo hồ sơ
        # ---------------------------------------------------------
        print_separator("TEST 3: Giai đoạn 1 & 2 — Tác giả nộp bản thảo trực tuyến 5 bước")

        co_authors = [
            {"hoTen": "TS. Đặng Thành Thi", "email": author_email, "donVi": "HUIT", "laTacGiaLienHe": True, "thuTu": 1},
            {"hoTen": "ThS. Nghiên Cứu Viên", "email": "nghiencuuvien@huit.edu.vn", "donVi": "HUIT", "laTacGiaLienHe": False, "thuTu": 2}
        ]
        proposed_reviewers = [
            {"hoTen": "GS.TS. Cố Vấn", "email": "covan@vnuhcm.edu.vn", "donVi": "ĐHQG-HCM", "linhVuc": "Trí tuệ nhân tạo", "laChuyenGiaHeThong": False}
        ]
        manuscript_content = b"%PDF-1.5 ORIGINAL MANUSCRIPT CONTENT FOR E2E TEST - HUIT JOURNAL 2026..."
        files = {
            "TapTinBanThao": ("Manuscript_Original.pdf", manuscript_content, "application/pdf")
        }
        submit_data = {
            "TieuDe": f"Nghiên cứu kiến trúc phản biện kín và xuất bản số HUIT ({ts})",
            "TieuDeTiengAnh": "Research on Double-Blind Review and Digital Publishing Architecture",
            "TomTat": "Nghiên cứu phân tích các điểm kiểm soát chất lượng trong quy trình xuất bản số học thuật...",
            "TuKhoa": "Publishing, Double-Blind, Editorial, HUIT",
            "MaChuyenNganh": 1,
            "DongTacGiaJson": json.dumps(co_authors),
            "PhanBienDeXuatJson": json.dumps(proposed_reviewers)
        }

        res_submit = requests.post(f"{BASE_URL}/baibao/submit", headers=headers_author, data=submit_data, files=files)
        assert res_submit.status_code == 200, f"Nộp bài thất bại: {res_submit.text}"
        submit_res = res_submit.json()
        created_paper_id = submit_res["maBaiBao"]
        print(f"  [✓] 3.1 Nộp bài trực tuyến thành công: Mã bài #{created_paper_id}, Mã định danh: {submit_res.get('maDinhDanh')}")

        # Tác giả tải tệp bản thảo gốc của chính mình
        res_dl_manuscript = requests.get(f"{BASE_URL}/baibao/{created_paper_id}/manuscript", headers=headers_author)
        assert res_dl_manuscript.status_code == 200, "Tác giả không tải được bản thảo gốc"
        assert res_dl_manuscript.content == manuscript_content, "Nội dung bản thảo tải về không khớp!"
        print(f"  [✓] 3.2 Tác giả tải tệp bản thảo gốc: Khớp toàn vẹn {len(res_dl_manuscript.content)} bytes.")
        results.append(("TEST 3: Giai đoạn 1 & 2 — Nộp bản thảo & Lưu trữ tệp", True))

        # ---------------------------------------------------------
        # TEST 4: Giai đoạn 3 — Bản thảo ẩn danh, Phân công & Chặn lộ danh tính
        # ---------------------------------------------------------
        print_separator("TEST 4: Giai đoạn 3 — Bản thảo ẩn danh, Phân công & Chặn lộ danh tính (Double-Blind)")

        # 4.1 Phân công khi chưa có tệp ẩn danh -> Phải bị từ chối
        res_assign_no_anon = requests.post(f"{BASE_URL}/phanbien/assign", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "maNguoiDungReviewer": reviewer_id,
            "soVong": 1
        })
        assert res_assign_no_anon.status_code == 400, "LỖI BẢO MẬT: Cho phép phân công khi chưa có bản thảo ẩn danh!"
        print(f"  [✓] 4.1 Cổng bảo vệ: Từ chối phân công khi chưa có tệp ẩn danh (HTTP 400).")

        # 4.2 Ban biên tập tải lên bản thảo ẩn danh Vòng 1
        anon_content_r1 = b"%PDF-1.5 ANONYMOUS MANUSCRIPT ROUND 1 - ALL AUTHOR METADATA STRIPPED"
        res_upload_anon = requests.post(
            f"{BASE_URL}/baibao/{created_paper_id}/upload-anonymous-manuscript?soVong=1",
            headers=headers_editor,
            files={"file": ("Anonymous_R1.pdf", anon_content_r1, "application/pdf")}
        )
        assert res_upload_anon.status_code == 200, f"Tải tệp ẩn danh thất bại: {res_upload_anon.text}"
        print("  [✓] 4.2 Ban biên tập tải lên tệp bản thảo ẩn danh Vòng 1.")

        # 4.3 Phân công phản biện viên (dangvang / Đặng Văn G)
        res_assign_ok = requests.post(f"{BASE_URL}/phanbien/assign", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "maNguoiDungReviewer": reviewer_id,
            "soVong": 1
        })
        assert res_assign_ok.status_code == 200, f"Phân công thất bại: {res_assign_ok.text}"
        assignment_id = res_assign_ok.json()["maPhanCong"]
        print(f"  [✓] 4.3 Phân công chuyên gia phản biện thành công (Mã phân công #{assignment_id}).")

        # 4.4 Kiểm tra tính bảo mật phản biện kín phía tác giả
        res_author_view = requests.get(f"{BASE_URL}/baibao/{created_paper_id}", headers=headers_author)
        assert res_author_view.status_code == 200
        author_detail = res_author_view.json()
        for h in author_detail.get("lichSuTrangThais", []):
            ghi_chu = h.get("ghiChu", "")
            nguoi_thuc_hien = h.get("nguoiThucHien", "")
            assert "Đặng Văn G" not in ghi_chu, "LỖI LỘ DANH TÍNH: Tên phản biện viên lộ trong ghiChu của tác giả!"
            assert "dangvang" not in ghi_chu.lower(), "LỖI LỘ DANH TÍNH: Username phản biện lộ trong ghiChu của tác giả!"
            assert "Đặng Văn G" not in str(nguoi_thuc_hien), "LỖI LỘ DANH TÍNH: Tên phản biện lộ trong nguoiThucHien!"
        print("  [✓] 4.4 Xác minh bảo mật Double-Blind: Tác giả hoàn toàn không thấy danh tính chuyên gia phản biện.")
        results.append(("TEST 4: Giai đoạn 3 — Phân công kín & Bảo mật ẩn danh", True))

        # ---------------------------------------------------------
        # TEST 5: Giai đoạn 4 — Thẩm định & Phiếu đánh giá BM-04
        # ---------------------------------------------------------
        print_separator("TEST 5: Giai đoạn 4 — Chuyên gia phản biện tải tệp ẩn danh & Nộp phiếu BM-04")

        # 5.1 Chuyên gia kiểm tra bàn làm việc
        res_my_jobs = requests.get(f"{BASE_URL}/phanbien/my-assignments", headers=headers_rev)
        assert res_my_jobs.status_code == 200
        assert any(j["maPhanCong"] == assignment_id for j in res_my_jobs.json()), "Không tìm thấy nhiệm vụ phân công!"
        print(f"  [✓] 5.1 Bàn làm việc phản biện: Nhận diện đúng nhiệm vụ #{assignment_id}.")

        # 5.2 Chuyên gia tải tệp ẩn danh
        res_dl_anon = requests.get(f"{BASE_URL}/phanbien/assignments/{assignment_id}/manuscript", headers=headers_rev)
        assert res_dl_anon.status_code == 200
        assert res_dl_anon.content == anon_content_r1, "Tệp phản biện viên nhận được không khớp tệp ẩn danh!"
        print(f"  [✓] 5.2 Chuyên gia tải tệp bản thảo: Chính xác là tệp ẩn danh ({len(res_dl_anon.content)} bytes).")

        # 5.3 Chuyên gia nộp phiếu BM-04
        eval_dto = {
            "maPhanCong": assignment_id,
            "diemTinhMoi": 8.5,
            "diemPhuongPhap": 8.0,
            "diemKetQua": 8.5,
            "diemTrinhBay": 9.0,
            "diemTongKet": 8.5,
            "nhanXetChoTacGia": "Nghiên cứu có cấu trúc tốt, cần bổ sung bảng so sánh thực nghiệm với các phương pháp trước.",
            "nhanXetBaoMat": "Tác giả có phương pháp phù hợp, đề xuất cho chỉnh sửa để nâng cao chất lượng.",
            "kienNghi": "Chỉnh sửa nhỏ"
        }
        res_eval = requests.post(f"{BASE_URL}/phanbien/evaluate", headers=headers_rev, json=eval_dto)
        assert res_eval.status_code == 200, f"Gửi phiếu BM-04 thất bại: {res_eval.text}"
        print(f"  [✓] 5.3 Chuyên gia hoàn thành đánh giá và gửi phiếu BM-04.")
        results.append(("TEST 5: Giai đoạn 4 — Thẩm định & Phiếu BM-04", True))

        # ---------------------------------------------------------
        # TEST 6: Cổng kiểm soát chuyển trạng thái (Workflow Transition Guards)
        # ---------------------------------------------------------
        print_separator("TEST 6: Cổng kiểm soát chuyển trạng thái biên tập (Workflow Guards)")

        # 6.1 Thử xuất bản ngay khi chưa xếp vào số và chưa có PDF -> Phải bị chặn
        res_pub_early = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã xuất bản",
            "ghiChu": "Thử nghiệm xuất bản sớm"
        })
        assert res_pub_early.status_code == 400, "LỖI QUY TRÌNH: Cho phép 'Đã xuất bản' khi chưa xếp số / chưa có PDF!"
        print(f"  [✓] 6.1 Cổng bảo vệ: Chặn xuất bản khi chưa đủ điều kiện: '{res_pub_early.json().get('message')}'.")
        results.append(("TEST 6: Cổng kiểm soát chuyển trạng thái", True))

        # ---------------------------------------------------------
        # TEST 7: Giai đoạn 5 — Quyết định 'Chờ chỉnh sửa' & Nộp bản sửa Vòng 2
        # ---------------------------------------------------------
        print_separator("TEST 7: Giai đoạn 5 — Quyết định biên tập & Tác giả nộp BM-03 Vòng 2")

        # 7.1 Ban biên tập ra quyết định 'Chờ chỉnh sửa'
        res_dec_edit = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Chờ chỉnh sửa",
            "ghiChu": "Kính gửi tác giả: Đề nghị chỉnh sửa theo góp ý của phản biện viên trong phiếu BM-04."
        })
        assert res_dec_edit.status_code == 200, f"Quyết định biên tập thất bại: {res_dec_edit.text}"
        print("  [✓] 7.1 Ban biên tập chuyển trạng thái bài báo sang 'Chờ chỉnh sửa'.")

        # 7.2 Tác giả nộp bản sửa và BM-03
        clean_content = b"%PDF-1.5 CLEAN REVISED MANUSCRIPT ROUND 2..."
        bm03_content = b"%PDF-1.5 FORM BM-03 AUTHOR RESPONSE ROUND 2..."
        tracked_content = b"%PDF-1.5 TRACK CHANGES MANUSCRIPT ROUND 2..."
        res_resubmit = requests.post(
            f"{BASE_URL}/baibao/{created_paper_id}/resubmit",
            headers=headers_author,
            data={"GiaiTrinh": "Chúng tôi đã bổ sung đầy đủ bảng so sánh thực nghiệm tại Mục 4 theo yêu cầu phản biện."},
            files={
                "FileClean": ("Clean_Paper_R2.pdf", clean_content, "application/pdf"),
                "FileBm03": ("BM03_GiaiTrinh.pdf", bm03_content, "application/pdf"),
                "FileTracked": ("TrackChanges_R2.pdf", tracked_content, "application/pdf")
            }
        )
        assert res_resubmit.status_code == 200, f"Nộp lại bản sửa thất bại: {res_resubmit.text}"
        print("  [✓] 7.2 Tác giả nộp bản chỉnh sửa Vòng 2 và giải trình BM-03 thành công.")
        results.append(("TEST 7: Giai đoạn 5 — Chờ chỉnh sửa & Nộp bản sửa Vòng 2", True))

        # ---------------------------------------------------------
        # TEST 8: Giai đoạn 6 — Chấp nhận đăng, Xếp số báo & Tải PDF thành phẩm
        # ---------------------------------------------------------
        print_separator("TEST 8: Giai đoạn 6 — Chấp nhận đăng, Xếp số phát hành & Tải PDF thành phẩm")

        # 8.1 Ban biên tập chấp nhận đăng
        res_accept = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã chấp nhận",
            "ghiChu": "Bài báo đáp ứng chuẩn khoa học, chuyển sang khâu chế bản và xếp số phát hành."
        })
        assert res_accept.status_code == 200, f"Chấp nhận bài thất bại: {res_accept.text}"
        print("  [✓] 8.1 Ban biên tập chuyển trạng thái sang 'Đã chấp nhận'.")

        # 8.2 Ban biên tập xếp bài báo vào Số 42 (MaSoTapChi = 1, Đã phát hành)
        res_assign_issue = requests.post(f"{BASE_URL}/baibao/{created_paper_id}/assign-issue", headers=headers_editor, json={
            "maSoTapChi": 1,
            "trangBatDau": 73,
            "trangKetThuc": 88,
            "maDOI": f"10.59876/huit.jsc.2026.42.06_{created_paper_id}"
        })
        assert res_assign_issue.status_code == 200, f"Xếp số thất bại: {res_assign_issue.text}"
        print("  [✓] 8.2 Ban biên tập xếp bài báo vào Số 42 (Trang 73-88, có mã DOI).")

        # 8.3 Ban biên tập tải lên tệp PDF xuất bản thành phẩm
        published_pdf_bytes = b"%PDF-1.5 OFFICIAL PUBLISHED ARTICLE PDF - FINAL PRODUCTION VERSION FOR READERS"
        res_upload_pub_pdf = requests.post(
            f"{BASE_URL}/baibao/{created_paper_id}/upload-published-pdf",
            headers=headers_editor,
            files={"file": (f"Official_Published_Paper_{created_paper_id}.pdf", published_pdf_bytes, "application/pdf")}
        )
        assert res_upload_pub_pdf.status_code == 200, f"Tải PDF xuất bản thất bại: {res_upload_pub_pdf.text}"
        print("  [✓] 8.3 Ban biên tập tải lên tệp PDF xuất bản thành phẩm.")

        # 8.4 Ban biên tập chuyển trạng thái sang 'Đã xuất bản'
        res_pub_final = requests.post(f"{BASE_URL}/phanbien/decision", headers=headers_editor, json={
            "maBaiBao": created_paper_id,
            "trangThaiMoi": "Đã xuất bản",
            "ghiChu": "Phát hành chính thức trên Cổng thông tin Tạp chí."
        })
        assert res_pub_final.status_code == 200, f"Chuyển trạng thái xuất bản thất bại: {res_pub_final.text}"
        print("  [✓] 8.4 Ban biên tập chính thức công bố: Bài báo chuyển sang 'Đã xuất bản'.")
        results.append(("TEST 8: Giai đoạn 6 — Chấp nhận đăng, Xếp số & Xuất bản", True))

        # ---------------------------------------------------------
        # TEST 9: Độc giả công chúng truy cập và Tải PDF chính thức
        # ---------------------------------------------------------
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
        print(f"  [✓] 9.2 Độc giả tải tệp PDF công khai: HTTP 200 OK, khớp chính xác 100% {len(res_reader_pdf.content)} bytes tệp thành phẩm.")

        # 9.3 Kiểm tra bài báo xuất hiện trong số tạp chí công khai
        res_issue_check = requests.get(f"{BASE_URL}/sotapchi/1")
        assert res_issue_check.status_code == 200
        issue_articles = res_issue_check.json().get("danhSachBaiBao", [])
        found_in_issue = any(a.get("maBaiBao") == created_paper_id and a.get("filePdfUrl") == expected_pdf_url for a in issue_articles)
        assert found_in_issue, "Bài báo không hiển thị trong danh sách bài của Số 42!"
        print(f"  [✓] 9.3 Số tạp chí 42 phản ánh tức thì bài báo mới với liên kết PDF bảo mật.")
        results.append(("TEST 9: Độc giả công chúng truy cập & Tải PDF thành phẩm", True))

    finally:
        # ---------------------------------------------------------
        # CLEANUP: Dọn dẹp dữ liệu thử nghiệm
        # ---------------------------------------------------------
        print_separator("DỌN DẸP DỮ LIỆU THỬ NGHIỆM SAU KIỂM THỬ")
        clean_database(paper_id=created_paper_id, author_id=created_author_id)

    # ---------------------------------------------------------
    # TỔNG KẾT
    # ---------------------------------------------------------
    print_separator("BÁO CÁO KẾT QUẢ KIỂM THỬ TÍCH HỢP (INTEGRATION TEST REPORT)")
    all_passed = True
    for name, passed in results:
        status_text = "PASS" if passed else "FAIL"
        print(f"  [{status_text}] {name}")
        if not passed:
            all_passed = False

    print("\n" + "=" * 75)
    if all_passed and len(results) == 9:
        print(" KẾT QUẢ: 9/9 NHÓM KIỂM THỬ TÍCH HỢP ĐẠT KẾT QUẢ (PASS)!")
        print(" Luồng 6 giai đoạn liên thông xuyên suốt giữa CSDL SQL Server và Backend API.")
    else:
        print(" CẢNH BÁO: MỘT HOẶC NHIỀU BỘ KIỂM THỬ CHƯA ĐẠT!")
    print("=" * 75 + "\n")

if __name__ == "__main__":
    try:
        run_tests()
    except Exception as e:
        print(f"\n[LỖI THỰC THI]: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
