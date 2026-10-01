document.addEventListener('DOMContentLoaded', async () => {
  const status = document.getElementById('status');
  const report = (message, error = false) => { status.textContent = message; status.dataset.error = String(error); };
  const labels = { Contact: 'Liên hệ', Withdrawal: 'Yêu cầu rút', Proof: 'Bản bông' };
  async function load() {
    const rows = await journalWorkflow.request('inbox'), inbox = document.getElementById('inbox'); inbox.replaceChildren();
    for (const r of rows) {
      const data = JSON.parse(r.payload), row = document.createElement('div'); row.className = 'workflow-record';
      const title = document.createElement('strong'); title.textContent = `${labels[r.kind]} · ${r.articleId ? 'Bài #' + r.articleId : data.Email || ''} · ${r.state}`;
      const text = document.createElement('p'); text.textContent = [data.Subject, data.Message, data.Reason, data.Note, data.Reply].filter(Boolean).join(' — '); row.append(title, text);
      if (r.articleId) { const link = document.createElement('a'); link.href = 'article-workflow.html?id=' + r.articleId; link.textContent = 'Xem hồ sơ'; row.append(link); }
      if (r.state === 'Pending' && ['Contact', 'Withdrawal'].includes(r.kind)) {
        const form = document.createElement('form'), note = document.createElement('textarea'); note.minLength = 10; note.maxLength = 2000; note.required = true; note.rows = 3; note.setAttribute('aria-label', 'Nội dung phản hồi');
        const actions = document.createElement('div'); actions.className = 'workflow-actions';
        for (const choice of r.kind === 'Contact' ? ['reply'] : ['approve', 'reject']) { const button = document.createElement('button'); button.value = choice; button.textContent = choice === 'reply' ? 'Gửi phản hồi' : choice === 'approve' ? 'Duyệt rút bài' : 'Không duyệt'; actions.append(button); }
        form.append(note, actions); form.onsubmit = e => { e.preventDefault(); send(form, r.kind === 'Contact' ? `contact/${r.id}/reply` : `withdrawal/${r.id}/review`, r.kind === 'Contact' ? { reason: note.value.trim() } : { approve: e.submitter.value === 'approve', note: note.value.trim() }); }; row.append(form);
      }
      inbox.append(row);
    }
    if (!rows.length) inbox.textContent = 'Chưa có hồ sơ.';
  }
  async function send(form, route, body) {
    const buttons = [...form.querySelectorAll('button')]; buttons.forEach(b => b.disabled = true);
    try { await journalWorkflow.request(route, { method: 'POST', body: body instanceof FormData ? body : JSON.stringify(body) }); report('Đã lưu trên hệ thống.'); await load(); }
    catch(e) { report(e.message, true); } finally { buttons.forEach(b => b.disabled = false); }
  }
  document.getElementById('refresh').onclick = () => load().catch(e => report(e.message, true));
  document.getElementById('screening').onsubmit = e => {
    e.preventDefault(); const data = new FormData(); data.append('SimilarityPercent', document.getElementById('similarity').value); data.append('Approve', document.getElementById('screen-result').value); data.append('Note', document.getElementById('screen-note').value.trim()); data.append('Report', document.getElementById('report').files[0]); send(e.target, `article/${Number(document.getElementById('article').value)}/screening`, data);
  };
  document.getElementById('send-proof').onclick = () => {
    const id = Number(document.getElementById('article').value); if (!Number.isInteger(id) || id < 1) return report('Nhập mã bài hợp lệ.', true);
    send(document.getElementById('screening'), `article/${id}/proof`, undefined);
  };
  document.getElementById('upload-document').onsubmit = async e => {
    e.preventDefault(); const form = e.target, button = form.querySelector('button'), file = document.getElementById('upload-file').files[0];
    const kind = document.getElementById('upload-kind').value, id = Number(document.getElementById('upload-article').value);
    if (kind === 'published-pdf' && !file.name.toLowerCase().endsWith('.pdf')) return report('PDF thành phẩm phải là tệp PDF.', true);
    if (file.size > 30 * 1024 * 1024) return report('Tệp vượt quá 30 MB.', true);
    const data = new FormData(); data.append('file', file); button.disabled = true;
    try {
      const round = Number(document.getElementById('upload-round').value);
      const url = API_BASE + '/baibao/' + id + '/upload-' + kind + (kind === 'anonymous-manuscript' ? '?soVong=' + round : '');
      const response = await fetchWithTimeout(url, { method: 'POST', body: data, headers: { Authorization: 'Bearer ' + localStorage.getItem('journal_token') }, timeout: 120000 });
      const result = await response.json(); if (!response.ok) throw new Error(result.message || 'Không tải được tài liệu.');
      report(result.message || 'Đã tải tài liệu lên hệ thống.'); document.getElementById('article').value = id; form.reset();
    } catch (error) { report(error.message, true); } finally { button.disabled = false; }
  };
  document.getElementById('replace-reviewer').onsubmit = e => {
    e.preventDefault(); send(e.target, 'assignments/' + Number(document.getElementById('replace-assignment').value) + '/replace', {
      reviewerId: Number(document.getElementById('replace-user').value), responseDue: document.getElementById('replace-response').value,
      completionDue: document.getElementById('replace-completion').value, reason: document.getElementById('replace-reason').value.trim()
    });
  };
  document.getElementById('publication').onsubmit = async e => {
    e.preventDefault(); const id = Number(document.getElementById('issue').value), time = document.getElementById('publish-at').value;
    if (e.submitter.value === 'schedule') { if (!time) return report('Chọn thời điểm phát hành.', true); return send(e.target, `issue/${id}/schedule`, { publishAt: time + ':00+07:00' }); }
    const buttons = [...e.target.querySelectorAll('button')]; buttons.forEach(b => b.disabled = true);
    try { const response = await fetchWithTimeout(`${API_BASE}/sotapchi/${id}/publish`, { method: 'POST', headers: { Authorization: `Bearer ${localStorage.getItem('journal_token')}` } }); const result = await response.json(); if (!response.ok) throw new Error(result.message); report(result.message); }
    catch(error) { report(error.message, true); } finally { buttons.forEach(b => b.disabled = false); }
  };
  document.getElementById('notice').onsubmit = e => { e.preventDefault(); send(e.target, `article/${Number(document.getElementById('notice-article').value)}/publication-notice`, { kind: document.getElementById('notice-kind').value, text: document.getElementById('notice-text').value.trim() }); };
  try {
    const user = await apiGetProfile();
    if (!user?.vaiTros?.some(r => ['Quản trị hệ thống', 'Tổng biên tập', 'Ban biên tập'].includes(r))) throw new Error('Trang này dành cho tài khoản tòa soạn.');
    document.getElementById('staff-work').hidden = false;
    document.getElementById('final-work').hidden = !user.vaiTros.some(r => ['Quản trị hệ thống', 'Tổng biên tập'].includes(r)); await load();
  } catch(e) { report(e.message, true); }
});
