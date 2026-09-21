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
// API CLIENT - KẾT NỐI BACKEND ASP.NET CORE WEB API (.NET 9)
// =========================================================================
const API_BASE = window.location.port === '5000' 
  ? '/api' 
  : 'http://localhost:5000/api';

async function apiLogin(usernameOrEmail, password) {
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ usernameOrEmail, password })
  });
  return await res.json();
}

async function apiRegister(data) {
  try {
    const res = await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    const json = await res.json().catch(() => null);
    if (!res.ok) {
      return json || { success: false, message: `Lỗi máy chủ (${res.status}): Không thể hoàn tất đăng ký.` };
    }
    return json;
  } catch (e) {
    console.error('apiRegister error:', e);
    return { success: false, message: 'Không thể kết nối đến máy chủ Backend (http://localhost:5000). Vui lòng đảm bảo backend đang chạy.' };
  }
}

async function apiGetProfile() {
  const token = localStorage.getItem('journal_token');
  if (!token) return null;
  try {
    const res = await fetch(`${API_BASE}/auth/profile`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.status === 401) {
      localStorage.removeItem('journal_token');
      localStorage.removeItem('journal_user');
      return null;
    }
    return await res.json();
  } catch (e) {
    return null;
  }
}

async function apiUpdateProfile(data) {
  const token = localStorage.getItem('journal_token');
  const res = await fetch(`${API_BASE}/auth/profile`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(data)
  });
  return await res.json();
}

function getAvatarUrl(path) {
  if (!path) return '';
  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('data:')) {
    return path;
  }
  const host = (window.location.port === '5000') ? '' : 'http://localhost:5000';
  return host + (path.startsWith('/') ? path : ('/' + path));
}

async function apiUploadAvatar(file) {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Chưa đăng nhập hệ thống.' };
  try {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch(`${API_BASE}/auth/upload-avatar`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` },
      body: formData
    });
    const json = await res.json().catch(() => null);
    if (!res.ok) {
      return json || { success: false, message: `Lỗi máy chủ (${res.status}): Không thể tải ảnh lên.` };
    }
    return json;
  } catch (e) {
    console.error('apiUploadAvatar error:', e);
    return { success: false, message: 'Không thể kết nối đến máy chủ Backend để tải ảnh.' };
  }
}

async function apiDeleteAvatar() {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Chưa đăng nhập hệ thống.' };
  try {
    const res = await fetch(`${API_BASE}/auth/avatar`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const json = await res.json().catch(() => null);
    if (!res.ok) {
      return json || { success: false, message: `Lỗi máy chủ (${res.status}): Không thể xóa ảnh.` };
    }
    return json;
  } catch (e) {
    console.error('apiDeleteAvatar error:', e);
    return { success: false, message: 'Không thể kết nối đến máy chủ Backend để xóa ảnh.' };
  }
}

async function apiChangePassword(currentPassword, newPassword) {
  const token = localStorage.getItem('journal_token');
  const res = await fetch(`${API_BASE}/auth/change-password`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ currentPassword, newPassword })
  });
  return await res.json();
}

async function apiLookupUser(email) {
  if (!email) return null;
  try {
    const res = await fetch(`${API_BASE}/auth/lookup?email=${encodeURIComponent(email.trim())}`);
    if (!res.ok) return null;
    return await res.json();
  } catch (e) {
    return null;
  }
}

// -------------------------------------------------------------
// API MODULE BÀI BÁO (SUBMISSIONS & ARTICLES)
// -------------------------------------------------------------
async function apiSubmitPaper(formData) {
  const token = localStorage.getItem('journal_token');
  const res = await fetch(`${API_BASE}/baibao/submit`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });
  return await res.json();
}

async function apiGetMySubmissions() {
  const token = localStorage.getItem('journal_token');
  if (!token) return [];
  try {
    const res = await fetch(`${API_BASE}/baibao/my-submissions`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) return [];
    return await res.json();
  } catch (e) {
    return [];
  }
}

async function apiGetSubmissionDetail(id) {
  const token = localStorage.getItem('journal_token');
  if (!token) return null;
  try {
    const res = await fetch(`${API_BASE}/baibao/${id}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) return null;
    return await res.json();
  } catch (e) {
    return null;
  }
}

async function apiGetPublicArticle(id) {
  try {
    const res = await fetch(`${API_BASE}/baibao/public/${id}`);
    if (!res.ok) return null;
    return await res.json();
  } catch (e) {
    return null;
  }
}

async function apiGetLatestArticles(limit = 10) {
  try {
    const res = await fetch(`${API_BASE}/baibao/public/latest?limit=${limit}`);
    if (!res.ok) return [];
    return await res.json();
  } catch (e) {
    return [];
  }
}

// -------------------------------------------------------------
// API MODULE SỐ TẠP CHÍ & LƯU TRỮ (ARCHIVES & ISSUES)
// -------------------------------------------------------------
async function apiGetPublishedIssues() {
  try {
    const res = await fetch(`${API_BASE}/sotapchi`);
    if (!res.ok) return [];
    return await res.json();
  } catch (e) {
    return [];
  }
}

async function apiGetIssueDetail(id) {
  try {
    const res = await fetch(`${API_BASE}/sotapchi/${id}`);
    if (!res.ok) return null;
    return await res.json();
  } catch (e) {
    return null;
  }
}

// -------------------------------------------------------------
// API MODULE PHẢN BIỆN (PEER REVIEW WORKFLOW)
// -------------------------------------------------------------
async function apiAssignReviewer(data) {
  const token = localStorage.getItem('journal_token');
  const res = await fetch(`${API_BASE}/phanbien/assign`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(data)
  });
  return await res.json();
}

async function apiGetMyAssignments() {
  const token = localStorage.getItem('journal_token');
  if (!token) return [];
  try {
    const res = await fetch(`${API_BASE}/phanbien/my-assignments`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) return [];
    return await res.json();
  } catch (e) {
    return [];
  }
}

async function apiSubmitEvaluation(data) {
  const token = localStorage.getItem('journal_token');
  const res = await fetch(`${API_BASE}/phanbien/evaluate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(data)
  });
  return await res.json();
}

async function apiMakeDecision(data) {
  const token = localStorage.getItem('journal_token');
  const res = await fetch(`${API_BASE}/phanbien/decision`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(data)
  });
  return await res.json();
}

async function apiResubmitPaper(baiBaoId, formData) {
  const token = localStorage.getItem('journal_token');
  try {
    const res = await fetch(`${API_BASE}/baibao/${baiBaoId}/resubmit`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      },
      body: formData
    });
    return await res.json();
  } catch (e) {
    return { success: false, message: 'Không thể kết nối đến máy chủ Backend (http://localhost:5000).' };
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

// =============================================================================
// DỮ LIỆU DỰ PHÒNG DEMO NGOẠI TUYẾN (OFFLINE FALLBACK PRESETS)
// Chú thích kỹ thuật: Mảng SYSTEM_ACCOUNTS dưới đây CHỈ đóng vai trò dữ liệu
// dự phòng khi Backend API offline hoặc môi trường demo không có kết nối CSDL.
// Khi hệ thống hoạt động bình thường, 100% dữ liệu tài khoản và phân quyền
// được truy vấn trực tiếp từ bảng NguoiDung trong SQL Server 2022 qua RESTful API.
// =============================================================================
const SYSTEM_ACCOUNTS = [
  {
    id: 1,
    tenDangNhap: 'nguyenvana',
    hoTen: 'GS.TS. Nguyễn Văn A',
    email: 'nguyenvana@journal.vn',
    donVi: 'Trường Đại học Công Thương TP.HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Giáo sư',
    orcid: '0000-0001-2345-6789',
    vaiTro: 'Tổng biên tập'
  },
  {
    id: 2,
    tenDangNhap: 'tranthib',
    hoTen: 'ThS. Trần Thị B',
    email: 'tranthib@journal.vn',
    donVi: 'Phòng Quản lý Khoa học, Trường ĐH Công Thương TP.HCM',
    hocVi: 'Thạc sĩ',
    hocHam: 'Không',
    orcid: '0000-0002-3456-7890',
    vaiTro: 'Thư ký tòa soạn'
  },
  {
    id: 3,
    tenDangNhap: 'vuthif',
    hoTen: 'TS. Vũ Thị F',
    email: 'vuthif@huit.edu.vn',
    donVi: 'Khoa Công nghệ Thông tin, Trường ĐH Công Thương TP.HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Không',
    orcid: '0000-0002-1825-0097',
    vaiTro: 'Tác giả'
  },
  {
    id: 4,
    tenDangNhap: 'dangvang',
    hoTen: 'PGS.TS. Đặng Văn G',
    email: 'dangvang@vnuhcm.edu.vn',
    donVi: 'Viện Công nghệ Tiên tiến, ĐHQG-HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Phó giáo sư',
    orcid: '0000-0002-4512-8890',
    vaiTro: 'Phản biện viên',
    chuyenNganhIds: [1, 2],
    chuyenMonStr: 'Trí tuệ nhân tạo, Thị giác máy tính, Cơ điện tử robot'
  },
  {
    id: 5,
    tenDangNhap: 'tranthih',
    hoTen: 'TS. Trần Thị H',
    email: 'tranthih@ctub.edu.vn',
    donVi: 'Khoa Môi trường, Đại học Cần Thơ',
    hocVi: 'Tiến sĩ',
    hocHam: 'Không',
    orcid: '0000-0002-9912-3344',
    vaiTro: 'Phản biện viên',
    chuyenNganhIds: [3, 5],
    chuyenMonStr: 'Kỹ thuật môi trường, Hợp chất sinh học, Màng bao thực phẩm'
  },
  {
    id: 6,
    tenDangNhap: 'nguyenvank',
    hoTen: 'TS. Nguyễn Văn K',
    email: 'nguyenvank@hcmut.edu.vn',
    donVi: 'Khoa Cơ khí, Đại học Bách Khoa TP.HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Không',
    orcid: '0000-0003-1122-3344',
    vaiTro: 'Phản biện viên',
    chuyenNganhIds: [2],
    chuyenMonStr: 'Gia công cơ khí chính xác, Tối ưu hóa Taguchi, Robot công nghiệp'
  },
  {
    id: 7,
    tenDangNhap: 'lythim',
    hoTen: 'TS. Lý Thị M',
    email: 'lythim@huit.edu.vn',
    donVi: 'Khoa Cơ khí, Trường ĐH Công Thương TP.HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Không',
    orcid: '0000-0002-3344-5566',
    vaiTro: 'Tác giả'
  },
  {
    id: 8,
    tenDangNhap: 'tranvann',
    hoTen: 'TS. Trần Văn N',
    email: 'tranvann@huit.edu.vn',
    donVi: 'Khoa CNTT, Trường ĐH Công Thương TP.HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Không',
    orcid: '0000-0001-5566-7788',
    vaiTro: 'Phản biện viên',
    chuyenNganhIds: [1],
    chuyenMonStr: 'Xử lý ngôn ngữ tự nhiên, Học sâu, Dữ liệu lớn'
  },
  {
    id: 9,
    tenDangNhap: 'phamthiq',
    hoTen: 'TS. Phạm Thị Q',
    email: 'phamthiq@ueh.edu.vn',
    donVi: 'Đại học Kinh tế TP.HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Không',
    orcid: '0000-0002-7788-9900',
    vaiTro: 'Phản biện viên',
    chuyenNganhIds: [4],
    chuyenMonStr: 'Kinh tế số, Quản trị chuỗi cung ứng, Tài chính doanh nghiệp'
  },
  {
    id: 10,
    tenDangNhap: 'dangthanhthi',
    hoTen: 'TS. Đặng Thành Thi',
    email: 'dangthanhthi@huit.edu.vn',
    donVi: 'Khoa CNTT, Trường ĐH Công Thương TP.HCM',
    hocVi: 'Tiến sĩ',
    hocHam: 'Không',
    orcid: '0000-0003-8899-0011',
    vaiTro: 'Phản biện viên',
    chuyenNganhIds: [1, 5],
    chuyenMonStr: 'Blockchain, IoT, Bảo quản nông sản thực phẩm'
  }
];

// Hàm tra cứu tài khoản hệ thống phục vụ Auto-match đồng tác giả
// Ưu tiên 1: Tra cứu trực tiếp từ CSDL SQL Server qua API /api/auth/lookup
// Ưu tiên 2: Dự phòng ngoại tuyến từ SYSTEM_ACCOUNTS khi mất kết nối mạng
async function findAccountByEmail(email) {
  if (!email) return null;
  const cleanEmail = email.trim().toLowerCase();

  // 1. Tra cứu trực tiếp thời gian thực từ CSDL SQL Server
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

  // 2. Dự phòng ngoại tuyến (Offline fallback)
  return SYSTEM_ACCOUNTS.find(acc => acc.email.toLowerCase() === cleanEmail) || null;
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
            <a href="submit-paper.html" class="dropdown-item">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="12" y1="18" x2="12" y2="12"></line><line x1="9" y1="15" x2="15" y2="15"></line></svg>
              Gửi bản thảo bài báo mới
            </a>
            <a href="profile.html#tab-password" class="dropdown-item">
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
              <input type="email" required placeholder="nhapemail@journal.edu.vn" value="vuthif@journal.edu.vn">
            </div>
            <div class="auth-form-group">
              <label>Mật khẩu</label>
              <input type="password" required placeholder="••••••••" value="MatKhau@123">
            </div>
            <div class="auth-form-group">
              <label>Phân hệ truy cập</label>
              <select>
                <option value="author">Tác giả (Nộp bài & Theo dõi bản thảo)</option>
                <option value="reviewer">Phản biện viên (Chuyên gia đánh giá)</option>
                <option value="editor">Biên tập viên / Thư ký tòa soạn</option>
                <option value="reader">Bạn đọc / Nghiên cứu sinh</option>
              </select>
            </div>
            <button type="submit" class="auth-submit-btn">Đăng nhập vào hệ thống</button>
          </form>

          <!-- FORM ĐĂNG KÝ -->
          <form id="auth-register-form" style="display:none;" onsubmit="handleRegisterSubmit(event)">
            <div class="auth-form-group">
              <label>Họ và tên đầy đủ</label>
              <input type="text" required placeholder="Ví dụ: TS. Vũ Thị F">
            </div>
            <div class="auth-form-group">
              <label>Email liên hệ học thuật</label>
              <input type="email" required placeholder="name@institution.edu.vn">
            </div>
            <div class="auth-form-group">
              <label>Cơ quan công tác / Trường đại học</label>
              <input type="text" required placeholder="Trường Đại học">
            </div>
            <div class="auth-form-group">
              <label>Mã định danh khoa học ORCID (tùy chọn)</label>
              <input type="text" placeholder="0000-0002-XXXX-XXXX">
            </div>
            <div class="auth-form-group">
              <label>Mật khẩu mới</label>
              <input type="password" required placeholder="Tối thiểu 8 ký tự">
            </div>
            <button type="submit" class="auth-submit-btn">Hoàn tất đăng ký tài khoản</button>
          </form>
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

function handleLoginSubmit(e) {
  e.preventDefault();
  closeModal('modal-auth');
  showToast('Đăng nhập thành công! Chào mừng tác giả Vũ Thị F quay lại.');
}

function handleRegisterSubmit(e) {
  e.preventDefault();
  closeModal('modal-auth');
  showToast('Đăng ký tài khoản thành công! Tòa soạn đã gửi email kích hoạt.');
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
  apa: `Vũ, T. F., & Đặng, V. G. (2026). Ứng dụng mô hình Transformer trong phân loại văn bản tiếng Việt quy mô lớn. Tạp chí Khoa học & Công nghệ, 15(42), 45–58. https://doi.org/10.59876/jst.jsc.2026.42.04`,
  bibtex: `@article{vu2026transformer,
  title={Ứng dụng mô hình Transformer trong phân loại văn bản tiếng Việt quy mô lớn},
  author={Vũ Thị F and Đặng Văn G},
  journal={Tạp chí Khoa học & Công nghệ Trường Đại học},
  volume={15},
  number={42},
  pages={45--58},
  year={2026},
  publisher={JST Press},
  doi={10.59876/jst.jsc.2026.42.04}
}`,
  ris: `TY  - JOUR
TI  - Ứng dụng mô hình Transformer trong phân loại văn bản tiếng Việt quy mô lớn
AU  - Vũ, Thị F
AU  - Đặng, Văn G
JO  - Tạp chí Khoa học & Công nghệ
VL  - 15
IS  - 42
SP  - 45
EP  - 58
PY  - 2026
DO  - 10.59876/jst.jsc.2026.42.04
ER  - `
};

let currentCiteFormat = 'apa';

function switchCiteFormat(format) {
  currentCiteFormat = format;
  document.querySelectorAll('.cite-tab-btn').forEach(b => b.classList.remove('active'));
  event.target.classList.add('active');
  const display = document.getElementById('citation-content-display');
  if (display) {
    display.textContent = citations[format];
  }
}

function copyCitation() {
  const text = citations[currentCiteFormat];
  navigator.clipboard.writeText(text).then(() => {
    showToast('Đã sao chép trích dẫn vào bộ nhớ tạm (Clipboard)!');
  }).catch(() => {
    showToast('Đã chọn toàn bộ trích dẫn, vui lòng nhấn Ctrl+C để sao chép.');
  });
}

function downloadCitationFile() {
  const ext = currentCiteFormat === 'bibtex' ? 'bib' : (currentCiteFormat === 'ris' ? 'ris' : 'txt');
  const blob = new Blob([citations[currentCiteFormat]], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `citation-transformer-2026.${ext}`;
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
        display.textContent = citations.apa;
      }
      openModal('modal-citation');
    });
  });
}

// Download PDF simulator
function initDownloadTriggers() {
  document.querySelectorAll('.btn-download-pdf, a[href="#download-pdf"]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      showToast('Đang khởi tạo tải xuống bản toàn văn PDF chính thức (1.4 MB)...');
      setTimeout(() => {
        // Tải file thông báo mẫu hoặc bản tóm tắt thực tế
        const dummyPdfContent = `%PDF-1.4\n%Scientific Journal System\n1 0 obj\n<< /Title (Ung dung mo hinh Transformer trong phan loai van ban tieng Viet quy mo lon)\n/Author (Vu Thi F, Dang Van G)\n/Journal (Tap chi Khoa hoc Dai hoc Cong Thuong - p-ISSN 3030-4113 e-ISSN 3030-413X)\n>>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF`;
        const blob = new Blob([dummyPdfContent], { type: 'application/pdf' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = 'HUIT_Journal_2026_Vol15_No42_Transformer_Classification.pdf';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        showToast('Tải xuống toàn văn bài báo hoàn tất!');
      }, 700);
    });
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
      if (input && input.value.trim()) {
        showToast(`Đã ghi nhận đăng ký cho email ${input.value.trim()}! Tòa soạn sẽ gửi mục lục số mới nhất.`);
        input.value = '';
      } else {
        showToast('Vui lòng nhập địa chỉ email hợp lệ trước khi bấm đăng ký.');
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
