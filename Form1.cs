using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

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
        private HotKeyConfig _hotKeyConfig = HotKeyConfig.Load();

        private readonly List<int> _registeredHotKeyIds = [];
        private GlobalKeyboardHook? _keyboardHook;
        private NotifyIcon? _trayIcon;
        private ToolStripMenuItem? _autoStartMenuItem;
        private string _registeredHotKeys = "";
        private bool _isCapturing;
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupValueName = "PrintScreenApp";

        public Form1()
        {
            InitializeComponent();
            _screenshotHelper = new ScreenshotHelper();

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("立即截图", null, (_, _) => BeginInvoke(new Action(StartRegionScreenshot)));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("设置快捷键…", null, (_, _) => OpenHotKeySettings());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (_, _) => ExitApplication());
            _autoStartMenuItem = new ToolStripMenuItem("Run at startup")
            {
                CheckOnClick = true,
                Checked = IsAutoStartEnabled()
            };
            _autoStartMenuItem.CheckedChanged += AutoStartMenuItem_CheckedChanged;
            contextMenu.Items.Insert(Math.Max(0, contextMenu.Items.Count - 1), _autoStartMenuItem);
            ContextMenuStrip = contextMenu;

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "PrintScreenApp",
                Visible = true,
                ContextMenuStrip = contextMenu
            };
            _trayIcon.DoubleClick += (_, _) => BeginInvoke(new Action(StartRegionScreenshot));

            ShowInTaskbar = false;
            Opacity = 0;
            WindowState = FormWindowState.Minimized;
            EnsureAutoStartEnabled();
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

            value = false;

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
                BeginInvoke(new Action(StartRegionScreenshot));
            }
            else if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                Log($"WM_HOTKEY received: id={id}, lParam=0x{m.LParam.ToInt64():X}");
                int idx = _registeredHotKeyIds.IndexOf(id);
                if (idx >= 0 && idx < _hotKeyConfig.Entries.Count)
                {
                    Log($"Hotkey triggered: {_hotKeyConfig.Entries[idx].DisplayName}");
                    BeginInvoke(new Action(StartRegionScreenshot));
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void InitializeHotKey()
        {
            DisposeHotKeys();

            List<string> registeredNames = [];
            const uint MOD_NOREPEAT = 0x4000;

            for (int i = 0; i < _hotKeyConfig.Entries.Count; i++)
            {
                HotKeyEntry entry = _hotKeyConfig.Entries[i];
                if (!entry.IsValid)
                {
                    Log($"Hotkey skipped: invalid entry at index {i}");
                    continue;
                }

                int id = 0xB000 + i;
                uint mod = (uint)entry.GetModifiers() | MOD_NOREPEAT;
                uint vk = (uint)entry.Key;

                if (RegisterHotKey(Handle, id, mod, vk))
                {
                    _registeredHotKeyIds.Add(id);
                    registeredNames.Add(entry.DisplayName);
                    Log($"Hotkey registered: {entry.DisplayName} (id={id}, hWnd=0x{Handle.ToInt64():X})");
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"Hotkey failed: {entry.DisplayName}. Win32 error {err}");
                }
            }

            if (_registeredHotKeyIds.Count > 0)
            {
                _registeredHotKeys = string.Join(" / ", registeredNames);
                labelHotkey.Text = $"快捷键：{_registeredHotKeys}（点此修改）";
                InitializeKeyboardHook();
                return;
            }

            labelHotkey.Text = "快捷键：未启用（点此设置）";
            MessageBox.Show(
                "快捷键注册全部失败，请在托盘菜单「设置快捷键…」中换一组未被占用的组合。",
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

        private void OpenHotKeySettings()
        {
            using var dlg = new HotKeySettingsForm(_hotKeyConfig);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _hotKeyConfig = dlg.Config;
                try
                {
                    _hotKeyConfig.Save();
                    Log($"Hotkey config saved to {HotKeyConfig.ConfigPath}");
                }
                catch (Exception ex)
                {
                    Log($"Hotkey config save failed: {ex.Message}");
                    MessageBox.Show($"保存配置失败：{ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                InitializeHotKey();
            }
        }

        private void InitializeKeyboardHook()
        {
            if (_keyboardHook != null)
            {
                _keyboardHook.Matcher = HookMatch;
                return;
            }

            _keyboardHook = new GlobalKeyboardHook { Matcher = HookMatch };
            Log($"Keyboard hook installed: {_keyboardHook.IsInstalled}");
            _keyboardHook.KeyPressed += (_, key) =>
            {
                Log($"Keyboard hook triggered: {key}");
                BeginInvoke(new Action(StartRegionScreenshot));
            };
        }

        /// <summary>
        /// 按配置匹配；任一条 entry 命中（修饰键全相等）就返回 true。
        /// 同时为了兼容用户在某些键盘上把 Alt 按成 Win（或反之），如果 entry 同时只用 Ctrl+(Alt|Win)+key，则 Alt/Win 二选一即可。
        /// </summary>
        private bool HookMatch(int vk, bool ctrl, bool alt, bool shift, bool win)
        {
            foreach (HotKeyEntry e in _hotKeyConfig.Entries)
            {
                if (!e.IsValid || (int)e.Key != vk) continue;
                if (e.Ctrl != ctrl) continue;
                if (e.Shift != shift) continue;

                bool altOk = e.Alt == alt;
                bool winOk = e.Win == win;
                bool altWinSwap = e.Alt && !e.Win && win && !alt
                                  || e.Win && !e.Alt && alt && !win;
                if ((altOk && winOk) || altWinSwap)
                {
                    return true;
                }
            }
            return false;
        }

        private void ExitApplication()
        {
            if (MessageBox.Show("确定要退出应用吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void AutoStartMenuItem_CheckedChanged(object? sender, EventArgs e)
        {
            if (_autoStartMenuItem != null)
            {
                SetAutoStart(_autoStartMenuItem.Checked);
            }
        }

        private void EnsureAutoStartEnabled()
        {
            if (IsAutoStartEnabled())
            {
                return;
            }

            SetAutoStart(true);
            SyncAutoStartMenuItem();
        }

        private static bool IsAutoStartEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                string? value = key?.GetValue(StartupValueName) as string;
                return string.Equals(value, GetStartupCommand(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Log($"Auto-start read failed: {ex.Message}");
                return false;
            }
        }

        private void SetAutoStart(bool enabled)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(StartupRegistryKey);
                if (enabled)
                {
                    key.SetValue(StartupValueName, GetStartupCommand(), RegistryValueKind.String);
                    Log("Auto-start enabled.");
                }
                else
                {
                    key.DeleteValue(StartupValueName, false);
                    Log("Auto-start disabled.");
                }
            }
            catch (Exception ex)
            {
                Log($"Auto-start update failed: {ex.Message}");
                MessageBox.Show($"Failed to update auto-start setting: {ex.Message}", "PrintScreenApp",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SyncAutoStartMenuItem();
            }
        }

        private void SyncAutoStartMenuItem()
        {
            if (_autoStartMenuItem == null)
            {
                return;
            }

            _autoStartMenuItem.CheckedChanged -= AutoStartMenuItem_CheckedChanged;
            _autoStartMenuItem.Checked = IsAutoStartEnabled();
            _autoStartMenuItem.CheckedChanged += AutoStartMenuItem_CheckedChanged;
        }

        private static string GetStartupCommand()
        {
            return $"\"{Application.ExecutablePath}\"";
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

                // 用户在批注界面取消（按 Esc / 点取消）→ 整次截图作废，什么都不保存
                if (editorResult != DialogResult.OK || editor.EditedImage == null)
                {
                    Log("Annotation cancelled, screenshot discarded.");
                    return;
                }

                Bitmap finalImage = editor.EditedImage;
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

        private void LabelHotkey_Click(object? sender, EventArgs e) => OpenHotKeySettings();

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

        private bool MatchesAnyConfiguredKey(Keys vk)
        {
            foreach (HotKeyEntry e in _hotKeyConfig.Entries)
            {
                if (e.Key == vk) return true;
            }
            return false;
        }

        private static void Log(string message)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintScreenApp.log");
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
    }
}
