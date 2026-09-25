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
// API CLIENT & STANDALONE ENGINE (HUIT JOURNAL ONLINE / OFFLINE HYBRID)
// Tự động kết nối Backend C# ASP.NET Core (.NET 9) khi có sẵn (localhost / tunnel),
// và tự động kích hoạt Standalone Local Engine khi chạy trên Vercel / điện thoại mà máy tính tắt.
// =========================================================================
const CUSTOM_API_URL = (typeof localStorage !== 'undefined') ? localStorage.getItem('huit_api_url') : null;
const API_BASE = CUSTOM_API_URL 
  ? CUSTOM_API_URL.replace(/\/$/, '')
  : (window.location.port === '5000' ? '/api' : 'http://localhost:5000/api');

async function fetchWithTimeout(resource, options = {}) {
  const { timeout = 2500, ...restOptions } = options;
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

// Danh sách tài khoản mẫu của Hội đồng biên tập & Tác giả Tạp chí HUIT
const STANDALONE_DEFAULT_ACCOUNTS = [
  {
    maNguoiDung: 99,
    hoTen: "GS.TS. Đặng Thành Thi",
    tenDangNhap: "dangthanhthi",
    email: "dangthanhthi2134@gmail.com",
    donVi: "Khoa Công nghệ Thông tin, Trường ĐH Công Thương TP.HCM",
    hocVi: "Tiến sĩ",
    hocHam: "Giáo sư",
    vaiTros: ["Tác giả", "Chuyên gia phản biện", "Độc giả"],
    chucVu: "Tác giả, Chuyên gia phản biện, Độc giả",
    chuyenMonIds: [1],
    maORCID: "0000-0002-1825-0097",
    soDienThoai: "0984012345",
    soTaiKhoan: "1234567890",
    nganHang: "Vietcombank",
    gioiTinh: "Nam",
    ngonNgu: "Tiếng Việt",
    quocGia: "Vietnam"
  },
  {
    maNguoiDung: 1,
    hoTen: "TS. Vũ Thị F",
    tenDangNhap: "vuthif",
    email: "vuthif@huit.edu.vn",
    donVi: "Khoa Công nghệ Thông tin, Trường ĐH Công Thương TP.HCM",
    hocVi: "Tiến sĩ",
    hocHam: "Không",
    vaiTros: ["Tác giả", "Độc giả"],
    chucVu: "Tác giả, Độc giả",
    chuyenMonIds: [1],
    maORCID: "0000-0002-1825-0097",
    soDienThoai: "0901234567",
    soTaiKhoan: "9876543210",
    nganHang: "BIDV",
    gioiTinh: "Nữ",
    ngonNgu: "Tiếng Việt",
    quocGia: "Vietnam"
  },
  {
    maNguoiDung: 2,
    hoTen: "TS. Bùi Hồng Đăng",
    tenDangNhap: "buihongdang",
    email: "buihongdang@huit.edu.vn",
    donVi: "Hội đồng trường, Trường ĐH Công Thương TP.HCM",
    hocVi: "Tiến sĩ",
    hocHam: "Không",
    vaiTros: ["Tổng biên tập", "Tác giả", "Độc giả"],
    chucVu: "Tổng biên tập, Tác giả, Độc giả",
    chuyenMonIds: [4],
    maORCID: "0000-0003-9999-1234",
    soDienThoai: "02838163318",
    soTaiKhoan: "",
    nganHang: "",
    gioiTinh: "Nam",
    ngonNgu: "Tiếng Việt",
    quocGia: "Vietnam"
  },
  {
    maNguoiDung: 3,
    hoTen: "PGS.TS. Nguyễn Xuân Hoàn",
    tenDangNhap: "nguyenxuanhoan",
    email: "nguyenhoan@huit.edu.vn",
    donVi: "Ban Giám hiệu, Trường ĐH Công Thương TP.HCM",
    hocVi: "Tiến sĩ",
    hocHam: "Phó giáo sư",
    vaiTros: ["Chuyên gia phản biện", "Tác giả", "Độc giả"],
    chucVu: "Chuyên gia phản biện, Tác giả, Độc giả",
    chuyenMonIds: [3],
    maORCID: "0000-0001-5555-8888",
    soDienThoai: "02838163318",
    soTaiKhoan: "",
    nganHang: "",
    gioiTinh: "Nam",
    ngonNgu: "Tiếng Việt",
    quocGia: "Vietnam"
  }
];

// Danh sách bản thảo mẫu thời gian thực khi chạy độc lập (Vercel Standalone)
const STANDALONE_DEFAULT_SUBMISSIONS = [
  {
    maBaiBao: 101,
    maDinhDanh: 'JST-2026-01',
    tieuDe: 'Nghiên cứu cấu trúc phân tử và hoạt tính kháng oxy hóa của các hợp chất tự nhiên chiết xuất từ thực vật Việt Nam',
    tieuDeTiengAnh: 'Study on Molecular Structure and Antioxidant Activity of Natural Compounds Extracted from Vietnamese Flora',
    chuyenNganh: 'Hóa học & Công nghệ thực phẩm',
    maChuyenNganh: 5,
    trangThai: 'Đang phản biện kín',
    ngayGui: '2026-09-02T08:30:00',
    ngayCapNhat: '2026-09-15T14:20:00',
    soDongTacGia: 3,
    tapTinGoc: 'BanThao_Goc_JST_2026_01.docx',
    tacGiaChinh: 'GS.TS. Đặng Thành Thi',
    emailTacGiaChinh: 'dangthanhthi2134@gmail.com',
    tomTat: 'Nghiên cứu tập trung vào phân tích cấu trúc không gian và đánh giá hoạt tính sinh học của các hợp chất phenolic tự nhiên.',
    tuKhoa: 'Kháng oxy hóa; Hợp chất tự nhiên; Hóa thực phẩm; Chiết xuất'
  },
  {
    maBaiBao: 102,
    maDinhDanh: 'JST-2026-02',
    tieuDe: 'Ứng dụng mô hình học sâu Transformer trong nhận dạng và phân loại lỗi bề mặt sản phẩm cơ khí chính xác',
    tieuDeTiengAnh: 'Application of Deep Learning Transformer Models in Detecting and Classifying Surface Defects of Precision Mechanical Products',
    chuyenNganh: 'Cơ khí – Chế tạo máy – Tự động hóa',
    maChuyenNganh: 2,
    trangThai: 'Chờ tác giả chỉnh sửa',
    ngayGui: '2026-08-20T10:15:00',
    ngayCapNhat: '2026-09-18T09:40:00',
    soDongTacGia: 2,
    tapTinGoc: 'BanThao_Transformer_Mechatronics.docx',
    tacGiaChinh: 'GS.TS. Đặng Thành Thi',
    emailTacGiaChinh: 'dangthanhthi2134@gmail.com',
    nhanXetPhanBien: 'Hội đồng phản biện yêu cầu: Bổ sung ma trận nhầm lẫn (confusion matrix), làm rõ thời gian đáp ứng thời gian thực (inference latency) của mô hình trên phần cứng nhúng và hoàn thiện Bảng giải trình tiếp thu (BM-03).',
    tomTat: 'Bài báo đề xuất kiến trúc mạng nơ-ron tích chập kết hợp cơ chế chú ý (Self-Attention) để phát hiện vi khuyết tật trên kim loại.',
    tuKhoa: 'Học sâu; Transformer; Thị giác máy tính; Kiểm định chất lượng'
  },
  {
    maBaiBao: 103,
    maDinhDanh: 'JST-2026-03',
    tieuDe: 'Thuật toán tối ưu hóa bầy đàn thích nghi đa mục tiêu cho bài toán lập lịch dây chuyền sản xuất may mặc',
    tieuDeTiengAnh: 'Adaptive Multi-Objective Particle Swarm Optimization for Garment Production Line Scheduling',
    chuyenNganh: 'Công nghệ thông tin & Trí tuệ nhân tạo',
    maChuyenNganh: 1,
    trangThai: 'Đã xuất bản',
    ngayGui: '2026-06-10T14:00:00',
    ngayCapNhat: '2026-08-01T16:30:00',
    soDongTacGia: 1,
    tapTinGoc: 'BanThao_ChinhThuc_XuatBan_2026.pdf',
    tacGiaChinh: 'GS.TS. Đặng Thành Thi',
    emailTacGiaChinh: 'dangthanhthi2134@gmail.com',
    tomTat: 'Đề xuất thuật toán AMPSO giải quyết xung đột thời gian gia công và chi phí chuyển đổi công đoạn trong nhà máy dệt may.',
    tuKhoa: 'Tối ưu hóa bầy đàn; Lập lịch sản xuất; Trí tuệ nhân tạo; Công nghệ thông tin'
  }
];

function isExplicitDemoMode() {
  const urlParams = new URLSearchParams(window.location.search);
  const modeParam = urlParams.get('mode');
  if (modeParam === 'demo') {
    sessionStorage.setItem('journal_app_mode', 'demo');
    return true;
  }
  if (modeParam === 'online') {
    sessionStorage.removeItem('journal_app_mode');
    return false;
  }
  if (sessionStorage.getItem('journal_app_mode') === 'demo') {
    return true;
  }
  if (window.location.hostname.includes('vercel.app') || window.location.hostname.includes('github.io')) {
    return true;
  }
  return false;
}

function renderDemoModeBanner() {
  if (isExplicitDemoMode()) {
    if (!document.getElementById('demo-mode-persistent-badge') && document.body) {
      const b = document.createElement('div');
      b.id = 'demo-mode-persistent-badge';
      b.style.cssText = 'position:fixed;bottom:12px;left:12px;background:#c53030;color:#fff;padding:6px 14px;border-radius:20px;font-size:12px;font-weight:700;z-index:999999;box-shadow:0 4px 12px rgba(0,0,0,0.3);display:flex;align-items:center;gap:8px;font-family:Inter,sans-serif;';
      b.innerHTML = '<span>● CHẾ ĐỘ MÔ PHỎNG DEMO</span><a href="?mode=online" style="color:#fff;text-decoration:underline;font-size:11px;opacity:0.9;margin-left:4px;">Chuyển Online</a>';
      document.body.appendChild(b);
    }
  }
}
if (typeof window !== 'undefined') {
  window.addEventListener('DOMContentLoaded', renderDemoModeBanner);
}

async function apiLogin(usernameOrEmail, password) {
  const cleanInput = (usernameOrEmail || '').trim().toLowerCase();

  // 1. Chế độ Online chính thức: Bắt buộc kết nối Backend C# thật
  if (!isExplicitDemoMode()) {
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
      // Khi ở phiên thật, nếu API lỗi mạng -> DỪNG LẠI Ở LỖI, KHÔNG tự ý rơi vào chế độ demo
      return {
        success: false,
        message: 'Không thể kết nối đến máy chủ Tòa soạn (Backend API). Vui lòng kiểm tra lại dịch vụ Backend đang chạy tại http://localhost:5000 hoặc chuyển sang chế độ Demo mô phỏng (?mode=demo).'
      };
    }
  }

  // 2. Chế độ Demo mô phỏng (Chỉ chạy khi có cờ ?mode=demo hoặc trên Vercel)
  let localUsers = [];
  try { localUsers = JSON.parse(localStorage.getItem('huit_standalone_users') || '[]'); } catch(e){}

  let matched = localUsers.find(u => 
    (u.email && u.email.toLowerCase() === cleanInput) ||
    (u.tenDangNhap && u.tenDangNhap.toLowerCase() === cleanInput)
  );

  if (!matched) {
    matched = STANDALONE_DEFAULT_ACCOUNTS.find(u =>
      (u.email && u.email.toLowerCase() === cleanInput) ||
      (u.tenDangNhap && u.tenDangNhap.toLowerCase() === cleanInput)
    );
  }

  if (matched) {
    const inputHash = 'bcr_sha_' + btoa(unescape(encodeURIComponent(password || '')));
    if (matched.passwordHash && matched.passwordHash !== inputHash) {
      return { success: false, message: 'Mật khẩu truy cập Demo không chính xác.' };
    } else if (matched.password && matched.password !== password) {
      return { success: false, message: 'Mật khẩu truy cập Demo không chính xác.' };
    }
    const token = 'standalone_token_' + Date.now();
    const cleanUser = { ...matched };
    delete cleanUser.password;
    delete cleanUser.passwordHash;
    const userObj = {
      isLoggedIn: true,
      ...cleanUser,
      chucVu: (matched.vaiTros && matched.vaiTros.length > 0) ? matched.vaiTros.join(', ') : 'Tác giả'
    };
    return {
      success: true,
      token: token,
      user: userObj,
      message: `Đăng nhập thành công (Chế độ mô phỏng Demo)! Xin chào: ${matched.hoTen}.`
    };
  }

  return {
    success: false,
    message: 'Tài khoản hoặc mật khẩu không chính xác trong kho tài khoản Demo.'
  };
}

async function apiRegister(data) {
  // 1. Chế độ Online chính thức: Bắt buộc gửi lên Backend C# thật
  if (!isExplicitDemoMode()) {
    try {
      const res = await fetchWithTimeout(`${API_BASE}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      });
      const json = await res.json().catch(() => null);
      if (res.ok && json && json.success) {
        return json;
      }
      return {
        success: false,
        message: json?.message || 'Không thể đăng ký tài khoản. Vui lòng kiểm tra lại thông tin.'
      };
    } catch (e) {
      // Khi phiên thật gặp lỗi API -> DỪNG LẠI Ở LỖI, TUYỆT ĐỐI KHÔNG ghi thông tin thật vào demo localStorage
      return {
        success: false,
        message: 'Không thể kết nối đến máy chủ Tòa soạn để ghi nhận đăng ký. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau.'
      };
    }
  }

  // 2. Chế độ Demo mô phỏng (Chỉ chạy khi có cờ ?mode=demo hoặc trên Vercel)
  const newUserId = Math.floor(Math.random() * 90000) + 1000;
  const hocVi = data.hocVi || 'Không';
  const hocHam = data.hocHam || 'Không';

  // Theo chuẩn COPE: Người đăng ký mới chỉ nhận vai trò Tác giả và Độc giả
  const vaiTros = ['Tác giả', 'Độc giả'];

  const fullName = (data.hoDem ? (data.hoDem + ' ' + data.ten) : (data.hoTen || '')).trim();

  const newUser = {
    maNguoiDung: newUserId,
    hoTen: fullName || 'Tác giả HUIT (Demo)',
    tenDangNhap: data.tenDangNhap || (data.email ? data.email.split('@')[0] : 'user' + newUserId),
    email: data.email,
    passwordHash: 'bcr_sha_' + btoa(unescape(encodeURIComponent(data.password || ''))),
    hocVi: hocVi,
    hocHam: hocHam,
    donVi: data.donVi || 'Trường Đại học Công Thương TP.HCM',
    diaChi: data.diaChi || 'TP. Hồ Chí Minh',
    soDienThoai: data.soDienThoai || '',
    gioiTinh: data.gioiTinh || 'Nam',
    quocGia: data.quocGia || 'Vietnam',
    ngonNgu: data.ngonNgu || 'Tiếng Việt',
    soTaiKhoan: data.soTaiKhoan || '',
    chuTaiKhoan: data.chuTaiKhoan || '',
    nganHang: data.nganHang || '',
    maORCID: data.maORCID || '',
    chuyenMonIds: [parseInt(data.chuyenNganhId) || 1],
    vaiTros: vaiTros,
    chucVu: vaiTros.join(', ')
  };

  let localUsers = [];
  try { localUsers = JSON.parse(localStorage.getItem('huit_standalone_users') || '[]'); } catch(e){}
  localUsers.push(newUser);
  localStorage.setItem('huit_standalone_users', JSON.stringify(localUsers));

  const token = 'standalone_token_' + Date.now();
  const reviewerNotice = data.dangKyPhanBien
    ? ' (Đơn xin tham gia phản biện đã được chuyển đến Ban biên tập để thẩm định hồ sơ).'
    : '';

  const cleanUser = { ...newUser };
  delete cleanUser.passwordHash;

  return {
    success: true,
    token: token,
    user: cleanUser,
    message: `Đăng ký tài khoản thành công (Chế độ mô phỏng Demo)! Xin chào mừng: ${newUser.hoTen}.${reviewerNotice}`
  };
}

async function apiGetProfile() {
  const token = localStorage.getItem('journal_token');
  if (!token) return null;

  // Nếu là phiên làm việc độc lập Standalone, nạp trực tiếp mà không cần chờ Backend
  if (token.startsWith('standalone_token_')) {
    return getCurrentUser() || null;
  }

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
    // Backend offline: Duy trì phiên người dùng hiện tại, không đăng xuất
    return getCurrentUser();
  }
  return getCurrentUser();
}

async function apiUpdateProfile(data) {
  const token = localStorage.getItem('journal_token');
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/profile`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(data)
    });
    if (res.ok) {
      return await res.json();
    }
  } catch (e) {
    console.log('Backend offline: Cập nhật hồ sơ cục bộ (Standalone)');
  }

  let user = getCurrentUser() || {};
  user = { ...user, ...data };
  setCurrentUser(user);
  return { success: true, message: 'Cập nhật hồ sơ thành công!', profile: user };
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

  // 1. Thử gửi lên Backend C# thật
  try {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetchWithTimeout(`${API_BASE}/auth/upload-avatar`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` },
      body: formData
    });
    const json = await res.json().catch(() => null);
    if (res.ok && json && json.success) {
      return json;
    }
  } catch (e) {
    console.log('Backend offline / Vercel cloud mode: Kích hoạt Standalone Avatar Engine');
  }

  // 2. Chuyển tệp thành Base64 Data URL để lưu trữ và hiển thị trực tiếp trên trình duyệt
  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onload = function(e) {
      const base64Url = e.target.result;
      let user = getCurrentUser() || {};
      user.anhDaiDien = base64Url;
      setCurrentUser(user);
      resolve({
        success: true,
        avatarUrl: base64Url,
        message: 'Cập nhật ảnh đại diện thành công!'
      });
    };
    reader.onerror = function() {
      resolve({ success: false, message: 'Không thể xử lý tệp ảnh.' });
    };
    reader.readAsDataURL(file);
  });
}

async function apiDeleteAvatar() {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Chưa đăng nhập hệ thống.' };

  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/avatar`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const json = await res.json().catch(() => null);
    if (res.ok && json && json.success) {
      return json;
    }
  } catch (e) {
    console.log('Backend offline: Xóa ảnh đại diện cục bộ');
  }

  let user = getCurrentUser() || {};
  user.anhDaiDien = null;
  setCurrentUser(user);
  return { success: true, message: 'Đã xóa ảnh đại diện thành công.' };
}

async function apiChangePassword(currentPassword, newPassword) {
  const token = localStorage.getItem('journal_token');
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
    if (res.ok) {
      return { success: true, message: data.message || 'Đổi mật khẩu thành công.' };
    } else if (res.status === 400 || res.status === 401) {
      const msg = data.message || (res.status === 401 ? 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.' : 'Mật khẩu hiện tại không chính xác.');
      return { success: false, message: msg };
    }
  } catch (e) {
    console.log('Backend offline / Vercel cloud mode: Kích hoạt Standalone Password Engine');
  }

  // Cập nhật mật khẩu trong phiên Standalone
  const currentUser = getCurrentUser();
  if (currentUser) {
    let localUsers = [];
    try { localUsers = JSON.parse(localStorage.getItem('huit_standalone_users') || '[]'); } catch(e){}
    const idx = localUsers.findIndex(u => u.email === currentUser.email || u.tenDangNhap === currentUser.tenDangNhap);
    if (idx !== -1) {
      localUsers[idx].passwordHash = 'bcr_sha_' + btoa(unescape(encodeURIComponent(newPassword || '')));
      delete localUsers[idx].password;
      localStorage.setItem('huit_standalone_users', JSON.stringify(localUsers));
    }
  }

  return {
    success: true,
    message: 'Đổi mật khẩu tài khoản thành công! Thông tin bảo mật đã được cập nhật.'
  };
}

async function apiLookupUser(email) {
  if (!email) return null;
  const clean = email.trim().toLowerCase();
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/lookup?email=${encodeURIComponent(clean)}`);
    if (res.ok) return await res.json();
  } catch (e) {
    // Offline lookup
  }

  let localUsers = [];
  try { localUsers = JSON.parse(localStorage.getItem('huit_standalone_users') || '[]'); } catch(e){}
  const found = localUsers.find(u => u.email && u.email.toLowerCase() === clean) ||
                STANDALONE_DEFAULT_ACCOUNTS.find(u => u.email && u.email.toLowerCase() === clean);
  if (found) {
    return {
      maNguoiDung: found.maNguoiDung,
      hoTen: found.hoTen,
      email: found.email,
      donVi: found.donVi,
      maORCID: found.maORCID
    };
  }
  return null;
}

// -------------------------------------------------------------
// API MODULE YÊU CẦU NÂNG CẤP VAI TRÒ (REVIEWER ROLE UPGRADE)
// -------------------------------------------------------------
async function apiRequestReviewerRole() {
  const token = localStorage.getItem('journal_token');
  if (!token) return { success: false, message: 'Chưa đăng nhập hệ thống.' };

  // 1. Standalone mode: Cập nhật trực tiếp vào tài khoản đang lưu
  if (token.startsWith('standalone_token_')) {
    let user = getCurrentUser() || {};
    let roles = user.vaiTros || ['Tác giả'];
    if (!roles.includes('Chuyên gia phản biện') && !roles.includes('Phản biện viên')) {
      roles.push('Chuyên gia phản biện');
      user.vaiTros = roles;
      user.chucVu = roles.join(', ');
      setCurrentUser(user);

      let localUsers = [];
      try { localUsers = JSON.parse(localStorage.getItem('huit_standalone_users') || '[]'); } catch(e){}
      const idx = localUsers.findIndex(u => (u.email && u.email === user.email) || (u.tenDangNhap && u.tenDangNhap === user.tenDangNhap));
      if (idx !== -1) {
        localUsers[idx].vaiTros = roles;
        localUsers[idx].chucVu = user.chucVu;
        localStorage.setItem('huit_standalone_users', JSON.stringify(localUsers));
      }
    }
    return { success: true, message: 'Đã thêm vai trò Chuyên gia Phản biện vào tài khoản của bạn!' };
  }

  // 2. Thử gọi backend API C#
  try {
    const res = await fetchWithTimeout(`${API_BASE}/auth/request-reviewer`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      }
    });
    const data = await res.json().catch(() => ({}));
    if (res.ok) {
      return { success: true, message: data.message || 'Đã thêm vai trò Chuyên gia Phản biện vào tài khoản của bạn!' };
    }
    return { success: false, message: data.message || 'Không thể thêm vai trò. Vui lòng liên hệ Ban biên tập.' };
  } catch (e) {
    // Khi phiên Online bị mất kết nối, báo lỗi rõ ràng, không tự cấp quyền giả
    return { success: false, message: 'Không thể kết nối đến máy chủ tòa soạn để gửi đơn đăng ký. Vui lòng thử lại sau.' };
  }
}

// -------------------------------------------------------------
// API MODULE BÀI BÁO (SUBMISSIONS & ARTICLES)
// -------------------------------------------------------------
async function apiSubmitPaper(formData) {
  const token = localStorage.getItem('journal_token');
  const isStandalone = !token || token.startsWith('standalone_token_');

  // 1. Chế độ Online: Gửi trực tiếp lên máy chủ Web API thực tế
  if (!isStandalone) {
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

  // Standalone Engine: Lưu bản thảo mới vào danh sách bài nộp trên trình duyệt
  const user = getCurrentUser() || {};
  const newId = Math.floor(Math.random() * 900) + 100;
  const code = 'JST-2026-' + (newId < 10 ? '0' + newId : newId);
  const titleVi = formData.get('TieuDe') || 'Bản thảo bài báo khoa học mới nộp';
  const titleEn = formData.get('TieuDeTiengAnh') || '';
  const majorId = parseInt(formData.get('MaChuyenNganh')) || 1;
  const majorNames = {
    1: 'Công nghệ thông tin & Trí tuệ nhân tạo',
    2: 'Cơ khí – Chế tạo máy – Tự động hóa',
    3: 'Khoa học Môi trường & Nông nghiệp',
    4: 'Kinh tế – Quản trị kinh doanh & Tài chính',
    5: 'Hóa học & Công nghệ thực phẩm'
  };

  const newSubmission = {
    maBaiBao: newId,
    maDinhDanh: code,
    tieuDe: titleVi,
    tieuDeTiengAnh: titleEn,
    chuyenNganh: majorNames[majorId] || 'Công nghệ thông tin & Trí tuệ nhân tạo',
    maChuyenNganh: majorId,
    trangThai: 'Chờ sơ duyệt',
    ngayGui: new Date().toISOString(),
    ngayCapNhat: new Date().toISOString(),
    soDongTacGia: 1,
    tapTinGoc: formData.get('TapTinBanThao')?.name || 'BanThao_Goc.docx',
    tacGiaChinh: user.hoTen || 'Tác giả chính',
    emailTacGiaChinh: user.email || 'tacgia@huit.edu.vn',
    tomTat: formData.get('TomTat') || '',
    tuKhoa: formData.get('TuKhoa') || ''
  };

  let localSubs = [];
  try { localSubs = JSON.parse(localStorage.getItem('huit_standalone_submissions') || '[]'); } catch(e){}
  localSubs.unshift(newSubmission);
  localStorage.setItem('huit_standalone_submissions', JSON.stringify(localSubs));

  return {
    success: true,
    maBaiBao: newId,
    maDinhDanh: code,
    message: 'Nộp bản thảo bài báo thành công (Chế độ trình diễn mô phỏng trên trình duyệt)!'
  };
}

async function apiGetMySubmissions() {
  const token = localStorage.getItem('journal_token');
  if (!token) return [];
  try {
    const res = await fetchWithTimeout(`${API_BASE}/baibao/my-submissions`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) return data;
    }
  } catch (e) {
    // Backend offline
  }

  // Kết hợp bài nộp độc lập đã lưu trong localStorage và danh sách mặc định
  let localSubs = [];
  try { localSubs = JSON.parse(localStorage.getItem('huit_standalone_submissions') || '[]'); } catch(e){}
  return [...localSubs, ...STANDALONE_DEFAULT_SUBMISSIONS];
}

async function apiGetSubmissionDetail(id) {
  const token = localStorage.getItem('journal_token');
  try {
    const res = await fetchWithTimeout(`${API_BASE}/baibao/${id}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.ok) return await res.json();
  } catch (e) {
    // Backend offline
  }

  // Tìm trong danh sách độc lập
  const all = await apiGetMySubmissions();
  const found = all.find(s => s.maBaiBao === id || s.maBaiBao === parseInt(id));
  if (found) {
    return {
      ...found,
      chuyenGiaDeXuats: [
        { stt: 1, hoTen: 'PGS.TS. Trần Văn M', hocVi: 'Tiến sĩ', hocHam: 'Phó giáo sư', donVi: 'Trường ĐH Bách Khoa TP.HCM', email: 'tranvanm@hcmut.edu.vn' },
        { stt: 2, hoTen: 'TS. Lê Thị N', hocVi: 'Tiến sĩ', hocHam: 'Không', donVi: 'Trường ĐH Khoa học Tự nhiên TP.HCM', email: 'lethin@hcmus.edu.vn' }
      ]
    };
  }
  return null;
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
    const res = await fetchWithTimeout(`${API_BASE}/baibao/public/latest?limit=${limit}`);
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

// -------------------------------------------------------------
// API MODULE PHẢN BIỆN (PEER REVIEW WORKFLOW)
// -------------------------------------------------------------
async function apiAssignReviewer(data) {
  const token = localStorage.getItem('journal_token');
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
  } catch (e) {
    console.log('Backend offline: Phân công chuyên gia phản biện trong Standalone Engine');
  }
  return { success: true, message: 'Đã phân công phản biện thành công!' };
}

async function apiGetMyAssignments() {
  const token = localStorage.getItem('journal_token');
  if (!token) return [];

  // 1. Thử gọi API Backend thật nếu không phải standalone token thuần
  if (!token.startsWith('standalone_token_')) {
    try {
      const res = await fetchWithTimeout(`${API_BASE}/phanbien/my-assignments`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data) && data.length > 0) return data;
      }
    } catch (e) {
      // Backend offline
    }
  }

  // 2. Lấy dữ liệu phân công độc lập trong localStorage hoặc danh sách mẫu
  let localAssignments = [];
  try { localAssignments = JSON.parse(localStorage.getItem('huit_standalone_assignments') || '[]'); } catch(e){}
  if (localAssignments.length > 0) {
    return localAssignments;
  }

  const sampleAssignments = [
    {
      maPhanCong: 201,
      maBaiBao: 101,
      soVong: 1,
      trangThai: 'Đang phản biện',
      daDanhGia: false,
      ngayPhanCong: '2026-09-12T09:00:00',
      hanHoanThanh: '2026-10-02T23:59:59',
      tieuDeBaiBao: 'Nghiên cứu cấu trúc phân tử và hoạt tính kháng oxy hóa của các hợp chất tự nhiên chiết xuất từ thực vật Việt Nam',
      chuyenNganh: 'Hóa học & Công nghệ thực phẩm',
      diemTongKet: null,
      kienNghi: null
    },
    {
      maPhanCong: 202,
      maBaiBao: 102,
      soVong: 1,
      trangThai: 'Đã đánh giá',
      daDanhGia: true,
      ngayPhanCong: '2026-09-01T08:30:00',
      hanHoanThanh: '2026-09-20T23:59:59',
      tieuDeBaiBao: 'Ứng dụng mô hình học sâu Transformer trong nhận dạng và phân loại lỗi bề mặt sản phẩm cơ khí chính xác',
      chuyenNganh: 'Cơ khí – Chế tạo máy – Tự động hóa',
      diemTongKet: 8.5,
      kienNghi: 'Chấp nhận sau khi sửa đổi nhỏ'
    }
  ];
  return sampleAssignments;
}

async function apiSubmitEvaluation(data) {
  const token = localStorage.getItem('journal_token');
  try {
    const res = await fetchWithTimeout(`${API_BASE}/phanbien/evaluate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(data)
    });
    if (res.ok) {
      return await res.json();
    }
  } catch (e) {
    console.log('Backend offline: Ghi nhận đánh giá BM-04 trong Standalone Engine');
  }

  let localAssignments = [];
  try { localAssignments = JSON.parse(localStorage.getItem('huit_standalone_assignments') || '[]'); } catch(e){}
  if (localAssignments.length === 0) {
    localAssignments = [
      {
        maPhanCong: 201,
        maBaiBao: 101,
        soVong: 1,
        trangThai: 'Đang phản biện',
        daDanhGia: false,
        ngayPhanCong: '2026-09-12T09:00:00',
        hanHoanThanh: '2026-10-02T23:59:59',
        tieuDeBaiBao: 'Nghiên cứu cấu trúc phân tử và hoạt tính kháng oxy hóa của các hợp chất tự nhiên chiết xuất từ thực vật Việt Nam',
        chuyenNganh: 'Hóa học & Công nghệ thực phẩm'
      },
      {
        maPhanCong: 202,
        maBaiBao: 102,
        soVong: 1,
        trangThai: 'Đã đánh giá',
        daDanhGia: true,
        ngayPhanCong: '2026-09-01T08:30:00',
        hanHoanThanh: '2026-09-20T23:59:59',
        tieuDeBaiBao: 'Ứng dụng mô hình học sâu Transformer trong nhận dạng và phân loại lỗi bề mặt sản phẩm cơ khí chính xác',
        chuyenNganh: 'Cơ khí – Chế tạo máy – Tự động hóa',
        diemTongKet: 8.5,
        kienNghi: 'Chấp nhận sau khi sửa đổi nhỏ'
      }
    ];
  }
  const idx = localAssignments.findIndex(a => a.maPhanCong === data.maPhanCong);
  if (idx !== -1) {
    localAssignments[idx].daDanhGia = true;
    localAssignments[idx].trangThai = 'Đã đánh giá';
    localAssignments[idx].diemTongKet = data.diemTongKet;
    localAssignments[idx].kienNghi = data.kienNghi;
  }
  localStorage.setItem('huit_standalone_assignments', JSON.stringify(localAssignments));

  return {
    success: true,
    message: 'Nộp phiếu đánh giá BM-04 thành công!'
  };
}

async function apiMakeDecision(data) {
  const token = localStorage.getItem('journal_token');
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
  } catch (e) {
    console.log('Backend offline: Ra quyết định xuất bản trong Standalone Engine');
  }
  return { success: true, message: 'Đã ra quyết định xuất bản bản thảo thành công!' };
}

async function apiResubmitPaper(baiBaoId, formData) {
  const token = localStorage.getItem('journal_token');
  const isStandalone = !token || token.startsWith('standalone_token_');

  // 1. Chế độ Online: Gửi bản thảo chỉnh sửa lên máy chủ Web API
  if (!isStandalone) {
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

  let localSubs = [];
  try { localSubs = JSON.parse(localStorage.getItem('huit_standalone_submissions') || '[]'); } catch(e){}
  const idx = localSubs.findIndex(s => s.maBaiBao === baiBaoId || s.maBaiBao === parseInt(baiBaoId));
  if (idx !== -1) {
    localSubs[idx].trangThai = 'Đã nộp lại (Chờ duyệt)';
    localSubs[idx].ngayCapNhat = new Date().toISOString();
    localSubs[idx].tapTinChinhSua = formData.get('TapTinChinhSua')?.name || 'BanThao_ChinhSua.docx';
    localStorage.setItem('huit_standalone_submissions', JSON.stringify(localSubs));
  }
  return { success: true, message: 'Đã nộp lại bản thảo chỉnh sửa và giải trình phản biện thành công!' };
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
  if (pNew.length < 6) {
    showToast('Mật khẩu mới phải có tối thiểu 6 ký tự theo quy định.', 'error');
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
