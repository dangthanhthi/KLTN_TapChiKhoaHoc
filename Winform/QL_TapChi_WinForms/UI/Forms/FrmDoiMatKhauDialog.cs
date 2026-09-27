using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmDoiMatKhauDialog : Form
    {
        private TextBox txtOldPassword = null!;
        private TextBox txtNewPassword = null!;
        private TextBox txtConfirmPassword = null!;
        private CheckBox chkShowPassword = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public FrmDoiMatKhauDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Đổi mật khẩu tài khoản";
            Size = new Size(460, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
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
                Text = "ĐỔI MẬT KHẨU TÀI KHOẢN",
                Font = UITheme.FontSection,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                AutoSize = true
            };

            var userEmail = AuthService.CurrentUser?.Email ?? "admin@huit.edu.vn";
            var lblSub = new Label
            {
                Text = $"Đang đổi mật khẩu cho: {userEmail}",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 38),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Fields
            var lblOld = new Label { Text = "Mật khẩu hiện tại (*):", Font = UITheme.FontBodyBold, Location = new Point(24, 85), AutoSize = true };
            txtOldPassword = new TextBox { Location = new Point(24, 110), Size = new Size(395, 28), Font = new Font("Segoe UI", 10f), UseSystemPasswordChar = true };

            var lblNew = new Label { Text = "Mật khẩu mới (*):", Font = UITheme.FontBodyBold, Location = new Point(24, 155), AutoSize = true };
            txtNewPassword = new TextBox { Location = new Point(24, 180), Size = new Size(395, 28), Font = new Font("Segoe UI", 10f), UseSystemPasswordChar = true };

            var lblConfirm = new Label { Text = "Xác nhận mật khẩu mới (*):", Font = UITheme.FontBodyBold, Location = new Point(24, 225), AutoSize = true };
            txtConfirmPassword = new TextBox { Location = new Point(24, 250), Size = new Size(395, 28), Font = new Font("Segoe UI", 10f), UseSystemPasswordChar = true };

            chkShowPassword = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 290),
                AutoSize = true
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                bool show = chkShowPassword.Checked;
                txtOldPassword.UseSystemPasswordChar = !show;
                txtNewPassword.UseSystemPasswordChar = !show;
                txtConfirmPassword.UseSystemPasswordChar = !show;
            };

            btnSave = new ModernButton
            {
                Text = "Lưu thay đổi",
                Location = new Point(205, 325),
                Size = new Size(130, 38)
            };
            UITheme.ApplyPrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnCancel = new ModernButton
            {
                Text = "Đóng",
                Location = new Point(345, 325),
                Size = new Size(74, 38)
            };
            UITheme.ApplySecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                lblOld, txtOldPassword,
                lblNew, txtNewPassword,
                lblConfirm, txtConfirmPassword,
                chkShowPassword,
                btnSave, btnCancel
            });
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string oldPass = txtOldPassword.Text.Trim();
            string newPass = txtNewPassword.Text.Trim();
            string confirmPass = txtConfirmPassword.Text.Trim();

            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass.Length < 6)
            {
                MessageBox.Show("Mật khẩu mới phải có tối thiểu 6 ký tự!", "Bảo mật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Xác nhận mật khẩu mới không trùng khớp!", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int userId = AuthService.CurrentUser?.MaNguoiDung ?? 1;
            bool ok = AuthService.DoiMatKhau(userId, oldPass, newPass, out string? err);
            if (ok)
            {
                MessageBox.Show("Đã đổi mật khẩu thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(err ?? "Không thể đổi mật khẩu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
