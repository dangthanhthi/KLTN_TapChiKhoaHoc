"""Responsive smoke test for static pages; business flows live in API-backed E2E tests."""

import asyncio
import http.server
import socketserver
import sys
import threading
from pathlib import Path

from playwright.async_api import async_playwright

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")


PORT = 8089
WEB_DIR = Path(__file__).resolve().parents[1]
PAGES = [
    "UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html",
    "about.html",
    "publishing-policy.html",
    "editorial-board.html",
    "contact.html",
    "guidelines.html",
    "archives.html",
    "article-detail.html",
    "submit-paper.html",
    "login.html",
    "register.html",
    "profile.html",
]
VIEWPORTS = [
    ("Mobile 320px", 320, 568),
    ("Mobile 375px", 375, 667),
    ("Tablet 768px", 768, 1024),
    ("Laptop 1024px", 1024, 768),
    ("Desktop 1366px", 1366, 768),
]


class QuietHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(WEB_DIR), **kwargs)

    def log_message(self, _format, *_args):
        pass


class ReusableTCPServer(socketserver.TCPServer):
    allow_reuse_address = True


async def main():
    server = ReusableTCPServer(("127.0.0.1", PORT), QuietHandler)
    server_thread = threading.Thread(target=server.serve_forever, daemon=True)
    server_thread.start()
    results = []

    try:
        async with async_playwright() as playwright:
            browser = None
            for channel in ("msedge", "chrome", None):
                try:
                    browser = await playwright.chromium.launch(headless=True, channel=channel)
                    break
                except Exception:
                    continue
            if browser is None:
                raise RuntimeError("Install a Playwright browser or provide Microsoft Edge/Chrome to run this test.")
            try:
                for page_name in PAGES:
                    for viewport_name, width, height in VIEWPORTS:
                        context = await browser.new_context(
                            viewport={"width": width, "height": height},
                            device_scale_factor=1,
                        )
                        page = await context.new_page()
                        page_errors = []
                        page.on("pageerror", lambda error: page_errors.append(str(error)))
                        await page.route("**/api/**", lambda route: route.fulfill(
                            status=503,
                            content_type="application/json",
                            body='{"message":"Backend intentionally unavailable in static layout test."}',
                        ))

                        try:
                            response = await page.goto(
                                f"http://127.0.0.1:{PORT}/{page_name}",
                                wait_until="domcontentloaded",
                                timeout=8000,
                            )
                            await page.wait_for_timeout(250)
                            dimensions = await page.evaluate("""() => ({
                                viewport: document.documentElement.clientWidth,
                                document: document.documentElement.scrollWidth,
                                body: document.body.scrollWidth
                            })""")
                            overflow = max(dimensions["document"], dimensions["body"]) > width + 1
                            errors = list(page_errors)
                            if response is None or response.status != 200:
                                errors.append(f"HTTP {response.status if response else 'no response'}")
                            if overflow:
                                errors.append(f"horizontal overflow: {dimensions}")

                            result = {
                                "page": page_name,
                                "viewport": viewport_name,
                                "status": "PASS" if not errors else "FAIL",
                                "errors": errors,
                            }
                            results.append(result)
                            symbol = "✓" if result["status"] == "PASS" else "✗"
                            print(f"[{symbol}] {page_name:<42} {viewport_name}")
                            for error in errors:
                                print(f"    {error}")
                        finally:
                            await context.close()
            finally:
                await browser.close()
    finally:
        server.shutdown()
        server.server_close()

    passed = sum(result["status"] == "PASS" for result in results)
    print(f"Responsive static-page smoke: {passed}/{len(results)} passed")
    if passed != len(results):
        raise SystemExit(1)


if __name__ == "__main__":
    asyncio.run(main())
