document.addEventListener('DOMContentLoaded', () => {
  const request = document.getElementById('reset-request'), confirm = document.getElementById('reset-confirm'), status = document.getElementById('status');
  let resetId;
  function report(message, error = false) { status.textContent = message; status.dataset.error = String(error); }
  request.addEventListener('submit', async e => {
    e.preventDefault(); const button = request.querySelector('button'); button.disabled = true; report('Đang gửi yêu cầu…');
    try {
      const result = await journalWorkflow.request('password-reset/request', { method: 'POST', body: JSON.stringify({ email: document.getElementById('email').value.trim() }) });
      resetId = result.id; request.hidden = true; confirm.hidden = false; report(result.message); document.getElementById('code').focus();
    } catch (error) { report(error.message, true); } finally { button.disabled = false; }
  });
  confirm.addEventListener('submit', async e => {
    e.preventDefault();
    if (document.getElementById('password').value !== document.getElementById('repeat').value) { report('Hai mật khẩu chưa trùng nhau.', true); return; }
    const button = confirm.querySelector('button'); button.disabled = true;
    try {
      const result = await journalWorkflow.request('password-reset/confirm', { method: 'POST', body: JSON.stringify({ id: resetId, code: document.getElementById('code').value.trim(), password: document.getElementById('password').value }) });
      if (!result.success) throw new Error('Mã không đúng, đã hết hạn hoặc vượt số lần thử. Hãy gửi lại mã.');
      localStorage.removeItem('journal_token'); localStorage.removeItem('journal_user'); confirm.hidden = true; report('Đã cập nhật mật khẩu. Vui lòng đăng nhập lại.');
    } catch (error) { report(error.message, true); } finally { button.disabled = false; }
  });
  document.getElementById('back').onclick = () => { confirm.reset(); confirm.hidden = true; request.hidden = false; report(''); };
});
