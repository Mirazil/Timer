using System;
using System.Windows.Forms;

namespace TimerApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Запускаем невидимую главную форму
            Application.Run(new MainForm());
        }
    }
}
