using System;
using System.IO;
using MgsPadEditor.Models;
using Xunit;

namespace MgsPadEditor.Tests
{
    public class IniFileTests
    {
        [Fact]
        public void Read_PreservesPreambleCommentsAndBlanks()
        {
            string iniContent = "; File header comment\r\n; Author: Test\r\n\r\n[general]\r\nonelayout = TRUE\r\n";
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, iniContent);

                var ini = new IniFile();
                ini.Read(tempFile);

                Assert.Equal(3, ini.PreambleLines.Count); // 2 comments + 1 blank
                Assert.Single(ini.Sections);
                Assert.Equal("general", ini.Sections[0].Name);

                string tempOutput = Path.GetTempFileName();
                ini.Write(tempOutput);

                string writtenContent = File.ReadAllText(tempOutput);
                Assert.StartsWith("; File header comment\r\n; Author: Test\r\n\r\n[general]", writtenContent);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void Read_HandlesDuplicateSectionNamesWithoutCollision()
        {
            string iniContent = @"[microsoft_xbox360_a]
guid = 02B6045E-0000-0000-0000-504944564944
pad_zaxisplus = pad_button6

[microsoft_xbox360_a]
guid = 02A8045E-0000-0000-0000-504944564944
pad_zaxisplus = pad_button7
";
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, iniContent);

                var ini = new IniFile();
                ini.Read(tempFile);

                Assert.Equal(2, ini.Sections.Count);
                Assert.Equal("microsoft_xbox360_a", ini.Sections[0].DisplayName);
                Assert.Equal("microsoft_xbox360_a #2", ini.Sections[1].DisplayName);

                Assert.Equal("02B6045E-0000-0000-0000-504944564944", ini.Sections[0].GetValue("guid"));
                Assert.Equal("02A8045E-0000-0000-0000-504944564944", ini.Sections[1].GetValue("guid"));

                // Modifying section 2 must not touch section 1
                ini.Sections[1].SetValue("guid", "NEW_GUID");
                Assert.Equal("02B6045E-0000-0000-0000-504944564944", ini.Sections[0].GetValue("guid"));
                Assert.Equal("NEW_GUID", ini.Sections[1].GetValue("guid"));
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadRealDixiIni_SucceedsAndPreservesSections()
        {
            string realDixi = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\dixi.ini"));
            if (!File.Exists(realDixi))
            {
                realDixi = @"o:\Desarrollos\MGS PAD Editor\dixi.ini";
            }

            Assert.True(File.Exists(realDixi), $"dixi.ini not found at {realDixi}");

            var ini = new IniFile();
            ini.Read(realDixi);

            Assert.True(ini.Sections.Count > 15, "dixi.ini should have more than 15 sections");
            Assert.NotNull(ini.GetSectionByName("general"));
            Assert.NotNull(ini.GetSectionByName("sony_dualsense"));
            Assert.NotNull(ini.GetSectionByName("keyboardandmouse"));

            Assert.Equal("TRUE", ini.GetGeneralValue("onelayout"));
            Assert.Equal("FALSE", ini.GetGeneralValue("dpadtoanalog"));
            Assert.Equal("FALSE", ini.GetGeneralValue("keyboardandmouse"));
            Assert.Equal("5", ini.GetGeneralValue("mousespeed"));
        }

        [Fact]
        public void Write_CreatesBakBackup()
        {
            string tempFile = Path.GetTempFileName();
            string bakFile = tempFile + ".bak";
            try
            {
                File.WriteAllText(tempFile, "[general]\r\nonelayout = TRUE\r\n");

                var ini = new IniFile();
                ini.Read(tempFile);
                ini.SetGeneralValue("onelayout", "FALSE");
                ini.Write();

                Assert.True(File.Exists(bakFile), "Backup file .bak should exist after save");
                string bakContent = File.ReadAllText(bakFile);
                Assert.Contains("onelayout", bakContent);
                Assert.Contains("TRUE", bakContent);

                string newContent = File.ReadAllText(tempFile);
                Assert.Contains("onelayout", newContent);
                Assert.Contains("FALSE", newContent);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                if (File.Exists(bakFile)) File.Delete(bakFile);
            }
        }
    }
}
