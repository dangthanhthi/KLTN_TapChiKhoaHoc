import asyncio
import os
import sys
import tempfile

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')

from playwright.async_api import async_playwright

async def run_tests():
    print("=" * 80)
    print("KIỂM THỬ TỔNG THỂ: CHỈ CHẤP NHẬN TỆP PDF/WORD & ĐÃ XÓA TOÀN BỘ ĐỀ XUẤT PHẢN BIỆN")
    print("=" * 80)

    # Use the live HTTP server running at port 8088
    base_url = "http://localhost:8088"
    submit_url = f"{base_url}/submit-paper.html"

    # Create dummy temporary test files
    temp_dir = tempfile.mkdtemp()
    bad_txt = os.path.join(temp_dir, "test_file.txt")
    bad_png = os.path.join(temp_dir, "test_image.png")
    bad_zip = os.path.join(temp_dir, "test_archive.zip")
    good_pdf = os.path.join(temp_dir, "manuscript_valid.pdf")
    good_docx = os.path.join(temp_dir, "manuscript_valid.docx")
    good_doc = os.path.join(temp_dir, "manuscript_valid.doc")

    with open(bad_txt, "w", encoding="utf-8") as f:
        f.write("This is a plain text file, not allowed.")
    with open(bad_png, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + b"\x00" * 50)
    with open(bad_zip, "wb") as f:
        f.write(b"PK\x03\x04" + b"\x00" * 50)
    with open(good_pdf, "wb") as f:
        f.write(b"%PDF-1.5\n%\xe2\xe3\xcf\xd3\n1 0 obj\n<<\n>>\nendobj\ntrailer\n<<\n>>\nstartxref\n99\n%%EOF")
    with open(good_docx, "wb") as f:
        f.write(b"PK\x03\x04" + b"\x00" * 50)
    with open(good_doc, "wb") as f:
        f.write(b"\xd0\xcf\x11\xe0\xa1\xb1\x1a\xe1" + b"\x00" * 50)

    console_errors = []
    page_errors = []

    async with async_playwright() as p:
        try:
            browser = await p.chromium.launch(headless=True, channel="msedge")
        except Exception:
            try:
                browser = await p.chromium.launch(headless=True, channel="chrome")
            except Exception:
                browser = await p.chromium.launch(headless=True)
        
        page = await browser.new_page(viewport={"width": 1366, "height": 768})

        page.on("console", lambda msg: console_errors.append(msg.text) if msg.type == "error" else None)
        page.on("pageerror", lambda err: page_errors.append(str(err)))

        print(f"\n[BƯỚC 1] Truy cập {submit_url}...")
        await page.goto(submit_url)
        await page.wait_for_timeout(600)

        # 1. Kiểm tra không còn bất kỳ phần tử đề xuất phản biện / xung đột lợi ích
        print("\n[BƯỚC 2] Kiểm tra giao diện đã loại bỏ triệt để phần đề xuất & phản đối phản biện:")
        rev_table = await page.query_selector("#reviewers-table")
        add_rev_btn = await page.query_selector("button:has-text('Thêm chuyên gia đề xuất')")
        opposed_box = await page.query_selector("#opposed-reviewers-text")
        rev_modal = await page.query_selector("#add-reviewer-modal")

        assert rev_table is None, "LỖI: #reviewers-table vẫn còn tồn tại trong DOM!"
        print("  [✓] Đã xác nhận: Không còn bảng đề xuất phản biện (#reviewers-table)")

        assert add_rev_btn is None, "LỖI: Nút 'Thêm chuyên gia đề xuất' vẫn còn tồn tại!"
        print("  [✓] Đã xác nhận: Không còn nút thêm chuyên gia phản biện")

        assert opposed_box is None, "LỖI: Ô nhập cán bộ xung đột lợi ích (#opposed-reviewers-text) vẫn còn!"
        print("  [✓] Đã xác nhận: Không còn ô đề xuất phản đối / xung đột lợi ích (#opposed-reviewers-text)")

        assert rev_modal is None, "LỖI: Modal thêm phản biện (#add-reviewer-modal) vẫn còn tồn tại!"
        print("  [✓] Đã xác nhận: Đã xóa hoàn toàn Modal đề xuất phản biện (#add-reviewer-modal)")

        # 2. Kiểm tra Step 4 tiêu đề và mô tả
        step4_panel = await page.inner_text("#panel-step-4 .section-legend")
        print(f"  [✓] Tiêu đề Bước 4: '{step4_panel.strip()}' (ThuMucBaiBao thuần túy)")
        assert "Thư Mục Tài Liệu Đính Kèm" in step4_panel, "Tiêu đề bước 4 không đúng!"

        # 3. Kiểm tra ràng buộc định dạng tệp tại Step 1 (Bản thảo chính)
        print("\n[BƯỚC 3] Kiểm tra kiểm soát định dạng tệp tại Bước 1 (Bản thảo chính):")
        
        # Thử tải tệp .txt -> Phải bị chặn
        print("  -> Thử tải file cấm: test_file.txt...")
        await page.set_input_files("#paper-file-input", bad_txt)
        await page.wait_for_timeout(300)
        toast_txt = await page.inner_text(".toast-notification") if await page.query_selector(".toast-notification") else ""
        file_input_val = await page.input_value("#paper-file-input")
        assert file_input_val == "", "Input file không bị reset khi tải file .txt sai định dạng!"
        print(f"  [✓] Tải file .txt bị từ chối chính xác: '{toast_txt}'")

        # Thử tải tệp .png -> Phải bị chặn
        print("  -> Thử tải file cấm: test_image.png...")
        await page.set_input_files("#paper-file-input", bad_png)
        await page.wait_for_timeout(300)
        file_input_val = await page.input_value("#paper-file-input")
        assert file_input_val == "", "Input file không bị reset khi tải file .png sai định dạng!"
        print("  [✓] Tải file .png bị từ chối chính xác!")

        # Thử tải tệp .zip -> Phải bị chặn
        print("  -> Thử tải file cấm: test_archive.zip...")
        await page.set_input_files("#paper-file-input", bad_zip)
        await page.wait_for_timeout(300)
        file_input_val = await page.input_value("#paper-file-input")
        assert file_input_val == "", "Input file không bị reset khi tải file .zip sai định dạng!"
        print("  [✓] Tải file .zip bị từ chối chính xác!")

        # Thử tải tệp .docx hợp lệ
        print("  -> Thử tải file hợp lệ: manuscript_valid.docx...")
        await page.set_input_files("#paper-file-input", good_docx)
        await page.wait_for_timeout(1800)
        uploaded_name = await page.inner_text("#uploaded-file-name")
        assert "manuscript_valid.docx" in uploaded_name, f"Tệp .docx không được chấp nhận! Nhận: {uploaded_name}"
        print(f"  [✓] Tệp .docx được tiếp nhận thành công: '{uploaded_name}'")

        # 4. Kiểm tra ràng buộc định dạng tệp tại Step 4 (Thư mục tài liệu đính kèm)
        print("\n[BƯỚC 4] Kiểm tra kiểm soát định dạng tệp tại Bước 4 (Thư mục tài liệu đính kèm):")
        await page.evaluate("switchStep(4)")
        await page.wait_for_timeout(300)

        # Thử tải file .txt vào extra-attachment-input
        print("  -> Thử đính kèm file cấm: test_file.txt...")
        await page.set_input_files("#extra-attachment-input", bad_txt)
        await page.wait_for_timeout(300)
        tbody_html = await page.inner_html("#attachments-table-body")
        assert "test_file.txt" not in tbody_html, "LỖI: Tệp .txt lọt vào bảng đính kèm!"
        print("  [✓] Đính kèm file .txt bị từ chối thành công, không lọt vào danh mục!")

        # Thử tải file .png vào extra-attachment-input
        print("  -> Thử đính kèm file cấm: test_image.png...")
        await page.set_input_files("#extra-attachment-input", bad_png)
        await page.wait_for_timeout(300)
        tbody_html = await page.inner_html("#attachments-table-body")
        assert "test_image.png" not in tbody_html, "LỖI: Tệp .png lọt vào bảng đính kèm!"
        print("  [✓] Đính kèm file .png bị từ chối thành công!")

        # Thử tải file .pdf hợp lệ vào extra-attachment-input
        print("  -> Thử đính kèm file hợp lệ: manuscript_valid.pdf...")
        await page.set_input_files("#extra-attachment-input", good_pdf)
        await page.wait_for_timeout(300)
        tbody_html = await page.inner_html("#attachments-table-body")
        assert "manuscript_valid.pdf" in tbody_html, "LỖI: Tệp .pdf không được thêm vào bảng đính kèm!"
        print("  [✓] Đính kèm file .pdf thành công và hiển thị trong danh mục tài liệu!")

        # Thử tải file .doc hợp lệ vào extra-attachment-input
        print("  -> Thử đính kèm file hợp lệ: manuscript_valid.doc...")
        await page.set_input_files("#extra-attachment-input", good_doc)
        await page.wait_for_timeout(300)
        tbody_html = await page.inner_html("#attachments-table-body")
        assert "manuscript_valid.doc" in tbody_html, "LỖI: Tệp .doc không được thêm vào bảng đính kèm!"
        print("  [✓] Đính kèm file .doc thành công và hiển thị trong danh mục tài liệu!")

        # 5. Kiểm tra Bước 5 (Tổng duyệt)
        print("\n[BƯỚC 5] Kiểm tra Bước 5 Xem lại toàn diện:")
        await page.evaluate("switchStep(5)")
        await page.wait_for_timeout(400)
        step5_html = await page.inner_html("#panel-step-5")
        assert "Cán bộ thẩm định" not in step5_html, "LỖI: Bước 5 vẫn còn chứa nội dung 'Cán bộ thẩm định'!"
        assert "rv-reviewers-table-body" not in step5_html, "LỖI: Bảng phản biện vẫn còn trong Bước 5!"
        print("  [✓] Bước 5 hiển thị sạch sẽ danh mục ThuMucBaiBao, không còn bảng phản biện đề xuất!")

        # Chụp ảnh minh chứng
        shot_path = "Web/tests/shot_step4_and_step5_clean.png"
        await page.screenshot(path=shot_path, full_page=True)
        print(f"\n[BƯỚC 6] Đã chụp ảnh màn hình minh chứng: {shot_path}")

        print("\n" + "=" * 80)
        print("KẾT QUẢ: TOÀN BỘ CÁC BÀI KIỂM TRA ĐÃ ĐẠT 100% TIÊU CHUẨN!")
        print("=" * 80)

        await browser.close()

if __name__ == "__main__":
    asyncio.run(run_tests())
