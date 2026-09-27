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
    public class UcQuanLyNguoiDung : UserControl
    {
        private Label lblTitle = null!;
        private Label lblSearch = null!;
        private Label lblRole = null!;
        private DataGridView dgvUsers = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboRole = null!;
        private ModernButton btnAdd = null!;
        private ModernButton btnEdit = null!;
        private ModernButton btnToggle = null!;
        private ModernButton btnDelete = null!;
        private ModernButton btnRefresh = null!;

        public UcQuanLyNguoiDung()
        {
            InitializeComponent();
            ApplyLanguage();
            LanguageService.OnLanguageChanged += ApplyLanguage;
            LoadUsers();
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

            // Filter & Action Toolbar Panel (Structured, responsive, no overlaps)
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 138,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 10)
            };
            pnlToolbar.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlToolbar.Width - 1, pnlToolbar.Height - 1);
            };

            // Row 1: Page Title
            var pnlTitleRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = LanguageService.Get("Users_Title"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlTitleRow.Controls.Add(lblTitle);

            // Row 2: Search & Filter Controls (FlowLayoutPanel LeftToRight)
            var flpFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            lblSearch = new Label
            {
                Text = LanguageService.Get("Search"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 6, 6, 0)
            };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(260, 28),
                PlaceholderText = LanguageService.Get("Users_SearchPlaceholder"),
                Margin = new Padding(0, 2, 20, 0)
            };
            txtSearch.TextChanged += (s, e) => LoadUsers();

            lblRole = new Label
            {
                Text = LanguageService.Get("Users_RoleFilter"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 6, 6, 0)
            };

            cboRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(210, 28),
                Margin = new Padding(0, 2, 0, 0)
            };
            cboRole.SelectedIndexChanged += (s, e) => LoadUsers();

            flpFilters.Controls.Add(lblSearch);
            flpFilters.Controls.Add(txtSearch);
            flpFilters.Controls.Add(lblRole);
            flpFilters.Controls.Add(cboRole);

            // Row 3: Action Buttons Toolbar (Full width, seamless layout)
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
                Text = LanguageService.Get("Users_BtnAdd"),
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplyPrimaryButton(btnAdd);
            btnAdd.Click += (s, e) =>
            {
                using var frm = new FrmNguoiDungDialog();
                if (frm.ShowDialog() == DialogResult.OK) LoadUsers();
            };

            btnEdit = new ModernButton
            {
                Text = LanguageService.Get("Users_BtnEdit"),
                Size = new Size(140, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplySecondaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;

            btnToggle = new ModernButton
            {
                Text = LanguageService.Get("Users_BtnToggle"),
                Size = new Size(145, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplySecondaryButton(btnToggle);
            btnToggle.Click += BtnToggle_Click;

            btnDelete = new ModernButton
            {
                Text = LanguageService.Get("Users_BtnDelete"),
                Size = new Size(120, 36),
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.ApplyDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;

            btnRefresh = new ModernButton
            {
                Text = LanguageService.Get("Refresh"),
                Size = new Size(85, 36),
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplySecondaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadUsers();

            flpActions.Controls.Add(btnAdd);
            flpActions.Controls.Add(btnEdit);
            flpActions.Controls.Add(btnToggle);
            flpActions.Controls.Add(btnDelete);
            flpActions.Controls.Add(btnRefresh);

            pnlToolbar.Controls.Add(flpActions);
            pnlToolbar.Controls.Add(flpFilters);
            pnlToolbar.Controls.Add(pnlTitleRow);

            // Grid Container Panel
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(0, 16, 0, 0)
            };
            pnlGrid.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlGrid.Width - 1, pnlGrid.Height - 1);
            };

            dgvUsers = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyCompactGridStyle(dgvUsers, 48);
            SetupGridColumns();

            dgvUsers.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgvUsers.ClearSelection();
                    dgvUsers.Rows[e.RowIndex].Selected = true;
                    dgvUsers.CurrentCell = dgvUsers.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
                }
            };

            dgvUsers.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) BtnEdit_Click(s, e);
            };

            pnlGrid.Controls.Add(dgvUsers);

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

            Controls.Add(pnlGrid);
            Controls.Add(pnlSpacer);
            Controls.Add(pnlToolbar);
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = LanguageService.Get("Users_Title");
            lblSearch.Text = LanguageService.Get("Search");
            txtSearch.PlaceholderText = LanguageService.Get("Users_SearchPlaceholder");
            lblRole.Text = LanguageService.Get("Users_RoleFilter");

            int prevIdx = cboRole.SelectedIndex;
            cboRole.Items.Clear();
            cboRole.Items.AddRange(new object[]
            {
                LanguageService.Get("Role_All"),
                LanguageService.Get("Role_Admin"),
                LanguageService.Get("Role_Editor"),
                LanguageService.Get("Role_Author"),
                LanguageService.Get("Role_Reviewer"),
                LanguageService.Get("Role_Reader")
            });
            cboRole.SelectedIndex = (prevIdx >= 0 && prevIdx < cboRole.Items.Count) ? prevIdx : 0;

            btnAdd.Text = LanguageService.Get("Users_BtnAdd");
            btnEdit.Text = LanguageService.Get("Users_BtnEdit");
            btnToggle.Text = LanguageService.Get("Users_BtnToggle");
            btnDelete.Text = LanguageService.Get("Users_BtnDelete");
            btnRefresh.Text = LanguageService.Get("Refresh");

            if (dgvUsers.Columns["Ma"] != null) dgvUsers.Columns["Ma"].HeaderText = LanguageService.Get("Col_Id");
            if (dgvUsers.Columns["HoTen"] != null) dgvUsers.Columns["HoTen"].HeaderText = LanguageService.Get("Col_FullName");
            if (dgvUsers.Columns["HocVi"] != null) dgvUsers.Columns["HocVi"].HeaderText = LanguageService.Get("Col_Degree");
            if (dgvUsers.Columns["Email"] != null) dgvUsers.Columns["Email"].HeaderText = LanguageService.Get("Col_Email");
            if (dgvUsers.Columns["DonVi"] != null) dgvUsers.Columns["DonVi"].HeaderText = LanguageService.Get("Col_Affiliation");
            if (dgvUsers.Columns["VaiTro"] != null) dgvUsers.Columns["VaiTro"].HeaderText = LanguageService.Get("Col_Role");
            if (dgvUsers.Columns["Orcid"] != null) dgvUsers.Columns["Orcid"].HeaderText = LanguageService.Get("Col_Orcid");
            if (dgvUsers.Columns["TrangThai"] != null) dgvUsers.Columns["TrangThai"].HeaderText = LanguageService.Get("Col_Status");

            LoadUsers();
        }

        private void SetupGridColumns()
        {
            dgvUsers.Columns.Clear();

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Ma",
                HeaderText = LanguageService.Get("Col_Id"),
                Width = 50,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "HoTen",
                HeaderText = LanguageService.Get("Col_FullName"),
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Font = UITheme.FontBodyBold, WrapMode = DataGridViewTriState.True }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "VaiTro",
                HeaderText = LanguageService.Get("Col_Role"),
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(10, 116, 183), Font = UITheme.FontSmallBold, WrapMode = DataGridViewTriState.True }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "HocVi",
                HeaderText = LanguageService.Get("Col_Degree"),
                Width = 75,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Email",
                HeaderText = LanguageService.Get("Col_Email"),
                Width = 180,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DonVi",
                HeaderText = LanguageService.Get("Col_Affiliation"),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 140,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = LanguageService.Get("Col_Status"),
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UITheme.FontSmallBold }
            });

            // Context Menu
            var cms = new ContextMenuStrip();
            cms.Items.Add("Sửa thông tin & Phân quyền", null, (s, e) => BtnEdit_Click(s, e));
            cms.Items.Add("Khóa / Mở khóa tài khoản", null, (s, e) => BtnToggle_Click(s, e));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("Xóa tài khoản", null, (s, e) => BtnDelete_Click(s, e));
            dgvUsers.ContextMenuStrip = cms;
        }

        public void LoadUsers()
        {
            string? kw = txtSearch.Text.Trim();
            string? role = cboRole.SelectedIndex switch
            {
                1 => "Quản trị hệ thống",
                2 => "Ban biên tập",
                3 => "Tác giả",
                4 => "Chuyên gia phản biện",
                5 => "Độc giả",
                _ => null
            };

            var list = NguoiDungService.GetAllUsers(kw, role);
            dgvUsers.Rows.Clear();

            foreach (var u in list)
            {
                string st = u.TrangThai ? LanguageService.Get("Status_Active") : LanguageService.Get("Status_Locked");
                int idx = dgvUsers.Rows.Add(
                    u.MaNguoiDung,
                    u.HoTen,
                    LanguageService.TranslateRole(u.VaiTroHienThi),
                    u.HocVi ?? "-",
                    u.Email,
                    u.DonVi ?? "-",
                    st
                );
                dgvUsers.Rows[idx].Tag = u;

                if (!u.TrangThai)
                {
                    dgvUsers.Rows[idx].DefaultCellStyle.ForeColor = UITheme.TextMuted;
                }
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.CurrentRow?.Tag is NguoiDung u)
            {
                using var frm = new FrmNguoiDungDialog(u);
                if (frm.ShowDialog() == DialogResult.OK) LoadUsers();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn người dùng cần chỉnh sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnToggle_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.CurrentRow?.Tag is NguoiDung u)
            {
                if (AuthService.CurrentUser != null && (u.MaNguoiDung == AuthService.CurrentUser.MaNguoiDung || u.Email.Equals(AuthService.CurrentUser.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Bạn không thể tự khóa tài khoản quản trị đang đăng nhập của chính mình!", "Cảnh báo bảo mật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool nextStatus = !u.TrangThai;
                string actionName = nextStatus ? "Mở khóa" : "Khóa";

                var res = MessageBox.Show($"Xác nhận {actionName} tài khoản của '{u.HoTen}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    NguoiDungService.ToggleUserStatus(u.MaNguoiDung, nextStatus);
                    LoadUsers();
                }
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.CurrentRow?.Tag is NguoiDung u)
            {
                if (AuthService.CurrentUser != null && (u.MaNguoiDung == AuthService.CurrentUser.MaNguoiDung || u.Email.Equals(AuthService.CurrentUser.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Bạn không thể xóa tài khoản quản trị đang đăng nhập của chính mình!", "Cảnh báo bảo mật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var dr = MessageBox.Show($"Bạn có chắc chắn muốn XÓA tài khoản người dùng:\n\n• Họ tên: {u.HoTen}\n• Email: {u.Email}\n• Đơn vị: {u.DonVi}\n\nLưu ý: Nếu tài khoản đã có bài nộp hoặc phân công phản biện, hệ thống sẽ tự động chuyển sang trạng thái ĐÃ KHÓA để bảo toàn lịch sử dữ liệu học thuật.", "Xác nhận xóa tài khoản", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    bool ok = NguoiDungService.DeleteUser(u.MaNguoiDung, out string msg, out bool isSoftDeleted);
                    if (ok)
                    {
                        MessageBox.Show(msg, isSoftDeleted ? "Đã khóa tài khoản" : "Xóa thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadUsers();
                    }
                    else
                    {
                        MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một tài khoản người dùng cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
