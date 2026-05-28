using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public partial class Form1 : Form
    {
        private ScreenshotHelper _screenshotHelper = null!;
        private HotKeyManager _hotKeyManager = null!;

        public Form1()
        {
            InitializeComponent();
            _screenshotHelper = new ScreenshotHelper();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _hotKeyManager?.Dispose();
            _hotKeyManager = null!;
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Register global hotkey: Ctrl + Alt + Q
        /// </summary>
        private void RegisterHotKey()
        {
            try
            {
                _hotKeyManager?.Dispose();
                _hotKeyManager = new HotKeyManager(Handle);
                var modifiers = HotKeyManager.KeyModifiers.Ctrl
                              | HotKeyManager.KeyModifiers.Alt
                              | HotKeyManager.KeyModifiers.NoRepeat;
                _hotKeyManager.Register(modifiers, Keys.Q);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hotkey registration failed: {ex.Message}\n\nPlease restart the application.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Intercept WndProc messages for hotkey handling
        /// </summary>
        protected override void WndProc(ref Message message)
        {
            if (_hotKeyManager != null && _hotKeyManager.IsHotKeyMessage(message))
            {
                // Defer to avoid re-entrancy issues when starting modal dialogs from WndProc
                BeginInvoke(new Action(StartRegionScreenshot));
            }
            base.WndProc(ref message);
        }

        /// <summary>
        /// Start region screenshot (shared by hotkey and button)
        /// </summary>
        private void StartRegionScreenshot()
        {
            button1_Click(this, EventArgs.Empty);
        }

        /// <summary>
        /// Cleanup on form closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _hotKeyManager?.Dispose();
            _hotKeyManager = null!;
            base.OnFormClosing(e);
        }

        // Region Screenshot
        private void button1_Click(object sender, EventArgs e)
        {
            // Hide instantly without a visible flash (opacity 0 before DoEvents)
            double previousOpacity = Opacity;
            Opacity = 0;
            Hide();
            Application.DoEvents();

            try
            {
                var regionForm = new RegionSelectorForm();
                var result = regionForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var image = regionForm.CapturedImage;
                    if (image != null)
                    {
                        var editor = new AnnotationEditorForm(image, regionForm.SelectionScreenBounds);
                        var editorResult = editor.ShowDialog();

                        Show();
                        Opacity = previousOpacity;

                        if (editorResult == DialogResult.OK && editor.EditedImage != null)
                        {
                            _screenshotHelper.SaveCapturedImage(editor.EditedImage);
                            MessageBox.Show("Screenshot captured and edited successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            _screenshotHelper.SaveCapturedImage(image);
                            MessageBox.Show("Screenshot captured (edit cancelled)!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        return;
                    }
                }
            }
            finally
            {
                Show();
                Opacity = previousOpacity;
            }
        }

        // Save Screenshot
        private void button2_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Files|*.png|JPEG Files|*.jpg|All Files|*.*";
                sfd.FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _screenshotHelper.SaveToFile(sfd.FileName);
                }
            }
        }

        // Copy to Clipboard
        private void button3_Click(object sender, EventArgs e)
        {
            _screenshotHelper.CopyToClipboard();
        }

        /// <summary>
        /// Mouse enter - highlight button
        /// </summary>
        private void Button_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.BackColor = ControlPaint.Light(btn.BackColor, 0.2f);
            }
        }

        /// <summary>
        /// Mouse leave - restore button color
        /// </summary>
        private void Button_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset button colors
                if (btn.Name == "button1")
                    btn.BackColor = Color.FromArgb(0, 120, 212);
                else if (btn.Name == "button2")
                    btn.BackColor = Color.FromArgb(107, 105, 214);
                else if (btn.Name == "button3")
                    btn.BackColor = Color.FromArgb(59, 185, 72);
            }
        }
    }
}
