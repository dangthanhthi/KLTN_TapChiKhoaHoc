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
    public class UcQuanLyBaiBao : UserControl
    {
        private Label lblTitle = null!;
        private Label lblSearch = null!;
        private Label lblCategory = null!;
        private DataGridView dgvArticles = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboCategory = null!;
        private ModernButton btnAdd = null!;
        private ModernButton btnDetail = null!;
        private ModernButton btnEdit = null!;
        private ModernButton btnDelete = null!;
        private ModernButton btnRefresh = null!;

        private readonly List<ModernButton> _pipelineTabButtons = new();
        private int _activePipelineTabIndex = 0;

        public UcQuanLyBaiBao()
        {
            InitializeComponent();
            ApplyLanguage();
            LanguageService.OnLanguageChanged += ApplyLanguage;
            LoadCategories();
            LoadArticles();
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
            Padding = new Padding(24, 14, 24, 20);

            // Filter & Action Toolbar Panel (Compact 2-tier layout, saves 60px height)
            // Filter & Action Toolbar Panel (Structured 3-tier TableLayout)
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 122,
                BackColor = Color.White,
                Padding = new Padding(16, 6, 16, 6)
            };
            pnlToolbar.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlToolbar.Width - 1, pnlToolbar.Height - 1);
            };

            var tlpToolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpToolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f)); // Row 0: Title & Filter
            tlpToolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f)); // Row 1: Pipeline Tabs
            tlpToolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f)); // Row 2: Action Buttons
            tlpToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Tier 1: Page Title (Left) + Search & Category Filters (Right)
            var pnlRow1 = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = LanguageService.Get("Articles_Title"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            var flpFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false,
                Padding = new Padding(0, 1, 0, 0)
            };

            lblSearch = new Label
            {
                Text = LanguageService.Get("Search"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 6, 4, 0)
            };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(160, 26),
                PlaceholderText = LanguageService.Get("Articles_SearchPlaceholder"),
                Margin = new Padding(0, 2, 10, 0)
            };
            txtSearch.TextChanged += (s, e) => LoadArticles();

            lblCategory = new Label
            {
                Text = LanguageService.Get("Articles_CategoryFilter"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0)
            };

            cboCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(175, 26),
                Margin = new Padding(0, 2, 0, 0)
            };
            cboCategory.SelectedIndexChanged += (s, e) => LoadArticles();

            flpFilters.Controls.Add(lblSearch);
            flpFilters.Controls.Add(txtSearch);
            flpFilters.Controls.Add(lblCategory);
            flpFilters.Controls.Add(cboCategory);

            pnlRow1.Controls.Add(lblTitle);
            pnlRow1.Controls.Add(flpFilters);

            // Tier 2: Editorial Workflow Pipeline Tabs (Pill buttons with live counters)
            var flpPipelineTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false,
                AutoScroll = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };

            string[] tabTitles = new string[]
            {
                "Tất cả (0)",
                "Sơ duyệt (0)",
                "Phản biện (0)",
                "Quyết định (0)",
                "Chế bản (0)",
                "Xuất bản (0)",
                "Từ chối (0)"
            };

            _pipelineTabButtons.Clear();
            for (int i = 0; i < tabTitles.Length; i++)
            {
                int tabIdx = i;
                var btnTab = new ModernButton
                {
                    Text = tabTitles[i],
                    Height = 28,
                    AutoSize = true,
                    Padding = new Padding(8, 0, 8, 0),
                    NormalColor = (i == 0) ? UITheme.Primary : Color.FromArgb(243, 246, 250),
                    ForeColor = (i == 0) ? Color.White : UITheme.TextSecondary,
                    BorderColor = (i == 0) ? UITheme.Primary : UITheme.BorderColor,
                    HoverColor = (i == 0) ? UITheme.PrimaryHover : Color.FromArgb(232, 238, 245),
                    Margin = new Padding(0, 0, 5, 0),
                    Font = (i == 0) ? UITheme.FontSmallBold : UITheme.FontSmall
                };
                btnTab.Click += (s, e) => SelectPipelineTab(tabIdx);
                _pipelineTabButtons.Add(btnTab);
                flpPipelineTabs.Controls.Add(btnTab);
            }

            // Tier 3: Action Buttons (Full width across bottom of toolbar)
            var flpActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };

            btnAdd = new ModernButton
            {
                Text = LanguageService.Get("Articles_BtnAdd"),
                Size = new Size(135, 32),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplyPrimaryButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;

            btnDetail = new ModernButton
            {
                Text = LanguageService.Get("Articles_BtnDetail"),
                Size = new Size(180, 32),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplySecondaryButton(btnDetail);
            btnDetail.Click += BtnDetail_Click;

            btnEdit = new ModernButton
            {
                Text = LanguageService.Get("Articles_BtnEdit"),
                Size = new Size(95, 32),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplySecondaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new ModernButton
            {
                Text = LanguageService.Get("Articles_BtnDelete"),
                Size = new Size(85, 32),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplyDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;

            btnRefresh = new ModernButton
            {
                Text = LanguageService.Get("Refresh"),
                Size = new Size(85, 32),
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplySecondaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadArticles();

            flpActions.Controls.AddRange(new Control[] { btnAdd, btnDetail, btnEdit, btnDelete, btnRefresh });

            tlpToolbar.Controls.Add(pnlRow1, 0, 0);
            tlpToolbar.Controls.Add(flpPipelineTabs, 0, 1);
            tlpToolbar.Controls.Add(flpActions, 0, 2);

            pnlToolbar.Controls.Add(tlpToolbar);

            // Grid Container Panel
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0, 12, 0, 0)
            };
            pnlGrid.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlGrid.Width - 1, pnlGrid.Height - 1);
            };

            dgvArticles = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyModernGridStyle(dgvArticles);
            SetupGridColumns();

            dgvArticles.CellPainting += DgvArticles_CellPainting;
            dgvArticles.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) BtnDetail_Click(s, e);
            };

            pnlGrid.Controls.Add(dgvArticles);

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 12 };

            Controls.Add(pnlGrid);
            Controls.Add(pnlSpacer);
            Controls.Add(pnlToolbar);
        }

        private void SetupGridColumns()
        {
            dgvArticles.Columns.Clear();

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaDinhDanh",
                HeaderText = LanguageService.Get("Col_ArticleCode"),
                Width = 75,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    ForeColor = UITheme.PrimaryDark,
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TieuDe",
                HeaderText = LanguageService.Get("Col_ArticleTitle"),
                MinimumWidth = 140,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ChuyenNganh",
                HeaderText = LanguageService.Get("Col_Field"),
                Width = 130,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TacGia",
                HeaderText = LanguageService.Get("Col_LeadAuthor"),
                Width = 110,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = LanguageService.Get("Col_Stage"),
                Width = 115,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PhanBien",
                HeaderText = LanguageService.Get("Col_Reviews"),
                Width = 100,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NgayCapNhat",
                HeaderText = LanguageService.Get("Col_UpdatedDate"),
                Width = 110,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
        }

        private void DgvArticles_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvArticles.Columns[e.ColumnIndex].Name == "TrangThai")
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

        private void LoadCategories()
        {
            int prevCatId = (cboCategory.SelectedItem is ComboBoxItem item && item.Value > 0) ? item.Value : 0;
            cboCategory.Items.Clear();
            string allFieldsText = LanguageService.CurrentLanguage == "vi" ? "Tất cả chuyên ngành" : "All Fields";
            cboCategory.Items.Add(new ComboBoxItem(allFieldsText, 0));

            var list = SoTapChiService.GetAllChuyenNganh();
            int selectIdx = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                cboCategory.Items.Add(new ComboBoxItem(c.TenChuyenNganh, c.MaChuyenNganh));
                if (c.MaChuyenNganh == prevCatId) selectIdx = i + 1;
            }
            cboCategory.SelectedIndex = selectIdx;
        }

        public void LoadArticles()
        {
            string? kw = txtSearch.Text.Trim();
            int? catId = (cboCategory.SelectedItem is ComboBoxItem item && item.Value > 0) ? item.Value : null;

            // Map tab index to exact status array (eliminates tab desync bug!)
            string[]? statusFilterList = _activePipelineTabIndex switch
            {
                1 => new[] { "Chờ sơ duyệt", "Chờ sửa hình thức" },
                2 => new[] { "Đang phản biện" },
                3 => new[] { "Chờ quyết định", "Chờ chỉnh sửa", "Đã chấp nhận", "Chấp nhận đăng" },
                4 => new[] { "Đang chế bản", "Sẵn sàng xuất bản" },
                5 => new[] { "Đã xuất bản" },
                6 => new[] { "Từ chối" },
                _ => null
            };

            var list = BaiBaoService.GetAllBaiBao(kw, null, catId, statusFilterList);
            dgvArticles.Rows.Clear();

            string reviewUnit = LanguageService.CurrentLanguage == "vi" ? "phiếu" : "reviews";

            foreach (var b in list)
            {
                string pbInfo = b.SoPhanBienDaGiao > 0 ? $"{b.SoPhanBienDaDanhGia}/{b.SoPhanBienDaGiao} {reviewUnit}" : "—";
                string displayStatus = LanguageService.TranslateStatus(b.TrangThai);
                int idx = dgvArticles.Rows.Add(
                    b.MaDinhDanh,
                    b.TieuDe,
                    b.TenChuyenNganh,
                    b.TenTacGia,
                    displayStatus,
                    pbInfo,
                    b.NgayCapNhat.ToString("dd/MM/yyyy")
                );
                dgvArticles.Rows[idx].Tag = b;
            }

            UpdatePipelineTabCounts();
        }

        public void SelectPipelineTab(int index)
        {
            if (index < 0 || index >= _pipelineTabButtons.Count) return;
            _activePipelineTabIndex = index;

            for (int i = 0; i < _pipelineTabButtons.Count; i++)
            {
                bool isActive = (i == index);
                _pipelineTabButtons[i].NormalColor = isActive ? UITheme.Primary : Color.FromArgb(243, 246, 250);
                _pipelineTabButtons[i].ForeColor = isActive ? Color.White : UITheme.TextSecondary;
                _pipelineTabButtons[i].BorderColor = isActive ? UITheme.Primary : UITheme.BorderColor;
                _pipelineTabButtons[i].HoverColor = isActive ? UITheme.PrimaryHover : Color.FromArgb(232, 238, 245);
                _pipelineTabButtons[i].Font = isActive ? UITheme.FontSmallBold : UITheme.FontSmall;
            }

            LoadArticles();
        }

        public void SetStatusFilter(string statusName)
        {
            if (string.IsNullOrEmpty(statusName))
            {
                SelectPipelineTab(0);
                return;
            }

            if (statusName.Contains("sơ duyệt", StringComparison.OrdinalIgnoreCase)) SelectPipelineTab(1);
            else if (statusName.Contains("phản biện", StringComparison.OrdinalIgnoreCase)) SelectPipelineTab(2);
            else if (statusName.Contains("sửa", StringComparison.OrdinalIgnoreCase) || statusName.Contains("quyết định", StringComparison.OrdinalIgnoreCase)) SelectPipelineTab(3);
            else if (statusName.Contains("chế bản", StringComparison.OrdinalIgnoreCase)) SelectPipelineTab(4);
            else if (statusName.Contains("xuất bản", StringComparison.OrdinalIgnoreCase)) SelectPipelineTab(5);
            else if (statusName.Contains("từ chối", StringComparison.OrdinalIgnoreCase)) SelectPipelineTab(6);
            else SelectPipelineTab(0);
        }

        private void UpdatePipelineTabCounts()
        {
            if (_pipelineTabButtons.Count < 7) return;

            var allArticles = BaiBaoService.GetAllBaiBao(null, null, null);
            int total = allArticles.Count;
            int soDuyet = allArticles.FindAll(b => b.TrangThai == "Chờ sơ duyệt" || b.TrangThai == "Chờ sửa hình thức").Count;
            int phanBien = allArticles.FindAll(b => b.TrangThai == "Đang phản biện").Count;
            int choSua = allArticles.FindAll(b => b.TrangThai == "Chờ chỉnh sửa" || b.TrangThai == "Chờ quyết định" || b.TrangThai == "Đã chấp nhận" || b.TrangThai == "Chấp nhận đăng").Count;
            int cheBan = allArticles.FindAll(b => b.TrangThai == "Đang chế bản" || b.TrangThai == "Sẵn sàng xuất bản").Count;
            int xuatBan = allArticles.FindAll(b => b.TrangThai == "Đã xuất bản").Count;
            int tuChoi = allArticles.FindAll(b => b.TrangThai == "Từ chối").Count;

            string isEn = LanguageService.CurrentLanguage == "en" ? "en" : "vi";

            _pipelineTabButtons[0].Text = isEn == "en" ? $"All ({total})" : $"Tất cả ({total})";
            _pipelineTabButtons[1].Text = isEn == "en" ? $"Screening ({soDuyet})" : $"Sơ duyệt ({soDuyet})";
            _pipelineTabButtons[2].Text = isEn == "en" ? $"Review ({phanBien})" : $"Phản biện ({phanBien})";
            _pipelineTabButtons[3].Text = isEn == "en" ? $"Decision ({choSua})" : $"Quyết định ({choSua})";
            _pipelineTabButtons[4].Text = isEn == "en" ? $"Production ({cheBan})" : $"Chế bản ({cheBan})";
            _pipelineTabButtons[5].Text = isEn == "en" ? $"Published ({xuatBan})" : $"Xuất bản ({xuatBan})";
            _pipelineTabButtons[6].Text = isEn == "en" ? $"Rejected ({tuChoi})" : $"Từ chối ({tuChoi})";
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = LanguageService.Get("Articles_Title");
            lblSearch.Text = LanguageService.Get("Search");
            txtSearch.PlaceholderText = LanguageService.Get("Articles_SearchPlaceholder");
            lblCategory.Text = LanguageService.Get("Articles_CategoryFilter");

            btnAdd.Text = LanguageService.Get("Articles_BtnAdd");
            btnDetail.Text = LanguageService.Get("Articles_BtnDetail");
            btnEdit.Text = LanguageService.Get("Articles_BtnEdit");
            btnDelete.Text = LanguageService.Get("Articles_BtnDelete");
            btnRefresh.Text = LanguageService.Get("Refresh");

            if (dgvArticles.Columns["MaDinhDanh"] != null) dgvArticles.Columns["MaDinhDanh"].HeaderText = LanguageService.Get("Col_ArticleCode");
            if (dgvArticles.Columns["TieuDe"] != null) dgvArticles.Columns["TieuDe"].HeaderText = LanguageService.Get("Col_ArticleTitle");
            if (dgvArticles.Columns["ChuyenNganh"] != null) dgvArticles.Columns["ChuyenNganh"].HeaderText = LanguageService.Get("Col_Field");
            if (dgvArticles.Columns["TacGia"] != null) dgvArticles.Columns["TacGia"].HeaderText = LanguageService.Get("Col_LeadAuthor");
            if (dgvArticles.Columns["TrangThai"] != null) dgvArticles.Columns["TrangThai"].HeaderText = LanguageService.Get("Col_Stage");
            if (dgvArticles.Columns["PhanBien"] != null) dgvArticles.Columns["PhanBien"].HeaderText = LanguageService.Get("Col_Reviews");
            if (dgvArticles.Columns["NgayCapNhat"] != null) dgvArticles.Columns["NgayCapNhat"].HeaderText = LanguageService.Get("Col_UpdatedDate");

            LoadCategories();
            UpdatePipelineTabCounts();
            LoadArticles();
        }

        private void BtnDetail_Click(object? sender, EventArgs e)
        {
            if (dgvArticles.CurrentRow?.Tag is BaiBao b)
            {
                using var frm = new FrmChiTietBaiBao(b.MaBaiBao);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadArticles();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bài báo trong bảng để xem chi tiết.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var frm = new FrmBaiBaoDialog();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadArticles();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvArticles.CurrentRow?.Tag is BaiBao b)
            {
                using var frm = new FrmBaiBaoDialog(b.MaBaiBao);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadArticles();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bản thảo trong bảng để sửa thông tin.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvArticles.CurrentRow?.Tag is BaiBao b)
            {
                var dr = MessageBox.Show($"Rút hồ sơ bản thảo:\n\"{b.TieuDe}\" (Mã: JST-{b.MaBaiBao:D4})?\n\nChỉ được rút trước phản biện. Hồ sơ và lịch sử vẫn được lưu để kiểm toán.", "Xác nhận rút hồ sơ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    bool ok = BaiBaoService.XoaBaiBao(b.MaBaiBao, out string? err);
                    if (ok)
                    {
                        MessageBox.Show("Đã rút hồ sơ bản thảo.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadArticles();
                    }
                    else
                    {
                        MessageBox.Show(err ?? "Không thể rút hồ sơ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bản thảo trong bảng để rút hồ sơ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private class ComboBoxItem
        {
            public string Text { get; }
            public int Value { get; }

            public ComboBoxItem(string text, int value)
            {
                Text = text;
                Value = value;
            }

            public override string ToString() => Text;
        }
    }
}
