using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Views
{
    public class UcQuanLyChuyenNganh : UserControl
    {
        private Label lblTitle = null!;
        private Label lblFormHeader = null!;
        private Label lblTen = null!;
        private Label lblMoTa = null!;
        private Label lblNotice = null!;
        private DataGridView dgvCategories = null!;
        private TextBox txtTen = null!;
        private TextBox txtMoTa = null!;
        private Label lblMaDisplay = null!;
        private ModernButton btnAdd = null!;
        private ModernButton btnDelete = null!;
        private ModernButton btnRefresh = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;
        private int _selectedId = 0;

        public UcQuanLyChuyenNganh()
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
            Padding = new Padding(24);

            // 1. TOOLBAR PANEL
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
                Text = LanguageService.Get("Fields_Title"),
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
                Text = LanguageService.Get("Fields_BtnAdd"),
                Size = new Size(185, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplyPrimaryButton(btnAdd);
            btnAdd.Click += (s, e) => ResetForm();

            btnDelete = new ModernButton
            {
                Text = LanguageService.Get("Fields_BtnDelete"),
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplyDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;

            btnRefresh = new ModernButton
            {
                Text = LanguageService.Get("Refresh"),
                Size = new Size(100, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplySecondaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadData();

            flpActions.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnRefresh });
            pnlToolbar.Controls.Add(flpActions);
            pnlToolbar.Controls.Add(pnlTitleRow);

            // 2. MAIN CONTAINER
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 16, 0, 0),
                BackColor = UITheme.AppBackground
            };

            // RIGHT FORM PANEL (Editor Card)
            var pnlRightEditor = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            pnlRightEditor.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlRightEditor.Width - 1, pnlRightEditor.Height - 1);
            };

            lblFormHeader = new Label
            {
                Text = LanguageService.Get("Fields_DetailTitle"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(20, 20),
                AutoSize = true,
                UseMnemonic = false
            };

            lblMaDisplay = new Label
            {
                Text = "Mã số: [Tự động sinh khi tạo mới]",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(20, 52),
                AutoSize = true,
                UseMnemonic = false
            };

            lblTen = new Label
            {
                Text = LanguageService.Get("Fields_NameLabel"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(20, 86),
                AutoSize = true,
                UseMnemonic = false
            };

            txtTen = new TextBox
            {
                Font = UITheme.FontBody,
                Location = new Point(20, 110),
                Size = new Size(280, 28)
            };

            lblMoTa = new Label
            {
                Text = LanguageService.Get("Fields_DescLabel"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(20, 150),
                AutoSize = true,
                UseMnemonic = false
            };

            txtMoTa = new TextBox
            {
                Font = UITheme.FontBody,
                Location = new Point(20, 174),
                Size = new Size(280, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            // Notice Box
            var pnlNotice = new Panel
            {
                Location = new Point(20, 290),
                Size = new Size(280, 85),
                BackColor = UITheme.Sky050,
                Padding = new Padding(12)
            };
            pnlNotice.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.Sky200);
                e.Graphics.DrawRectangle(p, 0, 0, pnlNotice.Width - 1, pnlNotice.Height - 1);
            };

            lblNotice = new Label
            {
                Dock = DockStyle.Fill,
                Text = LanguageService.Get("Fields_NoticeText"),
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = UITheme.PrimaryDark,
                TextAlign = ContentAlignment.TopLeft
            };
            pnlNotice.Controls.Add(lblNotice);

            // Buttons
            btnSave = new ModernButton
            {
                Text = LanguageService.Get("Save"),
                Location = new Point(20, 395),
                Size = new Size(135, 38)
            };
            UITheme.ApplyPrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnCancel = new ModernButton
            {
                Text = LanguageService.Get("Fields_BtnClear"),
                Location = new Point(165, 395),
                Size = new Size(135, 38)
            };
            UITheme.ApplySecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => ResetForm();

            pnlRightEditor.Controls.AddRange(new Control[]
            {
                lblFormHeader, lblMaDisplay,
                lblTen, txtTen,
                lblMoTa, txtMoTa,
                pnlNotice,
                btnSave, btnCancel
            });

            // LEFT GRID PANEL
            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 16, 0),
                BackColor = UITheme.AppBackground
            };

            var pnlGridBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlGridBox.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlGridBox.Width - 1, pnlGridBox.Height - 1);
            };

            dgvCategories = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyCompactGridStyle(dgvCategories, 48);

            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Ma",
                HeaderText = LanguageService.Get("Col_Id"),
                Width = 50,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Ten",
                HeaderText = LanguageService.Get("Col_FieldName"),
                Width = 160,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SoBaiBao",
                HeaderText = LanguageService.Get("Col_ArticleCount"),
                Width = 80,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MoTa",
                HeaderText = LanguageService.Get("Col_FieldDesc"),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 120,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvCategories.SelectionChanged += DgvCategories_SelectionChanged;

            pnlGridBox.Controls.Add(dgvCategories);
            pnlGridContainer.Controls.Add(pnlGridBox);

            pnlBody.Controls.Add(pnlGridContainer);
            pnlBody.Controls.Add(pnlRightEditor);
            pnlGridContainer.BringToFront();

            Controls.Add(pnlBody);
            Controls.Add(pnlToolbar);
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = LanguageService.Get("Fields_Title");
            btnAdd.Text = LanguageService.Get("Fields_BtnAdd");
            btnDelete.Text = LanguageService.Get("Fields_BtnDelete");
            btnRefresh.Text = LanguageService.Get("Refresh");

            lblFormHeader.Text = LanguageService.Get("Fields_DetailTitle");
            lblTen.Text = LanguageService.Get("Fields_NameLabel");
            lblMoTa.Text = LanguageService.Get("Fields_DescLabel");
            lblNotice.Text = LanguageService.Get("Fields_NoticeText");
            btnCancel.Text = LanguageService.Get("Fields_BtnClear");

            if (dgvCategories.Columns["Ma"] != null) dgvCategories.Columns["Ma"].HeaderText = LanguageService.Get("Col_FieldCode");
            if (dgvCategories.Columns["Ten"] != null) dgvCategories.Columns["Ten"].HeaderText = LanguageService.Get("Col_FieldName");
            if (dgvCategories.Columns["SoBaiBao"] != null) dgvCategories.Columns["SoBaiBao"].HeaderText = LanguageService.Get("Col_ArticleCount");
            if (dgvCategories.Columns["MoTa"] != null) dgvCategories.Columns["MoTa"].HeaderText = LanguageService.Get("Col_FieldDesc");

            if (_selectedId == 0)
            {
                lblMaDisplay.Text = LanguageService.Get("Fields_CodeAuto");
                btnSave.Text = LanguageService.Get("Add");
            }
            else
            {
                string codePrefix = LanguageService.CurrentLanguage == "vi" ? "Mã số" : "Code";
                lblMaDisplay.Text = $"{codePrefix}: CN-{_selectedId:D3}";
                btnSave.Text = LanguageService.Get("Save");
            }

            LoadData();
        }

        public void LoadData()
        {
            dgvCategories.Rows.Clear();
            var list = ChuyenNganhService.GetAllChuyenNganh();

            string articleSuffix = LanguageService.CurrentLanguage == "vi" ? "bài báo" : "articles";

            foreach (var cn in list)
            {
                int idx = dgvCategories.Rows.Add(
                    cn.MaChuyenNganh,
                    cn.TenChuyenNganh,
                    $"{cn.SoBaiBao} {articleSuffix}",
                    string.IsNullOrWhiteSpace(cn.MoTa) ? "—" : cn.MoTa
                );
                dgvCategories.Rows[idx].Tag = cn;
            }

            if (dgvCategories.Rows.Count > 0)
            {
                dgvCategories.CurrentCell = dgvCategories.Rows[0].Cells[1];
                dgvCategories.Rows[0].Selected = true;
                DgvCategories_SelectionChanged(dgvCategories, EventArgs.Empty);
            }
            else
            {
                ResetForm();
            }
        }

        private void DgvCategories_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvCategories.CurrentRow?.Tag is ChuyenNganh cn)
            {
                _selectedId = cn.MaChuyenNganh;
                string codePrefix = LanguageService.CurrentLanguage == "vi" ? "Mã số" : "Code";
                lblMaDisplay.Text = $"{codePrefix}: CN-{cn.MaChuyenNganh:D3}";
                txtTen.Text = cn.TenChuyenNganh;
                txtMoTa.Text = cn.MoTa ?? string.Empty;
                btnSave.Text = LanguageService.Get("Save");
                btnDelete.Enabled = true;
            }
        }

        private void ResetForm()
        {
            _selectedId = 0;
            dgvCategories.ClearSelection();
            lblMaDisplay.Text = LanguageService.Get("Fields_CodeAuto");
            txtTen.Clear();
            txtMoTa.Clear();
            btnSave.Text = LanguageService.Get("Add");
            btnDelete.Enabled = false;
            txtTen.Focus();
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTen.Text))
            {
                MessageBox.Show("Vui lòng nhập tên chuyên ngành khoa học!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTen.Focus();
                return;
            }

            var cn = new ChuyenNganh
            {
                MaChuyenNganh = _selectedId,
                TenChuyenNganh = txtTen.Text.Trim(),
                MoTa = string.IsNullOrWhiteSpace(txtMoTa.Text) ? null : txtMoTa.Text.Trim()
            };

            bool ok = ChuyenNganhService.SaveChuyenNganh(cn, out string? err);
            if (ok)
            {
                MessageBox.Show(_selectedId == 0 ? "Thêm chuyên ngành thành công!" : "Cập nhật chuyên ngành thành công!",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            else
            {
                MessageBox.Show("Lỗi khi lưu chuyên ngành: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedId == 0)
            {
                MessageBox.Show("Vui lòng chọn chuyên ngành cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var res = MessageBox.Show($"Bạn có chắc chắn muốn xóa chuyên ngành '{txtTen.Text}'?\nThao tác này không thể hoàn tác nếu không có ràng buộc dữ liệu.",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                bool ok = ChuyenNganhService.DeleteChuyenNganh(_selectedId, out string? err);
                if (ok)
                {
                    MessageBox.Show("Xóa chuyên ngành thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetForm();
                    LoadData();
                }
                else
                {
                    MessageBox.Show(err ?? "Không thể xóa chuyên ngành.", "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
        }
    }
}
