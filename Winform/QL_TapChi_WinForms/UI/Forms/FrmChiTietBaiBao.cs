using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmChiTietBaiBao : Form
    {
        private readonly int _maBaiBao;
        private BaiBao? _baiBao;

        private WorkflowStepperControl stepper = null!;
        private Label lblTitleVn = null!;
        private Label lblTitleEn = null!;
        private Label lblAuthor = null!;
        private Label lblCategory = null!;
        private Label lblDoi = null!;
        private Label lblStatusBadge = null!;

        private TabControl tabDetails = null!;
        private TextBox txtAbstract = null!;
        private TextBox txtKeywords = null!;
        private DataGridView dgvReviewers = null!;
        private DataGridView dgvHistory = null!;
        private DataGridView dgvFiles = null!;
        private Label lblActionHint = null!;

        // Action Buttons
        private ModernButton btnCheckPlagiarism = null!;
        private ModernButton btnPreRevision = null!;
        private ModernButton btnApproveInitial = null!;
        private ModernButton btnAssignNewReviewer = null!;
        private ModernButton btnReviewer3 = null!;
        private ModernButton btnRequestRevision = null!;
        private ModernButton btnAccept = null!;
        private ModernButton btnSendToGalley = null!;
        private ModernButton btnSendProof = null!;
        private ModernButton btnReject = null!;

        // Controls for Publishing tab
        private ComboBox cboIssues = null!;
        private NumericUpDown numPageStart = null!;
        private NumericUpDown numPageEnd = null!;
        private TextBox txtDoi = null!;
        private ModernButton btnPublish = null!;

        public FrmChiTietBaiBao(int maBaiBao)
        {
            _maBaiBao = maBaiBao;
            InitializeComponent();
            LoadArticleDetails();
        }

        private void InitializeComponent()
        {
            Text = "Hồ sơ thẩm định & Chi tiết bản thảo · JST";
            Size = new Size(1100, 850);
            MinimumSize = new Size(950, 700);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UITheme.AppBackground;
            Font = UITheme.FontBody;

            // 1. Top Header Info Panel
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 175,
                BackColor = Color.White,
                Padding = new Padding(28, 16, 28, 16)
            };
            pnlTop.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            lblStatusBadge = new Label
            {
                Text = "TRẠNG THÁI",
                Font = UITheme.FontSmallBold,
                Location = new Point(28, 12),
                Size = new Size(170, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblCode = new Label
            {
                Text = $"Mã bản thảo: JST-{_maBaiBao:D4}",
                Font = new Font("Consolas", 10.5f, FontStyle.Bold),
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(215, 15),
                AutoSize = true
            };

            lblTitleVn = new Label
            {
                Text = "Tiêu đề tiếng Việt",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                Location = new Point(28, 44),
                Size = new Size(pnlTop.Width - 56, 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };

            lblTitleEn = new Label
            {
                Text = "English Title",
                Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                ForeColor = UITheme.TextSecondary,
                Location = new Point(28, 102),
                Size = new Size(pnlTop.Width - 56, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };

            var flpMeta = new FlowLayoutPanel
            {
                Location = new Point(28, 134),
                Size = new Size(pnlTop.Width - 56, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
                WrapContents = false,
                AutoScroll = false
            };

            lblAuthor = new Label
            {
                Text = "Tác giả:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextPrimary,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 4, 6, 0)
            };

            var lblSep1 = new Label { Text = "•", ForeColor = UITheme.TextMuted, AutoSize = true, Margin = new Padding(0, 4, 6, 0) };

            lblCategory = new Label
            {
                Text = "Ngành:",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 4, 6, 0)
            };

            var lblSep2 = new Label { Text = "•", ForeColor = UITheme.TextMuted, AutoSize = true, Margin = new Padding(0, 4, 6, 0) };

            lblDoi = new Label
            {
                Text = "DOI:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.PrimaryDark,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 4, 0, 0)
            };

            flpMeta.Controls.AddRange(new Control[] { lblAuthor, lblSep1, lblCategory, lblSep2, lblDoi });

            pnlTop.Controls.AddRange(new Control[]
            {
                lblStatusBadge, lblCode,
                lblTitleVn, lblTitleEn,
                flpMeta
            });

            // 2. Stepper
            stepper = new WorkflowStepperControl
            {
                Dock = DockStyle.Top,
                Height = 82
            };

            // 3. Action Toolbar (Guided Stage-Based Grouping)
            var pnlActions = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.White,
                Padding = new Padding(24, 6, 24, 8)
            };
            pnlActions.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawLine(p, 0, pnlActions.Height - 1, pnlActions.Width, pnlActions.Height - 1);
            };

            // Top Guidance Strip
            var pnlHintBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(12, 0, 12, 0)
            };
            pnlHintBanner.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(186, 230, 253));
                e.Graphics.DrawRectangle(p, 0, 0, pnlHintBanner.Width - 1, pnlHintBanner.Height - 1);
            };

            lblActionHint = new Label
            {
                Dock = DockStyle.Fill,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.PrimaryDark,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Đang xác định tiến trình thẩm định bài báo...",
                UseMnemonic = false
            };
            pnlHintBanner.Controls.Add(lblActionHint);

            // Bottom Buttons Bar grouped by stages
            var flpActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            // Giai đoạn 1: Sơ duyệt
            btnCheckPlagiarism = new ModernButton
            {
                Text = "Đạo văn: chưa tích hợp",
                Size = new Size(170, 34),
                Enabled = false,
                NormalColor = Color.FromArgb(243, 232, 255),
                HoverColor = Color.FromArgb(233, 213, 255),
                BorderColor = Color.FromArgb(192, 132, 252),
                ForeColor = Color.FromArgb(126, 34, 206),
                Margin = new Padding(0, 0, 6, 0)
            };

            btnPreRevision = new ModernButton
            {
                Text = "Sửa thể thức",
                Size = new Size(115, 34),
                NormalColor = UITheme.Sky050,
                HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200,
                ForeColor = UITheme.TextPrimary,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnPreRevision.Click += (s, e) => ChangeStatusPrompt("Chờ sửa hình thức", "Bản thảo chưa đáp ứng đúng quy định thể lệ bài báo. Yêu cầu tác giả định dạng lại trước khi sơ duyệt.");

            btnApproveInitial = new ModernButton
            {
                Text = "Duyệt sơ bộ",
                Size = new Size(110, 34),
                NormalColor = UITheme.Success,
                HoverColor = Color.FromArgb(12, 130, 80),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 14, 0)
            };
            btnApproveInitial.Click += (s, e) => ChangeStatusPrompt("Đang phản biện", "Đạt yêu cầu sơ duyệt hình thức, chuyển sang giai đoạn phản biện chuyên môn.");

            // Giai đoạn 2: Phản biện
            btnAssignNewReviewer = new ModernButton
            {
                Text = "Giao phản biện",
                Size = new Size(125, 34),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryHover,
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnAssignNewReviewer.Click += (s, e) => AssignReviewerDialog();

            btnReviewer3 = new ModernButton
            {
                Text = "Mời phản biện 3",
                Size = new Size(130, 34),
                NormalColor = Color.FromArgb(254, 243, 199),
                HoverColor = Color.FromArgb(253, 230, 138),
                BorderColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.FromArgb(180, 83, 9),
                Margin = new Padding(0, 0, 14, 0)
            };
            btnReviewer3.Click += (s, e) =>
            {
                using var frm = new FrmPhanCongPhanBien(_maBaiBao, isReviewer3Mode: true);
                if (frm.ShowDialog() == DialogResult.OK) LoadArticleDetails();
            };

            // Giai đoạn 3: Quyết định
            btnRequestRevision = new ModernButton
            {
                Text = "Yêu cầu sửa",
                Size = new Size(110, 34),
                NormalColor = UITheme.Sky050,
                HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200,
                ForeColor = UITheme.TextPrimary,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnRequestRevision.Click += (s, e) => ChangeStatusPrompt("Chờ chỉnh sửa", "Ban biên tập tổng hợp nhận xét phản biện và yêu cầu tác giả hoàn thiện bản thảo.");

            btnAccept = new ModernButton
            {
                Text = "Chấp nhận đăng",
                Size = new Size(135, 34),
                NormalColor = UITheme.Success,
                HoverColor = Color.FromArgb(12, 130, 80),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 14, 0)
            };
            btnAccept.Click += (s, e) => ChangeStatusPrompt("Đã chấp nhận", "Hội đồng biên tập thông qua quyết định chấp nhận bài báo.");

            // Giai đoạn 4: Chế bản & Xuất bản
            btnSendToGalley = new ModernButton
            {
                Text = "Chế bản",
                Size = new Size(95, 34),
                NormalColor = UITheme.Sky050,
                HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200,
                ForeColor = UITheme.TextPrimary,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnSendToGalley.Click += (s, e) => ChangeStatusPrompt("Đang chế bản", "Chuyển bài báo sang bộ phận chế bản trình bày mẫu in ấn.");

            btnSendProof = new ModernButton
            {
                Text = "In thử: chưa tích hợp",
                Size = new Size(165, 34),
                Enabled = false,
                NormalColor = UITheme.Sky050,
                HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200,
                ForeColor = UITheme.TextPrimary,
                Margin = new Padding(0, 0, 14, 0)
            };

            btnReject = new ModernButton
            {
                Text = "Từ chối bài",
                Size = new Size(105, 34),
                NormalColor = UITheme.DangerBg,
                HoverColor = Color.FromArgb(254, 202, 202),
                BorderColor = Color.FromArgb(248, 113, 113),
                ForeColor = UITheme.Danger,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnReject.Click += (s, e) => ChangeStatusPrompt("Từ chối", "Bài báo không đáp ứng chuẩn chất lượng học thuật của Tạp chí.");

            flpActions.Controls.AddRange(new Control[]
            {
                btnCheckPlagiarism, btnPreRevision, btnApproveInitial,
                btnAssignNewReviewer, btnReviewer3,
                btnRequestRevision, btnAccept,
                btnSendToGalley, btnSendProof,
                btnReject
            });

            pnlActions.Controls.Add(flpActions);
            pnlActions.Controls.Add(pnlHintBanner);

            // 4. Tab Content
            tabDetails = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(16, 9),
                Font = UITheme.FontBodyBold
            };

            // Tab 1: Nội dung tóm tắt & Bản thảo
            var tabContent = new TabPage("1. Tóm tắt & Tệp tin");
            tabContent.Font = UITheme.FontBody;
            tabContent.BackColor = Color.White;
            tabContent.Padding = new Padding(20);
            tabContent.AutoScroll = true;

            var lblAbstractHead = new Label
            {
                Text = "Tóm tắt bản thảo (Abstract):",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(20, 12),
                AutoSize = true
            };

            txtAbstract = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Location = new Point(20, 34),
                Size = new Size(tabContent.Width - 40, 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10f)
            };

            var lblKwHead = new Label
            {
                Text = "Từ khóa (Keywords):",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(20, 185),
                AutoSize = true
            };

            txtKeywords = new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Location = new Point(20, 206),
                Size = new Size(tabContent.Width - 40, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10f)
            };

            var lblFilesHead = new Label
            {
                Text = "Danh sách tệp tin đính kèm:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(20, 250),
                AutoSize = true
            };

            var btnPreviewFile = new ModernButton
            {
                Text = "Tải tệp đã chọn",
                Size = new Size(175, 30),
                Location = new Point(414, 274),
                NormalColor = UITheme.Sky050,
                HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200,
                ForeColor = UITheme.PrimaryDark
            };
            btnPreviewFile.Click += async (s, e) =>
            {
                if (dgvFiles.CurrentRow?.Tag is TapTinBaiBao file)
                {
                    using var save = new SaveFileDialog { FileName = file.TenTapTin, Filter = "Tất cả tệp|*.*" };
                    if (save.ShowDialog(this) != DialogResult.OK) return;
                    btnPreviewFile.Enabled = false;
                    try
                    {
                        var ok = await JournalApiClient.DownloadFileAsync($"/api/desktop-editorial/files/{file.MaTapTin}/download", save.FileName);
                        MessageBox.Show(this, ok ? "Đã tải tệp về máy." : JournalApiClient.LastError ?? "Tải tệp thất bại.",
                            "Tải tệp", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                    }
                    finally { btnPreviewFile.Enabled = true; }
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một tệp bản thảo trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnUploadAnonymous = new ModernButton { Text = "Tải bản ẩn danh", Size = new Size(185, 30),
                Location = new Point(20, 274), NormalColor = UITheme.Sky050, HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200, ForeColor = UITheme.PrimaryDark };
            btnUploadAnonymous.Click += async (s, e) =>
            {
                using var open = new OpenFileDialog { Filter = "Bản thảo PDF/Word|*.pdf;*.doc;*.docx" };
                if (open.ShowDialog(this) != DialogResult.OK) return;
                btnUploadAnonymous.Enabled = false;
                try
                {
                    var ok = await JournalApiClient.UploadFileAsync($"/api/baibao/{_maBaiBao}/upload-anonymous-manuscript", open.FileName);
                    MessageBox.Show(this, ok ? "Đã tải bản thảo ẩn danh lên máy chủ." : JournalApiClient.LastError ?? "Tải lên thất bại.",
                        "Bản thảo ẩn danh", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                    if (ok) LoadArticleDetails();
                }
                finally { btnUploadAnonymous.Enabled = true; }
            };

            var btnUploadPublished = new ModernButton { Text = "Tải PDF thành phẩm", Size = new Size(185, 30),
                Location = new Point(217, 274), NormalColor = UITheme.Sky050, HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200, ForeColor = UITheme.PrimaryDark };
            btnUploadPublished.Click += async (s, e) =>
            {
                using var open = new OpenFileDialog { Filter = "PDF thành phẩm|*.pdf" };
                if (open.ShowDialog(this) != DialogResult.OK) return;
                btnUploadPublished.Enabled = false;
                try
                {
                    var ok = await JournalApiClient.UploadFileAsync($"/api/baibao/{_maBaiBao}/upload-published-pdf", open.FileName);
                    MessageBox.Show(this, ok ? "Đã tải PDF thành phẩm lên máy chủ." : JournalApiClient.LastError ?? "Tải lên thất bại.",
                        "PDF thành phẩm", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                    if (ok) LoadArticleDetails();
                }
                finally { btnUploadPublished.Enabled = true; }
            };

            dgvFiles = new DataGridView
            {
                Location = new Point(20, 315),
                Size = new Size(tabContent.Width - 40, 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            UITheme.ApplyModernGridStyle(dgvFiles);
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ten", HeaderText = "Tên tệp tin", Width = 380, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Loai", HeaderText = "Loại tập tin", Width = 180, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vong", HeaderText = "Vòng nộp", Width = 135, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ngay", HeaderText = "Ngày tải lên", Width = 160, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            tabContent.Controls.AddRange(new Control[]
            {
                lblAbstractHead, txtAbstract,
                lblKwHead, txtKeywords,
                lblFilesHead, btnUploadAnonymous, btnUploadPublished, btnPreviewFile, dgvFiles
            });

            // Tab 2: Chuyên gia Phản biện & Đánh giá
            var tabReview = new TabPage("2. Hội đồng & Phiếu phản biện");
            tabReview.Font = UITheme.FontBody;
            tabReview.BackColor = Color.White;
            tabReview.Padding = new Padding(20);

            var pnlReviewTop = new Panel { Dock = DockStyle.Top, Height = 44 };
            var lblReviewList = new Label
            {
                Text = "Danh sách chuyên gia phản biện đã được giao bài:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 460,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var btnViewScore = new ModernButton
            {
                Text = "Xem Phiếu đánh giá chi tiết →",
                Size = new Size(240, 34),
                Dock = DockStyle.Right,
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryHover
            };
            btnViewScore.Click += BtnViewScore_Click;

            pnlReviewTop.Controls.Add(btnViewScore);
            pnlReviewTop.Controls.Add(lblReviewList);

            dgvReviewers = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyModernGridStyle(dgvReviewers);
            dgvReviewers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ten", HeaderText = "Chuyên gia phản biện", Width = 220, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvReviewers.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị công tác", Width = 260, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvReviewers.Columns.Add(new DataGridViewTextBoxColumn { Name = "HanHoanThanh", HeaderText = "Hạn hoàn thành", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvReviewers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvReviewers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diem", HeaderText = "Điểm tổng", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UITheme.FontBodyBold, ForeColor = UITheme.PrimaryDark } });
            dgvReviewers.Columns.Add(new DataGridViewTextBoxColumn { Name = "KienNghi", HeaderText = "Kiến nghị", Width = 240, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });

            tabReview.Controls.Add(dgvReviewers);
            tabReview.Controls.Add(pnlReviewTop);

            // Tab 3: Lịch sử Audit Trail
            var tabHistory = new TabPage("3. Lịch sử tiến trình");
            tabHistory.Font = UITheme.FontBody;
            tabHistory.BackColor = Color.White;
            tabHistory.Padding = new Padding(20);

            dgvHistory = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyModernGridStyle(dgvHistory);
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThoiGian", HeaderText = "Thời gian", Width = 160 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "TuTrangThai", HeaderText = "Từ trạng thái", Width = 160 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "SangTrangThai", HeaderText = "Sang trạng thái", Width = 170 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "NguoiThucHien", HeaderText = "Người thực hiện", Width = 170, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu", HeaderText = "Ghi chú / Nội dung", Width = 340, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });

            tabHistory.Controls.Add(dgvHistory);

            // Tab 4: Xuất bản & Số Tạp chí
            var tabPublish = new TabPage("4. Xuất bản & Số báo");
            tabPublish.Font = UITheme.FontBody;
            tabPublish.BackColor = Color.White;
            tabPublish.Padding = new Padding(28);

            var lblPubTitle = new Label
            {
                Text = "Phân bổ vào số tạp chí & Công bố toàn văn",
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(28, 20),
                AutoSize = true
            };

            var lblIssueSelect = new Label
            {
                Text = "Chọn Số tạp chí phát hành:",
                Font = UITheme.FontBodyBold,
                Location = new Point(28, 65),
                AutoSize = true
            };

            cboIssues = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(28, 90),
                Size = new Size(520, 32)
            };

            var lblPageStart = new Label
            {
                Text = "Trang bắt đầu:",
                Font = UITheme.FontBodyBold,
                Location = new Point(28, 140),
                AutoSize = true
            };
            numPageStart = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = 1,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(28, 165),
                Size = new Size(130, 30)
            };

            var lblPageEnd = new Label
            {
                Text = "Trang kết thúc:",
                Font = UITheme.FontBodyBold,
                Location = new Point(190, 140),
                AutoSize = true
            };
            numPageEnd = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = 15,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(190, 165),
                Size = new Size(130, 30)
            };

            var lblDoiInput = new Label
            {
                Text = "Mã định danh học thuật (DOI):",
                Font = UITheme.FontBodyBold,
                Location = new Point(28, 215),
                AutoSize = true
            };

            txtDoi = new TextBox
            {
                Text = $"10.59876/jst.{DateTime.Now.Year}.{_maBaiBao:D2}",
                Font = new Font("Segoe UI", 10f),
                Location = new Point(28, 240),
                Size = new Size(520, 30)
            };

            btnPublish = new ModernButton
            {
                Text = "XÁC NHẬN XUẤT BẢN CHÍNH THỨC",
                Size = new Size(290, 44),
                Location = new Point(28, 300),
                NormalColor = UITheme.Success,
                HoverColor = Color.FromArgb(12, 130, 80)
            };
            btnPublish.Click += BtnPublish_Click;

            tabPublish.Controls.AddRange(new Control[]
            {
                lblPubTitle,
                lblIssueSelect, cboIssues,
                lblPageStart, numPageStart,
                lblPageEnd, numPageEnd,
                lblDoiInput, txtDoi,
                btnPublish
            });

            tabDetails.TabPages.AddRange(new TabPage[] { tabContent, tabReview, tabHistory, tabPublish });

            Controls.Add(tabDetails);
            Controls.Add(pnlActions);
            Controls.Add(stepper);
            Controls.Add(pnlTop);
        }

        private void LoadArticleDetails()
        {
            _baiBao = BaiBaoService.GetBaiBaoById(_maBaiBao);
            if (_baiBao == null) return;

            lblTitleVn.Text = _baiBao.TieuDe;
            lblTitleEn.Text = _baiBao.TieuDeTiengAnh ?? "Chưa có tiêu đề tiếng Anh";
            lblAuthor.Text = $"Tác giả: {_baiBao.TenTacGia} ({_baiBao.DonViTacGia})";
            lblCategory.Text = $"Ngành: {_baiBao.TenChuyenNganh}";
            lblDoi.Text = $"DOI: {(_baiBao.MaDOI ?? "Chưa cấp")}";

            stepper.CurrentStatus = _baiBao.TrangThai;

            // Status Badge
            var (textColor, bgColor, borderColor) = UITheme.GetStageColors(_baiBao.TrangThai);
            lblStatusBadge.Text = _baiBao.TrangThai.ToUpper();
            lblStatusBadge.ForeColor = textColor;
            lblStatusBadge.BackColor = bgColor;
            lblStatusBadge.Paint += (s, e) =>
            {
                using var p = new Pen(borderColor, 1);
                e.Graphics.DrawRectangle(p, 0, 0, lblStatusBadge.Width - 1, lblStatusBadge.Height - 1);
            };

            txtAbstract.Text = _baiBao.TomTat ?? "(Chưa có tóm tắt)";
            txtKeywords.Text = _baiBao.TuKhoa ?? "(Chưa có từ khóa)";

            // Load Files
            var files = BaiBaoService.GetTapTinBaiBao(_maBaiBao);
            dgvFiles.Rows.Clear();
            foreach (var f in files)
            {
                var row = dgvFiles.Rows.Add(f.TenTapTin, f.LoaiTapTin, $"Vòng {f.SoVong}", f.NgayTaiLen.ToString("dd/MM/yyyy HH:mm"));
                dgvFiles.Rows[row].Tag = f;
            }

            // Load Reviewers
            var reviewers = PhanBienService.GetPhanCongByBaiBao(_maBaiBao);
            dgvReviewers.Rows.Clear();
            foreach (var r in reviewers)
            {
                string diem = r.DiemTongKet.HasValue ? r.DiemTongKet.Value.ToString("F1") : "Chưa chấm";
                int idx = dgvReviewers.Rows.Add(
                    r.TenPhanBien,
                    r.DonViPhanBien,
                    r.HanHoanThanh?.ToString("dd/MM/yyyy") ?? "Không có",
                    r.TrangThai,
                    diem,
                    r.KienNghi ?? "Chưa gửi kiến nghị"
                );
                dgvReviewers.Rows[idx].Tag = r;
            }

            // Load Audit Trail
            var history = BaiBaoService.GetLichSuTrangThai(_maBaiBao);
            dgvHistory.Rows.Clear();
            foreach (var h in history)
            {
                dgvHistory.Rows.Add(
                    h.NgayChuyen.ToString("dd/MM/yyyy HH:mm"),
                    h.TrangThaiCu ?? "Mới nộp",
                    h.TrangThaiMoi,
                    h.TenNguoiThucHien,
                    h.GhiChu
                );
            }

            // Load Issues for Publish tab
            cboIssues.Items.Clear();
            var issues = SoTapChiService.GetAllSoTapChi();
            foreach (var isItem in issues)
            {
                cboIssues.Items.Add(new IssueItem(isItem));
            }
            if (cboIssues.Items.Count > 0) cboIssues.SelectedIndex = 0;

            if (_baiBao.TrangBatDau.HasValue) numPageStart.Value = _baiBao.TrangBatDau.Value;
            if (_baiBao.TrangKetThuc.HasValue) numPageEnd.Value = _baiBao.TrangKetThuc.Value;
            if (!string.IsNullOrEmpty(_baiBao.MaDOI)) txtDoi.Text = _baiBao.MaDOI;

            UpdateActionState();
        }

        private void UpdateActionState()
        {
            if (_baiBao == null) return;

            string status = _baiBao.TrangThai;

            // Hide all buttons by default (Stage-based action gating)
            btnCheckPlagiarism.Visible = false;
            btnPreRevision.Visible = false;
            btnApproveInitial.Visible = false;
            btnAssignNewReviewer.Visible = false;
            btnReviewer3.Visible = false;
            btnRequestRevision.Visible = false;
            btnAccept.Visible = false;
            btnSendToGalley.Visible = false;
            btnSendProof.Visible = false;
            btnReject.Visible = false;

            // Reset standard secondary styles
            btnCheckPlagiarism.NormalColor = UITheme.Sky050;
            btnCheckPlagiarism.ForeColor = UITheme.PrimaryDark;
            btnCheckPlagiarism.BorderColor = UITheme.Sky200;

            btnPreRevision.NormalColor = UITheme.Sky050;
            btnPreRevision.ForeColor = UITheme.TextPrimary;
            btnPreRevision.BorderColor = UITheme.Sky200;

            btnApproveInitial.NormalColor = UITheme.Sky050;
            btnApproveInitial.ForeColor = UITheme.TextPrimary;
            btnApproveInitial.BorderColor = UITheme.Sky200;

            btnAssignNewReviewer.NormalColor = UITheme.Sky050;
            btnAssignNewReviewer.ForeColor = UITheme.TextPrimary;
            btnAssignNewReviewer.BorderColor = UITheme.Sky200;

            btnReviewer3.NormalColor = UITheme.Sky050;
            btnReviewer3.ForeColor = UITheme.TextPrimary;
            btnReviewer3.BorderColor = UITheme.Sky200;

            btnRequestRevision.NormalColor = UITheme.Sky050;
            btnRequestRevision.ForeColor = UITheme.TextPrimary;
            btnRequestRevision.BorderColor = UITheme.Sky200;

            btnAccept.NormalColor = UITheme.Sky050;
            btnAccept.ForeColor = UITheme.TextPrimary;
            btnAccept.BorderColor = UITheme.Sky200;

            btnSendToGalley.NormalColor = UITheme.Sky050;
            btnSendToGalley.ForeColor = UITheme.TextPrimary;
            btnSendToGalley.BorderColor = UITheme.Sky200;

            btnSendProof.NormalColor = UITheme.Sky050;
            btnSendProof.ForeColor = UITheme.TextPrimary;
            btnSendProof.BorderColor = UITheme.Sky200;

            btnReject.NormalColor = UITheme.DangerBg;
            btnReject.ForeColor = UITheme.Danger;
            btnReject.BorderColor = Color.FromArgb(248, 113, 113);

            switch (status)
            {
                case "Chờ sơ duyệt":
                    lblActionHint.Text = "[Giai đoạn 1 - Sơ duyệt] Vui lòng Soi đạo văn và kiểm tra thể thức. Bấm Duyệt sơ bộ để chuyển sang phản biện.";
                    btnCheckPlagiarism.Visible = true;
                    btnCheckPlagiarism.NormalColor = Color.FromArgb(243, 232, 255);
                    btnCheckPlagiarism.ForeColor = Color.FromArgb(126, 34, 206);
                    btnCheckPlagiarism.BorderColor = Color.FromArgb(192, 132, 252);

                    btnPreRevision.Visible = true;

                    btnApproveInitial.Visible = true;
                    btnApproveInitial.NormalColor = UITheme.Success;
                    btnApproveInitial.ForeColor = Color.White;
                    btnApproveInitial.BorderColor = UITheme.Success;

                    btnReject.Visible = true;
                    break;

                case "Chờ sửa hình thức":
                    lblActionHint.Text = "[Chờ sửa hình thức] Bản thảo đang chờ tác giả nộp lại tệp đúng quy cách. Khi đã có tệp mới, bấm Duyệt sơ bộ.";
                    btnCheckPlagiarism.Visible = true;

                    btnApproveInitial.Visible = true;
                    btnApproveInitial.NormalColor = UITheme.Success;
                    btnApproveInitial.ForeColor = Color.White;
                    btnApproveInitial.BorderColor = UITheme.Success;

                    btnReject.Visible = true;
                    break;

                case "Đang phản biện":
                    lblActionHint.Text = "[Giai đoạn 2 - Phản biện] Bấm Giao phản biện để chỉ định chuyên gia. Nếu cần phân xử, bấm Mời phản biện 3.";
                    btnAssignNewReviewer.Visible = true;
                    btnAssignNewReviewer.NormalColor = UITheme.Primary;
                    btnAssignNewReviewer.ForeColor = Color.White;
                    btnAssignNewReviewer.BorderColor = UITheme.Primary;

                    btnReviewer3.Visible = true;
                    btnReviewer3.NormalColor = Color.FromArgb(254, 243, 199);
                    btnReviewer3.ForeColor = Color.FromArgb(180, 83, 9);
                    btnReviewer3.BorderColor = Color.FromArgb(245, 158, 11);

                    btnRequestRevision.Visible = true;
                    btnAccept.Visible = true;
                    btnReject.Visible = true;
                    break;

                case "Chờ chỉnh sửa":
                    lblActionHint.Text = "[Chờ chỉnh sửa] Ban biên tập đã gửi nhận xét phản biện cho tác giả. Chờ tác giả tải lên bản thảo hoàn thiện ở Tab 1.";
                    btnAssignNewReviewer.Visible = true;
                    btnAssignNewReviewer.NormalColor = UITheme.Primary;
                    btnAssignNewReviewer.ForeColor = Color.White;

                    btnRequestRevision.Visible = true;
                    btnAccept.Visible = true;
                    btnReject.Visible = true;
                    break;

                case "Chờ quyết định":
                    lblActionHint.Text = "[Giai đoạn 3 - Quyết định] Xem xét kết quả thẩm định ở Tab 2. Chọn Chấp nhận đăng, Yêu cầu sửa hoặc Từ chối.";
                    btnAccept.Visible = true;
                    btnAccept.NormalColor = UITheme.Success;
                    btnAccept.ForeColor = Color.White;
                    btnAccept.BorderColor = UITheme.Success;

                    btnRequestRevision.Visible = true;
                    btnRequestRevision.NormalColor = UITheme.Primary;
                    btnRequestRevision.ForeColor = Color.White;

                    btnReviewer3.Visible = true;
                    btnReject.Visible = true;
                    break;

                case "Đã chấp nhận":
                    lblActionHint.Text = "[Đã chấp nhận] Bản thảo đã được chấp nhận. Bấm Chế bản để chuyển bộ phận kỹ thuật trình bày ấn phẩm.";
                    btnSendToGalley.Visible = true;
                    btnSendToGalley.NormalColor = UITheme.Primary;
                    btnSendToGalley.ForeColor = Color.White;
                    btnSendToGalley.BorderColor = UITheme.Primary;

                    btnReject.Visible = true;
                    break;

                case "Đang chế bản":
                    lblActionHint.Text = "[Giai đoạn 4 - Chế bản] Bấm Gửi in thử cho tác giả, hoặc chuyển sang Tab 4 để phân bổ vào Số tạp chí & Cấp DOI.";
                    btnSendProof.Visible = true;
                    btnSendProof.NormalColor = UITheme.Primary;
                    btnSendProof.ForeColor = Color.White;
                    btnSendProof.BorderColor = UITheme.Primary;
                    break;

                case "Sẵn sàng xuất bản":
                    lblActionHint.Text = "[Sẵn sàng xuất bản] Bản thảo đã hoàn tất chế bản. Chuyển sang Tab 4 để xếp bài vào Số tạp chí.";
                    break;

                case "Đã xuất bản":
                    lblActionHint.Text = "[Đã xuất bản] Bài báo đã được phát hành chính thức trong số tạp chí kèm mã định danh DOI toàn văn.";
                    break;

                case "Từ chối":
                    lblActionHint.Text = "[Từ chối] Bài báo đã dừng quy trình do không đạt tiêu chuẩn học thuật của tạp chí.";
                    break;

                default:
                    lblActionHint.Text = $"Bản thảo đang ở trạng thái '{status}'. Xem các thẻ chi tiết bên dưới để thực hiện nghiệp vụ.";
                    btnReject.Visible = true;
                    break;
            }
        }

        private void ChangeStatusPrompt(string newStatus, string defaultNote)
        {
            var result = MessageBox.Show(
                $"Xác nhận chuyển bài báo sang trạng thái '{newStatus}'?\n\nGhi chú: {defaultNote}",
                "Xác nhận thay đổi tiến độ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                bool ok = BaiBaoService.ChuyenTrangThai(_maBaiBao, newStatus, defaultNote);
                if (ok)
                {
                    MessageBox.Show("Cập nhật trạng thái thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadArticleDetails();
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show(JournalApiClient.LastError ?? "Có lỗi xảy ra khi cập nhật trạng thái.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AssignReviewerDialog()
        {
            using var frm = new FrmPhanCongPhanBien(_maBaiBao);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadArticleDetails();
                DialogResult = DialogResult.OK;
            }
        }

        private void BtnViewScore_Click(object? sender, EventArgs e)
        {
            if (dgvReviewers.CurrentRow?.Tag is PhanCongPhanBien pc)
            {
                using var frm = new FrmPhieuDanhGiaDialog(pc.MaPhanCong);
                frm.ShowDialog();
                LoadArticleDetails();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một chuyên gia phản biện trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnPublish_Click(object? sender, EventArgs e)
        {
            if (_baiBao != null && _baiBao.TrangThai != "Đang chế bản" && _baiBao.TrangThai != "Đã chấp nhận" && _baiBao.TrangThai != "Chấp nhận đăng" && _baiBao.TrangThai != "Sẵn sàng xuất bản")
            {
                MessageBox.Show($"Bản thảo đang ở trạng thái '{_baiBao.TrangThai}', chưa hoàn thành quy trình phản biện và chế bản.\nChỉ những bài báo đã được chấp nhận và chế bản mới đủ điều kiện xuất bản chính thức.", "Cảnh báo quy trình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboIssues.SelectedItem is IssueItem issueItem)
            {
                int start = (int)numPageStart.Value;
                int end = (int)numPageEnd.Value;
                string doi = txtDoi.Text.Trim();

                if (end < start)
                {
                    MessageBox.Show("Trang kết thúc phải lớn hơn hoặc bằng trang bắt đầu.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show(
                    $"Xác nhận xuất bản bài báo vào '{issueItem.Issue.TenSo}'?\nTrang: {start} - {end}\nDOI: {doi}",
                    "Xác nhận xếp bài vào số",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    bool ok = BaiBaoService.GanSoTapChi(_maBaiBao, issueItem.Issue.MaSoTapChi, start, end, doi);
                    if (ok)
                    {
                        MessageBox.Show("Đã xếp bài vào số tạp chí. Bài sẽ công khai khi số được phát hành qua Backend API.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadArticleDetails();
                        DialogResult = DialogResult.OK;
                    }
                    else MessageBox.Show(JournalApiClient.LastError ?? "Không thể xếp bài vào số tạp chí.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn Số tạp chí cần xếp bài.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private class IssueItem
        {
            public SoTapChi Issue { get; }
            public IssueItem(SoTapChi issue) => Issue = issue;
            public override string ToString() => Issue.HienThiTenSo;
        }
    }
}
