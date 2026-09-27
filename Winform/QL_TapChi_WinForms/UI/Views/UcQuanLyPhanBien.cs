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
    public class UcQuanLyPhanBien : UserControl
    {
        private Label lblTitle = null!;
        private Label lblFilter = null!;
        private DataGridView dgvReviews = null!;
        private ComboBox cboStatus = null!;
        private ModernButton btnAssignNew = null!;
        private ModernButton btnInviteReviewer3 = null!;
        private ModernButton btnViewScore = null!;
        private ModernButton btnRemind = null!;
        private ModernButton btnExtend = null!;
        private ModernButton btnCancel = null!;
        private ModernButton btnRefresh = null!;

        public UcQuanLyPhanBien()
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

        private void InitializeComponent()
        {
            BackColor = UITheme.AppBackground;
            Dock = DockStyle.Fill;
            Padding = new Padding(24);

            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlToolbar.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlToolbar.Width - 1, pnlToolbar.Height - 1);
            };

            // Row 1: Title and Status Filter
            var pnlRow1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = LanguageService.Get("Reviews_Title"),
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnRefresh = new ModernButton
            {
                Text = LanguageService.Get("Refresh"),
                Size = new Size(72, 30),
                Padding = new Padding(4, 0, 4, 0),
                Margin = new Padding(0, 1, 0, 0)
            };
            UITheme.ApplySecondaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadData();

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(150, 28),
                Margin = new Padding(6, 2, 6, 0)
            };
            cboStatus.SelectedIndexChanged += (s, e) => LoadData();

            lblFilter = new Label
            {
                Text = LanguageService.Get("Reviews_FilterStatus"),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 7, 2, 0),
                UseMnemonic = false
            };

            var pnlFilterBox = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Height = 34,
                Width = 390,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                WrapContents = false
            };

            pnlFilterBox.Controls.Add(btnRefresh);
            pnlFilterBox.Controls.Add(cboStatus);
            pnlFilterBox.Controls.Add(lblFilter);

            pnlRow1.Controls.Add(lblTitle);
            pnlRow1.Controls.Add(pnlFilterBox);

            // Row 2: Action Toolbar (Full width, seamless layout)
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

            btnAssignNew = new ModernButton
            {
                Text = LanguageService.Get("Reviews_BtnAssign"),
                Size = new Size(130, 34),
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplyPrimaryButton(btnAssignNew);
            btnAssignNew.Click += BtnAssignNew_Click;

            btnInviteReviewer3 = new ModernButton
            {
                Text = LanguageService.Get("Reviews_BtnInvite3"),
                Size = new Size(100, 34),
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplySecondaryButton(btnInviteReviewer3);
            btnInviteReviewer3.Click += BtnInviteReviewer3_Click;

            btnViewScore = new ModernButton
            {
                Text = LanguageService.Get("Reviews_BtnScore"),
                Size = new Size(110, 34),
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplySecondaryButton(btnViewScore);
            btnViewScore.Click += BtnViewScore_Click;

            btnRemind = new ModernButton
            {
                Text = LanguageService.Get("Reviews_BtnRemind"),
                Size = new Size(85, 34),
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplySecondaryButton(btnRemind);
            btnRemind.Click += BtnRemind_Click;

            btnExtend = new ModernButton
            {
                Text = LanguageService.Get("Reviews_BtnExtend"),
                Size = new Size(80, 34),
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0, 0, 6, 0)
            };
            UITheme.ApplySecondaryButton(btnExtend);
            btnExtend.Click += BtnExtend_Click;

            btnCancel = new ModernButton
            {
                Text = LanguageService.Get("Reviews_BtnCancel"),
                Size = new Size(125, 34),
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplyDangerButton(btnCancel);
            btnCancel.Click += BtnCancel_Click;

            flpActions.Controls.Add(btnAssignNew);
            flpActions.Controls.Add(btnInviteReviewer3);
            flpActions.Controls.Add(btnViewScore);
            flpActions.Controls.Add(btnRemind);
            flpActions.Controls.Add(btnExtend);
            flpActions.Controls.Add(btnCancel);

            pnlToolbar.Controls.Add(flpActions);
            pnlToolbar.Controls.Add(pnlRow1);

            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlGrid.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlGrid.Width - 1, pnlGrid.Height - 1);
            };

            dgvReviews = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyModernGridStyle(dgvReviews);
            SetupGridColumns();

            dgvReviews.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) BtnViewScore_Click(s, e);
            };

            pnlGrid.Controls.Add(dgvReviews);

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

            Controls.Add(pnlGrid);
            Controls.Add(pnlSpacer);
            Controls.Add(pnlToolbar);
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = LanguageService.Get("Reviews_Title");
            lblFilter.Text = LanguageService.Get("Reviews_FilterStatus");

            int prevIdx = cboStatus.SelectedIndex;
            cboStatus.Items.Clear();
            cboStatus.Items.AddRange(new object[]
            {
                LanguageService.Get("Role_All"),
                LanguageService.TranslateStatus("Chờ phản hồi"),
                LanguageService.TranslateStatus("Đồng ý phản biện"),
                LanguageService.TranslateStatus("Đang đánh giá"),
                LanguageService.TranslateStatus("Đã đánh giá"),
                LanguageService.TranslateStatus("Từ chối phản biện"),
                LanguageService.TranslateStatus("Quá hạn")
            });
            cboStatus.SelectedIndex = (prevIdx >= 0 && prevIdx < cboStatus.Items.Count) ? prevIdx : 0;

            btnAssignNew.Text = LanguageService.Get("Reviews_BtnAssign");
            btnInviteReviewer3.Text = LanguageService.Get("Reviews_BtnInvite3");
            btnViewScore.Text = LanguageService.Get("Reviews_BtnScore");
            btnRemind.Text = LanguageService.Get("Reviews_BtnRemind");
            btnExtend.Text = LanguageService.Get("Reviews_BtnExtend");
            btnCancel.Text = LanguageService.Get("Reviews_BtnCancel");
            btnRefresh.Text = LanguageService.Get("Refresh");

            if (dgvReviews.Columns["MaPC"] != null) dgvReviews.Columns["MaPC"].HeaderText = LanguageService.Get("Col_ReviewCode");
            if (dgvReviews.Columns["BaiBao"] != null) dgvReviews.Columns["BaiBao"].HeaderText = LanguageService.Get("Col_ArticleTitle");
            if (dgvReviews.Columns["PhanBien"] != null) dgvReviews.Columns["PhanBien"].HeaderText = LanguageService.Get("Col_ReviewerName");
            if (dgvReviews.Columns["Vong"] != null) dgvReviews.Columns["Vong"].HeaderText = LanguageService.Get("Col_Round");
            if (dgvReviews.Columns["TrangThai"] != null) dgvReviews.Columns["TrangThai"].HeaderText = LanguageService.Get("Col_Stage");
            if (dgvReviews.Columns["HanNop"] != null) dgvReviews.Columns["HanNop"].HeaderText = LanguageService.Get("Col_Deadline");
            if (dgvReviews.Columns["Diem"] != null) dgvReviews.Columns["Diem"].HeaderText = LanguageService.Get("Col_Score");
            if (dgvReviews.Columns["KienNghi"] != null) dgvReviews.Columns["KienNghi"].HeaderText = LanguageService.Get("Col_Recommendation");

            LoadData();
        }

        private void SetupGridColumns()
        {
            dgvReviews.Columns.Clear();

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaPC",
                HeaderText = LanguageService.Get("Col_ReviewCode"),
                Width = 50,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BaiBao",
                HeaderText = LanguageService.Get("Col_ArticleTitle"),
                MinimumWidth = 140,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PhanBien",
                HeaderText = LanguageService.Get("Col_ReviewerName"),
                Width = 130,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Font = UITheme.FontBodyBold, WrapMode = DataGridViewTriState.True }
            });

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Vong",
                HeaderText = LanguageService.Get("Col_Round"),
                Width = 72,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UITheme.FontBodyBold }
            });

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = LanguageService.Get("Col_Stage"),
                Width = 95,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "HanNop",
                HeaderText = LanguageService.Get("Col_Deadline"),
                Width = 130,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Diem",
                HeaderText = LanguageService.Get("Col_Score"),
                Width = 65,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Font = UITheme.FontBodyBold, ForeColor = UITheme.PrimaryDark, Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReviews.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "KienNghi",
                HeaderText = LanguageService.Get("Col_Recommendation"),
                Width = 115,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });
        }

        public void LoadData()
        {
            string? tt = cboStatus.SelectedIndex switch
            {
                1 => "Chờ phản hồi",
                2 => "Đồng ý phản biện",
                3 => "Đang đánh giá",
                4 => "Đã đánh giá",
                5 => "Từ chối phản biện",
                6 => "Quá hạn",
                _ => null
            };

            var list = PhanBienService.GetAllPhanCong(tt);
            dgvReviews.Rows.Clear();

            string defaultKienNghi = LanguageService.CurrentLanguage == "vi" ? "Chưa có kiến nghị" : "Pending recommendation";

            foreach (var pc in list)
            {
                string diemStr = pc.DiemTongKet.HasValue ? pc.DiemTongKet.Value.ToString("F1") : "-";
                string displayStatus = LanguageService.TranslateStatus(pc.TrangThai);

                string hanNopDisplay = "-";
                if (pc.HanHoanThanh.HasValue)
                {
                    var daysLeft = (pc.HanHoanThanh.Value.Date - DateTime.Today).Days;
                    bool isEn = LanguageService.CurrentLanguage == "en";
                    if (pc.TrangThai == "Đã đánh giá")
                    {
                        hanNopDisplay = isEn ? $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Submitted)" : $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Đã nộp)";
                    }
                    else if (daysLeft < 0)
                    {
                        hanNopDisplay = isEn ? $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Overdue {Math.Abs(daysLeft)}d)" : $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Quá hạn {Math.Abs(daysLeft)} ngày)";
                    }
                    else if (daysLeft == 0)
                    {
                        hanNopDisplay = isEn ? $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Today)" : $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Hôm nay)";
                    }
                    else if (daysLeft <= 3)
                    {
                        hanNopDisplay = isEn ? $"{pc.HanHoanThanh.Value:dd/MM/yyyy} ({daysLeft}d left - Urgent)" : $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Còn {daysLeft} ngày - Gấp)";
                    }
                    else
                    {
                        hanNopDisplay = isEn ? $"{pc.HanHoanThanh.Value:dd/MM/yyyy} ({daysLeft}d left)" : $"{pc.HanHoanThanh.Value:dd/MM/yyyy} (Còn {daysLeft} ngày)";
                    }
                }

                int idx = dgvReviews.Rows.Add(
                    pc.MaPhanCong,
                    pc.TieuDeBaiBao,
                    pc.TenPhanBien,
                    LanguageService.TranslateRound(pc.SoVong),
                    displayStatus,
                    hanNopDisplay,
                    diemStr,
                    LanguageService.TranslateRecommendation(pc.KienNghi)
                );
                dgvReviews.Rows[idx].Tag = pc;
            }
        }

        private void BtnRemind_Click(object? sender, EventArgs e)
        {
            if (dgvReviews.CurrentRow?.Tag is PhanCongPhanBien pc)
            {
                if (pc.TrangThai == "Đã đánh giá")
                {
                    MessageBox.Show($"Chuyên gia {pc.TenPhanBien} đã hoàn thành thẩm định và nộp phiếu đánh giá bài báo này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string hanStr = pc.HanHoanThanh?.ToString("dd/MM/yyyy") ?? "chưa xác định";
                var dr = MessageBox.Show(
                    $"Xác nhận gửi thông báo và email tự động nhắc nhở chuyên gia hoàn thiện phản biện?\n\n• Chuyên gia: {pc.TenPhanBien} ({pc.DonViPhanBien ?? "Đơn vị"})\n• Bản thảo: {pc.TieuDeBaiBao}\n• Hạn hoàn thành: {hanStr}",
                    "Gửi nhắc nhở phản biện",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    MessageBox.Show($"Đã gửi thông báo nhắc hạn thẩm định thành công đến chuyên gia {pc.TenPhanBien}!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một dòng chuyên gia phản biện trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnViewScore_Click(object? sender, EventArgs e)
        {
            if (dgvReviews.CurrentRow?.Tag is PhanCongPhanBien pc)
            {
                using var frm = new FrmPhieuDanhGiaDialog(pc.MaPhanCong);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một dòng phân công phản biện.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnAssignNew_Click(object? sender, EventArgs e)
        {
            int maBaiBao = (dgvReviews.CurrentRow?.Tag is PhanCongPhanBien pc) ? pc.MaBaiBao : 0;
            if (maBaiBao == 0)
            {
                var articles = BaiBaoService.GetAllBaiBao();
                var cand = articles.Find(b => b.TrangThai == "Chờ sơ duyệt" || b.TrangThai == "Đang phản biện");
                if (cand != null) maBaiBao = cand.MaBaiBao;
            }

            if (maBaiBao > 0)
            {
                using var frm = new FrmPhanCongPhanBien(maBaiBao, isReviewer3Mode: false);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bản thảo trong danh sách hoặc chuyển sang màn hình 'Quản lý bản thảo' để chọn bài cần phân công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnInviteReviewer3_Click(object? sender, EventArgs e)
        {
            if (dgvReviews.CurrentRow?.Tag is PhanCongPhanBien pc)
            {
                using var frm = new FrmPhanCongPhanBien(pc.MaBaiBao, isReviewer3Mode: true);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bài báo trong bảng để mời phản biện thứ 3 (Trọng tài khoa học phân xử bất đồng).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExtend_Click(object? sender, EventArgs e)
        {
            if (dgvReviews.CurrentRow?.Tag is PhanCongPhanBien pc)
            {
                DateTime curHan = pc.HanHoanThanh ?? DateTime.Now.AddDays(15);
                DateTime newHan = curHan.AddDays(15);
                var dr = MessageBox.Show($"Gia hạn thêm 15 ngày hoàn thành đánh giá cho chuyên gia:\n{pc.TenPhanBien}\n\n• Hạn hiện tại: {curHan:dd/MM/yyyy}\n• Hạn mới đề xuất: {newHan:dd/MM/yyyy}", "Xác nhận gia hạn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    bool ok = PhanBienService.GiaHanPhanBien(pc.MaPhanCong, newHan);
                    if (ok)
                    {
                        MessageBox.Show("Đã gia hạn thời gian phản biện thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else MessageBox.Show(JournalApiClient.LastError ?? "Không thể gia hạn phản biện.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một dòng phân công cần gia hạn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (dgvReviews.CurrentRow?.Tag is PhanCongPhanBien pc)
            {
                var dr = MessageBox.Show($"Bạn có chắc chắn muốn HỦY phân công phản biện này?\n\n• Bản thảo: JST-{pc.MaBaiBao:D4}\n• Chuyên gia: {pc.TenPhanBien}\n• Đơn vị: {pc.DonViPhanBien}\n\n(Dữ liệu phân công này sẽ được gỡ bỏ khỏi hệ thống)", "Xác nhận hủy phân công", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    bool ok = PhanBienService.XoaPhanCong(pc.MaPhanCong);
                    if (ok)
                    {
                        MessageBox.Show("Đã hủy phân công phản biện thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show(JournalApiClient.LastError ?? "Không thể hủy phân công phản biện.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một dòng phân công cần hủy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
