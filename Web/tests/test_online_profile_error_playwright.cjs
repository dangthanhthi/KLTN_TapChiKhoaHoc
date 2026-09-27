const assert = require('node:assert/strict');
const http = require('node:http');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');

const webRoot = path.resolve(__dirname, '..');
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml' };

async function main() {
  const server = http.createServer(async (request, response) => {
    const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
    const file = path.resolve(webRoot, `.${pathname === '/' ? '/profile.html' : pathname}`);
    if (!file.startsWith(webRoot + path.sep)) { response.writeHead(403).end(); return; }
    try {
      const body = await fs.readFile(file);
      response.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream' }).end(body);
    } catch { response.writeHead(404).end(); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));

  let browser;
  try {
    browser = await chromium.launch({ channel: 'msedge', headless: true });
    for (const width of [375, 1024]) {
      const page = await browser.newPage({ viewport: { width, height: 800 } });
      await page.addInitScript(() => {
        localStorage.setItem('journal_token', 'real-jwt-token');
        localStorage.setItem('journal_user', JSON.stringify({ isLoggedIn: true, hoTen: 'Tác giả kiểm thử', email: 'test@example.com', vaiTros: ['Tác giả'] }));
      });
      await page.route('**/api/**', route => route.fulfill({ status: 503, contentType: 'application/json', body: JSON.stringify({ message: 'API đang bảo trì' }) }));
      await page.goto(`http://127.0.0.1:${server.address().port}/profile.html`);
      const message = page.locator('#my-submissions-container [role="alert"]');
      await message.waitFor({ timeout: 10000 });
      assert.match(await message.textContent(), /Không thể tải bản thảo từ máy chủ/);
      assert.equal(await page.locator('#my-submissions-container').textContent().then(text => text.includes('Nghiên cứu cấu trúc phân tử')), false);
      await page.close();
    }
    console.log('Online profile error UI checks passed at 375px and 1024px.');
  } finally {
    if (browser) await browser.close();
    await new Promise(resolve => server.close(resolve));
  }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
