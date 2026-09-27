using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmNguoiDungDialog : Form
    {
        private readonly NguoiDung _user;
        private TextBox txtHoTen = null!;
        private TextBox txtEmail = null!;
        private TextBox txtMatKhau = null!;
        private TextBox txtPhone = null!;
        private TextBox txtDonVi = null!;
        private ComboBox cboHocVi = null!;
        private TextBox txtOrcid = null!;
        private CheckBox chkTrangThai = null!;
        private CheckedListBox clbRoles = null!;
        private ModernButton btnSave = null!;

        public FrmNguoiDungDialog(NguoiDung? user = null)
        {
            _user = user ?? new NguoiDung { TrangThai = true };
            InitializeComponent();
            LoadRoles();
            BindUser();
        }

        private void InitializeComponent()
        {
            Text = _user.MaNguoiDung == 0 ? "Thêm mới tài khoản người dùng" : "Cập nhật tài khoản & phân quyền";
            Size = new Size(580, 645);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = UITheme.FontBody;

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = UITheme.Sky050,
                Padding = new Padding(24, 12, 24, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.Sky200);
                e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = _user.MaNguoiDung == 0 ? "THÊM TÀI KHOẢN MỚI" : "CHỈNH SỬA THÔNG TIN & PHÂN QUYỀN",
                Font = UITheme.FontSection,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Cung cấp quyền hạn và định danh khoa học (ORCID, Học vị)",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 36),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Fields
            var lblName = new Label { Text = "Họ và tên (*):", Font = UITheme.FontBodyBold, Location = new Point(24, 80), AutoSize = true };
            txtHoTen = new TextBox { Location = new Point(24, 102), Size = new Size(515, 30), Font = new Font("Segoe UI", 10f) };

            var lblEmail = new Label { Text = "Email đăng nhập (*):", Font = UITheme.FontBodyBold, Location = new Point(24, 140), AutoSize = true };
            txtEmail = new TextBox { Location = new Point(24, 162), Size = new Size(250, 30), Font = new Font("Segoe UI", 10f) };

            var lblPass = new Label { Text = "Mật khẩu mới (ít nhất 8 ký tự):", Font = UITheme.FontBodyBold, Location = new Point(284, 140), AutoSize = true };
            txtMatKhau = new TextBox { Location = new Point(284, 162), Size = new Size(255, 30), Font = new Font("Segoe UI", 10f), UseSystemPasswordChar = true };

            var lblDonVi = new Label { Text = "Đơn vị công tác / Viện / Khoa:", Font = UITheme.FontBodyBold, Location = new Point(24, 200), AutoSize = true };
            txtDonVi = new TextBox { Location = new Point(24, 222), Size = new Size(515, 30), Font = new Font("Segoe UI", 10f) };

            var lblHocVi = new Label { Text = "Học vị:", Font = UITheme.FontBodyBold, Location = new Point(24, 260), Size = new Size(120, 20) };
            cboHocVi = new ComboBox { Location = new Point(24, 282), Size = new Size(125, 30), Font = new Font("Segoe UI", 10f), DropDownStyle = ComboBoxStyle.DropDownList };
            cboHocVi.Items.AddRange(new object[] { "CN", "Kỹ sư", "ThS", "TS", "PGS.TS", "GS.TS" });
            cboHocVi.SelectedIndex = 2;

            var lblOrcid = new Label { Text = "Mã ORCID:", Font = UITheme.FontBodyBold, Location = new Point(160, 260), Size = new Size(180, 20) };
            txtOrcid = new TextBox { Location = new Point(160, 282), Size = new Size(180, 30), Font = new Font("Segoe UI", 10f), PlaceholderText = "0000-0002-1825-0091" };

            var lblPhone = new Label { Text = "Số điện thoại:", Font = UITheme.FontBodyBold, Location = new Point(356, 260), Size = new Size(183, 20) };
            txtPhone = new TextBox { Location = new Point(356, 282), Size = new Size(183, 30), Font = new Font("Segoe UI", 10f), PlaceholderText = "090xxxxxxx" };

            var lblRoles = new Label { Text = "Phân quyền các vai trò hệ thống:", Font = UITheme.FontBodyBold, Location = new Point(24, 325), AutoSize = true };
            clbRoles = new CheckedListBox
            {
                Location = new Point(24, 350),
                Size = new Size(515, 128),
                Font = UITheme.FontBody,
                CheckOnClick = true,
                IntegralHeight = false
            };

            chkTrangThai = new CheckBox
            {
                Text = "Tài khoản đang hoạt động (cho phép đăng nhập)",
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(24, 490),
                AutoSize = true,
                Checked = true
            };

            btnSave = new ModernButton
            {
                Text = "Lưu tài khoản",
                Size = new Size(160, 40),
                Location = new Point(379, 538),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryDark
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 40),
                Location = new Point(265, 538),
                FlatStyle = FlatStyle.Flat,
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextSecondary
            };
            btnCancel.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                lblName, txtHoTen,
                lblEmail, txtEmail,
                lblPass, txtMatKhau,
                lblDonVi, txtDonVi,
                lblHocVi, cboHocVi,
                lblOrcid, txtOrcid,
                lblPhone, txtPhone,
                lblRoles, clbRoles,
                chkTrangThai,
                btnSave, btnCancel
            });
        }

        private void LoadRoles()
        {
            clbRoles.Items.Clear();
            var roles = NguoiDungService.GetAllRoles();
            foreach (var r in roles)
            {
                clbRoles.Items.Add(new RoleItem(r));
            }
        }

        private void BindUser()
        {
            if (_user.MaNguoiDung > 0)
            {
                txtHoTen.Text = _user.HoTen;
                txtEmail.Text = _user.Email;
                txtMatKhau.Text = "";
                txtMatKhau.Enabled = false;
                txtDonVi.Text = _user.DonVi ?? "";
                txtPhone.Text = _user.SoDienThoai ?? "";
                txtOrcid.Text = _user.MaORCID ?? "";
                chkTrangThai.Checked = _user.TrangThai;

                if (AuthService.CurrentUser != null && AuthService.CurrentUser.MaNguoiDung == _user.MaNguoiDung)
                {
                    chkTrangThai.Enabled = false;
                    chkTrangThai.Text += " (Đang đăng nhập)";
                }

                if (!string.IsNullOrEmpty(_user.HocVi))
                {
                    int idx = cboHocVi.FindStringExact(_user.HocVi);
                    if (idx >= 0) cboHocVi.SelectedIndex = idx;
                }

                // Check assigned roles
                for (int i = 0; i < clbRoles.Items.Count; i++)
                {
                    if (clbRoles.Items[i] is RoleItem item && _user.DanhSachVaiTro.Contains(item.Role.TenVaiTro))
                    {
                        clbRoles.SetItemChecked(i, true);
                    }
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHoTen.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Họ tên và Email.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (AuthService.CurrentUser != null && AuthService.CurrentUser.MaNguoiDung == _user.MaNguoiDung)
            {
                if (!chkTrangThai.Checked)
                {
                    MessageBox.Show("Bạn không thể tự khóa tài khoản quản trị viên đang đăng nhập!", "Cảnh báo an toàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkTrangThai.Checked = true;
                    return;
                }

                bool hasAdmin = false;
                foreach (var item in clbRoles.CheckedItems)
                {
                    if (item is RoleItem rItem && (rItem.Role.MaVaiTro == 1 || rItem.Role.TenVaiTro == "QuanTriVien"))
                    {
                        hasAdmin = true;
                        break;
                    }
                }
                if (!hasAdmin && AuthService.CurrentUser.DanhSachVaiTro.Contains("QuanTriVien"))
                {
                    MessageBox.Show("Bạn không thể tự tước vai trò Quản trị viên (QuanTriVien) của chính mình!", "Cảnh báo an toàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            _user.HoTen = txtHoTen.Text.Trim();
            _user.Email = txtEmail.Text.Trim();
            _user.MatKhau = txtMatKhau.Text.Trim();
            _user.DonVi = txtDonVi.Text.Trim();
            _user.HocVi = cboHocVi.SelectedItem?.ToString();
            _user.MaORCID = string.IsNullOrWhiteSpace(txtOrcid.Text) ? null : txtOrcid.Text.Trim();
            _user.SoDienThoai = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim();
            _user.TrangThai = chkTrangThai.Checked;

            var selectedRoleIds = new List<int>();
            foreach (var item in clbRoles.CheckedItems)
            {
                if (item is RoleItem rItem)
                {
                    selectedRoleIds.Add(rItem.Role.MaVaiTro);
                }
            }

            bool ok = NguoiDungService.SaveUser(_user, selectedRoleIds);
            if (ok)
            {
                MessageBox.Show("Lưu thông tin người dùng thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(JournalApiClient.LastError ?? "Không thể lưu thông tin người dùng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class RoleItem
        {
            public VaiTro Role { get; }
            public RoleItem(VaiTro role) => Role = role;
            public override string ToString() => Role.TenVaiTro;
        }
    }
}
