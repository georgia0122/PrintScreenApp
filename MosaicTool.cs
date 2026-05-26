using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Mosaic blur tool
    /// </summary>
    public class MosaicTool : IAnnotationTool
    {
        public string Name => "Mosaic";
        public Color ToolColor { get; set; } = Color.Black;
        public int ToolSize { get; set; } = 10;

        private Point _startPoint;
        private Point _endPoint;
        private bool _isDrawing = false;
        private Rectangle _mosaicRect = Rectangle.Empty;

        public void OnMouseDown(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            _isDrawing = true;
            _startPoint = e.Location;
            _endPoint = e.Location;
        }

        public void OnMouseMove(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (_isDrawing)
            {
                _endPoint = e.Location;
                _mosaicRect = GetRectangleFromPoints(_startPoint, _endPoint);
            }
        }

        public void OnMouseUp(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                _endPoint = e.Location;
                _mosaicRect = GetRectangleFromPoints(_startPoint, _endPoint);
                if (_mosaicRect.Width > 0 && _mosaicRect.Height > 0)
                {
                    Commit(graphics, targetBitmap);
                }
            }
        }

        public void DrawPreview(Graphics graphics)
        {
            if (!_isDrawing || _mosaicRect.IsEmpty) return;

            DrawMosaicRegion(graphics, _mosaicRect);
        }

        public void Commit(Graphics graphics, Bitmap targetBitmap)
        {
            if (_mosaicRect.IsEmpty) return;

            ApplyMosaicEffect(targetBitmap, _mosaicRect);
            DrawMosaicRegion(graphics, _mosaicRect);
        }

        public void Reset()
        {
            _isDrawing = false;
            _mosaicRect = Rectangle.Empty;
        }

        private Rectangle GetRectangleFromPoints(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, width, height);
        }

        private void ApplyMosaicEffect(Bitmap bitmap, Rectangle rect)
        {
            int blockSize = ToolSize;
            
            for (int y = rect.Top; y < rect.Bottom; y += blockSize)
            {
                for (int x = rect.Left; x < rect.Right; x += blockSize)
                {
                    Rectangle blockRect = new Rectangle(
                        x, y,
                        Math.Min(blockSize, rect.Right - x),
                        Math.Min(blockSize, rect.Bottom - y)
                    );

                    Color avgColor = GetAverageColor(bitmap, blockRect);

                    // Fill block with average color
                    using (Brush brush = new SolidBrush(avgColor))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.FillRectangle(brush, blockRect);
                        }
                    }
                }
            }
        }

        private Color GetAverageColor(Bitmap bitmap, Rectangle rect)
        {
            long r = 0, g = 0, b = 0;
            int count = 0;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    if (bitmap.Width > x && bitmap.Height > y && x >= 0 && y >= 0)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        r += pixel.R;
                        g += pixel.G;
                        b += pixel.B;
                        count++;
                    }
                }
            }

            if (count == 0) return Color.Black;
            return Color.FromArgb(
                (int)(r / count),
                (int)(g / count),
                (int)(b / count)
            );
        }

        private void DrawMosaicRegion(Graphics graphics, Rectangle rect)
        {
            using (Brush brush = new HatchBrush(System.Drawing.Drawing2D.HatchStyle.DarkDownwardDiagonal, Color.Gray, Color.LightGray))
            {
                graphics.FillRectangle(brush, rect);
            }
            using (Pen pen = new Pen(Color.LightGray, 1))
            {
                graphics.DrawRectangle(pen, rect);
            }
        }
    }
}
