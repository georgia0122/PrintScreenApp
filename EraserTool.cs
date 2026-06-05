using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public class EraserTool : IAnnotationTool, ISourceImageTool
    {
        public string Name => "Eraser";
        public Color ToolColor { get; set; } = Color.White;
        public int ToolSize { get; set; } = 18;
        public Bitmap SourceImage { get; set; }

        private readonly List<Point> _points = new List<Point>();
        private bool _isDrawing;

        public EraserTool(Bitmap sourceImage)
        {
            SourceImage = sourceImage;
        }

        public void OnMouseDown(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            _isDrawing = true;
            _points.Clear();
            _points.Add(e.Location);
            RestoreAt(graphics, targetBitmap, e.Location);
        }

        public void OnMouseMove(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (!_isDrawing)
            {
                return;
            }

            _points.Add(e.Location);
            RestoreStroke(graphics, targetBitmap);
        }

        public void OnMouseUp(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap)
        {
            if (!_isDrawing)
            {
                return;
            }

            _isDrawing = false;
            _points.Add(e.Location);
            RestoreStroke(graphics, targetBitmap);
            _points.Clear();
        }

        public void DrawPreview(Graphics graphics)
        {
            if (_points.Count == 0)
            {
                return;
            }

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(160, 255, 255, 255), Math.Max(10, ToolSize))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using var outline = new Pen(Color.FromArgb(160, 40, 40, 40), 1.5f);

            if (_points.Count > 1)
            {
                graphics.DrawLines(pen, _points.ToArray());
            }

            Point p = _points[^1];
            int size = Math.Max(10, ToolSize);
            graphics.DrawEllipse(outline, p.X - size / 2, p.Y - size / 2, size, size);
        }

        public void Commit(Graphics graphics, Bitmap targetBitmap)
        {
            RestoreStroke(graphics, targetBitmap);
            _points.Clear();
        }

        public void Reset()
        {
            _points.Clear();
            _isDrawing = false;
        }

        private void RestoreAt(Graphics graphics, Bitmap targetBitmap, Point point)
        {
            int size = Math.Max(10, ToolSize);
            using var path = new GraphicsPath();
            path.AddEllipse(point.X - size / 2, point.Y - size / 2, size, size);
            RestorePath(graphics, path, targetBitmap);
        }

        private void RestoreStroke(Graphics graphics, Bitmap targetBitmap)
        {
            if (_points.Count == 0)
            {
                return;
            }

            if (_points.Count == 1)
            {
                RestoreAt(graphics, targetBitmap, _points[0]);
                return;
            }

            using var path = new GraphicsPath();
            using var pen = new Pen(Color.Black, Math.Max(10, ToolSize))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            path.AddLines(_points.ToArray());
            path.Widen(pen);
            RestorePath(graphics, path, targetBitmap);
        }

        private void RestorePath(Graphics graphics, GraphicsPath path, Bitmap targetBitmap)
        {
            if (SourceImage == null)
            {
                return;
            }

            Region oldClip = graphics.Clip;
            using var clip = new Region(path);
            graphics.SetClip(clip, CombineMode.Replace);
            graphics.DrawImage(SourceImage, new Rectangle(Point.Empty, targetBitmap.Size));
            graphics.Clip = oldClip;
            oldClip.Dispose();
        }
    }
}
