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

// Quản lý trạng thái phiên đăng nhập người dùng (User Session Management)
function initUserSession() {
  // Mặc định khởi tạo tài khoản tác giả mẫu nếu chưa có
  if (!localStorage.getItem('journal_user')) {
    const defaultUser = {
      isLoggedIn: true,
      hoTen: 'TS. Vũ Thị F',
      email: 'vuthif@journal.edu.vn',
      donVi: 'Khoa Công nghệ Thông tin, Viện Nghiên cứu Khoa học & Công nghệ',
      chucVu: 'Tác giả & Nhà nghiên cứu',
      orcid: '0000-0002-1825-0097'
    };
    localStorage.setItem('journal_user', JSON.stringify(defaultUser));
  }
  renderAuthNavbar();
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
      container.innerHTML = `
        <div class="user-logged-badge" onclick="toggleUserDropdown(event)">
          <div class="user-avatar-circle">${initial}</div>
          <div class="user-badge-info">
            <span class="user-badge-name">${user.hoTen}</span>
            <span class="user-badge-role">${user.chucVu || 'Tác giả'}</span>
          </div>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#ffffff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"></polyline></svg>

          <!-- Dropdown Menu -->
          <div class="user-dropdown-menu" id="global-user-dropdown">
            <div class="dropdown-header">
              <div class="dropdown-header-name">${user.hoTen}</div>
              <div class="dropdown-header-email">${user.email}</div>
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
  const user = getCurrentUser();
  if (user) {
    user.isLoggedIn = false;
    localStorage.setItem('journal_user', JSON.stringify(user));
  }
  showToast('Đã đăng xuất khỏi hệ thống!');
  renderAuthNavbar();
  setTimeout(() => {
    window.location.href = 'UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html';
  }, 600);
}

// Toast Helper
function showToast(message, iconSvg = null) {
  let toast = document.getElementById('journal-global-toast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'journal-global-toast';
    toast.className = 'journal-toast';
    document.body.appendChild(toast);
  }
  const defaultIcon = `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>`;
  toast.innerHTML = `${iconSvg || defaultIcon} <span>${message}</span>`;
  toast.classList.add('active');

  setTimeout(() => {
    toast.classList.remove('active');
  }, 3500);
}

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
        const dummyPdfContent = `%PDF-1.4\n%Scientific Journal System\n1 0 obj\n<< /Title (Ung dung mo hinh Transformer trong phan loai van ban tieng Viet quy mo lon)\n/Author (Vu Thi F, Dang Van G)\n/Journal (Tap chi Khoa hoc & Cong nghe JST - ISSN 1859-3097)\n>>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF`;
        const blob = new Blob([dummyPdfContent], { type: 'application/pdf' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = 'JST_2026_Vol15_No42_Transformer_Classification.pdf';
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
    title: 'Về Tạp chí Khoa học',
    html: `
      <h4>Tôn chỉ & Mục đích</h4>
      <p>Tạp chí Khoa học là ấn phẩm học thuật uy tín được cấp chỉ số ISSN 1859-3097. Tạp chí là diễn đàn công bố các kết quả nghiên cứu khoa học nguyên gốc và các giải pháp công nghệ tiên tiến thuộc các lĩnh vực Công nghệ thông tin, Cơ khí chế tạo máy, Công nghệ sinh học & Thực phẩm, Khoa học Môi trường và Kinh tế.</p>
      <h4>Hội đồng biên tập</h4>
      <ul>
        <li><strong>Tổng biên tập:</strong> PGS. TS. Nguyễn Xuân Hoàn (Viện Nghiên cứu Khoa học & Công nghệ)</li>
        <li><strong>Phó Tổng biên tập:</strong> GS. TS. Lê Văn B (Chuyên ngành Khoa học Máy tính)</li>
        <li><strong>Thư ký tòa soạn:</strong> ThS. Trần Thị D</li>
      </ul>
      <h4>Chỉ số xuất bản & Định danh</h4>
      <p>Tạp chí được lập chỉ mục tại Google Scholar, Vietnam Citation Index (VCI) và được cấp mã DOI bởi CrossRef.</p>
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
    title: 'Thông tin liên hệ Tòa soạn JST',
    html: `
      <h4>Văn phòng Tòa soạn Tạp chí Khoa học</h4>
      <ul>
        <li><strong>Địa chỉ:</strong> Tòa nhà Trung tâm Nghiên cứu Khoa học & Công nghệ, TP. Hồ Chí Minh</li>
        <li><strong>Điện thoại:</strong> (028) 3816 1673 - Số nội bộ: 136</li>
        <li><strong>Email tiếp nhận bản thảo:</strong> tapchikhoahoc@journal.edu.vn</li>
        <li><strong>Cổng thông tin trực tuyến:</strong> http://journal.edu.vn</li>
        <li><strong>Thời gian làm việc:</strong> Thứ Hai – Thứ Sáu (07:30 – 16:30)</li>
      </ul>
    `
  },
  guidelines: {
    title: 'Thể lệ & Quy trình nộp bài',
    html: `
      <h4>Quy cách bản thảo</h4>
      <p>Bản thảo được soạn thảo trên phần mềm MS Word (font Times New Roman, size 12, cách dòng 1.2), độ dài thông thường từ 8 đến 15 trang (bao gồm cả tài liệu tham khảo và phụ lục).</p>
      <h4>Cấu trúc bài báo chuẩn IMRAD</h4>
      <ul>
        <li><strong>Tiêu đề & Tóm tắt:</strong> Đầy đủ cả tiếng Việt và tiếng Anh (150 – 250 từ) kèm 4 – 6 từ khóa.</li>
        <li><strong>Mở đầu (Introduction):</strong> Đặt vấn đề và tổng quan tài liệu nghiên cứu.</li>
        <li><strong>Phương pháp (Methods):</strong> Mô tả chi tiết phương pháp tiếp cận, thuật toán hoặc thiết kế thực nghiệm.</li>
        <li><strong>Kết quả & Thảo luận (Results & Discussion):</strong> Số liệu, bảng biểu minh họa rõ ràng.</li>
        <li><strong>Kết luận (Conclusion):</strong> Tóm lược đóng góp và hướng phát triển.</li>
        <li><strong>Tài liệu tham khảo:</strong> Trình bày theo chuẩn APA 7th.</li>
      </ul>
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
