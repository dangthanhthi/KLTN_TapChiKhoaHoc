using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmPhanCongPhanBien : Form
    {
        private readonly int _maBaiBao;
        private readonly bool _isReviewer3Mode;
        private BaiBao? _baiBao;
        private List<DongTacGia> _dongTacGiaList = new();
        private ComboBox cboReviewers = null!;
        private DateTimePicker dtpResponse = null!;
        private DateTimePicker dtpComplete = null!;
        private NumericUpDown numRound = null!;
        private Label lblReviewerInfo = null!;
        private Label lblCoiWarning = null!;
        private TextBox txtReason = null!;
        private ModernButton btnSave = null!;

        public FrmPhanCongPhanBien(int maBaiBao, bool isReviewer3Mode = false)
        {
            _maBaiBao = maBaiBao;
            _isReviewer3Mode = isReviewer3Mode;
            _baiBao = BaiBaoService.GetBaiBaoById(_maBaiBao);
            _dongTacGiaList = BaiBaoService.GetDongTacGia(_maBaiBao);
            InitializeComponent();
            LoadReviewers();
        }

        private void InitializeComponent()
        {
            Text = _isReviewer3Mode ? "Mời phản biện thứ ba (Trọng tài khoa học) · JST" : "Phân công chuyên gia phản biện · JST";
            Size = new Size(620, _isReviewer3Mode ? 520 : 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = UITheme.FontBody;

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = _isReviewer3Mode ? Color.FromArgb(254, 243, 199) : UITheme.Sky050,
                Padding = new Padding(20, 12, 20, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(_isReviewer3Mode ? Color.FromArgb(245, 158, 11) : UITheme.Sky200);
                e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = _isReviewer3Mode ? "MỜI PHẢN BIỆN THỨ 3 (TRỌNG TÀI KHOA HỌC)" : "GIAO BÀI CHO CHUYÊN GIA PHẢN BIỆN",
                Font = UITheme.FontSection,
                ForeColor = _isReviewer3Mode ? Color.FromArgb(180, 83, 9) : UITheme.PrimaryDark,
                Location = new Point(20, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = _isReviewer3Mode
                    ? $"Chỉ định chuyên gia thứ ba phân xử bản thảo JST-{_maBaiBao:D4} khi có bất đồng ý kiến"
                    : $"Gán bản thảo JST-{_maBaiBao:D4} cho hội đồng phản biện kín hai chiều",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(20, 38),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            int top = 82;

            // Main form fields
            var lblSelect = new Label
            {
                Text = _isReviewer3Mode ? "Chọn chuyên gia phản biện thứ 3 (Trọng tài):" : "Chọn chuyên gia phản biện:",
                Font = UITheme.FontBodyBold,
                Location = new Point(24, top),
                AutoSize = true
            };
            top += 24;

            cboReviewers = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(24, top),
                Size = new Size(555, 30)
            };
            cboReviewers.SelectedIndexChanged += CboReviewers_SelectedIndexChanged;
            top += 36;

            lblReviewerInfo = new Label
            {
                Text = "",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, top),
                Size = new Size(555, 22),
                AutoEllipsis = true
            };
            top += 24;

            lblCoiWarning = new Label
            {
                Text = "",
                Font = UITheme.FontSmallBold,
                Location = new Point(24, top),
                Size = new Size(555, 46),
                Visible = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 4, 8, 4),
                AutoEllipsis = true
            };
            top += 52;

            if (_isReviewer3Mode)
            {
                var lblReason = new Label
                {
                    Text = "Lý do chỉ định phản biện thứ ba (Bất đồng ý kiến):",
                    Font = UITheme.FontBodyBold,
                    ForeColor = Color.FromArgb(180, 83, 9),
                    Location = new Point(24, top),
                    AutoSize = true
                };
                Controls.Add(lblReason);
                top += 22;

                txtReason = new TextBox
                {
                    Font = UITheme.FontBody,
                    Location = new Point(24, top),
                    Size = new Size(555, 26),
                    Text = "Hai phản biện ban đầu có ý kiến trái ngược nhau (1 chấp nhận, 1 từ chối/yêu cầu sửa lớn)."
                };
                Controls.Add(txtReason);
                top += 36;
            }

            var lblRound = new Label
            {
                Text = "Vòng phản biện:",
                Font = UITheme.FontBodyBold,
                Location = new Point(24, top),
                AutoSize = true
            };

            numRound = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 5,
                Value = 1,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(24, top + 24),
                Size = new Size(130, 30)
            };

            var lblResponseDate = new Label
            {
                Text = "Hạn phản hồi:",
                Font = UITheme.FontBodyBold,
                Location = new Point(175, top),
                AutoSize = true
            };

            dtpResponse = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddDays(7),
                Font = new Font("Segoe UI", 10f),
                Location = new Point(175, top + 24),
                Size = new Size(185, 30)
            };

            var lblCompleteDate = new Label
            {
                Text = "Hạn hoàn thành:",
                Font = UITheme.FontBodyBold,
                Location = new Point(380, top),
                AutoSize = true
            };

            dtpComplete = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddDays(30),
                Font = new Font("Segoe UI", 10f),
                Location = new Point(380, top + 24),
                Size = new Size(199, 30)
            };
            top += 68;

            btnSave = new ModernButton
            {
                Text = _isReviewer3Mode ? "Mời phản biện thứ 3" : "Xác nhận giao bài",
                Size = new Size(210, 42),
                Location = new Point(369, top),
                NormalColor = _isReviewer3Mode ? Color.FromArgb(217, 119, 6) : UITheme.Primary,
                HoverColor = _isReviewer3Mode ? Color.FromArgb(180, 83, 9) : UITheme.PrimaryDark,
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Size = new Size(100, 42),
                Location = new Point(255, top),
                FlatStyle = FlatStyle.Flat,
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextSecondary
            };
            btnCancel.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                lblSelect, cboReviewers, lblReviewerInfo, lblCoiWarning,
                lblRound, numRound,
                lblResponseDate, dtpResponse,
                lblCompleteDate, dtpComplete,
                btnSave, btnCancel
            });
        }

        private void LoadReviewers()
        {
            var list = NguoiDungService.GetReviewers();
            cboReviewers.Items.Clear();
            foreach (var r in list)
            {
                cboReviewers.Items.Add(new ReviewerItem(r));
            }

            if (cboReviewers.Items.Count > 0)
            {
                cboReviewers.SelectedIndex = 0;
            }
        }

        private void CboReviewers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboReviewers.SelectedItem is ReviewerItem item)
            {
                lblReviewerInfo.Text = $"Học vị: {item.User.HocVi ?? "TS"}  •  Email: {item.User.Email}  •  {item.User.DonVi}";

                // Kiểm soát Xung đột Lợi ích (COI - Conflict of Interest)
                if (_baiBao != null)
                {
                    bool isAuthor = item.User.MaNguoiDung == _baiBao.MaNguoiDung ||
                                    string.Equals(item.User.HoTen, _baiBao.TenTacGia, StringComparison.OrdinalIgnoreCase);

                    bool isCoAuthor = _dongTacGiaList.Exists(dtg =>
                        string.Equals(dtg.HoTen, item.User.HoTen, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(dtg.Email) && string.Equals(dtg.Email, item.User.Email, StringComparison.OrdinalIgnoreCase)));

                    bool isSameAffiliation = !string.IsNullOrEmpty(item.User.DonVi) &&
                                            !string.IsNullOrEmpty(_baiBao.DonViTacGia) &&
                                            (item.User.DonVi.Contains(_baiBao.DonViTacGia, StringComparison.OrdinalIgnoreCase) ||
                                             _baiBao.DonViTacGia.Contains(item.User.DonVi, StringComparison.OrdinalIgnoreCase) ||
                                             (item.User.DonVi.Contains("HUIT") && _baiBao.DonViTacGia.Contains("HUIT")) ||
                                             (item.User.DonVi.Contains("Công thương") && _baiBao.DonViTacGia.Contains("Công thương")));

                    if (isAuthor || isCoAuthor)
                    {
                        lblCoiWarning.Visible = true;
                        lblCoiWarning.BackColor = Color.FromArgb(254, 242, 242);
                        lblCoiWarning.ForeColor = UITheme.Danger;
                        lblCoiWarning.Text = isAuthor
                            ? "[XUNG ĐỘT LỢI ÍCH] Chuyên gia được chọn chính là TÁC GIẢ CHÍNH bản thảo!\nKhông thể chỉ định chuyên gia tự phản biện bài viết của mình."
                            : "[XUNG ĐỘT LỢI ÍCH] Chuyên gia được chọn là ĐỒNG TÁC GIẢ bản thảo!\nKhông thể chỉ định đồng tác giả tham gia phản biện bài viết.";
                        btnSave.Enabled = false;
                    }
                    else if (isSameAffiliation)
                    {
                        lblCoiWarning.Visible = true;
                        lblCoiWarning.BackColor = Color.FromArgb(254, 243, 199);
                        lblCoiWarning.ForeColor = Color.FromArgb(180, 83, 9);
                        lblCoiWarning.Text = $"[CẢNH BÁO COI] Cùng đơn vị [{_baiBao.DonViTacGia}].\nTòa soạn khuyến nghị chọn chuyên gia khác đơn vị để bảo đảm khách quan.";
                        btnSave.Enabled = true;
                    }
                    else
                    {
                        lblCoiWarning.Visible = true;
                        lblCoiWarning.BackColor = Color.FromArgb(240, 253, 244);
                        lblCoiWarning.ForeColor = UITheme.Success;
                        lblCoiWarning.Text = "[HỢP LỆ] Chuyên gia độc lập, không phát hiện xung đột lợi ích.";
                        btnSave.Enabled = true;
                    }
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cboReviewers.SelectedItem is ReviewerItem item)
            {
                int reviewerId = item.User.MaNguoiDung;
                DateTime hanPhanHoi = dtpResponse.Value;
                DateTime hanHoanThanh = dtpComplete.Value;
                int round = (int)numRound.Value;

                if (hanHoanThanh < hanPhanHoi)
                {
                    MessageBox.Show("Hạn hoàn thành phải sau hạn phản hồi.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if ((hanHoanThanh.Date - hanPhanHoi.Date).TotalDays < 3)
                {
                    MessageBox.Show("Thời hạn hoàn thành phản biện phải cách hạn phản hồi tối thiểu 3 ngày để chuyên gia có đủ thời gian thẩm định.", "Cảnh báo thời hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_baiBao != null)
                {
                    bool isAuthor = item.User.MaNguoiDung == _baiBao.MaNguoiDung || string.Equals(item.User.HoTen, _baiBao.TenTacGia, StringComparison.OrdinalIgnoreCase);
                    bool isCoAuthor = _dongTacGiaList.Exists(dtg =>
                        string.Equals(dtg.HoTen, item.User.HoTen, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(dtg.Email) && string.Equals(dtg.Email, item.User.Email, StringComparison.OrdinalIgnoreCase)));

                    if (isAuthor || isCoAuthor)
                    {
                        MessageBox.Show(isAuthor
                            ? "Vi phạm quy chế phản biện: Chuyên gia được chọn chính là tác giả chính của bài báo!"
                            : "Vi phạm quy chế phản biện: Chuyên gia được chọn là đồng tác giả của bài báo!",
                            "Xung đột lợi ích (COI)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    bool isSameAffiliation = !string.IsNullOrEmpty(item.User.DonVi) &&
                                            !string.IsNullOrEmpty(_baiBao.DonViTacGia) &&
                                            (item.User.DonVi.Contains(_baiBao.DonViTacGia, StringComparison.OrdinalIgnoreCase) ||
                                             _baiBao.DonViTacGia.Contains(item.User.DonVi, StringComparison.OrdinalIgnoreCase));

                    if (isSameAffiliation)
                    {
                        var coiConfirm = MessageBox.Show(
                            $"Chuyên gia {item.User.HoTen} cùng đơn vị công tác với tác giả ({_baiBao.DonViTacGia}).\nBạn có chắc chắn muốn tiếp tục chỉ định phản biện cùng đơn vị này?",
                            "Cảnh báo Xung đột Lợi ích (COI)",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        if (coiConfirm != DialogResult.Yes) return;
                    }
                }

                bool ok;
                if (_isReviewer3Mode)
                {
                    string reason = txtReason?.Text.Trim() ?? "Bất đồng ý kiến đánh giá";
                    ok = PhanBienService.MoiPhanBienThuBa(_maBaiBao, reviewerId, hanPhanHoi, hanHoanThanh, reason, round);
                    if (ok)
                    {
                        MessageBox.Show($"Đã chỉ định Chuyên gia phản biện thứ 3: {item.User.HoTen} (Trọng tài khoa học) thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(JournalApiClient.LastError ?? "Không thể mời phản biện.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    ok = PhanBienService.PhanCongReviewer(_maBaiBao, reviewerId, hanPhanHoi, hanHoanThanh, round);
                    if (ok)
                    {
                        MessageBox.Show($"Đã phân công phản biện {item.User.HoTen} thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(JournalApiClient.LastError ?? "Không thể phân công phản biện.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private class ReviewerItem
        {
            public NguoiDung User { get; }
            public ReviewerItem(NguoiDung user) => User = user;
            public override string ToString() => $"{User.HocVi} {User.HoTen} - {User.DonVi}";
        }
    }
}
