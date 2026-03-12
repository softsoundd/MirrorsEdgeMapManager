using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MirrorsEdgeMapManager.Services;

public class PathService
{
    private const int CSIDL_PERSONAL = 5;
    private const int SHGFP_TYPE_CURRENT = 0;

    [DllImport("shell32.dll")]
    private static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, StringBuilder pszPath);

    public string GetDocumentsPath()
    {
        var path = new StringBuilder(260);
        SHGetFolderPath(IntPtr.Zero, CSIDL_PERSONAL, IntPtr.Zero, SHGFP_TYPE_CURRENT, path);
        return path.ToString();
    }

    public string GetMirrorsEdgeDocumentsPath()
    {
        return Path.Combine(GetDocumentsPath(), "EA Games", "Mirror's Edge");
    }

    public string GetPublishedPath()
    {
        return Path.Combine(GetMirrorsEdgeDocumentsPath(), "TdGame", "Published");
    }

    public string GetCookedPcPath()
    {
        return Path.Combine(GetPublishedPath(), "CookedPC");
    }

    public string GetMemmFolderPath(string gameInstallPath, string memmLocation)
    {
        if (memmLocation == "Game" && !string.IsNullOrEmpty(gameInstallPath))
        {
            return Path.Combine(gameInstallPath, "TdGame", "CookedPC", "MEMM");
        }
        return Path.Combine(GetCookedPcPath(), "MEMM");
    }

    public string GetMemmInactiveFolderPath(string gameInstallPath, string memmLocation)
    {
        if (memmLocation == "Game" && !string.IsNullOrEmpty(gameInstallPath))
        {
            return Path.Combine(gameInstallPath, "TdGame", "MEMMInactive");
        }
        return Path.Combine(GetPublishedPath(), "MEMMInactive");
    }

    public string GetExecutablePath(string gameInstallPath)
    {
        return Path.Combine(gameInstallPath, "Binaries", "MirrorsEdge.exe");
    }

    public string GetConfigFolderPath(string gameInstallPath)
    {
        return Path.Combine(gameInstallPath, "TdGame", "Config");
    }

    public string GetDefaultEngineIniPath(string gameInstallPath)
    {
        return Path.Combine(GetConfigFolderPath(gameInstallPath), "DefaultEngine.ini");
    }

    public string GetDefaultGameIniPath(string gameInstallPath)
    {
        return Path.Combine(GetConfigFolderPath(gameInstallPath), "DefaultGame.ini");
    }

    public string GetDefaultGameStockBackupPath(string gameInstallPath)
    {
        return Path.Combine(GetConfigFolderPath(gameInstallPath), "DefaultGame.MEMMStockBackup.ini");
    }

    public string GetDefaultGameModdedBackupPath(string gameInstallPath)
    {
        return Path.Combine(GetConfigFolderPath(gameInstallPath), "DefaultGame.MEMMModdedBackup.ini");
    }

    public string GetDefaultCustomMapsIniPath(string gameInstallPath)
    {
        return Path.Combine(GetConfigFolderPath(gameInstallPath), "DefaultCustom_Maps.ini");
    }

    public string GetDefaultStoryExperiencesIniPath(string gameInstallPath)
    {
        return Path.Combine(GetConfigFolderPath(gameInstallPath), "DefaultStory_Experiences.ini");
    }

    public string GetTdGameUiPath(string gameInstallPath)
    {
        return Path.Combine(gameInstallPath, "TdGame", "Localization", "INT", "TdGameUI.int");
    }

    public string GetDocumentsTdEngineIniPath()
    {
        return Path.Combine(GetMirrorsEdgeDocumentsPath(), "TdGame", "Config", "TdEngine.ini");
    }

    public bool ValidateGameInstallPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var executablePath = GetExecutablePath(path);
        return File.Exists(executablePath);
    }

    public bool DocumentsPathExists()
    {
        return Directory.Exists(GetMirrorsEdgeDocumentsPath());
    }

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
            return 0;

        long size = 0;
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                var fileInfo = new FileInfo(file);
                if (!fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    size += fileInfo.Length;
                }
            }
            catch
            {
            }
        }
        return size;
    }

    public string GetDriveLetter(string path)
    {
        return Path.GetPathRoot(path) ?? "C:\\";
    }

    public long GetDriveFreeSpace(string path)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
            return drive.AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }

    public string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

