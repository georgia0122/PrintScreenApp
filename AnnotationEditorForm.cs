using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// WeChat/QQ style annotation editor form
    /// </summary>
    public partial class AnnotationEditorForm : Form
    {
        private Bitmap _originalImage = null!;
        private Bitmap _editingImage = null!;
        private DrawingManager _drawingManager = null!;
        private IAnnotationTool _currentTool = null!;
        private Panel _toolbarPanel = null!;
        private PictureBox _canvasBox = null!;
        private Color _currentColor = Color.Red;
        private int _currentSize = 2;

        // Tools
        private PenTool _penTool = null!;
        private ArrowTool _arrowTool = null!;
        private MosaicTool _mosaicTool = null!;
        private RectangleTool _rectangleTool = null!;
        private CircleTool _circleTool = null!;

        public Bitmap EditedImage { get; private set; } = null!;

        public AnnotationEditorForm(Bitmap image)
        {
            InitializeComponent();
            _originalImage = (Bitmap)image.Clone();
            _editingImage = (Bitmap)image.Clone();
            _drawingManager = new DrawingManager(image);
            InitializeTools();
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

            _currentTool = _penTool;
        }

        private void InitializeForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.FromPoint(Cursor.Position).WorkingArea;
        }

        private void InitializeUI()
        {
            // Create canvas
            _canvasBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = _editingImage
            };
            _canvasBox.Paint += CanvasBox_Paint;
            _canvasBox.MouseDown += CanvasBox_MouseDown;
            _canvasBox.MouseMove += CanvasBox_MouseMove;
            _canvasBox.MouseUp += CanvasBox_MouseUp;

            // Create toolbar at top
            _toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.None
            };

            CreateToolbarButtons();

            // Add top-docked toolbar first, then Fill canvas, so the toolbar keeps its space
            this.Controls.Add(_toolbarPanel);
            this.Controls.Add(_canvasBox);
            _toolbarPanel.BringToFront();
        }

        private void CreateToolbarButtons()
        {
            int x = 10;
            int toolbarHeight = _toolbarPanel.Height;

            // Tool buttons
            var tools = new (string name, IAnnotationTool tool, Keys key)[]
            {
                ("Pen (1)", _penTool, Keys.D1),
                ("Arrow (2)", _arrowTool, Keys.D2),
                ("Mosaic (3)", _mosaicTool, Keys.D3),
                ("Rect (4)", _rectangleTool, Keys.D4),
                ("Circle (5)", _circleTool, Keys.D5),
            };

            foreach (var (name, tool, _) in tools)
            {
                var btn = new Button
                {
                    Text = name,
                    Location = new Point(x, 10),
                    Size = new Size(70, 30),
                    BackColor = tool == _currentTool ? Color.SkyBlue : Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Tag = tool
                };

                btn.Click += (s, e) => SelectTool((IAnnotationTool)btn.Tag, btn);
                _toolbarPanel.Controls.Add(btn);
                x += 80;
            }

            x += 20;

            // Color picker
            var colorBtn = new Button
            {
                Text = "Color",
                Location = new Point(x, 10),
                Size = new Size(70, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            colorBtn.Click += ColorBtn_Click;
            _toolbarPanel.Controls.Add(colorBtn);
            x += 80;

            // Size selector
            var sizeLabel = new Label
            {
                Text = "Size:",
                Location = new Point(x, 15),
                Size = new Size(35, 20),
                AutoSize = true
            };
            _toolbarPanel.Controls.Add(sizeLabel);

            var sizeTracker = new TrackBar
            {
                Location = new Point(x + 40, 10),
                Size = new Size(100, 30),
                Minimum = 1,
                Maximum = 15,
                Value = _currentSize,
                TickStyle = TickStyle.None
            };
            sizeTracker.ValueChanged += (s, e) => {
                _currentSize = sizeTracker.Value;
                UpdateToolSize();
            };
            _toolbarPanel.Controls.Add(sizeTracker);

            x += 150;

            // Undo/Redo
            var undoBtn = new Button
            {
                Text = "↶ Undo",
                Location = new Point(x, 10),
                Size = new Size(70, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            undoBtn.Click += (s, e) => {
                if (_drawingManager.Undo())
                {
                    _editingImage = _drawingManager.GetCurrentImage();
                    _canvasBox.Image = _editingImage;
                    _canvasBox.Invalidate();
                }
            };
            _toolbarPanel.Controls.Add(undoBtn);
            x += 80;

            var redoBtn = new Button
            {
                Text = "↷ Redo",
                Location = new Point(x, 10),
                Size = new Size(70, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            redoBtn.Click += (s, e) => {
                if (_drawingManager.Redo())
                {
                    _editingImage = _drawingManager.GetCurrentImage();
                    _canvasBox.Image = _editingImage;
                    _canvasBox.Invalidate();
                }
            };
            _toolbarPanel.Controls.Add(redoBtn);
            x += 80;

            // Confirm/Cancel
            var confirmBtn = new Button
            {
                Text = "✓ Save",
                Location = new Point(x, 10),
                Size = new Size(70, 30),
                BackColor = Color.LimeGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            confirmBtn.Click += (s, e) => {
                EditedImage = _editingImage;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            _toolbarPanel.Controls.Add(confirmBtn);
            x += 80;

            var cancelBtn = new Button
            {
                Text = "✕ Cancel",
                Location = new Point(x, 10),
                Size = new Size(70, 30),
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            cancelBtn.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            _toolbarPanel.Controls.Add(cancelBtn);
        }

        private void SelectTool(IAnnotationTool tool, Button button)
        {
            _currentTool = tool;

            // Update button colors
            foreach (Button btn in _toolbarPanel.Controls.OfType<Button>())
            {
                if (btn.Tag is IAnnotationTool t && t == tool)
                {
                    btn.BackColor = Color.SkyBlue;
                }
                else if (btn.Tag is IAnnotationTool)
                {
                    btn.BackColor = Color.White;
                }
            }

            _drawingManager.SaveState();
        }

        private void ColorBtn_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog { Color = _currentColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    _currentColor = colorDialog.Color;
                    UpdateToolColor();
                }
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

        private void CanvasBox_MouseDown(object sender, MouseEventArgs e)
        {
            using (Graphics g = Graphics.FromImage(_editingImage))
            {
                _currentTool.OnMouseDown(e, g, _editingImage);
            }
            _canvasBox.Invalidate();
        }

        private void CanvasBox_MouseMove(object sender, MouseEventArgs e)
        {
            using (Graphics g = Graphics.FromImage(_editingImage))
            {
                _currentTool.OnMouseMove(e, g, _editingImage);
            }
            _canvasBox.Invalidate();
        }

        private void CanvasBox_MouseUp(object sender, MouseEventArgs e)
        {
            using (Graphics g = Graphics.FromImage(_editingImage))
            {
                _currentTool.OnMouseUp(e, g, _editingImage);
            }
            _canvasBox.Invalidate();
        }

        private void CanvasBox_Paint(object sender, PaintEventArgs e)
        {
            _currentTool.DrawPreview(e.Graphics);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.D1: SelectTool(_penTool, null); break;
                case Keys.D2: SelectTool(_arrowTool, null); break;
                case Keys.D3: SelectTool(_mosaicTool, null); break;
                case Keys.D4: SelectTool(_rectangleTool, null); break;
                case Keys.D5: SelectTool(_circleTool, null); break;
                case Keys.Z when (e.Control):
                    if (_drawingManager.Undo())
                    {
                        _editingImage = _drawingManager.GetCurrentImage();
                        _canvasBox.Image = _editingImage;
                        _canvasBox.Invalidate();
                    }
                    break;
                case Keys.Y when (e.Control):
                    if (_drawingManager.Redo())
                    {
                        _editingImage = _drawingManager.GetCurrentImage();
                        _canvasBox.Image = _editingImage;
                        _canvasBox.Invalidate();
                    }
                    break;
                case Keys.Escape: 
                    this.DialogResult = DialogResult.Cancel; 
                    this.Close(); 
                    break;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _drawingManager?.Dispose();
            _canvasBox?.Dispose();
            _toolbarPanel?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
