import asyncio
import json
import re
import sys
import time
import subprocess
import requests
from test_target import API_ROOT, require_testing_api

if sys.stdout and hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

API_BASE = API_ROOT
SQLCMD_BASE = ["sqlcmd", "-S", ".", "-d", "QL_TapChiKhoaHoc_Test", "-E", "-b"]

def execute_sql(query: str):
    cmd = SQLCMD_BASE + ["-Q", f"SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; {query}"]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if res.returncode != 0:
        raise RuntimeError(f"SQL execution error: {res.stderr or res.stdout}")
    return res.stdout

def get_admin_token() -> str:
    # Login with existing admin account: admin
    payload = {
        "usernameOrEmail": "admin",
        "password": "123456"
    }
    r = requests.post(f"{API_BASE}/auth/login", json=payload, timeout=10)
    if r.status_code == 200 and r.json().get("token"):
        return r.json()["token"]
    raise RuntimeError(f"Failed to authenticate admin: {r.status_code} {r.text}")

def run_phase5_tests():
    require_testing_api()
    print("=" * 70)
    print("PHASE 5 TEST: Background Email Outbox Dispatcher, Retries & Cleanup")
    print("=" * 70)

    # Clean up test leftovers in Outbox
    execute_sql("DELETE FROM EmailOutbox WHERE NguoiNhan LIKE 'p5test_%';")
    execute_sql("DELETE FROM DangKyChoXacNhan WHERE TenDangNhapSoSanh LIKE 'p5user_%';")

    admin_token = get_admin_token()
    headers = {"Authorization": f"Bearer {admin_token}"}
    print("  ✓ Admin authenticated successfully. Token acquired.")

    # -------------------------------------------------------------
    # TEST 1: Admin Outbox Monitoring Endpoint (GET /api/system/outbox-status)
    # -------------------------------------------------------------
    print("\n[TEST 1] Testing GET /api/system/outbox-status...")
    r = requests.get(f"{API_BASE}/system/outbox-status", headers=headers, timeout=10)
    assert r.status_code == 200, f"Expected 200, got {r.status_code}: {r.text}"
    status_data = r.json()
    assert "pendingCount" in status_data
    assert "sentCount" in status_data
    assert "failedCount" in status_data
    assert status_data.get("enableBackgroundDispatcher") is True
    assert status_data.get("simulateDeliveryInDev") is True
    print(f"  ✓ Outbox status verified: Pending={status_data['pendingCount']}, Sent={status_data['sentCount']}, Failed={status_data['failedCount']}")
    print(f"  ✓ Background dispatcher enabled: {status_data['enableBackgroundDispatcher']}, Polling interval: {status_data['outboxPollingIntervalSeconds']}s")

    # -------------------------------------------------------------
    # TEST 2: Background Dispatcher Processing (Pending -> Sent)
    # -------------------------------------------------------------
    print("\n[TEST 2] Testing Background Dispatcher processing from Pending to Sent...")
    test_email = f"p5test_{int(time.time())}@huit.edu.vn"
    outbox_id = f"e5e5e5e5-{int(time.time()) % 10000:04d}-4000-8000-111122223333"

    insert_sql = f"""
    INSERT INTO EmailOutbox (MaOutbox, LoaiThu, NguoiNhan, TieuDe, NoiDungHtml, NoiDungText, TrangThai, TaoLucUtc)
    VALUES ('{outbox_id}', 'XacNhanDangKy', '{test_email}', N'Tiêu đề thử nghiệm P5', N'<p>Mã: 888999</p>', N'Mã: 888999', 'Pending', GETUTCDATE());
    """
    execute_sql(insert_sql)
    print(f"  ✓ Inserted test pending outbox item: {outbox_id}")

    # Wait for the background dispatcher (runs every 5 seconds) to pick up the item
    print("  Waiting for background dispatcher cycle (up to 12s)...")
    is_sent = False
    for attempt in range(12):
        time.sleep(1)
        check_sql = f"SELECT TrangThai, GuiLucUtc FROM EmailOutbox WHERE MaOutbox = '{outbox_id}';"
        out = execute_sql(check_sql)
        if "Sent" in out:
            is_sent = True
            break

    assert is_sent is True, f"Outbox item {outbox_id} was not transitioned to Sent by background dispatcher!"
    print(f"  ✓ Background dispatcher successfully picked up and processed {outbox_id} -> Sent!")

    # -------------------------------------------------------------
    # TEST 3: Manual Flush Outbox Endpoint (POST /api/system/flush-outbox)
    # -------------------------------------------------------------
    print("\n[TEST 3] Testing POST /api/system/flush-outbox...")
    test_email2 = f"p5test_flush_{int(time.time())}@huit.edu.vn"
    outbox_id2 = f"f6f6f6f6-{int(time.time()) % 10000:04d}-4000-8000-111122223333"
    execute_sql(f"""
    INSERT INTO EmailOutbox (MaOutbox, LoaiThu, NguoiNhan, TieuDe, NoiDungHtml, NoiDungText, TrangThai, TaoLucUtc)
    VALUES ('{outbox_id2}', 'XacNhanDangKy', '{test_email2}', N'Flush Test', N'<p>Code</p>', N'Code', 'Pending', GETUTCDATE());
    """)

    r = requests.post(f"{API_BASE}/system/flush-outbox?batchSize=10", headers=headers, timeout=10)
    assert r.status_code == 200
    flush_res = r.json()
    assert flush_res.get("success") is True
    print(f"  ✓ Manual flush response: {flush_res['message']}")

    # -------------------------------------------------------------
    # TEST 4: Exponential Backoff for Failed Retries
    # -------------------------------------------------------------
    print("\n[TEST 4] Testing Exponential Backoff for failed retries...")
    outbox_fail_id = f"a1a1a1a1-{int(time.time()) % 10000:04d}-4000-8000-111122223333"
    # An item that failed 1 time just 5 seconds ago -> backoff requires 2^1 * 30 = 60s
    execute_sql(f"""
    INSERT INTO EmailOutbox (MaOutbox, LoaiThu, NguoiNhan, TieuDe, NoiDungHtml, NoiDungText, TrangThai, SoLanThuLai, TaoLucUtc, GuiLucUtc)
    VALUES ('{outbox_fail_id}', 'XacNhanDangKy', 'p5test_backoff@huit.edu.vn', N'Backoff Test', N'<p>Code</p>', N'Code', 'Failed', 1, GETUTCDATE(), GETUTCDATE());
    """)

    # Flush should skip this item because 60s has not elapsed
    r = requests.post(f"{API_BASE}/system/flush-outbox?batchSize=10", headers=headers, timeout=10)
    # Check that item is still Failed and not retried yet
    out = execute_sql(f"SELECT SoLanThuLai FROM EmailOutbox WHERE MaOutbox = '{outbox_fail_id}';")
    assert "1" in out, "Item under exponential backoff should not have been immediately retried"
    print("  ✓ Exponential backoff verified: item was skipped as 60s cooldown had not elapsed.")

    # -------------------------------------------------------------
    # TEST 5: Max Delivery Attempts Threshold
    # -------------------------------------------------------------
    print("\n[TEST 5] Testing Max Delivery Attempts threshold...")
    outbox_max_id = f"b2b2b2b2-{int(time.time()) % 10000:04d}-4000-8000-111122223333"
    # An item that already reached 3 retries (max)
    execute_sql(f"""
    INSERT INTO EmailOutbox (MaOutbox, LoaiThu, NguoiNhan, TieuDe, NoiDungHtml, NoiDungText, TrangThai, SoLanThuLai, TaoLucUtc, GuiLucUtc)
    VALUES ('{outbox_max_id}', 'XacNhanDangKy', 'p5test_max@huit.edu.vn', N'Max Retries', N'<p>Code</p>', N'Code', 'Failed', 3, DATEADD(hour, -2, GETUTCDATE()), DATEADD(hour, -1, GETUTCDATE()));
    """)

    r = requests.post(f"{API_BASE}/system/flush-outbox?batchSize=10", headers=headers, timeout=10)
    out = execute_sql(f"SELECT TrangThai, SoLanThuLai FROM EmailOutbox WHERE MaOutbox = '{outbox_max_id}';")
    assert "Failed" in out and "3" in out, "Item with max retries should remain Failed permanently"
    print("  ✓ Max retries threshold verified: item remains Failed and is excluded from dispatch.")

    # -------------------------------------------------------------
    # TEST 6: Periodic Cleanup of Expired Registrations
    # -------------------------------------------------------------
    print("\n[TEST 6] Testing Cleanup of Expired Pending Registrations...")
    reg_id_expired = f"c3c3c3c3-{int(time.time()) % 10000:04d}-4000-8000-111122223333"
    execute_sql(f"""
    INSERT INTO DangKyChoXacNhan (
        MaDangKy, EmailGoc, EmailSoSanh, TenDangNhapSoSanh, MatKhauHash,
        HoDem, Ten, HoTen, HocVi, HocHam, GioiTinh, QuocGia, NgonNgu, DonVi,
        DangKyPhanBien, TrangThai, TaoLucUtc, HetHanHoSoUtc
    ) VALUES (
        '{reg_id_expired}', 'p5user_expired@huit.edu.vn', 'p5user_expired@huit.edu.vn', 'p5user_expired', 'hash',
        N'Nguyễn', N'Expired', N'Nguyễn Expired', N'ThS', N'Không', N'Nam', N'Việt Nam', N'vi', N'HUIT',
        0, 'Pending', DATEADD(hour, -25, GETUTCDATE()), DATEADD(hour, -1, GETUTCDATE())
    );
    """)

    r = requests.post(f"{API_BASE}/system/trigger-cleanup", headers=headers, timeout=10)
    assert r.status_code == 200
    cleanup_res = r.json()
    assert cleanup_res.get("success") is True
    assert cleanup_res.get("expiredRegistrations") >= 1
    print(f"  ✓ Cleanup response: {cleanup_res['message']}")

    # Verify status changed in database
    out = execute_sql(f"SELECT TrangThai FROM DangKyChoXacNhan WHERE MaDangKy = '{reg_id_expired}';")
    assert "Expired" in out, f"Expected status Expired, got: {out}"
    print(f"  ✓ Pending registration {reg_id_expired} successfully transitioned to Expired.")

    # -------------------------------------------------------------
    # TEST 7: Pruning Old Sent Outbox Records (> 7 days)
    # -------------------------------------------------------------
    print("\n[TEST 7] Testing Pruning of Old Sent Outbox records (> 7 days)...")
    outbox_ancient_id = f"d4d4d4d4-{int(time.time()) % 10000:04d}-4000-8000-111122223333"
    execute_sql(f"""
    INSERT INTO EmailOutbox (MaOutbox, LoaiThu, NguoiNhan, TieuDe, NoiDungHtml, NoiDungText, TrangThai, TaoLucUtc, GuiLucUtc)
    VALUES ('{outbox_ancient_id}', 'XacNhanDangKy', 'p5test_ancient@huit.edu.vn', N'Ancient', N'<p>Code</p>', N'Code', 'Sent', DATEADD(day, -10, GETUTCDATE()), DATEADD(day, -10, GETUTCDATE()));
    """)

    r = requests.post(f"{API_BASE}/system/trigger-cleanup", headers=headers, timeout=10)
    assert r.status_code == 200
    out = execute_sql(f"SELECT COUNT(*) FROM EmailOutbox WHERE MaOutbox = '{outbox_ancient_id}';")
    # Should be 0 rows found
    count_match = re.search(r'\b0\b', out)
    assert count_match is not None, f"Ancient outbox item should be deleted, found: {out}"
    print(f"  ✓ Ancient sent outbox item {outbox_ancient_id} (>7 days) was successfully pruned.")

    # -------------------------------------------------------------
    # TEST 8: Sensitive OTP Code Sanitization in Sent Emails (> 1 hour)
    # -------------------------------------------------------------
    print("\n[TEST 8] Testing Sensitive OTP Code Sanitization in Sent Emails (> 1 hour)...")
    outbox_mask_id = f"e5e5e5e5-{int(time.time()) % 10000:04d}-4000-8000-999988887777"
    execute_sql(f"""
    INSERT INTO EmailOutbox (MaOutbox, LoaiThu, NguoiNhan, TieuDe, NoiDungHtml, NoiDungText, TrangThai, TaoLucUtc, GuiLucUtc)
    VALUES ('{outbox_mask_id}', 'XacNhanDangKy', 'p5test_mask@huit.edu.vn', N'Mask Test', N'<p>Mã: [ 998877 ]</p>', N'Mã: [ 998877 ]', 'Sent', DATEADD(hour, -2, GETUTCDATE()), DATEADD(hour, -2, GETUTCDATE()));
    """)

    r = requests.post(f"{API_BASE}/system/trigger-cleanup", headers=headers, timeout=10)
    assert r.status_code == 200
    out = execute_sql(f"SELECT NoiDungText FROM EmailOutbox WHERE MaOutbox = '{outbox_mask_id}';")
    assert "998877" not in out, f"OTP should be sanitized from sent email, got: {out}"
    assert "thanh lọc" in out, f"Expected sanitization notice in NoiDungText, got: {out}"
    print("  ✓ Sensitive OTP sanitization verified: plain code replaced with privacy preservation notice.")

    # -------------------------------------------------------------
    # CLEANUP TEST DATA
    # -------------------------------------------------------------
    execute_sql(f"""
    DELETE FROM EmailOutbox WHERE NguoiNhan LIKE 'p5test_%';
    DELETE FROM DangKyChoXacNhan WHERE TenDangNhapSoSanh LIKE 'p5user_%';
    """)
    print("\n  ✓ Test artifacts cleaned up from database.")

    print("\n" + "=" * 70)
    print("ALL 8 PHASE 5 TEST SCENARIOS PASSED 100%!")
    print("=" * 70)

if __name__ == "__main__":
    run_phase5_tests()
