using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Forms;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Views
{
    public class UcDashboard : UserControl
    {
        private MetricCardControl cardScreening = null!;
        private MetricCardControl cardReview = null!;
        private MetricCardControl cardDecision = null!;
        private MetricCardControl cardPublished = null!;

        private Label lblEyebrow = null!;
        private Label lblHeaderTitle = null!;
        private Label lblUserGreeting = null!;
        private Label lblUrgentTitle = null!;
        private Label lblUrgent1Desc = null!;
        private Label lblUrgent2Desc = null!;
        private Label lblUrgent3Desc = null!;
        private Label lblWorkflowTitle = null!;
        private Label lblGridTitle = null!;

        private Panel pnlUrgentQueue = null!;
        private Panel pnlWorkflow = null!;
        private DataGridView dgvRecent = null!;
        private ModernButton btnViewAll = null!;
        private ModernButton btnAct1 = null!;
        private ModernButton btnAct2 = null!;
        private ModernButton btnAct3 = null!;

        private readonly List<Button> _workflowStepButtons = new();
        private int _urgentScreeningId = 0;
        private int _urgentDecisionId = 0;

        public event Action<string>? OnNavigateRequested;

        public UcDashboard()
        {
            InitializeComponent();
            ApplyLanguage();
            LanguageService.OnLanguageChanged += ApplyLanguage;
            LoadData();
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

            // 1. Top Header Banner
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(24, 12, 24, 6),
                BackColor = Color.White
            };
            pnlTop.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            lblEyebrow = new Label
            {
                Text = "TÒA SOẠN TẠP CHÍ KHOA HỌC & CÔNG NGHỆ (JST - HUIT)",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.Primary,
                Location = new Point(24, 10),
                AutoSize = true,
                UseMnemonic = false
            };

            lblHeaderTitle = new Label
            {
                Text = "Bàn làm việc Ban Thư ký & Biên tập",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                Location = new Point(24, 28),
                AutoSize = true,
                UseMnemonic = false
            };

            lblUserGreeting = new Label
            {
                Text = "Chào mừng bạn trở lại! Đang tải số liệu công việc hôm nay...",
                Font = UITheme.FontBody,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(25, 54),
                AutoSize = true,
                UseMnemonic = false
            };

            pnlTop.Controls.AddRange(new Control[] { lblEyebrow, lblHeaderTitle, lblUserGreeting });

            // 2. Metrics 4 Cards Row
            var tblMetrics = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 104,
                Padding = new Padding(24, 10, 24, 6),
                BackColor = UITheme.AppBackground,
                RowCount = 1,
                ColumnCount = 4
            };
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            cardScreening = new MetricCardControl
            {
                Title = "Chờ sơ duyệt",
                Value = "0",
                Subtext = "Soi đạo văn & thể lệ",
                AccentColor = Color.FromArgb(234, 88, 12), // Orange Warning
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0)
            };

            cardReview = new MetricCardControl
            {
                Title = "Đang phản biện",
                Value = "0",
                Subtext = "Đang giao chuyên gia",
                AccentColor = UITheme.Primary,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 0, 3, 0)
            };

            cardDecision = new MetricCardControl
            {
                Title = "Chờ quyết định",
                Value = "0",
                Subtext = "Hội đồng ra quyết định",
                AccentColor = UITheme.Danger,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 0, 3, 0)
            };

            cardPublished = new MetricCardControl
            {
                Title = "Đã xuất bản",
                Value = "0",
                Subtext = "Đã cấp số & DOI",
                AccentColor = UITheme.Success,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0)
            };

            tblMetrics.Controls.Add(cardScreening, 0, 0);
            tblMetrics.Controls.Add(cardReview, 1, 0);
            tblMetrics.Controls.Add(cardDecision, 2, 0);
            tblMetrics.Controls.Add(cardPublished, 3, 0);

            cardScreening.Click += (s, e) => OnNavigateRequested?.Invoke("BaiBao:sơ duyệt");
            cardReview.Click += (s, e) => OnNavigateRequested?.Invoke("BaiBao:phản biện");
            cardDecision.Click += (s, e) => OnNavigateRequested?.Invoke("BaiBao:quyết định");
            cardPublished.Click += (s, e) => OnNavigateRequested?.Invoke("SoTapChi");

            // 3. Urgent Action Queue (Hàng đợi Tác vụ Cần Xử lý Ngay)
            pnlUrgentQueue = CreateUrgentQueuePanel();

            // 4. Interactive 5-stage Workflow Stepper Bar
            pnlWorkflow = CreateWorkflowStepperPanel();

            // 5. Bottom Panel: Recent Submissions Grid
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 4, 24, 20),
                BackColor = UITheme.AppBackground
            };

            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            pnlCard.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            var pnlCardHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38
            };

            lblGridTitle = new Label
            {
                Text = "Bản thảo đang xử lý & Cập nhật gần đây",
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };

            btnViewAll = new ModernButton
            {
                Text = "Toàn bộ bài báo →",
                Size = new Size(160, 30),
                Dock = DockStyle.Right
            };
            UITheme.ApplySecondaryButton(btnViewAll);
            btnViewAll.Click += (s, e) => OnNavigateRequested?.Invoke("BaiBao");

            pnlCardHeader.Controls.Add(lblGridTitle);
            pnlCardHeader.Controls.Add(btnViewAll);

            dgvRecent = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyCompactGridStyle(dgvRecent, 46);
            SetupGridColumns();

            dgvRecent.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvRecent.Rows[e.RowIndex].Tag is BaiBao bb)
                {
                    OpenArticleDetail(bb.MaBaiBao);
                }
            };

            dgvRecent.CellPainting += DgvRecent_CellPainting;

            pnlCard.Controls.Add(dgvRecent);
            pnlCard.Controls.Add(pnlCardHeader);

            pnlContent.Controls.Add(pnlCard);

            // Add all controls into user control (Order: Bottom to Top docking)
            Controls.Add(pnlContent);
            Controls.Add(pnlWorkflow);
            Controls.Add(pnlUrgentQueue);
            Controls.Add(tblMetrics);
            Controls.Add(pnlTop);
        }

        private Panel CreateUrgentQueuePanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 118,
                Padding = new Padding(24, 4, 24, 6),
                BackColor = UITheme.AppBackground
            };

            var pnlInner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14, 8, 14, 8)
            };
            pnlInner.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(254, 202, 202)); // Soft red/rose border
                e.Graphics.DrawRectangle(p, 0, 0, pnlInner.Width - 1, pnlInner.Height - 1);
            };

            lblUrgentTitle = new Label
            {
                Text = "DANH SÁCH TÁC VỤ CẦN XỬ LÝ (ƯU TIÊN)",
                Font = UITheme.FontSmallBold,
                ForeColor = Color.FromArgb(185, 28, 28),
                Dock = DockStyle.Top,
                Height = 20,
                UseMnemonic = false
            };

            var tblCards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            tblCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            // Task 1: Sơ duyệt
            var pnlTask1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(255, 247, 237), Margin = new Padding(0, 0, 4, 0), Padding = new Padding(8, 4, 8, 4) };
            var tblTask1 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = Color.Transparent };
            tblTask1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblTask1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTask1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblUrgent1Desc = new Label
            {
                Text = "Bài mới chờ sơ duyệt\nChưa kiểm tra đạo văn",
                Font = UITheme.FontSmall,
                ForeColor = Color.FromArgb(154, 52, 18),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            btnAct1 = new ModernButton
            {
                Text = "Sơ duyệt bài",
                Anchor = AnchorStyles.None,
                AutoSize = false,
                Size = new Size(102, 34),
                NormalColor = Color.FromArgb(234, 88, 12),
                HoverColor = Color.FromArgb(194, 65, 12),
                ForeColor = Color.White,
                Font = UITheme.FontSmallBold
            };
            btnAct1.Click += (s, e) =>
            {
                if (_urgentScreeningId > 0) OpenArticleDetail(_urgentScreeningId);
                else OnNavigateRequested?.Invoke("BaiBao:sơ duyệt");
            };
            tblTask1.Controls.Add(lblUrgent1Desc, 0, 0);
            tblTask1.Controls.Add(btnAct1, 1, 0);
            pnlTask1.Controls.Add(tblTask1);

            // Task 2: Quyết định
            var pnlTask2 = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(254, 242, 242), Margin = new Padding(3, 0, 3, 0), Padding = new Padding(8, 4, 8, 4) };
            var tblTask2 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = Color.Transparent };
            tblTask2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblTask2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTask2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblUrgent2Desc = new Label
            {
                Text = "Đã có điểm đánh giá\nChờ Hội đồng ra QĐ",
                Font = UITheme.FontSmall,
                ForeColor = Color.FromArgb(153, 27, 27),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            btnAct2 = new ModernButton
            {
                Text = "Ra quyết định",
                Anchor = AnchorStyles.None,
                AutoSize = false,
                Size = new Size(112, 34),
                NormalColor = UITheme.Danger,
                HoverColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                Font = UITheme.FontSmallBold
            };
            btnAct2.Click += (s, e) =>
            {
                if (_urgentDecisionId > 0) OpenArticleDetail(_urgentDecisionId);
                else OnNavigateRequested?.Invoke("BaiBao:quyết định");
            };
            tblTask2.Controls.Add(lblUrgent2Desc, 0, 0);
            tblTask2.Controls.Add(btnAct2, 1, 0);
            pnlTask2.Controls.Add(tblTask2);

            // Task 3: Phân công PB
            var pnlTask3 = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 249, 255), Margin = new Padding(4, 0, 0, 0), Padding = new Padding(8, 4, 8, 4) };
            var tblTask3 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = Color.Transparent };
            tblTask3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblTask3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTask3.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblUrgent3Desc = new Label
            {
                Text = "Quản lý phản biện\nGiao bài & nhắc hạn",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.PrimaryDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            btnAct3 = new ModernButton
            {
                Text = "Giao PB ngay",
                Anchor = AnchorStyles.None,
                AutoSize = false,
                Size = new Size(108, 34),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryHover,
                ForeColor = Color.White,
                Font = UITheme.FontSmallBold
            };
            btnAct3.Click += (s, e) => OnNavigateRequested?.Invoke("PhanBien");
            tblTask3.Controls.Add(lblUrgent3Desc, 0, 0);
            tblTask3.Controls.Add(btnAct3, 1, 0);
            pnlTask3.Controls.Add(tblTask3);

            tblCards.Controls.Add(pnlTask1, 0, 0);
            tblCards.Controls.Add(pnlTask2, 1, 0);
            tblCards.Controls.Add(pnlTask3, 2, 0);

            pnlInner.Controls.Add(tblCards);
            pnlInner.Controls.Add(lblUrgentTitle);
            pnl.Controls.Add(pnlInner);

            return pnl;
        }

        private Panel CreateWorkflowStepperPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(24, 2, 24, 6),
                BackColor = UITheme.AppBackground
            };

            var pnlInner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12, 4, 12, 4)
            };
            pnlInner.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlInner.Width - 1, pnlInner.Height - 1);
            };

            var flpContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            lblWorkflowTitle = new Label
            {
                Text = "QUY TRÌNH:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0),
                UseMnemonic = false
            };
            flpContainer.Controls.Add(lblWorkflowTitle);

            string[] stepLabels = new[]
            {
                "1. Sơ duyệt",
                "2. Phản biện",
                "3. Quyết định",
                "4. Chế bản",
                "5. Xuất bản"
            };

            string[] stepFilterKeys = new[]
            {
                "BaiBao:sơ duyệt",
                "BaiBao:phản biện",
                "BaiBao:quyết định",
                "BaiBao:chế bản",
                "SoTapChi"
            };

            _workflowStepButtons.Clear();
            for (int i = 0; i < stepLabels.Length; i++)
            {
                int stepIdx = i;
                var btnStep = new Button
                {
                    Text = stepLabels[i],
                    Height = 28,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = UITheme.PrimaryDark,
                    BackColor = Color.FromArgb(243, 247, 252),
                    Margin = new Padding(0, 1, 0, 0),
                    Padding = new Padding(3, 0, 3, 0),
                    Cursor = Cursors.Hand
                };
                btnStep.FlatAppearance.BorderColor = Color.FromArgb(219, 234, 254);
                btnStep.Click += (s, e) => OnNavigateRequested?.Invoke(stepFilterKeys[stepIdx]);

                _workflowStepButtons.Add(btnStep);
                flpContainer.Controls.Add(btnStep);

                if (i < stepLabels.Length - 1)
                {
                    var lblArrow = new Label
                    {
                        Text = "→",
                        ForeColor = Color.FromArgb(148, 163, 184),
                        Font = UITheme.FontBodyBold,
                        AutoSize = true,
                        Margin = new Padding(2, 5, 2, 0)
                    };
                    flpContainer.Controls.Add(lblArrow);
                }
            }

            pnlInner.Controls.Add(flpContainer);
            pnl.Controls.Add(pnlInner);

            return pnl;
        }

        private void SetupGridColumns()
        {
            dgvRecent.Columns.Clear();

            dgvRecent.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaDinhDanh",
                HeaderText = LanguageService.Get("Col_ArticleCode"),
                Width = 75,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    ForeColor = UITheme.PrimaryDark,
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            dgvRecent.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TieuDe",
                HeaderText = LanguageService.Get("Col_ArticleTitle"),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 140,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvRecent.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ChuyenNganh",
                HeaderText = LanguageService.Get("Col_Field"),
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvRecent.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TacGia",
                HeaderText = LanguageService.Get("Col_LeadAuthor"),
                Width = 115,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvRecent.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = LanguageService.Get("Col_Stage"),
                Width = 105,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvRecent.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NgayCapNhat",
                HeaderText = LanguageService.Get("Col_UpdatedDate"),
                Width = 115,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
        }

        private void DgvRecent_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvRecent.Columns[e.ColumnIndex].Name == "TrangThai")
            {
                e.PaintBackground(e.CellBounds, true);
                if (e.Graphics == null) return;

                string statusText = e.Value?.ToString() ?? "";
                var (textColor, bgColor, borderColor) = UITheme.GetStageColors(statusText);

                var badgeRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 6, e.CellBounds.Width - 12, e.CellBounds.Height - 12);

                using (var path = UITheme.CreateRoundedRectangle(badgeRect, 4))
                using (var bgBrush = new SolidBrush(bgColor))
                using (var borderPen = new Pen(borderColor, 1))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(bgBrush, path);
                    e.Graphics.DrawPath(borderPen, path);
                }

                using (var textBrush = new SolidBrush(textColor))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(statusText, UITheme.FontSmallBold, textBrush, badgeRect, sf);
                }

                e.Handled = true;
            }
        }

        private void ApplyLanguage()
        {
            bool isEn = LanguageService.CurrentLanguage == "en";

            lblEyebrow.Text = isEn ? "JOURNAL OF SCIENCE AND TECHNOLOGY (JST - HUIT)" : "TÒA SOẠN TẠP CHÍ KHOA HỌC & CÔNG NGHỆ (JST - HUIT)";
            lblHeaderTitle.Text = isEn ? "Editorial Desk & Workflow Operations" : "Bàn làm việc Ban Thư ký & Biên tập";

            cardScreening.Title = isEn ? "Screening Needed" : "Chờ sơ duyệt";
            cardScreening.Subtext = isEn ? "Plagiarism & formatting" : "Soi đạo văn & thể lệ";

            cardReview.Title = isEn ? "Under Review" : "Đang phản biện";
            cardReview.Subtext = isEn ? "Expert peer reviews" : "Đang giao chuyên gia";

            cardDecision.Title = isEn ? "Editorial Decision" : "Chờ quyết định";
            cardDecision.Subtext = isEn ? "Council decision pending" : "Hội đồng ra quyết định";

            cardPublished.Title = isEn ? "Published" : "Đã xuất bản";
            cardPublished.Subtext = isEn ? "Assigned Issue & DOI" : "Đã cấp số & DOI";

            lblUrgentTitle.Text = isEn ? "ACTION ITEMS NEEDING ATTENTION TODAY" : "DANH SÁCH TÁC VỤ CẦN XỬ LÝ (ƯU TIÊN)";
            lblWorkflowTitle.Text = isEn ? "PIPELINE:" : "QUY TRÌNH:";
            lblGridTitle.Text = isEn ? "In-Progress Manuscripts & Recent Updates" : "Bản thảo đang xử lý & Cập nhật gần đây";
            btnViewAll.Text = isEn ? "All Manuscripts →" : "Toàn bộ bài báo →";

            if (btnAct1 != null) btnAct1.Text = isEn ? "Screening" : "Sơ duyệt bài";
            if (btnAct2 != null) btnAct2.Text = isEn ? "Make Decision" : "Ra quyết định";
            if (btnAct3 != null) btnAct3.Text = isEn ? "Assign Review" : "Giao PB ngay";

            if (dgvRecent.Columns["MaDinhDanh"] != null) dgvRecent.Columns["MaDinhDanh"].HeaderText = LanguageService.Get("Col_ArticleCode");
            if (dgvRecent.Columns["TieuDe"] != null) dgvRecent.Columns["TieuDe"].HeaderText = LanguageService.Get("Col_ArticleTitle");
            if (dgvRecent.Columns["ChuyenNganh"] != null) dgvRecent.Columns["ChuyenNganh"].HeaderText = LanguageService.Get("Col_Field");
            if (dgvRecent.Columns["TacGia"] != null) dgvRecent.Columns["TacGia"].HeaderText = LanguageService.Get("Col_LeadAuthor");
            if (dgvRecent.Columns["TrangThai"] != null) dgvRecent.Columns["TrangThai"].HeaderText = LanguageService.Get("Col_Stage");
            if (dgvRecent.Columns["NgayCapNhat"] != null) dgvRecent.Columns["NgayCapNhat"].HeaderText = LanguageService.Get("Col_UpdatedDate");

            LoadData();
        }

        public void LoadData()
        {
            var metrics = ThongKeService.GetDashboardMetrics();
            var metricsError = JournalApiClient.LastError;

            cardScreening.Value = metricsError == null ? metrics.ChoSoDuyet.ToString() : "—";
            cardReview.Value = metricsError == null ? metrics.DangPhanBien.ToString() : "—";
            cardDecision.Value = metricsError == null ? metrics.ChoQuyetDinh.ToString() : "—";
            cardPublished.Value = metricsError == null ? metrics.DaXuatBan.ToString() : "—";

            int urgentCount = metrics.ChoSoDuyet + metrics.ChoQuyetDinh;
            string userName = AuthService.CurrentUser?.HoTen ?? "Ban Biên tập";
            bool isEn = LanguageService.CurrentLanguage == "en";

            lblUserGreeting.Text = isEn 
                ? $"Welcome back, {userName}! System detected {urgentCount} manuscripts requiring your action today."
                : $"Chào mừng {userName}! Hệ thống phát hiện có {urgentCount} bài báo cần Ban Biên tập xử lý hôm nay.";

            // Load Articles
            var list = BaiBaoService.GetAllBaiBao();
            var articlesError = JournalApiClient.LastError;
            if (metricsError != null || articlesError != null)
                lblUserGreeting.Text = isEn
                    ? $"Could not load editorial data: {articlesError ?? metricsError}"
                    : $"Không tải được dữ liệu tòa soạn: {articlesError ?? metricsError}";
            dgvRecent.Rows.Clear();

            _urgentScreeningId = 0;
            _urgentDecisionId = 0;

            int soDuyetCount = 0, phanBienCount = 0, quyetDinhCount = 0, cheBanCount = 0;

            foreach (var b in list)
            {
                if (b.TrangThai == "Chờ sơ duyệt" || b.TrangThai == "Chờ sửa hình thức")
                {
                    soDuyetCount++;
                    if (_urgentScreeningId == 0) _urgentScreeningId = b.MaBaiBao;
                }
                else if (b.TrangThai == "Đang phản biện")
                {
                    phanBienCount++;
                }
                else if (b.TrangThai == "Chờ quyết định" || b.TrangThai == "Chờ chỉnh sửa" || b.TrangThai == "Đã chấp nhận")
                {
                    quyetDinhCount++;
                    if (_urgentDecisionId == 0) _urgentDecisionId = b.MaBaiBao;
                }
                else if (b.TrangThai == "Đang chế bản" || b.TrangThai == "Sẵn sàng xuất bản")
                {
                    cheBanCount++;
                }

                string displayStatus = LanguageService.TranslateStatus(b.TrangThai);
                int rowIdx = dgvRecent.Rows.Add(
                    b.MaDinhDanh,
                    b.TieuDe,
                    b.TenChuyenNganh,
                    b.TenTacGia,
                    displayStatus,
                    b.NgayCapNhat.ToString("dd/MM/yyyy")
                );
                dgvRecent.Rows[rowIdx].Tag = b;
            }

            // Update Workflow Steps with live counts
            if (_workflowStepButtons.Count >= 5)
            {
                _workflowStepButtons[0].Text = isEn ? $"Screening ({soDuyetCount})" : $"1. Sơ duyệt ({soDuyetCount})";
                _workflowStepButtons[1].Text = isEn ? $"Review ({phanBienCount})" : $"2. Phản biện ({phanBienCount})";
                _workflowStepButtons[2].Text = isEn ? $"Decision ({quyetDinhCount})" : $"3. Quyết định ({quyetDinhCount})";
                _workflowStepButtons[3].Text = isEn ? $"Production ({cheBanCount})" : $"4. Chế bản ({cheBanCount})";
                _workflowStepButtons[4].Text = isEn ? $"Published ({metrics.DaXuatBan})" : $"5. Xuất bản ({metrics.DaXuatBan})";
            }

            // Update Urgent Descriptions
            if (lblUrgent1Desc != null)
            {
                lblUrgent1Desc.Text = isEn
                    ? $"New submissions: {soDuyetCount}\nFormat and editorial screening"
                    : $"Bài mới nộp: {soDuyetCount}\nKiểm tra thể thức và sơ duyệt";
            }

            if (lblUrgent2Desc != null)
            {
                lblUrgent2Desc.Text = isEn
                    ? $"Reviews ready: {quyetDinhCount}\nEditorial decision pending"
                    : $"Đã có điểm PB: {quyetDinhCount}\nChờ Hội đồng ra quyết định";
            }

            if (lblUrgent3Desc != null)
            {
                lblUrgent3Desc.Text = isEn
                    ? $"Under review: {phanBienCount}\nAssign & remind reviewers"
                    : $"Đang phản biện: {phanBienCount}\nPhân công & đôn đốc hạn";
            }
        }

        private void OpenArticleDetail(int maBaiBao)
        {
            using var frm = new FrmChiTietBaiBao(maBaiBao);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadData();
            }
        }
    }
}
