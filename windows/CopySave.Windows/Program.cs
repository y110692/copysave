using System.Threading;
using System.Windows.Forms;

namespace CopySave.Windows
{
    internal static class Program
    {
        [System.STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, "CopySave.Windows.Singleton", out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ApplicationContextHost());
            }
        }
    }
}
