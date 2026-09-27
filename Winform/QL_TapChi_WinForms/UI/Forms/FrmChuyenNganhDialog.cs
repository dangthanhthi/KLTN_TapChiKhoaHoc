using System;
using System.Drawing;
using System.Windows.Forms;
using QL_TapChi_WinForms.Models;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Components;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Forms
{
    public class FrmChuyenNganhDialog : Form
    {
        private DataGridView dgvCategories = null!;
        private TextBox txtTen = null!;
        private TextBox txtMoTa = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnDelete = null!;
        private ModernButton btnAddNew = null!;
        private int _selectedId = 0;

        public FrmChuyenNganhDialog()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Quản lý Danh mục Chuyên ngành Nghiên cứu Khoa học · JST";
            Size = new Size(880, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.AppBackground;

            // 1. Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(24, 12, 24, 12)
            };

            var lblTitle = new Label
            {
                Text = "QUẢN LÝ DANH MỤC CHUYÊN NGÀNH KHOA HỌC",
                Font = UITheme.FontTitle,
                ForeColor = Color.White,
                Location = new Point(20, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Thêm mới, điều chỉnh tên và mô tả các lĩnh vực khoa học xuất bản theo chuẩn phân loại",
                Font = UITheme.FontSmall,
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(22, 38),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // 2. Main Split
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            Shown += (s, e) =>
            {
                try
                {
                    if (split.Width > 500)
                    {
                        split.SplitterDistance = Math.Max(200, (int)(split.Width * 0.55));
                    }
                }
                catch { }
            };

            // Left: Grid
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

            dgvCategories = new DataGridView { Dock = DockStyle.Fill };
            UITheme.ApplyModernGridStyle(dgvCategories);
            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = "MÃ", Width = 65, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ten", HeaderText = "TÊN CHUYÊN NGÀNH", Width = 340, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
            dgvCategories.SelectionChanged += DgvCategories_SelectionChanged;

            pnlGrid.Controls.Add(dgvCategories);
            split.Panel1.Controls.Add(pnlGrid);

            // Right: Form editor
            var pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            pnlForm.Paint += (s, e) =>
            {
                using var p = new Pen(UITheme.BorderColor);
                e.Graphics.DrawRectangle(p, 0, 0, pnlForm.Width - 1, pnlForm.Height - 1);
            };

            var lblFormTitle = new Label
            {
                Text = "THÔNG TIN CHUYÊN NGÀNH",
                Font = UITheme.FontSection,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(20, 16),
                AutoSize = true
            };

            var lblTen = new Label
            {
                Text = "Tên chuyên ngành (*):",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(20, 56),
                AutoSize = true
            };

            txtTen = new TextBox
            {
                Font = UITheme.FontBody,
                Location = new Point(20, 78),
                Size = new Size(320, 28)
            };

            var lblMoTa = new Label
            {
                Text = "Mô tả phạm vi nghiên cứu:",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(20, 120),
                AutoSize = true
            };

            txtMoTa = new TextBox
            {
                Font = UITheme.FontBody,
                Location = new Point(20, 142),
                Size = new Size(320, 160),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            btnAddNew = new ModernButton
            {
                Text = "+ Tạo mới",
                Size = new Size(95, 38),
                Location = new Point(20, 320),
                NormalColor = UITheme.Sky050,
                HoverColor = UITheme.Sky100,
                BorderColor = UITheme.Sky200,
                ForeColor = UITheme.PrimaryDark
            };
            btnAddNew.Click += (s, e) => ResetForm();

            btnDelete = new ModernButton
            {
                Text = "Xóa",
                Size = new Size(85, 38),
                Location = new Point(125, 320),
                NormalColor = UITheme.DangerBg,
                HoverColor = Color.FromArgb(254, 226, 226),
                BorderColor = Color.FromArgb(254, 202, 202),
                ForeColor = Color.FromArgb(185, 28, 28)
            };
            btnDelete.Click += BtnDelete_Click;

            btnSave = new ModernButton
            {
                Text = "Lưu chuyên ngành",
                Size = new Size(130, 38),
                Location = new Point(218, 320),
                NormalColor = UITheme.Primary,
                HoverColor = UITheme.PrimaryDark,
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;

            pnlForm.Controls.AddRange(new Control[]
            {
                lblFormTitle,
                lblTen, txtTen,
                lblMoTa, txtMoTa,
                btnAddNew, btnDelete, btnSave
            });
            split.Panel2.Controls.Add(pnlForm);

            Controls.AddRange(new Control[] { split, pnlHeader });
        }

        private void LoadData()
        {
            var list = ChuyenNganhService.GetAllChuyenNganh();
            dgvCategories.Rows.Clear();
            foreach (var cn in list)
            {
                int idx = dgvCategories.Rows.Add(cn.MaChuyenNganh, cn.TenChuyenNganh);
                dgvCategories.Rows[idx].Tag = cn;
            }

            if (dgvCategories.Rows.Count > 0)
            {
                dgvCategories.Rows[0].Selected = true;
                DgvCategories_SelectionChanged(null, EventArgs.Empty);
            }
            else
            {
                ResetForm();
            }
        }

        private void DgvCategories_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvCategories.CurrentRow?.Tag is ChuyenNganh cn)
            {
                _selectedId = cn.MaChuyenNganh;
                txtTen.Text = cn.TenChuyenNganh;
                txtMoTa.Text = cn.MoTa ?? "";
                btnDelete.Enabled = true;
            }
        }

        private void ResetForm()
        {
            _selectedId = 0;
            txtTen.Clear();
            txtMoTa.Clear();
            btnDelete.Enabled = false;
            txtTen.Focus();
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTen.Text))
            {
                MessageBox.Show("Vui lòng nhập tên chuyên ngành.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTen.Focus();
                return;
            }

            var cn = new ChuyenNganh
            {
                MaChuyenNganh = _selectedId,
                TenChuyenNganh = txtTen.Text.Trim(),
                MoTa = txtMoTa.Text.Trim()
            };

            bool ok = ChuyenNganhService.SaveChuyenNganh(cn, out string? err);
            if (ok)
            {
                MessageBox.Show("Lưu thông tin chuyên ngành thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            else
            {
                MessageBox.Show(err ?? "Có lỗi khi lưu chuyên ngành.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedId > 0)
            {
                var dr = MessageBox.Show($"Bạn có chắc chắn muốn XÓA chuyên ngành:\n'{txtTen.Text}'?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    bool ok = ChuyenNganhService.DeleteChuyenNganh(_selectedId, out string? err);
                    if (ok)
                    {
                        MessageBox.Show("Đã xóa chuyên ngành thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show(err ?? "Không thể xóa chuyên ngành.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
    }
}
