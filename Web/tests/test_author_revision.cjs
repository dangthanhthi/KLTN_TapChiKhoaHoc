const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { chromium } = require('../node_modules/playwright');
const root = path.resolve(__dirname, '..');
const server = http.createServer((req, res) => {
  const target = path.resolve(root, '.' + new URL(req.url, 'http://localhost').pathname);
  if (!target.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
  fs.readFile(target, (error, data) => {
    if (error) { res.writeHead(404).end(); return; }
    res.setHeader('Content-Type', {'.html':'text/html', '.js':'application/javascript', '.css':'text/css'}[path.extname(target)] || 'application/octet-stream');
    res.end(data);
  });
});
const user = {maNguoiDung: 100, hoTen: 'Tác giả kiểm thử', email: 'author@example.test', vaiTros: ['Tác giả'], isLoggedIn: true};
let browser;
let passed = 0;
function check(ok, name) { assert.ok(ok, name); passed++; console.log('PASS ' + name); }
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  browser = await chromium.launch({channel: 'msedge', headless: true});
  const context = await browser.newContext({viewport:{width:1366,height:1000}});
  await context.addInitScript(u => {
    localStorage.setItem('journal_token', 'author-test-token');
    localStorage.setItem('journal_user', JSON.stringify(u));
  }, user);
  const item = {maBaiBao:7, maDinhDanh:'QA-7', tieuDe:'Bài thử <img src=x onerror=window.injected=true>', chuyenNganh:'Công nghệ thông tin',
    trangThai:'Chờ sửa hình thức', coTheNopLai:true, nhanXetPhanBien:'Sửa bảng biểu.\n<img src=x onerror=window.injected=true>',
    soDongTacGia:0, ngayGui:'2026-10-01T10:00:00', ngayCapNhat:'2026-10-01T10:00:00'};
  const posts = [];
  let reject = false;
  let downloaded = false;
  await context.route('**/*', async route => {
    const u = new URL(route.request().url());
    const json = body => route.fulfill({contentType:'application/json', body:JSON.stringify(body)});
    if (u.pathname.endsWith('/api/auth/profile')) return json(user);
    if (u.pathname.endsWith('/api/baibao/my-submissions')) return json([item]);
    if (u.pathname.endsWith('/api/chuyennganh')) return json([]);
    if (u.pathname.endsWith('/api/phanbien/my-assignments')) return json([]);
    if (u.pathname.endsWith('/api/baibao/7/resubmit')) {
      posts.push(route.request().postDataBuffer().toString());
      if (reject) return route.fulfill({status:400, contentType:'application/json', body:JSON.stringify({message:'Yêu cầu chỉnh sửa đã thay đổi.'})});
      item.trangThai = item.trangThai === 'Chờ sửa hình thức' ? 'Chờ sơ duyệt' : 'Chờ quyết định';
      item.coTheNopLai = false;
      return json({success:true,message:'Đã nhận bản sửa.'});
    }
    if (u.pathname.endsWith('/api/baibao/7/manuscript')) {
      downloaded = u.searchParams.get('fileId') === '70' && route.request().headers().authorization === 'Bearer author-test-token';
      return route.fulfill({contentType:'application/pdf', body:'%PDF-1.4\n%%EOF'});
    }
    if (u.pathname.endsWith('/api/baibao/7')) return json({...item, tenChuyenNganh:item.chuyenNganh,
      tacGiaChinh:user.hoTen, emailTacGiaChinh:user.email, dongTacGias:[], phanBienDeXuats:[],
      tapTins:[{maThuMuc:70,tenThuMuc:'BanSua.pdf',loaiThuMuc:'Bản chỉnh sửa',kichThuoc:1048576,soVong:2,ngayTaiLen:'2026-10-01T10:00:00'}],
      lichSuTrangThais:[{trangThaiMoi:'Chờ chỉnh sửa',ghiChu:item.nhanXetPhanBien,ngayChuyen:'2026-10-01T10:00:00'}]});
    if (u.hostname === '127.0.0.1') return route.continue();
    return route.abort();
  });
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto(base + '/profile.html');
  await page.locator('.btn-action-revise').waitFor();
  check((await page.locator('#my-submissions-container').innerText()).includes('Sửa bảng biểu.'), 'Dashboard displays real editorial feedback');
  check(await page.evaluate(() => !window.injected), 'Author title and feedback render as text, never executable HTML');
  await page.locator('.btn-action-revise').click();
  check(await page.locator('#rev-modal-heading').innerText() === 'Nộp bản sửa hình thức', 'Format correction uses its own form heading');
  check(await page.locator('#rev-file-bm03').isHidden() && !await page.locator('#rev-file-bm03').evaluate(e=>e.required), 'Format correction does not require BM-03');
  await page.locator('#rev-explanation').fill('Đã sửa thể thức.');
  const pdf = {name:'clean.pdf',mimeType:'application/pdf',buffer:Buffer.from('%PDF-1.4\n%%EOF')};
  await page.locator('#rev-file-clean').setInputFiles(pdf);
  await page.locator('#btn-submit-revision').click();
  await page.waitForFunction(()=>!document.querySelector('#modal-revision').classList.contains('open'));
  check(posts.length === 1 && posts[0].includes('name="FileClean"') && !posts[0].includes('name="FileBm03"'), 'Format revision submits clean manuscript without BM-03');
  item.trangThai = 'Chờ chỉnh sửa'; item.coTheNopLai = true;
  await page.evaluate(()=>loadMySubmissions());
  await page.locator('.btn-action-revise').click();
  check(await page.locator('#rev-file-bm03').isVisible() && await page.locator('#rev-file-bm03').evaluate(e=>e.required), 'Peer-review revision requires BM-03');
  check(await page.locator('#rev-file-clean').evaluate(e=>e.files.length) === 0, 'Opening another revision clears old attachments');
  await page.locator('#rev-explanation').fill('Đã tiếp thu phản biện.');
  await page.locator('#rev-file-clean').setInputFiles(pdf);
  await page.locator('#btn-submit-revision').click();
  check(posts.length === 1, 'Missing BM-03 blocks peer-review resubmission');
  await page.locator('#rev-file-bm03').setInputFiles({...pdf,name:'BM03.pdf'});
  reject = true;
  await page.locator('#btn-submit-revision').click();
  await page.waitForFunction(()=>!document.querySelector('#btn-submit-revision').disabled);
  check(await page.locator('#rev-explanation').inputValue() === 'Đã tiếp thu phản biện.' && await page.locator('#rev-file-clean').evaluate(e=>e.files.length) === 1,
    'Rejected resubmission preserves explanation and attachments for recovery');
  reject = false;
  await page.locator('#btn-submit-revision').click();
  await page.waitForFunction(()=>!document.querySelector('#modal-revision').classList.contains('open'));
  await page.evaluate(()=>loadMySubmissions());
  check(await page.locator('.btn-action-revise').count() === 0 && (await page.locator('#my-submissions-container').innerText()).includes('Tòa soạn đã nhận bản sửa'),
    'Successful revision hides upload action and explains pending editorial decision');
  await page.evaluate(()=>openSubmissionDetailModal(7));
  await page.locator('#dt-files-container button').waitFor();
  check((await page.locator('#dt-files-container').innerText()).includes('BanSua.pdf') && (await page.locator('#dt-files-container').innerText()).includes('1.00 MB') &&
    !(await page.locator('#dt-files-container').innerText()).includes('Invalid Date'), 'File history uses API names, sizes, dates and revision rounds');
  const download = page.waitForEvent('download');
  await page.locator('#dt-files-container button').click();
  await download;
  check(downloaded, 'File download calls authorized API with the selected file id');
  for (const width of [320,375,768,1024,1366]) {
    await page.evaluate(()=>closeSubmissionDetailModal());
    await page.setViewportSize({width,height:900});
    item.trangThai='Chờ chỉnh sửa'; item.coTheNopLai=true;
    await page.evaluate(()=>loadMySubmissions());
    await page.locator('.btn-action-revise').click();
    const bounds=await page.locator('#form-revision-submit').boundingBox();
    check(bounds.x >= 0 && bounds.x + bounds.width <= width + 1, `Revision form fits ${width}px viewport`);
    await page.evaluate(()=>closeRevisionDialog());
  }
  item.laDongTacGia=true; item.coTheNopLai=false; item.trangThai='Chờ chỉnh sửa';
  await page.evaluate(()=>loadMySubmissions());
  check(await page.locator('.btn-action-revise').count() === 0 &&
    (await page.locator('#my-submissions-container').innerText()).includes('Đồng tác giả · Chỉ xem'),
    'Coauthor dashboard labels read-only membership and hides resubmission');
  check((await page.locator('#my-submissions-container').innerText()).includes('Bạn vẫn có thể nộp bài mới của mình'),
    'Coauthor permission applies to this article, not the entire account');
  item.trangThai='Đã xuất bản'; item.ngayPhatHanh='2026-10-01T00:00:00';
  await page.evaluate(()=>loadMySubmissions());
  check((await page.locator('#my-submissions-container').innerText()).includes('Ngày phát hành:') &&
    (await page.locator('#my-submissions-container').innerText()).includes('1/10/2026'),
    'Coauthor dashboard displays publication date');
  check(errors.length===0, 'Author dashboard and revision form have no JavaScript errors');
  console.log(JSON.stringify({passed,total:passed}));
})().catch(error=>{console.error(error);process.exitCode=1;}).finally(async()=>{
  if(browser) await browser.close();
  server.close();
});
