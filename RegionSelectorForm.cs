using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// 区域选择器窗体 - 全屏遮罩截图
    /// </summary>
    public partial class RegionSelectorForm : Form
    {
        private Bitmap _fullScreenshot;
        private Point _startPoint;
        private Point _endPoint;
        private bool _isSelecting = false;
        private Rectangle _selectionRectangle = Rectangle.Empty;
        private Bitmap _overlayBuffer;

        public Bitmap CapturedImage { get; private set; }
        public Rectangle SelectionRegion { get; private set; }

        public RegionSelectorForm()
        {
            InitializeComponent();
            ConfigureForm();
            CaptureFullScreen();
        }

        /// <summary>
        /// 配置窗体属性为全屏遮罩模式
        /// </summary>
        private void ConfigureForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.Opacity = 0.3;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
        }

        /// <summary>
        /// 捕获全屏截图
        /// </summary>
        private void CaptureFullScreen()
        {
            try
            {
                Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
                _fullScreenshot = new Bitmap(screenBounds.Width, screenBounds.Height);
                using (Graphics g = Graphics.FromImage(_fullScreenshot))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, screenBounds.Size);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"截图失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        /// 从选定区域捕获图像
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
                MessageBox.Show($"保存截图失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!_selectionRectangle.IsEmpty && _fullScreenshot != null)
            {
                try
                {
                    // 绘制遮罩层（黑色半透明）
                    DrawMaskLayer(e);

                    // 在选定区域内绘制原始截图（完全不透明）
                    DrawHighlightedRegion(e);

                    // 绘制选择框边框
                    DrawSelectionBorder(e);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"绘制错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 绘制黑色遮罩层
        /// </summary>
        private void DrawMaskLayer(PaintEventArgs e)
        {
            using (Brush maskBrush = new SolidBrush(Color.FromArgb(77, 0, 0, 0))) // 30% 透明度的黑色
            {
                e.Graphics.FillRectangle(maskBrush, this.ClientRectangle);
            }
        }

        /// <summary>
        /// 在选定矩形区域绘制原始截图（高亮显示）
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
        /// 绘制选择框边框
        /// </summary>
        private void DrawSelectionBorder(PaintEventArgs e)
        {
            using (Pen borderPen = new Pen(Color.FromArgb(255, 0, 120, 215), 2))
            {
                e.Graphics.DrawRectangle(borderPen, _selectionRectangle);
            }

            // 绘制四个角的标记
            DrawCornerMarkers(e);

            // 绘制尺寸信息
            DrawSizeInfo(e);
        }

        /// <summary>
        /// 绘制选择框四个角的标记
        /// </summary>
        private void DrawCornerMarkers(PaintEventArgs e)
        {
            const int markerSize = 6;
            using (Brush markerBrush = new SolidBrush(Color.FromArgb(255, 0, 120, 215)))
            {
                // 左上
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Left - markerSize / 2,
                    _selectionRectangle.Top - markerSize / 2,
                    markerSize, markerSize);

                // 右上
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Right - markerSize / 2,
                    _selectionRectangle.Top - markerSize / 2,
                    markerSize, markerSize);

                // 左下
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Left - markerSize / 2,
                    _selectionRectangle.Bottom - markerSize / 2,
                    markerSize, markerSize);

                // 右下
                e.Graphics.FillRectangle(markerBrush,
                    _selectionRectangle.Right - markerSize / 2,
                    _selectionRectangle.Bottom - markerSize / 2,
                    markerSize, markerSize);
            }
        }

        /// <summary>
        /// 绘制选择区域的尺寸信息
        /// </summary>
        private void DrawSizeInfo(PaintEventArgs e)
        {
            string sizeText = $"{_selectionRectangle.Width} × {_selectionRectangle.Height}";
            using (Font infoFont = new Font("微软雅黑", 12, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Brush shadowBrush = new SolidBrush(Color.Black))
            {
                // 计算文本位置（在选框下方中央）
                SizeF textSize = e.Graphics.MeasureString(sizeText, infoFont);
                float textX = _selectionRectangle.Left + (_selectionRectangle.Width - textSize.Width) / 2;
                float textY = _selectionRectangle.Bottom + 10;

                // 绘制阴影（偏移1像素）
                e.Graphics.DrawString(sizeText, infoFont, shadowBrush, textX + 1, textY + 1);

                // 绘制文本
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
        /// 根据两点计算矩形
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
