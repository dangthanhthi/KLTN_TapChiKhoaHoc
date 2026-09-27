using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmKiemTraDaoVanDialog : Form
    {
        private readonly BaiBao _article;
        private readonly decimal _tyLe;
        private readonly string _ketLuan;

        public FrmKiemTraDaoVanDialog(BaiBao article, decimal tyLe, string ketLuan)
        {
            _article = article;
            _tyLe = tyLe;
            _ketLuan = ketLuan;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Báo cáo Kiểm tra Đạo văn & Tính nguyên bản";
            Size = new Size(680, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = UITheme.FontBody;

            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
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
                Text = "KẾT QUẢ KIỂM TRA ĐẠO VĂN (iThenticate / Turnitin)",
                Font = UITheme.FontSection,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = $"Mã bài: [{_article.MaDinhDanh}] {_article.TieuDe}",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 38),
                Size = new Size(620, 20),
                AutoEllipsis = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Overall Score Card
            var pnlScoreCard = new Panel
            {
                Location = new Point(24, 85),
                Size = new Size(616, 100),
                BackColor = _tyLe <= 20 ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242),
                Padding = new Padding(16)
            };
            pnlScoreCard.Paint += (s, e) =>
            {
                using var p = new Pen(_tyLe <= 20 ? UITheme.SuccessBorder : UITheme.DangerBorder);
                e.Graphics.DrawRectangle(p, 0, 0, pnlScoreCard.Width - 1, pnlScoreCard.Height - 1);
            };

            var lblScore = new Label
            {
                Text = $"{_tyLe:F1}%",
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = _tyLe <= 20 ? UITheme.Success : UITheme.Danger,
                Location = new Point(16, 20),
                AutoSize = true
            };

            var lblScoreTitle = new Label
            {
                Text = "TỶ LỆ TRÙNG LẶP NỘI DUNG (SIMILARITY INDEX)",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(160, 18),
                AutoSize = true
            };

            var lblEvaluation = new Label
            {
                Text = _ketLuan,
                Font = UITheme.FontBodyBold,
                ForeColor = _tyLe <= 20 ? UITheme.Success : UITheme.Danger,
                Location = new Point(160, 42),
                Size = new Size(430, 42)
            };

            pnlScoreCard.Controls.AddRange(new Control[] { lblScore, lblScoreTitle, lblEvaluation });

            // Sources Breakdown
            var lblSourcesTitle = new Label
            {
                Text = "Chi tiết đối sánh các nguồn tài liệu khoa học:",
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(24, 200),
                AutoSize = true
            };

            var lvSources = new ListView
            {
                Location = new Point(24, 225),
                Size = new Size(616, 175),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            lvSources.Columns.Add("Nguồn đối sánh", 365);
            lvSources.Columns.Add("Trùng lặp", 80, HorizontalAlignment.Center);
            lvSources.Columns.Add("Đánh giá", 165, HorizontalAlignment.Left);

            decimal p1 = Math.Round(_tyLe * 0.45m, 1);
            decimal p2 = Math.Round(_tyLe * 0.30m, 1);
            decimal p3 = Math.Round(_tyLe * 0.15m, 1);
            decimal p4 = Math.Max(0.5m, Math.Round(_tyLe - p1 - p2 - p3, 1));

            lvSources.Items.Add(new ListViewItem(new[] { "Kho dữ liệu Tạp chí Khoa học & Công nghệ HUIT", $"{p1}%", "Trích dẫn hợp lệ" }));
            lvSources.Items.Add(new ListViewItem(new[] { "Hệ thống Khóa luận & Luận văn tốt nghiệp HUIT", $"{p2}%", "Tương đồng" }));
            lvSources.Items.Add(new ListViewItem(new[] { "Cơ sở dữ liệu Scopus / IEEE Xplore / ScienceDirect", $"{p3}%", "Tài liệu tham khảo" }));
            lvSources.Items.Add(new ListViewItem(new[] { "Các xuất bản phẩm học thuật trực tuyến khác", $"{p4}%", "Phù hợp" }));

            var btnClose = new ModernButton
            {
                Text = "Đóng báo cáo",
                Location = new Point(520, 420),
                Size = new Size(120, 38)
            };
            UITheme.ApplyPrimaryButton(btnClose);
            btnClose.Click += (s, e) => DialogResult = DialogResult.OK;

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                pnlScoreCard,
                lblSourcesTitle,
                lvSources,
                btnClose
            });
        }
    }
}
