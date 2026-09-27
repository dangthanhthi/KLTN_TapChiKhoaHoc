using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Views
{
    public class UcThongKeBaoCao : UserControl
    {
        private Label lblTitle = null!;
        private Label lblStatusTitle = null!;
        private Label lblCatTitle = null!;
        private DataGridView dgvStatus = null!;
        private DataGridView dgvCategory = null!;
        private ModernButton btnExportArticles = null!;
        private ModernButton btnExportSummary = null!;

        public UcThongKeBaoCao()
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

        private void ApplyLanguage()
        {
            lblTitle.Text = LanguageService.Get("Reports_Title");
            btnExportArticles.Text = LanguageService.Get("Reports_BtnExportArticles");
            btnExportSummary.Text = LanguageService.Get("Reports_BtnExportSummary");
            lblStatusTitle.Text = LanguageService.Get("Reports_SectionStatus");
            lblCatTitle.Text = LanguageService.Get("Reports_SectionField");

            if (dgvStatus.Columns.Contains("TrangThai"))
                dgvStatus.Columns["TrangThai"].HeaderText = LanguageService.Get("Col_StageName");
            if (dgvStatus.Columns.Contains("SoLuong"))
                dgvStatus.Columns["SoLuong"].HeaderText = LanguageService.Get("Col_Quantity");

            if (dgvCategory.Columns.Contains("ChuyenNganh"))
                dgvCategory.Columns["ChuyenNganh"].HeaderText = LanguageService.Get("Col_Field");
            if (dgvCategory.Columns.Contains("SoLuong"))
                dgvCategory.Columns["SoLuong"].HeaderText = LanguageService.Get("Col_Quantity");

            LoadData();
        }

        private void InitializeComponent()
        {
            BackColor = UITheme.AppBackground;
            Dock = DockStyle.Fill;
            Padding = new Padding(24);

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
                Text = LanguageService.Get("Reports_Title"),
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

            btnExportArticles = new ModernButton
            {
                Text = "Xuất DS Bài báo (CSV/Excel)",
                Size = new Size(225, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.ApplyPrimaryButton(btnExportArticles);
            btnExportArticles.Click += BtnExportArticles_Click;

            btnExportSummary = new ModernButton
            {
                Text = "Xuất Thống kê Tổng hợp",
                Size = new Size(205, 36),
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplySecondaryButton(btnExportSummary);
            btnExportSummary.Click += BtnExportSummary_Click;

            flpActions.Controls.Add(btnExportArticles);
            flpActions.Controls.Add(btnExportSummary);

            pnlToolbar.Controls.Add(flpActions);
            pnlToolbar.Controls.Add(pnlTitleRow);

            // 2 Column Panels
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            split.SizeChanged += (s, e) =>
            {
                if (split.Width > 500)
                {
                    try
                    {
                        split.SplitterDistance = split.Width / 2;
                    }
                    catch { }
                }
            };

            // Left: Status statistics
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            pnlLeft.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlLeft.Width - 1, pnlLeft.Height - 1);
            };

            lblStatusTitle = new Label
            {
                Text = LanguageService.Get("Reports_SectionStatus"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.PrimaryDark,
                Dock = DockStyle.Top,
                Height = 36,
                UseMnemonic = false
            };

            dgvStatus = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyCompactGridStyle(dgvStatus, 42);
            dgvStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = LanguageService.Get("Col_StageName"), MinimumWidth = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvStatus.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoLuong", HeaderText = LanguageService.Get("Col_Quantity"), Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Font = UITheme.FontBodyBold, Alignment = DataGridViewContentAlignment.MiddleCenter } });

            pnlLeft.Controls.Add(dgvStatus);
            pnlLeft.Controls.Add(lblStatusTitle);
            split.Panel1.Controls.Add(pnlLeft);

            // Right: Category statistics
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            pnlRight.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlRight.Width - 1, pnlRight.Height - 1);
            };

            lblCatTitle = new Label
            {
                Text = LanguageService.Get("Reports_SectionField"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.PrimaryDark,
                Dock = DockStyle.Top,
                Height = 36,
                UseMnemonic = false
            };

            dgvCategory = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyCompactGridStyle(dgvCategory, 42);
            dgvCategory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChuyenNganh", HeaderText = LanguageService.Get("Col_Field"), MinimumWidth = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvCategory.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoLuong", HeaderText = LanguageService.Get("Col_Quantity"), Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Font = UITheme.FontBodyBold, Alignment = DataGridViewContentAlignment.MiddleCenter } });

            pnlRight.Controls.Add(dgvCategory);
            pnlRight.Controls.Add(lblCatTitle);
            split.Panel2.Controls.Add(pnlRight);

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

            Controls.Add(split);
            Controls.Add(pnlSpacer);
            Controls.Add(pnlToolbar);
        }

        public void LoadData()
        {
            var stDict = ThongKeService.GetCountByTrangThai();
            dgvStatus.Rows.Clear();
            foreach (var kvp in stDict)
            {
                dgvStatus.Rows.Add(LanguageService.TranslateStatus(kvp.Key), kvp.Value);
            }

            var catDict = ThongKeService.GetCountByChuyenNganh();
            dgvCategory.Rows.Clear();
            foreach (var kvp in catDict)
            {
                dgvCategory.Rows.Add(kvp.Key, kvp.Value);
            }
        }

        private void BtnExportArticles_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Tệp CSV (*.csv)|*.csv",
                FileName = $"BaoCao_DanhSachBaiBao_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var dt = new DataTable();
                dt.Columns.Add("Mã bản thảo");
                dt.Columns.Add("Tiêu đề bài báo");
                dt.Columns.Add("Tác giả chính");
                dt.Columns.Add("Chuyên ngành");
                dt.Columns.Add("Trạng thái tiến độ");
                dt.Columns.Add("Số tạp chí");
                dt.Columns.Add("Mã DOI");
                dt.Columns.Add("Ngày gửi");

                var list = BaiBaoService.GetAllBaiBao();
                foreach (var b in list)
                {
                    dt.Rows.Add(
                        b.MaDinhDanh,
                        b.TieuDe,
                        b.TenTacGia,
                        b.TenChuyenNganh,
                        b.TrangThai,
                        b.TenSoTapChi ?? "Chưa gán",
                        b.MaDOI ?? "",
                        b.NgayGui.ToString("dd/MM/yyyy")
                    );
                }

                bool ok = ThongKeService.ExportToCsv(sfd.FileName, dt, "Báo cáo Danh sách Bản thảo & Tiến độ Thẩm định Tạp chí Khoa học");
                if (ok)
                {
                    MessageBox.Show($"Xuất báo cáo thành công vào tệp:\n{sfd.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi xuất báo cáo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnExportSummary_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Tệp CSV (*.csv)|*.csv",
                FileName = $"BaoCao_ThongKe_TongHop_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var dt = new DataTable();
                dt.Columns.Add("Hạng mục thống kê");
                dt.Columns.Add("Phân loại");
                dt.Columns.Add("Số lượng bài báo");

                var stDict = ThongKeService.GetCountByTrangThai();
                foreach (var kvp in stDict)
                {
                    dt.Rows.Add("Theo tiến độ", kvp.Key, kvp.Value);
                }

                var catDict = ThongKeService.GetCountByChuyenNganh();
                foreach (var kvp in catDict)
                {
                    dt.Rows.Add("Theo chuyên ngành", kvp.Key, kvp.Value);
                }

                bool ok = ThongKeService.ExportToCsv(sfd.FileName, dt, "Báo cáo Thống kê Tổng hợp Hoạt động Xuất bản Tạp chí Khoa học");
                if (ok)
                {
                    MessageBox.Show($"Xuất báo cáo thống kê tổng hợp thành công:\n{sfd.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
