using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmSoTapChiDialog : Form
    {
        private readonly SoTapChi _issue;
        private TextBox txtTenSo = null!;
        private NumericUpDown numTap = null!;
        private NumericUpDown numSo = null!;
        private NumericUpDown numNam = null!;
        private DateTimePicker dtpPhatHanh = null!;
        private CheckBox chkCoPhatHanh = null!;
        private ComboBox cboTrangThai = null!;
        private ModernButton btnSave = null!;

        public FrmSoTapChiDialog(SoTapChi? issue = null)
        {
            _issue = issue ?? new SoTapChi
            {
                Tap = 15,
                So = 44,
                Nam = DateTime.Now.Year,
                TenSo = $"Tạp chí Khoa học và Công nghệ - Số 44",
                TrangThai = "Đang biên tập"
            };

            InitializeComponent();
            BindData();
        }

        private void InitializeComponent()
        {
            Text = _issue.MaSoTapChi == 0 ? "Thêm mới số tạp chí phát hành" : "Chỉnh sửa thông tin số tạp chí";
            Size = new Size(520, 480);
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
                Text = _issue.MaSoTapChi == 0 ? "THÊM SỐ TẠP CHÍ MỚI" : "CHỈNH SỬA SỐ TẠP CHÍ",
                Font = UITheme.FontSection,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Quản lý các tập san, số chuyên đề và ngày phát hành chính thức",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 36),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Fields
            var lblTen = new Label { Text = "Tên số tạp chí / Chuyên đề (*):", Font = UITheme.FontBodyBold, Location = new Point(24, 80), AutoSize = true };
            txtTenSo = new TextBox { Location = new Point(24, 102), Size = new Size(455, 30), Font = new Font("Segoe UI", 10f) };

            var lblTap = new Label { Text = "Tập (Volume):", Font = UITheme.FontBodyBold, Location = new Point(24, 145), AutoSize = true };
            numTap = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 15, Location = new Point(24, 170), Size = new Size(130, 30), Font = new Font("Segoe UI", 10f) };

            var lblSo = new Label { Text = "Số (Issue):", Font = UITheme.FontBodyBold, Location = new Point(185, 145), AutoSize = true };
            numSo = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 44, Location = new Point(185, 170), Size = new Size(130, 30), Font = new Font("Segoe UI", 10f) };

            var lblNam = new Label { Text = "Năm xuất bản:", Font = UITheme.FontBodyBold, Location = new Point(345, 145), AutoSize = true };
            numNam = new NumericUpDown { Minimum = 2000, Maximum = 2099, Value = 2026, Location = new Point(345, 170), Size = new Size(134, 30), Font = new Font("Segoe UI", 10f) };

            chkCoPhatHanh = new CheckBox
            {
                Text = "Đã có ngày phát hành chính thức:",
                Font = UITheme.FontBodyBold,
                Location = new Point(24, 220),
                AutoSize = true
            };
            chkCoPhatHanh.CheckedChanged += (s, e) => dtpPhatHanh.Enabled = chkCoPhatHanh.Checked;

            dtpPhatHanh = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now,
                Location = new Point(24, 245),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 10f),
                Enabled = false
            };

            var lblTrangThai = new Label { Text = "Trạng thái số báo:", Font = UITheme.FontBodyBold, Location = new Point(255, 220), AutoSize = true };
            cboTrangThai = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(255, 245),
                Size = new Size(224, 30)
            };
            cboTrangThai.Items.AddRange(new object[] { "Đang biên tập", "Đã đóng" });
            cboTrangThai.SelectedIndex = 0;

            btnSave = new ModernButton
            {
                Text = "Lưu số tạp chí",
                Size = new Size(160, 42),
                Location = new Point(319, 360),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryDark
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 42),
                Location = new Point(180, 360),
                FlatStyle = FlatStyle.Flat,
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextSecondary
            };
            btnCancel.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                lblTen, txtTenSo,
                lblTap, numTap,
                lblSo, numSo,
                lblNam, numNam,
                chkCoPhatHanh, dtpPhatHanh,
                lblTrangThai, cboTrangThai,
                btnSave, btnCancel
            });
        }

        private void BindData()
        {
            txtTenSo.Text = _issue.TenSo;
            numTap.Value = _issue.Tap;
            numSo.Value = _issue.So;
            numNam.Value = _issue.Nam;
            if (_issue.NgayPhatHanh.HasValue)
            {
                chkCoPhatHanh.Checked = true;
                dtpPhatHanh.Value = _issue.NgayPhatHanh.Value;
                dtpPhatHanh.Enabled = true;
            }

            int idx = cboTrangThai.FindStringExact(_issue.TrangThai);
            if (idx >= 0) cboTrangThai.SelectedIndex = idx;
            if (_issue.TrangThai == "Đã xuất bản" || _issue.TrangThai == "Đã phát hành")
            {
                btnSave.Enabled = false;
                cboTrangThai.Enabled = false;
                Text = "Xem số tạp chí đã phát hành";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenSo.Text))
            {
                MessageBox.Show("Vui lòng nhập tên số tạp chí.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _issue.TenSo = txtTenSo.Text.Trim();
            _issue.Tap = (int)numTap.Value;
            _issue.So = (int)numSo.Value;
            _issue.Nam = (int)numNam.Value;
            _issue.NgayPhatHanh = chkCoPhatHanh.Checked ? dtpPhatHanh.Value.Date : null;
            _issue.TrangThai = cboTrangThai.SelectedItem?.ToString() ?? "Đang biên tập";

            bool ok = SoTapChiService.SaveSoTapChi(_issue);
            if (ok)
            {
                MessageBox.Show("Lưu số tạp chí thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(JournalApiClient.LastError ?? "Không thể lưu số tạp chí.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
