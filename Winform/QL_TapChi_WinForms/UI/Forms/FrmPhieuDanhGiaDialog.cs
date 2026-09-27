using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmPhieuDanhGiaDialog : Form
    {
        private readonly int _maPhanCong;
        private PhieuDanhGia? _phieu;

        private NumericUpDown numTinhMoi = null!;
        private NumericUpDown numPhuongPhap = null!;
        private NumericUpDown numKetQua = null!;
        private NumericUpDown numTrinhBay = null!;
        private Label lblTongKet = null!;
        private TextBox txtNhanXetTacGia = null!;
        private TextBox txtNhanXetBaoMat = null!;
        private ComboBox cboKienNghi = null!;
        private ModernButton btnSave = null!;

        public FrmPhieuDanhGiaDialog(int maPhanCong)
        {
            _maPhanCong = maPhanCong;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Phiếu đánh giá bài báo chuyên môn · JST";
            Size = new Size(680, 680);
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
                Text = "PHIẾU ĐÁNH GIÁ CỦA CHUYÊN GIA PHẢN BIỆN",
                Font = UITheme.FontSection,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Thang điểm 10 theo chuẩn kiểm định học thuật Tạp chí Khoa học & Công nghệ",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 36),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // 4 Criteria Scores Panel
            var pnlScores = new Panel
            {
                Location = new Point(24, 80),
                Size = new Size(615, 110),
                BackColor = UITheme.Sky050
            };
            pnlScores.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.Sky200);
                e.Graphics.DrawRectangle(p, 0, 0, pnlScores.Width - 1, pnlScores.Height - 1);
            };

            // Criteria 1: Tính mới
            var lbl1 = new Label { Text = "1. Tính mới (0-10):", Font = UITheme.FontSmallBold, Location = new Point(16, 14), AutoSize = true };
            numTinhMoi = CreateScoreBox(new Point(16, 36));

            // Criteria 2: Phương pháp
            var lbl2 = new Label { Text = "2. Phương pháp (0-10):", Font = UITheme.FontSmallBold, Location = new Point(160, 14), AutoSize = true };
            numPhuongPhap = CreateScoreBox(new Point(160, 36));

            // Criteria 3: Kết quả
            var lbl3 = new Label { Text = "3. Kết quả (0-10):", Font = UITheme.FontSmallBold, Location = new Point(310, 14), AutoSize = true };
            numKetQua = CreateScoreBox(new Point(310, 36));

            // Criteria 4: Trình bày
            var lbl4 = new Label { Text = "4. Trình bày (0-10):", Font = UITheme.FontSmallBold, Location = new Point(450, 14), AutoSize = true };
            numTrinhBay = CreateScoreBox(new Point(450, 36));

            lblTongKet = new Label
            {
                Text = "ĐIỂM TRUNG BÌNH TỔNG KẾT: 0.0 / 10.0",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(16, 76),
                AutoSize = true
            };

            numTinhMoi.ValueChanged += (s, e) => UpdateAvgScore();
            numPhuongPhap.ValueChanged += (s, e) => UpdateAvgScore();
            numKetQua.ValueChanged += (s, e) => UpdateAvgScore();
            numTrinhBay.ValueChanged += (s, e) => UpdateAvgScore();

            pnlScores.Controls.AddRange(new Control[] { lbl1, numTinhMoi, lbl2, numPhuongPhap, lbl3, numKetQua, lbl4, numTrinhBay, lblTongKet });

            // Comments to Author
            var lblCommentTg = new Label
            {
                Text = "Nhận xét chi tiết gửi Tác giả (góp ý chuyên môn):",
                Font = UITheme.FontBodyBold,
                Location = new Point(24, 205),
                AutoSize = true
            };

            txtNhanXetTacGia = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(24, 230),
                Size = new Size(615, 120)
            };

            // Confidential Comments to Editor
            var lblCommentBm = new Label
            {
                Text = "Nhận xét bảo mật cho Ban biên tập (không gửi tác giả):",
                Font = UITheme.FontBodyBold,
                Location = new Point(24, 360),
                AutoSize = true
            };

            txtNhanXetBaoMat = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(24, 385),
                Size = new Size(615, 80)
            };

            // Recommendation
            var lblKienNghi = new Label
            {
                Text = "Kiến nghị cuối cùng của Phản biện:",
                Font = UITheme.FontBodyBold,
                Location = new Point(24, 480),
                AutoSize = true
            };

            cboKienNghi = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(24, 505),
                Size = new Size(615, 30)
            };
            cboKienNghi.Items.AddRange(new object[]
            {
                "Chấp nhận đăng",
                "Chỉnh sửa nhỏ",
                "Chỉnh sửa lớn và phản biện lại",
                "Từ chối đăng"
            });
            cboKienNghi.SelectedIndex = 0;

            btnSave = new ModernButton
            {
                Text = "Lưu phiếu đánh giá",
                Size = new Size(180, 42),
                Location = new Point(459, 565),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryDark
            };
            btnSave.Click += BtnSave_Click;

            var btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 42),
                Location = new Point(310, 565),
                FlatStyle = FlatStyle.Flat,
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextSecondary
            };
            btnClose.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                pnlScores,
                lblCommentTg, txtNhanXetTacGia,
                lblCommentBm, txtNhanXetBaoMat,
                lblKienNghi, cboKienNghi,
                btnSave, btnClose
            });
        }

        private NumericUpDown CreateScoreBox(Point pt)
        {
            return new NumericUpDown
            {
                DecimalPlaces = 1,
                Increment = 0.5m,
                Minimum = 0,
                Maximum = 10,
                Value = 8.0m,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = pt,
                Size = new Size(110, 30)
            };
        }

        private void UpdateAvgScore()
        {
            decimal avg = (numTinhMoi.Value + numPhuongPhap.Value + numKetQua.Value + numTrinhBay.Value) / 4m;
            lblTongKet.Text = $"ĐIỂM TRUNG BÌNH TỔNG KẾT: {avg:F1} / 10.0";
        }

        private void LoadData()
        {
            _phieu = PhanBienService.GetPhieuDanhGiaByPhanCong(_maPhanCong);
            if (_phieu != null)
            {
                if (_phieu.DiemTinhMoi.HasValue) numTinhMoi.Value = _phieu.DiemTinhMoi.Value;
                if (_phieu.DiemPhuongPhap.HasValue) numPhuongPhap.Value = _phieu.DiemPhuongPhap.Value;
                if (_phieu.DiemKetQua.HasValue) numKetQua.Value = _phieu.DiemKetQua.Value;
                if (_phieu.DiemTrinhBay.HasValue) numTrinhBay.Value = _phieu.DiemTrinhBay.Value;
                txtNhanXetTacGia.Text = _phieu.NhanXetChoTacGia ?? "";
                txtNhanXetBaoMat.Text = _phieu.NhanXetBaoMat ?? "";
                if (!string.IsNullOrEmpty(_phieu.KienNghi))
                {
                    int idx = cboKienNghi.FindStringExact(_phieu.KienNghi);
                    if (idx >= 0) cboKienNghi.SelectedIndex = idx;
                }
                UpdateAvgScore();
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string kn = cboKienNghi.SelectedItem?.ToString() ?? "Chấp nhận đăng";
            bool ok = PhanBienService.LuuPhieuDanhGia(
                _maPhanCong,
                numTinhMoi.Value,
                numPhuongPhap.Value,
                numKetQua.Value,
                numTrinhBay.Value,
                txtNhanXetTacGia.Text.Trim(),
                txtNhanXetBaoMat.Text.Trim(),
                kn);

            if (ok)
            {
                MessageBox.Show("Đã lưu kết quả phiếu đánh giá thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(JournalApiClient.LastError ?? "Phiếu BM-04 phải do chuyên gia được phân công nộp bằng tài khoản của chính mình trên Web.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
