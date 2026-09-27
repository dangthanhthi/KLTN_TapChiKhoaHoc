"""Guard integration tests from writing to the production journal database."""

import json
import os
import urllib.request

API_ROOT = os.environ.get("JOURNAL_TEST_BASE_URL", "http://127.0.0.1:5001/api").rstrip("/")


def require_testing_api():
    with urllib.request.urlopen(f"{API_ROOT}/testing/environment", timeout=10) as response:
        target = json.load(response)
    if target.get("environment") != "Testing" or target.get("database") != "QL_TapChiKhoaHoc_Test":
        raise RuntimeError("Integration tests require the isolated Testing API and database.")
