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
        private ToolStripMenuItem _korovaMenuItem;
        private ToolStripMenuItem _udarnikMenuItem;

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
            AddDurationOptions();
            _trayMenu.Items.Add("Сбросить местоположение", null, OnResetLocationClick);
            _trayMenu.Items.Add(new ToolStripSeparator());
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

            // По умолчанию выбираем режим "Ударник"
            SetDuration(TimeSpan.FromSeconds(90), _udarnikMenuItem);

            // Регистрируем глобальный хоткей "\" (Keys.Oem5)
            bool registered = RegisterHotKey(Handle, HOTKEY_ID, MOD_NONE, (uint)Keys.Oem5);

            if (!registered)
            {
                MessageBox.Show("Не удалось зарегистрировать глобальный хоткей \"\\\".",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddDurationOptions()
        {
            _korovaMenuItem = new ToolStripMenuItem("Корова (1:15)");
            _udarnikMenuItem = new ToolStripMenuItem("Ударник (1:30)");

            _korovaMenuItem.Click += (_, _) => SetDuration(TimeSpan.FromSeconds(75), _korovaMenuItem);
            _udarnikMenuItem.Click += (_, _) => SetDuration(TimeSpan.FromSeconds(90), _udarnikMenuItem);

            _trayMenu.Items.Add(_korovaMenuItem);
            _trayMenu.Items.Add(_udarnikMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
        }

        private void SetDuration(TimeSpan duration, ToolStripMenuItem selectedItem)
        {
            if (_overlay != null)
            {
                _overlay.SetDefaultDuration(duration);
            }

            _korovaMenuItem.Checked = selectedItem == _korovaMenuItem;
            _udarnikMenuItem.Checked = selectedItem == _udarnikMenuItem;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // Переключаем таймер между запуском и остановкой
                _overlay?.ToggleTimer();
            }

            base.WndProc(ref m);
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnResetLocationClick(object? sender, EventArgs e)
        {
            _overlay?.ResetLocationToDefault();
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
