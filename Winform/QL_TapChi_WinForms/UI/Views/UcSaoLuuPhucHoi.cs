using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Views
{
    public class UcSaoLuuPhucHoi : UserControl
    {
        private Label lblTitle = null!;
        private Label lblBackupSection = null!;
        private Label lblFolderDesc = null!;
        private Label lblFolder = null!;
        private ModernButton btnBrowseFolder = null!;
        private Label lblRestoreSection = null!;
        private Label lblRestoreDesc = null!;
        private Label lblFile = null!;
        private ModernButton btnBrowseFile = null!;
        private Label lblLog = null!;
        private TextBox txtBackupFolder = null!;
        private TextBox txtRestoreFile = null!;
        private TextBox txtLog = null!;
        private ModernButton btnBackup = null!;
        private ModernButton btnRestore = null!;

        public UcSaoLuuPhucHoi()
        {
            InitializeComponent();
            ApplyLanguage();
            LanguageService.OnLanguageChanged += ApplyLanguage;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageService.OnLanguageChanged -= ApplyLanguage;
            }
            base.Dispose(disposing);
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = LanguageService.Get("Backup_Title");
            lblBackupSection.Text = LanguageService.Get("Backup_Section1");
            lblFolderDesc.Text = LanguageService.Get("Backup_Desc1");
            lblFolder.Text = LanguageService.Get("Backup_FolderLabel");
            btnBrowseFolder.Text = LanguageService.Get("Backup_BtnBrowse");
            btnBackup.Text = LanguageService.Get("Backup_BtnRun");

            lblRestoreSection.Text = LanguageService.Get("Backup_Section2");
            lblRestoreDesc.Text = LanguageService.Get("Backup_Desc2");
            lblFile.Text = LanguageService.Get("Backup_FileLabel");
            txtRestoreFile.PlaceholderText = LanguageService.Get("Backup_PlaceholderFile");
            btnBrowseFile.Text = LanguageService.Get("Backup_BtnBrowseFile");
            btnRestore.Text = LanguageService.Get("Backup_BtnRestore");

            lblLog.Text = LanguageService.Get("Backup_LogLabel");
        }

        private void InitializeComponent()
        {
            BackColor = UITheme.AppBackground;
            Dock = DockStyle.Fill;
            Padding = new Padding(24);

            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(20, 14, 20, 10)
            };
            pnlToolbar.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlToolbar.Width - 1, pnlToolbar.Height - 1);
            };

            var pnlTitleRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = LanguageService.Get("Backup_Title"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlTitleRow.Controls.Add(lblTitle);

            pnlToolbar.Controls.Add(pnlTitleRow);

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            pnlContent.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlContent.Width - 1, pnlContent.Height - 1);
            };

            // Section 1: Backup
            lblBackupSection = new Label
            {
                Text = LanguageService.Get("Backup_Section1"),
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 18),
                AutoSize = true,
                UseMnemonic = false
            };

            lblFolderDesc = new Label
            {
                Text = LanguageService.Get("Backup_Desc1"),
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 42),
                Size = new Size(820, 20),
                AutoEllipsis = true,
                UseMnemonic = false
            };

            lblFolder = new Label
            {
                Text = LanguageService.Get("Backup_FolderLabel"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(24, 68),
                AutoSize = true,
                UseMnemonic = false
            };

            txtBackupFolder = new TextBox
            {
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DatabaseBackups"),
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(330, 30),
                Margin = new Padding(0, 3, 10, 0)
            };

            btnBrowseFolder = new ModernButton
            {
                Text = LanguageService.Get("Backup_BtnBrowse"),
                Size = new Size(125, 34),
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.ApplySecondaryButton(btnBrowseFolder);
            btnBrowseFolder.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK) txtBackupFolder.Text = fbd.SelectedPath;
            };

            btnBackup = new ModernButton
            {
                Text = LanguageService.Get("Backup_BtnRun"),
                Size = new Size(150, 34),
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplyPrimaryButton(btnBackup);
            btnBackup.Click += BtnBackup_Click;

            var flpBackupRow = new FlowLayoutPanel
            {
                Location = new Point(24, 90),
                Size = new Size(800, 42),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            flpBackupRow.Controls.Add(txtBackupFolder);
            flpBackupRow.Controls.Add(btnBrowseFolder);
            flpBackupRow.Controls.Add(btnBackup);

            // Section 2: Restore
            lblRestoreSection = new Label
            {
                Text = LanguageService.Get("Backup_Section2"),
                Font = UITheme.FontBodyBold,
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(24, 142),
                AutoSize = true,
                UseMnemonic = false
            };

            lblRestoreDesc = new Label
            {
                Text = LanguageService.Get("Backup_Desc2"),
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 166),
                Size = new Size(670, 20),
                AutoEllipsis = true,
                UseMnemonic = false
            };

            lblFile = new Label
            {
                Text = LanguageService.Get("Backup_FileLabel"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(24, 192),
                AutoSize = true,
                UseMnemonic = false
            };

            txtRestoreFile = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(330, 30),
                PlaceholderText = LanguageService.Get("Backup_PlaceholderFile"),
                Margin = new Padding(0, 3, 10, 0)
            };

            btnBrowseFile = new ModernButton
            {
                Text = LanguageService.Get("Backup_BtnBrowseFile"),
                Size = new Size(125, 34),
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.ApplySecondaryButton(btnBrowseFile);
            btnBrowseFile.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog { Filter = "Tệp sao lưu (*.bak)|*.bak" };
                if (ofd.ShowDialog() == DialogResult.OK) txtRestoreFile.Text = ofd.FileName;
            };

            btnRestore = new ModernButton
            {
                Text = LanguageService.Get("Backup_BtnRestore"),
                Size = new Size(150, 34),
                NormalColor = UITheme.Danger,
                HoverColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnRestore.Click += BtnRestore_Click;

            var flpRestoreRow = new FlowLayoutPanel
            {
                Location = new Point(24, 214),
                Size = new Size(800, 42),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            flpRestoreRow.Controls.Add(txtRestoreFile);
            flpRestoreRow.Controls.Add(btnBrowseFile);
            flpRestoreRow.Controls.Add(btnRestore);

            // Section 3: Log
            lblLog = new Label
            {
                Text = LanguageService.Get("Backup_LogLabel"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 264),
                AutoSize = true,
                UseMnemonic = false
            };

            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.FromArgb(248, 250, 252),
                Location = new Point(24, 288),
                Size = new Size(720, 160)
            };

            pnlContent.Resize += (s, e) =>
            {
                int newHeight = pnlContent.Height - 315;
                if (newHeight > 90) txtLog.Height = newHeight;
                txtLog.Width = Math.Max(300, pnlContent.Width - 48);
            };

            pnlContent.Controls.AddRange(new Control[]
            {
                lblBackupSection, lblFolderDesc, lblFolder, flpBackupRow,
                lblRestoreSection, lblRestoreDesc, lblFile, flpRestoreRow,
                lblLog, txtLog
            });

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

            Controls.Add(pnlContent);
            Controls.Add(pnlSpacer);
            Controls.Add(pnlToolbar);

            Log("Hệ thống sao lưu & phục hồi CSDL QL_TapChiKhoaHoc sẵn sàng hoạt động.");
        }

        private void Log(string msg)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
        }

        private void BtnBackup_Click(object? sender, EventArgs e)
        {
            string folder = txtBackupFolder.Text.Trim();
            if (string.IsNullOrEmpty(folder))
            {
                MessageBox.Show("Vui lòng nhập hoặc chọn thư mục lưu trữ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Log("Bắt đầu sao lưu cơ sở dữ liệu QL_TapChiKhoaHoc...");
            btnBackup.Enabled = false;
            Application.DoEvents();

            var (ok, msg) = BackupRestoreService.BackupDatabase(folder);
            btnBackup.Enabled = true;

            Log(msg);
            if (ok)
            {
                MessageBox.Show(msg, "Sao lưu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(msg, "Lỗi sao lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestore_Click(object? sender, EventArgs e)
        {
            string bakPath = txtRestoreFile.Text.Trim();
            if (!File.Exists(bakPath))
            {
                MessageBox.Show("Vui lòng chọn tệp bản sao .bak hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "CẢNH BÁO: Quá trình phục hồi sẽ ghi đè toàn bộ dữ liệu hiện tại bằng nội dung trong tệp sao lưu!\nBạn có chắc chắn muốn tiếp tục?",
                "Xác nhận phục hồi dữ liệu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            Log($"Bắt đầu phục hồi cơ sở dữ liệu từ tệp: {bakPath}...");
            btnRestore.Enabled = false;
            Application.DoEvents();

            var (ok, msg) = BackupRestoreService.RestoreDatabase(bakPath);
            btnRestore.Enabled = true;

            Log(msg);
            if (ok)
            {
                MessageBox.Show(msg, "Phục hồi thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(msg, "Lỗi phục hồi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
