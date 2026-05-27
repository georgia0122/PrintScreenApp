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
    /// Floating annotation toolbar shown near the screenshot selection.
    /// </summary>
    public class ToolbarForm : Form
    {
        private static readonly Color Background = Color.FromArgb(0x2D, 0x2D, 0x2D);
        private static readonly Color ButtonHover = Color.FromArgb(0x3E, 0x3E, 0x3E);
        private static readonly Color ActiveAccent = Color.FromArgb(0x00, 0x78, 0xD4);
        private static readonly Color SaveAccent = Color.FromArgb(0x2D, 0x8A, 0x45);
        private static readonly Color CancelAccent = Color.FromArgb(0xC4, 0x2B, 0x1C);

        private const int ToolbarHeight = 44;
        private const int ButtonHeight = 32;
        private const int HorizontalPadding = 8;
        private const int Gap = 6;

        private AnnotationToolKind _activeTool = AnnotationToolKind.Pen;
        private Button? _activeButton;

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
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            DoubleBuffered = true;
        }

        private void BuildToolbar()
        {
            int x = HorizontalPadding;
            int y = (ToolbarHeight - ButtonHeight) / 2;

            _activeButton = AddToolButton(ref x, y, "Pen", AnnotationToolKind.Pen);
            AddToolButton(ref x, y, "Arrow", AnnotationToolKind.Arrow);
            AddToolButton(ref x, y, "Mosaic", AnnotationToolKind.Mosaic);
            AddToolButton(ref x, y, "Rect", AnnotationToolKind.Rectangle);
            AddToolButton(ref x, y, "Circle", AnnotationToolKind.Circle);

            x += Gap;
            AddSeparator(x, y);
            x += 10;

            var colorBtn = CreateButton("Color", 52);
            colorBtn.Location = new Point(x, y);
            colorBtn.Click += (_, _) => ColorPickRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(colorBtn);
            x += colorBtn.Width + Gap;

            var sizeLabel = new Label
            {
                Text = "Size",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Background,
                Location = new Point(x, y + 8)
            };
            Controls.Add(sizeLabel);
            x += sizeLabel.Width + 4;

            var sizeTracker = new TrackBar
            {
                Location = new Point(x, y - 2),
                Size = new Size(90, 32),
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
            x += 10;

            var undoBtn = CreateButton("Undo", 52);
            undoBtn.Location = new Point(x, y);
            undoBtn.Click += (_, _) => UndoRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(undoBtn);
            x += undoBtn.Width + Gap;

            var redoBtn = CreateButton("Redo", 52);
            redoBtn.Location = new Point(x, y);
            redoBtn.Click += (_, _) => RedoRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(redoBtn);
            x += redoBtn.Width + Gap;

            AddSeparator(x, y);
            x += 10;

            var saveBtn = CreateButton("Save", 52);
            saveBtn.Location = new Point(x, y);
            saveBtn.BackColor = SaveAccent;
            saveBtn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(SaveAccent, 0.15f);
            saveBtn.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(saveBtn);
            x += saveBtn.Width + Gap;

            var cancelBtn = CreateButton("Cancel", 58);
            cancelBtn.Location = new Point(x, y);
            cancelBtn.BackColor = CancelAccent;
            cancelBtn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(CancelAccent, 0.15f);
            cancelBtn.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(cancelBtn);
            x += cancelBtn.Width + HorizontalPadding;

            ClientSize = new Size(x, ToolbarHeight);
        }

        private Button AddToolButton(ref int x, int y, string text, AnnotationToolKind kind)
        {
            var btn = CreateButton(text, 58);
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

        private Button CreateButton(string text, int width)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, ButtonHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Background,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                TabStop = false
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
        /// Place toolbar below the selection; if there is not enough room, place at the inside top of the selection.
        /// </summary>
        public void PositionNearSelection(Rectangle selectionScreenRect)
        {
            Screen screen = Screen.FromRectangle(selectionScreenRect);
            Rectangle workingArea = screen.WorkingArea;
            const int gap = 8;

            int x = selectionScreenRect.Left + (selectionScreenRect.Width - Width) / 2;
            x = Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - Width));

            int belowY = selectionScreenRect.Bottom + gap;
            int y;

            if (belowY + Height <= workingArea.Bottom)
            {
                y = belowY;
            }
            else
            {
                y = selectionScreenRect.Top + gap;
            }

            Location = new Point(x, y);
        }
    }
}
