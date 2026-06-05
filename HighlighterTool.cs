using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public class HighlighterTool : IAnnotationTool
    {
        public string Name => "Highlighter";
        public Color ToolColor { get; set; } = Color.FromArgb(255, 235, 59);
        public int ToolSize { get; set; } = 12;

        private readonly List<Point> _points = new List<Point>();
        private bool _isDrawing;

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
            if (!_isDrawing)
            {
                return;
            }

            _isDrawing = false;
            _points.Add(e.Location);
            Commit(graphics, targetBitmap);
        }

        public void DrawPreview(Graphics graphics)
        {
            DrawStroke(graphics);
        }

        public void Commit(Graphics graphics, Bitmap targetBitmap)
        {
            DrawStroke(graphics);
            _points.Clear();
        }

        public void Reset()
        {
            _points.Clear();
            _isDrawing = false;
        }

        private void DrawStroke(Graphics graphics)
        {
            if (_points.Count < 2)
            {
                return;
            }

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingMode = CompositingMode.SourceOver;
            using var pen = new Pen(Color.FromArgb(95, ToolColor), Math.Max(8, ToolSize))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            graphics.DrawLines(pen, _points.ToArray());
        }
    }
}
