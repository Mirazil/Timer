using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace TimerApp
{
    public class MainForm : Form
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private OverlayForm _overlay;

        private const int HOTKEY_ID = 1;
        private const uint MOD_NONE = 0x0000;
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainForm()
        {
            // Делаем форму невидимой
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Opacity = 0;

            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            // Иконка в трее
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Выход", null, OnExitClick);

            _trayIcon = new NotifyIcon
            {
                Text = "Overlay Timer",
                Icon = SystemIcons.Information,
                ContextMenuStrip = _trayMenu,
                Visible = true
            };

            // Создаём и показываем оверлей
            _overlay = new OverlayForm();
            _overlay.Show();

            // Регистрируем глобальный хоткей "\" (Keys.Oem5)
            bool registered = RegisterHotKey(Handle, HOTKEY_ID, MOD_NONE, (uint)Keys.Oem5);

            if (!registered)
            {
                MessageBox.Show("Не удалось зарегистрировать глобальный хоткей \"\\\".",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // Запускаем/перезапускаем таймер на 1:23
                _overlay?.StartTimer();
            }

            base.WndProc(ref m);
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                UnregisterHotKey(Handle, HOTKEY_ID);
            }
            catch { }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            _overlay?.Close();
        }
    }
}
