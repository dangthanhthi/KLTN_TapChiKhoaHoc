using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Components
{
    public class ModernButton : Button
    {
        private Color _normalColor = UITheme.Primary;
        private Color _hoverColor = UITheme.PrimaryDark;
        private Color _pressedColor = Color.FromArgb(10, 45, 90);
        private Color _borderColor = Color.Transparent;
        private int _borderRadius = 5;
        private bool _isHovered = false;
        private bool _isPressed = false;

        public Color NormalColor
        {
            get => _normalColor;
            set { _normalColor = value; Invalidate(); }
        }

        public Color HoverColor
        {
            get => _hoverColor;
            set { _hoverColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value; UpdateRegion(); Invalidate(); }
        }

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Size = new Size(130, 36);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowOnly;
            Padding = new Padding(14, 0, 14, 0);

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; _isPressed = false; Invalidate(); };
            MouseDown += (s, e) => { _isPressed = true; Invalidate(); };
            MouseUp += (s, e) => { _isPressed = false; Invalidate(); };
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            // Do not use 1-bit window region clipping as it produces black/jagged corner artifacts in WinForms.
            // Rounded corners are smoothly anti-aliased directly in OnPaint using parent background color.
            Region = null;
        }

        private Color GetParentBackgroundColor()
        {
            Control? p = Parent;
            while (p != null)
            {
                if (p.BackColor != Color.Transparent && p.BackColor.A == 255)
                    return p.BackColor;
                p = p.Parent;
            }
            return Color.White;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color parentBg = GetParentBackgroundColor();
            using var brush = new SolidBrush(parentBg);
            pevent.Graphics.FillRectangle(brush, ClientRectangle);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var measured = TextRenderer.MeasureText(Text ?? string.Empty, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            int targetWidth = Math.Max(75, measured.Width + 24);
            int targetHeight = Math.Max(36, Height > 0 ? Height : 36);
            return new Size(targetWidth, targetHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color parentBg = GetParentBackgroundColor();
            using (var bgBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = _isPressed ? _pressedColor : (_isHovered ? _hoverColor : _normalColor);

            if (_borderRadius > 0)
            {
                using var path = UITheme.CreateRoundedRectangle(rect, _borderRadius);
                using var brush = new SolidBrush(fill);
                g.FillPath(brush, path);

                if (_borderColor != Color.Transparent)
                {
                    using var pen = new Pen(_borderColor, 1);
                    g.DrawPath(pen, path);
                }
            }
            else
            {
                using var brush = new SolidBrush(fill);
                g.FillRectangle(brush, rect);

                if (_borderColor != Color.Transparent)
                {
                    using var pen = new Pen(_borderColor, 1);
                    g.DrawRectangle(pen, rect);
                }
            }

            // Draw Text centered cleanly, never convert & to shortcut underscore, never cut off
            TextRenderer.DrawText(g, Text, Font, new Rectangle(2, 0, Width - 4, Height), ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }
}
