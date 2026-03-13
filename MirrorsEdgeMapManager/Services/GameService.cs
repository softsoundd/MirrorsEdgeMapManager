using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.IO.Compression;

namespace MirrorsEdgeMapManager.Services;

public class GameService
{
    private readonly PathService _pathService;
    private readonly HttpClient _httpClient;

    // Retail/GOG patterns
    private static readonly byte[] RetailConfigPatternUnpatched = [0x01, 0x00, 0x00, 0x68, 0x98, 0x11, 0x05, 0x02, 0xC7, 0x05, 0xA0, 0x13, 0x05, 0x02, 0x01, 0x00];
    private static readonly byte[] RetailConfigPatternPatched = [0x01, 0x00, 0x00, 0x68, 0x98, 0x11, 0x05, 0x02, 0xC7, 0x05, 0xA0, 0x13, 0x05, 0x02, 0x00, 0x00];
    // Steam patterns
    private static readonly byte[] SteamConfigPatternUnpatched = [0x01, 0x00, 0x00, 0x68, 0xD8, 0x80, 0x03, 0x02, 0xC7, 0x05, 0xE0, 0x82, 0x03, 0x02, 0x01, 0x00];
    private static readonly byte[] SteamConfigPatternPatched = [0x01, 0x00, 0x00, 0x68, 0xD8, 0x80, 0x03, 0x02, 0xC7, 0x05, 0xE0, 0x82, 0x03, 0x02, 0x00, 0x00];
    private const int ConfigPatchOffset = 14;

    // Dependency URLs
    private const string CustomMapMenuModUrl = "https://github.com/softsoundd/MirrorsEdgeMapManager/raw/refs/heads/main/Downloads/Dependencies/CustomMapMenuModDependency.zip";
    private const string CustomMapMenuModTweaksUIUrl = "https://github.com/softsoundd/MirrorsEdgeMapManager/raw/refs/heads/main/Downloads/Dependencies/CustomMapMenuModDependency_TweaksScriptsUI.zip";
    private const string CommonAssetsUrl = "https://archive.mirrorsedgearchive.org/Mirror's%20Edge%20(2008)/Mods/unlisted_softsoundd/Mirror's%20Edge%20Map%20Manager/Dependencies/CommonAssetsDependency.zip";
    private const string ShaderCacheUrl = "https://archive.mirrorsedgearchive.org/Mirror's%20Edge%20(2008)/Mods/unlisted_softsoundd/Mirror's%20Edge%20Map%20Manager/Dependencies/ShaderCacheDependency.zip";

    private static readonly string DefaultCustomMapsIniSeed = string.Join(Environment.NewLine,
    [
        "; Mirror's Edge Map Manager generated file.",
        "; [LR_STRETCH{N} UIDataProvider_TdCustomLevelRaceStretch]",
        "; [TT_STRETCH{N} UIDataProvider_TdCustomTimeTrialStretch]",
        string.Empty
    ]);

    private static readonly string DefaultStoryExperiencesIniSeed = string.Join(Environment.NewLine,
    [
        "; Mirror's Edge Map Manager generated file.",
        "; [Scene_{N} UIDataProvider_TdCustomSceneStretch]",
        string.Empty
    ]);
    // Gamepass executable size
    private const long GamepassExecutableSize = 31606704;
    private const long SteamExecutableSize = 31946072;

    public GameService(PathService pathService)
    {
        _pathService = pathService;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    public enum PatchStatus
    {
        Patched,
        Unpatched,
        Unknown
    }

    public PatchStatus GetConfigPatchStatus(string gameInstallPath)
    {
        try
        {
            var executablePath = _pathService.GetExecutablePath(gameInstallPath);
            if (!File.Exists(executablePath))
                return PatchStatus.Unknown;

            var data = File.ReadAllBytes(executablePath);
            
            if (ContainsPattern(data, RetailConfigPatternPatched) || ContainsPattern(data, SteamConfigPatternPatched))
                return PatchStatus.Patched;
            
            if (ContainsPattern(data, RetailConfigPatternUnpatched) || ContainsPattern(data, SteamConfigPatternUnpatched))
                return PatchStatus.Unpatched;

            return PatchStatus.Unknown;
        }
        catch
        {
            return PatchStatus.Unknown;
        }
    }

    public Task<(bool success, string message)> ToggleConfigPatchAsync(
        string gameInstallPath,
        IProgress<(int percentage, string status)>? progress = null)
    {
        try
        {
            var executablePath = _pathService.GetExecutablePath(gameInstallPath);
            if (!File.Exists(executablePath))
                return Task.FromResult((false, "Game executable not found"));

            var fileInfo = new FileInfo(executablePath);
            
            // Gamepass executable patching is currently unsupported (until EA App lets me launch it to debug)
            if (fileInfo.Length == GamepassExecutableSize)
            {
                progress?.Report((100, "Gamepass executable detected - patching is not supported"));
                return Task.FromResult((false, "MEMM currently does not support patching the Gamepass executable."));
            }

            progress?.Report((80, "Applying patch..."));
            
            var data = File.ReadAllBytes(executablePath);
            var currentStatus = GetConfigPatchStatus(gameInstallPath);

            if (currentStatus == PatchStatus.Unpatched)
            {
                int patchesApplied = 0;
                
                var retailIndex = FindPattern(data, RetailConfigPatternUnpatched);
                if (retailIndex != -1)
                {
                    data[retailIndex + ConfigPatchOffset] = 0x00;
                    patchesApplied++;
                }
                
                var steamIndex = FindPattern(data, SteamConfigPatternUnpatched);
                if (steamIndex != -1)
                {
                    data[steamIndex + ConfigPatchOffset] = 0x00;
                    patchesApplied++;
                }
                
                if (patchesApplied > 0)
                {
                    File.WriteAllBytes(executablePath, data);
                    progress?.Report((100, "Patch applied"));
                    return Task.FromResult((true, "Config modification patch applied successfully"));
                }
                
                return Task.FromResult((false, "Failed to locate patch location"));
            }
            else if (currentStatus == PatchStatus.Patched)
            {
                int patchesRemoved = 0;
                
                var retailIndex = FindPattern(data, RetailConfigPatternPatched);
                if (retailIndex != -1)
                {
                    data[retailIndex + ConfigPatchOffset] = 0x01;
                    patchesRemoved++;
                }
                
                var steamIndex = FindPattern(data, SteamConfigPatternPatched);
                if (steamIndex != -1)
                {
                    data[steamIndex + ConfigPatchOffset] = 0x01;
                    patchesRemoved++;
                }
                
                if (patchesRemoved > 0)
                {
                    File.WriteAllBytes(executablePath, data);
                    progress?.Report((100, "Patch removed"));
                    return Task.FromResult((true, "Config modification patch removed successfully"));
                }
                
                return Task.FromResult((false, "Failed to locate patch location"));
            }

            return Task.FromResult((false, "Unable to determine patch status"));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"Error toggling patch: {ex.Message}"));
        }
    }

    public async Task<(bool success, string message)> InstallDependencyAsync(
        string dependencyType,
        string targetPath,
        IProgress<(int percentage, string status)>? progress = null,
        CancellationToken cancellationToken = default,
        string? gameInstallPath = null)
    {
        var url = dependencyType switch
        {
            "CustomMapMenuMod" => CustomMapMenuModUrl,
            "CustomMapMenuModTweaksUI" => CustomMapMenuModTweaksUIUrl,
            "CommonAssets" => CommonAssetsUrl,
            "ShaderCache" => ShaderCacheUrl,
            _ => throw new ArgumentException($"Unknown dependency type: {dependencyType}")
        };

        var displayName = dependencyType switch
        {
            "CustomMapMenuMod" => "Custom Map Menu Mod",
            "CustomMapMenuModTweaksUI" => "Custom Map Menu Mod (Tweaks Scripts UI)",
            "CommonAssets" => "Common Assets",
            "ShaderCache" => "Shader Cache",
            _ => dependencyType
        };

        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalSize = response.Content.Headers.ContentLength ?? 0;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memoryStream = new MemoryStream();

            var buffer = new byte[8192];
            var totalBytesRead = 0L;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;

                if (totalSize > 0)
                {
                    var percentage = (int)((totalBytesRead * 100) / totalSize);
                    var sizeMb = totalSize / (1024.0 * 1024.0);
                    var sizeGb = totalSize / (1024.0 * 1024.0 * 1024.0);
                    var sizeDisplay = totalSize >= 1024 * 1024 * 1024 
                        ? $"{sizeGb:F2} GB" 
                        : $"{sizeMb:F2} MB";
                    progress?.Report((percentage, $"Downloading {displayName}... ({sizeDisplay})"));
                }
            }

            progress?.Report((100, $"Extracting {displayName}..."));

            memoryStream.Position = 0;
            using var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
            zipArchive.ExtractToDirectory(targetPath, true);

            if ((dependencyType == "CustomMapMenuMod" || dependencyType == "CustomMapMenuModTweaksUI") && !string.IsNullOrEmpty(gameInstallPath))
            {
                progress?.Report((100, "Backing up save files..."));
                var saveBackupResult = BackupSaveFiles();
                if (!saveBackupResult.success)
                {
                    return (true, $"{displayName} installed successfully, but save backup failed: {saveBackupResult.message}");
                }

                if (dependencyType == "CustomMapMenuMod")
                {
                    var cookedPcPath = _pathService.GetCookedPcPath();
                    var tweaksUIMarkerFile = Path.Combine(cookedPcPath, "UI", "TdUI_SofTimer.upk");
                    if (File.Exists(tweaksUIMarkerFile))
                    {
                        progress?.Report((100, "Removing Tweaks UI files..."));
                        try
                        {
                            File.Delete(tweaksUIMarkerFile);
                        }
                        catch
                        {
                        }
                    }
                }

                progress?.Report((100, "Installing config files..."));
                var configResult = InstallConfigFiles(gameInstallPath, progress);
                if (!configResult.success)
                {
                    return (true, $"{displayName} installed successfully, but config files failed: {configResult.message}");
                }

                var tdEngineResult = EnsureTdEngineMemmDataStores();
                if (!tdEngineResult.success)
                {
                    return (true, $"{displayName} installed successfully, but TdEngine.ini update failed: {tdEngineResult.message}");
                }
            }

            return (true, $"{displayName} installed successfully");
        }
        catch (OperationCanceledException)
        {
            return (false, "Installation cancelled");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to install {displayName}: {ex.Message}");
        }
    }

    public DependencyStatus GetDependencyStatus(string gameInstallPath, string memmFolderPath, string cookedPcPath)
    {
        var status = new DependencyStatus();

        status.CustomMapMenuModInstalled = 
            File.Exists(Path.Combine(cookedPcPath, "CustomStretches.u")) &&
            File.Exists(Path.Combine(cookedPcPath, "Fp.u")) &&
            File.Exists(Path.Combine(cookedPcPath, "Maps", "Menu", "TdMainMenu.me1")) &&
            File.Exists(Path.Combine(cookedPcPath, "UI", "TdUI_Custom_Races.upk")) &&
            File.Exists(Path.Combine(cookedPcPath, "UI", "TdUI_FrontEnd.upk"));

        // check if Tweaks UI variant is installed (has the additional TdUI_SofTimer.upk file)
        var tweaksUIMarkerFile = Path.Combine(cookedPcPath, "UI", "TdUI_SofTimer.upk");
        status.IsTweaksUIVariant = File.Exists(tweaksUIMarkerFile);

        status.CommonAssetsInstalled = IsCommonAssetsInstalled(memmFolderPath);

        var shaderCachePath = Path.Combine(cookedPcPath, "LocalShaderCache-PC-D3D-SM3.upk");
        status.ShaderCacheInstalled = File.Exists(shaderCachePath) && 
            new FileInfo(shaderCachePath).Length >= 73400320;

        status.ConfigFilesInstalled =
            File.Exists(_pathService.GetDefaultEngineIniPath(gameInstallPath)) &&
            File.Exists(_pathService.GetDefaultGameIniPath(gameInstallPath)) &&
            File.Exists(_pathService.GetDefaultCustomMapsIniPath(gameInstallPath)) &&
            File.Exists(_pathService.GetDefaultStoryExperiencesIniPath(gameInstallPath)) &&
            File.Exists(_pathService.GetTdGameUiPath(gameInstallPath));

        return status;
    }

    private static bool IsCommonAssetsInstalled(string memmFolderPath)
    {
        var commonAssetsPath = Path.Combine(memmFolderPath, "Common Assets");
        if (!Directory.Exists(commonAssetsPath))
            return false;

        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(commonAssetsPath))
            {
                var entryName = Path.GetFileName(entry);
                if (string.IsNullOrWhiteSpace(entryName))
                    continue;

                // DLC assets alone should not count as common assets being installed
                if (string.Equals(entryName, "DLC Assets", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entryName, "desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entryName, "thumbs.db", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public (bool success, string message) InstallConfigFiles(string gameInstallPath, IProgress<(int percentage, string status)>? progress = null)
    {
        try
        {
            progress?.Report((0, "Installing config files..."));

            var configFolder = _pathService.GetConfigFolderPath(gameInstallPath);
            _pathService.EnsureDirectoryExists(configFolder);

            var localizationFolder = Path.GetDirectoryName(_pathService.GetTdGameUiPath(gameInstallPath));
            if (localizationFolder != null)
            {
                _pathService.EnsureDirectoryExists(localizationFolder);
            }

            var filesToMerge = new[]
            {
                ("DefaultCustom_Maps.ini", _pathService.GetDefaultCustomMapsIniPath(gameInstallPath), DefaultCustomMapsIniSeed),
                ("DefaultEngine.ini", _pathService.GetDefaultEngineIniPath(gameInstallPath), (string?)null),
                ("DefaultGame.ini", _pathService.GetDefaultGameIniPath(gameInstallPath), (string?)null),
                ("DefaultStory_Experiences.ini", _pathService.GetDefaultStoryExperiencesIniPath(gameInstallPath), DefaultStoryExperiencesIniSeed),
                ("TdGameUI.int", _pathService.GetTdGameUiPath(gameInstallPath), (string?)null)
            };

            int filesProcessed = 0;
            foreach (var (templateFile, targetPath, inCodeSeed) in filesToMerge)
            {
                if (!string.IsNullOrEmpty(inCodeSeed))
                {
                    var created = EnsureSeedFileExists(targetPath, inCodeSeed);
                    filesProcessed++;
                    progress?.Report((filesProcessed * 100 / filesToMerge.Length, $"{(created ? "Created" : "Verified")} {templateFile}"));
                    continue;
                }

                var existedBeforeMerge = File.Exists(targetPath);

                if (templateFile == "DefaultGame.ini" && File.Exists(targetPath))
                {
                    EnsureDefaultGameStockBackup(gameInstallPath, targetPath);
                }

                // cleanup legacy lines that can crash startup if left
                if (templateFile == "DefaultEngine.ini" && File.Exists(targetPath))
                {
                    Helpers.IniFileHelper.RemoveKeyValuePairFromIniFile(
                        targetPath,
                        "Engine.DataStoreClient",
                        "+GlobalDataStoreClasses=\"TdGame.UIDataStore_TdStringAliasMap");
                    Helpers.IniFileHelper.RemoveKeyValuePairFromIniFile(
                        targetPath,
                        "Engine.DataStoreClient",
                        "GlobalDataStoreClasses=\"TdGame.UIDataStore_TdStringAliasMap");
                    Helpers.IniFileHelper.RemoveKeyValuePairFromIniFile(
                        targetPath,
                        "Engine.DataStoreClient",
                        "+GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomTimeTrialData\"");
                    Helpers.IniFileHelper.RemoveKeyValuePairFromIniFile(
                        targetPath,
                        "Engine.DataStoreClient",
                        "+GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomMapCheckpoints\"");
                    Helpers.IniFileHelper.RemoveKeyValuePairFromIniFile(
                        targetPath,
                        "Engine.DataStoreClient",
                        "GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomTimeTrialData\"");
                    Helpers.IniFileHelper.RemoveKeyValuePairFromIniFile(
                        targetPath,
                        "Engine.DataStoreClient",
                        "GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomMapCheckpoints\"");
                }

                if (!Helpers.ConfigTemplateProvider.MergeTemplateIntoIniFile(templateFile, targetPath, out var mergeError))
                {
                    return (false, $"Failed to apply {templateFile}: {mergeError}");
                }

                filesProcessed++;
                var mergeAction = existedBeforeMerge ? "Merged" : "Created";
                progress?.Report((filesProcessed * 100 / filesToMerge.Length, $"{mergeAction} {templateFile}"));
            }

            progress?.Report((100, "Config files installed"));
            return (true, "Config files installed successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to install config files: {ex.Message}");
        }
    }

    public (bool success, string message) LaunchGame(string gameInstallPath)
    {
        try
        {
            var executablePath = _pathService.GetExecutablePath(gameInstallPath);
            if (!File.Exists(executablePath))
                return (false, "Game executable not found");

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            return (true, "Game launched successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to launch game: {ex.Message}");
        }
    }

    public bool IsSteamVersion(string gameInstallPath)
    {
        try
        {
            var executablePath = _pathService.GetExecutablePath(gameInstallPath);
            if (!File.Exists(executablePath))
                return false;

            var fileInfo = new FileInfo(executablePath);
            return fileInfo.Length == SteamExecutableSize;
        }
        catch
        {
            return false;
        }
    }

    private static bool ContainsPattern(byte[] data, byte[] pattern)
    {
        return FindPattern(data, pattern) != -1;
    }

    private static int FindPattern(byte[] data, byte[] pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
                return i;
        }
        return -1;
    }

    private static bool EnsureSeedFileExists(string targetPath, string content)
    {
        if (File.Exists(targetPath))
            return false;

        File.WriteAllText(targetPath, content);
        return true;
    }

    private void EnsureDefaultGameStockBackup(string gameInstallPath, string defaultGamePath)
    {
        var backupPath = _pathService.GetDefaultGameStockBackupPath(gameInstallPath);
        if (File.Exists(backupPath) || !File.Exists(defaultGamePath))
            return;

        if (LooksLikeMemmModifiedDefaultGame(defaultGamePath))
            return;

        File.Copy(defaultGamePath, backupPath, false);
    }

    private (bool success, string message) BackupSaveFiles()
    {
        try
        {
            var savefilesPath = _pathService.GetDocumentsSavefilesPath();
            if (!Directory.Exists(savefilesPath))
            {
                return (true, "Savefiles directory does not exist");
            }

            var saveFiles = Directory.GetFiles(savefilesPath, "*.dat", SearchOption.AllDirectories);
            foreach (var saveFile in saveFiles)
            {
                var backupPath = Path.ChangeExtension(saveFile, ".bak");
                File.Copy(saveFile, backupPath, true);
            }

            return (true, $"Backed up {saveFiles.Length} save file(s)");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private (bool success, string message) EnsureTdEngineMemmDataStores()
    {
        var tdEnginePath = _pathService.GetDocumentsTdEngineIniPath();

        try
        {
            var tdEngineDirectory = Path.GetDirectoryName(tdEnginePath);
            if (!string.IsNullOrEmpty(tdEngineDirectory))
            {
                Directory.CreateDirectory(tdEngineDirectory);
            }

            if (!File.Exists(tdEnginePath))
            {
                return (true, "TdEngine.ini does not exist yet");
            }

            EnsureFileWritable(tdEnginePath);
            Helpers.IniFileHelper.RewriteTdEngineDataStoreClientSection(tdEnginePath, useMemmDataStores: true);

            if (File.Exists(tdEnginePath))
            {
                var updatedAttributes = File.GetAttributes(tdEnginePath);
                File.SetAttributes(tdEnginePath, updatedAttributes | FileAttributes.ReadOnly);
            }

            return (true, "TdEngine.ini updated");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static void EnsureFileWritable(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var attributes = File.GetAttributes(filePath);
        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
        }
    }

    private static bool LooksLikeMemmModifiedDefaultGame(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return content.Contains("[Fp.UIDataStore_TdCustomMapsGameData]", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("[Fp.UIDataStore_TdCustomMapsTimeTrialData]", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("[Fp.UIDataStore_TdCustomMapsRaceData]", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("CustomStretches.UIDataProvider_TdCustom", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

public class DependencyStatus
{
    public bool CustomMapMenuModInstalled { get; set; }
    public bool IsTweaksUIVariant { get; set; }
    public bool CommonAssetsInstalled { get; set; }
    public bool ShaderCacheInstalled { get; set; }
    public bool ConfigFilesInstalled { get; set; }

    // only custom map menu mod and config files are hard requirements
    public bool AllRequiredInstalled => CustomMapMenuModInstalled && ConfigFilesInstalled;
}

