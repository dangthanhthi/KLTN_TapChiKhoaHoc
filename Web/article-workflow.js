document.addEventListener('DOMContentLoaded', async () => {
  const id = Number(new URLSearchParams(location.search).get('id')), status = document.getElementById('status');
  let proof, pdfUrl;
  const names = { Submission: 'Nộp hồ sơ', Proof: 'Bản bông', Withdrawal: 'Yêu cầu rút bài', PublicationNotice: 'Thông báo xuất bản' };
  const states = { Pending: 'Chờ xử lý', Submitted: 'Đã nộp', Approved: 'Đã duyệt', Rejected: 'Không duyệt', ChangesRequested: 'Yêu cầu chỉnh sửa', Superseded: 'Đã thay bằng bản mới' };
  function report(text, error = false) { status.textContent = text; status.dataset.error = String(error); }
  async function load() {
    if (!Number.isInteger(id) || id < 1) throw new Error('Mã bản thảo không hợp lệ.');
    const data = await journalWorkflow.request(`article/${id}`);
    document.getElementById('summary').textContent = `Bài #${id} · ${data.trangThai}${data.readOnly ? ' · Chỉ xem hồ sơ này.' : ''}`;
    const records = data.records || [];
    proof = records.find(r => r.kind === 'Proof' && r.state === 'Pending');
    document.getElementById('proof').hidden = !proof;
    document.getElementById('proof-form').hidden = data.readOnly;
    document.getElementById('withdrawal').hidden = data.readOnly || !['Chờ sơ duyệt', 'Chờ sửa hình thức'].includes(data.trangThai) || records.some(r => r.kind === 'Withdrawal' && r.state === 'Pending');
    const files = document.getElementById('files'); files.replaceChildren();
    for (const file of data.files || []) {
      const row = document.createElement('div'); row.className = 'workflow-record';
      const button = document.createElement('button'); button.type = 'button'; button.textContent = 'Tải tệp';
      button.onclick = async () => { button.disabled = true; try { await journalWorkflow.download(`files/${file.id}`, file.name); } catch(e) { report(e.message, true); } finally { button.disabled = false; } };
      const name = document.createElement('p'); name.textContent = `${file.name} · ${Math.ceil(file.size / 1024)} KB`; row.append(name, button); files.append(row);
    }
    if (!files.children.length) files.textContent = 'Chưa có tệp bổ sung. Bản thảo và các bản chỉnh sửa nằm trong chi tiết bài ở trang cá nhân.';
    const history = document.getElementById('records'); history.replaceChildren();
    for (const r of records) {
      const row = document.createElement('div'); row.className = 'workflow-record';
      const title = document.createElement('strong'); title.textContent = `${names[r.kind] || r.kind} · ${states[r.state] || r.state}`;
      const note = document.createElement('p'); note.textContent = [new Date(r.updatedUtc).toLocaleString('vi-VN'), r.data?.Reason, r.data?.Note, r.data?.Text].filter(Boolean).join(' — ');
      row.append(title, note); history.append(row);
    }
  }
  async function action(form, route, payload) {
    const buttons = [...form.querySelectorAll('button')]; buttons.forEach(b => b.disabled = true);
    try { await journalWorkflow.request(route, { method: 'POST', body: JSON.stringify(payload) }); report('Đã lưu phản hồi trên hệ thống.'); await load(); }
    catch(e) { report(e.message, true); } finally { buttons.forEach(b => b.disabled = false); }
  }
  document.getElementById('proof-form').onsubmit = e => {
    e.preventDefault(); const approve = e.submitter?.value === 'approve', note = document.getElementById('proof-note').value.trim();
    if (!approve && !note) { report('Vui lòng nêu lỗi cần sửa.', true); return; }
    if (proof) action(e.target, `proof/${proof.id}/review`, { approve, note });
  };
  document.getElementById('withdrawal-form').onsubmit = e => { e.preventDefault(); action(e.target, `article/${id}/withdrawal`, { reason: document.getElementById('reason').value.trim() }); };
  document.getElementById('read-proof').onclick = async e => {
    e.target.disabled = true;
    try { if (pdfUrl) URL.revokeObjectURL(pdfUrl); pdfUrl = URL.createObjectURL(await journalWorkflow.blob(`proof/${proof.id}/pdf`)); const frame = document.getElementById('proof-pdf'); frame.src = pdfUrl; frame.hidden = false; }
    catch(e) { report(e.message, true); } finally { document.getElementById('read-proof').disabled = false; }
  };
  window.addEventListener('pagehide', () => { if (pdfUrl) URL.revokeObjectURL(pdfUrl); });
  try { await load(); } catch(e) { report(e.message, true); }
});
