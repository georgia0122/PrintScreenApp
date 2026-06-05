using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Annotation canvas overlay with a separate floating toolbar.
    /// </summary>
    public partial class AnnotationEditorForm : Form
    {
        // 外描边宽度，让用户能看清批注区域的边界
        private const int BorderThickness = 4;
        private static readonly Color BorderColor = Color.FromArgb(255, 230, 50, 50);

        private readonly Rectangle _screenBounds;
        private Bitmap _originalImage = null!;
        private Bitmap _editingImage = null!;
        private DrawingManager _drawingManager = null!;
        private IAnnotationTool _currentTool = null!;
        private ToolbarForm _toolbar = null!;
        private System.Windows.Forms.Timer _toolbarKeeper = null!;
        private PictureBox _canvasBox = null!;
        private Color _currentColor = Color.Red;
        private int _currentSize = 2;

        private PenTool _penTool = null!;
        private ArrowTool _arrowTool = null!;
        private MosaicTool _mosaicTool = null!;
        private RectangleTool _rectangleTool = null!;
        private CircleTool _circleTool = null!;

        public Bitmap EditedImage { get; private set; } = null!;

        public AnnotationEditorForm(Bitmap image, Rectangle screenBounds)
        {
            _screenBounds = screenBounds;
            InitializeComponent();
            _originalImage = (Bitmap)image.Clone();
            _editingImage = (Bitmap)image.Clone();
            _drawingManager = new DrawingManager(image);
            InitializeTools();
            InitializeForm();
            InitializeUI();
            InitializeToolbar();
        }

        private void InitializeTools()
        {
            _penTool = new PenTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _arrowTool = new ArrowTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _mosaicTool = new MosaicTool { ToolColor = _currentColor, ToolSize = 10 };
            _rectangleTool = new RectangleTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _circleTool = new CircleTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _currentTool = _penTool;
        }

        private void InitializeForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = BorderColor; // 表单背景充当边框
            TopMost = true;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;

            // 在选区外拓宽边框厚度，同时限制在屏幕内
            Rectangle screen = Screen.FromRectangle(_screenBounds).Bounds;
            Rectangle expanded = Rectangle.Inflate(_screenBounds, BorderThickness, BorderThickness);
            expanded.Intersect(screen);
            Bounds = expanded;
        }

        private void InitializeUI()
        {
            _canvasBox = new PictureBox
            {
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = _editingImage,
                Location = new Point(BorderThickness, BorderThickness),
                Size = new Size(
                    Math.Max(1, ClientSize.Width - BorderThickness * 2),
                    Math.Max(1, ClientSize.Height - BorderThickness * 2)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _canvasBox.Paint += CanvasBox_Paint;
            _canvasBox.MouseDown += CanvasBox_MouseDown;
            _canvasBox.MouseMove += CanvasBox_MouseMove;
            _canvasBox.MouseUp += CanvasBox_MouseUp;
            Controls.Add(_canvasBox);
        }

        private void InitializeToolbar()
        {
            _toolbar = new ToolbarForm();
            _toolbar.ToolSelected += (_, kind) => SelectTool(GetTool(kind));
            _toolbar.ColorPickRequested += (_, _) => PickColor();
            _toolbar.BrushSizeChanged += (_, size) =>
            {
                _currentSize = size;
                UpdateToolSize();
            };
            _toolbar.UndoRequested += (_, _) => Undo();
            _toolbar.RedoRequested += (_, _) => Redo();
            _toolbar.SaveRequested += (_, _) =>
            {
                EditedImage = _editingImage;
                DialogResult = DialogResult.OK;
                Close();
            };
            _toolbar.CancelRequested += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            _toolbarKeeper = new System.Windows.Forms.Timer { Interval = 250 };
            _toolbarKeeper.Tick += (_, _) => KeepToolbarVisible();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _toolbar.UpdatePosition(_screenBounds);
            _toolbar.Show(this);
            _toolbarKeeper.Start();
            KeepToolbarVisible();
        }

        private IAnnotationTool GetTool(AnnotationToolKind kind) => kind switch
        {
            AnnotationToolKind.Pen => _penTool,
            AnnotationToolKind.Arrow => _arrowTool,
            AnnotationToolKind.Mosaic => _mosaicTool,
            AnnotationToolKind.Rectangle => _rectangleTool,
            AnnotationToolKind.Circle => _circleTool,
            _ => _penTool
        };

        private AnnotationToolKind GetToolKind(IAnnotationTool tool)
        {
            if (tool == _penTool) return AnnotationToolKind.Pen;
            if (tool == _arrowTool) return AnnotationToolKind.Arrow;
            if (tool == _mosaicTool) return AnnotationToolKind.Mosaic;
            if (tool == _rectangleTool) return AnnotationToolKind.Rectangle;
            if (tool == _circleTool) return AnnotationToolKind.Circle;
            return AnnotationToolKind.Pen;
        }

        private void SelectTool(IAnnotationTool tool)
        {
            _currentTool = tool;
            _toolbar.SetActiveTool(GetToolKind(tool));
            _drawingManager.SaveState();
            KeepToolbarVisible();
        }

        private void PickColor()
        {
            using ColorDialog colorDialog = new ColorDialog { Color = _currentColor };
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                _currentColor = colorDialog.Color;
                UpdateToolColor();
            }

            KeepToolbarVisible();
        }

        private void UpdateToolColor()
        {
            _penTool.ToolColor = _currentColor;
            _arrowTool.ToolColor = _currentColor;
            _rectangleTool.ToolColor = _currentColor;
            _circleTool.ToolColor = _currentColor;
        }

        private void UpdateToolSize()
        {
            _penTool.ToolSize = _currentSize;
            _arrowTool.ToolSize = _currentSize;
            _rectangleTool.ToolSize = _currentSize;
            _circleTool.ToolSize = _currentSize;
        }

        private void Undo()
        {
            if (_drawingManager.Undo())
            {
                _editingImage = _drawingManager.GetCurrentImage();
                _canvasBox.Image = _editingImage;
                _canvasBox.Invalidate();
            }

            KeepToolbarVisible();
        }

        private void Redo()
        {
            if (_drawingManager.Redo())
            {
                _editingImage = _drawingManager.GetCurrentImage();
                _canvasBox.Image = _editingImage;
                _canvasBox.Invalidate();
            }

            KeepToolbarVisible();
        }

        private void CanvasBox_MouseDown(object? sender, MouseEventArgs e)
        {
            using Graphics g = Graphics.FromImage(_editingImage);
            _currentTool.OnMouseDown(e, g, _editingImage);
            _canvasBox.Invalidate();
            KeepToolbarVisible();
        }

        private void CanvasBox_MouseMove(object? sender, MouseEventArgs e)
        {
            using Graphics g = Graphics.FromImage(_editingImage);
            _currentTool.OnMouseMove(e, g, _editingImage);
            _canvasBox.Invalidate();
        }

        private void CanvasBox_MouseUp(object? sender, MouseEventArgs e)
        {
            using Graphics g = Graphics.FromImage(_editingImage);
            _currentTool.OnMouseUp(e, g, _editingImage);
            _canvasBox.Invalidate();
            KeepToolbarVisible();
        }

        private void CanvasBox_Paint(object? sender, PaintEventArgs e)
        {
            _currentTool.DrawPreview(e.Graphics);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            KeepToolbarVisible();
        }

        private void KeepToolbarVisible()
        {
            if (_toolbar == null || _toolbar.IsDisposed)
            {
                return;
            }

            if (!_toolbar.Visible)
            {
                _toolbar.Show(this);
            }

            _toolbar.TopMost = true;
            _toolbar.BringToFront();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.D1: SelectTool(_penTool); break;
                case Keys.D2: SelectTool(_arrowTool); break;
                case Keys.D3: SelectTool(_mosaicTool); break;
                case Keys.D4: SelectTool(_rectangleTool); break;
                case Keys.D5: SelectTool(_circleTool); break;
                case Keys.Z when e.Control: Undo(); break;
                case Keys.Y when e.Control: Redo(); break;
                case Keys.Escape:
                    DialogResult = DialogResult.Cancel;
                    Close();
                    break;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _toolbarKeeper?.Stop();
            _toolbarKeeper?.Dispose();
            _toolbar?.Close();
            _toolbar?.Dispose();
            _drawingManager?.Dispose();
            _canvasBox?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
