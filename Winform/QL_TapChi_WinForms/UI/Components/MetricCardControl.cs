using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Components
{
    public class MetricCardControl : UserControl
    {
        private string _title = "CHỈ SỐ";
        private string _value = "0";
        private string _subtext = "";
        private Color _accentColor = UITheme.Primary;
        private bool _isHovered = false;

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        public string Subtext
        {
            get => _subtext;
            set { _subtext = value; Invalidate(); }
        }

        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        public MetricCardControl()
        {
            DoubleBuffered = true;
            Size = new Size(240, 100);
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 0, 16, 0);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Fill card background: White or soft sky tint on hover
            Color bg = _isHovered ? Color.FromArgb(243, 248, 253) : Color.White;
            using (var brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, rect);
            }

            // Draw clean subtle border (Web var(--line) #E1E9F1)
            Color borderColor = _isHovered ? UITheme.Primary : UITheme.BorderColor;
            using (var pen = new Pen(borderColor, 1))
            {
                g.DrawRectangle(pen, rect);
            }

            // Draw accent strip on left border (4px)
            using (var accentBrush = new SolidBrush(_accentColor))
            {
                g.FillRectangle(accentBrush, 0, 0, 4, Height);
            }

            // Draw Title (Clean spec-sheet label in var(--ink-500) #6B7684)
            using (var brush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
            {
                var titleRect = new RectangleF(14, 11, Width - 26, 18);
                g.DrawString(_title, UITheme.FontSmallBold, brush, titleRect, sf);
            }

            // Draw Value (Prominent bold number or status text, dynamically scaled to fit)
            float valFontSize = (_value.Length > 7) ? 14.5f : ((_value.Length > 4) ? 16.5f : 20f);
            using (var font = new Font("Segoe UI", valFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(UITheme.TextPrimary))
            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
            {
                var valRect = new RectangleF(13, 33, Width - 26, 36);
                g.DrawString(_value, font, brush, valRect, sf);
            }

            // Draw Subtext in muted tone
            if (!string.IsNullOrEmpty(_subtext))
            {
                using (var brush = new SolidBrush(UITheme.TextMuted))
                using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
                {
                    var subRect = new RectangleF(14, 71, Width - 26, 18);
                    g.DrawString(_subtext, UITheme.FontSmall, brush, subRect, sf);
                }
            }
        }
    }
}
