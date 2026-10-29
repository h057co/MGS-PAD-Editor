using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MgsPadEditor.Models
{
    public enum IniLineType
    {
        Preamble,
        Blank,
        Comment,
        Header,
        KeyValue,
        Other
    }

    public class IniLine
    {
        public IniLineType Type { get; set; }
        public string Raw { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static IniLine CreateBlank(string raw = "\r\n") => new() { Type = IniLineType.Blank, Raw = raw };
        public static IniLine CreateComment(string raw) => new() { Type = IniLineType.Comment, Raw = raw };
        public static IniLine CreateHeader(string sectionName) => new() { Type = IniLineType.Header, Raw = $"[{sectionName}]\r\n" };
        public static IniLine CreateKeyValue(string key, string value) => new() { Type = IniLineType.KeyValue, Key = key, Value = value, Raw = $"{key,-20}= {value}\r\n" };
        public static IniLine CreatePreamble(string raw) => new() { Type = IniLineType.Preamble, Raw = raw };
        public static IniLine CreateOther(string raw) => new() { Type = IniLineType.Other, Raw = raw };
    }

    public class IniSection
    {
        public Guid UniqueId { get; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<IniLine> Lines { get; set; } = new();

        public IniSection(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }

        public string? GetValue(string key)
        {
            var line = Lines.FirstOrDefault(l => l.Type == IniLineType.KeyValue && string.Equals(l.Key, key, StringComparison.OrdinalIgnoreCase));
            return line?.Value;
        }

        public void SetValue(string key, string value)
        {
            var line = Lines.FirstOrDefault(l => l.Type == IniLineType.KeyValue && string.Equals(l.Key, key, StringComparison.OrdinalIgnoreCase));
            if (line != null)
            {
                line.Value = value;
                line.Raw = $"{line.Key,-20}= {value}\r\n";
            }
            else
            {
                Lines.Add(IniLine.CreateKeyValue(key, value));
            }
        }

        public bool RemoveValue(string key)
        {
            int removed = Lines.RemoveAll(l => l.Type == IniLineType.KeyValue && string.Equals(l.Key, key, StringComparison.OrdinalIgnoreCase));
            return removed > 0;
        }

        public Dictionary<string, string> GetAllKeyValues()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in Lines.Where(l => l.Type == IniLineType.KeyValue))
            {
                dict[line.Key] = line.Value;
            }
            return dict;
        }
    }

    public class IniFile
    {
        public List<IniLine> PreambleLines { get; } = new();
        public List<IniSection> Sections { get; } = new();
        public string? CurrentFilePath { get; private set; }

        public void Read(string filePath)
        {
            CurrentFilePath = filePath;
            PreambleLines.Clear();
            Sections.Clear();

            string[] rawLines = File.ReadAllLines(filePath, Encoding.UTF8);

            IniSection? currentSection = null;
            var sectionCountByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in rawLines)
            {
                string line = rawLine;
                string stripped = line.Trim();

                if (string.IsNullOrEmpty(stripped))
                {
                    if (currentSection == null)
                    {
                        PreambleLines.Add(IniLine.CreateBlank(line + "\r\n"));
                    }
                    else
                    {
                        currentSection.Lines.Add(IniLine.CreateBlank(line + "\r\n"));
                    }
                }
                else if (stripped.StartsWith(";") || stripped.StartsWith("#"))
                {
                    if (currentSection == null)
                    {
                        PreambleLines.Add(IniLine.CreateComment(line + "\r\n"));
                    }
                    else
                    {
                        currentSection.Lines.Add(IniLine.CreateComment(line + "\r\n"));
                    }
                }
                else if (stripped.StartsWith("[") && stripped.EndsWith("]"))
                {
                    string secName = stripped.Substring(1, stripped.Length - 2).Trim();
                    sectionCountByName[secName] = sectionCountByName.GetValueOrDefault(secName, 0) + 1;
                    int count = sectionCountByName[secName];
                    string displayName = count == 1 ? secName : $"{secName} #{count}";

                    currentSection = new IniSection(secName, displayName);
                    currentSection.Lines.Add(IniLine.CreateHeader(secName));
                    Sections.Add(currentSection);
                }
                else if (stripped.Contains('=') && currentSection != null)
                {
                    int eqIdx = stripped.IndexOf('=');
                    string key = stripped.Substring(0, eqIdx).Trim();
                    string val = stripped.Substring(eqIdx + 1).Trim();
                    currentSection.Lines.Add(IniLine.CreateKeyValue(key, val));
                }
                else if (currentSection != null)
                {
                    currentSection.Lines.Add(IniLine.CreateOther(line + "\r\n"));
                }
                else
                {
                    PreambleLines.Add(IniLine.CreatePreamble(line + "\r\n"));
                }
            }
        }

        public void Write(string? filePath = null)
        {
            string path = filePath ?? CurrentFilePath ?? throw new InvalidOperationException("No file path specified to save.");

            // Always create backup first if overwriting existing file
            if (File.Exists(path))
            {
                CreateBackup(path);
            }

            var sb = new StringBuilder();

            // 1. Write preamble lines
            foreach (var line in PreambleLines)
            {
                sb.Append(FormatLine(line));
            }

            // 2. Write sections
            foreach (var sec in Sections)
            {
                foreach (var line in sec.Lines)
                {
                    sb.Append(FormatLine(line));
                }
            }

            // Write using UTF-8 without BOM with strict CRLF line endings
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            CurrentFilePath = path;
        }

        public static void CreateBackup(string originalPath)
        {
            try
            {
                string bakPath = originalPath + ".bak";
                File.Copy(originalPath, bakPath, true);
            }
            catch
            {
                // Backup creation failure should not block save, but is best effort
            }
        }

        private static string FormatLine(IniLine line)
        {
            return line.Type switch
            {
                IniLineType.Header => line.Raw.EndsWith("\r\n") ? line.Raw : line.Raw.TrimEnd() + "\r\n",
                IniLineType.KeyValue => $"{line.Key,-20}= {line.Value}\r\n",
                _ => line.Raw.EndsWith("\r\n") ? line.Raw : line.Raw.TrimEnd() + "\r\n"
            };
        }

        public IniSection? GetSectionByDisplayName(string displayName)
        {
            return Sections.FirstOrDefault(s => string.Equals(s.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
        }

        public IniSection? GetSectionByName(string name)
        {
            return Sections.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public IniSection AddSection(string name, string? guid = null)
        {
            int count = Sections.Count(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)) + 1;
            string displayName = count == 1 ? name : $"{name} #{count}";

            var sec = new IniSection(name, displayName);
            sec.Lines.Add(IniLine.CreateHeader(name));
            if (!string.IsNullOrWhiteSpace(guid))
            {
                sec.Lines.Add(IniLine.CreateKeyValue("guid", guid.Trim()));
            }

            // Insert before [keyboardandmouse] if it exists, otherwise append
            int kbdIdx = Sections.FindIndex(s => string.Equals(s.Name, "keyboardandmouse", StringComparison.OrdinalIgnoreCase));
            if (kbdIdx >= 0)
            {
                Sections.Insert(kbdIdx, sec);
            }
            else
            {
                Sections.Add(sec);
            }

            return sec;
        }

        public bool RemoveSection(string displayName)
        {
            int index = Sections.FindIndex(s => string.Equals(s.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                Sections.RemoveAt(index);
                return true;
            }
            return false;
        }

        public string? GetGeneralValue(string key)
        {
            var sec = GetSectionByName("general");
            return sec?.GetValue(key);
        }

        public void SetGeneralValue(string key, string value)
        {
            var sec = GetSectionByName("general");
            if (sec == null)
            {
                sec = AddSection("general");
            }
            sec.SetValue(key, value);
        }
    }
}
