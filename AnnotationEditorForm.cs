using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Annotation canvas overlay with an embedded floating toolbar.
    /// </summary>
    public partial class AnnotationEditorForm : Form
    {
        private readonly Rectangle _screenBounds;
        private Rectangle _hostBounds;
        private Bitmap _originalImage = null!;
        private Bitmap _editingImage = null!;
        private DrawingManager _drawingManager = null!;
        private IAnnotationTool _currentTool = null!;
        private AnnotationToolbarControl _toolbar = null!;
        private System.Windows.Forms.Timer _toolbarKeeper = null!;
        private PictureBox _canvasBox = null!;
        private Rectangle _toolbarScreenBounds;
        private Color _currentColor = Color.Red;
        private int _currentSize = 2;

        private PenTool _penTool = null!;
        private ArrowTool _arrowTool = null!;
        private MosaicTool _mosaicTool = null!;
        private RectangleTool _rectangleTool = null!;
        private CircleTool _circleTool = null!;
        private HighlighterTool _highlighterTool = null!;
        private EraserTool _eraserTool = null!;

        public Bitmap EditedImage { get; private set; } = null!;

        public AnnotationEditorForm(Bitmap image, Rectangle screenBounds)
        {
            _screenBounds = screenBounds;
            InitializeComponent();
            _originalImage = (Bitmap)image.Clone();
            _editingImage = (Bitmap)image.Clone();
            _drawingManager = new DrawingManager(image);
            InitializeTools();
            InitializeToolbar();
            InitializeForm();
            InitializeUI();
        }

        private void InitializeTools()
        {
            _penTool = new PenTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _arrowTool = new ArrowTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _mosaicTool = new MosaicTool { ToolColor = _currentColor, ToolSize = 10 };
            _rectangleTool = new RectangleTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _circleTool = new CircleTool { ToolColor = _currentColor, ToolSize = _currentSize };
            _highlighterTool = new HighlighterTool { ToolSize = 14 };
            _eraserTool = new EraserTool(_originalImage) { ToolSize = 20 };
            _currentTool = _penTool;
        }

        private void InitializeForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;

            _hostBounds = Screen.FromRectangle(_screenBounds).Bounds;
            _toolbarScreenBounds = _toolbar.GetPreferredScreenBounds(_screenBounds);
            _hostBounds = Rectangle.Union(_screenBounds, _toolbarScreenBounds);
            _hostBounds.Inflate(6, 6);
            Bounds = _hostBounds;

            BackColor = Color.FromArgb(1, 1, 1);
            TransparencyKey = BackColor;
        }

        private void InitializeUI()
        {
            _canvasBox = new PictureBox
            {
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = _editingImage,
                Location = new Point(_screenBounds.Left - _hostBounds.Left, _screenBounds.Top - _hostBounds.Top),
                Size = new Size(Math.Max(1, _screenBounds.Width), Math.Max(1, _screenBounds.Height)),
                Anchor = AnchorStyles.None
            };
            _canvasBox.Paint += CanvasBox_Paint;
            _canvasBox.MouseDown += CanvasBox_MouseDown;
            _canvasBox.MouseMove += CanvasBox_MouseMove;
            _canvasBox.MouseUp += CanvasBox_MouseUp;
            Controls.Add(_canvasBox);
        }

        private void InitializeToolbar()
        {
            _toolbar = new AnnotationToolbarControl();
            _toolbar.ToolSelected += (_, kind) => SelectTool(GetTool(kind));
            _toolbar.ColorPickRequested += (_, _) => PickColor();
            _toolbar.BrushSizeChanged += (_, size) =>
            {
                _currentSize = size;
                UpdateToolSize();
            };
            _toolbar.UndoRequested += (_, _) => Undo();
            _toolbar.RedoRequested += (_, _) => Redo();
            _toolbar.SaveRequested += (_, _) => Finish(DialogResult.OK);
            _toolbar.CancelRequested += (_, _) => Finish(DialogResult.Cancel);

            _toolbarKeeper = new System.Windows.Forms.Timer { Interval = 1000 };
            _toolbarKeeper.Tick += (_, _) => KeepToolbarVisible();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Controls.Add(_toolbar);
            _toolbar.Location = new Point(_toolbarScreenBounds.Left - _hostBounds.Left, _toolbarScreenBounds.Top - _hostBounds.Top);
            _toolbar.Show();
            _toolbar.BringToFront();
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
            AnnotationToolKind.Highlighter => _highlighterTool,
            AnnotationToolKind.Eraser => _eraserTool,
            _ => _penTool
        };

        private AnnotationToolKind GetToolKind(IAnnotationTool tool)
        {
            if (tool == _penTool) return AnnotationToolKind.Pen;
            if (tool == _arrowTool) return AnnotationToolKind.Arrow;
            if (tool == _mosaicTool) return AnnotationToolKind.Mosaic;
            if (tool == _rectangleTool) return AnnotationToolKind.Rectangle;
            if (tool == _circleTool) return AnnotationToolKind.Circle;
            if (tool == _highlighterTool) return AnnotationToolKind.Highlighter;
            if (tool == _eraserTool) return AnnotationToolKind.Eraser;
            return AnnotationToolKind.Pen;
        }

        private void SelectTool(IAnnotationTool tool)
        {
            _currentTool = tool;
            _toolbar.SetActiveTool(GetToolKind(tool));
            KeepToolbarVisible();
        }

        private void PickColor()
        {
            using ColorDialog colorDialog = new ColorDialog { Color = _currentColor };
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
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
            _highlighterTool.ToolColor = _currentColor;
        }

        private void UpdateToolSize()
        {
            _penTool.ToolSize = _currentSize;
            _arrowTool.ToolSize = _currentSize;
            _rectangleTool.ToolSize = _currentSize;
            _circleTool.ToolSize = _currentSize;
            _highlighterTool.ToolSize = Math.Max(8, _currentSize * 4);
            _eraserTool.ToolSize = Math.Max(12, _currentSize * 5);
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
            _drawingManager.SaveState(_editingImage);
            using Graphics g = Graphics.FromImage(_editingImage);
            _currentTool.OnMouseDown(e, g, _editingImage);
            _canvasBox.Invalidate();
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
            _drawingManager.SetCurrentImage(_editingImage);
            _canvasBox.Invalidate();
            KeepToolbarVisible();
        }

        private void CanvasBox_Paint(object? sender, PaintEventArgs e)
        {
            _currentTool.DrawPreview(e.Graphics);
            using var borderPen = new Pen(Color.FromArgb(255, 230, 50, 50), 3);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(borderPen, 1, 1, _canvasBox.Width - 3, _canvasBox.Height - 3);
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

            if (!_toolbar.Visible) _toolbar.Visible = true;
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
                case Keys.D6: SelectTool(_highlighterTool); break;
                case Keys.D7: SelectTool(_eraserTool); break;
                case Keys.Z when e.Control: Undo(); break;
                case Keys.Y when e.Control: Redo(); break;
                case Keys.Enter: Finish(DialogResult.OK); break;
                case Keys.Escape: Finish(DialogResult.Cancel); break;
            }
        }

        private void Finish(DialogResult result)
        {
            if (result == DialogResult.OK)
            {
                EditedImage = _editingImage;
            }

            DialogResult = result;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _toolbarKeeper?.Stop();
            _toolbarKeeper?.Dispose();
            _toolbar?.Dispose();
            _drawingManager?.Dispose();
            _canvasBox?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
