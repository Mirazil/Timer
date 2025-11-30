using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace TimerApp
{
    public class OverlayForm : Form
    {
        private Label _timeLabel;
        private System.Windows.Forms.Timer _timer;
        private TimeSpan _remaining;

        private bool _isDragging;
        private Point _dragStart;

        public OverlayForm()
        {
            // Визуальные настройки оверлея
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;

            // Можно сразу где-то в углу экрана
            Location = new Point(100, 100);
            BackColor = Color.Black;
            Opacity = 0.8; // чуть прозрачный, чтобы не бесил совсем

            // Размер
            Size = new Size(180, 60);

            // Текст таймера
            _timeLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                Text = "01:23"
            };

            Controls.Add(_timeLabel);

            // Таймер на 1 секунду
            _timer = new Timer
            {
                Interval = 1000
            };
            _timer.Tick += Timer_Tick;

            // Перетаскивание формы мышью
            MouseDown += Overlay_MouseDown;
            MouseMove += Overlay_MouseMove;
            MouseUp += Overlay_MouseUp;

            // То же самое для лейбла, чтобы можно было тянуть за текст
            _timeLabel.MouseDown += Overlay_MouseDown;
            _timeLabel.MouseMove += Overlay_MouseMove;
            _timeLabel.MouseUp += Overlay_MouseUp;
        }

        /// <summary>
        /// Запуск/перезапуск таймера на 1 минуту 23 секунды.
        /// </summary>
        public void StartTimer()
        {
            _remaining = TimeSpan.FromSeconds(83); // 1 мин 23 сек
            _timeLabel.Text = FormatTime(_remaining);
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remaining.TotalSeconds <= 0)
            {
                _timer.Stop();
                _remaining = TimeSpan.Zero;
                _timeLabel.Text = FormatTime(_remaining);
                return;
            }

            _remaining = _remaining.Add(TimeSpan.FromSeconds(-1));
            _timeLabel.Text = FormatTime(_remaining);
        }

        private static string FormatTime(TimeSpan ts)
        {
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
        }

        // Логика перетаскивания

        private void Overlay_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStart = e.Location;
            }
        }

        private void Overlay_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point screenPos = PointToScreen(e.Location);
                Location = new Point(screenPos.X - _dragStart.X, screenPos.Y - _dragStart.Y);
            }
        }

        private void Overlay_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
            }
        }
    }
}
