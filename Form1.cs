using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public partial class Form1 : Form
    {
        private ScreenshotHelper _screenshotHelper;
        private HotKeyManager _hotKeyManager;

        public Form1()
        {
            InitializeComponent();
            _screenshotHelper = new ScreenshotHelper();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeHotKey();
        }

        /// <summary>
        /// Initialize hotkey: Alt + Q
        /// </summary>
        private void InitializeHotKey()
        {
            try
            {
                _hotKeyManager = new HotKeyManager(Handle);
                var modifiers = HotKeyManager.KeyModifiers.Alt;
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
                ShowOrActivate();
            }
            base.WndProc(ref message);
        }

        /// <summary>
        /// Show or activate the main form
        /// </summary>
        private void ShowOrActivate()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
            BringToFront();
        }

        /// <summary>
        /// Cleanup on form closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Explicitly unregister hotkey to prevent occupying the hotkey slot
            if (_hotKeyManager != null)
            {
                _hotKeyManager.Unregister();
                _hotKeyManager.Dispose();
                _hotKeyManager = null;
            }
            base.OnFormClosing(e);
        }

        // Region Screenshot
        private void button1_Click(object sender, EventArgs e)
        {
            // Hide main window
            this.Hide();
            
            // Process pending UI messages
            Application.DoEvents();
            
            // Wait 200ms to ensure window is completely hidden
            System.Threading.Thread.Sleep(200);
            
            // Show region selector
            var regionForm = new RegionSelectorForm();
            var result = regionForm.ShowDialog();
            
            // Show main window
            this.Show();
            
            if (result == DialogResult.OK)
            {
                var image = regionForm.CapturedImage;
                if (image != null)
                {
                    _screenshotHelper.SaveCapturedImage(image);
                    MessageBox.Show("Screenshot captured successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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
