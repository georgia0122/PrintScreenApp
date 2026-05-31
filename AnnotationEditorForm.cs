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
        private readonly Rectangle _screenBounds;
        private Bitmap _originalImage = null!;
        private Bitmap _editingImage = null!;
        private DrawingManager _drawingManager = null!;
        private IAnnotationTool _currentTool = null!;
        private ToolbarForm _toolbar = null!;
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
            BackColor = Color.Black;
            TopMost = true;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = _screenBounds;
        }

        private void InitializeUI()
        {
            _canvasBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = _editingImage
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
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _toolbar.UpdatePosition(_screenBounds);
            _toolbar.Show();
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
        }

        private void PickColor()
        {
            using ColorDialog colorDialog = new ColorDialog { Color = _currentColor };
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                _currentColor = colorDialog.Color;
                UpdateToolColor();
            }
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
        }

        private void Redo()
        {
            if (_drawingManager.Redo())
            {
                _editingImage = _drawingManager.GetCurrentImage();
                _canvasBox.Image = _editingImage;
                _canvasBox.Invalidate();
            }
        }

        private void CanvasBox_MouseDown(object? sender, MouseEventArgs e)
        {
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
            _canvasBox.Invalidate();
        }

        private void CanvasBox_Paint(object? sender, PaintEventArgs e)
        {
            _currentTool.DrawPreview(e.Graphics);
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
            _toolbar?.Close();
            _toolbar?.Dispose();
            _drawingManager?.Dispose();
            _canvasBox?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
