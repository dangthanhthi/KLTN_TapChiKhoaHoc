const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const assert = require('node:assert/strict');

const root = path.resolve(__dirname, '..');
const artifactDir = path.join(__dirname, 'reviewer-artifacts');
fs.mkdirSync(artifactDir, { recursive: true });
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.svg': 'image/svg+xml', '.png': 'image/png' };
const server = http.createServer((req, res) => {
  const url = new URL(req.url, 'http://localhost');
  const file = path.resolve(root, '.' + decodeURIComponent(url.pathname));
  if (!file.startsWith(root + path.sep)) { res.writeHead(403); res.end(); return; }
  fs.readFile(file, (err, data) => {
    if (err) { res.writeHead(404); res.end(); return; }
    res.setHeader('Content-Type', types[path.extname(file)] || 'application/octet-stream');
    res.end(data);
  });
});

(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  let browser;
  try { browser = await chromium.launch({ channel: 'msedge', headless: true }); }
  catch { browser = await chromium.launch({ headless: true }); }
  const context = await browser.newContext({ viewport: { width: 1366, height: 900 } });
  const page = await context.newPage();
  const errors = [];
  let passwordCalls = 0;
  page.on('pageerror', error => errors.push(error.message));
  await page.addInitScript(() => {
    localStorage.setItem('journal_token', 'password-test-token');
    localStorage.setItem('journal_user', JSON.stringify({ maNguoiDung: 1, hoTen: 'Tài khoản kiểm thử', email: 'test@example.test', vaiTros: ['Tác giả'] }));
  });
  await page.route('https://fonts.googleapis.com/**', route => route.fulfill({ status: 200, body: '' }));
  await page.route('**/api/**', route => {
    const url = new URL(route.request().url());
    if (url.pathname === '/api/auth/profile') return route.fulfill({ status: 200, json: { maNguoiDung: 1, hoTen: 'Tài khoản kiểm thử', email: 'test@example.test', vaiTros: ['Tác giả'] } });
    if (url.pathname === '/api/auth/change-password') {
      passwordCalls++;
      const body = route.request().postDataJSON();
      assert.equal(route.request().headers().authorization, 'Bearer password-test-token');
      return route.fulfill(body.currentPassword === 'Current#123' ? { status: 200, json: { success: true } } : { status: 400, json: { success: false, message: 'Mật khẩu hiện tại không chính xác.' } });
    }
    if (url.pathname === '/api/baibao/public/19') return route.fulfill({ status: 200, json: { maBaiBao: 19, tieuDe: 'Bài báo kiểm thử DOI', chuyenNganh: 'Nông nghiệp', maDOI: '10.58810/yersin.2026.a0f8bd', nam: 2026, tacGias: [], filePdfUrl: null } });
    if (url.pathname.endsWith('/latest')) return route.fulfill({ status: 200, json: [] });
    return route.fulfill({ status: 404, json: { message: 'Không có dữ liệu trong phép thử.' } });
  });
  try {
    await page.goto(base + '/change-password.html');
    await page.locator('#password-form').waitFor({ state: 'visible' });
    await page.screenshot({ path: path.join(artifactDir, 'change-password-desktop.png'), fullPage: true });
    await page.setViewportSize({ width: 375, height: 900 });
    await page.screenshot({ path: path.join(artifactDir, 'change-password-mobile.png'), fullPage: true });
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.click('#submit-password');
    assert.equal(passwordCalls, 0);
    assert.equal(await page.locator('#current-password').getAttribute('aria-invalid'), 'true');
    assert.equal(await page.locator('#error-summary').isVisible(), true);

    await page.fill('#current-password', 'Wrong#123');
    await page.fill('#new-password', 'NewSecure#456');
    await page.fill('#confirm-password', 'NewSecure#456');
    await page.click('#submit-password');
    await page.getByText('Mật khẩu hiện tại không chính xác.', { exact: true }).first().waitFor();
    assert.equal(passwordCalls, 1);

    await page.fill('#current-password', 'Current#123');
    await page.click('#submit-password');
    await page.locator('#success-message').waitFor({ state: 'visible' });
    assert.equal(passwordCalls, 2);
    assert.equal(await page.locator('#password-form').isVisible(), false);

    for (const width of [320, 375, 768, 1366]) {
      await page.setViewportSize({ width, height: 900 });
      const extent = await page.evaluate(() => document.documentElement.scrollWidth);
      assert(extent <= width, `Đổi mật khẩu tràn ngang ở ${width}px: ${extent}px`);
    }

    assert.equal(await page.evaluate(() => getPublicDoi('10.58810/yersin.2026.a0f8bd')), '');
    assert.equal(await page.evaluate(() => getPublicDoi('10.59876/huit.jsc.2026.42.06_18')), '');
    assert.equal(await page.evaluate(() => getPublicDoi('10.1038/s41586-024-00000-0')), '10.1038/s41586-024-00000-0');
    assert.equal(await page.evaluate(() => {
      generateArticleCitations({ maBaiBao: 19, tieuDe: 'Bài báo kiểm thử', nam: 2026, maDOI: '10.58810/yersin.2026.a0f8bd' });
      return citations.apa.includes('doi.org') || citations.bibtex.includes('doi={') || citations.ris.includes('DO  - ');
    }), false);

    await page.goto(base + '/article-detail.html?id=19');
    await page.waitForFunction(() => document.querySelector('.article-pub-meta')?.textContent.includes('Chưa xác thực DOI'));
    assert.equal(await page.locator('a[href*="doi.org/"]').count(), 0);

    await page.goto(base + '/profile.html');
    await page.getByRole('link', { name: 'Đổi mật khẩu', exact: true }).click();
    await page.waitForURL(/change-password\.html$/);
    assert.equal(await page.locator('#password-form').isVisible(), true);

    await page.goto(base + '/guidelines.html');
    assert.equal(await page.locator('#tab-submission svg').count(), 0);
    assert.equal(await page.locator('.guidelines-sidebar svg').count(), 0);
    assert.equal(await page.locator('#tab-submission .wf-step-card').count(), 6);
    assert.equal(await page.locator('#tab-submission .wf-step-meta').allTextContents().then(values => values.some(value => value.includes('⏱'))), false);
    assert.deepEqual(errors, []);
    console.log('PASS password page, DOI placeholder and submission guide');
  } finally {
    await context.close();
    await browser.close();
    server.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; server.close(); });
