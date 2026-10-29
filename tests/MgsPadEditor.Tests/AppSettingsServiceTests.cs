using System.IO;
using MgsPadEditor.Services;
using Xunit;

namespace MgsPadEditor.Tests
{
    public class AppSettingsServiceTests
    {
        [Fact]
        public void DetectGogGamePath_FindsRealInstallation()
        {
            string? path = AppSettingsService.DetectGogGamePath();
            Assert.NotNull(path);
            Assert.True(Directory.Exists(path));
            Assert.True(File.Exists(Path.Combine(path, "dixi.ini")));
        }

        [Fact]
        public void LoadAndSaveSettings_WorksCorrectly()
        {
            var settings = new AppSettings
            {
                GameDirectory = @"K:\Juegos\Metal Gear Solid"
            };

            AppSettingsService.Save(settings);
            var loaded = AppSettingsService.Load();

            Assert.Equal(@"K:\Juegos\Metal Gear Solid", loaded.GameDirectory);
        }
    }
}

