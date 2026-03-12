using MirrorsEdgeMapManager.Models;
using System.IO;

namespace MirrorsEdgeMapManager.Services;

public class ConfigurationService
{
    private readonly string _configPath;
    private const string ConfigFileName = "memm.ini";

    public ConfigurationService()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = Path.Combine(appDirectory, ConfigFileName);
    }

    public AppConfiguration LoadConfiguration()
    {
        var config = new AppConfiguration();

        if (!File.Exists(_configPath))
        {
            SaveConfiguration(config);
            return config;
        }

        try
        {
            var lines = File.ReadAllLines(_configPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("[") || trimmed.StartsWith(";"))
                    continue;

                var parts = trimmed.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "GameInstallPath":
                        config.GameInstallPath = value;
                        break;
                    case "MEMMLocation":
                        config.MEMMLocation = value;
                        break;
                }
            }
        }
        catch
        {
        }

        return config;
    }

    public void SaveConfiguration(AppConfiguration config)
    {
        try
        {
            var content = $"""
                [MirrorsEdgeMapManager]
                GameInstallPath={config.GameInstallPath}
                MEMMLocation={config.MEMMLocation}

                """;
            File.WriteAllText(_configPath, content);
        }
        catch
        {
        }
    }
}

