using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public enum AnnotationToolKind
    {
        Pen,
        Arrow,
        Mosaic,
        Rectangle,
        Circle,
        Highlighter,
        Eraser
    }

    internal enum ToolbarIcon
    {
        Pen,
        Arrow,
        Mosaic,
        Rectangle,
        Circle,
        Highlighter,
        Eraser,
        Color,
        Undo,
        Redo,
        Save,
        Cancel
    }

    /// <summary>
    /// Floating annotation toolbar with screen-edge aware placement.
    /// </summary>
    public class ToolbarForm : Form
    {
        private static readonly Color Background = Color.FromArgb(252, 252, 252);
        private static readonly Color ButtonIdle = Color.FromArgb(252, 252, 252);
        private static readonly Color ButtonHover = Color.FromArgb(241, 243, 245);
        private static readonly Color ButtonActive = Color.FromArgb(231, 245, 255);
        private static readonly Color Stroke = Color.FromArgb(34, 36, 38);
        private static readonly Color ActiveStroke = Color.FromArgb(0, 132, 255);
        private static readonly Color Separator = Color.FromArgb(224, 226, 229);
        private static readonly Color SaveAccent = Color.FromArgb(0, 190, 108);
        private static readonly Color CancelAccent = Color.FromArgb(255, 76, 82);
        private const int ToolbarHeight = 74;
        private const int ButtonSize = 48;
        private const int SidePadding = 18;
        private const int Gap = 6;
        private const int GroupGap = 14;
        private const int DragHandleWidth = 12;
        private const int CornerRadius = 10;
        private const int EdgeMargin = 12;
        private const int OutsideGap = 10;

        private static readonly IntPtr HwndTopMost = new IntPtr(-1);
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExTopMost = 0x00000008;
        private const int WsExNoActivate = 0x08000000;

        private readonly ToolTip _toolTip = new ToolTip();
        private AnnotationToolKind _activeTool = AnnotationToolKind.Pen;
        private IconButton? _activeButton;
        private Rectangle _lastScreenWorkArea = Rectangle.Empty;
        private Point _mouseOffset;
        private bool _isDragging;
        private bool _userMoved;

        public event EventHandler<AnnotationToolKind>? ToolSelected;
        public event EventHandler? ColorPickRequested;
        public event EventHandler<int>? BrushSizeChanged;
        public event EventHandler? UndoRequested;
        public event EventHandler? RedoRequested;
        public event EventHandler? SaveRequested;
        public event EventHandler? CancelRequested;

        public ToolbarForm()
        {
            InitializeForm();
            BuildToolbar();
        }

        private void InitializeForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Background;
            ForeColor = Stroke;
            DoubleBuffered = true;
            Padding = Padding.Empty;

            MouseDown += Toolbar_MouseDown;
            MouseMove += Toolbar_MouseMove;
            MouseUp += Toolbar_MouseUp;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WsExToolWindow | WsExTopMost | WsExNoActivate;
                return cp;
            }
        }

        private void BuildToolbar()
        {
            SuspendLayout();
            Controls.Clear();

            var handle = CreateDragHandle();
            Controls.Add(handle);

            int x = DragHandleWidth + SidePadding;
            int y = (ToolbarHeight - ButtonSize) / 2;

            AddToolButton(ref x, y, ToolbarIcon.Rectangle, AnnotationToolKind.Rectangle, "Rectangle");
            AddToolButton(ref x, y, ToolbarIcon.Circle, AnnotationToolKind.Circle, "Circle");
            AddToolButton(ref x, y, ToolbarIcon.Arrow, AnnotationToolKind.Arrow, "Arrow");
            _activeButton = AddToolButton(ref x, y, ToolbarIcon.Pen, AnnotationToolKind.Pen, "Pen");
            AddToolButton(ref x, y, ToolbarIcon.Highlighter, AnnotationToolKind.Highlighter, "Highlighter");
            AddToolButton(ref x, y, ToolbarIcon.Eraser, AnnotationToolKind.Eraser, "Eraser");
            AddToolButton(ref x, y, ToolbarIcon.Mosaic, AnnotationToolKind.Mosaic, "Mosaic");

            AddSeparator(ref x, y);

            var colorBtn = CreateIconButton(ToolbarIcon.Color, "Color");
            colorBtn.Location = new Point(x, y);
            colorBtn.Click += (_, _) => ColorPickRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(colorBtn);
            x += colorBtn.Width + Gap;

            var sizeTracker = new TrackBar
            {
                Location = new Point(x, y + 2),
                Size = new Size(104, ButtonSize - 8),
                Minimum = 1,
                Maximum = 15,
                Value = 2,
                SmallChange = 1,
                LargeChange = 2,
                TickStyle = TickStyle.None,
                BackColor = Background,
                TabStop = false
            };
            sizeTracker.ValueChanged += (_, _) => BrushSizeChanged?.Invoke(this, sizeTracker.Value);
            Controls.Add(sizeTracker);
            x += sizeTracker.Width + Gap;

            AddSeparator(ref x, y);

            var undoBtn = CreateIconButton(ToolbarIcon.Undo, "Undo");
            undoBtn.Location = new Point(x, y);
            undoBtn.Click += (_, _) => UndoRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(undoBtn);
            x += undoBtn.Width + Gap;

            var redoBtn = CreateIconButton(ToolbarIcon.Redo, "Redo");
            redoBtn.Location = new Point(x, y);
            redoBtn.Click += (_, _) => RedoRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(redoBtn);
            x += redoBtn.Width + Gap;

            AddSeparator(ref x, y);

            var saveBtn = CreateIconButton(ToolbarIcon.Save, "Save");
            saveBtn.Location = new Point(x, y);
            saveBtn.HoverBackColor = Color.FromArgb(235, 255, 246);
            saveBtn.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(saveBtn);
            x += saveBtn.Width + Gap;

            var cancelBtn = CreateIconButton(ToolbarIcon.Cancel, "Cancel");
            cancelBtn.Location = new Point(x, y);
            cancelBtn.HoverBackColor = Color.FromArgb(255, 239, 240);
            cancelBtn.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(cancelBtn);
            x += cancelBtn.Width + SidePadding;

            ClientSize = new Size(x, ToolbarHeight);
            ApplyRoundedRegion();
            ResumeLayout(false);
        }

        private Panel CreateDragHandle()
        {
            var handle = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DragHandleWidth + 4, ToolbarHeight),
                BackColor = Background,
                Cursor = Cursors.SizeAll
            };
            handle.Paint += DragHandle_Paint;
            WireDragEvents(handle);
            return handle;
        }

        private void DragHandle_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(150, 153, 158));

            const int dotSize = 3;
            const int dotGap = 5;
            int centerX = DragHandleWidth / 2 + 1;
            int startY = (ToolbarHeight - dotSize * 3 - dotGap * 2) / 2;

            for (int i = 0; i < 3; i++)
            {
                int y = startY + i * (dotSize + dotGap);
                e.Graphics.FillEllipse(brush, centerX - dotSize / 2, y, dotSize, dotSize);
            }
        }

        private IconButton AddToolButton(ref int x, int y, ToolbarIcon icon, AnnotationToolKind kind, string tip)
        {
            var btn = CreateIconButton(icon, tip);
            btn.Location = new Point(x, y);
            btn.Tag = kind;
            btn.Click += ToolButton_Click;
            Controls.Add(btn);

            if (kind == _activeTool)
            {
                SetActiveButton(btn, kind);
            }

            x += btn.Width + Gap;
            return btn;
        }

        private IconButton CreateIconButton(ToolbarIcon icon, string tip)
        {
            var btn = new IconButton(icon)
            {
                Size = new Size(ButtonSize, ButtonSize),
                NormalBackColor = ButtonIdle,
                HoverBackColor = ButtonHover,
                ActiveBackColor = ButtonActive,
                IconColor = Stroke,
                ActiveIconColor = ActiveStroke,
                Cursor = Cursors.Hand,
                TabStop = false
            };

            _toolTip.SetToolTip(btn, tip);
            return btn;
        }

        private void AddSeparator(ref int x, int y)
        {
            x += GroupGap - Gap;
            Controls.Add(new Panel
            {
                BackColor = Separator,
                Location = new Point(x, y + 8),
                Size = new Size(1, ButtonSize - 16)
            });
            x += GroupGap;
        }

        private void ToolButton_Click(object? sender, EventArgs e)
        {
            if (sender is IconButton btn && btn.Tag is AnnotationToolKind kind)
            {
                SetActiveButton(btn, kind);
                ToolSelected?.Invoke(this, kind);
            }
        }

        public void SetActiveTool(AnnotationToolKind kind)
        {
            foreach (Control control in Controls)
            {
                if (control is IconButton btn && btn.Tag is AnnotationToolKind toolKind && toolKind == kind)
                {
                    SetActiveButton(btn, kind);
                    return;
                }
            }
        }

        private void SetActiveButton(IconButton btn, AnnotationToolKind kind)
        {
            foreach (Control control in Controls)
            {
                if (control is IconButton toolBtn && toolBtn.Tag is AnnotationToolKind)
                {
                    toolBtn.IsActive = false;
                }
            }

            _activeTool = kind;
            _activeButton = btn;
            btn.IsActive = true;
        }

        private void WireDragEvents(Control control)
        {
            control.MouseDown += Toolbar_MouseDown;
            control.MouseMove += Toolbar_MouseMove;
            control.MouseUp += Toolbar_MouseUp;
        }

        private void Toolbar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (sender is Control sourceControl && sourceControl != this && sourceControl.Cursor != Cursors.SizeAll)
            {
                return;
            }

            Point clientPoint = sender is Control childControl && childControl != this
                ? PointToClient(childControl.PointToScreen(e.Location))
                : e.Location;

            _mouseOffset = new Point(-clientPoint.X, -clientPoint.Y);
            _isDragging = true;
            Capture = true;
        }

        private void Toolbar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            var target = new Point(
                MousePosition.X + _mouseOffset.X,
                MousePosition.Y + _mouseOffset.Y);

            Location = ClampToWorkArea(target);
        }

        private void Toolbar_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
                Capture = false;
                _userMoved = true;
                Location = ClampToWorkArea(Location);
                EnsureVisibleOnTop();
            }
        }

        public void UpdatePosition(Rectangle selectionRect)
        {
            Screen screen = Screen.FromRectangle(selectionRect);
            _lastScreenWorkArea = screen.WorkingArea;

            if (_userMoved)
            {
                Location = ClampToWorkArea(Location);
                EnsureVisibleOnTop();
                return;
            }

            Location = GetOutsidePosition(selectionRect, _lastScreenWorkArea);
            EnsureVisibleOnTop();
        }

        public void EnsureVisibleOnTop()
        {
            if (!Visible)
            {
                Show();
            }

            TopMost = true;
            SetWindowPos(Handle, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var borderPen = new Pen(Color.FromArgb(75, Color.White), 1);
            using var path = CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
            e.Graphics.DrawPath(borderPen, path);
        }

        private Point ClampToWorkArea(Point point)
        {
            Rectangle work = _lastScreenWorkArea == Rectangle.Empty
                ? Screen.FromPoint(point).WorkingArea
                : _lastScreenWorkArea;

            int minLeft = work.Left + EdgeMargin;
            int maxLeft = work.Right - Width - EdgeMargin;
            int minTop = work.Top + EdgeMargin;
            int maxTop = work.Bottom - Height - EdgeMargin;

            return new Point(
                Clamp(point.X, minLeft, maxLeft),
                Clamp(point.Y, minTop, maxTop));
        }

        private Point GetOutsidePosition(Rectangle selectionRect, Rectangle work)
        {
            int centeredLeft = selectionRect.Left + (selectionRect.Width - Width) / 2;

            if (selectionRect.Bottom + OutsideGap + Height <= work.Bottom - EdgeMargin)
            {
                return ClampToWorkArea(new Point(centeredLeft, selectionRect.Bottom + OutsideGap));
            }

            if (selectionRect.Top - OutsideGap - Height >= work.Top + EdgeMargin)
            {
                return ClampToWorkArea(new Point(centeredLeft, selectionRect.Top - Height - OutsideGap));
            }

            int centeredTop = selectionRect.Top + (selectionRect.Height - Height) / 2;
            if (selectionRect.Right + OutsideGap + Width <= work.Right - EdgeMargin)
            {
                return ClampToWorkArea(new Point(selectionRect.Right + OutsideGap, centeredTop));
            }

            if (selectionRect.Left - OutsideGap - Width >= work.Left + EdgeMargin)
            {
                return ClampToWorkArea(new Point(selectionRect.Left - Width - OutsideGap, centeredTop));
            }

            int fallbackLeft = selectionRect.Left + (selectionRect.Width - Width) / 2;
            int fallbackTop = work.Bottom - Height - EdgeMargin;
            return ClampToWorkArea(new Point(fallbackLeft, fallbackTop));
        }

        private void ApplyRoundedRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            Region?.Dispose();
            using var path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius);
            Region = new Region(path);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (max < min)
            {
                return min;
            }

            return Math.Max(min, Math.Min(value, max));
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);

        private sealed class IconButton : Button
        {
            private bool _hovered;
            private bool _pressed;
            private bool _isActive;

            public IconButton(ToolbarIcon icon)
            {
                Icon = icon;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                Text = string.Empty;
                UseVisualStyleBackColor = false;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            }

            public ToolbarIcon Icon { get; }
            public Color NormalBackColor { get; set; } = ButtonIdle;
            public Color HoverBackColor { get; set; } = ButtonHover;
            public Color ActiveBackColor { get; set; } = ButtonActive;
            public Color IconColor { get; set; } = Stroke;
            public Color ActiveIconColor { get; set; } = ActiveStroke;

            public bool IsActive
            {
                get => _isActive;
                set
                {
                    _isActive = value;
                    Invalidate();
                }
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                _hovered = true;
                Invalidate();
                base.OnMouseEnter(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hovered = false;
                _pressed = false;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                _pressed = true;
                Invalidate();
                base.OnMouseDown(mevent);
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                _pressed = false;
                Invalidate();
                base.OnMouseUp(mevent);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                Color fill = IsActive ? ActiveBackColor : (_hovered ? HoverBackColor : NormalBackColor);
                if (_pressed)
                {
                    fill = ControlPaint.Dark(fill, 0.08f);
                }

                using (var bg = new SolidBrush(fill))
                using (var path = CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 7))
                {
                    e.Graphics.FillPath(bg, path);
                }

                DrawIcon(e.Graphics, ClientRectangle, Icon, IsActive ? ActiveIconColor : IconColor);
            }

            private static void DrawIcon(Graphics g, Rectangle bounds, ToolbarIcon icon, Color color)
            {
                using var pen = new Pen(color, 2.6f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                using var thickPen = new Pen(color, 3.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                using var brush = new SolidBrush(color);

                float cx = bounds.Left + bounds.Width / 2f;
                float cy = bounds.Top + bounds.Height / 2f;

                switch (icon)
                {
                    case ToolbarIcon.Pen:
                        g.DrawLine(thickPen, cx - 10, cy + 11, cx + 9, cy - 8);
                        g.DrawLine(pen, cx + 6, cy - 12, cx + 13, cy - 5);
                        g.FillEllipse(brush, cx - 12, cy + 10, 4, 4);
                        break;
                    case ToolbarIcon.Arrow:
                        g.DrawLine(pen, cx - 11, cy + 10, cx + 11, cy - 12);
                        g.DrawLine(pen, cx + 11, cy - 12, cx + 11, cy - 1);
                        g.DrawLine(pen, cx + 11, cy - 12, cx, cy - 12);
                        break;
                    case ToolbarIcon.Highlighter:
                        using (var marker = new Pen(Color.FromArgb(190, 255, 214, 64), 8f)
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round
                        })
                        {
                            g.DrawLine(marker, cx - 10, cy + 8, cx + 11, cy - 10);
                        }
                        g.DrawLine(pen, cx - 11, cy + 10, cx + 10, cy - 11);
                        g.DrawLine(pen, cx + 6, cy - 14, cx + 14, cy - 6);
                        break;
                    case ToolbarIcon.Eraser:
                        using (var path = new GraphicsPath())
                        {
                            PointF[] eraser =
                            [
                                new(cx - 13, cy + 4),
                                new(cx - 3, cy - 11),
                                new(cx + 13, cy),
                                new(cx + 3, cy + 15)
                            ];
                            path.AddPolygon(eraser);
                            g.DrawPath(pen, path);
                            g.DrawLine(pen, cx - 4, cy + 13, cx + 12, cy + 13);
                        }
                        break;
                    case ToolbarIcon.Mosaic:
                        for (int row = 0; row < 3; row++)
                        {
                            for (int col = 0; col < 3; col++)
                            {
                                int shade = 60 + ((row + col) % 2) * 120;
                                using var cellBrush = new SolidBrush(Color.FromArgb(shade, shade, shade));
                                g.FillRectangle(cellBrush, cx - 12 + col * 8, cy - 12 + row * 8, 6, 6);
                            }
                        }
                        break;
                    case ToolbarIcon.Rectangle:
                        g.DrawRectangle(pen, cx - 13, cy - 11, 26, 22);
                        break;
                    case ToolbarIcon.Circle:
                        g.DrawEllipse(pen, cx - 13, cy - 13, 26, 26);
                        break;
                    case ToolbarIcon.Color:
                        using (var colorWheel = new LinearGradientBrush(
                            new Rectangle((int)cx - 13, (int)cy - 13, 26, 26),
                            Color.FromArgb(255, 70, 70),
                            Color.FromArgb(60, 145, 255),
                            LinearGradientMode.ForwardDiagonal))
                        {
                            g.FillEllipse(colorWheel, cx - 12, cy - 12, 24, 24);
                        }
                        g.DrawEllipse(pen, cx - 12, cy - 12, 24, 24);
                        break;
                    case ToolbarIcon.Undo:
                        using (var undoPen = new Pen(Color.FromArgb(170, color), 2.6f)
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round,
                            LineJoin = LineJoin.Round
                        })
                        {
                            g.DrawArc(undoPen, cx - 12, cy - 10, 24, 20, 210, 260);
                            g.DrawLine(undoPen, cx - 10, cy - 4, cx - 15, cy - 11);
                            g.DrawLine(undoPen, cx - 10, cy - 4, cx - 3, cy - 6);
                        }
                        break;
                    case ToolbarIcon.Redo:
                        g.DrawArc(pen, cx - 12, cy - 10, 24, 20, -110, 260);
                        g.DrawLine(pen, cx + 10, cy - 4, cx + 15, cy - 11);
                        g.DrawLine(pen, cx + 10, cy - 4, cx + 3, cy - 6);
                        break;
                    case ToolbarIcon.Save:
                        using (var savePen = new Pen(SaveAccent, 3.2f)
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round,
                            LineJoin = LineJoin.Round
                        })
                        {
                            g.DrawLine(savePen, cx - 11, cy, cx - 3, cy + 9);
                            g.DrawLine(savePen, cx - 3, cy + 9, cx + 13, cy - 11);
                        }
                        break;
                    case ToolbarIcon.Cancel:
                        using (var cancelPen = new Pen(CancelAccent, 3.2f)
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round
                        })
                        {
                            g.DrawLine(cancelPen, cx - 11, cy - 11, cx + 11, cy + 11);
                            g.DrawLine(cancelPen, cx + 11, cy - 11, cx - 11, cy + 11);
                        }
                        break;
                }
            }
        }
    }
}
