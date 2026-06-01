using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public partial class Form1 : Form
    {
        private ScreenshotHelper _screenshotHelper = null!;
        private HotKeyManager? _hotKeyManager;

        public Form1()
        {
            InitializeComponent();
            _screenshotHelper = new ScreenshotHelper();
            
            // 创建右键菜单，支持恢复窗体或退出
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("隐藏", null, (s, e) => HideWindow());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => ExitApplication());
            this.ContextMenuStrip = contextMenu;
            
            // 禁用控件面板默认事件处理（只使用热键）
            this.ShowInTaskbar = true;
            this.Opacity = 0;  // 完全透明，即使显示也看不见
            this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// 【核心黑科技】：重写窗体创建参数。
        /// 通过赋予 WS_EX_TOOLWINDOW 样式，并在首次创建时抹去可见性，
        /// 可以让窗体在【完全隐形】的同时，保持【WndProc 消息循环 100% 畅通】。
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // 变成工具箱窗口样式（不在 Alt+Tab 中出现，不占任务栏）
                cp.ExStyle |= 0x00000080; 
                return cp;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            // 强行阻止程序启动时显示，保持完全隐形
            if (!IsHandleCreated)
            {
                CreateHandle();
                value = false; 
            }
            // 只有在明确调用ShowWindow()时才显示
            if (!_isManuallyShowed)
            {
                value = false;
            }
            base.SetVisibleCore(value);
        }
        
        private bool _isManuallyShowed = false;
        
        private void ShowWindow()
        {
            _isManuallyShowed = true;
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.Activate();
            _isManuallyShowed = false;
        }
        
        private void HideWindow()
        {
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }
        
        private void ExitApplication()
        {
            if (MessageBox.Show("确定要退出应用吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeHotKey();
        }

        private void InitializeHotKey()
        {
            try
            {
                _hotKeyManager = new HotKeyManager(Handle);

                // 【硬核测试】：为了排除一切干扰，我们直接用这个“四键合一”的终极冷门组合！
                // 必须同时按下：Ctrl + Shift + Alt + L
                var modifiers = HotKeyManager.KeyModifiers.Ctrl | 
                                HotKeyManager.KeyModifiers.Shift | 
                                HotKeyManager.KeyModifiers.Alt;
                
                _hotKeyManager.Register(modifiers, Keys.L);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"快捷键注册失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (_hotKeyManager != null && _hotKeyManager.IsHotKeyMessage(message))
            {
                // 收到消息后，立即触发全屏截图（同步调用，确保瞬间冻结）
                Console.WriteLine("[HOTKEY] 快捷键触发 - 启动全屏冻结截图...");
                
                // 直接调用（同步），确保最快的响应速度
                StartRegionScreenshot();
            }
            base.WndProc(ref message);
        }

        private void StartRegionScreenshot()
        {
            button1_Click(this, EventArgs.Empty);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_hotKeyManager != null)
            {
                _hotKeyManager.Unregister();
                _hotKeyManager.Dispose();
                _hotKeyManager = null;
            }
            base.OnFormClosing(e);
        }

        // 触发截图区域选择
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 立即显示全屏冻结覆盖
                var regionForm = new RegionSelectorForm();
                var result = regionForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var image = regionForm.CapturedImage;
                    if (image != null)
                    {
                        var editor = new AnnotationEditorForm(image, regionForm.SelectionScreenBounds);
                        var editorResult = editor.ShowDialog();

                        if (editorResult == DialogResult.OK && editor.EditedImage != null)
                        {
                            _screenshotHelper.SaveCapturedImage(editor.EditedImage);
                            // 截图完成后，保持后台隐形，不弹出任何消息框（可选）
                            // MessageBox.Show("截图并编辑成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Console.WriteLine("[SUCCESS] 截图并编辑已保存");
                        }
                        else
                        {
                            _screenshotHelper.SaveCapturedImage(image);
                            // MessageBox.Show("截图已保存（未编辑）！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Console.WriteLine("[INFO] 截图已保存（未编辑）");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"截图业务出错: {ex.Message}");
            }
        }

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
    }
}