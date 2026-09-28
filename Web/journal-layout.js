/**
 * ==========================================================================
 * TẠP CHÍ KHOA HỌC ĐẠI HỌC CÔNG THƯƠNG (HUIT JOURNAL OF SCIENCE)
 * SHARED LAYOUT ENGINE - BẢN GIAO DIỆN CHUNG TẬP TRUNG
 * ==========================================================================
 * Tệp này quản lý tập trung 100% Header, Dropdown Navbar và Footer chính thức.
 * Khi cần thay đổi thông tin Tòa soạn, Hotline, Email, Ban lãnh đạo hoặc Menu,
 * bạn chỉ cần chỉnh sửa tại tệp này, toàn bộ các trang sẽ tự động cập nhật!
 */

(function () {
  'use strict';

  // 1. CẤU HÌNH ĐỊNH DANH TÒA SOẠN CHÍNH THỨC (SINGLE SOURCE OF TRUTH)
  const HUIT_CONFIG = {
    journalNameVi: 'Tạp chí Khoa học Đại học Công Thương',
    journalNameEn: 'Huit Journal of Science',
    governingBody: 'Trường Đại học Công Thương TP. Hồ Chí Minh',
    license: 'Giấy phép hoạt động số: 53/GP-BTTTT ngày 06/3/2024 do Bộ Thông tin và Truyền thông cấp',
    pIssn: 'p-ISSN 3030-4113',
    eIssn: 'e-ISSN 3030-413X',
    editorInChief: 'TS. Bùi Hồng Đăng',
    boardChair: 'PGS. TS. Nguyễn Xuân Hoàn',
    address: '140 Lê Trọng Tấn, Phường Tây Thạnh, Thành phố Hồ Chí Minh',
    office: 'Tòa nhà C, Phòng C101, Trường ĐH Công Thương TP.HCM',
    phone: '028.38163318 - ext.112',
    email: 'journal@huit.edu.vn',
    websiteOfficial: 'https://huitjournal.vn',
    websitePortal: 'https://journal.huit.edu.vn',
    copyright: '©2023 - 2026 Trường Đại học Công Thương TP. Hồ Chí Minh. Bảo lưu mọi quyền.'
  };

  // 2. HÀM TỰ ĐỘNG XÁC ĐỊNH TRANG HIỆN TẠI (AUTO-DETECT ACTIVE TAB)
  function getAutoActiveTab() {
    const path = window.location.pathname.toLowerCase();
    if (path.includes('about')) return 'about';
    if (path.includes('publishing-policy')) return 'policy';
    if (path.includes('editorial-board')) return 'board';
    if (path.includes('contact')) return 'contact';
    if (path.includes('guidelines')) return 'guidelines';
    if (path.includes('archives')) return 'archives';
    if (path.includes('article-detail')) return 'article';
    if (path.includes('submit-paper')) return 'submit';
    if (path.includes('login')) return 'login';
    if (path.includes('register')) return 'register';
    if (path.includes('profile')) return 'profile';
    return 'home';
  }

  // 3. RENDER HEADER (BANNER + MASTHEAD + DROPDOWN NAVBAR)
  function renderHeader(targetEl, forceActiveTab) {
    if (!targetEl) return;
    const active = forceActiveTab || targetEl.getAttribute('data-active') || getAutoActiveTab();

    const isIntroActive = ['about', 'policy', 'board', 'contact'].includes(active);
    const isGuidelinesActive = active === 'guidelines';
    const isFieldsActive = active === 'fields' || active === 'article';
    const isArchivesActive = active === 'archives';
    const isHomeActive = active === 'home';

    const headerHtml = `
      <!-- KHỐI BANNER & NAVBAR NỀN XANH (#1da1f2) -->
      <div class="header-bar">
        <header class="masthead wrap" style="position: relative;">
          <div class="name"><a href="UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html">${HUIT_CONFIG.journalNameVi}</a></div>
          <div class="tagline">TRƯỜNG ĐẠI HỌC CÔNG THƯƠNG TP. HỒ CHÍ MINH · ${HUIT_CONFIG.journalNameEn.toUpperCase()}</div>
          <div class="banner-issn-text" style="position: absolute; right: 20px; bottom: 12px; text-align: right; color: #ffffff; font-family: 'Inter', system-ui, sans-serif; font-size: 13.5px; font-weight: 700; line-height: 1.45; letter-spacing: 0.03em; text-shadow: 0 1px 2px rgba(0,0,0,0.2); display: flex; flex-direction: column; align-items: flex-end; z-index: 10; pointer-events: none;">
            <span>p-ISSN 3030-4113</span>
            <span>e-ISSN 3030-413X</span>
          </div>
        </header>

        <nav class="navbar wrap">
          <div class="nav-links">
            <a href="UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html" class="${isHomeActive ? 'on' : ''}" style="display:inline-flex;align-items:center;gap:4px;">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"/></svg>
              Trang chủ
            </a>

            <!-- TAB DROPDOWN: GIỚI THIỆU -->
            <div class="nav-item-dropdown">
              <span class="nav-dropdown-toggle ${isIntroActive ? 'on' : ''}">
                Giới thiệu
                <svg viewBox="0 0 24 24"><polyline points="6 9 12 15 18 9"></polyline></svg>
              </span>
              <div class="nav-fields-menu">
                <a href="about.html" class="field-menu-item ${active === 'about' ? 'active-item' : ''}">
                  <span>Giới thiệu về tạp chí</span>
                </a>
                <a href="publishing-policy.html" class="field-menu-item ${active === 'policy' ? 'active-item' : ''}">
                  <span>Chính sách xuất bản</span>
                </a>
                <a href="editorial-board.html" class="field-menu-item ${active === 'board' ? 'active-item' : ''}">
                  <span>Hội đồng biên tập</span>
                </a>
                <a href="contact.html" class="field-menu-item ${active === 'contact' ? 'active-item' : ''}">
                  <span>Nhân sự &amp; Liên hệ</span>
                </a>
              </div>
            </div>

            <!-- TAB DROPDOWN: QUY TRÌNH - BIỂU MẪU -->
            <div class="nav-item-dropdown">
              <span class="nav-dropdown-toggle ${isGuidelinesActive ? 'on' : ''}">
                Quy trình - Biểu mẫu
                <svg viewBox="0 0 24 24"><polyline points="6 9 12 15 18 9"></polyline></svg>
              </span>
              <div class="nav-fields-menu">
                <a href="guidelines.html#submission" class="field-menu-item">
                  <span>Quy trình đăng bài</span>
                </a>
                <a href="guidelines.html#review" class="field-menu-item">
                  <span>Quy trình phản biện</span>
                </a>
                <a href="guidelines.html#format" class="field-menu-item">
                  <span>Thể lệ viết và gửi bài</span>
                </a>
                <a href="guidelines.html#templates" class="field-menu-item">
                  <span>Các biểu mẫu</span>
                </a>
              </div>
            </div>

            <!-- TAB DROPDOWN: LĨNH VỰC KHOA HỌC -->
            <div class="nav-item-dropdown">
              <span class="nav-dropdown-toggle ${isFieldsActive ? 'on' : ''}">
                Lĩnh vực khoa học
                <svg viewBox="0 0 24 24"><polyline points="6 9 12 15 18 9"></polyline></svg>
              </span>
              <div class="nav-fields-menu">
                <div data-major-menu-items class="nav-fields-menu-status">Đang tải lĩnh vực khoa học...</div>
                <a href="archives.html?view=articles" class="fields-menu-all">Xem tất cả các chuyên ngành &rarr;</a>
              </div>
            </div>

            <a href="archives.html" class="${isArchivesActive ? 'on' : ''}">Kho lưu trữ</a>
          </div>

          <!-- KHỐI PHẢI: NÚT NỘP BÀI + AUTH -->
          <div class="nav-actions">
            <a href="submit-paper.html" style="background:#ffffff;color:#1da1f2;padding:6px 14px;border-radius:4px;font-size:12.5px;font-weight:700;box-shadow:0 1px 3px rgba(0,0,0,0.12);display:inline-flex;align-items:center;gap:6px;transition:all .2s;">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M12 5v14M5 12h14"/></svg>
              Gửi bài báo
            </a>

            <div class="auth-links">
              <!-- Sẽ được journal-interactions.js tự động điền theo phiên đăng nhập -->
              <a href="login.html" class="login-btn">Đăng nhập</a>
              <a href="register.html" class="register-btn">Đăng ký</a>
            </div>
          </div>
        </nav>
      </div>
    `;

    targetEl.innerHTML = headerHtml;
  }

  // 4. RENDER FOOTER (CHUẨN FORMAT GỐC CỦA DỰ ÁN - CHỨA ĐỦ THÔNG TIN ĐỊNH DANH HUIT)
  function renderFooter(targetEl) {
    if (!targetEl) return;

    const footerHtml = `
      <footer>
        <div style="font-weight: 700; font-size: 13.5px; margin-bottom: 4px;">
          ${HUIT_CONFIG.journalNameVi.toUpperCase()} · ${HUIT_CONFIG.governingBody.toUpperCase()}
        </div>
        <div style="font-size: 12px; opacity: 0.95; line-height: 1.6;">
          ${HUIT_CONFIG.license} · ${HUIT_CONFIG.pIssn} · ${HUIT_CONFIG.eIssn} · Tổng biên tập: ${HUIT_CONFIG.editorInChief}
        </div>
        <div style="font-size: 11.5px; opacity: 0.9; margin-top: 3px;">
          Tòa soạn: ${HUIT_CONFIG.office}, ${HUIT_CONFIG.address} · Điện thoại: ${HUIT_CONFIG.phone} · Email: <a href="mailto:${HUIT_CONFIG.email}" style="color:#ffffff;text-decoration:underline;">${HUIT_CONFIG.email}</a>
        </div>
        <div class="flinks">
          <a href="about.html">Về tạp chí</a>
          <a href="guidelines.html">Thể lệ &amp; Quy trình nộp bài</a>
          <a href="publishing-policy.html">Đạo đức xuất bản</a>
          <a href="editorial-board.html">Hội đồng biên tập</a>
          <a href="contact.html">Liên hệ tòa soạn</a>
        </div>
      </footer>
    `;

    targetEl.innerHTML = footerHtml;
  }

  let publicArchiveArticleCounts = null;

  function normalizeMajorName(value) {
    return String(value || '').trim().normalize('NFC').toLocaleLowerCase('vi-VN');
  }

  function applyPublicArchiveArticleCounts(menu) {
    if (!menu || !publicArchiveArticleCounts) return;
    menu.querySelectorAll('.field-menu-item').forEach(link => {
      const label = link.querySelector('span:first-child');
      const badge = link.querySelector('.badge-count');
      if (!label || !badge) return;
      const count = publicArchiveArticleCounts[normalizeMajorName(label.textContent)];
      if (Number.isFinite(count)) badge.textContent = `${count} bài`;
    });
  }

  function setPublicArchiveArticleCounts(counts) {
    if (!counts || typeof counts !== 'object' || Array.isArray(counts)) return;
    publicArchiveArticleCounts = counts;
    applyPublicArchiveArticleCounts(document.querySelector('[data-major-menu-items]'));
  }

  // 5. CẬP NHẬT SỐ LIỆU ĐỘNG TỪ API CHO DROPDOWN LĨNH VỰC
  async function updateNavFieldBadges() {
    const menu = document.querySelector('[data-major-menu-items]');
    if (!menu) return;
    try {
      const isLocalStatic = ['localhost', '127.0.0.1'].includes(location.hostname) && ['8088', '5500', '5501'].includes(location.port);
      const baseUrl = typeof API_BASE !== 'undefined' ? API_BASE : (isLocalStatic ? 'http://localhost:5000/api' : '/api');
      const res = await fetch(`${baseUrl}/chuyennganh`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const categories = await res.json();
      if (!Array.isArray(categories)) throw new Error('Invalid category response');
      const escape = typeof escapeHtml === 'function' ? escapeHtml : value => String(value ?? '');
      const available = categories.filter(category => category?.tenChuyenNganh);
      menu.innerHTML = available.length
        ? available.map(category => {
          const major = normalizeMajorName(category.tenChuyenNganh);
          const publicCount = publicArchiveArticleCounts?.[major];
          const count = Number.isFinite(publicCount) ? publicCount : (Number(category.tongSoBaiBao) || 0);
          return `<a href="archives.html?major=${encodeURIComponent(category.tenChuyenNganh)}" class="field-menu-item"><span>${escape(category.tenChuyenNganh)}</span><span class="badge-count">${count} bài</span></a>`;
        }).join('')
        : 'Chưa có lĩnh vực khoa học.';
      applyPublicArchiveArticleCounts(menu);
    } catch (e) {
      menu.textContent = 'Không tải được danh mục lĩnh vực. Vui lòng thử lại sau.';
    }
  }

  // Tương tác đóng/mở dropdown khi chạm/click trên màn hình
  function initDropdownEvents() {
    document.addEventListener('click', (e) => {
      const toggle = e.target.closest('.nav-dropdown-toggle');
      if (toggle) {
        const parent = toggle.closest('.nav-item-dropdown');
        const menu = parent ? parent.querySelector('.nav-fields-menu') : null;
        if (menu) {
          const isOpen = menu.classList.contains('show');
          document.querySelectorAll('.nav-fields-menu.show').forEach(m => m.classList.remove('show'));
          if (!isOpen) menu.classList.add('show');
        }
      } else if (!e.target.closest('.nav-fields-menu')) {
        document.querySelectorAll('.nav-fields-menu.show').forEach(m => m.classList.remove('show'));
      }
    });
  }

  // 6. TỰ ĐỘNG KHỞI CHẠY (AUTO-MOUNT)
  function initLayout() {
    const headerPlaceholder = document.getElementById('huit-header') || document.querySelector('[data-huit-header]');
    if (headerPlaceholder) {
      renderHeader(headerPlaceholder);
    }

    const footerPlaceholder = document.getElementById('huit-footer') || document.querySelector('[data-huit-footer]');
    if (footerPlaceholder) {
      renderFooter(footerPlaceholder);
    }

    // Đồng bộ trạng thái người dùng với journal-interactions.js nếu đã tải
    if (typeof window.renderAuthNavbar === 'function') {
      window.renderAuthNavbar();
    }

    // Nạp số lượng bài báo và gắn sự kiện click cho dropdown
    updateNavFieldBadges();
    initDropdownEvents();
  }

  // Chạy ngay khi DOM sẵn sàng hoặc nếu đã tải xong
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initLayout);
  } else {
    initLayout();
  }

  // Xuất API toàn cục để có thể gọi thủ công khi cần
  window.HuitLayout = {
    config: HUIT_CONFIG,
    renderHeader: renderHeader,
    renderFooter: renderFooter,
    setPublicArchiveArticleCounts: setPublicArchiveArticleCounts,
    init: initLayout
  };

})();
