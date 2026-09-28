"""Guard integration tests from writing to the production journal database."""

import json
import os
import urllib.request
from urllib.parse import urlparse

API_ROOT = os.environ.get("JOURNAL_TEST_BASE_URL", "http://127.0.0.1:5001/api").rstrip("/")


def require_testing_api():
    parsed = urlparse(API_ROOT)
    if parsed.scheme != "http" or parsed.hostname not in ("localhost", "127.0.0.1") or parsed.path != "/api":
        raise RuntimeError("Integration tests require a local Testing API URL ending in /api.")
    with urllib.request.urlopen(f"{API_ROOT}/testing/environment", timeout=10) as response:
        target = json.load(response)
    if (target.get("environment") != "Testing" or
            target.get("database") != "QL_TapChiKhoaHoc_Test" or
            target.get("dataSource") != "."):
        raise RuntimeError("Integration tests require the isolated local Testing API and database.")
