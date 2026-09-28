/* Reviewer workspace: strict online adapter, safe DOM rendering. */
(() => {
  'use strict';
  const C = window.ReviewerCore;
  const $ = id => document.getElementById(id);
  const state = { user:null, token:null, mode:null, items:[], selected:null, page:1, view:'active', busy:false, selectionVersion:0, drafts:new Map(), draftVersion:0 };
  const PAGE_SIZE = 8;
  let toastTimer;
  let draftTimer;
  let draftSaveQueue = Promise.resolve();
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
    const isLocalStatic=['localhost','127.0.0.1'].includes(location.hostname) && ['8088','5500','5501'].includes(location.port);
    const raw=custom || (isLocalStatic?'http://localhost:5000/api':'/api');
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
      if(res.status===204 || res.status===205)return null;
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
    state.mode='online';
    state.user=read(localStorage,'journal_user',{});
    try {
      state.user=await request('/auth/profile');
      if(!C.hasRole(state.user)) { deny('Tài khoản của bạn chưa có vai trò người phản biện. Liên hệ Ban biên tập để được phân quyền.'); return; }
      if(!(state.user.maNguoiDung ?? state.user.id ?? state.user.email)) throw new Error('Thiếu định danh tài khoản. Vui lòng đăng nhập lại.');
      $('access-state').hidden=true; $('workspace').hidden=false;
      $('greeting').textContent=`Xin chào, ${state.user.hoTen || 'chuyên gia'}`;
      await refresh();
    } catch(e) { deny(e.message,e.status===401); }
  }
  async function refresh() {
    if(state.busy)return; state.busy=true; $('refresh').disabled=true; $('assignment-list').setAttribute('aria-busy','true');
    $('list-status').textContent='Đang tải công việc…';
    try {
      assertSession();
      const items=await request('/phanbien/my-assignments');
      if(!Array.isArray(items) || items.some(a=>!Number.isInteger(a.maPhanCong)||a.maPhanCong<1)) throw new Error('Danh sách phân công không đúng định dạng. Vui lòng liên hệ tòa soạn.');
      state.items=items;
      try {
        const drafts=await request('/phanbien/my-evaluation-drafts');
        state.drafts=new Map((Array.isArray(drafts)?drafts:[]).map(d=>[d.maPhanCong,{savedAt:d.ngayCapNhatUtc}]));
      } catch(e) { if(![404,405].includes(e.status))throw e; state.drafts=new Map(); }
      render();
      if(state.selected) { const found=items.find(a=>a.maPhanCong===state.selected.maPhanCong); if(found)await select(found,false); else clearDetail(); }
    } catch(e) {
      state.items=[]; render(); clearDetail(); $('list-status').textContent=e.message; $('list-status').classList.add('error'); fail(e);
    } finally { state.busy=false; $('refresh').disabled=false; $('assignment-list').setAttribute('aria-busy','false'); }
  }
  function draft(a) { return state.drafts.has(a.maPhanCong) || !!read(sessionStorage,key('draft',a)); }
  function render() {
    $('count-active').textContent=state.items.filter(a=>!C.inactive(a)).length;
    $('count-due').textContent=state.items.filter(a=>C.deadline(a)==='due').length;
    $('count-overdue').textContent=state.items.filter(a=>C.deadline(a)==='overdue').length;
    $('count-done').textContent=state.items.filter(C.completed).length;
    const filtered=C.filter(state.items,{view:state.view,search:$('search').value,status:$('status-filter').value,sort:$('sort').value,draftIds:state.items.filter(a=>draft(a)&&!C.inactive(a)).map(a=>a.maPhanCong)});
    const pages=Math.max(1,Math.ceil(filtered.length/PAGE_SIZE)); state.page=Math.min(state.page,pages);
    $('list-status').classList.remove('error'); $('list-status').textContent=`${filtered.length} công việc`;
    $('page-number').textContent=`${state.page} / ${pages}`; $('prev-page').disabled=state.page===1; $('next-page').disabled=state.page===pages;
    const list=$('assignment-list'); list.replaceChildren();
    if(!filtered.length) { const empty=el('div',null,'empty-state'); empty.append(el('h3',state.items.length?'Không có kết quả phù hợp':'Chưa có công việc'),el('p',state.items.length?'Thử đổi bộ lọc hoặc từ khóa.':'Các bài được tòa soạn phân công sẽ xuất hiện tại đây.')); list.append(empty); }
    for(const a of filtered.slice((state.page-1)*PAGE_SIZE,state.page*PAGE_SIZE)) {
      const article=el('article',null,'assignment'+(a.maPhanCong===state.selected?.maPhanCong?' is-selected':''));
      const top=el('div',null,'assignment-top'); top.append(el('span',`BÀI #${a.maBaiBao} · VÒNG ${a.soVong}`),el('span',C.completed(a)?'Đã đánh giá':a.trangThai,C.completed(a)?'done':''));
      const h=el('h3'); const open=button(a.tieuDeBaiBao,()=>select(a,true)); open.setAttribute('aria-label',`Mở công việc: ${a.tieuDeBaiBao}`); h.append(open);
      const deadlineText=C.completed(a)?`Kiến nghị: ${a.kienNghi || 'Chưa có thông tin'}`:`${a.trangThai==='Chờ phản hồi'?'Hạn phản hồi':'Hạn hoàn thành'}: ${date(a.trangThai==='Chờ phản hồi'?(a.hanPhanHoi||a.hanHoanThanh):a.hanHoanThanh)}`;
      article.append(top,h,el('p',a.chuyenNganh,'meta'),el('p',deadlineText+(C.deadline(a)==='overdue'?' · Quá hạn':''),'deadline '+(C.deadline(a)==='overdue'?'overdue':'')));
      if(draft(a)&&!C.inactive(a))article.append(el('span','Có bản nháp đã lưu','draft-tag'));
      list.append(article);
    }
  }
  function clearDetail() { state.selectionVersion++; state.selected=null; $('detail').replaceChildren(el('p','HỒ SƠ PHẢN BIỆN','eyebrow'),el('h2','Chọn một công việc'),el('p','Mở một bài trong danh sách để xem thông tin.','muted')); }
  function row(dl,label,value) { const r=el('div'); r.append(el('dt',label),el('dd',value??'Chưa có')); dl.append(r); }
  function formatFileSize(bytes) {
    if(!bytes || bytes <= 0) return '';
    if(bytes < 1024) return bytes + ' B';
    if(bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }
  let activeBlobUrl = null;
  let isSplitView = false;
  function restoreEvaluationForm() {
    const form = $('evaluation-form');
    const evalDialog = $('evaluation-dialog');
    if (form && evalDialog && form.parentElement !== evalDialog) {
      evalDialog.append(form);
    }
    const slot = $('manuscript-split-form-slot');
    if (slot) {
      slot.replaceChildren();
      slot.hidden = true;
    }
    $('manuscript-dialog')?.classList.remove('is-split');
    isSplitView = false;
  }
  function closeManuscriptReader() {
    restoreEvaluationForm();
    $('manuscript-dialog')?.close();
    const frame = $('manuscript-frame');
    if(frame) frame.src = 'about:blank';
    if(activeBlobUrl) {
      URL.revokeObjectURL(activeBlobUrl);
      activeBlobUrl = null;
    }
  }
  async function loadDraftIntoForm(a) {
    $('evaluation-title').textContent = a.tieuDeBaiBao;
    $('draft-status').textContent = 'Đang tải bản nháp từ hệ thống…';
    $('confirm-review').checked = false;
    setEvaluationLoading(true);
    updateTotal();
    await draftSaveQueue.catch(() => {});
    let saved = null;
    try {
      saved = await request(`/phanbien/assignments/${a.maPhanCong}/evaluation-draft`);
    } catch(e) {
      if (![404, 405].includes(e.status)) {
        $('draft-status').textContent = `Không tải được bản nháp từ hệ thống: ${e.message}`;
        setEvaluationLoading(false);
        return;
      }
    }
    if (state.selected?.maPhanCong !== a.maPhanCong) return;
    let localOnly = false;
    if (!saved) {
      saved = read(sessionStorage, key('draft', a));
      localOnly = !!saved;
    }
    if (saved) {
      for (const [k, v] of Object.entries(saved)) {
        const input = $('evaluation-form').elements.namedItem(k);
        if (input) input.value = v;
      }
    }
    $('draft-status').textContent = saved
      ? (localOnly ? 'Đã tải bản nháp chỉ lưu trong tab này. Phiếu chưa được gửi.' : 'Đã tải bản nháp đã lưu trên hệ thống. Phiếu chưa được gửi.')
      : 'Bản nháp chưa được gửi.';
    setEvaluationLoading(false);
    updateTotal();
  }
  async function setSplitView(enable, a) {
    isSplitView = enable;
    const dialog = $('manuscript-dialog');
    const slot = $('manuscript-split-form-slot');
    const toggleBtn = $('modal-toggle-split');
    if (enable && C.canEvaluate(a)) {
      dialog.classList.add('is-split');
      slot.hidden = false;
      const form = $('evaluation-form');
      slot.append(form);
      if (toggleBtn) {
        toggleBtn.textContent = 'Toàn màn hình';
        toggleBtn.classList.add('primary');
        toggleBtn.classList.remove('quiet');
      }
      await loadDraftIntoForm(a);
    } else {
      restoreEvaluationForm();
      if (toggleBtn) {
        toggleBtn.textContent = 'Đánh giá song song';
        toggleBtn.classList.remove('primary');
        toggleBtn.classList.add('quiet');
      }
    }
  }
  async function openManuscriptReader(a, startInSplit = false) {
    const dialog = $('manuscript-dialog');
    if(!dialog) return;
    state.selected = a;
    $('manuscript-dialog-title-text').textContent = a.tieuDeBaiBao;
    $('manuscript-dialog-eyebrow').textContent = `BẢN THẢO ẨN DANH · VÒNG ${a.soVong} · BÀI #${a.maBaiBao}`;
    $('manuscript-loading').hidden = false;
    $('manuscript-frame').hidden = true;
    $('manuscript-fallback').hidden = true;

    $('modal-download-manuscript').onclick = (e) => downloadManuscript(a, e.currentTarget);
    const toggleBtn = $('modal-toggle-split');
    if (C.canEvaluate(a)) {
      toggleBtn.hidden = false;
      toggleBtn.onclick = () => setSplitView(!isSplitView, a);
    } else {
      toggleBtn.hidden = true;
    }

    dialog.showModal();

    if (startInSplit && C.canEvaluate(a)) {
      await setSplitView(true, a);
    } else {
      await setSplitView(false, a);
    }

    try {
      const result = await request(`/phanbien/assignments/${a.maPhanCong}/manuscript?inline=true`, { blob: true });
      if(activeBlobUrl) {
        URL.revokeObjectURL(activeBlobUrl);
        activeBlobUrl = null;
      }
      activeBlobUrl = URL.createObjectURL(result.blob);
      const frame = $('manuscript-frame');
      frame.src = activeBlobUrl;
      $('manuscript-loading').hidden = true;
      frame.hidden = false;

      const fallbackLink = $('manuscript-fallback-link');
      fallbackLink.href = activeBlobUrl;
      const ext = result.type === 'application/pdf' ? 'pdf' : result.type.includes('word') ? 'docx' : 'bin';
      fallbackLink.download = `Ban-thao-an-danh-${a.maPhanCong}-vong-${a.soVong}.${ext}`;
    } catch(e) {
      $('manuscript-loading').hidden = true;
      $('manuscript-fallback').hidden = false;
      toast('Không thể tải bản thảo trực tuyến: ' + e.message);
    }
  }
  async function select(a,focus) {
    const version=++state.selectionVersion; state.selected=a; render();
    const panel=$('detail'); panel.replaceChildren(el('p',`BÀI #${a.maBaiBao} · PHÂN CÔNG #${a.maPhanCong}`,'eyebrow'));
    const title=el('h2',a.tieuDeBaiBao); title.id='detail-title'; panel.append(title);
    const dl=el('dl'); row(dl,'Chuyên ngành',a.chuyenNganh); row(dl,'Vòng phản biện',a.soVong); row(dl,'Trạng thái',a.trangThai); row(dl,'Ngày phân công',date(a.ngayPhanCong));
    if(a.trangThai==='Chờ phản hồi')row(dl,'Hạn phản hồi',date(a.hanPhanHoi));
    row(dl,'Hạn hoàn thành',date(a.hanHoanThanh)); panel.append(dl);
    const deadlineState=C.deadline(a);
    if(deadlineState==='overdue' || deadlineState==='due') {
      const deadlineLabel=a.trangThai==='Chờ phản hồi'?'phản hồi lời mời':'hoàn thành đánh giá';
      const deadlineDate=date(a.trangThai==='Chờ phản hồi'?(a.hanPhanHoi||a.hanHoanThanh):a.hanHoanThanh);
      panel.append(el('p',deadlineState==='overdue'
        ? `Đã quá hạn ${deadlineLabel} (${deadlineDate}). Vui lòng liên hệ Ban biên tập nếu cần điều chỉnh hạn.`
        : `Sắp đến hạn ${deadlineLabel} (${deadlineDate}).`,
      `deadline-callout ${deadlineState}`));
    }

    // Scholarly Content: Abstract & Keywords
    if(a.tomTat || a.tomTatTiengAnh || a.tuKhoa) {
      const summarySec = el('section', null, 'paper-summary-section');
      summarySec.append(el('h3', 'Tóm tắt bài báo (Abstract)'));
      if(a.tomTat) summarySec.append(el('p', a.tomTat, 'paper-abstract-text vi'));
      if(a.tomTatTiengAnh) summarySec.append(el('p', a.tomTatTiengAnh, 'paper-abstract-text en'));
      if(a.tuKhoa) {
        const kwWrap = el('div', null, 'paper-keywords-wrap');
        kwWrap.append(el('span', 'Từ khóa: ', 'keywords-label'));
        const tags = a.tuKhoa.split(/[,;]+/).map(s => s.trim()).filter(Boolean);
        const tagContainer = el('div', null, 'keywords-badges');
        tags.forEach(t => tagContainer.append(el('span', t, 'keyword-badge')));
        kwWrap.append(tagContainer);
        summarySec.append(kwWrap);
      }
      panel.append(summarySec);
    }

    if(a.trangThai === 'Chờ phản hồi') {
      const guide = el('div', null, 'invitation-guidance');
      guide.textContent = 'Bạn được mời phản biện bài báo này. Vui lòng đọc tóm tắt nghiên cứu ở trên và phản hồi lời mời. Sau khi nhận phản biện, bạn có thể đọc toàn văn bản thảo và gửi phiếu đánh giá.';
      panel.append(guide);
    }

    // Manuscript Document Card
    if(C.canDownload(a)) {
      const docSec = el('section', null, 'manuscript-file-section');
      docSec.append(el('h3', 'Tài liệu bản thảo ẩn danh'));
      const fileCard = el('div', null, 'manuscript-file-card');
      const fileMain = el('div', null, 'manuscript-file-main');
      const icon = el('div', null, 'manuscript-file-icon');
      icon.innerHTML = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line></svg>';
      const fileInfo = el('div', null, 'manuscript-file-info');
      const fileName = el('strong', a.tenFileAnDanh || `Ban-thao-an-danh-${a.maPhanCong}-vong-${a.soVong}.pdf`, 'manuscript-filename');
      const sizeStr = a.kichThuocFile ? formatFileSize(a.kichThuocFile) : 'Định dạng PDF';
      const fileMeta = el('span', `Bản thảo ẩn danh (Double-Blind) · ${sizeStr}`, 'manuscript-file-sub muted');
      fileInfo.append(fileName, fileMeta);
      fileMain.append(icon, fileInfo);

      const fileBtns = el('div', null, 'manuscript-card-actions');
      const readBtn = button('Đọc trực tuyến', () => openManuscriptReader(a, false), 'primary read-online-button');
      readBtn.setAttribute('title', 'Đọc bản thảo trực tiếp');
      const dlBtn = button('Tải bản thảo ẩn danh', e => downloadManuscript(a, e.currentTarget), 'download-file-btn');
      dlBtn.setAttribute('title', 'Tải tệp bản thảo về máy tính');
      fileBtns.append(readBtn, dlBtn);

      fileCard.append(fileMain, fileBtns);
      docSec.append(fileCard);
      panel.append(docSec);
    }

    const actions=el('div',null,'detail-actions');
    if(a.trangThai==='Chờ phản hồi') {
      actions.append(button('Nhận phản biện',e=>respond(a,true,e.currentTarget),'primary'));
      actions.append(button('Từ chối lời mời',e=>respond(a,false,e.currentTarget)));
    }
    if(C.canEvaluate(a)) {
      const dedicatedBtn = button(draft(a)?'Tiếp tục đánh giá':'Viết đánh giá', () => {
        location.href = `reviewer-evaluation.html?id=${a.maPhanCong}`;
      }, 'primary');
      dedicatedBtn.setAttribute('title', 'Mở không gian đọc bản thảo và viết phiếu BM-04');
      actions.append(
        dedicatedBtn,
        button('Mở phiếu nhanh', () => openEvaluation(a), 'primary'),
        button('Đọc và đánh giá song song',()=>openManuscriptReader(a, true),'primary')
      );
    }
    if(!C.inactive(a)&&Number.isFinite(C.due(a)))actions.append(button('Lưu hạn vào lịch',()=>calendar(a)));
    panel.append(actions);

    if(C.completed(a)) {
      const history=el('section'); history.append(el('h3','Đánh giá đã gửi'),el('p',`Điểm tổng kết: ${a.diemTongKet ?? '—'}/10`),el('p',`Kiến nghị: ${a.kienNghi || 'Chưa có thông tin'}`)); panel.append(history);
      let review=read(sessionStorage,key('receipt',a));
      if(!review) {
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
    b.disabled=true;
    try { const result=await request(`/phanbien/assignments/${a.maPhanCong}/manuscript`,{blob:true}); const ext=result.type==='application/pdf'?'pdf':result.type==='application/msword'?'doc':result.type.includes('wordprocessingml')?'docx':'bin'; download(result.blob,`Ban-thao-an-danh-${a.maPhanCong}-vong-${a.soVong}.${ext}`); } catch(e) { fail(e); } finally { b.disabled=false; }
  }
  async function respond(a,accept,b) {
    b.disabled=true;
    try {
      const result=await request(`/phanbien/assignments/${a.maPhanCong}/respond`,{method:'POST',body:{accept}});
      if(result.success!==true)throw new Error(result.message||'Tòa soạn chưa xác nhận phản hồi.');
      await refresh();
      toast(result.message||'Đã cập nhật lời mời phản biện.');
    } catch(e) { fail(e); }
    finally { b.disabled=false; }
  }
  function calendar(a) {
    const stamp=t=>new Date(t).toISOString().replace(/[-:]/g,'').replace(/\.\d{3}Z$/,'Z');
    const text=['BEGIN:VCALENDAR','VERSION:2.0','PRODID:-//HUIT Journal//Reviewer//VI','BEGIN:VEVENT',`UID:review-${a.maPhanCong}-${a.soVong}@huitjournal.local`,`DTSTAMP:${stamp(Date.now())}`,`DTSTART:${stamp(C.due(a))}`,`SUMMARY:Han phan bien bai ${a.maBaiBao} - vong ${a.soVong}`,'DESCRIPTION:Mo ban lam viec phan bien de xem chi tiet.','END:VEVENT','END:VCALENDAR',''].join('\r\n');
    download(text,`Han-phan-bien-${a.maPhanCong}.ics`,'text/calendar;charset=utf-8');
  }
  function exportReview(a,review) {
    const content=['BẢN GHI ĐÁNH GIÁ CÁ NHÂN',a.tieuDeBaiBao,`Phân công: ${a.maPhanCong} · Vòng: ${a.soVong}`,`Điểm: ${a.diemTongKet??'—'}/10`,`Kiến nghị: ${a.kienNghi||'—'}`,'Nhận xét gửi tác giả:',review?.nhanXetChoTacGia||'Chưa có nội dung phiếu từ tòa soạn.','', 'Bản xuất không bao gồm nhận xét bảo mật.'].join('\n');
    download('\uFEFF'+content,`Danh-gia-${a.maPhanCong}.txt`,'text/plain;charset=utf-8');
  }
  function formData() { const data=Object.fromEntries(new FormData($('evaluation-form'))); data.maPhanCong=state.selected.maPhanCong; return data; }
  function updateTotal() { const d=formData(); $('total-score').textContent=C.scoreKeys.every(k=>d[k]!==''&&Number.isFinite(Number(d[k])))?C.total(d).toFixed(1):'—'; }
  function hasDraftContent(data) { return [...C.scoreKeys,'nhanXetChoTacGia','nhanXetBaoMat','kienNghi'].some(k=>String(data[k]??'').trim()!==''); }
  function setEvaluationLoading(loading) {
    $('evaluation-form').querySelectorAll('input,textarea,select,button').forEach(control=>{
      if(control.id!=='close-evaluation')control.disabled=loading;
    });
    $('evaluation-dialog').setAttribute('aria-busy',String(loading));
  }
  function saveDraft(silent=false) {
    clearTimeout(draftTimer);
    try {
      assertSession(); const a=state.selected; const version=++state.draftVersion; const data=formData();
      const normalized={};
      C.scoreKeys.forEach(k=>normalized[k]=data[k]===''?null:Number(data[k]));
      normalized.nhanXetChoTacGia=data.nhanXetChoTacGia?.trim()||null;
      normalized.nhanXetBaoMat=data.nhanXetBaoMat?.trim()||null;
      normalized.kienNghi=data.kienNghi||null;
      if(!hasDraftContent(data)) {
        sessionStorage.removeItem(key('draft',a));
        $('draft-status').textContent='Đang xóa bản nháp trên hệ thống…';
        draftSaveQueue=draftSaveQueue.catch(()=>{}).then(()=>request(`/phanbien/assignments/${a.maPhanCong}/evaluation-draft`,{method:'DELETE'}))
          .then(()=>{state.drafts.delete(a.maPhanCong); $('draft-status').textContent='Bản nháp đã được xóa khỏi hệ thống.'; render();})
          .catch(e=>{$('draft-status').textContent=`Chưa xóa được bản nháp trên hệ thống: ${e.message}`;});
        return draftSaveQueue;
      }
      sessionStorage.setItem(key('draft',a),JSON.stringify({...normalized,savedAt:new Date().toISOString()}));
      $('draft-status').textContent='Đang đồng bộ bản nháp…';
      draftSaveQueue=draftSaveQueue.catch(()=>{}).then(()=>request(`/phanbien/assignments/${a.maPhanCong}/evaluation-draft`,{method:'PUT',body:normalized}))
        .then(result=>{
          if(version!==state.draftVersion)return;
          state.drafts.set(a.maPhanCong,{savedAt:result.savedAtUtc});
          sessionStorage.removeItem(key('draft',a));
          $('draft-status').textContent=`Đã đồng bộ lên hệ thống · ${new Date(result.savedAtUtc).toLocaleTimeString('vi-VN')}`;
          if(!silent)toast('Bản nháp BM-04 đã được lưu trên hệ thống. Phiếu chưa được gửi.'); render();
        }).catch(e=>{
          if(version===state.draftVersion)$('draft-status').textContent=`Chưa đồng bộ được bản nháp. Nội dung tạm được giữ trong tab này. ${e.message}`;
        });
      return draftSaveQueue;
    } catch(e) { $('draft-status').textContent=`Không lưu được bản nháp: ${e.message}`; return Promise.reject(e); }
  }
  async function openEvaluation(a) {
    if(!C.canEvaluate(a))return;
    restoreEvaluationForm();
    state.selected=a; $('evaluation-form').reset(); $('form-error').textContent='';
    $('confirm-review').checked=false;
    $('evaluation-dialog').showModal();
    await loadDraftIntoForm(a);
  }
  async function submit(event) {
    event.preventDefault(); if($('submit-review').disabled)return;
    const raw=formData(); const error=C.validate(raw); if(error) { $('form-error').textContent=error; return; }
    const a=state.selected; const data={...raw}; C.scoreKeys.forEach(k=>data[k]=Number(data[k])); data.diemTongKet=C.total(data); data.nhanXetChoTacGia=data.nhanXetChoTacGia.trim();
    $('submit-review').disabled=true; $('close-evaluation').disabled=true; $('submit-review').textContent='Đang gửi…'; $('form-error').textContent='';
    try {
      assertSession(); if(!C.canEvaluate(a))throw new Error('Cần nhận lời mời trước khi đánh giá. Vui lòng làm mới.');
      const result=await request('/phanbien/evaluate',{method:'POST',body:data});
      if(result.success!==true)throw new Error(result.message||'Tòa soạn chưa xác nhận phiếu đánh giá.');
      try { sessionStorage.setItem(key('receipt',a),JSON.stringify(data)); } catch {}
      state.items=state.items.map(x=>x.maPhanCong===a.maPhanCong?{...x,daDanhGia:true,trangThai:'Đã đánh giá',diemTongKet:data.diemTongKet,kienNghi:data.kienNghi}:x);
      try { sessionStorage.removeItem(key('draft',a)); state.drafts.delete(a.maPhanCong); } catch {}
      if(isSplitView) closeManuscriptReader();
      else $('evaluation-dialog').close();
      restoreEvaluationForm();
      state.view='history'; setView(); state.selected=state.items.find(x=>x.maPhanCong===a.maPhanCong); render(); await select(state.selected,false);
      toast('Tòa soạn đã xác nhận phiếu đánh giá.');
    } catch(e) { $('form-error').textContent=e.message; if(e.status===401||e.status===403)fail(e); }
    finally { $('submit-review').disabled=false; $('close-evaluation').disabled=false; $('submit-review').textContent='Gửi phiếu đánh giá'; }
  }
  function setView() { document.querySelectorAll('[data-view]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.view===state.view))); $('status-filter').value='all'; state.page=1; }
  function clearSessionDrafts() { try { for(let i=sessionStorage.length-1;i>=0;i--) { const k=sessionStorage.key(i); if(k.startsWith('huit-reviewer:'))sessionStorage.removeItem(k); } } catch {} }
  window.addEventListener('storage',e=>{ if(e.key==='journal_token' || e.key===null) { clearSessionDrafts(); deny('Phiên làm việc đã thay đổi ở tab khác. Vui lòng đăng nhập lại.',true); } });
  $('refresh').addEventListener('click',refresh);
  document.querySelectorAll('[data-view]').forEach(b=>b.addEventListener('click',()=>{state.view=b.dataset.view;setView();render();}));
  ['search','status-filter','sort'].forEach(id=>$(id).addEventListener(id==='search'?'input':'change',()=>{state.page=1;render();}));
  $('prev-page').addEventListener('click',()=>{state.page--;render();}); $('next-page').addEventListener('click',()=>{state.page++;render();});
  $('evaluation-form').addEventListener('input',()=>{updateTotal(); clearTimeout(draftTimer); draftTimer=setTimeout(()=>saveDraft(true),900);});
  $('evaluation-form').addEventListener('submit',submit); $('save-draft').addEventListener('click',()=>saveDraft(false).catch(()=>{}));
  $('close-evaluation').addEventListener('click',()=>{
    saveDraft(true);
    if(isSplitView) setSplitView(false, state.selected);
    else $('evaluation-dialog').close();
    select(state.selected,false);
  });
  $('evaluation-dialog').addEventListener('cancel',e=>{if($('submit-review').disabled)e.preventDefault();else {saveDraft(true);select(state.selected,false);}});
  $('close-manuscript')?.addEventListener('click', closeManuscriptReader);
  $('manuscript-dialog')?.addEventListener('cancel', closeManuscriptReader);
  boot();
})();
