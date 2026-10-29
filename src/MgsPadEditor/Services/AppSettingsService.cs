using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace MgsPadEditor.Services
{
    public class AppSettings
    {
        public string? GameDirectory { get; set; }
        public string? CustomIniPath { get; set; }
    }

    public static class AppSettingsService
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MgsPadEditor"
        );

        private static readonly string ConfigFilePath = Path.Combine(ConfigDir, "config.json");

        public static AppSettings Load()
        {
            AppSettings settings = new();

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        settings = loaded;
                    }
                }
            }
            catch
            {
                // Fallback to default
            }

            // If GameDirectory is not set or doesn't exist, try auto-detection
            if (string.IsNullOrWhiteSpace(settings.GameDirectory) || !Directory.Exists(settings.GameDirectory))
            {
                string? detected = DetectGogGamePath();
                if (!string.IsNullOrEmpty(detected) && Directory.Exists(detected))
                {
                    settings.GameDirectory = detected;
                    Save(settings);
                }
            }

            return settings;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // Best effort
            }
        }

        /// <summary>
        /// Reads Windows Registry for GOG Metal Gear Solid installation (ID: 1771973390)
        /// </summary>
        public static string? DetectGogGamePath()
        {
            // 1. Try 64-bit registry path (WOW6432Node)
            string[] keys = new[]
            {
                @"SOFTWARE\WOW6432Node\GOG.com\Games\1771973390",
                @"SOFTWARE\GOG.com\Games\1771973390"
            };

            foreach (var keyPath in keys)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key != null)
                    {
                        var pathVal = key.GetValue("path") as string;
                        if (!string.IsNullOrWhiteSpace(pathVal) && Directory.Exists(pathVal))
                        {
                            return pathVal;
                        }
                    }
                }
                catch
                {
                    // Ignore registry access errors
                }
            }

            // 2. Check well-known user path (e.g. K:\Juegos\Metal Gear Solid)
            string known = @"K:\Juegos\Metal Gear Solid";
            if (Directory.Exists(known))
            {
                return known;
            }

            return null;
        }
    }
}

