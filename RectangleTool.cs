using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Rectangle shape tool
    /// </summary>
    public class RectangleTool : IAnnotationTool
    {
        public string Name => "Rectangle";
        public Color ToolColor { get; set; } = Color.Red;
        public int ToolSize { get; set; } = 2;

        private Point _startPoint;
        private Point _currentPoint;
        private bool _isDrawing = false;

        public void OnMouseDown(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            _isDrawing = true;
            _startPoint = e.Location;
            _currentPoint = e.Location;
        }

        public void OnMouseMove(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (_isDrawing)
            {
                _currentPoint = e.Location;
            }
        }

        public void OnMouseUp(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                _currentPoint = e.Location;
                Commit(graphics, targetBitmap);
            }
        }

        public void DrawPreview(Graphics graphics)
        {
            if (!_isDrawing) return;

            Rectangle rect = GetRectangleFromPoints(_startPoint, _currentPoint);
            using (Pen pen = new Pen(ToolColor, ToolSize) { SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias })
            {
                graphics.DrawRectangle(pen, rect);
            }
        }

        public void Commit(Graphics graphics, Bitmap targetBitmap)
        {
            Rectangle rect = GetRectangleFromPoints(_startPoint, _currentPoint);
            using (Pen pen = new Pen(ToolColor, ToolSize) { SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias })
            {
                graphics.DrawRectangle(pen, rect);
            }
        }

        public void Reset()
        {
            _isDrawing = false;
        }

        private Rectangle GetRectangleFromPoints(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, width, height);
        }
    }
}
