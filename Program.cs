using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PrintScreenApp
{
    internal static class Program
    {
        // 单实例互斥体名称（Global\ 让它对当前用户全局生效）
        private const string SingleInstanceMutexName = "Global\\PrintScreenApp_SingleInstance_Mutex_{8E2A6F3C-9B1E-4F5D-9A2B-7C3D5E6F1A22}";
        private const int WM_SHOW_PRINTSCREEN_APP = 0x8001; // WM_APP + 1

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        public static uint ShowAppMessageId { get; private set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 注册一个跨进程消息，老实例收到后会把窗口显示出来
            ShowAppMessageId = RegisterWindowMessage("PrintScreenApp_ShowWindow_Message");

            using Mutex mutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                // 已经有一个实例在跑，通知它显示窗口然后退出
                PostMessage(new IntPtr(0xFFFF) /* HWND_BROADCAST */, ShowAppMessageId, IntPtr.Zero, IntPtr.Zero);
                MessageBox.Show("PrintScreenApp 已经在运行。\n请检查任务栏或系统托盘图标。", "PrintScreenApp",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Form1 form = new Form1();
            // 不直接Show，让窗体在后台最小化运行
            form.WindowState = FormWindowState.Minimized;
            form.ShowInTaskbar = true;  // 在任务栏显示，方便用户恢复
            Application.Run(form);

            GC.KeepAlive(mutex);
        }
    }
}