using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Region selector form - Full screen overlay screenshot
    /// </summary>
    public partial class RegionSelectorForm : Form
    {
        private enum InteractionMode { Idle, Drawing, Moving, Resizing }
        private enum HandleKind { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, Inside }

        private const int HandleSize = 10;          // 多边形句柄的边长
        private const int HandleHitPadding = 4;     // 点击面积往外拓宽一点，好拖

        private Bitmap _fullScreenshot = null!;
        private Point _startPoint;
        private Point _endPoint;
        private Rectangle _selectionRectangle = Rectangle.Empty;
        private Bitmap _overlayBuffer = null!;

        private InteractionMode _mode = InteractionMode.Idle;
        private HandleKind _activeHandle = HandleKind.None;
        private Point _dragOffset;            // 拖动整个选区时鼠标相对 selection 左上角的偏移
        private Rectangle _dragStartRect;     // 调整大小开始时的原始矩形

        public Bitmap CapturedImage { get; private set; } = null!;
        public Rectangle SelectionRegion { get; private set; }
        public Rectangle SelectionScreenBounds { get; private set; }

        public RegionSelectorForm()
        {
            InitializeComponent();
            ConfigureForm();
            CaptureFullScreen();
        }

        /// <summary>
        /// Configure form properties for full screen overlay mode
        /// </summary>
        private void ConfigureForm()
        {
            Screen screen = Screen.FromPoint(Cursor.Position);

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = screen.Bounds;
            this.BackColor = Color.Black;
            this.Opacity = 1;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
            this.KeyPreview = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.TopMost = true;
            this.BringToFront();
            this.Activate();
            this.Focus();
        }

        /// <summary>
        /// Capture full screen screenshot
        /// </summary>
        private void CaptureFullScreen()
        {
            try
            {
                Screen screen = Screen.FromPoint(Cursor.Position);
                Rectangle screenBounds = screen.Bounds;
                _fullScreenshot = new Bitmap(screenBounds.Width, screenBounds.Height);
                using (Graphics g = Graphics.FromImage(_fullScreenshot))
                {
                    g.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Screenshot failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Right)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }
            if (e.Button != MouseButtons.Left) return;

            // 如果已经有选区 —— 判断点击位置在句柄 / 选区内 / 选区外
            if (!_selectionRectangle.IsEmpty)
            {
                HandleKind hk = HitTest(e.Location);
                if (hk != HandleKind.None && hk != HandleKind.Inside)
                {
                    _mode = InteractionMode.Resizing;
                    _activeHandle = hk;
                    _dragStartRect = _selectionRectangle;
                    _startPoint = e.Location;
                    return;
                }
                if (hk == HandleKind.Inside)
                {
                    _mode = InteractionMode.Moving;
                    _dragOffset = new Point(e.X - _selectionRectangle.X, e.Y - _selectionRectangle.Y);
                    return;
                }
            }

            // 重新拉一个新选区
            _mode = InteractionMode.Drawing;
            _startPoint = e.Location;
            _selectionRectangle = Rectangle.Empty;
            this.Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            switch (_mode)
            {
                case InteractionMode.Drawing:
                    _endPoint = e.Location;
                    _selectionRectangle = GetRectangleFromPoints(_startPoint, _endPoint);
                    this.Invalidate();
                    break;

                case InteractionMode.Moving:
                    {
                        int newX = e.X - _dragOffset.X;
                        int newY = e.Y - _dragOffset.Y;
                        // 限制在屏幕范围内
                        newX = Math.Clamp(newX, 0, this.ClientSize.Width - _selectionRectangle.Width);
                        newY = Math.Clamp(newY, 0, this.ClientSize.Height - _selectionRectangle.Height);
                        _selectionRectangle = new Rectangle(newX, newY, _selectionRectangle.Width, _selectionRectangle.Height);
                        this.Invalidate();
                        break;
                    }

                case InteractionMode.Resizing:
                    _selectionRectangle = ResizeFromHandle(_dragStartRect, _activeHandle, e.Location);
                    this.Invalidate();
                    break;

                case InteractionMode.Idle:
                    // 鼠标悬停时根据位置动态切换光标
                    if (!_selectionRectangle.IsEmpty)
                    {
                        this.Cursor = CursorForHandle(HitTest(e.Location));
                    }
                    break;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            if (_mode == InteractionMode.Drawing)
            {
                _endPoint = e.Location;
                _selectionRectangle = GetRectangleFromPoints(_startPoint, _endPoint);
            }

            // 拖动/调整后保证选区不会反向且在屏幕内
            _selectionRectangle = NormalizeAndClamp(_selectionRectangle);

            _mode = InteractionMode.Idle;
            _activeHandle = HandleKind.None;
            this.Cursor = !_selectionRectangle.IsEmpty ? CursorForHandle(HitTest(e.Location)) : Cursors.Cross;
            this.Invalidate();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button == MouseButtons.Left && !_selectionRectangle.IsEmpty
                && _selectionRectangle.Contains(e.Location))
            {
                ConfirmSelection();
            }
        }

        /// <summary>
        /// Capture image from selected region
        /// </summary>
        private void CaptureSelection()
        {
            try
            {
                CapturedImage = new Bitmap(_selectionRectangle.Width, _selectionRectangle.Height);
                using (Graphics g = Graphics.FromImage(CapturedImage))
                {
                    g.DrawImage(_fullScreenshot, 0, 0, _selectionRectangle, GraphicsUnit.Pixel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save screenshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_fullScreenshot != null)
            {
                try
                {
                    e.Graphics.DrawImage(_fullScreenshot, this.ClientRectangle);

                    // Draw mask layer (semi-transparent black)
                    DrawMaskLayer(e);

                    if (!_selectionRectangle.IsEmpty)
                    {
                        // Draw original screenshot in selected area (fully opaque)
                        DrawHighlightedRegion(e);

                        // Draw selection border
                        DrawSelectionBorder(e);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Drawing error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Draw black mask layer
        /// </summary>
        private void DrawMaskLayer(PaintEventArgs e)
        {
            using (Brush maskBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0))) // ~55% 透明黑色，明显颜色变暗
            {
                e.Graphics.FillRectangle(maskBrush, this.ClientRectangle);
            }
        }

        /// <summary>
        /// Draw original screenshot in selected rectangle area (highlight)
        /// </summary>
        private void DrawHighlightedRegion(PaintEventArgs e)
        {
            if (_selectionRectangle.Width > 0 && _selectionRectangle.Height > 0)
            {
                e.Graphics.DrawImage(
                    _fullScreenshot,
                    _selectionRectangle,
                    _selectionRectangle,
                    GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// Draw selection border
        /// </summary>
        private void DrawSelectionBorder(PaintEventArgs e)
        {
            // 红色粗边框，任何背景上都看得清
            using (Pen borderPen = new Pen(Color.FromArgb(255, 230, 50, 50), 3))
            {
                e.Graphics.DrawRectangle(borderPen, _selectionRectangle);
            }

            // Draw corner markers
            DrawCornerMarkers(e);

            // Draw size information
            DrawSizeInfo(e);
        }

        /// <summary>
        /// Draw corner markers for selection frame
        /// </summary>
        private void DrawCornerMarkers(PaintEventArgs e)
        {
            const int markerSize = HandleSize;
            int half = markerSize / 2;
            Rectangle r = _selectionRectangle;
            int midX = r.Left + r.Width / 2;
            int midY = r.Top + r.Height / 2;

            Point[] handles =
            [
                new(r.Left,  r.Top),     // TL
                new(midX,    r.Top),     // T
                new(r.Right, r.Top),     // TR
                new(r.Right, midY),      // R
                new(r.Right, r.Bottom),  // BR
                new(midX,    r.Bottom),  // B
                new(r.Left,  r.Bottom),  // BL
                new(r.Left,  midY)       // L
            ];

            using Brush fill = new SolidBrush(Color.White);
            using Pen border = new Pen(Color.FromArgb(255, 230, 50, 50), 2);
            foreach (Point p in handles)
            {
                Rectangle hr = new Rectangle(p.X - half, p.Y - half, markerSize, markerSize);
                e.Graphics.FillRectangle(fill, hr);
                e.Graphics.DrawRectangle(border, hr);
            }
        }

        /// <summary>
        /// Draw size information of selection area
        /// </summary>
        private void DrawSizeInfo(PaintEventArgs e)
        {
            string sizeText = $"{_selectionRectangle.Width} × {_selectionRectangle.Height}";
            string hintText = "拖动框 / 八个句柄调整  ·  Enter 或双击确认  ·  Esc/右键取消";
            using (Font infoFont = new Font("Segoe UI", 12, FontStyle.Bold))
            using (Font hintFont = new Font("Segoe UI", 9, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Brush shadowBrush = new SolidBrush(Color.Black))
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                SizeF textSize = e.Graphics.MeasureString(sizeText, infoFont);
                SizeF hintSize = e.Graphics.MeasureString(hintText, hintFont);

                float bgWidth = Math.Max(textSize.Width, hintSize.Width) + 16;
                float bgHeight = textSize.Height + hintSize.Height + 12;

                // 默认放在选区下方；如果下面放不下就放在上方
                float bgX = _selectionRectangle.Left + (_selectionRectangle.Width - bgWidth) / 2;
                float bgY = _selectionRectangle.Bottom + 10;
                if (bgY + bgHeight > this.ClientSize.Height)
                {
                    bgY = _selectionRectangle.Top - bgHeight - 10;
                }
                bgX = Math.Clamp(bgX, 4, this.ClientSize.Width - bgWidth - 4);

                e.Graphics.FillRectangle(bgBrush, bgX, bgY, bgWidth, bgHeight);

                float textX = bgX + (bgWidth - textSize.Width) / 2;
                float textY = bgY + 4;
                e.Graphics.DrawString(sizeText, infoFont, shadowBrush, textX + 1, textY + 1);
                e.Graphics.DrawString(sizeText, infoFont, textBrush, textX, textY);

                float hintX = bgX + (bgWidth - hintSize.Width) / 2;
                float hintY = textY + textSize.Height + 2;
                e.Graphics.DrawString(hintText, hintFont, textBrush, hintX, hintY);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            else if (e.KeyCode == Keys.Enter && !_selectionRectangle.IsEmpty
                     && _selectionRectangle.Width > 0 && _selectionRectangle.Height > 0)
            {
                ConfirmSelection();
            }
        }

        private void ConfirmSelection()
        {
            if (_selectionRectangle.Width <= 0 || _selectionRectangle.Height <= 0)
            {
                return;
            }
            CaptureSelection();
            SelectionRegion = _selectionRectangle;
            SelectionScreenBounds = RectangleToScreen(_selectionRectangle);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Calculate rectangle from two points
        /// </summary>
        private Rectangle GetRectangleFromPoints(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 命中测试：判断鼠标位置在 8 个调整句柄、选区内部还是外部
        /// </summary>
        private HandleKind HitTest(Point p)
        {
            if (_selectionRectangle.IsEmpty) return HandleKind.None;

            Rectangle r = _selectionRectangle;
            int half = HandleSize / 2 + HandleHitPadding;

            // 8 个句柄
            if (new Rectangle(r.Left - half, r.Top - half, half * 2, half * 2).Contains(p)) return HandleKind.TopLeft;
            if (new Rectangle(r.Right - half, r.Top - half, half * 2, half * 2).Contains(p)) return HandleKind.TopRight;
            if (new Rectangle(r.Left - half, r.Bottom - half, half * 2, half * 2).Contains(p)) return HandleKind.BottomLeft;
            if (new Rectangle(r.Right - half, r.Bottom - half, half * 2, half * 2).Contains(p)) return HandleKind.BottomRight;

            int midX = r.Left + r.Width / 2;
            int midY = r.Top + r.Height / 2;
            if (new Rectangle(midX - half, r.Top - half, half * 2, half * 2).Contains(p)) return HandleKind.Top;
            if (new Rectangle(midX - half, r.Bottom - half, half * 2, half * 2).Contains(p)) return HandleKind.Bottom;
            if (new Rectangle(r.Left - half, midY - half, half * 2, half * 2).Contains(p)) return HandleKind.Left;
            if (new Rectangle(r.Right - half, midY - half, half * 2, half * 2).Contains(p)) return HandleKind.Right;

            // 内部
            if (r.Contains(p)) return HandleKind.Inside;
            return HandleKind.None;
        }

        private static Cursor CursorForHandle(HandleKind hk) => hk switch
        {
            HandleKind.TopLeft or HandleKind.BottomRight => Cursors.SizeNWSE,
            HandleKind.TopRight or HandleKind.BottomLeft => Cursors.SizeNESW,
            HandleKind.Top or HandleKind.Bottom => Cursors.SizeNS,
            HandleKind.Left or HandleKind.Right => Cursors.SizeWE,
            HandleKind.Inside => Cursors.SizeAll,
            _ => Cursors.Cross
        };

        private static Rectangle ResizeFromHandle(Rectangle start, HandleKind hk, Point cursor)
        {
            int left = start.Left, top = start.Top, right = start.Right, bottom = start.Bottom;
            switch (hk)
            {
                case HandleKind.TopLeft: left = cursor.X; top = cursor.Y; break;
                case HandleKind.Top: top = cursor.Y; break;
                case HandleKind.TopRight: right = cursor.X; top = cursor.Y; break;
                case HandleKind.Right: right = cursor.X; break;
                case HandleKind.BottomRight: right = cursor.X; bottom = cursor.Y; break;
                case HandleKind.Bottom: bottom = cursor.Y; break;
                case HandleKind.BottomLeft: left = cursor.X; bottom = cursor.Y; break;
                case HandleKind.Left: left = cursor.X; break;
            }
            return Rectangle.FromLTRB(
                Math.Min(left, right),
                Math.Min(top, bottom),
                Math.Max(left, right),
                Math.Max(top, bottom));
        }

        private Rectangle NormalizeAndClamp(Rectangle r)
        {
            if (r.Width < 0) r = new Rectangle(r.Right, r.Y, -r.Width, r.Height);
            if (r.Height < 0) r = new Rectangle(r.X, r.Bottom, r.Width, -r.Height);
            int x = Math.Max(0, r.X);
            int y = Math.Max(0, r.Y);
            int w = Math.Min(this.ClientSize.Width - x, r.Width);
            int h = Math.Min(this.ClientSize.Height - y, r.Height);
            if (w < 0) w = 0;
            if (h < 0) h = 0;
            return new Rectangle(x, y, w, h);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _fullScreenshot?.Dispose();
            _overlayBuffer?.Dispose();
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "RegionSelectorForm";
            this.Text = "RegionSelectorForm";
            this.ResumeLayout(false);
        }
    }
}
