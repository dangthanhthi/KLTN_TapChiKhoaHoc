using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QL_TapChi_WinForms.UI.Styles
{
    public static class UITheme
    {
        // Bảng màu học thuật hiện đại, trang nhã (Đồng bộ 100% với Web Tạp chí Khoa học & Công nghệ)
        // Azure Blue #1DA1F2 (Banner Web), Dark Ink Navy #16202B (--ink-900), Soft Slate #F8FAFC, Line #E1E9F1
        public static readonly Color Primary = Color.FromArgb(29, 161, 242);        // Azure Blue #1DA1F2 (Web Brand Blue)
        public static readonly Color PrimaryHover = Color.FromArgb(13, 141, 220);   // #0D8DDC
        public static readonly Color PrimaryDark = Color.FromArgb(10, 116, 183);    // #0A74B7
        public static readonly Color PrimaryLight = Color.FromArgb(240, 247, 255); // Soft Sky Tint #F0F7FF

        public static readonly Color HeaderBg = Color.FromArgb(29, 161, 242);       // Azure Blue #1DA1F2 (Đồng bộ Header Banner Web)
        public static readonly Color HeaderBar = Color.FromArgb(29, 161, 242);

        public static readonly Color SidebarBg = Color.FromArgb(255, 255, 255);        // Pure White Academic Sidebar #FFFFFF
        public static readonly Color SidebarHover = Color.FromArgb(240, 247, 255);     // Soft Sky Hover #F0F7FF
        public static readonly Color SidebarActive = Color.FromArgb(224, 242, 254);    // Sky-100 Active #E0F2FE
        public static readonly Color SidebarBorder = Color.FromArgb(225, 233, 241);    // Web --line #E1E9F1
        public static readonly Color SidebarText = Color.FromArgb(51, 65, 85);          // Slate-700 #334155
        public static readonly Color SidebarTextActive = Color.FromArgb(10, 116, 183); // Azure Brand Dark #0A74B7

        public static readonly Color AppBackground = Color.FromArgb(248, 250, 252);// Clean Slate-50 #F8FAFC
        public static readonly Color CardBackground = Color.White;
        public static readonly Color BorderColor = Color.FromArgb(225, 233, 241);  // Web --line #E1E9F1
        public static readonly Color BorderColorStrong = Color.FromArgb(201, 223, 244); // Web --sky-200 #C9DFF4

        public static readonly Color Sky050 = Color.FromArgb(243, 248, 253);       // #F3F8FD
        public static readonly Color Sky100 = Color.FromArgb(228, 239, 250);       // #E4EFFA
        public static readonly Color Sky200 = Color.FromArgb(201, 223, 244);       // #C9DFF4

        public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);     // Slate-900 #0F172A
        public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);  // Slate-600 #475569
        public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);    // Slate-400 #94A3B8

        // Trạng thái theo badge quy trình học thuật (Chuẩn Pastel cao cấp)
        public static readonly Color Success = Color.FromArgb(22, 163, 74);        // Green-600 #16A34A
        public static readonly Color SuccessBg = Color.FromArgb(220, 252, 231);    // Green-100 #DCFCE7
        public static readonly Color SuccessBorder = Color.FromArgb(134, 239, 172); // Green-300 #86EFAC

        public static readonly Color Warning = Color.FromArgb(217, 119, 6);        // Amber-600 #D97706
        public static readonly Color WarningBg = Color.FromArgb(254, 243, 199);    // Amber-100 #FEF3C7
        public static readonly Color WarningBorder = Color.FromArgb(253, 230, 138);// Amber-300 #FDE68A

        public static readonly Color Danger = Color.FromArgb(220, 38, 38);         // Red-600 #DC2626
        public static readonly Color DangerBg = Color.FromArgb(254, 226, 226);     // Red-100 #FEE2E2
        public static readonly Color DangerBorder = Color.FromArgb(254, 202, 202); // Red-200 #FECACA

        public static readonly Color Info = Color.FromArgb(2, 132, 199);           // Sky-600 #0284C7
        public static readonly Color InfoBg = Color.FromArgb(224, 242, 254);       // Sky-100 #E0F2FE
        public static readonly Color InfoBorder = Color.FromArgb(186, 230, 253);   // Sky-200 #BAE6FD

        public static readonly Color Purple = Color.FromArgb(124, 58, 237);        // Violet-600 #7C3AED
        public static readonly Color PurpleBg = Color.FromArgb(245, 243, 255);     // Violet-100 #F5F3FF
        public static readonly Color PurpleBorder = Color.FromArgb(221, 214, 254); // Violet-200 #DDD6FE

        // Typography (Segoe UI sắc nét với ClearType, tối ưu cho Windows 10/11)
        public static readonly Font FontBody = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font FontBodyBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font FontTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
        public static readonly Font FontSection = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        public static readonly Font FontSmall = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static readonly Font FontSmallBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        public static (Color TextColor, Color BgColor, Color BorderColor) GetStageColors(string? trangThai)
        {
            if (string.IsNullOrWhiteSpace(trangThai))
                return (TextSecondary, Sky050, BorderColor);

            return trangThai switch
            {
                "Đã xuất bản" or "Sẵn sàng xuất bản" or "Published" or "Ready to Publish" => (Success, SuccessBg, SuccessBorder),
                "Đã chấp nhận" or "Accepted" => (Success, SuccessBg, SuccessBorder),
                "Đang phản biện" or "Under Review" or "Review Accepted" or "Đồng ý phản biện" => (Info, InfoBg, InfoBorder),
                "Chờ sơ duyệt" or "Initial Review" => (Warning, WarningBg, WarningBorder),
                "Chờ chỉnh sửa" or "Chờ sửa hình thức" or "Revisions" or "Format Revision" => (Danger, DangerBg, DangerBorder),
                "Chờ quyết định" or "Decision Pending" => (Purple, PurpleBg, PurpleBorder),
                "Đang chế bản" or "Copyediting" => (Info, InfoBg, InfoBorder),
                "Từ chối" or "Rejected" or "Review Declined" or "Từ chối phản biện" => (Danger, DangerBg, DangerBorder),
                "Đang đánh giá" or "In Progress" => (Info, InfoBg, InfoBorder),
                "Đã đánh giá" or "Completed" => (Success, SuccessBg, SuccessBorder),
                "Quá hạn" or "Overdue" => (Danger, DangerBg, DangerBorder),
                _ => (TextPrimary, Sky050, BorderColor)
            };
        }

        public static void ApplyModernGridStyle(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(241, 245, 249);
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = true;
            dgv.AllowUserToResizeColumns = true;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.Font = FontBody;

            // Header Style: Sạch sẽ, cao 38px, tiêu đề căn giữa đồng bộ, cho phép kéo co dãn cột linh hoạt
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(51, 65, 85);
            dgv.ColumnHeadersDefaultCellStyle.Font = FontBodyBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersHeight = 38;

            // Custom painting for headers to guarantee 100% consistent color, center alignment, and modern sort indicators
            dgv.CellPainting += (s, e) =>
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    if (e.Graphics == null) return;
                    e.PaintBackground(e.CellBounds, true);
                    using var brush = new SolidBrush(Color.FromArgb(248, 250, 252));
                    e.Graphics.FillRectangle(brush, e.CellBounds);
                    using var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1);
                    e.Graphics.DrawLine(borderPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                    string headerText = dgv.Columns[e.ColumnIndex].HeaderText;
                    if (!string.IsNullOrEmpty(headerText))
                    {
                        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

                        bool isSorted = dgv.SortedColumn == dgv.Columns[e.ColumnIndex] && dgv.SortOrder != SortOrder.None;
                        int rightReserved = isSorted ? 14 : 4;
                        var textRect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top, Math.Max(0, e.CellBounds.Width - 8 - (isSorted ? 10 : 0)), e.CellBounds.Height);
                        TextRenderer.DrawText(e.Graphics, headerText, FontBodyBold, textRect, Color.FromArgb(51, 65, 85), flags);

                        if (isSorted)
                        {
                            int arrowX = e.CellBounds.Right - 8;
                            int arrowY = e.CellBounds.Top + (e.CellBounds.Height / 2);
                            using var arrowBrush = new SolidBrush(Color.FromArgb(10, 116, 183));
                            Point[] pts = dgv.SortOrder == SortOrder.Ascending
                                ? new Point[] { new Point(arrowX - 4, arrowY + 2), new Point(arrowX + 4, arrowY + 2), new Point(arrowX, arrowY - 3) }
                                : new Point[] { new Point(arrowX - 4, arrowY - 3), new Point(arrowX + 4, arrowY - 3), new Point(arrowX, arrowY + 2) };
                            e.Graphics.FillPolygon(arrowBrush, pts);
                        }
                    }

                    e.Handled = true;
                }
            };

            // Intelligent SortCompare for unbound grids (Handles numbers, dates, prefixes, and text)
            dgv.SortCompare += (s, e) =>
            {
                if (e.CellValue1 == null && e.CellValue2 == null) { e.SortResult = 0; e.Handled = true; return; }
                if (e.CellValue1 == null) { e.SortResult = -1; e.Handled = true; return; }
                if (e.CellValue2 == null) { e.SortResult = 1; e.Handled = true; return; }

                string s1 = e.CellValue1.ToString()?.Trim() ?? "";
                string s2 = e.CellValue2.ToString()?.Trim() ?? "";

                // 1. Direct or localized decimal/number comparison
                if (double.TryParse(s1.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d1) &&
                    double.TryParse(s2.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d2))
                {
                    e.SortResult = d1.CompareTo(d2);
                    e.Handled = true;
                    return;
                }

                // 2. Dates in format dd/MM/yyyy
                if (DateTime.TryParseExact(s1, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dt1) &&
                    DateTime.TryParseExact(s2, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dt2))
                {
                    e.SortResult = dt1.CompareTo(dt2);
                    e.Handled = true;
                    return;
                }

                // 2b. Dates with text suffix like "05/12/2026 (Còn 76 ngày)"
                if (s1.Length >= 10 && s2.Length >= 10 &&
                    DateTime.TryParseExact(s1.Substring(0, 10), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dtSub1) &&
                    DateTime.TryParseExact(s2.Substring(0, 10), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dtSub2))
                {
                    e.SortResult = dtSub1.CompareTo(dtSub2);
                    e.Handled = true;
                    return;
                }

                // 3. Formatted integers like "3 bài" or "5 articles"
                var m1 = System.Text.RegularExpressions.Regex.Match(s1, @"^(\d+)");
                var m2 = System.Text.RegularExpressions.Regex.Match(s2, @"^(\d+)");
                if (m1.Success && m2.Success && int.TryParse(m1.Groups[1].Value, out int n1) && int.TryParse(m2.Groups[1].Value, out int n2))
                {
                    e.SortResult = n1.CompareTo(n2);
                    e.Handled = true;
                    return;
                }

                // 4. Fallback string compare
                e.SortResult = string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase);
                e.Handled = true;
            };

            // Row Style: Chiều cao thoáng đãng 56px, tự động wrap text tiếng Việt không bị cắt chữ
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.RowsDefaultCellStyle.ForeColor = TextPrimary;
            dgv.RowsDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dgv.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254); // Soft Sky Tint #E0F2FE
            dgv.RowsDefaultCellStyle.SelectionForeColor = TextPrimary; // Text remains readable dark slate!
            dgv.RowTemplate.Height = 56;

            // Alternating rows
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);
            dgv.AlternatingRowsDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextPrimary;
        }

        public static void ApplyCompactGridStyle(DataGridView dgv, int rowHeight = 46)
        {
            ApplyModernGridStyle(dgv);
            dgv.RowTemplate.Height = rowHeight;
            dgv.RowsDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
            dgv.AlternatingRowsDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        }

        public static void ApplyPrimaryButton(QL_TapChi_WinForms.UI.Components.ModernButton btn)
        {
            btn.NormalColor = Primary;
            btn.HoverColor = PrimaryHover;
            btn.BorderColor = Color.Transparent;
            btn.ForeColor = Color.White;
            btn.Font = FontBodyBold;
        }

        public static void ApplySecondaryButton(QL_TapChi_WinForms.UI.Components.ModernButton btn)
        {
            btn.NormalColor = Color.White;
            btn.HoverColor = Color.FromArgb(243, 244, 246);
            btn.BorderColor = Color.FromArgb(209, 213, 219);
            btn.ForeColor = Color.FromArgb(31, 41, 55);
            btn.Font = FontBodyBold;
        }

        public static void ApplyDangerButton(QL_TapChi_WinForms.UI.Components.ModernButton btn)
        {
            btn.NormalColor = Color.White;
            btn.HoverColor = Color.FromArgb(254, 242, 242);
            btn.BorderColor = Color.FromArgb(209, 213, 219);
            btn.ForeColor = Color.FromArgb(220, 38, 38);
            btn.Font = FontBodyBold;
        }

        public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            if (bounds.Width <= 0 || bounds.Height <= 0) return path;

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
