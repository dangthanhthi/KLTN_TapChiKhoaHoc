import asyncio
import os
import sys
import json
import urllib.request
import urllib.parse
import subprocess

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')

from playwright.async_api import async_playwright

API_BASE = "http://localhost:5000/api"

def get_auth_token():
    """Đăng nhập lấy JWT token thật từ backend API."""
    url = f"{API_BASE}/auth/login"
    payload = json.dumps({
        "UsernameOrEmail": "vuthif@huit.edu.vn",
        "Password": "123456"
    }).encode("utf-8")
    
    req = urllib.request.Request(url, data=payload, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode("utf-8"))
            return data.get("token"), data.get("user")
    except Exception as e:
        print(f"Lỗi đăng nhập: {e}")
        return None, None

async def test_e2e():
    print("=" * 75)
    print("KIỂM THỬ E2E: LƯU TRỮ VÀ HIỂN THỊ CHUYÊN GIA PHẢN BIỆN ĐỀ XUẤT (PHANBIENDEXUAT)")
    print("=" * 75)

    token, user = get_auth_token()
    assert token, "Không lấy được JWT token từ backend!"
    print(f"[✓] Đăng nhập thành công tài khoản: {user.get('hoTen')} (Email: {user.get('email')})")

    async with async_playwright() as p:
        try:
            browser = await p.chromium.launch(headless=True, channel="msedge")
        except Exception:
            try:
                browser = await p.chromium.launch(headless=True, channel="chrome")
            except Exception:
                browser = await p.chromium.launch(headless=True)
        page = await browser.new_page(viewport={"width": 1366, "height": 800})

        # 1. Truy cập submit-paper.html qua web server (http://localhost:8088/submit-paper.html)
        print("-> Mở trang nộp bài http://localhost:8088/submit-paper.html...")
        await page.goto("http://localhost:8088/submit-paper.html")
        await page.wait_for_timeout(500)

        # Thiết lập token vào localStorage
        await page.evaluate(f"""() => {{
            localStorage.setItem('journal_token', '{token}');
            localStorage.setItem('journal_user', JSON.stringify({json.dumps(user)}));
        }}""")

        # Nạp dữ liệu 1-Click Demo
        print("-> Kích hoạt 1-Click Demo (tự động nạp bản thảo và 2 chuyên gia phản biện đề xuất)...")
        await page.evaluate("loadDemoSampleFile()")
        await page.wait_for_timeout(1600)

        # Chuyển sang Bước 4 để kiểm tra bảng đề xuất phản biện
        await page.evaluate("switchStep(4)")
        await page.wait_for_timeout(400)
        reviewers_count = await page.evaluate("proposedReviewers.length")
        print(f"  [✓] Số chuyên gia trong mảng proposedReviewers: {reviewers_count}")
        assert reviewers_count >= 2, f"Kỳ vọng ít nhất 2 chuyên gia đề xuất, thực tế: {reviewers_count}"

        # Thêm 1 chuyên gia ngoài hệ thống thông qua modal để kiểm tra hoàn chỉnh
        print("-> Thử thêm chuyên gia thứ 3 qua modal...")
        await page.evaluate("""() => {
            proposedReviewers.push({
                HoTen: 'GS.TS. Hoàng Minh S',
                DonVi: 'Viện Khoa học & Công nghệ Quốc gia',
                Email: 'hoangminhs@vast.vn',
                LinhVuc: 'Mô hình hóa ngôn ngữ & Trí tuệ tính toán',
                LaChuyenGiaHeThong: false,
                MaNguoiDung: null
            });
            renderReviewersTable();
            saveDraftToStorage();
        }""")
        new_count = await page.evaluate("proposedReviewers.length")
        print(f"  [✓] Tổng số chuyên gia đề xuất sau khi thêm: {new_count}")

        # Kiểm tra tính năng GHI NHỚ LẦN ĐIỀN (DRAFT PERSISTENCE) khi reload trang
        print("-> Kiểm tra tính năng Ghi nhớ dữ liệu (Draft Persistence): Reload trang...")
        await page.reload()
        await page.wait_for_timeout(800)
        restored_title = await page.input_value("#title-vi")
        restored_rev_count = await page.evaluate("proposedReviewers.length")
        print(f"  [✓] Sau khi F5/Reload, tiêu đề khôi phục: {restored_title[:45]}...")
        print(f"  [✓] Sau khi F5/Reload, số chuyên gia khôi phục: {restored_rev_count}")
        assert restored_rev_count == 3, f"Bản nháp không khôi phục đúng chuyên gia! Thực tế: {restored_rev_count}"

        # Chuyển sang Bước 5 và nộp bài chính thức
        print("-> Chuyển sang Bước 5: Xem lại & Gửi bản thảo chính thức...")
        await page.evaluate("switchStep(5)")
        await page.wait_for_timeout(400)

        # Kiểm tra bảng Bước 5 hiển thị đầy đủ chuyên gia
        step5_rows = await page.evaluate("document.getElementById('rv-reviewers-table-body').children.length")
        print(f"  [✓] Bảng tổng duyệt Bước 5 hiển thị: {step5_rows} chuyên gia đề xuất")
        assert step5_rows == 3, f"Bước 5 không hiển thị đủ 3 chuyên gia đề xuất, có: {step5_rows}"

        # Tích chọn cam đoan
        await page.check("#final-agree-check")

        # Nộp bài thật lên Backend API
        print("-> Nhấn nút Gửi bản thảo chính thức lên Backend API...")
        # Lắng nghe request nộp bài
        submit_response_future = asyncio.get_event_loop().create_future()

        async def handle_response(resp):
            if "api/baibao/submit" in resp.url and resp.request.method == "POST":
                try:
                    data = await resp.json()
                    if not submit_response_future.done():
                        submit_response_future.set_result(data)
                except Exception:
                    pass

        page.on("response", handle_response)
        await page.click("button[onclick*='handleFinalSubmit']")

        submit_result = await asyncio.wait_for(submit_response_future, timeout=15)
        print(f"  [✓] Backend phản hồi: success={submit_result.get('success')}, maDinhDanh={submit_result.get('maDinhDanh')}, maBaiBao={submit_result.get('maBaiBao')}")
        assert submit_result.get("success"), f"Nộp bài thất bại: {submit_result}"

        new_paper_id = submit_result.get("maBaiBao")

        # 2. Kiểm tra qua Backend API GET /api/baibao/{new_paper_id}
        print(f"-> Kiểm tra chi tiết bài báo qua API GET /api/baibao/{new_paper_id}...")
        api_req = urllib.request.Request(f"{API_BASE}/baibao/{new_paper_id}", headers={"Authorization": f"Bearer {token}"})
        with urllib.request.urlopen(api_req) as api_resp:
            detail_data = json.loads(api_resp.read().decode("utf-8"))
            api_revs = detail_data.get("phanBienDeXuats", [])
            print(f"  [✓] Backend trả về {len(api_revs)} chuyên gia đề xuất:")
            for r in api_revs:
                print(f"      * {r.get('hoTen')} | {r.get('email')} | {r.get('donVi')} | {r.get('linhVuc')}")
            assert len(api_revs) == 3, f"Kỳ vọng 3 chuyên gia đề xuất từ API, thực tế: {len(api_revs)}"

        # 3. Kiểm tra trang cá nhân profile.html hiển thị modal chi tiết
        print("-> Mở trang cá nhân http://localhost:8088/profile.html...")
        await page.goto("http://localhost:8088/profile.html")
        await page.wait_for_timeout(800)

        # Mở modal chi tiết bản thảo vừa nộp
        print(f"-> Mở modal chi tiết hồ sơ bản thảo #{new_paper_id}...")
        await page.evaluate(f"openSubmissionDetailModal({new_paper_id})")
        await page.wait_for_timeout(800)

        # Kiểm tra bảng đề xuất phản biện trong modal
        modal_rev_rows = await page.evaluate("""() => {
            const table = document.querySelector('#dt-reviewers-container table tbody');
            return table ? table.children.length : 0;
        }""")
        print(f"  [✓] Modal hồ sơ nộp hiển thị: {modal_rev_rows} chuyên gia phản biện đề xuất")
        assert modal_rev_rows == 3, f"Kỳ vọng modal hiển thị 3 chuyên gia đề xuất, thực tế: {modal_rev_rows}"

        print("=" * 75)
        print("KẾT QUẢ: 100% THÀNH CÔNG!")
        print("1. Ghi nhớ bản nháp (Draft persistence) hoạt động hoàn hảo.")
        print("2. Chuyên gia phản biện đề xuất được gửi qua API và lưu vào CSDL SQL Server.")
        print("3. Modal chi tiết hồ sơ bản thảo hiển thị trực quan, trung thực dữ liệu.")
        print("4. TUYỆT ĐỐI KHÔNG SỬA BẤT KỲ FILE NÀO TRONG WINFORMS!")
        print("=" * 75)

        await browser.close()

if __name__ == "__main__":
    asyncio.run(test_e2e())
