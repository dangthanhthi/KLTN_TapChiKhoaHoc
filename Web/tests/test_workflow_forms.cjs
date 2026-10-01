const assert = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path'), http = require('node:http');
const { chromium } = require('../node_modules/playwright');
const root = path.resolve(__dirname, '..');
const server = http.createServer((req,res) => {
  const target = path.resolve(root, '.' + new URL(req.url,'http://localhost').pathname);
  if (!target.startsWith(root + path.sep)) return res.writeHead(403).end();
  fs.readFile(target,(err,data) => { if (err) return res.writeHead(404).end(); res.setHeader('Content-Type', {'.html':'text/html','.js':'application/javascript','.css':'text/css'}[path.extname(target)] || 'application/octet-stream'); res.end(data); });
});
let browser, passed = 0;
const check = (condition,name) => { assert.ok(condition,name); passed++; console.log('PASS ' + name); };
(async () => {
  await new Promise(resolve => server.listen(0,'127.0.0.1',resolve));
  const base = 'http://127.0.0.1:' + server.address().port;
  browser = await chromium.launch({channel:'msedge',headless:true});
  const context = await browser.newContext({viewport:{width:1366,height:1000}});
  let user = {maNguoiDung:100,hoTen:'Tác giả thử',email:'author@example.test',vaiTros:['Tác giả']};
  let readonly = true, contactFail = true, draft = null, saved = [], posts = [], proofState = 'Pending';
  await context.addInitScript(() => { localStorage.setItem('journal_token','test-token'); });
  await context.route('**/*', async route => {
    const req=route.request(), url=new URL(req.url()), json = (data,status=200) => route.fulfill({status,contentType:'application/json',body:JSON.stringify(data)});
    if (url.pathname.includes('/api/')) {
      const endpoint = url.pathname.split('/api/')[1];
      if (endpoint === 'auth/profile') return json(user);
      if (endpoint === 'chuyennganh') return json([{maChuyenNganh:1,tenChuyenNganh:'Công nghệ thông tin'}]);
      if (endpoint === 'workflows/submission-draft') {
        if (req.method()==='GET') return json(draft);
        saved.push(req.postDataBuffer().toString()); return json({id:'11111111-1111-1111-1111-111111111111',version:'version-2'});
      }
      if (endpoint==='workflows/contact') { posts.push(req.postDataJSON()); return contactFail ? json({message:'Máy chủ chưa lưu được yêu cầu.'},503) : json({id:'contact-123',success:true}); }
      if (endpoint==='workflows/password-reset/request') return json({id:'22222222-2222-2222-2222-222222222222',message:'Nếu email tồn tại, mã sẽ được gửi.'});
      if (endpoint==='workflows/password-reset/confirm') { posts.push(req.postDataJSON()); return json({success:req.postDataJSON().code==='123456'}); }
      if (endpoint==='workflows/article/7') return json({maBaiBao:7,trangThai:'Đang chế bản',readOnly:readonly,files:[],records:[{id:'proof-7',kind:'Proof',state:proofState,updatedUtc:'2026-10-01T00:00:00Z',data:{Note:'<img src=x onerror=window.injected=true>'}}]});
      if (endpoint==='workflows/proof/proof-7/review') { posts.push(req.postDataJSON()); proofState=req.postDataJSON().approve?'Approved':'ChangesRequested'; return json({success:true}); }
      if (endpoint==='workflows/inbox') return json([]);
      if (endpoint==='baibao/submit') { posts.push(req.postDataBuffer().toString()); return json({success:true,maDinhDanh:'QA-100'}); }
      return json({message:'Not found'},404);
    }
    if (url.hostname === '127.0.0.1') return route.continue();
    return route.abort();
  });
  const page=await context.newPage(), errors=[]; page.on('pageerror',e=>errors.push(e.message));
  await page.goto(base+'/contact.html');
  await page.locator('#contact-name').fill('Người kiểm thử'); await page.locator('#contact-email').fill('tester@example.test'); await page.locator('#contact-message').fill('Hỏi về hồ sơ kiểm thử.');
  await page.locator('.btn-submit-contact').click(); await page.waitForFunction(()=>!document.querySelector('.btn-submit-contact').disabled);
  check(await page.locator('#contact-message').inputValue()==='Hỏi về hồ sơ kiểm thử.','Rejected contact preserves form instead of claiming success');
  contactFail=false; await page.locator('.btn-submit-contact').click(); await page.waitForFunction(()=>document.querySelector('#contact-message').value==='');
  check(posts.length===2 && posts[1].message==='Hỏi về hồ sơ kiểm thử.','Contact success follows actual API receipt');
  await page.goto(base+'/forgot-password.html'); await page.locator('#email').fill('tester@example.test'); await page.locator('#reset-request button').click(); await page.locator('#code').waitFor();
  await page.locator('#code').fill('123456'); await page.locator('#password').fill('NewTest#123'); await page.locator('#repeat').fill('Mismatch#123'); await page.locator('#reset-confirm button[type="submit"], #reset-confirm button:not([type])').first().click();
  check((await page.locator('#status').innerText()).includes('chưa trùng'),'Reset rejects mismatched passwords locally');
  await page.locator('#repeat').fill('NewTest#123'); await page.locator('#reset-confirm button:not([type])').click(); await page.waitForFunction(()=>document.querySelector('#reset-confirm').hidden);
  check(await page.evaluate(()=>!localStorage.getItem('journal_token')),'Reset clears old login session on success');
  await page.evaluate(()=>localStorage.setItem('journal_token','test-token'));
  await page.goto(base+'/article-workflow.html?id=7'); await page.waitForFunction(()=>document.querySelector('#summary').textContent.includes('Đồng tác giả'));
  check(await page.locator('#proof-form').isHidden() && await page.locator('#withdrawal').isHidden(),'Coauthor has read-only proof and withdrawal access');
  check(await page.evaluate(()=>!window.injected),'Workflow notes render as text and cannot inject HTML');
  readonly=false; await page.reload(); await page.locator('#proof-form').waitFor(); await page.locator('#proof-note').fill('Cần sửa lỗi căn lề.'); await page.locator('button[value="correct"]').click(); await page.waitForFunction(()=>document.querySelector('#proof').hidden);
  check(posts.at(-1).approve===false && posts.at(-1).note==='Cần sửa lỗi căn lề.','Author proof correction calls the owned-proof API');
  user.vaiTros=['Tổng biên tập']; await page.goto(base+'/editorial-workflow.html'); await page.locator('#staff-work').waitFor();
  check(await page.locator('#final-work').isVisible(),'Editor-in-chief sees final publication controls');
  user.vaiTros=['Ban biên tập']; await page.reload(); await page.locator('#staff-work').waitFor();
  check(await page.locator('#final-work').isHidden(),'Editorial staff cannot see final publication controls');
  user.vaiTros=['Tác giả'];
  await page.goto(base+'/submit-paper.html'); await page.waitForFunction(()=>window.submissionDraft?.id);
  await page.evaluate(()=>{ document.querySelector('#title-vi').value='Bản thảo kiểm thử'; saveDraftToStorage(); });
  await page.waitForFunction(()=>document.querySelector('#draft-status').textContent.includes('đã lưu'));
  check(saved.at(-1).includes('Bản thảo kiểm thử'),'Submission draft saves text to the server');
  check(await page.evaluate(()=>submissionDraft.id!==submissionDraft.draftId),'Draft and submission use different request identifiers');
  check(await page.evaluate(()=>!localStorage.getItem('jst_submission_draft') && !!localStorage.getItem('jst_submission_draft:100')),'Draft fallback is scoped to the authenticated account');
  for (const file of ['forgot-password.html','article-workflow.html?id=7','editorial-workflow.html']) {
    await page.goto(base+'/'+file);
    for (const width of [320,375,414,768,1024]) { await page.setViewportSize({width,height:1000}); check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`${file} fits ${width}px`); }
  }
  check(errors.length===0,'Workflow pages produce no JavaScript errors: '+errors.join(';'));
  console.log(JSON.stringify({passed}));
})().catch(e=>{ console.error(e); process.exitCode=1; }).finally(async()=>{if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));});
