using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DockerHostSwitcher.WinForms
{
    internal static class Program
    {
        [STAThread]
        internal static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (new NotifyIconMenu())
            {
                Application.Run();
            }
        }

        internal static void ExitApplication()
        {
            Application.Exit();
        }
    }
}
