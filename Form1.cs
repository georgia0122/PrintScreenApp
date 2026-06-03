using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public partial class Form1 : Form
    {
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly ScreenshotHelper _screenshotHelper;
        private readonly HotKeyDefinition[] _hotKeyCandidates =
        [
            new("Ctrl + Alt + Z", HotKeyManager.KeyModifiers.Ctrl | HotKeyManager.KeyModifiers.Alt, Keys.Z),
            new("Ctrl + Alt + B", HotKeyManager.KeyModifiers.Ctrl | HotKeyManager.KeyModifiers.Alt, Keys.B)
        ];

        private readonly List<int> _registeredHotKeyIds = [];
        private GlobalKeyboardHook? _keyboardHook;
        private NotifyIcon? _trayIcon;
        private string _registeredHotKeys = "";
        private bool _isManuallyShowed;
        private bool _isCapturing;

        public Form1()
        {
            InitializeComponent();
            _screenshotHelper = new ScreenshotHelper();

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示", null, (_, _) => ShowWindow());
            contextMenu.Items.Add("隐藏", null, (_, _) => HideWindow());
            contextMenu.Items.Add("立即截图", null, (_, _) => BeginInvoke(new Action(StartRegionScreenshot)));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (_, _) => ExitApplication());
            ContextMenuStrip = contextMenu;

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "PrintScreenApp",
                Visible = true,
                ContextMenuStrip = contextMenu
            };
            _trayIcon.DoubleClick += (_, _) => ShowWindow();

            ShowInTaskbar = true;
            Opacity = 0;
            WindowState = FormWindowState.Minimized;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InitializeHotKey();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
                value = false;
            }

            if (!_isManuallyShowed)
            {
                value = false;
            }

            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DisposeHotKeys();
            _keyboardHook?.Dispose();
            _keyboardHook = null;
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            // 第二实例启动时会广播这个消息，让现有窗口显示出来
            if (m.Msg != 0 && m.Msg == (int)Program.ShowAppMessageId)
            {
                BeginInvoke(new Action(ShowWindow));
            }
            else if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                int idx = _registeredHotKeyIds.IndexOf(id);
                if (idx >= 0 && idx < _hotKeyCandidates.Length)
                {
                    Log($"Hotkey triggered: {_hotKeyCandidates[idx].DisplayName}");
                    BeginInvoke(new Action(StartRegionScreenshot));
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void InitializeHotKey()
        {
            if (_registeredHotKeyIds.Count > 0)
            {
                return;
            }

            List<string> registeredNames = [];
            const uint MOD_NOREPEAT = 0x4000;

            for (int i = 0; i < _hotKeyCandidates.Length; i++)
            {
                HotKeyDefinition candidate = _hotKeyCandidates[i];
                int id = 0xB000 + i; // 自定义 ID
                uint mod = (uint)candidate.Modifiers | MOD_NOREPEAT;
                uint vk = (uint)candidate.Key;

                if (RegisterHotKey(Handle, id, mod, vk))
                {
                    _registeredHotKeyIds.Add(id);
                    registeredNames.Add(candidate.DisplayName);
                    Log($"Hotkey registered: {candidate.DisplayName} (id={id})");
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"Hotkey failed: {candidate.DisplayName}. Win32 error {err}");
                }
            }

            if (_registeredHotKeyIds.Count > 0)
            {
                _registeredHotKeys = string.Join(" / ", registeredNames);
                labelHotkey.Text = $"Hotkey: {_registeredHotKeys}";
                InitializeKeyboardHook();
                return;
            }

            labelHotkey.Text = "Hotkey: unavailable";
            MessageBox.Show(
                "快捷键注册失败：Ctrl + Alt + Z、Ctrl + Alt + B 都不可用，请关闭占用这些快捷键的应用后重试。",
                "快捷键错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void DisposeHotKeys()
        {
            foreach (int id in _registeredHotKeyIds)
            {
                UnregisterHotKey(Handle, id);
            }
            _registeredHotKeyIds.Clear();
        }

        private void InitializeKeyboardHook()
        {
            if (_keyboardHook != null)
            {
                return;
            }

            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyPressed += (_, key) =>
            {
                Log($"Keyboard hook triggered: Alt + Shift + {key}");
                BeginInvoke(new Action(StartRegionScreenshot));
            };
            Log("Keyboard hook initialized.");
        }

        private void ShowWindow()
        {
            _isManuallyShowed = true;
            WindowState = FormWindowState.Normal;
            Opacity = 1;
            Show();
            Activate();
            _isManuallyShowed = false;
        }

        private void HideWindow()
        {
            Opacity = 0;
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void ExitApplication()
        {
            if (MessageBox.Show("确定要退出应用吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void StartRegionScreenshot()
        {
            if (_isCapturing)
            {
                return;
            }

            button1_Click(this, EventArgs.Empty);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_isCapturing)
            {
                return;
            }

            _isCapturing = true;

            try
            {
                Log("Starting region screenshot.");
                using var regionForm = new RegionSelectorForm();
                DialogResult result = regionForm.ShowDialog(this);
                Log($"Region selector closed: {result}.");

                if (result != DialogResult.OK || regionForm.CapturedImage == null)
                {
                    return;
                }

                Bitmap image = regionForm.CapturedImage;
                using var editor = new AnnotationEditorForm(image, regionForm.SelectionScreenBounds);
                DialogResult editorResult = editor.ShowDialog(this);

                Bitmap finalImage = (editorResult == DialogResult.OK && editor.EditedImage != null)
                    ? editor.EditedImage
                    : image;

                _screenshotHelper.SaveCapturedImage(finalImage);

                // 自动复制到剪贴板 + 自动保存到本地，省得用户回主窗口点按钮
                AutoExportScreenshot(finalImage);
            }
            catch (Exception ex)
            {
                Log($"Screenshot error: {ex}");
                MessageBox.Show($"截图业务出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// 截图完成后：复制到剪贴板 + 保存到 图片\PrintScreenApp 文件夹 + 托盘气泡提示
        /// </summary>
        private void AutoExportScreenshot(Bitmap image)
        {
            string? savedPath = null;
            string clipboardStatus;

            try
            {
                Clipboard.SetImage(image);
                clipboardStatus = "已复制到剪贴板";
                Log("Screenshot copied to clipboard.");
            }
            catch (Exception ex)
            {
                clipboardStatus = "复制剪贴板失败";
                Log($"Clipboard copy failed: {ex.Message}");
            }

            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "PrintScreenApp");
                Directory.CreateDirectory(folder);
                savedPath = Path.Combine(folder, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                image.Save(savedPath, System.Drawing.Imaging.ImageFormat.Png);
                Log($"Screenshot auto-saved to {savedPath}.");
            }
            catch (Exception ex)
            {
                Log($"Auto-save failed: {ex.Message}");
            }

            if (_trayIcon != null)
            {
                string title = "截图完成";
                string text = savedPath != null
                    ? $"{clipboardStatus}\n已保存：{savedPath}"
                    : $"{clipboardStatus}\n保存到文件失败";
                _trayIcon.BalloonTipTitle = title;
                _trayIcon.BalloonTipText = text;
                _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                _trayIcon.ShowBalloonTip(2500);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "PNG Files|*.png|JPEG Files|*.jpg|All Files|*.*",
                FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _screenshotHelper.SaveToFile(sfd.FileName);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _screenshotHelper.CopyToClipboard();
        }

        private void Button_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Control ctl)
            {
                ctl.BackColor = ControlPaint.Light(ctl.BackColor, 0.2f);
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Control ctl)
            {
                switch (ctl.Name)
                {
                    case "labelButton1":
                        ctl.BackColor = Color.FromArgb(0, 120, 212);
                        break;
                    case "labelButton2":
                        ctl.BackColor = Color.FromArgb(107, 105, 214);
                        break;
                    case "labelButton3":
                        ctl.BackColor = Color.FromArgb(59, 185, 72);
                        break;
                }
            }
        }

        private sealed record HotKeyDefinition(string DisplayName, HotKeyManager.KeyModifiers Modifiers, Keys Key);

        private static void Log(string message)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintScreenApp.log");
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
    }
}
