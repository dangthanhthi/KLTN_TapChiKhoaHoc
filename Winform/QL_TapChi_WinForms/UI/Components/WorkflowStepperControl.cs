using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QL_TapChi_WinForms.UI.Styles;

namespace QL_TapChi_WinForms.UI.Components
{
    public class WorkflowStepperControl : UserControl
    {
        private static readonly string[] Stages = new[]
        {
            "1. Sơ duyệt",
            "2. Phản biện",
            "3. Chỉnh sửa",
            "4. Quyết định",
            "5. Chế bản",
            "6. Xuất bản"
        };

        private string _currentStatus = "Chờ sơ duyệt";

        public string CurrentStatus
        {
            get => _currentStatus;
            set
            {
                _currentStatus = value;
                Invalidate();
            }
        }

        public WorkflowStepperControl()
        {
            DoubleBuffered = true;
            Height = 82;
            BackColor = Color.FromArgb(248, 250, 252);
            Padding = new Padding(16, 12, 16, 12);
        }

        private int GetCurrentStageIndex()
        {
            return _currentStatus switch
            {
                "Chờ sơ duyệt" or "Chờ sửa hình thức" => 0,
                "Đang phản biện" => 1,
                "Chờ chỉnh sửa" => 2,
                "Chờ quyết định" or "Đã chấp nhận" => 3,
                "Đang chế bản" or "Sẵn sàng xuất bản" => 4,
                "Đã xuất bản" => 5,
                "Từ chối" => 3,
                _ => 0
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var borderPen = new Pen(UITheme.BorderColor))
            {
                g.DrawRectangle(borderPen, bounds);
            }

            int activeIdx = GetCurrentStageIndex();
            bool isAlert = _currentStatus.Contains("sửa") || _currentStatus == "Từ chối";

            int stepCount = Stages.Length;
            int stepWidth = Width / stepCount;

            // Background line connecting all steps
            int lineY = 26;
            using (var linePen = new Pen(Color.FromArgb(226, 232, 240), 2))
            {
                g.DrawLine(linePen, stepWidth / 2, lineY, Width - (stepWidth / 2), lineY);
            }

            // Completed line portion
            if (activeIdx > 0)
            {
                using (var completedPen = new Pen(UITheme.Success, 2))
                {
                    int endX = Math.Min(Width - (stepWidth / 2), (activeIdx * stepWidth) + (stepWidth / 2));
                    g.DrawLine(completedPen, stepWidth / 2, lineY, endX, lineY);
                }
            }

            for (int i = 0; i < stepCount; i++)
            {
                int centerX = (i * stepWidth) + (stepWidth / 2);
                int centerY = lineY;
                int dotSize = 24;
                var dotRect = new Rectangle(centerX - (dotSize / 2), centerY - (dotSize / 2), dotSize, dotSize);

                Color dotColor;
                Color textColor;
                string symbol = (i + 1).ToString();

                if (i < activeIdx)
                {
                    dotColor = UITheme.Success;
                    textColor = UITheme.Success;
                    symbol = "✓";
                }
                else if (i == activeIdx)
                {
                    dotColor = isAlert ? UITheme.Danger : UITheme.Primary;
                    textColor = isAlert ? UITheme.Danger : UITheme.Primary;

                    // Subtle glow ring
                    using (var ringBrush = new SolidBrush(Color.FromArgb(40, dotColor)))
                    {
                        g.FillEllipse(ringBrush, dotRect.X - 4, dotRect.Y - 4, dotSize + 8, dotSize + 8);
                    }
                }
                else
                {
                    dotColor = Color.FromArgb(203, 213, 225);
                    textColor = UITheme.TextSecondary;
                }

                // Fill dot
                using (var brush = new SolidBrush(dotColor))
                {
                    g.FillEllipse(brush, dotRect);
                }

                // Symbol inside dot
                using (var brush = new SolidBrush(Color.White))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(symbol, UITheme.FontSmallBold, brush, dotRect, sf);
                }

                // Stage Title text below
                var labelRect = new Rectangle(i * stepWidth, centerY + 16, stepWidth, 32);
                using (var brush = new SolidBrush(textColor))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
                {
                    Font stageFont = (i == activeIdx) ? UITheme.FontSmallBold : UITheme.FontSmall;
                    g.DrawString(Stages[i], stageFont, brush, labelRect, sf);
                }
            }
        }
    }
}
