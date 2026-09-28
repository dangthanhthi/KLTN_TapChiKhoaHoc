/**
 * Scientific Journal System - Global Interaction & Modal Script
 * Handles:
 * - Login & Registration Modals
 * - BibTeX / RIS / APA Citation Generator & One-Click Copy
 * - Full-Text PDF Preview & Download Simulator
 * - About / Policy / Ethics / Contact Dialogs
 * - Newsletter Subscriptions
 * - Global Search
 */

document.addEventListener('DOMContentLoaded', () => {
  initUserSession();
  initModals();
  initAuthTriggers();
  initCitationTriggers();
  initDownloadTriggers();
  initInfoTriggers();
  initNewsletterTriggers();
  initSearchTriggers();
});

// =========================================================================
// TIỆN ÍCH AN TOÀN - CHỐNG XSS & HTML INJECTION (Anti-XSS Sanitizer)
// =========================================================================
function escapeHtml(str) {
  if (str === null || str === undefined) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
if (typeof window !== 'undefined') {
  window.escapeHtml = escapeHtml;
}

// =========================================================================
// API CLIENT - KẾT NỐI BACKEND ASP.NET CORE WEB API (.NET 9)
// =========================================================================
const CUSTOM_API_URL = (typeof localStorage !== 'undefined') ? localStorage.getItem('huit_api_url') : null;
const CONFIGURED_API_URL = window.HUIT_JOURNAL_API_URL || CUSTOM_API_URL;
const IS_LOCAL_WEB = ['localhost', '127.0.0.1'].includes(window.location.hostname);
const IS_LOCAL_STATIC_SERVER = IS_LOCAL_WEB && ['8088', '5500', '5501'].includes(window.location.port);
const API_BASE = CONFIGURED_API_URL
  ? CONFIGURED_API_URL.replace(/\/$/, '')
  : (IS_LOCAL_STATIC_SERVER ? 'http://localhost:5000/api' : '/api');

async function apiError(res, fallback) {
  const data = await res.json().catch(() => null);
  return { success: false, message: data?.message || `${fallback} (HTTP ${res.status}).` };
}

const API_UNAVAILABLE_MESSAGE = 'Không kết nối được máy chủ tòa soạn. Dữ liệu chưa được lưu; vui lòng thử lại.';

async function fetchWithTimeout(resource, options = {}) {
  const { timeout = 15000, ...restOptions } = options;
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeout);
  try {
    const response = await fetch(resource, {
      ...restOptions,
      signal: controller.signal
    });
    clearTimeout(id);
    return response;
  } catch (error) {
    clearTimeout(id);
    throw error;
  }
}

async function apiLogin(usernameOrEmail, password) {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ usernameOrEmail, password })
    });
    const json = await res.json().catch(() => null);
    if (res.ok && json && json.success) {
      return json;
    }
    return {
      success: false,
      message: json?.message || 'Tài khoản hoặc mật khẩu không chính xác.'
    };
  } catch (e) {
    return {
      success: false,
      message: IS_LOCAL_STATIC_SERVER
        ? 'Không thể kết nối Backend API tại http://localhost:5000. Vui lòng khởi động dịch vụ Backend.'
        : API_UNAVAILABLE_MESSAGE
    };
  }
}

function maskEmailAddress(email) {
  if (!email || !email.includes('@')) return email || '';
  const [user, domain] = email.split('@');
  if (user.length <= 2) {
    return user[0] + '***@' + domain;
  }
  return user[0] + '***' + user[user.length - 1] + '@' + domain;
}

async function apiRegister(data) {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    const json = await res.json().catch(() => null);
    if ((res.status === 202 || res.ok) && json && json.success) {
      return json;
    }
    
    let errMsg = json?.message;
    const fieldErrors = json?.errors || {};

    if (!errMsg && fieldErrors && typeof fieldErrors === 'object' && Object.keys(fieldErrors).length > 0) {
      const errorStrings = [];
      for (const [field, errs] of Object.entries(fieldErrors)) {
        const msgs = Array.isArray(errs) ? errs : [errs];
        msgs.forEach(m => { if (m) errorStrings.push(m); });
      }
      if (errorStrings.length > 0) {
        errMsg = errorStrings.join('. ');
      }
    }

    if (!errMsg) {
      errMsg = json?.title || `Không thể đăng ký tài khoản (Mã lỗi HTTP ${res.status}). Vui lòng kiểm tra lại các trường được thông báo.`;
    }

    return {
      success: false,
      message: errMsg,
      errors: fieldErrors
    };
  } catch (e) {
    return {
      success: false,
      message: 'Không thể kết nối đến máy chủ Tòa soạn để ghi nhận đăng ký. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau.'
    };
  }
}

async function apiVerifyEmail(registrationId, verificationCode) {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/verify-email`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        registrationId: String(registrationId).trim(),
        verificationCode: String(verificationCode).trim()
      })
    });
    const json = await res.json().catch(() => null);
    if (res.ok && json && json.success) {
      if (json.token) {
        localStorage.setItem('journal_token', json.token);
        if (json.user) {
          localStorage.setItem('journal_user', JSON.stringify(json.user));
        }
      }
      return json;
    }
    return {
      success: false,
      message: json?.message || 'Mã xác thực không chính xác hoặc đã hết hiệu lực.',
      remainingAttempts: json?.remainingAttempts
    };
  } catch (e) {
    return {
      success: false,
      message: 'Không thể kết nối đến máy chủ Tòa soạn để xác thực email. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau.'
    };
  }
}

async function apiResendVerification(registrationId) {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/resend-verification`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        registrationId: String(registrationId).trim()
      })
    });
    const json = await res.json().catch(() => null);
    if (res.ok && json && json.success) {
      return json;
    }
    return {
      success: false,
      message: json?.message || 'Chưa thể gửi lại mã xác nhận lúc này.',
      resendAfterSeconds: json?.resendAfterSeconds
    };
  } catch (e) {
    return {
      success: false,
      message: 'Không thể kết nối đến máy chủ Tòa soạn để gửi lại mã. Vui lòng thử lại sau.'
    };
  }
}

async function apiGetProfile() {
  const token = localStorage.getItem('journal_token');
  if (!token) return null;

  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/profile`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.status === 401) {
      localStorage.removeItem('journal_token');
      localStorage.removeItem('journal_user');
      return null;
    }
    if (res.ok) {
      return await res.json();
    }
  } catch (e) {
    return null;
  }
  return null;
}

async function apiUpdateProfile(data) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Vui lòng đăng nhập lại vào hệ thống.' };
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/profile`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(data)
    });
    if (res.ok) return await res.json();
    return apiError(res, 'Không thể cập nhật hồ sơ');
  } catch (e) {
    return { success: false, message: API_UNAVAILABLE_MESSAGE };
  }
}

function getAvatarUrl(path) {
  if (!path) return '';
  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('data:')) {
    return path;
  }
  const host = new URL(API_BASE, window.location.href).origin;
  return host + (path.startsWith('/') ? path : ('/' + path));
}

async function apiUploadAvatar(file) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Chưa đăng nhập hệ thống.' };

  try {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetchWithTimeout(`${API_BASE}/auth/upload-avatar`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` },
      body: formData
    });
    if (res.ok) return await res.json();
    return apiError(res, 'Không thể cập nhật ảnh đại diện');
  } catch (e) {
    return { success: false, message: API_UNAVAILABLE_MESSAGE };
  }
}

async function apiDeleteAvatar() {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Chưa đăng nhập hệ thống.' };

  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/avatar`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.ok) return await res.json();
    return apiError(res, 'Không thể xóa ảnh đại diện');
  } catch (e) {
    return { success: false, message: API_UNAVAILABLE_MESSAGE };
  }
}

async function apiChangePassword(currentPassword, newPassword) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Vui lòng đăng nhập lại vào hệ thống.' };
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/change-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ currentPassword, newPassword })
    });
    const data = await res.json().catch(() => ({}));
    if (res.ok) return { success: data.success === true, message: data.message || 'Đổi mật khẩu thành công.' };
    return { success: false, message: data.message || `Không thể đổi mật khẩu (HTTP ${res.status}).` };
  } catch (e) {
    return { success: false, message: API_UNAVAILABLE_MESSAGE };
  }
}

async function apiLookupUser(email) {
  if (!email) return null;
  const clean = email.trim().toLowerCase();
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/lookup?email=${encodeURIComponent(clean)}`);
    if (res.ok) return await res.json();
  } catch (e) {}
  return null;
}

// -------------------------------------------------------------
// [REMOVED] API MODULE YÊU CẦU NÂNG CẤP VAI TRÒ (REVIEWER ROLE UPGRADE)
// Vai trò Phản biện viên: do Tổng biên tập phân công qua hệ thống WinForms
// -------------------------------------------------------------


// -------------------------------------------------------------
// API MODULE BÀI BÁO (SUBMISSIONS & ARTICLES)
// -------------------------------------------------------------
async function apiSubmitPaper(formData) {
  const token = localStorage.getItem('journal_token');
  if (!token) {
    return { success: false, message: 'Vui lòng đăng nhập lại vào hệ thống trước khi nộp bài.' };
  }
  try {
    const res = await fetchWithTimeout(`${API_BASE}/baibao/submit`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      },
      body: formData
    });
    if (res.ok) {
      return await res.json();
    }
    const errData = await res.json().catch(() => null);
    return {
      success: false,
      message: errData?.message || `Máy chủ từ chối tiếp nhận bản thảo (Mã lỗi HTTP ${res.status}). Vui lòng kiểm tra lại dữ liệu và tệp đính kèm.`
    };
  } catch (e) {
    return {
      success: false,
      message: 'Không thể kết nối đến máy chủ tòa soạn. Bản thảo CHƯA được nộp lên hệ thống. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau.'
    };
  }
}

async function apiGetMySubmissions() {
  const token = localStorage.getItem('journal_token');
  if (!token) throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  const res = await fetchWithTimeout(`${API_BASE}/baibao/my-submissions`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  if (!res.ok) throw new Error((await apiError(res, 'Không thể tải danh sách bản thảo')).message);
  const data = await res.json();
  if (!Array.isArray(data)) throw new Error('Danh sách bản thảo trả về không đúng định dạng.');
  return data;
}

async function apiGetSubmissionDetail(id) {
  const token = localStorage.getItem('journal_token');
  if (!token) throw new Error('Vui lòng đăng nhập lại vào hệ thống.');
  const res = await fetchWithTimeout(`${API_BASE}/baibao/${id}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  if (!res.ok) throw new Error((await apiError(res, 'Không thể tải hồ sơ bản thảo')).message);
  return await res.json();
}

async function apiGetPublicArticle(id) {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/baibao/public/${id}`);
    if (!res.ok) return null;
    return await res.json();
  } catch (e) {
    return null;
  }
}

async function apiGetLatestArticles(limit = 10) {
  try {
    const requestLatest = async requestedLimit => {
      const res = await fetchWithTimeout(`${API_BASE}/baibao/public/latest?limit=${requestedLimit}`);
      if (!res.ok) return [];
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    };

    const articles = await requestLatest(limit);
    // Older deployed APIs may interpret limit=0 as Take(0), while newer APIs
    // use it to mean the complete published archive. Retry with a bounded
    // compatibility limit so the public archive keeps working during rollout.
    if (Number(limit) === 0 && articles.length === 0) {
      return await requestLatest(1000);
    }
    return articles;
  } catch (e) {
    return [];
  }
}

// -------------------------------------------------------------
// API MODULE SỐ TẠP CHÍ & LƯU TRỮ (ARCHIVES & ISSUES)
// -------------------------------------------------------------
async function apiGetPublishedIssues() {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/sotapchi`);
    if (!res.ok) return [];
    return await res.json();
  } catch (e) {
    return [];
  }
}

async function apiGetIssueDetail(id) {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/sotapchi/${id}`);
    if (!res.ok) return null;
    return await res.json();
  } catch (e) {
    return null;
  }
}

async function apiGetCategories() {
  try {
    const res = await fetchWithTimeout(`${API_BASE}/chuyennganh`);
    if (!res.ok) return [];
    return await res.json();
  } catch (e) {
    return [];
  }
}

// -------------------------------------------------------------
// API MODULE PHẢN BIỆN (PEER REVIEW WORKFLOW)
// -------------------------------------------------------------
async function apiAssignReviewer(data) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Vui lòng đăng nhập lại vào hệ thống.' };
  try {
    const res = await fetchWithTimeout(`${API_BASE}/phanbien/assign`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(data)
    });
    if (res.ok) return await res.json();
    return apiError(res, 'Không thể phân công phản biện');
  } catch (e) {
    return { success: false, message: API_UNAVAILABLE_MESSAGE };
  }
}

async function apiGetMyAssignments() {
  const token = localStorage.getItem('journal_token');
  if (!token) throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  const res = await fetchWithTimeout(`${API_BASE}/phanbien/my-assignments`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  if (!res.ok) throw new Error((await apiError(res, 'Không thể tải nhiệm vụ phản biện')).message);
  const data = await res.json();
  if (!Array.isArray(data)) throw new Error('Danh sách nhiệm vụ trả về không đúng định dạng.');
  return data;
}

async function apiSubmitEvaluation(data) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Vui lòng đăng nhập lại vào hệ thống.' };
  try {
    const res = await fetchWithTimeout(`${API_BASE}/phanbien/evaluate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(data)
    });
    if (res.ok) return await res.json();
    return apiError(res, 'Không thể nộp phiếu BM-04');
  } catch (e) {
    return { success: false, message: API_UNAVAILABLE_MESSAGE };
  }
}

async function apiMakeDecision(data) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Vui lòng đăng nhập lại vào hệ thống.' };
  try {
    const res = await fetchWithTimeout(`${API_BASE}/phanbien/decision`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(data)
    });
    if (res.ok) return await res.json();
    return apiError(res, 'Không thể lưu quyết định biên tập');
  } catch (e) {
    return { success: false, message: API_UNAVAILABLE_MESSAGE };
  }
}

async function apiResubmitPaper(baiBaoId, formData) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Vui lòng đăng nhập lại vào hệ thống trước khi nộp bản chỉnh sửa.' };
  try {
    const res = await fetchWithTimeout(`${API_BASE}/baibao/${baiBaoId}/resubmit`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      },
      body: formData
    });
    if (res.ok) return await res.json();
    const errData = await res.json().catch(() => null);
    return {
      success: false,
      message: errData?.message || `Không thể nộp bản chỉnh sửa (Mã lỗi HTTP ${res.status}). Vui lòng thử lại.`
    };
  } catch (e) {
    return {
      success: false,
      message: 'Không thể kết nối đến máy chủ tòa soạn. Bản thảo chỉnh sửa chưa được lưu.'
    };
  }
}

// Quản lý trạng thái phiên đăng nhập người dùng (User Session Management)
async function initUserSession() {
  const token = localStorage.getItem('journal_token');
  if (token) {
    // Đồng bộ thông tin người dùng mới nhất từ database qua token
    const profile = await apiGetProfile();
    if (profile) {
      const user = {
        isLoggedIn: true,
        ...profile,
        chucVu: (profile.vaiTros && profile.vaiTros.length > 0) ? profile.vaiTros.join(', ') : 'Tác giả'
      };
      localStorage.setItem('journal_user', JSON.stringify(user));
    }
  }
  renderAuthNavbar();
}

// Hàm tra cứu tài khoản hệ thống phục vụ Auto-match đồng tác giả (Real-time CSDL SQL Server)
async function findAccountByEmail(email) {
  if (!email) return null;
  const cleanEmail = email.trim().toLowerCase();

  try {
    if (typeof apiLookupUser === 'function') {
      const liveUser = await apiLookupUser(cleanEmail);
      if (liveUser) {
        return {
          id: liveUser.maNguoiDung,
          hoTen: liveUser.hoTen,
          email: liveUser.email,
          donVi: liveUser.donVi || 'Chưa cập nhật đơn vị',
          orcid: liveUser.maORCID || '',
          vaiTro: 'Thành viên hệ thống'
        };
      }
    }
  } catch (e) {
    // API offline
  }

  return null;
}

function getCurrentUser() {
  try {
    return JSON.parse(localStorage.getItem('journal_user'));
  } catch (e) {
    return null;
  }
}

function setCurrentUser(userObj) {
  if (userObj) {
    localStorage.setItem('journal_user', JSON.stringify(userObj));
  } else {
    localStorage.removeItem('journal_user');
  }
  renderAuthNavbar();
}

function hasReviewerRole(user) {
  const allowed = ['Chuyên gia phản biện', 'Phản biện viên', 'Người phản biện', 'Phản biện', 'Reviewer'];
  return !!user && Array.isArray(user.vaiTros) && user.vaiTros.some(role => allowed.includes(role));
}

function renderAuthNavbar() {
  const user = getCurrentUser();
  const authContainers = document.querySelectorAll('.auth-links');
  
  authContainers.forEach(container => {
    if (user && user.isLoggedIn) {
      // Đã đăng nhập: Render Avatar Circle + Tên tác giả + Dropdown Menu
      const initial = user.hoTen ? user.hoTen.split(' ').pop().charAt(0).toUpperCase() : 'U';
      const avatarHtml = user.anhDaiDien
        ? `<img src="${getAvatarUrl(user.anhDaiDien)}" alt="Avatar" style="width:100%;height:100%;object-fit:cover;border-radius:50%;">`
        : initial;
      container.innerHTML = `
        <div class="user-logged-badge" onclick="toggleUserDropdown(event)">
          <div class="user-avatar-circle" style="${user.anhDaiDien ? 'background:transparent;padding:0;overflow:hidden;border:1.5px solid #fff;' : ''}">${avatarHtml}</div>
          <div class="user-badge-info">
            <span class="user-badge-name">${user.hoTen}</span>
            <span class="user-badge-role">${user.chucVu || 'Tác giả'}</span>
          </div>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#ffffff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"></polyline></svg>

          <!-- Dropdown Menu -->
          <div class="user-dropdown-menu" id="global-user-dropdown">
            <div class="dropdown-header" style="display:flex;align-items:center;gap:12px;">
              <div style="width:38px;height:38px;border-radius:50%;overflow:hidden;flex-shrink:0;background:#e2e8f0;display:flex;align-items:center;justify-content:center;font-weight:700;color:#1da1f2;font-size:15px;border:1px solid #cbd5e1;">
                ${user.anhDaiDien ? `<img src="${getAvatarUrl(user.anhDaiDien)}" style="width:100%;height:100%;object-fit:cover;">` : initial}
              </div>
              <div style="overflow:hidden;">
                <div class="dropdown-header-name" style="text-overflow:ellipsis;overflow:hidden;white-space:nowrap;">${user.hoTen}</div>
                <div class="dropdown-header-email" style="text-overflow:ellipsis;overflow:hidden;white-space:nowrap;">${user.email}</div>
              </div>
            </div>
            <a href="profile.html" class="dropdown-item">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
              Trang cá nhân &amp; Thống kê
            </a>
            ${hasReviewerRole(user) ? '<a href="reviewer.html" class="dropdown-item">Bàn làm việc phản biện</a>' : ''}
            <a href="submit-paper.html" class="dropdown-item">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="12" y1="18" x2="12" y2="12"></line><line x1="9" y1="15" x2="15" y2="15"></line></svg>
              Gửi bản thảo bài báo mới
            </a>
            <a href="javascript:void(0)" onclick="openPasswordModal()" class="dropdown-item">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>
              Đổi mật khẩu bảo mật
            </a>
            <div class="dropdown-divider"></div>
            <a href="javascript:void(0)" onclick="handleUserLogout()" class="dropdown-item logout">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path><polyline points="16 17 21 12 16 7"></polyline><line x1="21" y1="12" x2="9" y2="12"></line></svg>
              Đăng xuất tài khoản
            </a>
          </div>
        </div>
      `;
    } else {
      // Chưa đăng nhập: Render 2 nút Trắng Đăng nhập / Đăng ký
      container.innerHTML = `
        <a href="login.html" class="login-btn">Đăng nhập</a>
        <a href="register.html" class="register-btn">Đăng ký</a>
      `;
    }
  });
}

function toggleUserDropdown(event) {
  event.stopPropagation();
  const menu = document.getElementById('global-user-dropdown');
  if (menu) {
    menu.classList.toggle('show');
  }
}

// Đóng dropdown khi nhấn ra ngoài
document.addEventListener('click', () => {
  const menu = document.getElementById('global-user-dropdown');
  if (menu && menu.classList.contains('show')) {
    menu.classList.remove('show');
  }
});

function handleUserLogout() {
  localStorage.removeItem('journal_token');
  localStorage.removeItem('journal_user');
  sessionStorage.clear();
  showToast('Đã đăng xuất khỏi hệ thống thành công!');
  renderAuthNavbar();
  
  // Đóng dropdown menu nếu đang mở
  const menu = document.getElementById('global-user-dropdown');
  if (menu && menu.classList.contains('show')) {
    menu.classList.remove('show');
  }

  // Nếu đang ở trang yêu cầu xác thực (như profile hoặc submit-paper), chuyển về trang chủ
  const path = window.location.pathname.toLowerCase();
  if (path.includes('profile') || path.includes('submit-paper')) {
    setTimeout(() => {
      window.location.href = 'UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html';
    }, 600);
  }
}

// =============================================================================
// QUẢN LÝ VÀ ĐIỀU KHIỂN MODAL ĐỔI MẬT KHẨU TOÀN CỤC (GLOBAL PASSWORD MODAL)
// Hỗ trợ mở modal đổi mật khẩu từ bất kỳ trang nào trong toàn bộ hệ thống
// =============================================================================

function togglePasswordVisibility(inputId, btnEl) {
  const input = document.getElementById(inputId);
  if (!input) return;
  const isPass = input.type === 'password';
  input.type = isPass ? 'text' : 'password';
  if (btnEl) {
    btnEl.innerHTML = isPass
      ? `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>`
      : `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>`;
    btnEl.setAttribute('aria-label', isPass ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
  }
}

function evaluatePasswordStrength(val, barPrefix = 'pass-bar-', labelId = 'pass-strength-text') {
  const label = document.getElementById(labelId);
  const bars = [1, 2, 3, 4].map(i => document.getElementById(barPrefix + i));
  
  if (!val) {
    bars.forEach(b => { if (b) b.style.backgroundColor = '#e2e8f0'; });
    if (label) {
      label.innerHTML = '<span>Độ mạnh mật khẩu</span><span style="color:#94a3b8;">Chưa nhập</span>';
    }
    return 0;
  }

  let score = 0;
  if (val.length >= 6) score++;
  if (val.length >= 10) score++;
  if (/[a-z]/.test(val) && /[A-Z]/.test(val)) score++;
  if (/\d/.test(val) || /[^a-zA-Z0-9]/.test(val)) score++;

  const levels = [
    { text: 'Rất yếu', color: '#ef4444' },
    { text: 'Yếu', color: '#f97316' },
    { text: 'Trung bình', color: '#eab308' },
    { text: 'Mạnh', color: '#10b981' }
  ];

  const currentLevel = levels[Math.max(0, score - 1)];

  bars.forEach((b, idx) => {
    if (b) {
      b.style.backgroundColor = idx < score ? currentLevel.color : '#e2e8f0';
    }
  });

  if (label) {
    label.innerHTML = `<span>Độ mạnh mật khẩu</span><span style="color:${currentLevel.color};font-weight:700;">${currentLevel.text}</span>`;
  }
  return score;
}

function evaluatePasswordMatch(newId, confId, hintId) {
  const pNew = document.getElementById(newId)?.value || '';
  const pConf = document.getElementById(confId)?.value || '';
  const hint = document.getElementById(hintId);
  if (!hint) return;

  if (!pConf) {
    hint.innerHTML = '';
    return;
  }

  if (pNew === pConf) {
    hint.innerHTML = '<span style="color:#10b981;font-weight:600;">✓ Mật khẩu xác nhận hoàn toàn trùng khớp</span>';
  } else {
    hint.innerHTML = '<span style="color:#ef4444;font-weight:600;">⚠ Mật khẩu xác nhận chưa trùng khớp</span>';
  }
}

function resetPassValidationIndicators(newId, confId, barPrefix, labelId, hintId) {
  evaluatePasswordStrength('', barPrefix, labelId);
  const hint = document.getElementById(hintId);
  if (hint) hint.innerHTML = '';
}

function openPasswordModal() {
  // Đóng dropdown navbar nếu đang mở
  const menu = document.getElementById('global-user-dropdown');
  if (menu) menu.classList.remove('show');

  // Ưu tiên 1: Nếu trang hiện tại đã có modal-change-pass (như profile.html)
  const localModal = document.getElementById('modal-change-pass');
  if (localModal) {
    const form = localModal.querySelector('form');
    if (form) form.reset();
    resetPassValidationIndicators('p-new', 'p-conf', 'pass-bar-', 'pass-strength-text', 'p-match-hint');
    localModal.classList.add('open');
    document.body.style.overflow = 'hidden';
    const firstInput = localModal.querySelector('input[type="password"], input');
    if (firstInput) setTimeout(() => firstInput.focus(), 120);
    return;
  }

  // Ưu tiên 2: Nếu ở các trang khác (trang chủ, nộp bài, kho lưu trữ...), tự động chèn modal toàn cục
  ensureGlobalPasswordModal();
  const globalModal = document.getElementById('modal-global-change-pass');
  if (globalModal) {
    const form = globalModal.querySelector('form');
    if (form) form.reset();
    resetPassValidationIndicators('gp-new', 'gp-conf', 'gpass-bar-', 'gpass-strength-text', 'gp-match-hint');
    globalModal.classList.add('open');
    document.body.style.overflow = 'hidden';
    const firstInput = document.getElementById('gp-current');
    if (firstInput) setTimeout(() => firstInput.focus(), 120);
  }
}

function closePasswordModal(modalId = 'modal-change-pass') {
  const modal = document.getElementById(modalId) || document.getElementById('modal-global-change-pass');
  if (modal) {
    modal.classList.remove('open');
    document.body.style.overflow = '';
    const form = modal.querySelector('form');
    if (form) form.reset();
  }
  if (window.location.hash === '#tab-password' || window.location.hash === '#change-password') {
    history.replaceState(null, document.title, window.location.pathname + window.location.search);
  }
}

function ensureGlobalPasswordModal() {
  if (document.getElementById('modal-global-change-pass')) return;
  const html = `
  <div class="modal-overlay" id="modal-global-change-pass">
    <div class="modal-box-edit pass-modal-box">
      <div class="pass-modal-header">
        <div>
          <h3>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#1da1f2" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>
            Thiết lập mật khẩu bảo mật
          </h3>
          <p>Cập nhật mật khẩu tài khoản tác giả / chuyên gia phản biện của Tòa soạn.</p>
        </div>
        <button type="button" class="pass-modal-close" onclick="closePasswordModal('modal-global-change-pass')">&times;</button>
      </div>
      <form onsubmit="handleGlobalPasswordSubmit(event)">
        <div class="form-group-modal">
          <label>Mật khẩu hiện tại <span style="color:#ef4444;">*</span></label>
          <div class="pass-input-wrap">
            <input type="password" id="gp-current" class="form-control-modal" required placeholder="Nhập mật khẩu hiện tại">
            <button type="button" class="pass-toggle-btn" onclick="togglePasswordVisibility('gp-current', this)" title="Ẩn/hiện mật khẩu">
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>
            </button>
          </div>
        </div>
        <div class="form-group-modal">
          <label>Mật khẩu mới <span style="color:#ef4444;">*</span></label>
          <div class="pass-input-wrap">
            <input type="password" id="gp-new" class="form-control-modal" required placeholder="Tối thiểu 6 ký tự" oninput="evaluatePasswordStrength(this.value, 'gpass-bar-', 'gpass-strength-text'); evaluatePasswordMatch('gp-new', 'gp-conf', 'gp-match-hint');">
            <button type="button" class="pass-toggle-btn" onclick="togglePasswordVisibility('gp-new', this)" title="Ẩn/hiện mật khẩu">
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>
            </button>
          </div>
          <div class="pass-strength-meter">
            <div class="pass-strength-bar" id="gpass-bar-1"></div>
            <div class="pass-strength-bar" id="gpass-bar-2"></div>
            <div class="pass-strength-bar" id="gpass-bar-3"></div>
            <div class="pass-strength-bar" id="gpass-bar-4"></div>
          </div>
          <div class="pass-strength-label" id="gpass-strength-text">
            <span>Độ mạnh mật khẩu</span>
            <span style="color:#94a3b8;">Chưa nhập</span>
          </div>
        </div>
        <div class="form-group-modal">
          <label>Xác nhận lại mật khẩu mới <span style="color:#ef4444;">*</span></label>
          <div class="pass-input-wrap">
            <input type="password" id="gp-conf" class="form-control-modal" required placeholder="Nhập lại chính xác mật khẩu mới" oninput="evaluatePasswordMatch('gp-new', 'gp-conf', 'gp-match-hint');">
            <button type="button" class="pass-toggle-btn" onclick="togglePasswordVisibility('gp-conf', this)" title="Ẩn/hiện mật khẩu">
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>
            </button>
          </div>
          <div class="pass-match-hint" id="gp-match-hint"></div>
        </div>
        <div class="pass-policy-notice">
          <strong>Quy tắc an toàn mật khẩu HUIT Journal:</strong>
          <div style="margin-top:3px;">1. Độ dài tối thiểu 6 ký tự.</div>
          <div>2. Không trùng hoàn toàn với mật khẩu hiện tại.</div>
          <div>3. Nên phối hợp chữ hoa, chữ thường và chữ số.</div>
        </div>
        <div class="modal-btn-row">
          <button type="button" class="btn-modal-cancel" onclick="closePasswordModal('modal-global-change-pass')">Hủy bỏ</button>
          <button type="submit" class="btn-modal-save" id="btn-save-global-pass">Cập nhật mật khẩu</button>
        </div>
      </form>
    </div>
  </div>`;
  document.body.insertAdjacentHTML('beforeend', html);
}

async function handleGlobalPasswordSubmit(e) {
  e.preventDefault();
  const pCurr = document.getElementById('gp-current')?.value || '';
  const pNew  = document.getElementById('gp-new')?.value || '';
  const pConf = document.getElementById('gp-conf')?.value || '';

  if (!pCurr) {
    showToast('Vui lòng nhập mật khẩu hiện tại.', 'error');
    document.getElementById('gp-current')?.focus();
    return;
  }
  if (pNew.length < 8) {
    showToast('Mật khẩu mới phải có tối thiểu 8 ký tự theo quy định bảo mật.', 'error');
    document.getElementById('gp-new')?.focus();
    return;
  }
  const weakPasswords = ['123456', '12345678', '123456789', 'password', 'password123', 'admin123', 'qwerty'];
  if (weakPasswords.includes(pNew.toLowerCase())) {
    showToast('Mật khẩu mới quá đơn giản hoặc dễ đoán. Vui lòng chọn mật khẩu phức tạp hơn.', 'error');
    document.getElementById('gp-new')?.focus();
    return;
  }
  if (!/[A-Z]/.test(pNew) || !/[a-z]/.test(pNew) || (!/[0-9]/.test(pNew) && !/[^A-Za-z0-9]/.test(pNew))) {
    showToast('Mật khẩu mới phải bao gồm chữ hoa, chữ thường và chữ số hoặc ký tự đặc biệt.', 'error');
    document.getElementById('gp-new')?.focus();
    return;
  }
  if (pNew === pCurr) {
    showToast('Mật khẩu mới không được trùng hoàn toàn với mật khẩu hiện tại.', 'error');
    document.getElementById('gp-new')?.focus();
    return;
  }
  if (pNew !== pConf) {
    showToast('Mật khẩu mới và xác nhận mật khẩu không trùng khớp.', 'error');
    document.getElementById('gp-conf')?.focus();
    return;
  }

  const btn = document.getElementById('btn-save-global-pass');
  const oldText = btn ? btn.innerHTML : '';
  if (btn) { btn.disabled = true; btn.innerText = 'Đang cập nhật...'; }

  try {
    const res = await apiChangePassword(pCurr, pNew);
    if (res && res.success) {
      closePasswordModal('modal-global-change-pass');
      showToast('Đổi mật khẩu tài khoản thành công! Thông tin bảo mật đã được cập nhật.', 'success', 5000);
    } else {
      showToast(res.message || 'Mật khẩu hiện tại không chính xác.', 'error', 5000);
      document.getElementById('gp-current')?.focus();
    }
  } catch (err) {
    console.error(err);
    showToast('Lỗi kết nối máy chủ khi cập nhật mật khẩu.', 'error');
  } finally {
    if (btn) { btn.disabled = false; btn.innerHTML = oldText; }
  }
}

// Toast Helper
let journalToastTimer = null;
function showToast(message, typeOrIcon = 'success', duration = 4000) {
  let toast = document.getElementById('journal-global-toast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'journal-global-toast';
    document.body.appendChild(toast);
  }

  const ICONS = {
    success: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>`,
    error: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>`,
    warning: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>`,
    info: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>`
  };

  let typeClass = 'toast-success';
  let iconHtml = ICONS.success;

  if (typeof typeOrIcon === 'string') {
    if (typeOrIcon.startsWith('<svg')) {
      iconHtml = typeOrIcon;
    } else if (typeOrIcon === 'error' || typeOrIcon === 'danger') {
      typeClass = 'toast-error';
      iconHtml = ICONS.error;
    } else if (typeOrIcon === 'warning') {
      typeClass = 'toast-warning';
      iconHtml = ICONS.warning;
    } else if (typeOrIcon === 'info') {
      typeClass = 'toast-info';
      iconHtml = ICONS.info;
    } else if (typeOrIcon === 'success') {
      typeClass = 'toast-success';
      iconHtml = ICONS.success;
    }
  }

  toast.className = `journal-toast ${typeClass}`;
  const formattedMsg = (message || '').toString().replace(/\n/g, '<br>');
  toast.innerHTML = `
    ${iconHtml}
    <div class="toast-msg">${formattedMsg}</div>
    <button type="button" class="toast-close-btn" onclick="this.parentElement.classList.remove('active')" title="Đóng">&times;</button>
  `;

  // Reflow để khởi chạy lại animation
  toast.classList.remove('active');
  void toast.offsetWidth;
  toast.classList.add('active');

  if (journalToastTimer) clearTimeout(journalToastTimer);
  if (duration > 0) {
    journalToastTimer = setTimeout(() => {
      toast.classList.remove('active');
    }, duration);
  }
}

window.showToast = showToast;
window.showSuccessToast = (msg, d) => showToast(msg, 'success', d);
window.showErrorToast = (msg, d) => showToast(msg, 'error', d);
window.showWarningToast = (msg, d) => showToast(msg, 'warning', d);
window.showInfoToast = (msg, d) => showToast(msg, 'info', d);

// Modal Core Controller
function openModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.add('open');
    document.body.style.overflow = 'hidden';
  }
}

function closeModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.remove('open');
    document.body.style.overflow = '';
  }
}

// Global Modal Infrastructure
function initModals() {
  // Inject reusable modals if not present
  if (!document.getElementById('modal-auth')) {
    const authModalHtml = `
    <div id="modal-auth" class="modal-overlay">
      <div class="modal-card">
        <div class="modal-header">
          <h3 id="auth-modal-title">Cổng thông tin tài khoản</h3>
          <button type="button" class="modal-close-btn" onclick="closeModal('modal-auth')">&times;</button>
        </div>
        <div class="modal-body">
          <div class="auth-tabs">
            <button type="button" class="auth-tab-btn active" id="tab-login-btn" onclick="switchAuthTab('login')">Đăng nhập</button>
            <button type="button" class="auth-tab-btn" id="tab-register-btn" onclick="switchAuthTab('register')">Đăng ký mới</button>
          </div>
          
          <!-- FORM ĐĂNG NHẬP -->
          <form id="auth-login-form" onsubmit="handleLoginSubmit(event)">
            <div class="auth-form-group">
              <label>Email hoặc Tên đăng nhập</label>
              <input type="text" name="usernameOrEmail" required autocomplete="username" placeholder="Email hoặc tên đăng nhập">
            </div>
            <div class="auth-form-group">
              <label>Mật khẩu</label>
              <input type="password" name="password" required autocomplete="current-password" placeholder="Nhập mật khẩu">
            </div>
            <button type="submit" class="auth-submit-btn">Đăng nhập vào hệ thống</button>
          </form>

          <!-- FORM ĐĂNG KÝ -->
          <div id="auth-register-form" style="display:none;">
            <p>Đăng ký tài khoản qua biểu mẫu đăng ký chính thức.</p>
            <a class="auth-submit-btn" href="register.html">Mở trang đăng ký</a>
          </div>
        </div>
      </div>
    </div>`;
    document.body.insertAdjacentHTML('beforeend', authModalHtml);
  }

  // Modal Trích Dẫn (Citation Modal)
  if (!document.getElementById('modal-citation')) {
    const citeModalHtml = `
    <div id="modal-citation" class="modal-overlay">
      <div class="modal-card">
        <div class="modal-header">
          <h3>Xuất thông tin trích dẫn bài báo</h3>
          <button type="button" class="modal-close-btn" onclick="closeModal('modal-citation')">&times;</button>
        </div>
        <div class="modal-body">
          <div class="citation-format-tabs">
            <button type="button" class="cite-tab-btn active" onclick="switchCiteFormat('apa')">Chuẩn APA 7th</button>
            <button type="button" class="cite-tab-btn" onclick="switchCiteFormat('bibtex')">BibTeX</button>
            <button type="button" class="cite-tab-btn" onclick="switchCiteFormat('ris')">RIS (EndNote)</button>
          </div>
          <div id="citation-content-display" class="citation-text-box"></div>
        </div>
        <div class="modal-footer">
          <button type="button" class="cite-tab-btn" onclick="copyCitation()">Sao chép vào bộ nhớ</button>
          <button type="button" class="cite-tab-btn active" onclick="downloadCitationFile()">Tải file trích dẫn</button>
        </div>
      </div>
    </div>`;
    document.body.insertAdjacentHTML('beforeend', citeModalHtml);
  }

  // Modal Thông Tin Chung (About / Ethics / Contact Modal)
  if (!document.getElementById('modal-info')) {
    const infoModalHtml = `
    <div id="modal-info" class="modal-overlay">
      <div class="modal-card" style="max-width:600px;">
        <div class="modal-header">
          <h3 id="info-modal-title">Thông tin tòa soạn</h3>
          <button type="button" class="modal-close-btn" onclick="closeModal('modal-info')">&times;</button>
        </div>
        <div class="modal-body policy-content" id="info-modal-body">
        </div>
        <div class="modal-footer">
          <button type="button" class="auth-submit-btn" style="margin:0;width:auto;padding:8px 18px;" onclick="closeModal('modal-info')">Đã hiểu</button>
        </div>
      </div>
    </div>`;
    document.body.insertAdjacentHTML('beforeend', infoModalHtml);
  }

  // Close when clicking outside modal card
  document.querySelectorAll('.modal-overlay').forEach(modal => {
    modal.addEventListener('click', (e) => {
      if (e.target === modal) {
        closeModal(modal.id);
      }
    });
  });
}

// Auth Handlers
function switchAuthTab(tab) {
  const loginBtn = document.getElementById('tab-login-btn');
  const regBtn = document.getElementById('tab-register-btn');
  const loginForm = document.getElementById('auth-login-form');
  const regForm = document.getElementById('auth-register-form');
  const title = document.getElementById('auth-modal-title');

  if (tab === 'login') {
    loginBtn.classList.add('active');
    regBtn.classList.remove('active');
    loginForm.style.display = 'block';
    regForm.style.display = 'none';
    title.innerText = 'Đăng nhập hệ thống tòa soạn';
  } else {
    loginBtn.classList.remove('active');
    regBtn.classList.add('active');
    loginForm.style.display = 'none';
    regForm.style.display = 'block';
    title.innerText = 'Đăng ký tài khoản tác giả / bạn đọc';
  }
}

async function handleLoginSubmit(e) {
  e.preventDefault();
  const form = e.currentTarget;
  const usernameOrEmail = form.elements.usernameOrEmail?.value.trim();
  const password = form.elements.password?.value;
  const submitButton = form.querySelector('button[type="submit"]');
  if (!usernameOrEmail || !password) return;

  if (submitButton) {
    submitButton.disabled = true;
    submitButton.textContent = 'Đang xác thực...';
  }
  try {
    const result = await apiLogin(usernameOrEmail, password);
    if (!result?.success || !result.token || !result.user) {
      showToast(result?.message || 'Không thể xác thực tài khoản.', 'error');
      return;
    }
    localStorage.setItem('journal_token', result.token);
    const user = {
      isLoggedIn: true,
      ...result.user,
      chucVu: Array.isArray(result.user.vaiTros) && result.user.vaiTros.length
        ? result.user.vaiTros.join(', ')
        : 'Tác giả'
    };
    setCurrentUser(user);
    closeModal('modal-auth');
    showToast(`Đăng nhập thành công. Xin chào ${result.user.hoTen || result.user.email || ''}.`, 'success');
    setTimeout(() => { window.location.href = 'profile.html'; }, 600);
  } finally {
    if (submitButton) {
      submitButton.disabled = false;
      submitButton.textContent = 'Đăng nhập vào hệ thống';
    }
  }
}

function initAuthTriggers() {
  document.querySelectorAll('a[href="#login"]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      switchAuthTab('login');
      openModal('modal-auth');
    });
  });

  document.querySelectorAll('a[href="#register"]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      switchAuthTab('register');
      openModal('modal-auth');
    });
  });
}

// Citation Logic
const citations = {
  apa: '',
  bibtex: '',
  ris: ''
};

function generateArticleCitations(art) {
  if (!art) return;
  const authorNames = (art.tacGias && art.tacGias.length > 0)
    ? art.tacGias.map(t => t.hoTen).join(', ')
    : (art.tacGiaChinh || '');
  const year = art.nam || (art.ngayPhatHanh ? new Date(art.ngayPhatHanh).getFullYear() : 'n.d.');
  const vol = art.tap ? String(art.tap) : '';
  const no = art.so ? String(art.so) : '';
  const pages = art.trangBatDau && art.trangKetThuc ? `${art.trangBatDau}–${art.trangKetThuc}` : '';
  const doi = String(art.maDOI || '').trim().replace(/^https?:\/\/doi\.org\//i, '');
  const doiStr = doi ? ` https://doi.org/${doi}` : '';
  const journalName = art.tenSoTapChi && art.tenSoTapChi.includes('Yersin')
    ? 'Tạp chí Khoa học Yersin'
    : 'Tạp chí Khoa học Đại học Công Thương';
  const volumeIssue = `${vol ? `, ${vol}` : ''}${no ? `(${no})` : ''}`;
  const pagePart = pages ? `, ${pages}` : '';
  const authorBib = authorNames.replace(/,/g, ' and');
  const citationAuthors = authorNames || 'Chưa có thông tin tác giả';

  citations.apa = `${citationAuthors} (${year}). ${art.tieuDe}. ${journalName}${volumeIssue}${pagePart}.${doiStr}`;
  citations.bibtex = `@article{article_${art.maBaiBao || 'unknown'}${art.nam ? `_${art.nam}` : ''},
  title={${art.tieuDe}},
  ${authorNames ? `author={${authorBib}},` : ''}
  journal={${journalName}},
  ${vol ? `volume={${vol}},` : ''}
  ${no ? `number={${no}},` : ''}
  ${pages ? `pages={${pages.replace('–', '--')}},` : ''}
  ${art.nam ? `year={${art.nam}},` : ''}
  ${doi ? `doi={${doi}}` : ''}
}`;
  citations.ris = `TY  - JOUR
TI  - ${art.tieuDe}
${(art.tacGias && art.tacGias.length > 0 ? art.tacGias : (art.tacGiaChinh ? [{hoTen: art.tacGiaChinh}] : [])).map(t => `AU  - ${t.hoTen}`).join('\n')}
JO  - ${journalName}
${vol ? `VL  - ${vol}\n` : ''}${no ? `IS  - ${no}\n` : ''}${art.trangBatDau ? `SP  - ${art.trangBatDau}\n` : ''}${art.trangKetThuc ? `EP  - ${art.trangKetThuc}\n` : ''}${art.nam ? `PY  - ${art.nam}\n` : ''}${doi ? `DO  - ${doi}\n` : ''}
ER  - `;
}

let currentCiteFormat = 'apa';

function switchCiteFormat(format) {
  currentCiteFormat = format;
  document.querySelectorAll('.cite-tab-btn').forEach(b => b.classList.remove('active'));
  if (event && event.target) {
    event.target.classList.add('active');
  }
  const display = document.getElementById('citation-content-display');
  if (display) {
    display.textContent = citations[format] || 'Đang cập nhật trích dẫn...';
  }
}

function copyCitation() {
  const text = citations[currentCiteFormat];
  if (!text) {
    showToast('Chưa có thông tin trích dẫn.');
    return;
  }
  navigator.clipboard.writeText(text).then(() => {
    showToast('Đã sao chép trích dẫn vào bộ nhớ tạm (Clipboard)!');
  }).catch(() => {
    showToast('Đã chọn toàn bộ trích dẫn, vui lòng nhấn Ctrl+C để sao chép.');
  });
}

function downloadCitationFile() {
  const ext = currentCiteFormat === 'bibtex' ? 'bib' : (currentCiteFormat === 'ris' ? 'ris' : 'txt');
  const text = citations[currentCiteFormat] || '';
  const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `citation.${ext}`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
  showToast(`Đã tải xuống tệp trích dẫn (.${ext})!`);
}

function initCitationTriggers() {
  document.querySelectorAll('.btn-cite, a[href="#cite"]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      const display = document.getElementById('citation-content-display');
      if (display) {
        display.textContent = citations.apa || 'Đang nạp trích dẫn từ bài báo khoa học...';
      }
      openModal('modal-citation');
    });
  });
}

// Download PDF: Chỉ chặn nếu liên kết chưa có PDF thành phẩm (href="#download-pdf" hoặc "#")
function initDownloadTriggers() {
  document.querySelectorAll('.btn-download-pdf, a[href="#download-pdf"]').forEach(btn => {
    const href = btn.getAttribute('href');
    if (!href || href === '#' || href === '#download-pdf' || href.startsWith('javascript:')) {
      btn.addEventListener('click', (e) => {
        const currentHref = btn.getAttribute('href');
        if (!currentHref || currentHref === '#' || currentHref === '#download-pdf' || currentHref.startsWith('javascript:')) {
          e.preventDefault();
          showToast('Bài báo này hiện đang trong quá trình chế bản, chưa phát hành tệp PDF chính thức.');
        }
      });
    }
  });
}

// Info & Policy Dialogs
const infoContents = {
  about: {
    title: 'Về Tạp chí Khoa học Đại học Công Thương',
    html: `
      <h4>Tôn chỉ & Mục đích</h4>
      <p>Tạp chí Khoa học Đại học Công Thương (Huit Journal of Science) là ấn phẩm học thuật chính thức trực thuộc Trường Đại học Công Thương TP. Hồ Chí Minh, hoạt động theo Giấy phép số 53/GP-BTTTT do Bộ Thông tin và Truyền thông cấp. Chỉ số chuẩn quốc tế: <strong>p-ISSN 3030-4113</strong> (bản in) và <strong>e-ISSN 3030-413X</strong> (bản điện tử Open Access). Tạp chí là diễn đàn công bố các kết quả nghiên cứu khoa học nguyên gốc và các giải pháp công nghệ tiên tiến.</p>
      <h4>Ban lãnh đạo Tòa soạn</h4>
      <p><strong>Tổng biên tập:</strong> TS. Bùi Hồng Đăng (Chủ tịch Hội đồng trường)</p>
      <p><strong>Chủ tịch Hội đồng biên tập:</strong> PGS. TS. Nguyễn Xuân Hoàn (Hiệu trưởng nhà trường)</p>
      <p><strong>Văn phòng Tòa soạn:</strong> Phòng C101, Tòa nhà C, 140 Lê Trọng Tấn, Phường Tây Thạnh, TP.HCM</p>
      <h4>Chỉ số xuất bản & Định danh</h4>
      <p>Tạp chí được lập chỉ mục tại Google Scholar, Vietnam Citation Index (VCI) và được cấp mã định danh số DOI bởi CrossRef.</p>
    `
  },
  ethics: {
    title: 'Quy định Đạo đức Xuất bản & Liêm chính Học thuật',
    html: `
      <h4>Cam kết không trùng lặp (Anti-Plagiarism)</h4>
      <p>Tất cả bản thảo nộp lên hệ thống đều phải trải qua bước quét tương đồng văn bản bằng hệ thống Turnitin/DoIT. Mức độ trùng lặp cho phép không vượt quá 20% (và không vượt quá 3% cho từng tài liệu nguồn riêng lẻ).</p>
      <h4>Quy trình phản biện kín 2 chiều (Double-Blind Peer Review)</h4>
      <p>Bản thảo sau khi sơ duyệt sẽ được gỡ bỏ toàn bộ danh tính tác giả trước khi chuyển đến tối thiểu 02 chuyên gia độc lập cùng chuyên ngành thẩm định. Thời gian phản biện trung bình là 30 ngày.</p>
      <h4>Xung đột lợi ích & Bản quyền</h4>
      <p>Tác giả phải công khai mọi xung đột lợi ích tiềm ẩn (tài trợ tài chính, quan hệ thương mại). Các bài báo sau xuất bản được phân phối theo giấy phép Quốc tế Creative Commons Ghi nhận công của tác giả - Phi thương mại 4.0 (CC BY-NC 4.0).</p>
    `
  },
  contact: {
    title: 'Thông tin liên hệ Tòa soạn HUIT Journal',
    html: `
      <h4>Văn phòng Tòa soạn Tạp chí Khoa học Đại học Công Thương</h4>
      <p><strong>Địa chỉ:</strong> 140 Lê Trọng Tấn, Phường Tây Thạnh, Thành phố Hồ Chí Minh</p>
      <p><strong>Văn phòng:</strong> Phòng C101, Tòa nhà C, Trường ĐH Công Thương TP.HCM</p>
      <p><strong>Điện thoại:</strong> 028.38163318 - ext.112</p>
      <p><strong>Email tiếp nhận bản thảo:</strong> journal@huit.edu.vn</p>
      <p><strong>Cổng thông tin trực tuyến:</strong> https://huitjournal.vn | https://journal.huit.edu.vn</p>
      <p><strong>Tổng biên tập:</strong> TS. Bùi Hồng Đăng</p>
    `
  },
  guidelines: {
    title: 'Thể lệ & Quy trình nộp bài',
    html: `
      <h4>Quy cách bản thảo</h4>
      <p>Bản thảo được soạn thảo trên phần mềm MS Word (font Times New Roman, size 12, cách dòng 1.2), độ dài thông thường từ 8 đến 15 trang (bao gồm cả tài liệu tham khảo và phụ lục).</p>
      <h4>Cấu trúc bài báo chuẩn IMRAD</h4>
      <p><strong>01. Tiêu đề & Tóm tắt:</strong> Đầy đủ cả tiếng Việt và tiếng Anh (150 – 250 từ) kèm 4 – 6 từ khóa.</p>
      <p><strong>02. Mở đầu (Introduction):</strong> Đặt vấn đề và tổng quan tài liệu nghiên cứu.</p>
      <p><strong>03. Phương pháp (Methods):</strong> Mô tả chi tiết phương pháp tiếp cận, thuật toán hoặc thiết kế thực nghiệm.</p>
      <p><strong>04. Kết quả & Thảo luận (Results & Discussion):</strong> Số liệu, bảng biểu minh họa rõ ràng.</p>
      <p><strong>05. Kết luận (Conclusion):</strong> Tóm lược đóng góp và hướng phát triển.</p>
      <p><strong>06. Tài liệu tham khảo:</strong> Trình bày theo chuẩn APA 7th hoặc IEEE.</p>
    `
  }
};

function openInfoModal(type) {
  const content = infoContents[type] || infoContents.about;
  document.getElementById('info-modal-title').innerText = content.title;
  document.getElementById('info-modal-body').innerHTML = content.html;
  openModal('modal-info');
}

function initInfoTriggers() {
  // Cho phép tất cả các thẻ <a> điều hướng tự nhiên đến trang riêng (.html),
  // không chặn sự kiện (e.preventDefault) để tránh tình trạng bật popup modal gây ức chế người dùng.
  document.querySelectorAll('button[data-info-modal]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      const type = btn.getAttribute('data-info-modal');
      openInfoModal(type);
    });
  });
}

// Newsletter
function initNewsletterTriggers() {
  document.querySelectorAll('.newsletter-box button').forEach(btn => {
    btn.addEventListener('click', () => {
      const input = btn.parentElement.querySelector('input');
      if (input && input.value.trim() && input.checkValidity()) {
        showToast('Chức năng nhận bản tin chưa kết nối với máy chủ; email chưa được đăng ký.', 'warning', 6000);
      } else {
        showToast('Vui lòng nhập địa chỉ email hợp lệ trước khi bấm đăng ký.', 'warning');
      }
    });
  });
}

// Search & Archives Filter
function initSearchTriggers() {
  const archivesInput = document.getElementById('archivesSearchInput');
  if (archivesInput) {
    archivesInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        handleArchivesSearch();
      }
    });

    // Tự động kiểm tra tham số URL query nếu có (ví dụ archives.html?q=AI)
    const urlParams = new URLSearchParams(window.location.search);
    const queryParam = urlParams.get('q');
    if (queryParam) {
      archivesInput.value = queryParam;
      handleArchivesSearch();
    }
  }
}

// Xử lý tìm kiếm trong trang Kho lưu trữ
function handleArchivesSearch() {
  const input = document.getElementById('archivesSearchInput');
  if (!input) return;
  const keyword = input.value.trim().toLowerCase();
  const issueCards = document.querySelectorAll('.issue-card-large');
  
  if (!keyword) {
    // Hiển thị lại toàn bộ ấn phẩm
    issueCards.forEach(card => card.style.display = 'grid');
    showToast('Đang hiển thị toàn bộ ấn phẩm trong kho lưu trữ.');
    return;
  }

  let matchCount = 0;
  issueCards.forEach(card => {
    const text = card.textContent.toLowerCase();
    if (text.includes(keyword)) {
      card.style.display = 'grid';
      matchCount++;
    } else {
      card.style.display = 'none';
    }
  });

  if (matchCount > 0) {
    showToast(`Tìm thấy ${matchCount} số ấn phẩm / chuyên đề phù hợp với "${input.value.trim()}".`);
  } else {
    showToast(`Không tìm thấy kết quả phù hợp với từ khóa "${input.value.trim()}".`);
  }
}
