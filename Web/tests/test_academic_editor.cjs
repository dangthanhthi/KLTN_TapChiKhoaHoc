const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { chromium } = require('../node_modules/playwright');

const root = path.resolve(__dirname, '..');
const server = http.createServer((req, res) => {
  const parsed = new URL(req.url, 'http://localhost');
  const target = path.resolve(root, '.' + parsed.pathname);
  if (!target.startsWith(root + path.sep)) {
    res.writeHead(403).end();
    return;
  }
  fs.readFile(target, (err, data) => {
    if (err) {
      res.writeHead(404).end();
      return;
    }
    const ext = path.extname(target);
    const mime = {
      '.html': 'text/html; charset=utf-8',
      '.js': 'application/javascript; charset=utf-8',
      '.css': 'text/css; charset=utf-8',
      '.png': 'image/png',
      '.jpg': 'image/jpeg',
      '.svg': 'image/svg+xml'
    }[ext] || 'application/octet-stream';
    res.setHeader('Content-Type', mime);
    res.end(data);
  });
});

const user = {
  maNguoiDung: 101,
  hoTen: 'TS. Nguyễn Văn Nghiên Cứu',
  email: 'researcher@huit.edu.vn',
  donVi: 'Khoa Công nghệ Thông tin, Đại học Công Thương TP.HCM',
  orcid: '0000-0002-1825-0097',
  vaiTros: ['Tác giả'],
  isLoggedIn: true
};

let passed = 0;
function check(ok, name) {
  assert.ok(ok, name);
  passed++;
  console.log('PASS: ' + name);
}

(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const context = await browser.newContext({ viewport: { width: 1366, height: 900 } });

  await context.addInitScript(u => {
    localStorage.setItem('journal_token', 'author-token-999');
    localStorage.setItem('journal_user', JSON.stringify(u));
  }, user);

  await context.route('**/*', async route => {
    const u = new URL(route.request().url());
    const json = data => route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) });

    if (u.pathname.endsWith('/api/chuyennganh')) {
      return json([
        { maChuyenNganh: 1, tenChuyenNganh: 'Công nghệ Thông tin & Trí tuệ Nhân tạo' },
        { maChuyenNganh: 2, tenChuyenNganh: 'Công nghệ Thực phẩm & Hóa sinh' }
      ]);
    }
    if (u.pathname.endsWith('/api/auth/profile')) {
      return json(user);
    }
    if (u.hostname === '127.0.0.1') return route.continue();
    return route.abort();
  });

  const page = await context.newPage();
  const pageErrors = [];
  page.on('pageerror', err => pageErrors.push(err.message));

  await page.goto(base + '/submit-paper.html');
  await page.waitForLoadState('networkidle');

  // Kiểm tra không có lỗi JS
  check(pageErrors.length === 0, 'No console errors when loading submit-paper.html');

  // Chuyển sang Bước 2
  await page.evaluate(() => {
    if (typeof switchStep === 'function') switchStep(2);
  });
  await page.waitForTimeout(200);

  // 1. Kiểm tra 3 bộ Rich Text Editor đã được sinh ra
  const editorContainers = await page.locator('.academic-editor-container').count();
  check(editorContainers >= 3, 'Academic Rich Text Editor created for abstract-vi, abstract-en, and references');

  // 2. Kiểm tra thanh công cụ đầy đủ các công cụ học thuật (Kiểu chữ, cỡ chữ, phông chữ, symbol, căn lề...)
  const toolbars = page.locator('.academic-editor-toolbar');
  const firstToolbar = toolbars.first();
  check(await firstToolbar.locator('.rte-font').count() === 1, 'Toolbar has academic font dropdown (Times New Roman, Inter, Arial, etc.)');
  check(await firstToolbar.locator('.rte-size').count() === 1, 'Toolbar has font size dropdown (12px, 13px, 14px, 16px, 18px)');
  check(await firstToolbar.locator('.rte-style').count() === 1, 'Toolbar has paragraph/heading style dropdown');
  check(await firstToolbar.locator('[data-cmd="bold"]').count() === 1, 'Toolbar has bold button');
  check(await firstToolbar.locator('[data-cmd="italic"]').count() === 1, 'Toolbar has italic button for scientific nomenclature');
  check(await firstToolbar.locator('[data-cmd="sub"]').count() === 1, 'Toolbar has subscript button (x2)');
  check(await firstToolbar.locator('[data-cmd="sup"]').count() === 1, 'Toolbar has superscript button (x2)');
  check(await firstToolbar.locator('[data-cmd="alignJustify"]').count() === 1, 'Toolbar has justify alignment button');
  check(await firstToolbar.locator('[data-cmd="symbol"]').count() === 1, 'Toolbar has math and Greek symbols button (Omega)');
  check(await firstToolbar.locator('[data-cmd="source"]').count() === 1, 'Toolbar has source code HTML toggle');

  // 3. Thử mở menu ký hiệu khoa học Omega
  await firstToolbar.locator('[data-cmd="symbol"]').click();
  const popover = firstToolbar.locator('.rte-popover');
  check(await popover.evaluate(el => el.classList.contains('is-open')), 'Omega symbol popover opens upon click');
  const symbols = await popover.locator('.rte-symbol-btn').count();
  check(symbols >= 30, 'Omega popover provides rich scientific and Greek math symbols');
  // Đóng popover
  await page.locator('body').click({ position: { x: 10, y: 10 } });
  check(!await popover.evaluate(el => el.classList.contains('is-open')), 'Omega popover closes upon clicking outside');

  // 4. Kiểm tra đồng bộ dữ liệu: Gõ nội dung HTML vào contenteditable và kiểm tra textarea
  const firstBody = page.locator('.academic-editor-body').first();
  await firstBody.click();
  await page.keyboard.type('Nghiên cứu ứng dụng trí tuệ nhân tạo ');
  
  const viValue = await page.locator('#abstract-vi').inputValue();
  check(viValue.includes('Nghiên cứu ứng dụng trí tuệ nhân tạo'), 'Typing in visual editor syncs to underlying textarea');

  // 5. Kiểm tra trợ lý IMRaD chèn định dạng HTML chuẩn
  await page.evaluate(() => {
    insertIMRaDTemplate();
  });
  const viFormattedVal = await page.locator('#abstract-vi').inputValue();
  check(viFormattedVal.includes('<strong>Mục tiêu:</strong>'), 'IMRaD assistant inserts bold tags and structured sections');

  // 6. Kiểm tra bộ đếm từ (Word counter) loại bỏ thẻ HTML và đếm từ thực tế
  const wordCountBadgeText = await page.locator('#vi-word-counter').innerText();
  check(!wordCountBadgeText.includes('<strong>'), 'Word counter badge does not display raw HTML tags');
  // Số từ của template IMRaD chuẩn khoảng 68 từ (không tính các tag html <p>, <strong>)
  const wordCountNum = parseInt(wordCountBadgeText.match(/\d+/)[0], 10);
  check(wordCountNum > 20 && wordCountNum < 100, `Accurate word count excluding HTML tags (counted: ${wordCountNum} words)`);

  // 7. Kiểm tra trường Tài liệu tham khảo (References)
  const refTextarea = page.locator('#references-text');
  check(await refTextarea.count() === 1, 'References textarea exists in Step 2');
  const refBody = page.locator('.academic-editor-container').nth(2).locator('.academic-editor-body');
  await refBody.click();
  await page.keyboard.type('[1] Le, H. et al. (2025). Deep Learning for Medical Diagnostics. IEEE Trans.');
  const refVal = await refTextarea.inputValue();
  check(refVal.includes('IEEE Trans'), 'References field syncs with academic rich editor');

  // 8. Chuyển sang Bước 5 để kiểm tra render tổng duyệt
  await page.evaluate(() => {
    document.getElementById('title-vi').value = 'Nghiên cứu thực nghiệm hệ thống AI';
    document.getElementById('title-en').value = 'Experimental Study on AI Systems';
    document.getElementById('keywords-vi').value = 'Trí tuệ nhân tạo, Thị giác máy tính';
    switchStep(5);
  });
  await page.waitForTimeout(200);

  const rvAbstractHtml = await page.locator('#rv-abstract-vi').innerHTML();
  check(rvAbstractHtml.includes('<strong>Mục tiêu:</strong>'), 'Step 5 review renders styled HTML with strong tags preserved');

  const rvReferencesHtml = await page.locator('#rv-references').innerHTML();
  check(rvReferencesHtml.includes('IEEE Trans'), 'Step 5 review displays formatted references list');

  // 9. Kiểm tra responsive ở các kích thước
  const viewports = [320, 375, 768, 1024, 1366];
  for (const w of viewports) {
    await page.setViewportSize({ width: w, height: 800 });
    const hasHScroll = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 5);
    check(!hasHScroll, `Step 2 with academic editor fits ${w}px viewport without horizontal blowout`);
  }

  await browser.close();
  server.close();
  console.log(`\nALL ACADEMIC EDITOR TESTS PASSED: ${passed}/${passed}`);
})().catch(err => {
  console.error('TEST ERROR:', err);
  process.exit(1);
});
