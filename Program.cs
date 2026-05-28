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
            // Main form stays hidden; hotkeys and screenshot UI run in the background.
            Application.Run(new Form1());
        }
    }
}