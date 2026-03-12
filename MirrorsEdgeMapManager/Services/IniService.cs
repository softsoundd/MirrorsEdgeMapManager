using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MirrorsEdgeMapManager.Services;

public class IniService
{
    public Dictionary<string, Dictionary<string, string>> ReadIniFile(string filePath)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
            return result;

        try
        {
            var lines = File.ReadAllLines(filePath);
            string? currentSection = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.Contains("]"))
                {
                    currentSection = trimmed.TrimStart('[').Split(']')[0];
                    if (!result.ContainsKey(currentSection))
                    {
                        result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                else if (currentSection != null && trimmed.Contains("="))
                {
                    var parts = trimmed.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        result[currentSection][key] = value;
                    }
                }
            }
        }
        catch
        {
        }

        return result;
    }

    public void WriteIniFile(string filePath, Dictionary<string, Dictionary<string, string>> data)
    {
        var sb = new StringBuilder();

        foreach (var section in data)
        {
            sb.AppendLine($"[{section.Key}]");
            foreach (var kvp in section.Value)
            {
                sb.AppendLine($"{kvp.Key}={kvp.Value}");
            }
            sb.AppendLine();
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public void AppendToIniFile(string filePath, string sectionName, Dictionary<string, string> values)
    {
        var sb = new StringBuilder();
        
        if (File.Exists(filePath))
        {
            sb.Append(File.ReadAllText(filePath));
            if (!sb.ToString().EndsWith("\n"))
                sb.AppendLine();
        }

        sb.AppendLine($"[{sectionName}]");
        foreach (var kvp in values)
        {
            sb.AppendLine($"{kvp.Key}={kvp.Value}");
        }
        sb.AppendLine();

        File.WriteAllText(filePath, sb.ToString());
    }

    public void RemoveSectionByFriendlyName(string filePath, string friendlyName)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath).ToList();
        var result = new List<string>();
        var skipSection = false;
        var foundInSection = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("[") && line.Contains("]"))
            {
                if (skipSection && !foundInSection)
                {
                }
                skipSection = false;
                foundInSection = false;

                for (int j = i + 1; j < lines.Count; j++)
                {
                    var nextLine = lines[j].Trim();
                    if (nextLine.StartsWith("["))
                        break;
                    if (nextLine.StartsWith("FriendlyName=") && nextLine.Substring(13) == friendlyName)
                    {
                        skipSection = true;
                        foundInSection = true;
                        break;
                    }
                }
            }

            if (!skipSection)
            {
                result.Add(lines[i]);
            }
        }

        File.WriteAllText(filePath, string.Join(Environment.NewLine, result));
    }

    public int GetNextStretchNumber(string filePath, string stretchType)
    {
        var data = ReadIniFile(filePath);
        var maxNumber = 0;

        var pattern = new Regex($@"{stretchType}(\d+)", RegexOptions.IgnoreCase);

        foreach (var section in data.Keys)
        {
            var match = pattern.Match(section);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
            {
                if (num > maxNumber)
                    maxNumber = num;
            }
        }

        return maxNumber + 1;
    }

    public bool SectionContainsFriendlyName(string filePath, string friendlyName)
    {
        var data = ReadIniFile(filePath);

        foreach (var section in data.Values)
        {
            if (section.TryGetValue("FriendlyName", out var name) && name == friendlyName)
            {
                return true;
            }
        }

        return false;
    }

    public void ReorganiseSections(string filePath)
    {
        var data = ReadIniFile(filePath);
        
        var lrSections = new List<(string name, Dictionary<string, string> values)>();
        var ttSections = new List<(string name, Dictionary<string, string> values)>();

        foreach (var section in data)
        {
            if (section.Key.Contains("LR_STRETCH"))
            {
                lrSections.Add((section.Key, section.Value));
            }
            else if (section.Key.Contains("TT_STRETCH"))
            {
                ttSections.Add((section.Key, section.Value));
            }
        }

        lrSections = lrSections
            .OrderBy(s => s.values.GetValueOrDefault("FriendlyName", ""))
            .ToList();
        ttSections = ttSections
            .OrderBy(s => s.values.GetValueOrDefault("FriendlyName", ""))
            .ToList();

        var newData = new Dictionary<string, Dictionary<string, string>>();

        for (int i = 0; i < lrSections.Count; i++)
        {
            var newName = $"LR_STRETCH{i + 1} UIDataProvider_TdCustomLevelRaceStretch";
            newData[newName] = lrSections[i].values;
        }

        for (int i = 0; i < ttSections.Count; i++)
        {
            var newName = $"TT_STRETCH{i + 1} UIDataProvider_TdCustomTimeTrialStretch";
            newData[newName] = ttSections[i].values;
        }

        WriteIniFile(filePath, newData);
    }

    public void ReorganiseGameIniTtSections(string filePath)
    {
        var data = ReadIniFile(filePath);
        
        var ttSections = new List<(string name, Dictionary<string, string> values)>();
        var otherSections = new Dictionary<string, Dictionary<string, string>>();

        foreach (var section in data)
        {
            if (section.Key.Contains("TT_STRETCH") && section.Key.Contains("UIDataProvider_TdTimeTrialStretch"))
            {
                ttSections.Add((section.Key, section.Value));
            }
            else
            {
                otherSections[section.Key] = section.Value;
            }
        }

        ttSections = ttSections
            .OrderBy(s => s.values.GetValueOrDefault("FriendlyName", ""))
            .ToList();

        var newData = new Dictionary<string, Dictionary<string, string>>(otherSections);

        for (int i = 0; i < ttSections.Count; i++)
        {
            var newName = $"TT_STRETCH{i + 1} UIDataProvider_TdTimeTrialStretch";
            newData[newName] = ttSections[i].values;
        }

        WriteIniFile(filePath, newData);
    }

    public void InsertLineAfterMarker(string filePath, string marker, string lineToInsert, Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding(1252);
        
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath, encoding).ToList();
        
        if (lines.Contains(lineToInsert))
            return;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains(marker))
            {
                lines.Insert(i + 1, lineToInsert);
                break;
            }
        }

        File.WriteAllLines(filePath, lines, encoding);
    }

    public void AppendContentIfNotExists(string filePath, string content)
    {
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, content);
            return;
        }

        var existingContent = File.ReadAllText(filePath);
        if (!existingContent.Contains(content.Trim()))
        {
            File.AppendAllText(filePath, Environment.NewLine + content);
        }
    }

    public void RemoveLine(string filePath, string lineToRemove, Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding(1252);
        
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath, encoding).ToList();
        lines.RemoveAll(line => line.Trim() == lineToRemove.Trim());
        File.WriteAllLines(filePath, lines, encoding);
    }

    public void RemoveStoryExperienceByFriendlyName(string filePath, string friendlyName)
    {
        if (!File.Exists(filePath))
            return;

        var data = ReadIniFile(filePath);
        string? sceneNumber = null;
        string? mainSectionName = null;

        foreach (var section in data)
        {
            if (section.Key.Contains("UIDataProvider_TdCustomSceneStretch"))
            {
                if (section.Value.TryGetValue("FriendlyName", out var name) && name == friendlyName)
                {
                    if (section.Value.TryGetValue("SceneNumber", out var sceneNum))
                    {
                        sceneNumber = sceneNum;
                        mainSectionName = section.Key;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(sceneNumber) || string.IsNullOrEmpty(mainSectionName))
            return;

        var lines = File.ReadAllLines(filePath).ToList();
        var result = new List<string>();
        var skipSection = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("[") && line.Contains("]"))
            {
                var sectionName = line.TrimStart('[').Split(']')[0];
                
                var sectionFirstPart = sectionName.Split(' ')[0];
                var mainSectionFirstPart = mainSectionName.Split(' ')[0];
                
                if (sectionFirstPart == mainSectionFirstPart || 
                    sectionFirstPart.StartsWith(sceneNumber + "_", StringComparison.OrdinalIgnoreCase))
                {
                    skipSection = true;
                }
                else
                {
                    skipSection = false;
                }
            }

            if (!skipSection)
            {
                result.Add(lines[i]);
            }
        }

        File.WriteAllText(filePath, string.Join(Environment.NewLine, result));
    }
}

