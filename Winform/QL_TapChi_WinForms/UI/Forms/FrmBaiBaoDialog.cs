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
    public class FrmBaiBaoDialog : Form
    {
        private TextBox txtTieuDe = null!;
        private TextBox txtTieuDeEn = null!;
        private ComboBox cboChuyenNganh = null!;
        private ComboBox cboTacGia = null!;
        private TextBox txtTomTat = null!;
        private TextBox txtTuKhoa = null!;
        private ModernButton btnSave = null!;

        private readonly int _maBaiBao;
        private readonly bool _isEditMode;

        public FrmBaiBaoDialog(int maBaiBao = 0)
        {
            _maBaiBao = maBaiBao;
            _isEditMode = maBaiBao > 0;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = _isEditMode ? "Chỉnh sửa thông tin bản thảo · JST" : "Tiếp nhận bản thảo mới tại Tòa soạn · JST";
            Size = new Size(680, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.AppBackground;

            // 1. Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = UITheme.HeaderBg
            };

            var lblTitle = new Label
            {
                Text = _isEditMode ? "CẬP NHẬT THÔNG TIN BẢN THẢO" : "TIẾP NHẬN BẢN THẢO MỚI TẠI TÒA SOẠN",
                Font = UITheme.FontTitle,
                ForeColor = Color.White,
                Location = new Point(24, 14),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = _isEditMode ? $"Chỉnh sửa tiêu đề, chuyên ngành, tóm tắt và từ khóa bài báo (Mã: JST-{_maBaiBao:D4})" : "Nhập đầy đủ thông tin học thuật theo quy chuẩn Tạp chí Khoa học và Công nghệ",
                Font = UITheme.FontSmall,
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(25, 42),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // 2. Card Content
            var pnlCard = new Panel
            {
                Location = new Point(24, 86),
                Size = new Size(616, 510),
                BackColor = Color.White
            };
            pnlCard.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            int top = 16;

            // Tiêu đề tiếng Việt
            var lblTieuDe = new Label { Text = "Tiêu đề tiếng Việt (*):", Font = UITheme.FontSmallBold, ForeColor = UITheme.TextPrimary, Location = new Point(24, top), AutoSize = true };
            txtTieuDe = new TextBox { Font = UITheme.FontBody, Location = new Point(24, top + 22), Size = new Size(568, 28) };
            top += 58;

            // Tiêu đề tiếng Anh
            var lblTieuDeEn = new Label { Text = "English Title:", Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary, Location = new Point(24, top), AutoSize = true };
            txtTieuDeEn = new TextBox { Font = UITheme.FontBody, Location = new Point(24, top + 22), Size = new Size(568, 28) };
            top += 58;

            // Chuyên ngành & Tác giả
            var lblChuyenNganh = new Label { Text = "Chuyên ngành nghiên cứu (*):", Font = UITheme.FontSmallBold, ForeColor = UITheme.TextPrimary, Location = new Point(24, top), AutoSize = true };
            cboChuyenNganh = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontBody, Location = new Point(24, top + 22), Size = new Size(270, 28) };

            var lblTacGia = new Label { Text = "Tác giả chính gửi bài (*):", Font = UITheme.FontSmallBold, ForeColor = UITheme.TextPrimary, Location = new Point(314, top), AutoSize = true };
            cboTacGia = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontBody, Location = new Point(314, top + 22), Size = new Size(278, 28), Enabled = !_isEditMode };
            top += 58;

            // Từ khóa
            var lblTuKhoa = new Label { Text = "Từ khóa (cách nhau bởi dấu phẩy):", Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary, Location = new Point(24, top), AutoSize = true };
            txtTuKhoa = new TextBox { Font = UITheme.FontBody, Location = new Point(24, top + 22), Size = new Size(568, 28), PlaceholderText = "Ví dụ: AI, Deep Learning, Transformer, NLP" };
            top += 58;

            // Tóm tắt (Abstract)
            var lblTomTat = new Label { Text = "Tóm tắt bản thảo (Abstract) (*):", Font = UITheme.FontSmallBold, ForeColor = UITheme.TextPrimary, Location = new Point(24, top), AutoSize = true };
            txtTomTat = new TextBox
            {
                Font = UITheme.FontBody,
                Location = new Point(24, top + 22),
                Size = new Size(568, 175),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            pnlCard.Controls.AddRange(new Control[]
            {
                lblTieuDe, txtTieuDe,
                lblTieuDeEn, txtTieuDeEn,
                lblChuyenNganh, cboChuyenNganh,
                lblTacGia, cboTacGia,
                lblTuKhoa, txtTuKhoa,
                lblTomTat, txtTomTat
            });

            // 3. Footer Action Buttons
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.White
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            btnSave = new ModernButton
            {
                Text = _isEditMode ? "Lưu thay đổi" : "Tiếp nhận bản thảo",
                Size = new Size(180, 42),
                Location = new Point(460, 12),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryDark,
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Size = new Size(100, 42),
                Location = new Point(348, 12),
                FlatStyle = FlatStyle.Flat,
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextSecondary,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = UITheme.BorderColor;

            pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnSave });

            Controls.AddRange(new Control[] { pnlCard, pnlFooter, pnlHeader });
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadData()
        {
            // Load chuyên ngành
            var chuyenNganhs = ChuyenNganhService.GetAllChuyenNganh();
            cboChuyenNganh.DisplayMember = "TenChuyenNganh";
            cboChuyenNganh.ValueMember = "MaChuyenNganh";
            cboChuyenNganh.DataSource = chuyenNganhs;

            // Load tác giả (người dùng)
            var users = NguoiDungService.GetAllUsers();
            cboTacGia.DisplayMember = "HoTen";
            cboTacGia.ValueMember = "MaNguoiDung";
            cboTacGia.DataSource = users;

            if (_isEditMode)
            {
                var b = BaiBaoService.GetBaiBaoById(_maBaiBao);
                if (b != null)
                {
                    txtTieuDe.Text = b.TieuDe;
                    txtTieuDeEn.Text = b.TieuDeTiengAnh ?? "";
                    txtTomTat.Text = b.TomTat ?? "";
                    txtTuKhoa.Text = b.TuKhoa ?? "";
                    cboChuyenNganh.SelectedValue = b.MaChuyenNganh;
                    cboTacGia.SelectedValue = b.MaNguoiDung;
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTieuDe.Text))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề tiếng Việt của bản thảo.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTieuDe.Focus();
                return;
            }

            if (cboChuyenNganh.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn chuyên ngành nghiên cứu phù hợp.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int maChuyenNganh = (int)cboChuyenNganh.SelectedValue;

            if (_isEditMode)
            {
                bool ok = BaiBaoService.CapNhatThongTinBaiBao(_maBaiBao, txtTieuDe.Text.Trim(), txtTieuDeEn.Text.Trim(), txtTomTat.Text.Trim(), txtTuKhoa.Text.Trim(), maChuyenNganh);
                if (ok)
                {
                    MessageBox.Show("Cập nhật thông tin bản thảo thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi lưu thông tin bản thảo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (cboTacGia.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn tác giả chính nộp bài.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int maTacGia = (int)cboTacGia.SelectedValue;

                bool ok = BaiBaoService.ThemBaiBao(txtTieuDe.Text.Trim(), txtTieuDeEn.Text.Trim(), txtTomTat.Text.Trim(), txtTuKhoa.Text.Trim(), maChuyenNganh, maTacGia, out int newId);
                if (ok)
                {
                    MessageBox.Show($"Tiếp nhận bản thảo thành công!\nMã bản thảo được cấp: JST-{newId:D4}\nTrạng thái: Chờ sơ duyệt", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi tiếp nhận bản thảo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
