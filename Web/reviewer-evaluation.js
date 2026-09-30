/* Dedicated Scholarly Review Workspace Logic
   Adheres to Hallmark anti-AI-slop & Mem0 session retention standards. */
(function () {
  'use strict';

  const C = window.ReviewerCore;
  const $ = id => document.getElementById(id);
  const state = {
    token: null,
    user: null,
    assignmentId: null,
    assignment: null,
    activeBlobUrl: null,
    draftVersion: 0,
    draftTimer: null,
    draftQueue: Promise.resolve(),
    dirty: false,
    submitting: false,
    editVersion: 0
  };

  function apiUrl(path) {
    const isLocalStatic = ['localhost', '127.0.0.1'].includes(location.hostname) && ['8088', '5500', '5501'].includes(location.port);
    const raw = localStorage.getItem('huit_api_url') || (isLocalStatic ? 'http://localhost:5000/api' : '/api');
    const base = new URL(raw, location.href);
    if (!['http:', 'https:'].includes(base.protocol) || (location.protocol === 'https:' && base.protocol !== 'https:')) {
      throw new Error('Địa chỉ dịch vụ không hợp lệ hoặc chưa hỗ trợ HTTPS.');
    }
    return base.href.replace(/\/+$/, '') + (path.startsWith('/') ? path : '/' + path);
  }

  async function request(path, options = {}) {
    if (!state.token || localStorage.getItem('journal_token') !== state.token) {
      const error = new Error('Phiên đăng nhập đã thay đổi. Vui lòng đăng nhập lại.');
      error.status = 401;
      throw error;
    }
    const headers = { ...(options.headers || {}) };
    if (state.token) headers['Authorization'] = 'Bearer ' + state.token;
    if (options.body && !(options.body instanceof FormData) && !headers['Content-Type']) {
      headers['Content-Type'] = 'application/json';
    }
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 15000);
    let res;
    try {
      res = await fetch(apiUrl(path), {
        ...options,
        headers,
        signal: controller.signal,
        cache: 'no-store',
        redirect: 'error',
        body: options.body && !(options.body instanceof FormData) && typeof options.body === 'object'
          ? JSON.stringify(options.body)
          : options.body
      });
      if (localStorage.getItem('journal_token') !== state.token) throw new Error('Phiên đăng nhập đã thay đổi.');
    } catch (e) {
      if (e.name === 'AbortError') throw new Error('Kết nối quá thời gian chờ. Hãy kiểm tra lại trạng thái trước khi gửi tiếp.');
      if (e instanceof TypeError) throw new Error('Không kết nối được máy chủ tòa soạn.');
      throw e;
    } finally { clearTimeout(timer); }

    if (options.blob) {
      if (!res.ok) throw new Error(`Không tải được tệp (mã ${res.status}).`);
      const type = (res.headers.get('content-type') || '').split(';')[0];
      if (!['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'application/msword', 'application/octet-stream'].includes(type)) {
        throw new Error('Phản hồi không phải tệp bản thảo hợp lệ.');
      }
      const blob = await res.blob();
      return { blob, type };
    }

    if (res.status === 204) return null;
    const text = await res.text();
    let data;
    try { data = JSON.parse(text); } catch { data = { message: text }; }
    if (!res.ok) {
      const err = new Error(res.status === 401 ? 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.' : data.message || `Lỗi yêu cầu (mã ${res.status}).`);
      err.status = res.status;
      throw err;
    }
    return data;
  }

  function toast(msg) {
    const n = $('notice');
    if (!n) return;
    n.textContent = msg;
    n.hidden = false;
    clearTimeout(n.timer);
    n.timer = setTimeout(() => { n.hidden = true; }, 4000);
  }

  function setSyncStatus(status, text) {
    const ind = $('sync-indicator');
    const txt = $('sync-text');
    if (!ind || !txt) return;
    ind.classList.remove('is-saving', 'is-error', 'is-synced');
    if (status === 'saving') ind.classList.add('is-saving');
    else if (status === 'error') ind.classList.add('is-error');
    else ind.classList.add('is-synced');
    txt.textContent = text;
  }

  function getFormData() {
    const form = $('evaluation-form');
    const fd = new FormData(form);
    const data = Object.fromEntries(fd);
    data.maPhanCong = state.assignmentId;
    return data;
  }

  function updateTotal() {
    const data = getFormData();
    const isComplete = C.scoreKeys.every(k => data[k] !== '' && data[k] != null && !isNaN(Number(data[k])));
    if (isComplete) {
      const total = C.total(data);
      $('total-score').textContent = total.toFixed(1) + ' / 10';
    } else {
      $('total-score').textContent = '— / 10';
    }
  }

  function formatBytes(bytes) {
    if (!bytes || bytes <= 0) return 'Định dạng PDF';
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  function formatDate(d) {
    if (!d) return '—';
    try {
      const t = C.parseDate ? C.parseDate(d) : Date.parse(d);
      if (isNaN(t)) return d;
      return new Date(t).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    } catch {
      return d;
    }
  }

  function sessionDraftKey() {
    const userId = state.user?.maNguoiDung ?? state.user?.id ?? state.user?.email;
    const a = state.assignment;
    return `huit-reviewer:online:${userId}:draft:${a.maPhanCong}:${a.soVong}`;
  }

  function readSessionDraft() {
    try { return JSON.parse(sessionStorage.getItem(sessionDraftKey())); } catch { return null; }
  }

  function saveSessionDraft(data) {
    try { sessionStorage.setItem(sessionDraftKey(), JSON.stringify({ ...data, savedAt: new Date().toISOString() })); } catch {}
  }

  async function saveDraft(showToast = false) {
    clearTimeout(state.draftTimer);
    const a = state.assignment;
    if (!a || state.submitting) return;
    const version = ++state.draftVersion;
    const editVersion = state.editVersion;
    const data = getFormData();
    const normalized = {};
    C.scoreKeys.forEach(k => normalized[k] = data[k] === '' ? null : Number(data[k]));
    normalized.nhanXetChoTacGia = data.nhanXetChoTacGia?.trim() || null;
    normalized.nhanXetBaoMat = data.nhanXetBaoMat?.trim() || null;
    normalized.kienNghi = data.kienNghi || null;

    setSyncStatus('saving', 'Đang đồng bộ bản nháp…');
    saveSessionDraft(normalized);
    state.draftQueue = state.draftQueue.catch(() => {}).then(async () => {
      const result = await request(`/phanbien/assignments/${a.maPhanCong}/evaluation-draft`, {
        method: 'PUT',
        body: normalized
      });
      if (version === state.draftVersion) {
        try { sessionStorage.removeItem(sessionDraftKey()); } catch {}
        const timeStr = new Date(result?.savedAtUtc || Date.now()).toLocaleTimeString('vi-VN');
        setSyncStatus('synced', `Đã lưu nháp tự động (${timeStr})`);
        if (editVersion === state.editVersion) state.dirty = false;
        if (showToast) toast('Bản nháp đã được lưu thành công trên máy chủ.');
      }
    }).catch(e => {
      if (version === state.draftVersion) {
        if ([404, 405].includes(e.status)) {
          if (editVersion === state.editVersion) state.dirty = false;
          setSyncStatus('synced', 'Nháp chỉ lưu trong tab này; chưa đồng bộ lên tòa soạn');
          if (showToast) toast('Đã lưu nháp trong tab này. Đừng đóng tab trước khi gửi phiếu.');
          return;
        }
        state.dirty = true;
        setSyncStatus('error', `Chưa lưu được nháp lên hệ thống; nội dung còn trong tab này. ${e.message}`);
      }
      throw e;
    });
    return state.draftQueue;
  }

  async function loadDraft() {
    const a = state.assignment;
    if (!a) return;
    try {
      setSyncStatus('saving', 'Đang nạp bản nháp đã lưu…');
      const saved = await request(`/phanbien/assignments/${a.maPhanCong}/evaluation-draft`);
      if (saved && !state.dirty) {
        const form = $('evaluation-form');
        for (const [k, v] of Object.entries(saved)) {
          const el = form.elements.namedItem(k);
          if (el && v != null) el.value = v;
        }
        updateTotal();
        const timeStr = saved.ngayCapNhatUtc ? new Date(saved.ngayCapNhatUtc).toLocaleTimeString('vi-VN') : '';
        setSyncStatus('synced', timeStr ? `Đã nạp bản nháp (${timeStr})` : 'Bản nháp sẵn sàng');
      } else if (!state.dirty) {
        const local = readSessionDraft();
        if (local) {
          const form = $('evaluation-form');
          for (const [k, v] of Object.entries(local)) {
            const el = form.elements.namedItem(k);
            if (el && v != null) el.value = v;
          }
          updateTotal();
          setSyncStatus('synced', 'Đã nạp nháp lưu trong tab này; chưa đồng bộ lên tòa soạn');
          return;
        }
        setSyncStatus('synced', 'Chưa có bản nháp nào được lưu');
      }
    } catch (e) {
      if (![404, 405].includes(e.status)) {
        setSyncStatus('error', 'Không tải được bản nháp cũ');
      } else {
        const local = readSessionDraft();
        if (local && !state.dirty) {
          const form = $('evaluation-form');
          for (const [k, v] of Object.entries(local)) {
            const el = form.elements.namedItem(k);
            if (el && v != null) el.value = v;
          }
          updateTotal();
        }
        setSyncStatus('synced', local ? 'Đã nạp nháp lưu trong tab này; chưa đồng bộ lên tòa soạn' : 'Chưa có bản nháp nào được lưu');
      }
    }
  }

  async function loadManuscriptPdf() {
    const a = state.assignment;
    if (!a) return;
    $('pdf-loading').hidden = false;
    $('pdf-frame').hidden = true;
    $('pdf-fallback').hidden = true;

    try {
      const result = await request(`/phanbien/assignments/${a.maPhanCong}/manuscript?inline=true`, { blob: true });
      if (state.activeBlobUrl) {
        URL.revokeObjectURL(state.activeBlobUrl);
        state.activeBlobUrl = null;
      }
      state.activeBlobUrl = URL.createObjectURL(result.blob);
      const frame = $('pdf-frame');
      $('pdf-loading').hidden = true;
      const isPdf = result.type === 'application/pdf' || (result.type === 'application/octet-stream' && /\.pdf$/i.test(a.tenFileAnDanh || ''));
      if (isPdf) {
        frame.src = state.activeBlobUrl;
        frame.hidden = false;
      } else {
        $('pdf-fallback').hidden = false;
        $('pdf-fallback-text').textContent = 'Định dạng bản thảo này không xem trực tiếp được. Hãy tải tệp để đọc.';
      }

      const fallbackLink = $('pdf-fallback-link');
      fallbackLink.href = state.activeBlobUrl;
      fallbackLink.hidden = false;
      const ext = isPdf ? 'pdf' : result.type.includes('wordprocessingml') ? 'docx' : result.type === 'application/msword' ? 'doc' : 'bin';
      fallbackLink.download = `Ban-thao-an-danh-${a.maPhanCong}-vong-${a.soVong}.${ext}`;
    } catch (e) {
      $('pdf-loading').hidden = true;
      $('pdf-fallback').hidden = false;
      $('pdf-fallback-text').textContent = 'Không tải được bản thảo. Chọn Tải tệp để thử lại.';
      $('pdf-fallback-link').hidden = true;
      toast('Không thể tải tệp bản thảo trực tuyến: ' + e.message);
    }
  }

  async function downloadCurrentPdf() {
    const a = state.assignment;
    if (!a) return;
    try {
      if (!state.activeBlobUrl) await loadManuscriptPdf();
      if (!state.activeBlobUrl) return;
      const link = document.createElement('a');
      link.href = state.activeBlobUrl;
      link.download = a.tenFileAnDanh || `Ban-thao-an-danh-${a.maPhanCong}-vong-${a.soVong}.pdf`;
      document.body.append(link);
      link.click();
      link.remove();
    } catch (e) { toast(e.message); }
  }

  async function submitEvaluation(event) {
    if (event) event.preventDefault();
    if (state.submitting) return;
    const btnSubmit = $('btn-submit-review');
    const headerSubmit = $('submit-header');
    if (btnSubmit.disabled) return;

    const raw = getFormData();
    const err = C.validate(raw);
    if (err) {
      $('form-error-msg').textContent = err;
      $('form-error-msg').scrollIntoView({ behavior: 'smooth', block: 'center' });
      return;
    }
    if (!$('confirm-coi').checked) {
      $('form-error-msg').textContent = 'Vui lòng xác nhận không có xung đột lợi ích trước khi gửi phiếu.';
      $('confirm-coi').focus();
      return;
    }
    $('form-error-msg').textContent = '';
    $('confirm-score').textContent = C.total(raw).toFixed(1) + ' / 10';
    $('confirm-recommendation').textContent = raw.kienNghi;
    $('submit-confirmation').showModal();
  }

  async function confirmSubmission() {
    if (state.submitting) return;
    $('submit-confirmation').close();
    state.submitting = true;
    clearTimeout(state.draftTimer);
    const btnSubmit = $('btn-submit-review');
    const headerSubmit = $('submit-header');
    const raw = getFormData();
    const err = C.validate(raw);
    if (err || !$('confirm-coi').checked) {
      state.submitting = false;
      $('form-error-msg').textContent = err || 'Vui lòng xác nhận không có xung đột lợi ích.';
      return;
    }

    const a = state.assignment;
    const data = { ...raw };
    C.scoreKeys.forEach(k => data[k] = Number(data[k]));
    data.diemTongKet = C.total(data);
    data.nhanXetChoTacGia = data.nhanXetChoTacGia.trim();

    btnSubmit.disabled = true;
    headerSubmit.disabled = true;
    btnSubmit.textContent = 'Đang gửi phiếu…';
    headerSubmit.textContent = 'Đang gửi…';
    $('form-error-msg').textContent = '';

    try {
      await state.draftQueue.catch(() => {});
      const result = await request('/phanbien/evaluate', { method: 'POST', body: data });
      if (result.success !== true) throw new Error(result.message || 'Tòa soạn chưa xác nhận phiếu đánh giá.');
      state.dirty = false;

      try { sessionStorage.removeItem(sessionDraftKey()); } catch {}

      try {
        sessionStorage.setItem(`huit-reviewer:receipt:${a.maPhanCong}`, JSON.stringify(data));
      } catch {}

      toast('Tòa soạn đã tiếp nhận phiếu đánh giá BM-04 thành công!');
      setTimeout(() => {
        location.href = 'reviewer.html?submitted=' + a.maPhanCong;
      }, 1200);
    } catch (e) {
      $('form-error-msg').textContent = e.message;
      state.submitting = false;
      state.dirty = true;
      btnSubmit.disabled = false;
      headerSubmit.disabled = false;
      btnSubmit.textContent = 'Gửi phiếu đánh giá chính thức';
      headerSubmit.textContent = 'Gửi phiếu đánh giá';
    }
  }

  function setupTabs() {
    const tabPdf = $('tab-pdf');
    const tabAbs = $('tab-abstract');
    const panelPdf = $('pdf-view-panel');
    const panelAbs = $('abstract-view-panel');

    tabPdf.onclick = () => {
      tabPdf.classList.add('is-active');
      tabPdf.setAttribute('aria-selected', 'true');
      tabAbs.classList.remove('is-active');
      tabAbs.setAttribute('aria-selected', 'false');
      panelPdf.classList.add('is-active');
      panelPdf.hidden = false;
      panelAbs.classList.remove('is-active');
      panelAbs.hidden = true;
    };

    tabAbs.onclick = () => {
      tabAbs.classList.add('is-active');
      tabAbs.setAttribute('aria-selected', 'true');
      tabPdf.classList.remove('is-active');
      tabPdf.setAttribute('aria-selected', 'false');
      panelAbs.classList.add('is-active');
      panelAbs.hidden = false;
      panelPdf.classList.remove('is-active');
      panelPdf.hidden = true;
    };
  }

  async function init() {
    state.token = localStorage.getItem('journal_token');
    const userJson = localStorage.getItem('journal_user');
    try { state.user = userJson ? JSON.parse(userJson) : null; } catch { state.user = null; }

    if (!state.token || !C.hasRole(state.user)) {
      const params = new URLSearchParams(location.search);
      const requestedId = Number(params.get('id') || params.get('maPhanCong'));
      const next = Number.isSafeInteger(requestedId) && requestedId > 0
        ? `reviewer-evaluation.html?id=${requestedId}`
        : 'reviewer.html';
      location.href = `login.html?next=${encodeURIComponent(next)}`;
      return;
    }

    const params = new URLSearchParams(location.search);
    const id = Number(params.get('id') || params.get('maPhanCong'));
    if (!id || isNaN(id)) {
      alert('Không tìm thấy mã phân công đánh giá. Hệ thống sẽ quay lại Bàn làm việc.');
      location.href = 'reviewer.html';
      return;
    }
    state.assignmentId = id;

    // Fetch assignments list
    try {
      const profile = await request('/auth/profile');
      if (!C.hasRole(profile)) throw new Error('Tài khoản chưa có vai trò phản biện.');
      state.user = profile;
      const list = await request('/phanbien/my-assignments');
      const a = Array.isArray(list) ? list.find(x => x.maPhanCong === id) : null;
      if (!a) {
        alert('Phân công đánh giá không tồn tại hoặc đã bị hủy.');
        location.href = 'reviewer.html';
        return;
      }
      if (!C.canEvaluate(a)) {
        alert('Phân công này chưa ở trạng thái có thể đánh giá.');
        location.href = 'reviewer.html';
        return;
      }
      state.assignment = a;

      // Render Header
      $('paper-badge').textContent = `BÀI #${a.maBaiBao} · VÒNG ${a.soVong}`;
      $('paper-title').textContent = a.tieuDeBaiBao;
      document.title = `Đánh giá bài #${a.maBaiBao} · ${a.tieuDeBaiBao} · HUIT Journal`;

      // Deadline
      const isOverdue = C.deadline(a) === 'overdue';
      const deadlineText = `Hạn hoàn thành: ${formatDate(a.hanHoanThanh)}${isOverdue ? ' (Quá hạn)' : ''}`;
      $('deadline-pill').textContent = deadlineText;
      if (isOverdue) $('deadline-pill').classList.add('overdue');

      // Abstract Panel Metadata
      $('meta-discipline').textContent = a.chuyenNganh || 'Chưa phân loại';
      $('meta-assigned-date').textContent = formatDate(a.ngayPhanCong);
      $('meta-deadline').textContent = formatDate(a.hanHoanThanh);

      // Abstract text
      $('abstract-vi').textContent = a.tomTat || 'Chưa có bản tóm tắt tiếng Việt.';
      if (a.tomTatTiengAnh) {
        $('abstract-en').textContent = a.tomTatTiengAnh;
        $('abstract-en').hidden = false;
      } else {
        $('abstract-en').hidden = true;
      }

      // Keywords
      const kwContainer = $('keywords-container');
      kwContainer.replaceChildren();
      if (a.tuKhoa) {
        const tags = a.tuKhoa.split(/[,;]+/).map(s => s.trim()).filter(Boolean);
        tags.forEach(t => {
          const badge = document.createElement('span');
          badge.className = 'keyword-badge';
          badge.textContent = t;
          kwContainer.append(badge);
        });
      } else {
        kwContainer.textContent = 'Chưa có từ khóa.';
      }

      // File size tag
      $('file-size-tag').textContent = `Bản thảo ẩn danh · ${formatBytes(a.kichThuocFile)}`;

      // Setup tabs & events
      setupTabs();
      $('btn-download-pdf').onclick = downloadCurrentPdf;
      $('save-draft-header').onclick = () => saveDraft(true).catch(() => {});
      $('btn-save-draft').onclick = () => saveDraft(true).catch(() => {});
      $('submit-header').onclick = () => $('evaluation-form').requestSubmit();
      $('evaluation-form').onsubmit = submitEvaluation;
      $('confirm-send').onclick = confirmSubmission;
      $('cancel-send').onclick = () => $('submit-confirmation').close();
      $('back-link').addEventListener('click', async event => {
        if (!state.dirty && !state.draftTimer) return;
        event.preventDefault();
        try {
          await saveDraft();
          location.href = 'reviewer.html';
        } catch { toast('Bản nháp chưa lưu được. Vui lòng thử lại trước khi rời trang.'); }
      });
      window.addEventListener('beforeunload', event => {
        if (!state.dirty || state.submitting) return;
        event.preventDefault();
        event.returnValue = '';
      });

      // Score input listeners
      $('evaluation-form').addEventListener('input', () => {
        state.dirty = true;
        state.editVersion++;
        updateTotal();
        clearTimeout(state.draftTimer);
        state.draftTimer = setTimeout(() => { saveDraft(false).catch(() => {}); }, 900);
      });

      // Load PDF and Draft
      loadManuscriptPdf();
      loadDraft();

    } catch (e) {
      alert('Không thể kết nối đến máy chủ tòa soạn: ' + e.message);
      location.href = 'reviewer.html';
    }
  }

  window.addEventListener('DOMContentLoaded', init);
})();
