using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Media;

namespace TimerApp
{
    public class MainForm : Form
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private OverlayForm _overlay;
        private ToolStripMenuItem _korovaMenuItem;
        private ToolStripMenuItem _udarnikMenuItem;
        private ToolStripMenuItem _hotkeyMenuItem;
        private ToolStripMenuItem _soundMenuItem;

        private const int HOTKEY_ID = 1;
        private const uint MOD_NONE = 0x0000;
        private const int WM_HOTKEY = 0x0312;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private Keys _currentHotkey = Keys.Oem5;
        private bool _isListeningForHotkey;
        private bool _soundEnabled = true;
        private IntPtr _keyboardHookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _keyboardProc;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public MainForm()
        {
            // Делаем форму невидимой
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Opacity = 0;

            _keyboardProc = KeyboardHookCallback;

            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            // Иконка в трее
            _trayMenu = new ContextMenuStrip();
            AddDurationOptions();
            AddSoundOption();
            AddHotkeyOption();
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
            _overlay.TimerStarted += (_, _) => PlayBeepIfEnabled();
            _overlay.TimerFinished += (_, _) => PlayBeepIfEnabled();
            _overlay.Show();

            // По умолчанию выбираем режим "Ударник"
            SetDuration(TimeSpan.FromSeconds(90), _udarnikMenuItem);

            RegisterActivationHotkey();
        }

        private void AddDurationOptions()
        {
            _korovaMenuItem = new ToolStripMenuItem("Корова (1:15)");
            _udarnikMenuItem = new ToolStripMenuItem("Ударник (1:30)");

            _korovaMenuItem.Click += (_, _) => SetDuration(TimeSpan.FromSeconds(75), _korovaMenuItem, true);
            _udarnikMenuItem.Click += (_, _) => SetDuration(TimeSpan.FromSeconds(90), _udarnikMenuItem, true);

            _trayMenu.Items.Add(_korovaMenuItem);
            _trayMenu.Items.Add(_udarnikMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
        }

        private void SetDuration(TimeSpan duration, ToolStripMenuItem selectedItem, bool triggeredByUser = false)
        {
            if (_overlay != null)
            {
                _overlay.SetDefaultDuration(duration);
            }

            _korovaMenuItem.Checked = selectedItem == _korovaMenuItem;
            _udarnikMenuItem.Checked = selectedItem == _udarnikMenuItem;

            if (triggeredByUser)
            {
                PlayBeepIfEnabled();
            }
        }

        private void AddSoundOption()
        {
            _soundMenuItem = new ToolStripMenuItem("Звуковое оповещение")
            {
                Checked = _soundEnabled,
                CheckOnClick = true
            };
            _soundMenuItem.CheckedChanged += (_, _) => _soundEnabled = _soundMenuItem.Checked;
            _trayMenu.Items.Add(_soundMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
        }

        private void AddHotkeyOption()
        {
            _hotkeyMenuItem = new ToolStripMenuItem();
            UpdateHotkeyMenuText();
            _hotkeyMenuItem.Click += (_, _) => StartListeningForHotkey();
            _trayMenu.Items.Add(_hotkeyMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
        }

        private void UpdateHotkeyMenuText()
        {
            string hotkeyName = new KeysConverter().ConvertToString(_currentHotkey) ?? _currentHotkey.ToString();
            string suffix = _isListeningForHotkey ? "ожидание..." : hotkeyName;
            _hotkeyMenuItem.Text = $"Горячая клавиша ({suffix})";
        }

        private void StartListeningForHotkey()
        {
            if (_isListeningForHotkey)
            {
                return;
            }

            _isListeningForHotkey = true;
            UpdateHotkeyMenuText();

            using Process currentProcess = Process.GetCurrentProcess();
            using ProcessModule currentModule = currentProcess.MainModule!;
            IntPtr moduleHandle = GetModuleHandle(currentModule.ModuleName);

            _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);

            if (_keyboardHookId == IntPtr.Zero)
            {
                _isListeningForHotkey = false;
                MessageBox.Show("Не удалось начать прослушивание клавиатуры для выбора горячей клавиши.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateHotkeyMenuText();
            }
        }

        private void StopListeningForHotkey()
        {
            if (_keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }

            if (_isListeningForHotkey)
            {
                _isListeningForHotkey = false;
                UpdateHotkeyMenuText();
            }
        }

        private void SetNewHotkey(Keys newHotkey)
        {
            StopListeningForHotkey();

            if (_currentHotkey == newHotkey)
            {
                return;
            }

            _currentHotkey = newHotkey;
            RegisterActivationHotkey();
            UpdateHotkeyMenuText();
            PlayBeepIfEnabled();
        }

        private void RegisterActivationHotkey()
        {
            try
            {
                UnregisterHotKey(Handle, HOTKEY_ID);
            }
            catch
            {
                // ignored
            }

            bool registered = RegisterHotKey(Handle, HOTKEY_ID, MOD_NONE, (uint)_currentHotkey);

            if (!registered)
            {
                MessageBox.Show($"Не удалось зарегистрировать глобальный хоткей \"{_currentHotkey}\".",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys newHotkey = (Keys)vkCode;
                SetNewHotkey(newHotkey);
            }

            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
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

        private void PlayBeepIfEnabled()
        {
            if (_soundEnabled)
            {
                SystemSounds.Beep.Play();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                UnregisterHotKey(Handle, HOTKEY_ID);
            }
            catch { }

            StopListeningForHotkey();

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            _overlay?.Close();
        }
    }
}
