using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Arrow drawing tool
    /// </summary>
    public class ArrowTool : IAnnotationTool
    {
        public string Name => "Arrow";
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

            DrawArrow(graphics, _startPoint, _currentPoint);
        }

        public void Commit(Graphics graphics, Bitmap targetBitmap)
        {
            DrawArrow(graphics, _startPoint, _currentPoint);
        }

        public void Reset()
        {
            _isDrawing = false;
        }

        private void DrawArrow(Graphics graphics, Point from, Point to)
        {
            const int arrowSize = 10;
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(ToolColor, ToolSize))
            {
                graphics.DrawLine(pen, from, to);

                // Draw arrowhead
                Point arrowPoint1 = new Point(
                    (int)(to.X - arrowSize * Math.Cos(angle - Math.PI / 6)),
                    (int)(to.Y - arrowSize * Math.Sin(angle - Math.PI / 6))
                );
                Point arrowPoint2 = new Point(
                    (int)(to.X - arrowSize * Math.Cos(angle + Math.PI / 6)),
                    (int)(to.Y - arrowSize * Math.Sin(angle + Math.PI / 6))
                );

                graphics.DrawLine(pen, to, arrowPoint1);
                graphics.DrawLine(pen, to, arrowPoint2);

                using (Brush brush = new SolidBrush(ToolColor))
                {
                    Point[] arrowHead = { to, arrowPoint1, arrowPoint2 };
                    graphics.FillPolygon(brush, arrowHead);
                }
            }
        }
    }
}
