/* Reviewer workspace: strict online adapter, explicitly labelled demo, safe DOM rendering. */
(() => {
  'use strict';
  const C = window.ReviewerCore;
  const $ = id => document.getElementById(id);
  const state = { user:null, token:null, mode:null, items:[], selected:null, page:1, view:'active', busy:false, selectionVersion:0 };
  const PAGE_SIZE = 8;
  let toastTimer;
  function read(store,key,fallback=null) { try { return JSON.parse(store.getItem(key)) ?? fallback; } catch { return fallback; } }
  function token() { try { return localStorage.getItem('journal_token'); } catch { return null; } }
  function el(tag,text,cls) { const n=document.createElement(tag); if(text != null)n.textContent=String(text); if(cls)n.className=cls; return n; }
  function button(text,fn,cls) { const b=el('button',text,cls); b.type='button'; b.addEventListener('click',fn); return b; }
  function toast(message) { $('notice').textContent=message; $('notice').hidden=false; clearTimeout(toastTimer); toastTimer=setTimeout(()=>$('notice').hidden=true,6500); }
  function date(value) { const t=C.parseDate(value); return Number.isFinite(t)?new Intl.DateTimeFormat('vi-VN',{day:'2-digit',month:'2-digit',year:'numeric',timeZone:'Asia/Ho_Chi_Minh'}).format(t):'Chưa xác định'; }
  function scope() { return `${state.mode}:${state.user.maNguoiDung ?? state.user.id ?? state.user.email}`; }
  function key(kind,a) { return `huit-reviewer:${scope()}:${kind}${a?`:${a.maPhanCong}:${a.soVong}`:''}`; }
  function assertSession() { if (!state.user || token() !== state.token) { const e=new Error('Phiên làm việc đã thay đổi. Vui lòng đăng nhập lại.'); e.status=401; throw e; } }
  function base() {
    const custom=localStorage.getItem('huit_api_url');
    const raw=custom || (location.port==='5000'?'/api':'http://localhost:5000/api');
    const u=new URL(raw,location.href);
    if(!['http:','https:'].includes(u.protocol)) throw new Error('Địa chỉ dịch vụ không hợp lệ.');
    if(location.protocol==='https:' && u.protocol==='http:') throw new Error('Dịch vụ chưa có kết nối HTTPS phù hợp. Vui lòng liên hệ tòa soạn.');
    return u.href.replace(/\/$/,'');
  }
  async function request(path,{method='GET',body,blob=false}={}) {
    assertSession();
    const controller=new AbortController(); const timer=setTimeout(()=>controller.abort(),15000);
    try {
      const res=await fetch(`${base()}${path}`,{method,headers:{Authorization:`Bearer ${state.token}`,...(body?{'Content-Type':'application/json'}:{})},body:body?JSON.stringify(body):undefined,signal:controller.signal,cache:'no-store',redirect:'error'});
      assertSession();
      if(!res.ok) {
        const data=await res.json().catch(()=>null);
        const e=new Error(res.status===401?'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.':res.status===403?'Bạn không có quyền thực hiện thao tác này.':
          res.status===404||res.status===405?'Chức năng này chưa được tòa soạn cung cấp hoặc hồ sơ không còn khả dụng.':data?.message||`Không thể xử lý yêu cầu (${res.status}).`);
        e.status=res.status; throw e;
      }
      if(blob) {
        const type=(res.headers.get('content-type')||'').split(';')[0];
        if(!['application/pdf','application/vnd.openxmlformats-officedocument.wordprocessingml.document','application/msword','application/octet-stream'].includes(type)) throw new Error('Phản hồi không phải tệp bản thảo hợp lệ.');
        return {blob:await res.blob(),type};
      }
      return await res.json();
    } catch(e) {
      if(e.name==='AbortError') throw new Error('Kết nối quá thời gian chờ. Dữ liệu chưa được xác nhận; hãy làm mới trước khi gửi lại.');
      if(e instanceof TypeError) throw new Error('Không kết nối được tòa soạn. Hãy thử lại; dữ liệu không được chuyển sang chế độ mô phỏng.');
      throw e;
    } finally { clearTimeout(timer); }
  }
  function deny(message,login=false) {
    state.selectionVersion++; state.items=[]; state.selected=null;
    $('workspace').hidden=true; $('access-state').hidden=false;
    if($('evaluation-dialog').open)$('evaluation-dialog').close();
    $('access-state').replaceChildren(el('h2',login?'Cần đăng nhập':'Chưa thể mở bàn làm việc'),el('p',message));
    const link=el('a',login?'Đăng nhập để tiếp tục':'Về trang cá nhân'); link.href=login?'login.html?next=reviewer.html':'profile.html';
    $('access-state').append(link,button('Thử lại',()=>location.reload()));
  }
  function fail(e) { if(e.status===401 || e.status===403) deny(e.message,e.status===401); else toast(e.message); }
  async function boot() {
    state.token=token();
    if(!state.token) { deny('Sử dụng tài khoản có vai trò phản biện để truy cập.',true); return; }
    state.mode=state.token.startsWith('standalone_token_')?'demo':'online';
    state.user=read(localStorage,'journal_user',{});
    try {
      if(state.mode==='online') state.user=await request('/auth/profile');
      if(!C.hasRole(state.user)) { deny('Tài khoản của bạn chưa có vai trò người phản biện. Liên hệ Ban biên tập để được phân quyền.'); return; }
      if(!(state.user.maNguoiDung ?? state.user.id ?? state.user.email)) throw new Error('Thiếu định danh tài khoản. Vui lòng đăng nhập lại.');
      $('access-state').hidden=true; $('workspace').hidden=false; $('logout').hidden=false;
      $('greeting').textContent=`Xin chào, ${state.user.hoTen || 'chuyên gia'}`;
      $('mode-label').textContent=state.mode==='demo'?'Phiên trình diễn độc lập':'Phiên kết nối tòa soạn';
      $('demo-notice').hidden=state.mode!=='demo';
      await refresh();
    } catch(e) { deny(e.message,e.status===401); }
  }
  async function refresh() {
    if(state.busy)return; state.busy=true; $('refresh').disabled=true; $('assignment-list').setAttribute('aria-busy','true');
    $('list-status').textContent='Đang tải công việc…';
    try {
      assertSession();
      const items=state.mode==='demo'?read(localStorage,key('assignments'),[]):await request('/phanbien/my-assignments');
      if(!Array.isArray(items) || items.some(a=>!Number.isInteger(a.maPhanCong)||a.maPhanCong<1)) throw new Error('Danh sách phân công không đúng định dạng. Vui lòng liên hệ tòa soạn.');
      state.items=items; render();
      if(state.selected) { const found=items.find(a=>a.maPhanCong===state.selected.maPhanCong); if(found)await select(found,false); else clearDetail(); }
    } catch(e) {
      state.items=[]; render(); clearDetail(); $('list-status').textContent=e.message; $('list-status').classList.add('error'); fail(e);
    } finally { state.busy=false; $('refresh').disabled=false; $('assignment-list').setAttribute('aria-busy','false'); }
  }
  function draft(a) { return read(sessionStorage,key('draft',a)); }
  function render() {
    $('count-active').textContent=state.items.filter(a=>!C.inactive(a)).length;
    $('count-due').textContent=state.items.filter(a=>C.deadline(a)==='due').length;
    $('count-overdue').textContent=state.items.filter(a=>C.deadline(a)==='overdue').length;
    $('count-done').textContent=state.items.filter(C.completed).length;
    const filtered=C.filter(state.items,{view:state.view,search:$('search').value,status:$('status-filter').value,sort:$('sort').value,draftIds:state.items.filter(a=>draft(a)&&!C.inactive(a)).map(a=>a.maPhanCong)});
    const pages=Math.max(1,Math.ceil(filtered.length/PAGE_SIZE)); state.page=Math.min(state.page,pages);
    $('list-status').classList.remove('error'); $('list-status').textContent=`${filtered.length} công việc${state.mode==='demo'?' mô phỏng':''}`;
    $('page-number').textContent=`${state.page} / ${pages}`; $('prev-page').disabled=state.page===1; $('next-page').disabled=state.page===pages;
    const list=$('assignment-list'); list.replaceChildren();
    if(!filtered.length) { const empty=el('div',null,'empty-state'); empty.append(el('h3',state.items.length?'Không có kết quả phù hợp':'Chưa có công việc'),el('p',state.items.length?'Thử đổi bộ lọc hoặc từ khóa.':state.mode==='demo'?'Nạp công việc mẫu để trải nghiệm trang phản biện.':'Các bài được tòa soạn phân công sẽ xuất hiện tại đây.')); list.append(empty); }
    for(const a of filtered.slice((state.page-1)*PAGE_SIZE,state.page*PAGE_SIZE)) {
      const article=el('article',null,'assignment'+(a.maPhanCong===state.selected?.maPhanCong?' is-selected':''));
      const top=el('div',null,'assignment-top'); top.append(el('span',`BÀI #${a.maBaiBao} · VÒNG ${a.soVong}`),el('span',C.completed(a)?'Đã đánh giá':a.trangThai,C.completed(a)?'done':''));
      const h=el('h3'); const open=button(a.tieuDeBaiBao,()=>select(a,true)); open.setAttribute('aria-label',`Mở công việc: ${a.tieuDeBaiBao}`); h.append(open);
      const deadlineText=C.completed(a)?`Kiến nghị: ${a.kienNghi || 'Chưa có thông tin'}`:`${a.trangThai==='Chờ phản hồi'?'Hạn phản hồi':'Hạn hoàn thành'}: ${date(a.trangThai==='Chờ phản hồi'?(a.hanPhanHoi||a.hanHoanThanh):a.hanHoanThanh)}`;
      article.append(top,h,el('p',a.chuyenNganh,'meta'),el('p',deadlineText+(C.deadline(a)==='overdue'?' · Quá hạn':''),'deadline '+(C.deadline(a)==='overdue'?'overdue':'')));
      if(draft(a)&&!C.inactive(a))article.append(el('span','Có nháp trong phiên','draft-tag'));
      list.append(article);
    }
  }
  function clearDetail() { state.selectionVersion++; state.selected=null; $('detail').replaceChildren(el('p','HỒ SƠ PHẢN BIỆN','eyebrow'),el('h2','Chọn một công việc'),el('p','Mở một bài trong danh sách để xem thông tin.','muted')); }
  function row(dl,label,value) { const r=el('div'); r.append(el('dt',label),el('dd',value??'Chưa có')); dl.append(r); }
  async function select(a,focus) {
    const version=++state.selectionVersion; state.selected=a; render();
    const panel=$('detail'); panel.replaceChildren(el('p',`BÀI #${a.maBaiBao} · PHÂN CÔNG #${a.maPhanCong}`,'eyebrow'));
    const title=el('h2',a.tieuDeBaiBao); title.id='detail-title'; panel.append(title);
    const dl=el('dl'); row(dl,'Chuyên ngành',a.chuyenNganh); row(dl,'Vòng phản biện',a.soVong); row(dl,'Trạng thái',a.trangThai); row(dl,'Ngày phân công',date(a.ngayPhanCong)); row(dl,'Hạn phản hồi',date(a.hanPhanHoi)); row(dl,'Hạn hoàn thành',date(a.hanHoanThanh)); panel.append(dl);
    const actions=el('div',null,'detail-actions');
    if(!C.inactive(a)) actions.append(button(draft(a)?'Tiếp tục bản nháp':'Viết đánh giá',()=>openEvaluation(a),'primary'));
    actions.append(button('Tải bản thảo ẩn danh',e=>downloadManuscript(a,e.currentTarget)));
    if(!C.inactive(a)&&Number.isFinite(C.due(a)))actions.append(button('Lưu hạn vào lịch',()=>calendar(a)));
    panel.append(actions);
    if(state.mode==='demo'&&a.trangThai==='Chờ phản hồi') {
      const invite=el('div',null,'detail-actions'); invite.append(button('Nhận phản biện (demo)',()=>demoRespond(a,true)),button('Từ chối (demo)',()=>demoRespond(a,false))); panel.append(invite);
    }
    if(C.completed(a)) {
      const history=el('section'); history.append(el('h3','Đánh giá đã gửi'),el('p',`Điểm tổng kết: ${a.diemTongKet ?? '—'}/10`),el('p',`Kiến nghị: ${a.kienNghi || 'Chưa có thông tin'}`)); panel.append(history);
      let review=state.mode==='demo'?a.evaluation:read(sessionStorage,key('receipt',a));
      if(!review && state.mode==='online') {
        const loading=el('p','Đang tải nội dung phiếu…','muted'); history.append(loading);
        try { const detail=await request(`/phanbien/assignments/${a.maPhanCong}/evaluation`); if(detail.maPhanCong!==a.maPhanCong)throw new Error('Phiếu đánh giá không khớp phân công.'); review=detail; }
        catch(e) { if(version===state.selectionVersion) { loading.textContent=[404,405].includes(e.status)?'Hiện chỉ có điểm và kiến nghị. Nội dung phiếu cũ chưa được tòa soạn cung cấp.':e.message; if(e.status===401||e.status===403)fail(e); } }
        if(version!==state.selectionVersion)return; if(review)loading.remove();
      }
      if(review) {
        if(C.scoreKeys.some(k=>review[k]!=null)) { const scores=el('dl'); ['Tính mới','Phương pháp','Kết quả','Trình bày'].forEach((label,i)=>row(scores,label,review[C.scoreKeys[i]]??'—')); history.append(scores); }
        history.append(el('h3','Nhận xét gửi tác giả'),el('p',review.nhanXetChoTacGia||'Chưa có nội dung.','review-text'),el('h3','Nhận xét riêng cho Ban biên tập'),el('p',review.nhanXetBaoMat||'Không có.','review-text'));
      }
      history.append(button('Xuất bản ghi đánh giá',()=>exportReview(a,review)));
    }
    const previous=state.items.filter(x=>x.maBaiBao===a.maBaiBao&&x.maPhanCong!==a.maPhanCong&&C.completed(x)).sort((x,y)=>y.soVong-x.soVong);
    if(previous.length) { const rounds=el('section'); rounds.append(el('h3','Đánh giá khác của bạn trên bài này')); previous.forEach(p=>rounds.append(button(`Vòng ${p.soVong} · ${p.kienNghi||'Xem đánh giá'}`,()=>select(p,true)))); panel.append(rounds); }
    const note=el('div',null,'privacy-note'); note.append(el('strong','Bảo mật phản biện'),el('p','Trang này chỉ hiển thị công việc và đánh giá của bạn. Không chia sẻ bản thảo hoặc nhận xét bảo mật.')); panel.append(note);
    if(focus) { panel.focus({preventScroll:true}); if(innerWidth<=1000)panel.scrollIntoView({behavior:'auto',block:'start'}); }
  }
  function download(content,name,type) { const url=URL.createObjectURL(content instanceof Blob?content:new Blob([content],{type})); const a=el('a'); a.href=url; a.download=name; document.body.append(a); a.click(); a.remove(); setTimeout(()=>URL.revokeObjectURL(url),30000); }
  async function downloadManuscript(a,b) {
    if(state.mode==='demo') { toast('Dữ liệu trình diễn không có bản thảo thật. Chức năng tải yêu cầu tệp ẩn danh do tòa soạn cung cấp.'); return; }
    b.disabled=true;
    try { const result=await request(`/phanbien/assignments/${a.maPhanCong}/manuscript`,{blob:true}); const ext=result.type==='application/pdf'?'pdf':result.type==='application/msword'?'doc':result.type.includes('wordprocessingml')?'docx':'bin'; download(result.blob,`Ban-thao-an-danh-${a.maPhanCong}-vong-${a.soVong}.${ext}`); } catch(e) { fail(e); } finally { b.disabled=false; }
  }
  function calendar(a) {
    const stamp=t=>new Date(t).toISOString().replace(/[-:]/g,'').replace(/\.\d{3}Z$/,'Z');
    const text=['BEGIN:VCALENDAR','VERSION:2.0','PRODID:-//HUIT Journal//Reviewer//VI','BEGIN:VEVENT',`UID:review-${a.maPhanCong}-${a.soVong}@huitjournal.local`,`DTSTAMP:${stamp(Date.now())}`,`DTSTART:${stamp(C.due(a))}`,`SUMMARY:Han phan bien bai ${a.maBaiBao} - vong ${a.soVong}`,'DESCRIPTION:Mo ban lam viec phan bien de xem chi tiet.','END:VEVENT','END:VCALENDAR',''].join('\r\n');
    download(text,`Han-phan-bien-${a.maPhanCong}.ics`,'text/calendar;charset=utf-8');
  }
  function exportReview(a,review) {
    const content=[state.mode==='demo'?'DỮ LIỆU MÔ PHỎNG':'BẢN GHI ĐÁNH GIÁ CÁ NHÂN',a.tieuDeBaiBao,`Phân công: ${a.maPhanCong} · Vòng: ${a.soVong}`,`Điểm: ${a.diemTongKet??'—'}/10`,`Kiến nghị: ${a.kienNghi||'—'}`,'Nhận xét gửi tác giả:',review?.nhanXetChoTacGia||'Chưa có nội dung phiếu từ tòa soạn.','', 'Bản xuất không bao gồm nhận xét bảo mật.'].join('\n');
    download('\uFEFF'+content,`Danh-gia-${a.maPhanCong}.txt`,'text/plain;charset=utf-8');
  }
  function formData() { const data=Object.fromEntries(new FormData($('evaluation-form'))); data.maPhanCong=state.selected.maPhanCong; return data; }
  function updateTotal() { const d=formData(); $('total-score').textContent=C.scoreKeys.every(k=>d[k]!==''&&Number.isFinite(Number(d[k])))?C.total(d).toFixed(1):'—'; }
  function saveDraft(silent=false) {
    try { assertSession(); const data=formData();
      const hasContent=[...C.scoreKeys,'nhanXetChoTacGia','nhanXetBaoMat','kienNghi'].some(k=>String(data[k]??'').trim()!=='');
      if(!hasContent) { sessionStorage.removeItem(key('draft',state.selected)); $('draft-status').textContent='Bản nháp đang trống.'; render(); return; }
      sessionStorage.setItem(key('draft',state.selected),JSON.stringify({...data,savedAt:new Date().toISOString()})); $('draft-status').textContent='Đã lưu nháp trong phiên · '+new Date().toLocaleTimeString('vi-VN'); if(!silent)toast('Đã lưu nháp trong tab này. Phiếu chưa được gửi.'); render();
    } catch(e) { $('draft-status').textContent='Không lưu được nháp. Giữ tab mở và sao chép nội dung trước khi rời trang.'; }
  }
  function openEvaluation(a) {
    if(C.inactive(a))return;
    state.selected=a; $('evaluation-form').reset(); $('form-error').textContent='';
    $('evaluation-title').textContent=a.tieuDeBaiBao; const saved=draft(a);
    if(saved)for(const [k,v] of Object.entries(saved)) { const input=$('evaluation-form').elements.namedItem(k); if(input)input.value=v; }
    $('draft-status').textContent=saved?'Đã khôi phục nháp trong phiên. Phiếu chưa được gửi.':'Bản nháp chưa được gửi.';
    $('confirm-review').checked=false; updateTotal(); $('evaluation-dialog').showModal();
  }
  async function submit(event) {
    event.preventDefault(); if($('submit-review').disabled)return;
    const raw=formData(); const error=C.validate(raw); if(error) { $('form-error').textContent=error; return; }
    const a=state.selected; const data={...raw}; C.scoreKeys.forEach(k=>data[k]=Number(data[k])); data.diemTongKet=C.total(data); data.nhanXetChoTacGia=data.nhanXetChoTacGia.trim();
    $('submit-review').disabled=true; $('close-evaluation').disabled=true; $('submit-review').textContent='Đang gửi…'; $('form-error').textContent='';
    try {
      assertSession(); if(C.inactive(a))throw new Error('Công việc đã kết thúc. Vui lòng làm mới.');
      if(state.mode==='demo') {
        const updated=state.items.map(x=>x.maPhanCong===a.maPhanCong?{...x,daDanhGia:true,trangThai:'Đã đánh giá',diemTongKet:data.diemTongKet,kienNghi:data.kienNghi,evaluation:data}:x);
        localStorage.setItem(key('assignments'),JSON.stringify(updated)); state.items=updated;
      } else {
        const result=await request('/phanbien/evaluate',{method:'POST',body:data});
        if(result.success!==true)throw new Error(result.message||'Tòa soạn chưa xác nhận phiếu đánh giá.');
        // Receipt is only a session convenience, never an alternative source for assignments.
        try { sessionStorage.setItem(key('receipt',a),JSON.stringify(data)); } catch {}
        state.items=state.items.map(x=>x.maPhanCong===a.maPhanCong?{...x,daDanhGia:true,trangThai:'Đã đánh giá',diemTongKet:data.diemTongKet,kienNghi:data.kienNghi}:x);
      }
      try { sessionStorage.removeItem(key('draft',a)); } catch {}
      $('evaluation-dialog').close(); state.view='history'; setView(); state.selected=state.items.find(x=>x.maPhanCong===a.maPhanCong); render(); await select(state.selected,false);
      toast(state.mode==='demo'?'Đã lưu đánh giá mô phỏng. Không gửi đến tòa soạn.':'Tòa soạn đã xác nhận phiếu đánh giá.');
    } catch(e) { $('form-error').textContent=e.message; if(e.status===401||e.status===403)fail(e); }
    finally { $('submit-review').disabled=false; $('close-evaluation').disabled=false; $('submit-review').textContent='Gửi phiếu đánh giá'; }
  }
  function demoRespond(a,accept) {
    try { assertSession(); const changed=state.items.map(x=>x.maPhanCong===a.maPhanCong?{...x,trangThai:accept?'Đang phản biện':'Từ chối phản biện'}:x); localStorage.setItem(key('assignments'),JSON.stringify(changed)); state.items=changed; select(changed.find(x=>x.maPhanCong===a.maPhanCong),false); toast('Đã cập nhật lời mời mô phỏng.'); } catch(e) { fail(e); }
  }
  function seed() {
    if(state.mode!=='demo')return;
    if(state.items.length) { toast('Đã có công việc mô phỏng; dữ liệu hiện tại được giữ nguyên.'); return; }
    const day=n=>new Date(Date.now()+n*86400000).toISOString();
    const items=[
      {maPhanCong:201,maBaiBao:101,soVong:2,tieuDeBaiBao:'Đánh giá mô hình học máy trong dự báo chất lượng không khí đô thị',chuyenNganh:'Công nghệ thông tin',trangThai:'Đang phản biện',ngayPhanCong:day(-10),hanHoanThanh:day(2),daDanhGia:false},
      {maPhanCong:202,maBaiBao:102,soVong:1,tieuDeBaiBao:'Ứng dụng vật liệu sinh học trong xử lý nước thải công nghiệp',chuyenNganh:'Khoa học Môi trường',trangThai:'Chờ phản hồi',ngayPhanCong:day(-1),hanPhanHoi:day(1),hanHoanThanh:day(15),daDanhGia:false},
      {maPhanCong:203,maBaiBao:103,soVong:1,tieuDeBaiBao:'Các yếu tố ảnh hưởng đến chuyển đổi số tại doanh nghiệp nhỏ',chuyenNganh:'Kinh tế – Quản trị',trangThai:'Đang phản biện',ngayPhanCong:day(-22),hanHoanThanh:day(-2),daDanhGia:false},
      {maPhanCong:204,maBaiBao:101,soVong:1,tieuDeBaiBao:'Đánh giá mô hình học máy trong dự báo chất lượng không khí đô thị',chuyenNganh:'Công nghệ thông tin',trangThai:'Đã đánh giá',ngayPhanCong:day(-40),hanHoanThanh:day(-20),daDanhGia:true,diemTongKet:7.5,kienNghi:'Chỉnh sửa lớn và phản biện lại',evaluation:{nhanXetChoTacGia:'[Nhận xét mô phỏng] Cần mô tả tập dữ liệu kiểm chứng và bổ sung so sánh với phương pháp cơ sở.',nhanXetBaoMat:'[Nhận xét mô phỏng] Đề nghị xem lại sau khi tác giả hoàn thiện thực nghiệm.'}}
    ];
    try { assertSession(); localStorage.setItem(key('assignments'),JSON.stringify(items)); refresh(); } catch(e) { fail(e); }
  }
  function setView() { document.querySelectorAll('[data-view]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.view===state.view))); $('status-filter').value='all'; state.page=1; }
  function clearSessionDrafts() { try { for(let i=sessionStorage.length-1;i>=0;i--) { const k=sessionStorage.key(i); if(k.startsWith('huit-reviewer:'))sessionStorage.removeItem(k); } } catch {} }
  $('logout').addEventListener('click',()=>{ clearSessionDrafts(); localStorage.removeItem('journal_token'); localStorage.removeItem('journal_user'); location.href='login.html'; });
  window.addEventListener('storage',e=>{ if(e.key==='journal_token' || e.key===null) { clearSessionDrafts(); deny('Phiên làm việc đã thay đổi ở tab khác. Vui lòng đăng nhập lại.',true); } });
  $('refresh').addEventListener('click',refresh); $('load-demo').addEventListener('click',seed);
  document.querySelectorAll('[data-view]').forEach(b=>b.addEventListener('click',()=>{state.view=b.dataset.view;setView();render();}));
  ['search','status-filter','sort'].forEach(id=>$(id).addEventListener(id==='search'?'input':'change',()=>{state.page=1;render();}));
  $('prev-page').addEventListener('click',()=>{state.page--;render();}); $('next-page').addEventListener('click',()=>{state.page++;render();});
  $('evaluation-form').addEventListener('input',()=>{updateTotal();saveDraft(true);});
  $('evaluation-form').addEventListener('submit',submit); $('save-draft').addEventListener('click',()=>saveDraft());
  $('close-evaluation').addEventListener('click',()=>{saveDraft(true);$('evaluation-dialog').close();select(state.selected,false);});
  $('evaluation-dialog').addEventListener('cancel',e=>{if($('submit-review').disabled)e.preventDefault();else {saveDraft(true);select(state.selected,false);}});
  boot();
})();
