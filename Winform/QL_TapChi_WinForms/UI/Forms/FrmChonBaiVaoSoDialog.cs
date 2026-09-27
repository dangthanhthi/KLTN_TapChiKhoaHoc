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
    public class FrmChonBaiVaoSoDialog : Form
    {
        private readonly SoTapChi _issue;
        private DataGridView dgvArticles = null!;
        private TextBox txtSearch = null!;
        private NumericUpDown numStartPage = null!;
        private NumericUpDown numEndPage = null!;
        private TextBox txtDoi = null!;
        private ModernButton btnConfirm = null!;
        private ModernButton btnCancel = null!;
        private Label lblSelectedArticleInfo = null!;
        private List<BaiBao> _availableArticles = new();

        public FrmChonBaiVaoSoDialog(SoTapChi issue)
        {
            _issue = issue;
            InitializeComponent();
            LoadAvailableArticles();
        }

        private void InitializeComponent()
        {
            Text = "Chọn bài báo đưa vào Số tạp chí";
            Size = new Size(950, 620);
            MinimumSize = new Size(850, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = UITheme.FontBody;

            // 1. Header Banner
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
                Text = "ĐƯA BÀI BÁO VÀO SỐ TẠP CHÍ",
                Font = UITheme.FontSection,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = $"Đang chọn cho: {_issue.TenSo}  •  Tập {_issue.Tap}, Số {_issue.So} ({_issue.Nam})",
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 38),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // 2. Search & Toolbar
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 10, 24, 10)
            };

            var lblSearch = new Label
            {
                Text = "Tìm bài báo:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 15),
                AutoSize = true
            };

            txtSearch = new TextBox
            {
                Location = new Point(110, 11),
                Size = new Size(350, 28),
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Nhập mã bài, tiêu đề hoặc tác giả để tìm nhanh..."
            };
            txtSearch.TextChanged += (s, e) => FilterArticles();

            var lblHint = new Label
            {
                Text = "💡 Chỉ hiển thị các bài chưa gán số (ưu tiên bài Đang chế bản / Chấp nhận đăng)",
                Font = UITheme.FontSmall,
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(480, 15),
                AutoSize = true
            };

            pnlSearch.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblHint });

            // 3. Bottom Configuration Panel (Page setup & DOI)
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 145,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 14, 24, 14)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawLine(p, 0, 0, pnlBottom.Width, 0);
            };

            lblSelectedArticleInfo = new Label
            {
                Text = "Vui lòng chọn 1 bài báo từ danh sách phía trên...",
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(24, 12),
                Size = new Size(880, 24)
            };

            var lblStart = new Label { Text = "Trang bắt đầu:", Font = UITheme.FontSmallBold, Location = new Point(24, 45), AutoSize = true };
            numStartPage = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = 1,
                Location = new Point(24, 68),
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9.5f)
            };
            numStartPage.ValueChanged += (s, e) =>
            {
                if (numEndPage.Value < numStartPage.Value)
                {
                    numEndPage.Value = numStartPage.Value + 8; // Mặc định 8-10 trang cho 1 bài báo khoa học
                }
            };

            var lblEnd = new Label { Text = "Trang kết thúc:", Font = UITheme.FontSmallBold, Location = new Point(155, 45), AutoSize = true };
            numEndPage = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = 10,
                Location = new Point(155, 68),
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblDoi = new Label { Text = "Mã số DOI:", Font = UITheme.FontSmallBold, Location = new Point(285, 45), AutoSize = true };
            txtDoi = new TextBox
            {
                Location = new Point(285, 68),
                Size = new Size(330, 28),
                Font = new Font("Segoe UI", 9.5f),
                Text = $"10.59876/jst.{_issue.Nam}.{_issue.So}.01"
            };

            btnConfirm = new ModernButton
            {
                Text = "Đưa bài vào số",
                Size = new Size(160, 38),
                Location = new Point(635, 63)
            };
            UITheme.ApplyPrimaryButton(btnConfirm);
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new ModernButton
            {
                Text = "Đóng",
                Size = new Size(80, 38),
                Location = new Point(845, 63)
            };
            UITheme.ApplySecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            pnlBottom.Controls.AddRange(new Control[]
            {
                lblSelectedArticleInfo,
                lblStart, numStartPage,
                lblEnd, numEndPage,
                lblDoi, txtDoi,
                btnConfirm, btnCancel
            });

            // 4. Center Grid (Available Articles)
            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 10, 24, 10),
                BackColor = Color.White
            };

            dgvArticles = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyModernGridStyle(dgvArticles);
            SetupGridColumns();
            dgvArticles.SelectionChanged += DgvArticles_SelectionChanged;

            pnlGridContainer.Controls.Add(dgvArticles);

            Controls.Add(pnlGridContainer);
            Controls.Add(pnlBottom);
            Controls.Add(pnlSearch);
            Controls.Add(pnlHeader);
        }

        private void SetupGridColumns()
        {
            dgvArticles.Columns.Clear();
            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaDinhDanh",
                HeaderText = "Mã bài",
                Width = 95,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = UITheme.PrimaryDark, Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TieuDe",
                HeaderText = "Tiêu đề bài báo khoa học",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 260,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TacGia",
                HeaderText = "Tác giả chính",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ChuyenNganh",
                HeaderText = "Chuyên ngành",
                Width = 160,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvArticles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = "Trạng thái",
                Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
        }

        private void LoadAvailableArticles()
        {
            _availableArticles = BaiBaoService.GetBaiBaoChuaGanSo();
            FilterArticles();
        }

        private void FilterArticles()
        {
            string kw = txtSearch.Text.Trim().ToLowerInvariant();
            dgvArticles.Rows.Clear();

            var filtered = _availableArticles;
            if (!string.IsNullOrEmpty(kw))
            {
                filtered = _availableArticles.FindAll(b =>
                    b.MaDinhDanh.ToLowerInvariant().Contains(kw) ||
                    b.TieuDe.ToLowerInvariant().Contains(kw) ||
                    (b.TenTacGia?.ToLowerInvariant().Contains(kw) ?? false) ||
                    (b.TenChuyenNganh?.ToLowerInvariant().Contains(kw) ?? false));
            }

            foreach (var b in filtered)
            {
                int idx = dgvArticles.Rows.Add(
                    b.MaDinhDanh,
                    b.TieuDe,
                    b.TenTacGia,
                    b.TenChuyenNganh,
                    b.TrangThai
                );
                dgvArticles.Rows[idx].Tag = b;
            }

            if (dgvArticles.Rows.Count > 0)
            {
                dgvArticles.Rows[0].Selected = true;
                DgvArticles_SelectionChanged(null, EventArgs.Empty);
            }
            else
            {
                lblSelectedArticleInfo.Text = "Không tìm thấy bài báo nào chưa gán số phù hợp điều kiện.";
                btnConfirm.Enabled = false;
            }
        }

        private void DgvArticles_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvArticles.CurrentRow?.Tag is BaiBao b)
            {
                lblSelectedArticleInfo.Text = $"Đang chọn: [{b.MaDinhDanh}] {b.TieuDe}  (Tác giả: {b.TenTacGia ?? "N/A"})";
                btnConfirm.Enabled = true;

                // Tự động sinh DOI chuẩn quốc tế theo quy chuẩn tạp chí
                txtDoi.Text = $"10.59876/jst.{_issue.Nam}.{_issue.So}.{b.MaBaiBao:D2}";
            }
            else
            {
                lblSelectedArticleInfo.Text = "Vui lòng chọn 1 bài báo từ danh sách phía trên...";
                btnConfirm.Enabled = false;
            }
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            if (dgvArticles.CurrentRow?.Tag is not BaiBao b)
            {
                MessageBox.Show("Vui lòng chọn một bài báo từ danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int start = (int)numStartPage.Value;
            int end = (int)numEndPage.Value;
            if (end < start)
            {
                MessageBox.Show("Trang kết thúc phải lớn hơn hoặc bằng trang bắt đầu!", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string doi = txtDoi.Text.Trim();

            var dr = MessageBox.Show(
                $"Xác nhận đưa bài báo [{b.MaDinhDanh}] vào số tạp chí:\n\n" +
                $"• Số tạp chí: {_issue.TenSo}\n" +
                $"• Tập {_issue.Tap}, Số {_issue.So} ({_issue.Nam})\n" +
                $"• Khoảng trang: {start} - {end}\n" +
                $"• Mã DOI: {doi}\n\n" +
                "Bài báo sẽ được gán số xuất bản thành công!",
                "Xác nhận đưa bài vào số",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                bool ok = BaiBaoService.GanSoTapChi(b.MaBaiBao, _issue.MaSoTapChi, start, end, doi);
                if (ok)
                {
                    MessageBox.Show($"Đã đưa bài báo [{b.MaDinhDanh}] vào số tạp chí thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi lưu thông tin gán bài vào số.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
