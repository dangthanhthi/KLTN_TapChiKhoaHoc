(() => {
  'use strict';
  const $ = id => document.getElementById(id);
  const fields = ['current', 'new', 'confirm'];
  let busy = false;

  function clearError(name) {
    const input = $(`${name}-password`);
    const error = $(`${name}-error`);
    input.classList.remove('input-has-error');
    input.removeAttribute('aria-invalid');
    error.textContent = '';
    error.hidden = true;
  }

  function showErrors(errors) {
    fields.forEach(clearError);
    const summary = $('error-summary');
    summary.replaceChildren();
    const entries = Object.entries(errors);
    if (!entries.length) { summary.hidden = true; return; }
    entries.forEach(([name, message], index) => {
      const input = $(`${name}-password`);
      const error = $(`${name}-error`);
      input.classList.add('input-has-error');
      input.setAttribute('aria-invalid', 'true');
      error.textContent = message;
      error.hidden = false;
      const line = document.createElement('p');
      line.textContent = `${index + 1}. ${message}`;
      summary.append(line);
    });
    summary.hidden = false;
    $(`${entries[0][0]}-password`).focus();
    summary.scrollIntoView({ block: 'center' });
  }

  function showGeneralError(message) {
    fields.forEach(clearError);
    const summary = $('error-summary');
    summary.textContent = message;
    summary.hidden = false;
    summary.focus();
  }

  function validate(current, next, confirm) {
    const errors = {};
    if (!current) errors.current = 'Vui lòng nhập mật khẩu hiện tại.';
    if (!next) errors.new = 'Vui lòng nhập mật khẩu mới.';
    else if (next.length < 8 || next.length > 100) errors.new = 'Mật khẩu mới phải dài từ 8 đến 100 ký tự.';
    else if (!/[A-Z]/.test(next) || !/[a-z]/.test(next) || !(/[0-9]/.test(next) || /[^A-Za-z0-9]/.test(next))) errors.new = 'Mật khẩu mới cần chữ hoa, chữ thường và ít nhất một chữ số hoặc ký tự đặc biệt.';
    else if (next === current) errors.new = 'Mật khẩu mới không được trùng mật khẩu hiện tại.';
    if (!confirm) errors.confirm = 'Vui lòng nhập lại mật khẩu mới.';
    else if (next !== confirm) errors.confirm = 'Mật khẩu nhập lại chưa trùng khớp.';
    return errors;
  }

  async function submit(event) {
    event.preventDefault();
    if (busy) return;
    const current = $('current-password').value;
    const next = $('new-password').value;
    const confirm = $('confirm-password').value;
    const errors = validate(current, next, confirm);
    showErrors(errors);
    if (Object.keys(errors).length) return;
    busy = true;
    const button = $('submit-password');
    button.disabled = true;
    button.textContent = 'Đang cập nhật…';
    try {
      const result = await apiChangePassword(current, next);
      if (result.success) {
        $('password-form').reset();
        $('password-form').hidden = true;
        $('success-message').hidden = false;
        $('success-message').scrollIntoView({ block: 'center' });
      } else {
        const message = result.message || 'Không thể đổi mật khẩu. Vui lòng kiểm tra lại.';
        if (/mật khẩu hiện tại/i.test(message)) showErrors({ current: message });
        else if (/mật khẩu/i.test(message) && !/phiên đăng nhập/i.test(message)) showErrors({ new: message });
        else showGeneralError(message);
      }
    } catch {
      showGeneralError('Không kết nối được máy chủ tòa soạn. Vui lòng thử lại.');
    } finally {
      busy = false;
      button.disabled = false;
      button.textContent = 'Cập nhật mật khẩu';
    }
  }

  async function init() {
    if (!localStorage.getItem('journal_token')) {
      $('password-form').hidden = true;
      $('access-message').innerHTML = 'Bạn cần <a href="login.html">đăng nhập</a> để đổi mật khẩu.';
      $('access-message').hidden = false;
      return;
    }
    if (typeof apiGetProfile === 'function') {
      const profile = await apiGetProfile();
      if (!profile && !localStorage.getItem('journal_token')) {
        $('password-form').hidden = true;
        $('access-message').innerHTML = 'Phiên đăng nhập đã hết hạn. Vui lòng <a href="login.html">đăng nhập lại</a>.';
        $('access-message').hidden = false;
        return;
      }
    }
    $('password-form').addEventListener('submit', submit);
    fields.forEach(name => $(`${name}-password`).addEventListener('input', () => {
      clearError(name);
      $('error-summary').hidden = true;
    }));
    document.querySelectorAll('.visibility-toggle').forEach(button => button.addEventListener('click', () => {
      const input = $(button.dataset.for);
      const visible = input.type === 'password';
      input.type = visible ? 'text' : 'password';
      button.textContent = visible ? 'Ẩn' : 'Hiện';
      button.setAttribute('aria-label', `${visible ? 'Ẩn' : 'Hiện'} ${input.labels[0]?.textContent.trim() || 'mật khẩu'}`);
    }));
  }

  document.addEventListener('DOMContentLoaded', init);
})();
