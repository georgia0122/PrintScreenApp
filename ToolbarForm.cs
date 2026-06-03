using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public enum AnnotationToolKind
    {
        Pen,
        Arrow,
        Mosaic,
        Rectangle,
        Circle
    }

    /// <summary>
    /// Floating annotation toolbar — draggable like iPadOS pencil palette.
    /// </summary>
    public class ToolbarForm : Form
    {
        private static readonly Color Background = Color.FromArgb(0x2D, 0x2D, 0x2D);
        private static readonly Color ButtonHover = Color.FromArgb(0x3E, 0x3E, 0x3E);
        private static readonly Color ActiveAccent = Color.FromArgb(0x00, 0x78, 0xD4);
        private static readonly Color SaveAccent = Color.FromArgb(0x2D, 0x8A, 0x45);
        private static readonly Color CancelAccent = Color.FromArgb(0xC4, 0x2B, 0x1C);
        private static readonly Color GripColor = Color.FromArgb(0x88, 0x88, 0x88);

        private const int ToolbarHeight = 44;
        private const int ButtonHeight = 32;
        private const int SymbolButtonWidth = 36;
        private const int HorizontalPadding = 8;
        private const int Gap = 4;
        private const int DragHandleWidth = 12;

        private AnnotationToolKind _activeTool = AnnotationToolKind.Pen;
        private Button? _activeButton;
        private Point _mouseOffset;
        private bool _isDragging;

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
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI Emoji", 11F, FontStyle.Regular);
            DoubleBuffered = true;

            MouseDown += Toolbar_MouseDown;
            MouseMove += Toolbar_MouseMove;
            MouseUp += Toolbar_MouseUp;
        }

        private void BuildToolbar()
        {
            var dragHandle = CreateDragHandle();
            Controls.Add(dragHandle);

            int x = DragHandleWidth + HorizontalPadding;
            int y = (ToolbarHeight - ButtonHeight) / 2;

            _activeButton = AddToolButton(ref x, y, "✏️", AnnotationToolKind.Pen);
            AddToolButton(ref x, y, "↗️", AnnotationToolKind.Arrow);
            AddToolButton(ref x, y, "▦", AnnotationToolKind.Mosaic);
            AddToolButton(ref x, y, "⬜", AnnotationToolKind.Rectangle);
            AddToolButton(ref x, y, "⭕", AnnotationToolKind.Circle);

            x += Gap;
            AddSeparator(x, y);
            x += 8;

            var colorBtn = CreateSymbolButton("🎨");
            colorBtn.Location = new Point(x, y);
            colorBtn.Click += (_, _) => ColorPickRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(colorBtn);
            x += colorBtn.Width + Gap;

            var sizeTracker = new TrackBar
            {
                Location = new Point(x, y - 2),
                Size = new Size(80, 32),
                Minimum = 1,
                Maximum = 15,
                Value = 2,
                TickStyle = TickStyle.None,
                BackColor = Background
            };
            sizeTracker.ValueChanged += (_, _) => BrushSizeChanged?.Invoke(this, sizeTracker.Value);
            Controls.Add(sizeTracker);
            x += sizeTracker.Width + Gap;

            AddSeparator(x, y);
            x += 8;

            var undoBtn = CreateSymbolButton("↩");
            undoBtn.Location = new Point(x, y);
            undoBtn.Click += (_, _) => UndoRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(undoBtn);
            x += undoBtn.Width + Gap;

            var redoBtn = CreateSymbolButton("↪");
            redoBtn.Location = new Point(x, y);
            redoBtn.Click += (_, _) => RedoRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(redoBtn);
            x += redoBtn.Width + Gap;

            AddSeparator(x, y);
            x += 8;

            var saveBtn = CreateSymbolButton("✔️");
            saveBtn.Location = new Point(x, y);
            saveBtn.BackColor = SaveAccent;
            saveBtn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(SaveAccent, 0.15f);
            saveBtn.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(saveBtn);
            x += saveBtn.Width + Gap;

            var cancelBtn = CreateSymbolButton("❌");
            cancelBtn.Location = new Point(x, y);
            cancelBtn.BackColor = CancelAccent;
            cancelBtn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(CancelAccent, 0.15f);
            cancelBtn.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(cancelBtn);
            x += cancelBtn.Width + HorizontalPadding;

            ClientSize = new Size(x, ToolbarHeight);
        }

        private Panel CreateDragHandle()
        {
            var handle = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DragHandleWidth, ToolbarHeight),
                BackColor = Background,
                Cursor = Cursors.SizeAll
            };
            handle.Paint += DragHandle_Paint;
            WireDragEvents(handle);
            return handle;
        }

        private void DragHandle_Paint(object? sender, PaintEventArgs e)
        {
            const int dotSize = 3;
            const int dotGap = 4;
            int centerX = DragHandleWidth / 2;
            int totalHeight = dotSize * 3 + dotGap * 2;
            int startY = (ToolbarHeight - totalHeight) / 2;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(GripColor);
            for (int i = 0; i < 3; i++)
            {
                int y = startY + i * (dotSize + dotGap);
                e.Graphics.FillEllipse(brush, centerX - dotSize / 2, y, dotSize, dotSize);
            }
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

            Point clientPoint = sender is Control ctrl && ctrl != this
                ? PointToClient(ctrl.PointToScreen(e.Location))
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

            Location = new Point(
                MousePosition.X + _mouseOffset.X,
                MousePosition.Y + _mouseOffset.Y);
        }

        private void Toolbar_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            EndDrag();
        }

        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            Capture = false;
        }

        private Button AddToolButton(ref int x, int y, string symbol, AnnotationToolKind kind)
        {
            var btn = CreateSymbolButton(symbol);
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

        private void ToolButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is AnnotationToolKind kind)
            {
                SetActiveButton(btn, kind);
                ToolSelected?.Invoke(this, kind);
            }
        }

        public void SetActiveTool(AnnotationToolKind kind)
        {
            foreach (Control control in Controls)
            {
                if (control is Button btn && btn.Tag is AnnotationToolKind toolKind && toolKind == kind)
                {
                    SetActiveButton(btn, kind);
                    return;
                }
            }
        }

        private void SetActiveButton(Button btn, AnnotationToolKind kind)
        {
            foreach (Control control in Controls)
            {
                if (control is Button toolBtn && toolBtn.Tag is AnnotationToolKind)
                {
                    toolBtn.BackColor = Background;
                    toolBtn.ForeColor = Color.White;
                }
            }

            _activeTool = kind;
            _activeButton = btn;
            btn.BackColor = ActiveAccent;
            btn.ForeColor = Color.White;
        }

        private Button CreateSymbolButton(string symbol)
        {
            var btn = new Button
            {
                Text = symbol,
                Size = new Size(SymbolButtonWidth, ButtonHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Background,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                TabStop = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ButtonHover;
            return btn;
        }

        private void AddSeparator(int x, int y)
        {
            Controls.Add(new Panel
            {
                BackColor = Color.FromArgb(0x55, 0x55, 0x55),
                Location = new Point(x, y + 4),
                Size = new Size(1, ButtonHeight - 8)
            });
        }

        /// <summary>
        /// Initial placement near the screenshot selection (screen coordinates).
        /// User can freely reposition afterward by dragging.
        /// </summary>
        public void UpdatePosition(Rectangle selectionRect)
        {
            const int gap = 8;

            Screen screen = Screen.FromRectangle(selectionRect);
            Rectangle work = screen.WorkingArea;

            int desiredLeft = selectionRect.Left + (selectionRect.Width - Width) / 2;
            int belowTop = selectionRect.Bottom + gap;
            int aboveTop = selectionRect.Top - Height - gap;
            int desiredTop = belowTop + Height <= work.Bottom ? belowTop : aboveTop;

            if (desiredTop < work.Top)
            {
                desiredTop = work.Bottom - Height;
            }

            Left = Clamp(desiredLeft, work.Left, work.Right - Width);
            Top = Clamp(desiredTop, work.Top, work.Bottom - Height);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (max < min)
            {
                return min;
            }

            return Math.Max(min, Math.Min(value, max));
        }
    }
}
