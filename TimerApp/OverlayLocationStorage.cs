using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace TimerApp
{
    internal static class OverlayLocationStorage
    {
        private const string FileName = "overlay-location.json";

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimerApp",
            FileName);

        public static Point? Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                var json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<OverlayLocationData>(json);

                if (data == null)
                {
                    return null;
                }

                return new Point(data.X, data.Y);
            }
            catch
            {
                return null;
            }
        }

        public static void Save(Point location)
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = new OverlayLocationData { X = location.X, Y = location.Y };
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Игнорируем ошибки записи, чтобы не мешать работе таймера
            }
        }

        private sealed class OverlayLocationData
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
