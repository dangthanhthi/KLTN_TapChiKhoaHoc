using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;
using QL_TapChi_WinForms.UI.Views;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmMain : Form
    {
        private Panel pnlHeader = null!;
        private Panel pnlSidebar = null!;
        private Panel pnlContent = null!;

        private Label lblTitle = null!;
        private Label lblRoleBadge = null!;
        private Label lblUserName = null!;
        private Label lblUserRole = null!;
        private ModernButton btnLogout = null!;
        private ModernButton btnSwitchMode = null!;
        private FlagButton btnFlagVN = null!;
        private FlagButton btnFlagEN = null!;

        private readonly List<SidebarMenuItem> _menuButtons = new();
        private UserControl? _currentView = null;
        private string _currentKey = "Dashboard";

        // Current active workspace mode: "Editor" or "Admin"
        private string _activeRoleMode = "Editor";

        // View instances cached for smooth switching
        private UcDashboard? _viewDashboard;
        private UcAdminDashboard? _viewAdminDashboard;
        private UcQuanLyBaiBao? _viewBaiBao;
        private UcQuanLyNguoiDung? _viewNguoiDung;
        private UcQuanLyPhanBien? _viewPhanBien;
        private UcQuanLySoTapChi? _viewSoTapChi;
        private UcThongKeBaoCao? _viewThongKe;
        private UcSaoLuuPhucHoi? _viewSaoLuu;
        private UcQuanLyChuyenNganh? _viewChuyenNganh;

        public FrmMain()
        {
            if (AuthService.CurrentUser != null && 
                AuthService.CurrentUser.DanhSachVaiTro.Contains("Quản trị hệ thống"))
            {
                _activeRoleMode = "Admin";
            }
            else
            {
                _activeRoleMode = "Editor";
            }

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();

            InitializeComponent();
            LanguageService.OnLanguageChanged += SetupHeaderAndMenu;
            SetupHeaderAndMenu();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            LanguageService.OnLanguageChanged -= SetupHeaderAndMenu;
        }

        private void InitializeComponent()
        {
            Text = "Hệ thống Quản lý Tạp chí Khoa học và Công nghệ (JST)";
            Size = new Size(1420, 880);
            MinimumSize = new Size(1180, 720);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            BackColor = UITheme.AppBackground;
            Font = UITheme.FontBody;

            // 1. TOP HEADER (Azure Blue Web-Synchronized Banner, Responsive)
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = UITheme.HeaderBg
            };

            // Header Left: Official University Logo, Brand Title, and Role Tag
            var pnlHeaderLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 390,
                BackColor = Color.Transparent
            };

            Image? logoHuit = null;
            try
            {
                string path1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "huit_logo.png");
                string path2 = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "huit_logo.png");
                string path = File.Exists(path1) ? path1 : (File.Exists(path2) ? path2 : "");
                if (!string.IsNullOrEmpty(path))
                {
                    using var stream = new MemoryStream(File.ReadAllBytes(path));
                    logoHuit = Image.FromStream(stream);
                }
            }
            catch { }

            var pnlBadge = new Panel
            {
                Size = new Size(44, 44),
                Location = new Point(16, 14),
                BackColor = Color.Transparent
            };
            pnlBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using var p = UITheme.CreateRoundedRectangle(new Rectangle(0, 0, pnlBadge.Width - 1, pnlBadge.Height - 1), 8);
                using var brush = new SolidBrush(Color.White);
                e.Graphics.FillPath(brush, p);

                if (logoHuit != null)
                {
                    e.Graphics.DrawImage(logoHuit, new Rectangle(3, 3, pnlBadge.Width - 6, pnlBadge.Height - 6));
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, "HUIT", new Font("Segoe UI", 10f, FontStyle.Bold), new Rectangle(0, 0, pnlBadge.Width, pnlBadge.Height), UITheme.PrimaryDark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            lblTitle = new Label
            {
                Text = LanguageService.Get("AppTitle"),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(70, 14),
                AutoSize = true,
                UseMnemonic = false
            };

            lblRoleBadge = new Label
            {
                Text = "Quản trị hệ thống",
                Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 240, 255),
                Location = new Point(71, 41),
                AutoSize = true,
                UseMnemonic = false
            };

            pnlHeaderLeft.Controls.AddRange(new Control[] { pnlBadge, lblTitle, lblRoleBadge });

            // Header Right: Logout, Language Switcher (Flags), User Info
            var pnlHeaderRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Height = 72,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(6, 17, 16, 0),
                BackColor = Color.Transparent,
                WrapContents = false
            };

            btnLogout = new ModernButton
            {
                Text = "Đăng xuất",
                Size = new Size(95, 34),
                NormalColor = Color.White,
                HoverColor = Color.FromArgb(254, 242, 242),
                BorderColor = Color.White,
                ForeColor = Color.FromArgb(220, 38, 38),
                Margin = new Padding(6, 0, 0, 0)
            };
            btnLogout.Click += BtnLogout_Click;

            // Language Switcher (Vietnam and UK flags)
            var pnlLang = new Panel
            {
                Size = new Size(76, 34),
                BackColor = Color.Transparent,
                Margin = new Padding(12, 0, 10, 0)
            };

            btnFlagVN = new FlagButton("vi")
            {
                Location = new Point(0, 5),
                Size = new Size(34, 24),
                IsActive = (LanguageService.CurrentLanguage == "vi")
            };
            btnFlagVN.Click += (s, e) => LanguageService.SetLanguage("vi");

            btnFlagEN = new FlagButton("en")
            {
                Location = new Point(40, 5),
                Size = new Size(34, 24),
                IsActive = (LanguageService.CurrentLanguage == "en")
            };
            btnFlagEN.Click += (s, e) => LanguageService.SetLanguage("en");

            pnlLang.Controls.Add(btnFlagVN);
            pnlLang.Controls.Add(btnFlagEN);

            var pnlUserInfo = new Panel
            {
                Size = new Size(140, 42),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            lblUserName = new Label
            {
                Text = AuthService.CurrentUser?.HoTen ?? "Người dùng",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, 0),
                Size = new Size(140, 20),
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };

            lblUserRole = new Label
            {
                Text = AuthService.CurrentUser?.VaiTroHienThi ?? "Quản trị viên",
                Font = UITheme.FontSmall,
                ForeColor = Color.FromArgb(235, 245, 255),
                Location = new Point(0, 20),
                Size = new Size(140, 20),
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };

            void OpenPasswordDialog(object? s, EventArgs e)
            {
                using var frm = new FrmDoiMatKhauDialog();
                frm.ShowDialog();
            }
            pnlUserInfo.Click += OpenPasswordDialog;
            lblUserName.Click += OpenPasswordDialog;
            lblUserRole.Click += OpenPasswordDialog;

            var tt = new ToolTip();
            tt.SetToolTip(pnlUserInfo, "Bấm để đổi mật khẩu tài khoản");
            tt.SetToolTip(lblUserName, "Bấm để đổi mật khẩu tài khoản");

            pnlUserInfo.Controls.AddRange(new Control[] { lblUserName, lblUserRole });

            btnSwitchMode = new ModernButton
            {
                Size = new Size(135, 34),
                NormalColor = Color.FromArgb(240, 249, 255),
                HoverColor = Color.FromArgb(224, 242, 254),
                BorderColor = Color.FromArgb(186, 230, 253),
                ForeColor = UITheme.PrimaryDark,
                Font = UITheme.FontSmallBold,
                Margin = new Padding(4, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            btnSwitchMode.Click += (s, e) =>
            {
                _activeRoleMode = (_activeRoleMode == "Admin") ? "Editor" : "Admin";
                _currentView = null;
                SetupHeaderAndMenu();
            };

            pnlHeaderRight.Controls.AddRange(new Control[] { btnLogout, pnlLang, pnlUserInfo, btnSwitchMode });

            pnlHeader.Controls.Add(pnlHeaderRight);
            pnlHeader.Controls.Add(pnlHeaderLeft);

            // 2. LEFT SIDEBAR (Academic Light Sidebar with right border line)
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 245,
                BackColor = UITheme.SidebarBg,
                Padding = new Padding(0, 8, 0, 16),
                AutoScroll = true
            };
            pnlSidebar.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.SidebarBorder, 1);
                e.Graphics.DrawLine(pen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
            };

            // 3. MAIN CONTENT CONTAINER
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.AppBackground
            };

            Controls.Add(pnlContent);
            Controls.Add(pnlSidebar);
            Controls.Add(pnlHeader);
        }

        private void SetupHeaderAndMenu()
        {
            Text = (LanguageService.CurrentLanguage == "en")
                ? "Journal of Science and Technology Management System (JST)"
                : "Hệ thống Quản lý Tạp chí Khoa học và Công nghệ (JST)";

            pnlSidebar.Controls.Clear();
            _menuButtons.Clear();

            lblTitle.Text = LanguageService.Get("AppTitle");
            btnLogout.Text = LanguageService.Get("Logout");

            btnFlagVN.IsActive = (LanguageService.CurrentLanguage == "vi");
            btnFlagEN.IsActive = (LanguageService.CurrentLanguage == "en");

            pnlHeader.BackColor = UITheme.HeaderBg;

            bool canSwitch = AuthService.CurrentUser?.DanhSachVaiTro.Contains("Quản trị hệ thống") == true;
            btnSwitchMode.Visible = canSwitch;
            btnSwitchMode.Text = (_activeRoleMode == "Admin") 
                ? LanguageService.Get("SwitchToEditor") 
                : LanguageService.Get("SwitchToAdmin");

            string secWorkflow = LanguageService.CurrentLanguage == "vi" ? "QUY TRÌNH XUẤT BẢN" : "EDITORIAL WORKFLOW";
            string secAcademic = LanguageService.CurrentLanguage == "vi" ? "HỌC THUẬT & BÁO CÁO" : "ACADEMIC & ANALYTICS";
            string secSystem = LanguageService.CurrentLanguage == "vi" ? "QUẢN TRỊ HỆ THỐNG" : "SYSTEM & SECURITY";
            string secData = LanguageService.CurrentLanguage == "vi" ? "QUẢN TRỊ DỮ LIỆU" : "DATA MANAGEMENT";

            if (_activeRoleMode == "Admin")
            {
                lblRoleBadge.Text = LanguageService.Get("RoleAdmin");
                lblRoleBadge.ForeColor = Color.FromArgb(220, 240, 255);

                lblUserName.Text = !string.IsNullOrEmpty(AuthService.CurrentUser?.HoTen)
                    ? AuthService.CurrentUser.HoTen
                    : ((LanguageService.CurrentLanguage == "en") ? "System Admin" : "Quản trị Tạp chí");
                lblUserRole.Text = !string.IsNullOrEmpty(AuthService.CurrentUser?.Email)
                    ? AuthService.CurrentUser.Email
                    : "admin@huit.edu.vn";

                var adminItems = new List<SidebarItemDef>
                {
                    new("AdminDashboard", "📊", LanguageService.Get("Nav_AdminDashboard")),
                    new("NguoiDung", "👥", LanguageService.Get("Nav_NguoiDung"), secSystem),
                    new("ChuyenNganh", "🏷", LanguageService.Get("Nav_ChuyenNganh")),
                    new("SaoLuu", "💾", LanguageService.Get("Nav_SaoLuu")),
                    new("BaiBao", "📄", LanguageService.Get("Nav_BaiBaoTraCuu"), secData),
                    new("ThongKe", "📈", LanguageService.Get("Nav_ThongKe"))
                };

                BuildSidebar(adminItems);
                if (_currentView == null)
                    NavigateTo("AdminDashboard");
                else
                    RefreshCurrentMenuHighlight();
            }
            else
            {
                lblRoleBadge.Text = LanguageService.Get("RoleEditor");
                lblRoleBadge.ForeColor = Color.FromArgb(220, 240, 255);

                lblUserName.Text = !string.IsNullOrEmpty(AuthService.CurrentUser?.HoTen)
                    ? AuthService.CurrentUser.HoTen
                    : ((LanguageService.CurrentLanguage == "en") ? "Editorial Office" : "Ban Thư Ký Tòa Soạn");
                lblUserRole.Text = !string.IsNullOrEmpty(AuthService.CurrentUser?.Email)
                    ? AuthService.CurrentUser.Email
                    : "editor@huit.edu.vn";

                var editorItems = new List<SidebarItemDef>
                {
                    new("Dashboard", "📊", LanguageService.Get("Nav_Dashboard")),
                    new("BaiBao", "📄", LanguageService.Get("Nav_BaiBao"), secWorkflow),
                    new("PhanBien", "⚖", LanguageService.Get("Nav_PhanBien")),
                    new("SoTapChi", "📚", LanguageService.Get("Nav_SoTapChi")),
                    new("ChuyenNganh", "🏷", LanguageService.Get("Nav_ChuyenNganh"), secAcademic),
                    new("ThongKe", "📈", LanguageService.Get("Nav_ThongKeBaoCao"))
                };

                BuildSidebar(editorItems);
                if (_currentView == null)
                    NavigateTo("Dashboard");
                else
                    RefreshCurrentMenuHighlight();
            }
        }

        private class SidebarItemDef
        {
            public string Key { get; }
            public string Icon { get; }
            public string Title { get; }
            public string? SectionHeader { get; }

            public SidebarItemDef(string key, string icon, string title, string? sectionHeader = null)
            {
                Key = key;
                Icon = icon;
                Title = title;
                SectionHeader = sectionHeader;
            }
        }

        private void BuildSidebar(List<SidebarItemDef> items)
        {
            int top = 8;
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.SectionHeader))
                {
                    var lblSec = new Label
                    {
                        Text = item.SectionHeader,
                        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(148, 163, 184),
                        Location = new Point(16, top + 6),
                        Size = new Size(215, 18),
                        UseMnemonic = false
                    };
                    pnlSidebar.Controls.Add(lblSec);
                    top += 26;
                }

                var btn = new SidebarMenuItem(item.Key, item.Icon, item.Title)
                {
                    Location = new Point(0, top),
                    Size = new Size(245, 44)
                };

                btn.Click += (s, e) => NavigateTo(item.Key);

                _menuButtons.Add(btn);
                pnlSidebar.Controls.Add(btn);

                top += 46;
            }
        }

        private void RefreshCurrentMenuHighlight()
        {
            foreach (var btn in _menuButtons)
            {
                btn.IsActive = (btn.Key == _currentKey);
            }
        }

        public void NavigateTo(string key)
        {
            string mainKey = key.Contains(':') ? key.Split(':')[0] : key;
            string? subFilter = key.Contains(':') ? key.Split(':')[1] : null;

            _currentKey = mainKey;
            RefreshCurrentMenuHighlight();

            pnlContent.SuspendLayout();

            if (_currentView != null)
            {
                _currentView.Visible = false;
            }

            UserControl targetView = mainKey switch
            {
                "Dashboard" => _viewDashboard ??= CreateDashboard(),
                "AdminDashboard" => _viewAdminDashboard ??= CreateAdminDashboard(),
                "BaiBao" => _viewBaiBao ??= new UcQuanLyBaiBao(),
                "PhanBien" => _viewPhanBien ??= new UcQuanLyPhanBien(),
                "SoTapChi" => _viewSoTapChi ??= new UcQuanLySoTapChi(),
                "ChuyenNganh" => _viewChuyenNganh ??= new UcQuanLyChuyenNganh(),
                "NguoiDung" => _viewNguoiDung ??= new UcQuanLyNguoiDung(),
                "ThongKe" => _viewThongKe ??= new UcThongKeBaoCao(),
                "SaoLuu" => _viewSaoLuu ??= new UcSaoLuuPhucHoi(),
                _ => (_activeRoleMode == "Admin") ? (_viewAdminDashboard ??= CreateAdminDashboard()) : (_viewDashboard ??= CreateDashboard())
            };

            if (!pnlContent.Controls.Contains(targetView))
            {
                targetView.Dock = DockStyle.Fill;
                pnlContent.Controls.Add(targetView);
            }

            targetView.Visible = true;
            targetView.BringToFront();
            _currentView = targetView;

            pnlContent.ResumeLayout(true);

            if (targetView is UcDashboard d) d.LoadData();
            else if (targetView is UcAdminDashboard ad) ad.LoadData();
            else if (targetView is UcQuanLyBaiBao b)
            {
                if (!string.IsNullOrEmpty(subFilter)) b.SetStatusFilter(subFilter);
                else b.LoadArticles();
            }
            else if (targetView is UcQuanLyNguoiDung u) u.LoadUsers();
            else if (targetView is UcQuanLyPhanBien p) p.LoadData();
            else if (targetView is UcQuanLySoTapChi s) s.LoadIssues();
            else if (targetView is UcQuanLyChuyenNganh cn) cn.LoadData();
            else if (targetView is UcThongKeBaoCao t) t.LoadData();
        }

        private UcDashboard CreateDashboard()
        {
            var dash = new UcDashboard();
            dash.OnNavigateRequested += (dest) => NavigateTo(dest);
            return dash;
        }

        private UcAdminDashboard CreateAdminDashboard()
        {
            var dash = new UcAdminDashboard();
            dash.OnNavigateRequested += (dest) => NavigateTo(dest);
            return dash;
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            var res = MessageBox.Show(LanguageService.Get("LogoutConfirm"), LanguageService.Get("LogoutTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                AuthService.Logout();
                Hide();
                var frmLogin = new FrmLogin();
                frmLogin.Show();
            }
        }

        private class SidebarMenuItem : UserControl
        {
            public string Key { get; }
            private readonly string _icon;
            private readonly string _title;
            private bool _isActive = false;
            private bool _isHovered = false;

            public bool IsActive
            {
                get => _isActive;
                set { _isActive = value; Invalidate(); }
            }

            public SidebarMenuItem(string key, string icon, string title)
            {
                Key = key;
                _icon = icon;
                _title = title;

                DoubleBuffered = true;
                Cursor = Cursors.Hand;
                BackColor = UITheme.SidebarBg;

                MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
                MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Color bg = _isActive ? UITheme.SidebarActive : (_isHovered ? UITheme.SidebarHover : UITheme.SidebarBg);
                using (var brush = new SolidBrush(bg))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }

                if (_isActive)
                {
                    using var barBrush = new SolidBrush(UITheme.Primary);
                    g.FillRectangle(barBrush, 0, 4, 4, Height - 8);
                }

                // Draw Icon
                using (var brush = new SolidBrush(_isActive ? UITheme.Primary : (_isHovered ? UITheme.PrimaryDark : Color.FromArgb(100, 116, 139))))
                {
                    using var iconFont = new Font("Segoe UI Emoji", 10.5f);
                    g.DrawString(_icon, iconFont, brush, new PointF(18, 13));
                }

                // Draw Title Text (Vertically centered, never clipped)
                Color textColor = _isActive ? UITheme.SidebarTextActive : (_isHovered ? UITheme.SidebarTextActive : UITheme.SidebarText);
                using (var brush = new SolidBrush(textColor))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    Font textFont = _isActive ? UITheme.FontBodyBold : UITheme.FontBody;
                    var textRect = new RectangleF(48, 0, Width - 54, Height);
                    g.DrawString(_title, textFont, brush, textRect, sf);
                }
            }
        }

        private class FlagButton : Control
        {
            public string LanguageCode { get; }
            private bool _isActive = false;
            private bool _isHovered = false;

            public bool IsActive
            {
                get => _isActive;
                set { _isActive = value; Invalidate(); }
            }

            public FlagButton(string langCode)
            {
                LanguageCode = langCode;
                Size = new Size(34, 24);
                Cursor = Cursors.Hand;
                DoubleBuffered = true;

                MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
                MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                var flagRect = new Rectangle(1, 1, Width - 3, Height - 3);

                if (LanguageCode == "vi")
                {
                    // Red field for Vietnam flag
                    using var redBrush = new SolidBrush(Color.FromArgb(218, 37, 29));
                    g.FillRectangle(redBrush, flagRect);

                    // Gold star in center
                    float cx = flagRect.X + flagRect.Width / 2f;
                    float cy = flagRect.Y + flagRect.Height / 2f;
                    float rOuter = flagRect.Height * 0.35f;
                    float rInner = rOuter * 0.382f;
                    PointF[] pts = new PointF[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float r = (i % 2 == 0) ? rOuter : rInner;
                        double angle = -Math.PI / 2 + i * Math.PI / 5;
                        pts[i] = new PointF(cx + (float)(r * Math.Cos(angle)), cy + (float)(r * Math.Sin(angle)));
                    }
                    using var goldBrush = new SolidBrush(Color.FromArgb(255, 255, 0));
                    g.FillPolygon(goldBrush, pts);
                }
                else
                {
                    // UK Union Jack flag
                    using var navyBrush = new SolidBrush(Color.FromArgb(1, 33, 105));
                    g.FillRectangle(navyBrush, flagRect);

                    // White diagonal saltire
                    using var whitePen4 = new Pen(Color.White, 3);
                    g.DrawLine(whitePen4, flagRect.Left, flagRect.Top, flagRect.Right, flagRect.Bottom);
                    g.DrawLine(whitePen4, flagRect.Right, flagRect.Top, flagRect.Left, flagRect.Bottom);

                    // Red diagonal saltire
                    using var redPen2 = new Pen(Color.FromArgb(200, 16, 46), 1.5f);
                    g.DrawLine(redPen2, flagRect.Left, flagRect.Top, flagRect.Right, flagRect.Bottom);
                    g.DrawLine(redPen2, flagRect.Right, flagRect.Top, flagRect.Left, flagRect.Bottom);

                    // White cross
                    using var whiteBrush = new SolidBrush(Color.White);
                    int midX = flagRect.X + flagRect.Width / 2;
                    int midY = flagRect.Y + flagRect.Height / 2;
                    g.FillRectangle(whiteBrush, midX - 3, flagRect.Top, 6, flagRect.Height);
                    g.FillRectangle(whiteBrush, flagRect.Left, midY - 3, flagRect.Width, 6);

                    // Red cross
                    using var redBrush = new SolidBrush(Color.FromArgb(200, 16, 46));
                    g.FillRectangle(redBrush, midX - 1, flagRect.Top, 3, flagRect.Height);
                    g.FillRectangle(redBrush, flagRect.Left, midY - 1, flagRect.Width, 3);
                }

                // Border outline
                Color borderColor = _isActive ? Color.FromArgb(255, 235, 59) : (_isHovered ? Color.White : Color.FromArgb(140, 255, 255, 255));
                int borderWidth = _isActive ? 2 : 1;
                using var borderPen = new Pen(borderColor, borderWidth);
                g.DrawRectangle(borderPen, flagRect);
            }
        }
    }
}
