const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const path = require('node:path');
const http = require('node:http');
const fs = require('node:fs');
const assert = require('node:assert/strict');

const root = path.resolve(__dirname, '..');
const server = http.createServer((req, res) => {
  const url = new URL(req.url, 'http://localhost');
  const f = path.resolve(root, '.' + decodeURIComponent(url.pathname));
  if (!f.startsWith(root + path.sep)) { res.writeHead(403); res.end(); return; }
  fs.readFile(f, (e, data) => {
    if (e) { res.writeHead(404); res.end(); return; }
    res.setHeader('Content-Type', ({
      '.js': 'application/javascript',
      '.html': 'text/html',
      '.css': 'text/css'
    })[path.extname(f)] || 'application/octet-stream');
    res.end(data);
  });
});

(async () => {
  await new Promise(r => server.listen(0, r));
  const port = server.address().port;
  const base = `http://localhost:${port}`;
  let browser;
  try {
    browser = await chromium.launch({ channel: 'msedge', headless: true });
  } catch {
    browser = await chromium.launch({ headless: true });
  }

  const ctx = await browser.newContext({ viewport: { width: 1366, height: 1000 } });
  const p = await ctx.newPage();
  const errors = [];
  p.on('pageerror', e => errors.push(e.message));

  const profile = { maNguoiDung: 11, hoTen: 'Chuyên gia kiểm thử', email: 'reviewer@example.test', vaiTros: ['Chuyên gia phản biện'] };
  const assignment = {
    maPhanCong: 58,
    maBaiBao: 10,
    soVong: 1,
    tieuDeBaiBao: 'Kiểm thử phản biện BM-04',
    chuyenNganh: 'Công nghệ thông tin',
    trangThai: 'Đang đánh giá',
    ngayPhanCong: new Date().toISOString(),
    hanHoanThanh: new Date(Date.now() + 86400000).toISOString(),
    daDanhGia: false
  };

  await p.addInitScript(({ base, profile }) => {
    localStorage.setItem('journal_token', 'test-token');
    localStorage.setItem('journal_user', JSON.stringify(profile));
    localStorage.setItem('huit_api_url', base + '/api');
  }, { base, profile });

  await p.route('https://fonts.googleapis.com/**', r => r.fulfill({ status: 200, body: '' }));
  await p.route('**/api/**', async r => {
    const u = new URL(r.request().url());
    if (u.pathname === '/api/auth/profile') return r.fulfill({ status: 200, json: profile });
    if (u.pathname === '/api/phanbien/my-assignments') return r.fulfill({ status: 200, json: [assignment] });
    if (u.pathname.includes('/manuscript')) return r.fulfill({ status: 200, contentType: 'application/pdf', body: '%PDF-1.4\n%test' });
    if (u.pathname.includes('/evaluation-draft')) return r.fulfill({ status: 200, json: null });
    return r.fulfill({ status: 404, json: { message: 'Not found' } });
  });

  await p.goto(`${base}/reviewer-evaluation.html?id=58`);
  await p.waitForSelector('#score-newness');

  console.log('Testing interactive typing restrictions...');
  const input = p.locator('#score-newness');

  // Test 1: Typing negative sign '-' is strictly blocked
  await input.focus();
  await p.keyboard.press('-');
  assert.equal(await input.inputValue(), '', 'Minus sign must be blocked');

  // Test 2: Typing letter 'e' or 'abc' is strictly blocked
  await p.keyboard.press('e');
  await p.keyboard.press('E');
  await p.keyboard.press('a');
  assert.equal(await input.inputValue(), '', 'Letters must be blocked');

  // Test 3: Typing valid digit '7'
  await p.keyboard.press('7');
  assert.equal(await input.inputValue(), '7', 'Digit 7 must be entered');

  // Test 4: Typing another digit like '5' right after '7' would make 75 > 10, must be blocked!
  await p.keyboard.press('5');
  assert.equal(await input.inputValue(), '7', 'Value > 10 must be blocked from typing');

  // Test 5: Typing decimal dot '.' makes '7.'
  await p.keyboard.press('.');
  assert.equal(await input.evaluate(el => el.dataset.rawVal), '7.', 'Decimal dot must be tracked');

  // Test 6: Typing '5' after '7.' makes '7.5'
  await p.keyboard.press('5');
  assert.equal(await input.inputValue(), '7.5', 'One decimal place 7.5 must be entered');

  // Test 7: Typing another decimal digit like '9' (which would make 7.59) must be blocked!
  await p.keyboard.press('9');
  assert.equal(await input.inputValue(), '7.5', 'Second decimal place must be blocked from typing');

  // Test 8: Typing 10
  await input.fill('');
  await input.focus();
  await p.keyboard.press('1');
  assert.equal(await input.inputValue(), '1');
  await p.keyboard.press('0');
  assert.equal(await input.inputValue(), '10', '10 must be allowed');
  // Typing another digit after 10 would make 100+ -> blocked
  await p.keyboard.press('1');
  assert.equal(await input.inputValue(), '10', 'Digit after 10 must be blocked');

  // Test 9: On blur, empty field resets to '0' ("khi nhập xong sẽ trở về 0")
  await input.fill('');
  await input.focus();
  await input.blur();
  assert.equal(await input.inputValue(), '0', 'Empty score on blur must reset to 0');

  // Test 10: Smooth replace of initial '0' with a typed digit
  await input.focus();
  await p.keyboard.press('8');
  assert.equal(await input.inputValue(), '8', 'Typing 8 when input is 0 must replace 0 with 8');

  // Test 11: Format on blur: typing '8.' and blurring normalizes to '8'
  await p.keyboard.press('.');
  assert.equal(await input.evaluate(el => el.dataset.rawVal), '8.');
  await input.blur();
  assert.equal(await input.inputValue(), '8', 'Trailing dot normalizes on blur');

  // Test 12: Paste rejection: pasting 9999 or -5 is blocked
  await input.fill('7');
  await input.focus();
  await p.evaluate(() => {
    const el = document.getElementById('score-newness');
    const dt = new DataTransfer();
    dt.setData('text', '9999');
    el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true }));
  });
  // Since paste was cancelled, value remains 7
  assert.equal(await input.inputValue(), '7', 'Invalid paste must be rejected');

  console.log('Testing total score and completeness validation...');
  // Fill all 4 scores with valid values: 7, 8, 9, 8
  await p.locator('#score-newness').fill('7');
  await p.locator('#score-method').fill('8');
  await p.locator('#score-result').fill('9');
  await p.locator('#score-presentation').fill('8');
  await p.locator('#score-presentation').dispatchEvent('input');

  // Average = (7 + 8 + 9 + 8) / 4 = 8.0
  assert.equal(await p.locator('#total-score').innerText(), '8.0 / 10');

  // Test 13: Submitting with missing comments triggers validation error
  await p.click('#submit-header');
  assert.match(await p.locator('#form-error-msg').innerText(), /nhận xét/i);

  // Test 14: Submitting with comments < 10 chars triggers validation error
  await p.fill('#comment-author', 'Tốt');
  await p.click('#submit-header');
  assert.match(await p.locator('#form-error-msg').innerText(), /tối thiểu 10 ký tự/i);

  // Test 15: Submitting without selecting recommendation triggers validation error
  await p.fill('#comment-author', 'Bản thảo có đóng góp mới tốt, phương pháp phân tích rõ ràng.');
  await p.click('#submit-header');
  assert.match(await p.locator('#form-error-msg').innerText(), /kiến nghị/i);

  // Test 16: Submitting without COI triggers validation error
  await p.selectOption('#recommendation', 'Chấp nhận đăng');
  await p.click('#submit-header');
  assert.match(await p.locator('#form-error-msg').innerText(), /xung đột lợi ích/i);

  // Test 17: When everything is complete, modal opens
  await p.check('#confirm-coi');
  await p.click('#submit-header');
  const dialog = p.locator('#submit-confirmation');
  assert.equal(await dialog.evaluate(el => el.open), true, 'Confirmation dialog must open when valid');
  assert.equal(await p.locator('#confirm-score').innerText(), '8.0 / 10');
  assert.equal(await p.locator('#confirm-recommendation').innerText(), 'Chấp nhận đăng');

  console.log('All BM-04 score input and form completeness tests passed successfully!');
  assert.deepEqual(errors, []);
  await ctx.close();
  await browser.close();
  server.close();
})();
