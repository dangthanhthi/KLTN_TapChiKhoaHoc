using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Forms;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Views
{
    public class UcQuanLySoTapChi : UserControl
    {
        private Label lblTitle = null!;
        private DataGridView dgvIssues = null!;
        private DataGridView dgvArticlesInIssue = null!;
        private Label lblIssueArticlesTitle = null!;
        private Label lblEmptyNotice = null!;
        
        // Toolbar 1 (Issues level)
        private ModernButton btnAdd = null!;
        private ModernButton btnEdit = null!;
        private ModernButton btnPublish = null!;
        private ModernButton btnDelete = null!;
        private ModernButton btnRefresh = null!;

        // Toolbar 2 (Articles inside issue level)
        private ModernButton btnArticleAdd = null!;
        private ModernButton btnArticleEdit = null!;
        private ModernButton btnArticleRemove = null!;
        private ModernButton btnArticleDetail = null!;

        // Context Menus
        private ContextMenuStrip cmsIssues = null!;
        private ContextMenuStrip cmsArticles = null!;

        public UcQuanLySoTapChi()
        {
            InitializeComponent();
            ApplyLanguage();
            LanguageService.OnLanguageChanged += ApplyLanguage;
            LoadIssues();
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
            Padding = new Padding(24);

            // ================= 1. TOP TOOLBAR =================
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 104,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 10)
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
                Text = LanguageService.Get("Issues_Title"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlTitleRow.Controls.Add(lblTitle);

            var flpActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 2, 0, 0)
            };

            btnAdd = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnAdd"),
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplyPrimaryButton(btnAdd);
            btnAdd.Click += (s, e) =>
            {
                using var frm = new FrmSoTapChiDialog();
                if (frm.ShowDialog() == DialogResult.OK) LoadIssues();
            };

            btnEdit = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnEdit"),
                Size = new Size(105, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplySecondaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;

            btnPublish = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnPublish"),
                Size = new Size(165, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplySecondaryButton(btnPublish);
            btnPublish.Click += BtnPublishIssue_Click;

            btnDelete = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnDelete"),
                Size = new Size(105, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplyDangerButton(btnDelete);
            btnDelete.Click += BtnDeleteIssue_Click;

            btnRefresh = new ModernButton
            {
                Text = LanguageService.Get("Refresh"),
                Size = new Size(85, 36),
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplySecondaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadIssues();

            flpActions.Controls.Add(btnAdd);
            flpActions.Controls.Add(btnEdit);
            flpActions.Controls.Add(btnPublish);
            flpActions.Controls.Add(btnDelete);
            flpActions.Controls.Add(btnRefresh);

            pnlToolbar.Controls.Add(flpActions);
            pnlToolbar.Controls.Add(pnlTitleRow);

            // ================= 2. SPLIT CONTAINER =================
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            split.SizeChanged += (s, e) =>
            {
                if (split.Height > 350)
                {
                    try { split.SplitterDistance = (int)(split.Height * 0.45); } catch { }
                }
            };

            // Top Panel: Issues Grid
            var pnlTopGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlTopGrid.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlTopGrid.Width - 1, pnlTopGrid.Height - 1);
            };

            dgvIssues = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyCompactGridStyle(dgvIssues, 46);
            SetupIssueColumns();
            dgvIssues.SelectionChanged += DgvIssues_SelectionChanged;
            dgvIssues.CellDoubleClick += (s, e) => BtnEdit_Click(s, e);

            // Right click context menu for Issues
            cmsIssues = new ContextMenuStrip();
            cmsIssues.Items.Add("Thêm bài báo vào số này...", null, (s, e) => BtnArticleAdd_Click(s, e));
            cmsIssues.Items.Add(new ToolStripSeparator());
            cmsIssues.Items.Add("Chỉnh sửa thông tin số", null, (s, e) => BtnEdit_Click(s, e));
            cmsIssues.Items.Add("Phát hành trực tuyến số báo", null, (s, e) => BtnPublishIssue_Click(s, e));
            cmsIssues.Items.Add(new ToolStripSeparator());
            cmsIssues.Items.Add("Xóa số tạp chí", null, (s, e) => BtnDeleteIssue_Click(s, e));
            dgvIssues.ContextMenuStrip = cmsIssues;

            pnlTopGrid.Controls.Add(dgvIssues);
            split.Panel1.Controls.Add(pnlTopGrid);

            // Bottom Panel: Articles in selected issue + Actions
            var pnlBottomGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlBottomGrid.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlBottomGrid.Width - 1, pnlBottomGrid.Height - 1);
            };

            // Sub-Toolbar inside Bottom Panel
            var pnlSubHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 4, 12, 4)
            };
            pnlSubHeader.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawLine(p, 0, pnlSubHeader.Height - 1, pnlSubHeader.Width, pnlSubHeader.Height - 1);
            };

            lblIssueArticlesTitle = new Label
            {
                Text = LanguageService.Get("Issues_ArticlesInSelected"),
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.PrimaryDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                AutoEllipsis = true
            };

            var flpSubActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false
            };

            btnArticleAdd = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnAddArticle"),
                Size = new Size(125, 34),
                Margin = new Padding(0, 1, 6, 0)
            };
            UITheme.ApplyPrimaryButton(btnArticleAdd);
            btnArticleAdd.Click += BtnArticleAdd_Click;

            btnArticleEdit = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnEditPage"),
                Size = new Size(115, 34),
                Margin = new Padding(0, 1, 6, 0)
            };
            UITheme.ApplySecondaryButton(btnArticleEdit);
            btnArticleEdit.Click += BtnArticleEdit_Click;

            btnArticleRemove = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnRemoveArticle"),
                Size = new Size(80, 34),
                Margin = new Padding(0, 1, 6, 0)
            };
            UITheme.ApplyDangerButton(btnArticleRemove);
            btnArticleRemove.Click += BtnArticleRemove_Click;

            btnArticleDetail = new ModernButton
            {
                Text = LanguageService.Get("Issues_BtnViewDetail"),
                Size = new Size(90, 34),
                Margin = new Padding(0, 1, 0, 0)
            };
            UITheme.ApplySecondaryButton(btnArticleDetail);
            btnArticleDetail.Click += BtnArticleDetail_Click;

            flpSubActions.Controls.AddRange(new Control[]
            {
                btnArticleAdd,
                btnArticleEdit,
                btnArticleRemove,
                btnArticleDetail
            });

            pnlSubHeader.Controls.Add(lblIssueArticlesTitle);
            pnlSubHeader.Controls.Add(flpSubActions);

            // Empty state notice
            lblEmptyNotice = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(254, 249, 195),
                ForeColor = Color.FromArgb(133, 77, 14),
                Font = UITheme.FontSmallBold,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0),
                Text = LanguageService.Get("Issues_EmptyNotice"),
                Visible = false
            };

            dgvArticlesInIssue = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyCompactGridStyle(dgvArticlesInIssue, 46);
            SetupArticleInIssueColumns();
            dgvArticlesInIssue.CellDoubleClick += (s, e) => BtnArticleDetail_Click(s, e);

            // Context Menu for Articles in Issue
            cmsArticles = new ContextMenuStrip();
            cmsArticles.Items.Add("Xem chi tiết hồ sơ bài báo", null, (s, e) => BtnArticleDetail_Click(s, e));
            cmsArticles.Items.Add("Sửa số trang & mã DOI", null, (s, e) => BtnArticleEdit_Click(s, e));
            cmsArticles.Items.Add(new ToolStripSeparator());
            cmsArticles.Items.Add("Gỡ bài báo ra khỏi số tạp chí này", null, (s, e) => BtnArticleRemove_Click(s, e));
            dgvArticlesInIssue.ContextMenuStrip = cmsArticles;

            pnlBottomGrid.Controls.Add(dgvArticlesInIssue);
            pnlBottomGrid.Controls.Add(lblEmptyNotice);
            pnlBottomGrid.Controls.Add(pnlSubHeader);
            split.Panel2.Controls.Add(pnlBottomGrid);

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

            Controls.Add(split);
            Controls.Add(pnlSpacer);
            Controls.Add(pnlToolbar);
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = LanguageService.Get("Issues_Title");
            btnAdd.Text = LanguageService.Get("Issues_BtnAdd");
            btnEdit.Text = LanguageService.Get("Issues_BtnEdit");
            btnPublish.Text = LanguageService.Get("Issues_BtnPublish");
            btnDelete.Text = LanguageService.Get("Issues_BtnDelete");
            btnRefresh.Text = LanguageService.Get("Refresh");

            btnArticleAdd.Text = LanguageService.Get("Issues_BtnAddArticle");
            btnArticleEdit.Text = LanguageService.Get("Issues_BtnEditPage");
            btnArticleRemove.Text = LanguageService.Get("Issues_BtnRemoveArticle");
            btnArticleDetail.Text = LanguageService.Get("Issues_BtnViewDetail");
            lblEmptyNotice.Text = LanguageService.Get("Issues_EmptyNotice");

            if (dgvIssues.Columns["Ma"] != null) dgvIssues.Columns["Ma"].HeaderText = LanguageService.Get("Col_Id");
            if (dgvIssues.Columns["TenSo"] != null) dgvIssues.Columns["TenSo"].HeaderText = LanguageService.Get("Col_IssueName");
            if (dgvIssues.Columns["Tap"] != null) dgvIssues.Columns["Tap"].HeaderText = LanguageService.Get("Col_Volume");
            if (dgvIssues.Columns["So"] != null) dgvIssues.Columns["So"].HeaderText = LanguageService.Get("Col_Number");
            if (dgvIssues.Columns["Nam"] != null) dgvIssues.Columns["Nam"].HeaderText = LanguageService.Get("Col_Year");
            if (dgvIssues.Columns["SoLuongBai"] != null) dgvIssues.Columns["SoLuongBai"].HeaderText = LanguageService.Get("Issues_ColArticlesCount");
            if (dgvIssues.Columns["NgayPhatHanh"] != null) dgvIssues.Columns["NgayPhatHanh"].HeaderText = LanguageService.Get("Col_PublishDate");
            if (dgvIssues.Columns["TrangThai"] != null) dgvIssues.Columns["TrangThai"].HeaderText = LanguageService.Get("Col_Status");

            if (dgvArticlesInIssue.Columns["Ma"] != null) dgvArticlesInIssue.Columns["Ma"].HeaderText = LanguageService.Get("Col_ArticleCode");
            if (dgvArticlesInIssue.Columns["TieuDe"] != null) dgvArticlesInIssue.Columns["TieuDe"].HeaderText = LanguageService.Get("Col_ArticleTitle");
            if (dgvArticlesInIssue.Columns["TacGia"] != null) dgvArticlesInIssue.Columns["TacGia"].HeaderText = LanguageService.Get("Col_LeadAuthor");
            if (dgvArticlesInIssue.Columns["ChuyenNganh"] != null) dgvArticlesInIssue.Columns["ChuyenNganh"].HeaderText = LanguageService.Get("Col_Field");
            if (dgvArticlesInIssue.Columns["Trang"] != null) dgvArticlesInIssue.Columns["Trang"].HeaderText = LanguageService.Get("Col_Pages");
            if (dgvArticlesInIssue.Columns["DOI"] != null) dgvArticlesInIssue.Columns["DOI"].HeaderText = LanguageService.Get("Col_DOI");

            lblIssueArticlesTitle.Text = LanguageService.Get("Issues_ArticlesInSelected");

            LoadIssues();
        }

        private void SetupIssueColumns()
        {
            dgvIssues.Columns.Clear();
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = LanguageService.Get("Col_Id"), Width = 52, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenSo", HeaderText = LanguageService.Get("Col_IssueName"), MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tap", HeaderText = LanguageService.Get("Col_Volume"), Width = 55, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "So", HeaderText = LanguageService.Get("Col_Number"), Width = 48, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nam", HeaderText = LanguageService.Get("Col_Year"), Width = 56, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoLuongBai", HeaderText = LanguageService.Get("Issues_ColArticlesCount"), Width = 80, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UITheme.FontBodyBold } });
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "NgayPhatHanh", HeaderText = LanguageService.Get("Col_PublishDate"), Width = 118, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvIssues.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = LanguageService.Get("Col_Status"), Width = 120, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        }

        private void SetupArticleInIssueColumns()
        {
            dgvArticlesInIssue.Columns.Clear();
            dgvArticlesInIssue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = LanguageService.Get("Col_ArticleCode"), Width = 85, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = UITheme.PrimaryDark } });
            dgvArticlesInIssue.Columns.Add(new DataGridViewTextBoxColumn { Name = "TieuDe", HeaderText = LanguageService.Get("Col_ArticleTitle"), MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvArticlesInIssue.Columns.Add(new DataGridViewTextBoxColumn { Name = "TacGia", HeaderText = LanguageService.Get("Col_LeadAuthor"), Width = 115, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvArticlesInIssue.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChuyenNganh", HeaderText = LanguageService.Get("Col_Field"), Width = 135, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvArticlesInIssue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Trang", HeaderText = LanguageService.Get("Col_Pages"), Width = 70, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvArticlesInIssue.Columns.Add(new DataGridViewTextBoxColumn { Name = "DOI", HeaderText = LanguageService.Get("Col_DOI"), Width = 120, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        }

        public void LoadIssues()
        {
            int selectedId = 0;
            if (dgvIssues.CurrentRow?.Tag is SoTapChi curSo)
            {
                selectedId = curSo.MaSoTapChi;
            }

            var list = SoTapChiService.GetAllSoTapChi();
            dgvIssues.Rows.Clear();

            string inEditorialText = LanguageService.CurrentLanguage == "vi" ? "Đang biên tập" : "In Editorial";

            int selectIndex = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var so = list[i];
                string countText = so.SoLuongBai > 0 
                    ? (LanguageService.CurrentLanguage == "vi" ? $"{so.SoLuongBai} bài" : $"{so.SoLuongBai} articles") 
                    : (LanguageService.CurrentLanguage == "vi" ? "0 bài" : "0");

                int idx = dgvIssues.Rows.Add(
                    so.MaSoTapChi,
                    so.TenSo,
                    so.Tap,
                    so.So,
                    so.Nam,
                    countText,
                    so.NgayPhatHanh?.ToString("dd/MM/yyyy") ?? "—",
                    LanguageService.TranslateStatus(so.TrangThai)
                );
                dgvIssues.Rows[idx].Tag = so;

                if (so.MaSoTapChi == selectedId)
                {
                    selectIndex = idx;
                }
            }

            if (dgvIssues.Rows.Count > 0)
            {
                dgvIssues.Rows[selectIndex].Selected = true;
                DgvIssues_SelectionChanged(null, EventArgs.Empty);
            }
        }

        private void DgvIssues_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvIssues.CurrentRow?.Tag is SoTapChi so)
            {
                string issueLabel = so.TenSo.Contains("-") 
                    ? so.TenSo.Substring(so.TenSo.LastIndexOf('-') + 1).Trim() 
                    : so.TenSo;
                string volText = LanguageService.CurrentLanguage == "vi" ? $"Tập {so.Tap}" : $"Vol. {so.Tap}";
                string tmpl = LanguageService.Get("Issues_ArticlesInSelectedParam");
                lblIssueArticlesTitle.Text = string.Format(tmpl, $"{issueLabel} ({volText})");

                var allArticles = BaiBaoService.GetAllBaiBao();
                var inIssue = allArticles.FindAll(b => b.MaSoTapChi == so.MaSoTapChi);

                dgvArticlesInIssue.Rows.Clear();
                foreach (var b in inIssue)
                {
                    string trang = (b.TrangBatDau.HasValue && b.TrangKetThuc.HasValue) ? $"{b.TrangBatDau:D2} - {b.TrangKetThuc:D2}" : "-";
                    int idx = dgvArticlesInIssue.Rows.Add(
                        b.MaDinhDanh,
                        b.TieuDe,
                        b.TenTacGia,
                        b.TenChuyenNganh,
                        trang,
                        b.MaDOI ?? "-"
                    );
                    dgvArticlesInIssue.Rows[idx].Tag = b;
                }

                // Show empty notice if no articles
                lblEmptyNotice.Visible = (inIssue.Count == 0);
            }
        }

        private void BtnArticleAdd_Click(object? sender, EventArgs e)
        {
            if (dgvIssues.CurrentRow?.Tag is SoTapChi so)
            {
                using var frm = new FrmChonBaiVaoSoDialog(so);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadIssues();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một số tạp chí ở bảng trên trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnArticleEdit_Click(object? sender, EventArgs e)
        {
            if (dgvArticlesInIssue.CurrentRow?.Tag is BaiBao b)
            {
                using var frm = new FrmSuaTrangVaDoiDialog(b);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadIssues();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bài báo trong bảng dưới để chỉnh sửa trang và DOI.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnArticleRemove_Click(object? sender, EventArgs e)
        {
            if (dgvArticlesInIssue.CurrentRow?.Tag is BaiBao b)
            {
                var dr = MessageBox.Show(
                    $"Bạn có chắc chắn muốn GỠ bài báo [{b.MaDinhDanh}] ra khỏi số tạp chí này?\n\n" +
                    $"• Tiêu đề: {b.TieuDe}\n• Tác giả: {b.TenTacGia}\n\n" +
                    "Bài báo sẽ được đưa về lại trạng thái Chế bản và sẵn sàng gán vào số khác.",
                    "Xác nhận gỡ bài khỏi số",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    bool ok = BaiBaoService.GoBaiKhoiSo(b.MaBaiBao, out string? err);
                    if (ok)
                    {
                        MessageBox.Show($"Đã gỡ bài báo [{b.MaDinhDanh}] ra khỏi số tạp chí!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadIssues();
                    }
                    else
                    {
                        MessageBox.Show(err ?? "Không thể gỡ bài báo khỏi số.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bài báo trong bảng dưới cần gỡ khỏi số.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnArticleDetail_Click(object? sender, EventArgs e)
        {
            if (dgvArticlesInIssue.CurrentRow?.Tag is BaiBao b)
            {
                using var frm = new FrmChiTietBaiBao(b.MaBaiBao);
                frm.ShowDialog();
                LoadIssues();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bài báo để xem chi tiết.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvIssues.CurrentRow?.Tag is SoTapChi so)
            {
                using var frm = new FrmSoTapChiDialog(so);
                if (frm.ShowDialog() == DialogResult.OK) LoadIssues();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một số tạp chí.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDeleteIssue_Click(object? sender, EventArgs e)
        {
            if (dgvIssues.CurrentRow?.Tag is SoTapChi so)
            {
                var dr = MessageBox.Show($"Bạn có chắc chắn muốn xóa bản nháp số tạp chí này?\n\nTên số: {so.TenSo}\nTập {so.Tap}, Số {so.So} ({so.Nam})\n\nCần gỡ tất cả bài báo trước khi xóa số.", "Xác nhận xóa số tạp chí", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    bool ok = SoTapChiService.XoaSoTapChi(so.MaSoTapChi, out string? err);
                    if (ok)
                    {
                        MessageBox.Show("Đã xóa số tạp chí thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadIssues();
                    }
                    else
                    {
                        MessageBox.Show(err ?? "Không thể xóa số tạp chí.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một số tạp chí trong bảng cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnPublishIssue_Click(object? sender, EventArgs e)
        {
            if (dgvIssues.CurrentRow?.Tag is SoTapChi so)
            {
                var dr = MessageBox.Show($"XÁC NHẬN CÔNG KHAI VÀ PHÁT HÀNH TRỰC TUYẾN SỐ TẠP CHÍ?\n\n• Tên số: {so.TenSo}\n• Tập {so.Tap}, Số {so.So} ({so.Nam})\n\nToàn bộ các bài báo thuộc số này sẽ được chuyển sang trạng thái ĐÃ XUẤT BẢN và công khai lên cổng thông tin khoa học.", "Xác nhận phát hành trực tuyến", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    bool ok = SoTapChiService.PhatHanhSoBao(so.MaSoTapChi, out string? err);
                    if (ok)
                    {
                        MessageBox.Show("Phát hành số tạp chí trực tuyến thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadIssues();
                    }
                    else
                    {
                        MessageBox.Show(err ?? "Lỗi khi phát hành số báo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một số tạp chí cần phát hành.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
