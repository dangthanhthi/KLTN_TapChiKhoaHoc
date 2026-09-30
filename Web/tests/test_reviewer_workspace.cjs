const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const C=require('../reviewer-core.js');
const root=path.resolve(__dirname,'..');
const artifactDir=(process.env.REVIEWER_ARTIFACT_DIR || path.join(__dirname,'reviewer-artifacts'));
fs.mkdirSync(artifactDir,{recursive:true});
const results=[];
const server=http.createServer((req,res)=>{
  const url=new URL(req.url,'http://localhost'); const f=path.resolve(root,'.'+decodeURIComponent(url.pathname));
  if(!f.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
  fs.readFile(f,(e,data)=>{if(e){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',({'.js':'application/javascript','.html':'text/html','.css':'text/css'})[path.extname(f)]||'application/octet-stream');res.end(data);});
});
const profile={maNguoiDung:11,hoTen:'Chuyên gia kiểm thử',email:'reviewer@example.test',vaiTros:['Tác giả','Chuyên gia phản biện']};
const now=Date.now(); const day=n=>new Date(now+n*86400000).toISOString();
const items=[
  {maPhanCong:1,maBaiBao:10,soVong:2,tieuDeBaiBao:'Đánh giá mô hình học máy trong dự báo chất lượng không khí đô thị',chuyenNganh:'Công nghệ thông tin',trangThai:'Đồng ý phản biện',ngayPhanCong:day(-10),hanHoanThanh:day(2),daDanhGia:false},
  {maPhanCong:2,maBaiBao:20,soVong:1,tieuDeBaiBao:'Nghiên cứu ứng dụng vật liệu sinh học trong xử lý nước thải công nghiệp',chuyenNganh:'Khoa học Môi trường',trangThai:'Đồng ý phản biện',ngayPhanCong:day(-20),hanHoanThanh:day(-1),daDanhGia:false},
  {maPhanCong:3,maBaiBao:10,soVong:1,tieuDeBaiBao:'Đánh giá mô hình học máy trong dự báo chất lượng không khí đô thị',chuyenNganh:'Công nghệ thông tin',trangThai:'Đã đánh giá',ngayPhanCong:day(-30),hanHoanThanh:day(-10),daDanhGia:true,diemTongKet:0,kienNghi:'Chỉnh sửa lớn và phản biện lại'}
];
let browser,base;
async function test(name,fn){await fn(); results.push({name,status:'PASS'});console.log('PASS',name);}
async function pageFor(options={}){
  const ctx=await browser.newContext({viewport:{width:1366,height:1000}});
  const p=await ctx.newPage(); const errors=[];p.on('pageerror',e=>errors.push(e.message));
  const settings={user:profile,token:'real-test-token',list:items,submitStatus:200,submitCount:0,drafts:new Map(),...options};
  await p.addInitScript(({base,settings})=>{
    if(settings.token)localStorage.setItem('journal_token',settings.token);
    if(settings.user)localStorage.setItem('journal_user',JSON.stringify(settings.user));
    localStorage.setItem('huit_api_url',base+'/api');
  },{base,settings});
  await p.route('https://fonts.googleapis.com/**',r=>r.fulfill({status:200,body:''}));
  await p.route('**/api/**',async r=>{
    const u=new URL(r.request().url());
    if(u.pathname==='/api/auth/login'){
      settings.user=profile;settings.token='test-auth-token';
      return r.fulfill({status:200,json:{success:true,token:settings.token,user:profile}});
    }
    if(u.pathname==='/api/auth/profile')return r.fulfill({status:settings.profileStatus||200,json:settings.user});
    if(u.pathname==='/api/phanbien/my-assignments')return r.fulfill({status:settings.listStatus||200,json:settings.list});
    if(/^\/api\/phanbien\/assignments\/\d+\/manuscript$/.test(u.pathname))return r.fulfill({status:200,contentType:'application/pdf',body:'%PDF-1.4\n%test fixture'});
    if(u.pathname==='/api/phanbien/my-evaluation-drafts')return r.fulfill({status:200,json:[...settings.drafts].map(([maPhanCong,d])=>({maPhanCong,ngayCapNhatUtc:d.ngayCapNhatUtc}))});
    const draftMatch=u.pathname.match(/^\/api\/phanbien\/assignments\/(\d+)\/evaluation-draft$/);
    if(draftMatch){const id=Number(draftMatch[1]);if(r.request().method()==='GET'){const draft=settings.drafts.get(id);return draft?r.fulfill({status:200,json:draft}):r.fulfill({status:204,body:''});}if(r.request().method()==='PUT'){if(settings.draftStatus)return r.fulfill({status:settings.draftStatus,json:{message:'Không lưu được nháp.'}});const body=r.request().postDataJSON();const saved={...body,ngayCapNhatUtc:new Date().toISOString()};settings.drafts.set(id,saved);return r.fulfill({status:200,json:{success:true,savedAtUtc:saved.ngayCapNhatUtc}});}if(r.request().method()==='DELETE'){settings.drafts.delete(id);return r.fulfill({status:204,body:''});}}
    if(u.pathname==='/api/phanbien/evaluate'){settings.submitCount++;settings.payload=r.request().postDataJSON();if(settings.submitStatus===200)settings.drafts.delete(settings.payload.maPhanCong);return r.fulfill({status:settings.submitStatus,json:settings.submitStatus===200?{success:true}:{success:false,message:'Không thể nhận phiếu thử nghiệm.'}});}
    if(/^\/api\/phanbien\/assignments\/\d+\/respond$/.test(u.pathname)){
      settings.responsePayload=r.request().postDataJSON();
      settings.list=settings.list.map(a=>a.maPhanCong===Number(u.pathname.split('/')[4])?{...a,trangThai:settings.responsePayload.accept?'Đồng ý phản biện':'Từ chối phản biện'}:a);
      return r.fulfill({status:200,json:{success:true,message:'Đã ghi nhận phản hồi.'}});
    }
    if(/^\/api\/phanbien\/assignments\/\d+\/evaluation$/.test(u.pathname)){
      const id=Number(u.pathname.split('/')[4]);
      return r.fulfill({status:settings.evaluationStatus||404,json:settings.evaluationStatus===200?{maPhanCong:id,diemTinhMoi:0,diemPhuongPhap:0,diemKetQua:0,diemTrinhBay:0,nhanXetChoTacGia:'Nhận xét từ API'}:{message:'Chưa có phiếu'}});
    }
    return r.fulfill({status:404,json:{message:'Not implemented'}});
  });
  await p.goto(base+'/reviewer.html');
  await p.waitForFunction(()=>!document.getElementById('workspace').hidden || !document.getElementById('access-state').textContent.includes('Đang kiểm tra'));
  if(!options.profileStatus && C.hasRole(settings.user)&&settings.token)await p.waitForFunction(()=>!document.getElementById('list-status').textContent.includes('Đang tải'));
  return {p,ctx,settings,errors};
}
(async()=>{
  await new Promise(r=>server.listen(0,'127.0.0.1',r));base=`http://127.0.0.1:${server.address().port}`;
  try{browser=await chromium.launch({channel:'msedge',headless:true});}catch{browser=await chromium.launch({headless:true});}
  await test('Core: role is explicit; editor role and degree do not grant reviewer access',()=>{assert.equal(C.hasRole({vaiTros:['Ban biên tập'],hocVi:'Tiến sĩ'}),false);assert.equal(C.hasRole(profile),true);});
  await test('Core: deadlines exclude completed jobs; empty scores invalid; zero accepted',()=>{assert.equal(C.deadline(items[1],now),'overdue');assert.equal(C.deadline(items[2],now),'closed');assert.equal(C.filter(items,{view:'history'}).length,1); const d={maPhanCong:1,diemTinhMoi:0,diemPhuongPhap:0,diemKetQua:0,diemTrinhBay:0,nhanXetChoTacGia:'Cần bổ sung',kienNghi:'Từ chối đăng'};assert.equal(C.validate(d),'');assert.equal(C.total(d),0);assert.ok(C.validate({...d,diemTinhMoi:''}));});
  await test('Core: invitation needs acceptance before evaluation',()=>{assert.equal(C.canEvaluate({...items[0],trangThai:'Chờ phản hồi'}),false);assert.equal(C.canEvaluate(items[0]),true);assert.equal(C.canEvaluate(items[2]),false);});
  await test('Core: pending or declined reviewer cannot download manuscript',()=>{assert.equal(C.canDownload({...items[0],trangThai:'Chờ phản hồi'}),false);assert.equal(C.canDownload({...items[0],trangThai:'Từ chối phản biện'}),false);assert.equal(C.canDownload(items[0]),true);});
  await test('Guest and author cannot open workspace',async()=>{
    for(const options of [{token:null,user:null},{user:{...profile,vaiTros:['Tác giả']}}]){const {p,ctx}=await pageFor(options);assert.equal(await p.locator('#workspace').isVisible(),false);await ctx.close();}
  });
  await test('401 and empty/failed lists do not fall back to sample assignments',async()=>{
    for(const options of [{profileStatus:401},{list:[]},{listStatus:500,list:{message:'Lỗi thử nghiệm'}}]){
      const {p,ctx}=await pageFor(options);assert.equal(await p.locator('.assignment').count(),0);if(options.profileStatus)assert.equal(await p.locator('#workspace').isVisible(),false);await ctx.close();
    }
  });
  await test('Search, deadline filters, history including a zero score, missing historical detail',async()=>{
    const {p,ctx,errors}=await pageFor();assert.equal(await p.locator('.assignment').count(),2);
    await p.selectOption('#status-filter','overdue');assert.equal(await p.locator('.assignment').count(),1);
    await p.selectOption('#status-filter','all');await p.fill('#search','vật liệu');assert.equal(await p.locator('.assignment').count(),1);
    await p.fill('#search','');await p.click('[data-view=history]');await p.locator('.assignment h3 button').click();
    await p.getByText('Hiện chỉ có điểm và kiến nghị.',{exact:false}).waitFor();assert.match(await p.locator('#detail').innerText(),/0\/10/);
    assert.equal(await p.getByRole('button',{name:'Viết đánh giá',exact:true}).count(),0);assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Evaluation draft, rejected POST preserves draft, successful POST creates history',async()=>{
    const {p,ctx,settings,errors}=await pageFor({submitStatus:400});
    await p.locator('.assignment h3 button').first().click();assert.equal(await p.locator('#detail .detail-actions > button.primary').count(),3);assert.equal(await p.getByText('Tùy chọn khác',{exact:true}).count(),0);await p.getByRole('button',{name:'Mở phiếu nhanh',exact:true}).click();
    for(const k of C.scoreKeys)await p.locator(`[name=${k}]`).fill('0');
    await p.locator('[name=nhanXetChoTacGia]').fill('Nhận xét kiểm thử: cần bổ sung phương pháp và dữ liệu.');await p.selectOption('[name=kienNghi]','Từ chối đăng');
    await p.click('#close-evaluation');await p.reload();await p.waitForFunction(()=>document.getElementById('list-status').textContent==='2 công việc');
    await p.locator('#assignment-list .assignment h3 button').first().click();await p.getByRole('button',{name:'Mở phiếu nhanh',exact:true}).click();
    await p.getByText('Đã tải bản nháp đã lưu trên hệ thống.',{exact:false}).waitFor();
    assert.match(await p.locator('[name=nhanXetChoTacGia]').inputValue(),/Nhận xét kiểm thử/);await p.check('#confirm-review');await p.click('#submit-review');
    await p.getByText('Không thể nhận phiếu thử nghiệm.',{exact:true}).waitFor();assert.equal(await p.locator('#evaluation-dialog').isVisible(),true);assert.equal(settings.submitCount,1);assert.equal(settings.drafts.size,1);
    settings.submitStatus=200;await p.click('#submit-review');await p.waitForFunction(()=>!document.getElementById('evaluation-dialog').open);
    assert.equal(settings.payload.diemTongKet,0);assert.equal(settings.submitCount,2);assert.equal(settings.drafts.size,0);assert.equal(await p.locator('[data-view=history]').getAttribute('aria-pressed'),'true');assert.equal(await p.locator('#count-done').innerText(),'2');assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Untrusted title is rendered as text, not HTML',async()=>{
    const {p,ctx}=await pageFor({list:[{...items[0],tieuDeBaiBao:'<img src=x onerror="window.reviewXss=true">'}]});await p.locator('.assignment h3 button').click();assert.equal(await p.locator('#detail img').count(),0);assert.equal(await p.evaluate(()=>window.reviewXss),undefined);await ctx.close();
  });
  await test('Pagination, authorized manuscript download and privacy-safe calendar/export',async()=>{
    const many=Array.from({length:11},(_,i)=>({...items[0],maPhanCong:100+i,maBaiBao:500+i,tieuDeBaiBao:'Bản thảo kiểm thử '+i}));
    const {p,ctx}=await pageFor({list:many});assert.equal(await p.locator('.assignment').count(),8);await p.click('#next-page');assert.equal(await p.locator('.assignment').count(),3);
    await p.locator('.assignment h3 button').first().click();
    let auth;
    await p.route('**/api/phanbien/assignments/*/manuscript',r=>{auth=r.request().headers().authorization;return r.fulfill({status:200,contentType:'application/pdf',body:'%PDF-1.4\n%test fixture'});});
    const downloaded=p.waitForEvent('download');await p.getByRole('button',{name:'Tải bản thảo ẩn danh',exact:true}).click();const file=await downloaded;assert.match(file.suggestedFilename(),/^Ban-thao-an-danh-\d+-vong-2\.pdf$/);assert.equal(auth,'Bearer real-test-token');
    const icsDownload=p.waitForEvent('download');await p.getByRole('button',{name:'Lưu hạn vào lịch',exact:true}).click();const ics=await icsDownload;const content=fs.readFileSync(await ics.path(),'utf8');assert.match(content,/BEGIN:VCALENDAR/);assert(!content.includes('Bản thảo kiểm thử'));
    await ctx.close();
  });
  await test('Expired session during submission denies access without marking complete',async()=>{
    const {p,ctx,settings}=await pageFor({submitStatus:401});await p.locator('.assignment h3 button').first().click();await p.getByRole('button',{name:'Mở phiếu nhanh',exact:true}).click();
    for(const k of C.scoreKeys)await p.locator(`[name=${k}]`).fill('8');await p.locator('[name=nhanXetChoTacGia]').fill('Nhận xét đầy đủ.');await p.selectOption('[name=kienNghi]','Chỉnh sửa nhỏ');await p.check('#confirm-review');await p.click('#submit-review');await p.waitForFunction(()=>document.getElementById('workspace').hidden);assert.equal(settings.submitCount,1);await ctx.close();
  });
  await test('Pending invitation can be accepted and declined through API; form appears only after acceptance',async()=>{
    const pending={...items[0],trangThai:'Chờ phản hồi',hanPhanHoi:day(3)};
    const {p,ctx,settings,errors}=await pageFor({list:[pending]});
    await p.locator('.assignment h3 button').click();
    assert.equal(await p.getByRole('button',{name:'Viết đánh giá',exact:true}).count(),0);
    assert.equal(await p.getByRole('button',{name:'Tải bản thảo ẩn danh',exact:true}).count(),0);
    await p.getByRole('button',{name:'Nhận phản biện',exact:true}).click();
    await p.getByRole('button',{name:'Viết đánh giá',exact:true}).waitFor();
    assert.equal(settings.responsePayload.accept,true);
    assert.deepEqual(errors,[]);await ctx.close();
    const decline=await pageFor({list:[pending]});
    await decline.p.locator('.assignment h3 button').click();
    await decline.p.getByRole('button',{name:'Từ chối lời mời',exact:true}).click();
    await decline.p.waitForFunction(()=>document.getElementById('count-active').textContent==='0');
    assert.equal(decline.settings.responsePayload.accept,false);
    await decline.ctx.close();
  });
  await test('Completed evaluation detail is read from own reviewer API',async()=>{
    const {p,ctx,errors}=await pageFor({evaluationStatus:200});
    await p.click('[data-view=history]');await p.locator('.assignment h3 button').click();
    await p.getByText('Nhận xét từ API',{exact:true}).waitFor();
    assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Responsive layouts and dialog at 320, 375, 768, 1024, 1366; no page errors',async()=>{
    const {p,ctx,errors}=await pageFor();
    for(const width of [320,375,768,1024,1366]){
      await p.setViewportSize({width,height:1000});await p.locator('.assignment h3 button').first().click();
      const widths=await p.evaluate(()=>({doc:document.documentElement.scrollWidth,body:document.body.scrollWidth,w:innerWidth}));assert(widths.doc<=widths.w && widths.body<=widths.w,JSON.stringify(widths));
      await p.getByRole('button',{name:'Mở phiếu nhanh',exact:true}).click();const box=await p.locator('#evaluation-dialog').boundingBox();assert(box.x>=0 && box.x+box.width<=width+1);await p.click('#close-evaluation');
    }
    await p.setViewportSize({width:1366,height:1050});await p.screenshot({path:path.join(artifactDir,'reviewer-desktop.png'),fullPage:true});
    await p.setViewportSize({width:375,height:900});await p.screenshot({path:path.join(artifactDir,'reviewer-mobile.png'),fullPage:true});assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Dedicated BM-04 uses the same-origin API, protects confirmation and sends once',async()=>{
    const {p,ctx,settings,errors}=await pageFor({list:[items[0]]});
    await p.evaluate(()=>localStorage.removeItem('huit_api_url'));
    await p.locator('.assignment h3 button').first().click();
    await p.getByRole('button',{name:'Viết đánh giá',exact:true}).click();
    await p.waitForURL(/reviewer-evaluation\.html\?id=1/,{waitUntil:'commit',timeout:10000}).catch(e=>{throw new Error(`${e.message}; current URL: ${p.url()}`);});
    await p.getByText('Chưa có bản nháp nào được lưu').waitFor();
    assert.equal(await p.locator('#paper-title').innerText(),items[0].tieuDeBaiBao);
    await p.click('#submit-header');assert.equal(settings.submitCount,0);
    for(const k of C.scoreKeys)await p.locator(`#evaluation-form [name=${k}]`).fill('8');
    await p.locator('#comment-author').fill('Đề nghị bổ sung số liệu và giải thích phương pháp nghiên cứu.');
    await p.selectOption('#recommendation','Chỉnh sửa nhỏ');
    await p.click('#submit-header');assert.equal(settings.submitCount,0);assert.equal(await p.locator('#submit-confirmation').isVisible(),false);
    await p.check('#confirm-coi');await p.click('#submit-header');
    assert.equal(await p.locator('#submit-confirmation').isVisible(),true);
    await p.waitForTimeout(100);
    await p.click('#cancel-send');await p.locator('#submit-confirmation').waitFor({state:'hidden'});assert.equal(settings.submitCount,0);
    await p.click('#submit-header');await p.click('#confirm-send');
    await p.waitForFunction(()=>location.pathname.endsWith('/reviewer.html'));
    assert.equal(settings.submitCount,1);assert.equal(settings.payload.diemTongKet,8);assert.deepEqual(errors,[]);
    await ctx.close();
  });
  await test('Dedicated BM-04 fits 320 to 1366 pixels',async()=>{
    const {p,ctx,errors}=await pageFor();
    await p.goto(base+'/reviewer-evaluation.html?id=1');
    await p.getByText('Chưa có bản nháp nào được lưu').waitFor();
    for(const width of [320,375,768,1024,1366]){
      await p.setViewportSize({width,height:900});
      const sizes=await p.evaluate(()=>({document:document.documentElement.scrollWidth,body:document.body.scrollWidth,viewport:innerWidth}));
      assert(sizes.document<=width && sizes.body<=width,JSON.stringify(sizes));
      if(width===375 || width===1366)await p.screenshot({path:path.join(artifactDir,`reviewer-evaluation-${width}.png`),fullPage:true});
    }
    assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Dedicated BM-04 saves edits before returning to the task list',async()=>{
    const {p,ctx,settings,errors}=await pageFor({list:[items[0]]});
    await p.goto(base+'/reviewer-evaluation.html?id=1');
    await p.getByText('Chưa có bản nháp nào được lưu').waitFor();
    await p.fill('#comment-author','Bản nháp cần đọc lại phương pháp và đối chiếu số liệu.');
    await p.click('#back-link');
    await p.waitForURL(/reviewer\.html$/,{waitUntil:'commit'});
    assert.match(settings.drafts.get(1).nhanXetChoTacGia,/Bản nháp cần đọc lại/);
    await p.goto(base+'/reviewer-evaluation.html?id=1');
    await p.getByText('Đã nạp bản nháp',{exact:false}).waitFor();
    assert.match(await p.inputValue('#comment-author'),/Bản nháp cần đọc lại/);
    assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Final BM-04 submission remains possible when draft storage fails',async()=>{
    const {p,ctx,settings,errors}=await pageFor({list:[items[0]],draftStatus:503});
    await p.goto(base+'/reviewer-evaluation.html?id=1');
    await p.getByText('Chưa có bản nháp nào được lưu').waitFor();
    for(const k of C.scoreKeys)await p.locator(`#evaluation-form [name=${k}]`).fill('7');
    await p.fill('#comment-author','Phản biện chính thức với góp ý cụ thể về mẫu khảo sát.');
    await p.selectOption('#recommendation','Chỉnh sửa lớn và phản biện lại');
    await p.getByText('Chưa lưu được nháp',{exact:false}).waitFor();
    await p.check('#confirm-coi');await p.click('#submit-header');await p.click('#confirm-send');
    await p.waitForFunction(()=>location.pathname.endsWith('/reviewer.html'));
    assert.equal(settings.submitCount,1);assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Menu reviewer link only for role; safe login destination and profile integration',async()=>{
    const {p,ctx}=await pageFor();await p.goto(base+'/profile.html');await p.getByRole('link',{name:'Mở bàn làm việc phản biện →'}).waitFor();
    assert.equal(await p.locator('a.dropdown-item[href="reviewer.html"]').count(),1);await ctx.close();
    assert.match(fs.readFileSync(path.join(root,'login.html'),'utf8'),/function getSafeLoginDestination\(\)/);
  });
  await test('Unauthenticated BM-04 link resumes the assigned review after login',async()=>{
    const {p,ctx,errors}=await pageFor({token:null,user:null});
    await p.goto(base+'/reviewer-evaluation.html?id=1');
    await p.waitForURL(/login\.html\?/,{waitUntil:'commit'});
    assert.equal(new URL(p.url()).searchParams.get('next'),'reviewer-evaluation.html?id=1');
    await p.fill('#username',profile.email);await p.fill('#password','test-pass');
    await p.click('#login-page-form button[type="submit"]');
    await p.waitForURL(/reviewer-evaluation\.html\?id=1$/,{waitUntil:'commit',timeout:5000});
    await p.getByText('Chưa có bản nháp nào được lưu').waitFor();
    assert.deepEqual(errors,[]);await ctx.close();
  });
  console.log(JSON.stringify({passed:results.length,total:results.length}));
})().catch(e=>{console.error(e);results.push({status:'FAIL',error:String(e.stack)});process.exitCode=1;}).finally(async()=>{fs.writeFileSync(path.join(artifactDir,'test-results.json'),JSON.stringify({kind:'Browser integration with mocked API, not live backend validation',results},null,2));if(browser)await browser.close();server.close();});
