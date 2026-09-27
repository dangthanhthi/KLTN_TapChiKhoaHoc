using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmLogin : Form
    {
        private TextBox txtEmail = null!;
        private TextBox txtPassword = null!;
        private Label lblError = null!;
        private ModernButton btnLogin = null!;
        private Button btnRoleEditor = null!;
        private Button btnRoleAdmin = null!;
        private Label lblRoleDescription = null!;

        public FrmLogin()
        {
            InitializeComponent();
            SelectRole("Editor");
        }

        private void InitializeComponent()
        {
            Text = "Đăng nhập hệ thống · Tạp chí Khoa học và Công nghệ";
            Size = new Size(560, 710);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ShowInTaskbar = true;
            BackColor = UITheme.AppBackground;
            Font = UITheme.FontBody;

            // 1. TOP MASTHEAD BANNER (Professional Scholarly Journal Header, NO LOGO)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 115,
                BackColor = UITheme.HeaderBg
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

            // Custom official emblem badge
            var pnlBadge = new Panel
            {
                Size = new Size(58, 58),
                Location = new Point(26, 26),
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
                    e.Graphics.DrawImage(logoHuit, new Rectangle(4, 4, pnlBadge.Width - 8, pnlBadge.Height - 8));
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, "HUIT", new Font("Segoe UI", 12f, FontStyle.Bold), new Rectangle(0, 0, pnlBadge.Width, pnlBadge.Height), UITheme.PrimaryDark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            var lblJournalTitle = new Label
            {
                Text = "TẠP CHÍ KHOA HỌC VÀ CÔNG NGHỆ",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(96, 26),
                AutoSize = true
            };

            var lblJournalSub = new Label
            {
                Text = "JOURNAL OF SCIENCE AND TECHNOLOGY",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 197, 253),
                Location = new Point(98, 55),
                AutoSize = true
            };

            var lblTagline = new Label
            {
                Text = "Hệ thống Quản lý Bản thảo & Quy trình Xuất bản Học thuật",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(98, 75),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { pnlBadge, lblJournalTitle, lblJournalSub, lblTagline });

            // 2. MAIN CARD CONTAINER
            var pnlCard = new Panel
            {
                Size = new Size(484, 525),
                Location = new Point(36, 130),
                BackColor = Color.White
            };
            pnlCard.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            var lblRoleHeader = new Label
            {
                Text = "CHỌN PHÂN HỆ ĐĂNG NHẬP:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(28, 22),
                AutoSize = true
            };

            // Segmented Pill Control for 2 Roles
            btnRoleEditor = new Button
            {
                Text = "Ban Biên Tập",
                Location = new Point(28, 48),
                Size = new Size(210, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRoleEditor.Click += (s, e) => SelectRole("Editor");

            btnRoleAdmin = new Button
            {
                Text = "Quản Trị Hệ Thống",
                Location = new Point(246, 48),
                Size = new Size(210, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRoleAdmin.Click += (s, e) => SelectRole("Admin");

            lblRoleDescription = new Label
            {
                Text = "Tài khoản mẫu: Quản lý bản thảo, phản biện và xuất bản",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(28, 102),
                Size = new Size(428, 20),
                AutoEllipsis = true
            };

            var pnlDivider = new Panel
            {
                Location = new Point(28, 136),
                Size = new Size(428, 1),
                BackColor = UITheme.BorderColor
            };

            // Email Label & Input
            var lblEmail = new Label
            {
                Text = "Địa chỉ Email tài khoản:",
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(28, 160),
                AutoSize = true
            };

            txtEmail = new TextBox
            {
                Text = "editor@huit.edu.vn",
                Font = new Font("Segoe UI", 11f),
                Location = new Point(28, 185),
                Size = new Size(428, 34)
            };

            // Password Label & Input
            var lblPassword = new Label
            {
                Text = "Mật khẩu truy cập:",
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(28, 235),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Text = "123456",
                Font = new Font("Segoe UI", 11f),
                Location = new Point(28, 260),
                Size = new Size(428, 34),
                UseSystemPasswordChar = true
            };

            lblError = new Label
            {
                Text = "",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.Danger,
                Location = new Point(28, 304),
                Size = new Size(428, 24)
            };

            // Login Button
            btnLogin = new ModernButton
            {
                Text = "ĐĂNG NHẬP VÀO HỆ THỐNG",
                Location = new Point(28, 335),
                Size = new Size(428, 46),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryDark
            };
            btnLogin.Click += BtnLogin_Click;

            // Bottom Informational Panel
            var pnlBottomInfo = new Panel
            {
                Location = new Point(28, 400),
                Size = new Size(428, 98),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlBottomInfo.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlBottomInfo.Width - 1, pnlBottomInfo.Height - 1);
            };

            var lblHelpText = new Label
            {
                Text = "Đăng nhập bằng tài khoản tòa soạn đã được cấp.\n" +
                       "Quyền làm việc được lấy từ Backend API theo tài khoản của bạn.",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(14, 12),
                Size = new Size(400, 75)
            };
            pnlBottomInfo.Controls.Add(lblHelpText);

            pnlCard.Controls.AddRange(new Control[]
            {
                lblRoleHeader, btnRoleEditor, btnRoleAdmin, lblRoleDescription, pnlDivider,
                lblEmail, txtEmail,
                lblPassword, txtPassword,
                lblError, btnLogin,
                pnlBottomInfo
            });

            Controls.AddRange(new Control[] { pnlHeader, pnlCard });
            AcceptButton = btnLogin;
        }

        private void SelectRole(string role)
        {
            if (role == "Editor")
            {
                btnRoleEditor.BackColor = UITheme.Primary;
                btnRoleEditor.ForeColor = Color.White;
                btnRoleEditor.FlatAppearance.BorderSize = 0;

                btnRoleAdmin.BackColor = Color.FromArgb(241, 245, 249);
                btnRoleAdmin.ForeColor = UITheme.TextSecondary;
                btnRoleAdmin.FlatAppearance.BorderColor = UITheme.BorderColor;
                btnRoleAdmin.FlatAppearance.BorderSize = 1;

                lblRoleDescription.Text = "Quản lý bản thảo, phản biện và xuất bản";
                lblRoleDescription.ForeColor = UITheme.Primary;
                btnLogin.NormalColor = UITheme.Primary;
                btnLogin.HoverColor = UITheme.PrimaryDark;
            }
            else
            {
                btnRoleAdmin.BackColor = Color.FromArgb(15, 23, 42); // Deep Slate
                btnRoleAdmin.ForeColor = Color.White;
                btnRoleAdmin.FlatAppearance.BorderSize = 0;

                btnRoleEditor.BackColor = Color.FromArgb(241, 245, 249);
                btnRoleEditor.ForeColor = UITheme.TextSecondary;
                btnRoleEditor.FlatAppearance.BorderColor = UITheme.BorderColor;
                btnRoleEditor.FlatAppearance.BorderSize = 1;

                lblRoleDescription.Text = "Quản lý người dùng, phân quyền và sao lưu";
                lblRoleDescription.ForeColor = Color.FromArgb(15, 23, 42);
                btnLogin.NormalColor = Color.FromArgb(15, 23, 42);
                btnLogin.HoverColor = Color.FromArgb(30, 41, 59);
            }
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "Đang xác thực thông tin tài khoản...";
            lblError.ForeColor = UITheme.Primary;
            Application.DoEvents();

            bool success = AuthService.Login(txtEmail.Text.Trim(), txtPassword.Text.Trim());
            if (success)
            {
                lblError.Text = "Đăng nhập thành công! Đang mở giao diện làm việc...";
                lblError.ForeColor = UITheme.Success;
                Application.DoEvents();

                var frmMain = new FrmMain();
                frmMain.FormClosed += (s, args) => Application.Exit();
                frmMain.Show();
                this.Hide();
            }
            else
            {
                lblError.Text = AuthService.LastError ?? "Email hoặc mật khẩu không chính xác!";
                lblError.ForeColor = UITheme.Danger;
            }
        }
    }
}
