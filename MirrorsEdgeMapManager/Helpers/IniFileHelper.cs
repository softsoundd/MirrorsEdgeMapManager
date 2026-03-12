using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MirrorsEdgeMapManager.Helpers
{
    public class IniFileHelper
    {
        public static void RemoveSectionFromIniFile(string filePath, string sectionName)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            // Medge uses ANSI for its localisation files
            var encoding = filePath.EndsWith(".int", StringComparison.OrdinalIgnoreCase)
                ? Encoding.GetEncoding(1252)
                : Encoding.UTF8;

            var lines = File.ReadAllLines(filePath, encoding).ToList();
            var newLines = new List<string>();
            bool inTargetSection = false;
            bool sectionFound = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    var currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    
                    if (currentSection.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        inTargetSection = true;
                        sectionFound = true;
                        continue;
                    }
                    else
                    {
                        inTargetSection = false;
                    }
                }

                // skip lines that are part of the target section
                if (!inTargetSection)
                {
                    newLines.Add(line);
                }
            }

            if (sectionFound)
            {
                File.WriteAllLines(filePath, newLines, encoding);
            }
        }

        public static void RemoveKeyValuePairFromIniFile(string filePath, string sectionName, string keyValueToRemove)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            // Medge uses ANSI for its localisation files
            var encoding = filePath.EndsWith(".int", StringComparison.OrdinalIgnoreCase)
                ? Encoding.GetEncoding(1252)
                : Encoding.UTF8;

            var lines = File.ReadAllLines(filePath, encoding).ToList();
            var newLines = new List<string>();
            bool inTargetSection = false;
            bool removed = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    var currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    inTargetSection = currentSection.Equals(sectionName, StringComparison.OrdinalIgnoreCase);
                }

                if (inTargetSection && trimmedLine.Equals(keyValueToRemove, StringComparison.OrdinalIgnoreCase))
                {
                    removed = true;
                    continue;
                }

                newLines.Add(line);
            }

            if (removed)
            {
                File.WriteAllLines(filePath, newLines, encoding);
            }
        }

        public static void RewriteTdEngineDataStoreClientSection(string tdEnginePath, bool useMemmDataStores)
        {
            if (!File.Exists(tdEnginePath))
                return;

            var lines = File.ReadAllLines(tdEnginePath).ToList();
            var sectionStart = -1;
            var sectionEnd = lines.Count;

            for (int i = 0; i < lines.Count; i++)
            {
                var trimmed = lines[i].Trim();
                if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
                    continue;

                if (sectionStart >= 0)
                {
                    sectionEnd = i;
                    break;
                }

                if (trimmed.Equals("[Engine.DataStoreClient]", StringComparison.OrdinalIgnoreCase))
                {
                    sectionStart = i;
                }
            }

            if (sectionStart < 0)
                return;

            var sectionLines = lines
                .Skip(sectionStart + 1)
                .Take(sectionEnd - sectionStart - 1)
                .ToList();
            var originalSectionLines = sectionLines.ToList();

            const string tdGame = "GlobalDataStoreClasses=TdGame.UIDataStore_TdGameData";
            const string tdTimeTrial = "GlobalDataStoreClasses=TdGame.UIDataStore_TdTimeTrialData";
            const string fpGame = "GlobalDataStoreClasses=Fp.UIDataStore_TdCustomMapsGameData";
            const string fpRace = "GlobalDataStoreClasses=Fp.UIDataStore_TdCustomMapsRaceData";
            const string customTimeTrial = "GlobalDataStoreClasses=CustomStretches.UIDataStore_TdCustomTimeTrialData";
            const string customMapCheckpoints = "GlobalDataStoreClasses=CustomStretches.UIDataStore_TdCustomMapCheckpoints";
            const string mpAnchor = "GlobalDataStoreClasses=TdGame.UIDataStore_TdMPData";
            const string statsAnchor = "GlobalDataStoreClasses=TdGame.UIDataStore_TdOnlineStats";

            var removeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                tdGame,
                tdTimeTrial,
                fpGame,
                fpRace,
                customTimeTrial,
                customMapCheckpoints
            };

            sectionLines = sectionLines
                .Where(line => !removeSet.Contains(line.Trim()))
                .ToList();

            static void InsertAfter(List<string> target, string anchor, string value)
            {
                var anchorIndex = target.FindIndex(line => line.Trim().Equals(anchor, StringComparison.OrdinalIgnoreCase));
                if (anchorIndex >= 0)
                {
                    target.Insert(anchorIndex + 1, value);
                }
                else
                {
                    target.Add(value);
                }
            }

            if (useMemmDataStores)
            {
                InsertAfter(sectionLines, mpAnchor, fpGame);
                InsertAfter(sectionLines, statsAnchor, fpRace);
            }
            else
            {
                InsertAfter(sectionLines, mpAnchor, tdGame);
                InsertAfter(sectionLines, statsAnchor, tdTimeTrial);
            }

            if (originalSectionLines.SequenceEqual(sectionLines))
                return;

            lines.RemoveRange(sectionStart + 1, sectionEnd - sectionStart - 1);
            lines.InsertRange(sectionStart + 1, sectionLines);
            File.WriteAllLines(tdEnginePath, lines);
        }

        public static void MergeIniFiles(string templatePath, string targetPath)
        {
            var template = ParseIniFile(templatePath);
            var existing = File.Exists(targetPath) ? ParseIniFile(targetPath) : new IniFile();

            foreach (var templateSection in template.Sections)
            {
                var existingSections = existing.Sections
                    .Where(s => s.Name.Equals(templateSection.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!existingSections.Any())
                {
                    existing.Sections.Add(new IniSection
                    {
                        Name = templateSection.Name,
                        Keys = new List<IniKey>(templateSection.Keys),
                        Comments = new List<string>(templateSection.Comments)
                    });
                }
                else
                {
                    foreach (var templateKey in templateSection.Keys)
                    {
                        var exactPairExists = existingSections.Any(section =>
                            section.Keys.Any(k => 
                                k.Name.Equals(templateKey.Name, StringComparison.OrdinalIgnoreCase) &&
                                k.Value.Equals(templateKey.Value, StringComparison.Ordinal)));

                        if (!exactPairExists)
                        {
                            existingSections[0].Keys.Add(new IniKey
                            {
                                Name = templateKey.Name,
                                Value = templateKey.Value,
                                Comment = templateKey.Comment
                            });
                        }
                    }
                }
            }

            WriteIniFile(targetPath, existing);
        }

        private static IniFile ParseIniFile(string path)
        {
            var ini = new IniFile();
            IniSection? currentSection = null;
            var pendingComments = new List<string>();

            // Medge uses ANSI for its localisation files
            var encoding = path.EndsWith(".int", StringComparison.OrdinalIgnoreCase)
                ? Encoding.GetEncoding(1252)
                : Encoding.UTF8;

            foreach (var line in File.ReadLines(path, encoding))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var trimmed = line.Trim();

                if (trimmed.StartsWith(";") || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                {
                    pendingComments.Add(line);
                    continue;
                }

                // section header
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    var sectionName = trimmed.Substring(1, trimmed.Length - 2).Trim();
                    currentSection = new IniSection
                    {
                        Name = sectionName,
                        Comments = new List<string>(pendingComments)
                    };
                    ini.Sections.Add(currentSection);
                    pendingComments.Clear();
                    continue;
                }

                // key=value
                var equalsIndex = line.IndexOf('=');
                if (equalsIndex > 0 && currentSection != null)
                {
                    var keyName = line.Substring(0, equalsIndex).Trim();
                    var keyValue = equalsIndex < line.Length - 1 
                        ? line.Substring(equalsIndex + 1) 
                        : string.Empty;

                    string? comment = null;
                    if (pendingComments.Any())
                    {
                        comment = string.Join(Environment.NewLine, pendingComments);
                        pendingComments.Clear();
                    }

                    currentSection.Keys.Add(new IniKey
                    {
                        Name = keyName,
                        Value = keyValue,
                        Comment = comment
                    });
                }
            }

            return ini;
        }

        private static void WriteIniFile(string path, IniFile ini)
        {
            var sb = new StringBuilder();

            foreach (var section in ini.Sections)
            {
                foreach (var comment in section.Comments)
                {
                    sb.AppendLine(comment);
                }

                sb.AppendLine($"[{section.Name}]");

                foreach (var key in section.Keys)
                {
                    if (!string.IsNullOrEmpty(key.Comment))
                    {
                        sb.AppendLine(key.Comment);
                    }

                    sb.AppendLine($"{key.Name}={key.Value}");
                }

                sb.AppendLine();
            }

            // Medge uses ANSI for its localisation files
            var encoding = path.EndsWith(".int", StringComparison.OrdinalIgnoreCase)
                ? Encoding.GetEncoding(1252)
                : Encoding.UTF8;

            File.WriteAllText(path, sb.ToString(), encoding);
        }

        private class IniFile
        {
            public List<IniSection> Sections { get; set; } = new();
        }

        private class IniSection
        {
            public string Name { get; set; } = string.Empty;
            public List<IniKey> Keys { get; set; } = new();
            public List<string> Comments { get; set; } = new();
        }

        private class IniKey
        {
            public string Name { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public string? Comment { get; set; }
        }
    }
}

