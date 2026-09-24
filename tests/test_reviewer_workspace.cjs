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
  {maPhanCong:1,maBaiBao:10,soVong:2,tieuDeBaiBao:'Đánh giá mô hình học máy trong dự báo chất lượng không khí đô thị',chuyenNganh:'Công nghệ thông tin',trangThai:'Đang phản biện',ngayPhanCong:day(-10),hanHoanThanh:day(2),daDanhGia:false},
  {maPhanCong:2,maBaiBao:20,soVong:1,tieuDeBaiBao:'Nghiên cứu ứng dụng vật liệu sinh học trong xử lý nước thải công nghiệp',chuyenNganh:'Khoa học Môi trường',trangThai:'Đang phản biện',ngayPhanCong:day(-20),hanHoanThanh:day(-1),daDanhGia:false},
  {maPhanCong:3,maBaiBao:10,soVong:1,tieuDeBaiBao:'Đánh giá mô hình học máy trong dự báo chất lượng không khí đô thị',chuyenNganh:'Công nghệ thông tin',trangThai:'Đã đánh giá',ngayPhanCong:day(-30),hanHoanThanh:day(-10),daDanhGia:true,diemTongKet:0,kienNghi:'Chỉnh sửa lớn và phản biện lại'}
];
let browser,base;
async function test(name,fn){await fn(); results.push({name,status:'PASS'});console.log('PASS',name);}
async function pageFor(options={}){
  const ctx=await browser.newContext({viewport:{width:1366,height:1000}});
  const p=await ctx.newPage(); const errors=[];p.on('pageerror',e=>errors.push(e.message));
  const settings={user:profile,token:'real-test-token',list:items,submitStatus:200,submitCount:0,...options};
  await p.addInitScript(({base,settings})=>{
    if(settings.token)localStorage.setItem('journal_token',settings.token);
    if(settings.user)localStorage.setItem('journal_user',JSON.stringify(settings.user));
    localStorage.setItem('huit_api_url',base+'/api');
  },{base,settings});
  await p.route('https://fonts.googleapis.com/**',r=>r.fulfill({status:200,body:''}));
  await p.route('**/api/**',async r=>{
    const u=new URL(r.request().url());
    if(u.pathname==='/api/auth/profile')return r.fulfill({status:settings.profileStatus||200,json:settings.user});
    if(u.pathname==='/api/phanbien/my-assignments')return r.fulfill({status:settings.listStatus||200,json:settings.list});
    if(u.pathname==='/api/phanbien/evaluate'){settings.submitCount++;settings.payload=r.request().postDataJSON();return r.fulfill({status:settings.submitStatus,json:settings.submitStatus===200?{success:true}:{success:false,message:'Không thể nhận phiếu thử nghiệm.'}});}
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
    await p.locator('.assignment h3 button').first().click();await p.getByRole('button',{name:'Viết đánh giá',exact:true}).click();
    for(const k of C.scoreKeys)await p.locator(`[name=${k}]`).fill('0');
    await p.locator('[name=nhanXetChoTacGia]').fill('Nhận xét kiểm thử: cần bổ sung phương pháp và dữ liệu.');await p.selectOption('[name=kienNghi]','Từ chối đăng');
    await p.click('#close-evaluation');await p.getByRole('button',{name:'Tiếp tục bản nháp',exact:true}).click();
    assert.match(await p.locator('[name=nhanXetChoTacGia]').inputValue(),/Nhận xét kiểm thử/);await p.check('#confirm-review');await p.click('#submit-review');
    await p.getByText('Không thể nhận phiếu thử nghiệm.',{exact:true}).waitFor();assert.equal(await p.locator('#evaluation-dialog').isVisible(),true);assert.equal(settings.submitCount,1);
    settings.submitStatus=200;await p.click('#submit-review');await p.waitForFunction(()=>!document.getElementById('evaluation-dialog').open);
    assert.equal(settings.payload.diemTongKet,0);assert.equal(settings.submitCount,2);assert.equal(await p.locator('[data-view=history]').getAttribute('aria-pressed'),'true');assert.equal(await p.locator('#count-done').innerText(),'2');assert.deepEqual(errors,[]);await ctx.close();
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
    const {p,ctx,settings}=await pageFor({submitStatus:401});await p.locator('.assignment h3 button').first().click();await p.getByRole('button',{name:'Viết đánh giá',exact:true}).click();
    for(const k of C.scoreKeys)await p.locator(`[name=${k}]`).fill('8');await p.locator('[name=nhanXetChoTacGia]').fill('Nhận xét đầy đủ.');await p.selectOption('[name=kienNghi]','Chỉnh sửa nhỏ');await p.check('#confirm-review');await p.click('#submit-review');await p.waitForFunction(()=>document.getElementById('workspace').hidden);assert.equal(settings.submitCount,1);await ctx.close();
  });
  await test('Explicit demo data: isolated per account; no fallback to legacy shared storage',async()=>{
    const {p,ctx}=await pageFor({token:'standalone_token_test'});assert.equal(await p.locator('.assignment').count(),0);await p.click('#load-demo');await p.waitForFunction(()=>document.querySelectorAll('.assignment').length===3);
    assert.match(await p.locator('#demo-notice').innerText(),/dữ liệu mô phỏng/);
    const keys=await p.evaluate(()=>Object.keys(localStorage));assert(keys.includes('huit-reviewer:demo:11:assignments'));
    await p.evaluate(()=>{const u=JSON.parse(localStorage.getItem('journal_user'));u.maNguoiDung=12;localStorage.setItem('journal_user',JSON.stringify(u));});
    // Remove seeding script by checking storage boundary directly via a new page in the same context.
    const q=await ctx.newPage();await q.goto(base+'/reviewer.html');await q.waitForFunction(()=>document.getElementById('list-status').textContent==='0 công việc mô phỏng');assert.equal(await q.locator('.assignment').count(),0);await ctx.close();
  });
  await test('Responsive layouts and dialog at 320, 375, 768, 1024, 1366; no page errors',async()=>{
    const {p,ctx,errors}=await pageFor();
    for(const width of [320,375,768,1024,1366]){
      await p.setViewportSize({width,height:1000});await p.locator('.assignment h3 button').first().click();
      const widths=await p.evaluate(()=>({doc:document.documentElement.scrollWidth,body:document.body.scrollWidth,w:innerWidth}));assert(widths.doc<=widths.w && widths.body<=widths.w,JSON.stringify(widths));
      await p.getByRole('button',{name:/^(Viết đánh giá|Tiếp tục bản nháp)$/}).click();const box=await p.locator('#evaluation-dialog').boundingBox();assert(box.x>=0 && box.x+box.width<=width+1);await p.click('#close-evaluation');
    }
    await p.setViewportSize({width:1366,height:1050});await p.screenshot({path:path.join(artifactDir,'reviewer-desktop.png'),fullPage:true});
    await p.setViewportSize({width:375,height:900});await p.screenshot({path:path.join(artifactDir,'reviewer-mobile.png'),fullPage:true});assert.deepEqual(errors,[]);await ctx.close();
  });
  await test('Menu reviewer link only for role; safe login destination and profile integration',async()=>{
    const {p,ctx}=await pageFor();await p.goto(base+'/profile.html');await p.getByRole('link',{name:'Mở bàn làm việc phản biện →'}).waitFor();
    assert.equal(await p.locator('a.dropdown-item[href="reviewer.html"]').count(),1);await ctx.close();
    assert.match(fs.readFileSync(path.join(root,'login.html'),'utf8'),/get\('next'\) === 'reviewer.html'/);
  });
  console.log(JSON.stringify({passed:results.length,total:results.length}));
})().catch(e=>{console.error(e);results.push({status:'FAIL',error:String(e.stack)});process.exitCode=1;}).finally(async()=>{fs.writeFileSync(path.join(artifactDir,'test-results.json'),JSON.stringify({kind:'Browser integration with mocked API, not live backend validation',results},null,2));if(browser)await browser.close();server.close();});
