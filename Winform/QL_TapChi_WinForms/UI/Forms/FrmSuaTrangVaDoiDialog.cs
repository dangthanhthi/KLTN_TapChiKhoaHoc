using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmSuaTrangVaDoiDialog : Form
    {
        private readonly BaiBao _article;
        private NumericUpDown numStartPage = null!;
        private NumericUpDown numEndPage = null!;
        private TextBox txtDoi = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public FrmSuaTrangVaDoiDialog(BaiBao article)
        {
            _article = article;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Chỉnh sửa số trang & mã DOI";
            Size = new Size(520, 360);
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
                Text = "CẬP NHẬT TRANG & MÃ DOI XUẤT BẢN",
                Font = UITheme.FontSection,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = $"Bài báo: [{_article.MaDinhDanh}] {_article.TieuDe}",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 38),
                Size = new Size(460, 20),
                AutoEllipsis = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Fields
            var lblStart = new Label { Text = "Trang bắt đầu:", Font = UITheme.FontBodyBold, Location = new Point(24, 85), AutoSize = true };
            numStartPage = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = _article.TrangBatDau.HasValue ? _article.TrangBatDau.Value : 1,
                Location = new Point(24, 110),
                Size = new Size(210, 28),
                Font = new Font("Segoe UI", 10f)
            };

            var lblEnd = new Label { Text = "Trang kết thúc:", Font = UITheme.FontBodyBold, Location = new Point(265, 85), AutoSize = true };
            numEndPage = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = _article.TrangKetThuc.HasValue ? _article.TrangKetThuc.Value : 10,
                Location = new Point(265, 110),
                Size = new Size(210, 28),
                Font = new Font("Segoe UI", 10f)
            };

            var lblDoi = new Label { Text = "Mã số định danh DOI:", Font = UITheme.FontBodyBold, Location = new Point(24, 160), AutoSize = true };
            txtDoi = new TextBox
            {
                Location = new Point(24, 185),
                Size = new Size(450, 28),
                Font = new Font("Segoe UI", 10f),
                Text = _article.MaDOI ?? ""
            };

            // Buttons
            btnSave = new ModernButton
            {
                Text = "Lưu thay đổi",
                Location = new Point(255, 255),
                Size = new Size(130, 38)
            };
            UITheme.ApplyPrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnCancel = new ModernButton
            {
                Text = "Hủy",
                Location = new Point(395, 255),
                Size = new Size(80, 38)
            };
            UITheme.ApplySecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                lblStart, numStartPage,
                lblEnd, numEndPage,
                lblDoi, txtDoi,
                btnSave, btnCancel
            });
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            int start = (int)numStartPage.Value;
            int end = (int)numEndPage.Value;
            if (end < start)
            {
                MessageBox.Show("Trang kết thúc phải lớn hơn hoặc bằng trang bắt đầu!", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string doi = txtDoi.Text.Trim();
            bool ok = BaiBaoService.CapNhatTrangVaDOI(_article.MaBaiBao, start, end, string.IsNullOrEmpty(doi) ? null : doi, out string? err);
            if (ok)
            {
                MessageBox.Show("Đã cập nhật số trang và mã DOI thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(err ?? "Không thể cập nhật thông tin bài báo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
