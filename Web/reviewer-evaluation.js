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

  const SCORE_INPUT_IDS = ['score-newness', 'score-method', 'score-result', 'score-presentation'];

  function isScoreValid(val) {
    if (val === '' || val == null) return false;
    const num = Number(val);
    if (!Number.isFinite(num) || isNaN(num) || num < 0 || num > 10) return false;
    return Math.abs(num * 10 - Math.round(num * 10)) <= 1e-8;
  }

  function getFormData() {
    const form = $('evaluation-form');
    const fd = new FormData(form);
    const data = Object.fromEntries(fd);
    data.maPhanCong = state.assignmentId;
    return data;
  }

  function updateCharCounters() {
    const authorEl = $('comment-author');
    const authorCounter = $('author-comment-counter');
    if (authorEl && authorCounter) {
      const len = authorEl.value.length;
      authorCounter.textContent = `${len.toLocaleString('vi-VN')} / 20.000 ký tự (tối thiểu 10 ký tự)`;
      if (len > 0 && len < 10) {
        authorCounter.classList.add('has-min-error');
      } else {
        authorCounter.classList.remove('has-min-error');
      }
    }

    const editorEl = $('comment-editor');
    const editorCounter = $('editor-comment-counter');
    if (editorEl && editorCounter) {
      const len = editorEl.value.length;
      editorCounter.textContent = `${len.toLocaleString('vi-VN')} / 20.000 ký tự`;
    }
  }

  function updateConsistencyWarning(totalScore) {
    const warnBox = $('academic-consistency-warning');
    const warnText = $('consistency-warning-text');
    if (!warnBox || !warnText) return '';

    const rec = $('recommendation')?.value;
    let message = '';

    if (totalScore != null && Number.isFinite(totalScore)) {
      if (totalScore < 5.0 && rec === 'Chấp nhận đăng') {
        message = `Lưu ý học thuật: Điểm tổng kết (${totalScore.toFixed(1)}/10) dưới 5.0 thường không phù hợp với kiến nghị Chấp nhận đăng. Vui lòng cân nhắc kỹ.`;
      } else if (totalScore >= 8.5 && rec === 'Từ chối đăng') {
        message = `Lưu ý học thuật: Điểm tổng kết (${totalScore.toFixed(1)}/10) đạt xuất sắc nhưng kiến nghị Từ chối đăng. Vui lòng kiểm tra lại sự phù hợp.`;
      }
    }

    if (message) {
      warnText.textContent = message;
      warnBox.hidden = false;
    } else {
      warnText.textContent = '';
      warnBox.hidden = true;
    }
    return message;
  }

  function updateTotal() {
    const form = $('evaluation-form');
    const inputs = C.scoreKeys.map(k => form.elements.namedItem(k));
    let hasInvalid = false;
    let allFilled = true;

    inputs.forEach(input => {
      const raw = input.value.trim().replace(',', '.');
      const isEmpty = raw === '';
      const valid = !isEmpty && isScoreValid(raw) && input.validity.valid;

      if (!isEmpty && !valid) {
        hasInvalid = true;
        input.setAttribute('aria-invalid', 'true');
        input.classList.add('is-invalid');
        input.title = 'Điểm phải từ 0 đến 10 và tối đa một chữ số thập phân (ví dụ: 1.2, 7.5, 10).';
      } else {
        input.setAttribute('aria-invalid', 'false');
        input.classList.remove('is-invalid');
        input.title = '';
      }

      if (isEmpty) {
        allFilled = false;
      }
    });

    let currentTotal = null;
    if (hasInvalid) {
      $('total-score').textContent = 'Kiểm tra điểm / 10';
      $('total-score').classList.add('is-invalid-total');
    } else if (allFilled) {
      const data = getFormData();
      currentTotal = C.total(data);
      $('total-score').textContent = currentTotal.toFixed(1) + ' / 10';
      $('total-score').classList.remove('is-invalid-total');
    } else {
      $('total-score').textContent = '— / 10';
      $('total-score').classList.remove('is-invalid-total');
    }

    updateConsistencyWarning(currentTotal);
    updateCharCounters();
  }

  function setupScoreInputValidation() {
    SCORE_INPUT_IDS.forEach(id => {
      const input = $(id);
      if (!input) return;

      input.dataset.rawVal = input.value || '';

      input.addEventListener('select', () => {
        input.dataset.allSelected = 'true';
      });
      input.addEventListener('click', () => {
        input.dataset.allSelected = 'false';
      });

      // Real-time keydown filtering
      input.addEventListener('keydown', e => {
        if (e.ctrlKey || e.altKey || e.metaKey) return;
        const allowedKeys = [
          'Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
          'Tab', 'Home', 'End', 'Escape'
        ];
        if (allowedKeys.includes(e.key)) {
          input.dataset.allSelected = 'false';
          if (e.key === 'Backspace' && input.dataset.rawVal) {
            input.dataset.rawVal = input.dataset.rawVal.slice(0, -1);
          }
          return;
        }

        // Enter key finishes input -> trigger blur to format and reset to 0 if invalid
        if (e.key === 'Enter') {
          e.preventDefault();
          input.blur();
          return;
        }

        // Block e, E, +, -
        if (['e', 'E', '+', '-'].includes(e.key)) {
          e.preventDefault();
          return;
        }

        // If whole field was selected (e.g. Ctrl+A or selectText), typing replaces the value
        if (input.dataset.allSelected === 'true') {
          input.dataset.allSelected = 'false';
          if (/^[0-9]$/.test(e.key)) {
            e.preventDefault();
            input.value = e.key;
            input.dataset.rawVal = e.key;
            input.dispatchEvent(new Event('input', { bubbles: true }));
            return;
          }
          if (e.key === '.' || e.key === ',') {
            e.preventDefault();
            input.value = '0.';
            input.dataset.rawVal = '0.';
            input.dispatchEvent(new Event('input', { bubbles: true }));
            return;
          }
        }

        // Handle comma ',' natively as decimal '.'
        if (e.key === ',') {
          e.preventDefault();
          let cur = input.value || '';
          if (cur === '' && input.validity.badInput && input.dataset.rawVal) {
            cur = input.dataset.rawVal;
          }
          if (cur === '') {
            input.value = '0.';
            input.dataset.rawVal = '0.';
            input.dispatchEvent(new Event('input', { bubbles: true }));
            return;
          }
          if (!cur.includes('.')) {
            input.dataset.rawVal = cur + '.';
            document.execCommand('insertText', false, '.');
          }
          return;
        }

        let keyChar = e.key;
        if (!/^[0-9.]$/.test(keyChar)) {
          e.preventDefault();
          return;
        }

        // Use dataset.rawVal if input.value is empty due to badInput (e.g. trailing dot "7.")
        let currentVal = input.value || '';
        if (currentVal === '' && input.validity.badInput && input.dataset.rawVal) {
          currentVal = input.dataset.rawVal;
        }

        // If current value is '0' and user types 1-9: replace '0' with the digit for smooth UX
        if (currentVal === '0' && /^[1-9]$/.test(keyChar)) {
          e.preventDefault();
          input.value = keyChar;
          input.dataset.rawVal = keyChar;
          input.dispatchEvent(new Event('input', { bubbles: true }));
          return;
        }

        // If current value is empty and user types '.', turn into '0.'
        if (currentVal === '' && keyChar === '.') {
          e.preventDefault();
          input.value = '0.';
          input.dataset.rawVal = '0.';
          input.dispatchEvent(new Event('input', { bubbles: true }));
          return;
        }

        // If key is '.' and input already has '.'
        if (keyChar === '.' && currentVal.includes('.')) {
          e.preventDefault();
          return;
        }

        // Check if adding this character exceeds 10 or adds > 1 decimal place:
        // Case 1: Already has 1 decimal place (e.g. "7.5") -> cannot type another digit
        if (/\.[0-9]$/.test(currentVal)) {
          e.preventDefault();
          return;
        }

        // Case 2: Current value is "10"
        if (currentVal === '10') {
          if (keyChar === '.') {
            input.dataset.rawVal = '10.';
            return;
          }
          e.preventDefault();
          return;
        }

        // Case 3: Current value is "10."
        if (currentVal === '10.') {
          if (keyChar === '0') {
            input.dataset.rawVal = '10.0';
            return;
          }
          e.preventDefault();
          return;
        }

        // Case 4: Current value is "10.0"
        if (currentVal === '10.0') {
          e.preventDefault();
          return;
        }

        // Case 5: Current value is single digit 1-9
        if (/^[1-9]$/.test(currentVal) && keyChar !== '.') {
          if (currentVal === '1' && keyChar === '0') {
            input.dataset.rawVal = '10';
            return;
          }
          e.preventDefault();
          return;
        }

        input.dataset.rawVal = currentVal + keyChar;
      });

      // Paste handling: reject invalid formats / numbers out of [0, 10]
      input.addEventListener('paste', e => {
        const text = (e.clipboardData || window.clipboardData)?.getData('text') || '';
        const clean = text.trim().replace(',', '.');
        const num = Number(clean);
        if (!/^(?:10(?:\.0)?|[0-9](?:\.[0-9])?)$/.test(clean) || isNaN(num) || num < 0 || num > 10) {
          e.preventDefault();
        } else {
          input.dataset.rawVal = clean;
        }
      });

      // Blur handling: "khi nhập xong sẽ trở về 0"
      input.addEventListener('blur', () => {
        let raw = input.value.trim().replace(',', '.');
        if (raw === '' && input.validity.badInput && input.dataset.rawVal) {
          raw = input.dataset.rawVal;
        }
        if (raw.endsWith('.')) {
          raw = raw.slice(0, -1);
        }
        if (raw === '' || isNaN(Number(raw))) {
          input.value = '0';
        } else {
          let num = Number(raw);
          if (num < 0 || num > 10) {
            input.value = '0';
          } else {
            num = Math.round(num * 10) / 10;
            input.value = String(num);
          }
        }
        input.dataset.rawVal = input.value;
        input.dispatchEvent(new Event('input', { bubbles: true }));
        updateTotal();
      });

      input.addEventListener('input', () => {
        if (input.value !== '') {
          input.dataset.rawVal = input.value;
        }
      });
    });
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
    const invalidScore = C.scoreKeys.some(k => {
      const input = $('evaluation-form').elements.namedItem(k);
      const val = input.value.trim().replace(',', '.');
      return val !== '' && (!isScoreValid(val) || !input.validity.valid);
    });
    if (invalidScore) {
      saveSessionDraft(data);
      setSyncStatus('error', 'Điểm chưa hợp lệ nên chỉ giữ trong tab này; hãy nhập từ 0 đến 10, tối đa một chữ số thập phân.');
      if (showToast) toast('Chưa đồng bộ nháp vì có điểm ngoài giới hạn hoặc sai bước 0,1.');
      return;
    }
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
    function setDraftValue(form, k, v) {
      const el = form.elements.namedItem(k);
      if (!el || v == null) return;
      if (C.scoreKeys.includes(k)) {
        const num = Number(v);
        if (isNaN(num) || num < 0 || num > 10) el.value = '0';
        else el.value = String(Math.round(num * 10) / 10);
      } else {
        el.value = v;
      }
    }

    try {
      setSyncStatus('saving', 'Đang nạp bản nháp đã lưu…');
      const saved = await request(`/phanbien/assignments/${a.maPhanCong}/evaluation-draft`);
      if (saved && !state.dirty) {
        const form = $('evaluation-form');
        for (const [k, v] of Object.entries(saved)) {
          setDraftValue(form, k, v);
        }
        updateTotal();
        const timeStr = saved.ngayCapNhatUtc ? new Date(saved.ngayCapNhatUtc).toLocaleTimeString('vi-VN') : '';
        setSyncStatus('synced', timeStr ? `Đã nạp bản nháp (${timeStr})` : 'Bản nháp sẵn sàng');
      } else if (!state.dirty) {
        const local = readSessionDraft();
        if (local) {
          const form = $('evaluation-form');
          for (const [k, v] of Object.entries(local)) {
            setDraftValue(form, k, v);
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
            setDraftValue(form, k, v);
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

  function validateDetailedForm(raw) {
    const scoreFields = [
      { key: 'diemTinhMoi', id: 'score-newness', name: '1. Tính mới & Sáng tạo' },
      { key: 'diemPhuongPhap', id: 'score-method', name: '2. Phương pháp nghiên cứu' },
      { key: 'diemKetQua', id: 'score-result', name: '3. Kết quả & Thảo luận' },
      { key: 'diemTrinhBay', id: 'score-presentation', name: '4. Quy chuẩn trình bày' }
    ];

    // Clear previous error styles
    $('form-error-msg').textContent = '';
    const form = $('evaluation-form');
    form.querySelectorAll('.is-invalid').forEach(el => {
      el.classList.remove('is-invalid');
      el.setAttribute('aria-invalid', 'false');
    });

    // Check criteria scores
    for (const sf of scoreFields) {
      const val = raw[sf.key];
      const el = $(sf.id);
      if (val === '' || val == null) {
        if (el) {
          el.classList.add('is-invalid');
          el.setAttribute('aria-invalid', 'true');
          el.focus();
        }
        return `Vui lòng nhập điểm cho tiêu chí: ${sf.name} (thang điểm 0 – 10).`;
      }
      const num = Number(val);
      if (isNaN(num) || num < 0 || num > 10 || Math.abs(num * 10 - Math.round(num * 10)) > 1e-8) {
        if (el) {
          el.classList.add('is-invalid');
          el.setAttribute('aria-invalid', 'true');
          el.focus();
        }
        return `Điểm tiêu chí "${sf.name}" không hợp lệ. Phải từ 0 đến 10, tối đa một chữ số thập phân.`;
      }
    }

    // Check author comments
    const authorComments = raw.nhanXetChoTacGia?.trim() || '';
    const authorEl = $('comment-author');
    if (!authorComments) {
      if (authorEl) {
        authorEl.classList.add('is-invalid');
        authorEl.focus();
      }
      return 'Vui lòng nhập nhận xét chi tiết gửi tác giả.';
    }
    if (authorComments.length < 10) {
      if (authorEl) {
        authorEl.classList.add('is-invalid');
        authorEl.focus();
      }
      return 'Nhận xét gửi tác giả cần tối thiểu 10 ký tự học thuật để nêu rõ ý kiến góp ý.';
    }
    if (authorComments.length > 20000) {
      if (authorEl) {
        authorEl.classList.add('is-invalid');
        authorEl.focus();
      }
      return 'Nhận xét gửi tác giả vượt quá giới hạn 20.000 ký tự.';
    }

    // Check editor comments
    const editorComments = raw.nhanXetBaoMat?.trim() || '';
    const editorEl = $('comment-editor');
    if (editorComments.length > 20000) {
      if (editorEl) {
        editorEl.classList.add('is-invalid');
        editorEl.focus();
      }
      return 'Nhận xét riêng cho Ban biên tập vượt quá 20.000 ký tự.';
    }

    // Check recommendation
    const recEl = $('recommendation');
    if (!raw.kienNghi || !C.recommendations.includes(raw.kienNghi)) {
      if (recEl) {
        recEl.classList.add('is-invalid');
        recEl.focus();
      }
      return 'Vui lòng chọn một kiến nghị xuất bản của chuyên gia.';
    }

    // Check COI
    const coiEl = $('confirm-coi');
    if (!coiEl.checked) {
      coiEl.focus();
      return 'Vui lòng xác nhận không có xung đột lợi ích đối với công trình nghiên cứu trước khi gửi phiếu.';
    }

    return null;
  }

  async function submitEvaluation(event) {
    if (event) event.preventDefault();
    if (state.submitting) return;
    const btnSubmit = $('btn-submit-review');
    const headerSubmit = $('submit-header');
    if (btnSubmit.disabled) return;

    const raw = getFormData();
    const errorMsg = validateDetailedForm(raw);
    if (errorMsg) {
      $('form-error-msg').textContent = errorMsg;
      $('form-error-msg').scrollIntoView({ behavior: 'smooth', block: 'center' });
      return;
    }

    $('form-error-msg').textContent = '';
    const total = C.total(raw);
    $('confirm-score').textContent = total.toFixed(1) + ' / 10';
    $('confirm-recommendation').textContent = raw.kienNghi;
    $('confirm-comment-length').textContent = `${raw.nhanXetChoTacGia.trim().length.toLocaleString('vi-VN')} ký tự`;

    const consistencyMsg = updateConsistencyWarning(total);
    const warnRow = $('confirm-warning-row');
    const warnText = $('confirm-warning-text');
    if (warnRow && warnText) {
      if (consistencyMsg) {
        warnText.textContent = consistencyMsg;
        warnRow.hidden = false;
      } else {
        warnRow.hidden = true;
      }
    }

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
    const errorMsg = validateDetailedForm(raw);
    if (errorMsg) {
      state.submitting = false;
      $('form-error-msg').textContent = errorMsg;
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
      setupScoreInputValidation();
      updateCharCounters();

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

      // Score input & form listeners
      $('evaluation-form').addEventListener('input', e => {
        if (e.target && e.target.classList.contains('is-invalid')) {
          e.target.classList.remove('is-invalid');
          e.target.setAttribute('aria-invalid', 'false');
          $('form-error-msg').textContent = '';
        }
        state.dirty = true;
        state.editVersion++;
        updateTotal();
        clearTimeout(state.draftTimer);
        state.draftTimer = setTimeout(() => { saveDraft(false).catch(() => {}); }, 900);
      });

      $('recommendation').addEventListener('change', () => {
        $('recommendation').classList.remove('is-invalid');
        $('recommendation').setAttribute('aria-invalid', 'false');
        $('form-error-msg').textContent = '';
        updateTotal();
      });

      $('confirm-coi').addEventListener('change', () => {
        $('form-error-msg').textContent = '';
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
