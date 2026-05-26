using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Free drawing pen tool
    /// </summary>
    public class PenTool : IAnnotationTool
    {
        public string Name => "Pen";
        public Color ToolColor { get; set; } = Color.Red;
        public int ToolSize { get; set; } = 2;

        private List<Point> _points = new List<Point>();
        private bool _isDrawing = false;

        public void OnMouseDown(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            _isDrawing = true;
            _points.Clear();
            _points.Add(e.Location);
        }

        public void OnMouseMove(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (_isDrawing)
            {
                _points.Add(e.Location);
            }
        }

        public void OnMouseUp(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                Commit(graphics, targetBitmap);
            }
        }

        public void DrawPreview(Graphics graphics)
        {
            if (_points.Count < 2) return;

            using (Pen pen = new Pen(ToolColor, ToolSize) { SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias })
            {
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    graphics.DrawLine(pen, _points[i], _points[i + 1]);
                }
            }
        }

        public void Commit(Graphics graphics, Bitmap targetBitmap)
        {
            DrawPreview(graphics);
            _points.Clear();
        }

        public void Reset()
        {
            _points.Clear();
            _isDrawing = false;
        }
    }
}
