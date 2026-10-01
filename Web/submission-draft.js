/* Server draft is isolated by authenticated user; the local copy is a fallback. */
window.submissionDraft = (() => {
  let key, id, draftId, version, queue = Promise.resolve(), stopped = false, signatures = {}, token, conflict = false;
  const status = () => document.getElementById('draft-status');
  const report = message => { if (status()) status().textContent = message; };
  const fields = { Manuscript: 'paper-file-input', Bm02: 'bm02-input', Supplement: 'supplement-input' };
  async function init() {
    token = localStorage.getItem('journal_token');
    if (!token) return;
    const profile = await apiGetProfile();
    if (!profile) throw new Error('Vui lòng đăng nhập lại để lưu bản nháp.');
    key = 'jst_submission_draft:' + profile.maNguoiDung;
    const cached = JSON.parse(localStorage.getItem(key) || 'null');
    id = cached?.submissionId || crypto.randomUUID(); draftId = cached?.draftId || crypto.randomUUID();
    let remote;
    try { remote = await journalWorkflow.request('submission-draft'); }
    catch(e) { report('Chưa kết nối được bản nháp trên hệ thống. Bản trên máy này vẫn được giữ.'); return; }
    if (remote) {
      draftId = remote.id; id = JSON.parse(remote.payload).submissionId || crypto.randomUUID(); version = remote.version;
      localStorage.setItem(key, remote.payload);
      try { for (const kind of Object.keys(fields)) {
        const input = document.getElementById(fields[kind]), transfer = new DataTransfer();
        for (const f of remote.files.filter(f => f.kind.toLowerCase() === kind.toLowerCase())) {
          transfer.items.add(new File([await journalWorkflow.blob('files/' + f.id)], f.name));
        }
        if (transfer.files.length) { input.files = transfer.files; signatures[kind] = signature(input.files); }
      }
      if (document.getElementById('paper-file-input').files.length) handleSmartFileSelected({ target: document.getElementById('paper-file-input') });
      report('Đã khôi phục bản nháp và tệp từ hệ thống.');
      } catch(e) { report('Đã khôi phục nội dung. Có tệp chưa tải được, vui lòng tải lại trang trước khi gửi bài.'); }
    } else {
      const local = JSON.parse(localStorage.getItem(key) || 'null');
      id = local?.submissionId || crypto.randomUUID(); draftId = local?.draftId || crypto.randomUUID(); report('Bản nháp được lưu riêng cho tài khoản này.');
    }
  }
  function signature(files) { return [...files].map(f => `${f.name}:${f.size}:${f.lastModified}`).join('|'); }
  function local(draft) {
    if (!key || stopped) return;
    draft.submissionId = id; draft.draftId = draftId; localStorage.setItem(key, JSON.stringify(draft));
  }
  function save(draft) {
    local(draft);
    if (!key || stopped || conflict) return queue;
    queue = queue.catch(() => {}).then(async () => {
      if (stopped || conflict || localStorage.getItem('journal_token') !== token) return;
      const data = new FormData(); data.append('Id', draftId); data.append('Payload', JSON.stringify(draft));
      if (version) data.append('ExpectedVersion', version);
      const next = {};
      for (const kind of Object.keys(fields)) {
        const input = document.getElementById(fields[kind]), current = signature(input.files);
        if (current !== (signatures[kind] || '')) {
          data.append(kind === 'Supplement' ? 'ReplaceSupplements' : kind === 'Manuscript' ? 'ReplaceManuscript' : 'ReplaceBm02', 'true');
          for (const f of input.files) data.append(kind === 'Supplement' ? 'Supplements' : kind, f);
          next[kind] = current;
        }
      }
      report('Đang lưu bản nháp lên hệ thống…');
      try {
        const result = await journalWorkflow.request('submission-draft', { method: 'POST', body: data });
        version = result.version; Object.assign(signatures, next); report('Bản nháp đã lưu trên hệ thống.');
      } catch(e) {
        if (e.message.includes('máy khác') || e.message.includes('tải lại')) conflict = true;
        report('Chưa lưu lên hệ thống: ' + e.message + ' Nội dung trên máy này vẫn được giữ.');
        throw e;
      }
    });
    return queue;
  }
  return { init, save, local, read: () => key ? localStorage.getItem(key) : null,
    get id() { return id; }, get draftId() { return draftId; }, async pause() { await queue.catch(() => {}); stopped = true; }, resume() { stopped = false; },
    clear() { stopped = true; if (key) localStorage.removeItem(key); } };
})();
