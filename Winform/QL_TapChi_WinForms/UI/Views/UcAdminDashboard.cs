using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Forms;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Views
{
    public class UcAdminDashboard : UserControl
    {
        private MetricCardControl cardUsers = null!;
        private MetricCardControl cardReviewers = null!;
        private MetricCardControl cardDatabase = null!;
        private MetricCardControl cardAuditCount = null!;
        private DataGridView dgvRolesDistribution = null!;
        private DataGridView dgvRecentAudit = null!;
        private Label lblEyebrow = null!;
        private Label lblHeaderTitle = null!;
        private Label lblAdminGreeting = null!;
        private Label lblLeftTitle = null!;
        private Label lblRightTitle = null!;
        private ModernButton btnGoUsers = null!;
        private ModernButton btnBackupQuick = null!;

        public event Action<string>? OnNavigateRequested;

        public UcAdminDashboard()
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

        private void InitializeComponent()
        {
            BackColor = UITheme.AppBackground;
            Dock = DockStyle.Fill;
            AutoScroll = true;

            // Top Spec-sheet Title Bar
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                Padding = new Padding(24, 14, 24, 8),
                BackColor = Color.White
            };
            pnlTop.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            lblEyebrow = new Label
            {
                Text = LanguageService.Get("AdminDash_Eyebrow"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.Primary,
                Location = new Point(24, 12),
                AutoSize = true,
                UseMnemonic = false
            };

            lblHeaderTitle = new Label
            {
                Text = LanguageService.Get("AdminDash_Title"),
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                Location = new Point(24, 30),
                AutoSize = true,
                UseMnemonic = false
            };

            lblAdminGreeting = new Label
            {
                Text = LanguageService.Get("AdminDash_Subtitle"),
                Font = UITheme.FontBody,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(25, 58),
                AutoSize = true,
                UseMnemonic = false
            };

            pnlTop.Controls.AddRange(new Control[] { lblEyebrow, lblHeaderTitle, lblAdminGreeting });

            // Responsive Metrics Grid (4 equal columns, adapts to screen width)
            var tblMetrics = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 112,
                Padding = new Padding(24, 12, 24, 8),
                BackColor = UITheme.AppBackground,
                RowCount = 1,
                ColumnCount = 4
            };
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            cardUsers = new MetricCardControl
            {
                Title = "Tài khoản hệ thống",
                Value = "0",
                Subtext = "Người dùng đã đăng ký",
                AccentColor = UITheme.Primary,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };

            cardReviewers = new MetricCardControl
            {
                Title = "Chuyên gia phản biện",
                Value = "0",
                Subtext = "Hội đồng thẩm định",
                AccentColor = UITheme.Info,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };

            cardDatabase = new MetricCardControl
            {
                Title = "Cơ sở dữ liệu",
                Value = "Sẵn sàng",
                Subtext = "SQL Server trực tuyến",
                AccentColor = UITheme.Success,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };

            cardAuditCount = new MetricCardControl
            {
                Title = "Nhật ký hệ thống",
                Value = "0",
                Subtext = "Lịch sử Audit Trail",
                AccentColor = UITheme.Purple,
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0)
            };

            tblMetrics.Controls.Add(cardUsers, 0, 0);
            tblMetrics.Controls.Add(cardReviewers, 1, 0);
            tblMetrics.Controls.Add(cardDatabase, 2, 0);
            tblMetrics.Controls.Add(cardAuditCount, 3, 0);

            cardUsers.Click += (s, e) => OnNavigateRequested?.Invoke("NguoiDung");
            cardReviewers.Click += (s, e) => OnNavigateRequested?.Invoke("NguoiDung");
            cardDatabase.Click += (s, e) => OnNavigateRequested?.Invoke("SaoLuu");
            cardAuditCount.Click += (s, e) => OnNavigateRequested?.Invoke("ThongKe");

            // Content Panel (Split 2 Columns)
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 10, 24, 24),
                BackColor = UITheme.AppBackground
            };

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            split.SizeChanged += (s, e) =>
            {
                if (split.Width > 500)
                {
                    try
                    {
                        split.SplitterDistance = (int)(split.Width * 0.41);
                    }
                    catch { }
                }
            };

            // Left Card: User Role Distribution
            var pnlLeftCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            pnlLeftCard.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlLeftCard.Width - 1, pnlLeftCard.Height - 1);
            };

            var pnlLeftHeader = new Panel { Dock = DockStyle.Top, Height = 42 };

            lblLeftTitle = new Label
            {
                Text = LanguageService.Get("AdminDash_RolesTitle"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            btnGoUsers = new ModernButton
            {
                Text = LanguageService.Get("AdminDash_BtnManage"),
                Size = new Size(95, 30),
                Dock = DockStyle.Right
            };
            UITheme.ApplySecondaryButton(btnGoUsers);
            btnGoUsers.Click += (s, e) => OnNavigateRequested?.Invoke("NguoiDung");

            pnlLeftHeader.Controls.Add(lblLeftTitle);
            pnlLeftHeader.Controls.Add(btnGoUsers);

            dgvRolesDistribution = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyCompactGridStyle(dgvRolesDistribution, 42);
            dgvRolesDistribution.Columns.Add(new DataGridViewTextBoxColumn { Name = "VaiTro", HeaderText = LanguageService.Get("AdminDash_ColRole"), MinimumWidth = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvRolesDistribution.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoLuong", HeaderText = LanguageService.Get("AdminDash_ColCount"), Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Font = UITheme.FontBodyBold, Alignment = DataGridViewContentAlignment.MiddleCenter } });

            pnlLeftCard.Controls.Add(dgvRolesDistribution);
            pnlLeftCard.Controls.Add(pnlLeftHeader);
            split.Panel1.Controls.Add(pnlLeftCard);

            // Right Card: Recent Audit Logs
            var pnlRightCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            pnlRightCard.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlRightCard.Width - 1, pnlRightCard.Height - 1);
            };

            var pnlRightHeader = new Panel { Dock = DockStyle.Top, Height = 42 };

            lblRightTitle = new Label
            {
                Text = LanguageService.Get("AdminDash_AuditTitle"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            btnBackupQuick = new ModernButton
            {
                Text = LanguageService.Get("AdminDash_BtnBackup"),
                Size = new Size(95, 30),
                Dock = DockStyle.Right
            };
            UITheme.ApplySecondaryButton(btnBackupQuick);
            btnBackupQuick.Click += (s, e) => OnNavigateRequested?.Invoke("SaoLuu");

            pnlRightHeader.Controls.Add(lblRightTitle);
            pnlRightHeader.Controls.Add(btnBackupQuick);

            dgvRecentAudit = new DataGridView { Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
            UITheme.ApplyCompactGridStyle(dgvRecentAudit, 48);
            dgvRecentAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThoiGian", HeaderText = LanguageService.Get("AdminDash_ColTime"), Width = 135, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, WrapMode = DataGridViewTriState.False } });
            dgvRecentAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "NguoiThucHien", HeaderText = LanguageService.Get("AdminDash_ColUser"), Width = 115, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvRecentAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "HanhDong", HeaderText = LanguageService.Get("AdminDash_ColAction"), MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });

            pnlRightCard.Controls.Add(dgvRecentAudit);
            pnlRightCard.Controls.Add(pnlRightHeader);
            split.Panel2.Controls.Add(pnlRightCard);

            pnlContent.Controls.Add(split);

            Controls.Add(pnlContent);
            Controls.Add(tblMetrics);
            Controls.Add(pnlTop);
        }

        public void ApplyLanguage()
        {
            lblEyebrow.Text = LanguageService.Get("AdminDash_Eyebrow");
            lblHeaderTitle.Text = LanguageService.Get("AdminDash_Title");
            lblAdminGreeting.Text = LanguageService.Get("AdminDash_Subtitle");

            cardUsers.Title = LanguageService.Get("AdminDash_CardUsers");
            cardUsers.Subtext = LanguageService.Get("AdminDash_CardUsersSub");

            cardReviewers.Title = LanguageService.Get("AdminDash_CardReviewers");
            cardReviewers.Subtext = LanguageService.Get("AdminDash_CardReviewersSub");

            cardDatabase.Title = LanguageService.Get("AdminDash_CardDB");
            cardDatabase.Value = LanguageService.Get("AdminDash_CardDBVal");
            cardDatabase.Subtext = LanguageService.Get("AdminDash_CardDBSub");

            cardAuditCount.Title = LanguageService.Get("AdminDash_CardAudit");
            cardAuditCount.Subtext = LanguageService.Get("AdminDash_CardAuditSub");

            lblLeftTitle.Text = LanguageService.Get("AdminDash_RolesTitle");
            btnGoUsers.Text = LanguageService.Get("AdminDash_BtnManage");
            if (dgvRolesDistribution.Columns["VaiTro"] != null)
                dgvRolesDistribution.Columns["VaiTro"].HeaderText = LanguageService.Get("AdminDash_ColRole");
            if (dgvRolesDistribution.Columns["SoLuong"] != null)
                dgvRolesDistribution.Columns["SoLuong"].HeaderText = LanguageService.Get("AdminDash_ColCount");

            lblRightTitle.Text = LanguageService.Get("AdminDash_AuditTitle");
            btnBackupQuick.Text = LanguageService.Get("AdminDash_BtnBackup");
            if (dgvRecentAudit.Columns["ThoiGian"] != null)
                dgvRecentAudit.Columns["ThoiGian"].HeaderText = LanguageService.Get("AdminDash_ColTime");
            if (dgvRecentAudit.Columns["NguoiThucHien"] != null)
                dgvRecentAudit.Columns["NguoiThucHien"].HeaderText = LanguageService.Get("AdminDash_ColUser");
            if (dgvRecentAudit.Columns["HanhDong"] != null)
                dgvRecentAudit.Columns["HanhDong"].HeaderText = LanguageService.Get("AdminDash_ColAction");

            LoadData();
        }

        public void LoadData()
        {
            lblAdminGreeting.Text = LanguageService.Get("AdminDash_Subtitle");

            var metrics = ThongKeService.GetDashboardMetrics();
            var metricsError = JournalApiClient.LastError;
            cardUsers.Value = metricsError == null ? metrics.TongNguoiDung.ToString() : "—";
            cardReviewers.Value = metricsError == null ? metrics.TongPhanBien.ToString() : "—";

            bool isApiOnline = JournalApiClient.Get("/api/system/environment") != null;
            if (isApiOnline)
            {
                cardDatabase.Value = (LanguageService.CurrentLanguage == "en") ? "Online" : "Trực tuyến";
                cardDatabase.Subtext = (LanguageService.CurrentLanguage == "en") ? "API and database ready" : "API và cơ sở dữ liệu sẵn sàng";
                cardDatabase.AccentColor = UITheme.Success;
            }
            else
            {
                cardDatabase.Value = (LanguageService.CurrentLanguage == "en") ? "Unavailable" : "Không kết nối";
                cardDatabase.Subtext = JournalApiClient.LastError ?? "Không kết nối được API.";
                cardDatabase.AccentColor = UITheme.Warning;
            }

            dgvRolesDistribution.Rows.Clear();
            foreach (var role in AdminDashboardService.GetRoles())
                dgvRolesDistribution.Rows.Add(LanguageService.TranslateRole(role.TenVaiTro), role.SoLuong.ToString());

            dgvRecentAudit.Rows.Clear();
            var history = AdminDashboardService.GetRecentHistory();
            cardAuditCount.Value = history.Count.ToString();
            foreach (var item in history)
                dgvRecentAudit.Rows.Add(item.NgayChuyen.ToString("dd/MM/yyyy HH:mm"),
                    item.HoTen, $"{item.TrangThaiMoi} - {item.GhiChu}");
        }
    }
}
