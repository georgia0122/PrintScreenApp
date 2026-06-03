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
        private Bitmap _fullScreenshot = null!;
        private Point _startPoint;
        private Point _endPoint;
        private bool _isSelecting = false;
        private Rectangle _selectionRectangle = Rectangle.Empty;
        private Bitmap _overlayBuffer = null!;

        public Bitmap CapturedImage { get; private set; } = null!;
        public Rectangle SelectionRegion { get; private set; }
        public Rectangle SelectionScreenBounds { get; private set; }

        public RegionSelectorForm()
        {
            InitializeComponent();
            ConfigureForm();
            CaptureFullScreen();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_LAYERED = 0x00080000;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED;
                return cp;
            }
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
            if (e.Button == MouseButtons.Left)
            {
                _isSelecting = true;
                _startPoint = e.Location;
                _selectionRectangle = Rectangle.Empty;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isSelecting)
            {
                _endPoint = e.Location;
                _selectionRectangle = GetRectangleFromPoints(_startPoint, _endPoint);
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _isSelecting)
            {
                _isSelecting = false;
                _endPoint = e.Location;
                _selectionRectangle = GetRectangleFromPoints(_startPoint, _endPoint);

                if (_selectionRectangle.Width > 0 && _selectionRectangle.Height > 0)
                {
                    CaptureSelection();
                    SelectionRegion = _selectionRectangle;
                    SelectionScreenBounds = RectangleToScreen(_selectionRectangle);
                    this.DialogResult = DialogResult.OK;
                }
                this.Close();
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
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
            using (Brush maskBrush = new SolidBrush(Color.FromArgb(77, 0, 0, 0))) // 30% transparent black
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
            using (Pen borderPen = new Pen(Color.FromArgb(255, 0, 120, 215), 2))
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
            const int markerSize = 6;
            using (Brush markerBrush = new SolidBrush(Color.FromArgb(255, 0, 120, 215)))
            {
                // Top-left
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Left - markerSize / 2,
                    _selectionRectangle.Top - markerSize / 2,
                    markerSize, markerSize);

                // Top-right
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Right - markerSize / 2,
                    _selectionRectangle.Top - markerSize / 2,
                    markerSize, markerSize);

                // Bottom-left
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Left - markerSize / 2,
                    _selectionRectangle.Bottom - markerSize / 2,
                    markerSize, markerSize);

                // Bottom-right
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Right - markerSize / 2,
                    _selectionRectangle.Bottom - markerSize / 2,
                    markerSize, markerSize);
            }
        }

        /// <summary>
        /// Draw size information of selection area
        /// </summary>
        private void DrawSizeInfo(PaintEventArgs e)
        {
            string sizeText = $"{_selectionRectangle.Width} × {_selectionRectangle.Height}";
            using (Font infoFont = new Font("Segoe UI", 12, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Brush shadowBrush = new SolidBrush(Color.Black))
            {
                // Calculate text position (center below selection box)
                SizeF textSize = e.Graphics.MeasureString(sizeText, infoFont);
                float textX = _selectionRectangle.Left + (_selectionRectangle.Width - textSize.Width) / 2;
                float textY = _selectionRectangle.Bottom + 10;

                // Draw shadow (offset by 1 pixel)
                e.Graphics.DrawString(sizeText, infoFont, shadowBrush, textX + 1, textY + 1);

                // Draw text
                e.Graphics.DrawString(sizeText, infoFont, textBrush, textX, textY);
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
