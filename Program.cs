using System.Text;

namespace PrintScreenApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Form1 form = new Form1();
            // 不直接Show，让窗体在后台最小化运行
            form.WindowState = FormWindowState.Minimized;
            form.ShowInTaskbar = true;  // 在任务栏显示，方便用户恢复
            Application.Run(form);
        }
    }
}