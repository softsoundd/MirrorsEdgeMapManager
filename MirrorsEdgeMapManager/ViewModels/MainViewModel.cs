using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MirrorsEdgeMapManager.Helpers;
using MirrorsEdgeMapManager.Models;
using MirrorsEdgeMapManager.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MirrorsEdgeMapManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigurationService _configService;
    private readonly PathService _pathService;
    private readonly MapService _mapService;
    private readonly GameService _gameService;
    private readonly IniService _iniService;
    private readonly SpeedrunService _speedrunService;
    private CancellationTokenSource? _downloadCts;
    private static readonly string DefaultGameVanillaDataStoreSeed = string.Join(Environment.NewLine,
    [
        "[TdGame.UIDataStore_TdGameData]",
        "+ElementProviderTypes=(ProviderTag=\"TdMaps\",ProviderClassName=\"TdGame.UIDataProvider_TdMaps\")",
        string.Empty,
        "[TdGame.UIDataStore_TdTimeTrialData]",
        "+ElementProviderTypes=(ProviderTag=\"TdTimeTrialStretches\",ProviderClassName=\"TdGame.UIDataProvider_TdTimeTrialStretch\")",
        "+ElementProviderTypes=(ProviderTag=\"TdLevelRaceStretches\",ProviderClassName=\"TdGame.UIDataProvider_TdLevelRaceStretch\")",
        "WeeklyGhostCutoffRank=500",
        "MonthlyGhostCutoffRank=500",
        string.Empty
    ]);
    [ObservableProperty]
    private string _gameInstallPath = string.Empty;

    [ObservableProperty]
    private bool _isValidGamePath;

    [ObservableProperty]
    private bool _isDocumentsPathValid;

    [ObservableProperty]
    private string _documentsStatus = "Checking...";

    [ObservableProperty]
    private string _documentsStatusValue = "Checking...";

    [ObservableProperty]
    private string _documentsStatusColor = "#757575";

    [ObservableProperty]
    private string _documentsStatusBackground = "#F5F5F5";

    [ObservableProperty]
    private string _patchStatus = "Unknown";

    [ObservableProperty]
    private string _patchStatusValue = "Unknown";

    [ObservableProperty]
    private string _patchStatusColor = "#757575";

    [ObservableProperty]
    private string _patchStatusBackground = "#F5F5F5";

    [ObservableProperty]
    private bool _isPatchActive;

    [ObservableProperty]
    private string _dependenciesStatus = "Not checked";

    [ObservableProperty]
    private string _dependenciesStatusValue = "Not checked";

    [ObservableProperty]
    private string _dependenciesStatusColor = "#757575";

    [ObservableProperty]
    private string _dependenciesStatusBackground = "#F5F5F5";

    [ObservableProperty]
    private bool _areDependenciesInstalled;

    [ObservableProperty]
    private string _dependenciesChipBackground = "#F5F5F5";

    [ObservableProperty]
    private string _dependenciesChipBorder = "#BDBDBD";

    [ObservableProperty]
    private string _dependenciesChipForeground = "#424242";

    [ObservableProperty]
    private string _memmLocation = "Published";

    [ObservableProperty]
    private string _mapsStorageInfo = "";

    [ObservableProperty]
    private bool _isMemmDisabled;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingStatus = "";

    [ObservableProperty]
    private int _loadingProgress;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string _downloadStatus = "";

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private ObservableCollection<MapViewModel> _customMaps = [];

    [ObservableProperty]
    private ObservableCollection<MapViewModel> _customTimeTrials = [];

    [ObservableProperty]
    private ObservableCollection<MapViewModel> _storyExperiences = [];

    [ObservableProperty]
    private MapViewModel? _selectedMap;

    [ObservableProperty]
    private string _selectedMapName = "Click on a map for details";

    [ObservableProperty]
    private string _selectedMapAuthor = "";

    [ObservableProperty]
    private string _selectedMapDate = "";

    [ObservableProperty]
    private string _selectedMapDescription = "";

    [ObservableProperty]
    private BitmapImage? _selectedMapImage;

    [ObservableProperty]
    private ObservableCollection<string> _speedrunTopRuns = [];

    [ObservableProperty]
    private string _speedrunLeaderboardUrl = "";

    [ObservableProperty]
    private bool _hasSpeedrunStats;

    [ObservableProperty]
    private bool _isSpeedrunExpanded;

    [ObservableProperty]
    private bool _isSpeedrunLoading;

    [ObservableProperty]
    private bool _noSpeedrunsFound;

    [ObservableProperty]
    private bool _isMapDetailsLoading;

    [ObservableProperty]
    private bool _showMapDetails;

    [ObservableProperty]
    private string _selectedMapsCount = "Selected maps: 0";

    [ObservableProperty]
    private string _selectedMapsSize = "";

    [ObservableProperty]
    private ObservableCollection<string> _selectedMapsList = [];

    public bool IsMemmInteractionEnabled => !IsMemmDisabled;
    public bool CanDownloadMaps => IsMemmInteractionEnabled && !IsDownloading;

    partial void OnIsMemmDisabledChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMemmInteractionEnabled));
        OnPropertyChanged(nameof(CanDownloadMaps));
    }

    partial void OnIsDownloadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDownloadMaps));
    }

    public MainViewModel()
    {
        _configService = new ConfigurationService();
        _pathService = new PathService();
        _mapService = new MapService(_pathService);
        _gameService = new GameService(_pathService);
        _iniService = new IniService();
        _speedrunService = new SpeedrunService();
    }

    public async Task InitialiseAsync()
    {
        LoadConfiguration();
        CheckDocumentsPath();
        await RefreshMapsAsync();
        UpdatePatchStatus();
        UpdateDependenciesStatus();
        CalculateMapsSize();
    }

    private void LoadConfiguration()
    {
        var config = _configService.LoadConfiguration();
        GameInstallPath = config.GameInstallPath;
        MemmLocation = config.MEMMLocation;
        ValidateGamePath();
    }

    private void SaveConfiguration()
    {
        _configService.SaveConfiguration(new AppConfiguration
        {
            GameInstallPath = GameInstallPath,
            MEMMLocation = MemmLocation
        });
    }

    private void ValidateGamePath()
    {
        IsValidGamePath = _pathService.ValidateGameInstallPath(GameInstallPath);
    }

    private void CheckDocumentsPath()
    {
        if (_pathService.DocumentsPathExists())
        {
            IsDocumentsPathValid = true;
            DocumentsStatus = "Documents Configs: Found";
            DocumentsStatusValue = "Found";
            DocumentsStatusColor = "#2E7D32";
            DocumentsStatusBackground = "#E8F5E9";

            var publishedPath = _pathService.GetPublishedPath();
            _pathService.EnsureDirectoryExists(publishedPath);

            var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
            _pathService.EnsureDirectoryExists(memmPath);

            var inactivePath = _pathService.GetMemmInactiveFolderPath(GameInstallPath, MemmLocation);
            _pathService.EnsureDirectoryExists(inactivePath);

            CheckMemmDisabledState();
        }
        else
        {
            IsDocumentsPathValid = false;
            DocumentsStatus = "Documents Configs: Not Found";
            DocumentsStatusValue = "Not Found";
            DocumentsStatusColor = "#C62828";
            DocumentsStatusBackground = "#FFEBEE";
        }
    }

    private void CheckMemmDisabledState()
    {
        var inactivePath = _pathService.GetMemmInactiveFolderPath(GameInstallPath, MemmLocation);
        var publishedInactivePath = GetPublishedMemmInactiveFolderPath();
        IsMemmDisabled = HasAnyContents(inactivePath) || HasAnyContents(publishedInactivePath);
    }

    private string GetPublishedMemmInactiveFolderPath()
    {
        return Path.Combine(_pathService.GetPublishedPath(), "MEMMInactive");
    }

    private static bool HasAnyContents(string path)
    {
        try
        {
            return Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any();
        }
        catch
        {
            return false;
        }
    }

    private bool HasInactiveCustomMapMenuFiles()
    {
        var publishedInactivePath = GetPublishedMemmInactiveFolderPath();
        return Directory.Exists(Path.Combine(publishedInactivePath, "Maps")) &&
               Directory.Exists(Path.Combine(publishedInactivePath, "UI")) &&
               File.Exists(Path.Combine(publishedInactivePath, "CustomStretches.u")) &&
               File.Exists(Path.Combine(publishedInactivePath, "Fp.u"));
    }

    private void UpdatePatchStatus()
    {
        if (!IsValidGamePath)
        {
            PatchStatus = "Unlocked Configs: Invalid game path";
            PatchStatusValue = "Invalid Path";
            PatchStatusColor = "#C62828";
            PatchStatusBackground = "#FFEBEE";
            IsPatchActive = false;
            return;
        }

        var status = _gameService.GetConfigPatchStatus(GameInstallPath);
        switch (status)
        {
            case GameService.PatchStatus.Patched:
                PatchStatus = "Unlocked Configs: Patched";
                PatchStatusValue = "Patched";
                PatchStatusColor = "#2E7D32";
                PatchStatusBackground = "#E8F5E9";
                IsPatchActive = true;
                break;
            case GameService.PatchStatus.Unpatched:
                PatchStatus = "Unlocked Configs: Unpatched";
                PatchStatusValue = "Unpatched";
                PatchStatusColor = "#757575";
                PatchStatusBackground = "#F5F5F5";
                IsPatchActive = false;
                break;
            case GameService.PatchStatus.Mixed:
                PatchStatus = "Unlocked Configs: Partially Patched";
                PatchStatusValue = "Partially Patched";
                PatchStatusColor = "#EF6C00";
                PatchStatusBackground = "#FFF3E0";
                IsPatchActive = false;
                break;
            case GameService.PatchStatus.NotApplicable:
                PatchStatus = "Unlocked Configs: Not Applicable";
                PatchStatusValue = "Not Applicable";
                PatchStatusColor = "#757575";
                PatchStatusBackground = "#F5F5F5";
                IsPatchActive = false;
                break;
            default:
                PatchStatus = "Unlocked Configs: Unknown";
                PatchStatusValue = "Unknown";
                PatchStatusColor = "#EF6C00";
                PatchStatusBackground = "#FFF3E0";
                IsPatchActive = false;
                break;
        }
    }

    private void UpdateDependenciesStatus()
    {
        if (!IsValidGamePath || !IsDocumentsPathValid)
        {
            DependenciesStatus = "Dependencies: Missing";
            DependenciesStatusValue = "Missing";
            DependenciesStatusColor = "#C62828";
            DependenciesStatusBackground = "#FFEBEE";
            AreDependenciesInstalled = false;
            return;
        }

        var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
        var cookedPcPath = _pathService.GetCookedPcPath();
        var status = _gameService.GetDependencyStatus(GameInstallPath, memmPath, cookedPcPath);

        if (IsMemmDisabled && !status.CustomMapMenuModInstalled && HasInactiveCustomMapMenuFiles())
        {
            status.CustomMapMenuModInstalled = true;
        }

        if (status.AllRequiredInstalled)
        {
            DependenciesStatus = "Dependencies: Installed";
            DependenciesStatusValue = "Installed";
            DependenciesStatusColor = "#2E7D32";
            DependenciesStatusBackground = "#E8F5E9";
            AreDependenciesInstalled = true;
            
            DependenciesChipBackground = "#F5F5F5";
            DependenciesChipBorder = "#BDBDBD";
            DependenciesChipForeground = "#424242";
        }
        else
        {
            var missing = new List<string>();
            if (!status.CustomMapMenuModInstalled) missing.Add("Menu Mod");
            if (!status.ConfigFilesInstalled) missing.Add("Configs");
            
            var missingList = string.Join(", ", missing);
            DependenciesStatus = $"Dependencies: Missing {missingList}";
            DependenciesStatusValue = $"Missing {missingList}";
            DependenciesStatusColor = "#C62828";
            DependenciesStatusBackground = "#FFEBEE";
            AreDependenciesInstalled = false;
            
            DependenciesChipBackground = "#FFEBEE";
            DependenciesChipBorder = "#C62828";
            DependenciesChipForeground = "#C62828";
        }
    }

    private void CalculateMapsSize()
    {
        if (!IsDocumentsPathValid)
            return;

        var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
        var inactivePath = _pathService.GetMemmInactiveFolderPath(GameInstallPath, MemmLocation);
        var activePath = memmPath;
        if (IsMemmDisabled)
        {
            activePath = HasAnyContents(inactivePath) ? inactivePath : GetPublishedMemmInactiveFolderPath();
        }
        if (!Directory.Exists(activePath))
        {
            activePath = memmPath;
        }

        var size = _pathService.GetDirectorySize(activePath);
        var sizeDisplay = _pathService.FormatFileSize(size);

        var driveLetter = _pathService.GetDriveLetter(activePath);
        var freeSpace = _pathService.GetDriveFreeSpace(activePath);
        var freeSpaceDisplay = _pathService.FormatFileSize(freeSpace);

        MapsStorageInfo = $"Maps stored in {MemmLocation} folder = {sizeDisplay}\n{freeSpaceDisplay} on {driveLetter} Drive remaining";
    }

    [RelayCommand]
    private async Task BrowseGameFolderAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Mirror's Edge Installation Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            GameInstallPath = dialog.FolderName;
            ValidateGamePath();

            if (!IsValidGamePath)
            {
                await DialogHelper.ShowMessageAsync(
                    "Invalid Directory",
                    "The selected directory doesn't appear to contain Mirror's Edge.\n\n" +
                    "Please select the game's base directory containing the 'Binaries' folder with 'MirrorsEdge.exe'.\n\n" +
                    "Typical paths:\n" +
                    "• Steam: C:\\Program Files (x86)\\Steam\\steamapps\\common\\mirrors edge\n" +
                    "• EA: C:\\Program Files\\EA Games\\Mirrors Edge\n" +
                    "• GOG: C:\\Program Files (x86)\\GOG Galaxy\\Games\\Mirror's Edge",
                    DialogHelper.MessageType.Warning);
                return;
            }

            SaveConfiguration();
            CheckDocumentsPath();
            UpdatePatchStatus();
            UpdateDependenciesStatus();
            await RefreshMapsAsync();
            CalculateMapsSize();
        }
    }

    [RelayCommand]
    private async Task RefreshMapsAsync()
    {
        IsLoading = true;
        LoadingStatus = "Fetching maps...";

        try
        {
            var mapsData = await _mapService.FetchMapsAsync();

            var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
            var inactivePath = _pathService.GetMemmInactiveFolderPath(GameInstallPath, MemmLocation);

            Application.Current.Dispatcher.Invoke(() =>
            {
                CustomMaps.Clear();
                CustomTimeTrials.Clear();
                StoryExperiences.Clear();

                if (mapsData.TryGetValue("Custom Maps", out var customMapsList))
                {
                    int index = 0;
                    foreach (var entry in customMapsList)
                    {
                        var mapName = entry.PackName ?? entry.FriendlyName ?? "Unknown";
                        var isInstalled = _mapService.IsMapInstalled(mapName, memmPath, inactivePath);

                        var mapViewModel = new MapViewModel
                        {
                            Name = mapName,
                            IsInstalled = isInstalled,
                            Category = "Custom Maps",
                            MapEntry = entry,
                            OriginalIndex = index++
                        };
                        mapViewModel.SelectionChanged += OnMapSelectionChanged;
                        CustomMaps.Add(mapViewModel);
                    }
                }

                if (mapsData.TryGetValue("Custom Time Trials", out var timeTrialsList))
                {
                    int index = 0;
                    foreach (var entry in timeTrialsList)
                    {
                        var mapName = entry.PackName ?? entry.FriendlyName ?? "Unknown";
                        var isInstalled = _mapService.IsMapInstalled(mapName, memmPath, inactivePath);

                        var mapViewModel = new MapViewModel
                        {
                            Name = mapName,
                            IsInstalled = isInstalled,
                            Category = "Custom Time Trials",
                            MapEntry = entry,
                            OriginalIndex = index++
                        };
                        mapViewModel.SelectionChanged += OnMapSelectionChanged;
                        CustomTimeTrials.Add(mapViewModel);
                    }
                }

                if (mapsData.TryGetValue("Story Experiences", out var storyList))
                {
                    int index = 0;
                    foreach (var entry in storyList)
                    {
                        var mapName = entry.FriendlyName ?? "Unknown";
                        var isInstalled = _mapService.IsMapInstalled(mapName, memmPath, inactivePath);

                        var mapViewModel = new MapViewModel
                        {
                            Name = mapName,
                            IsInstalled = isInstalled,
                            Category = "Story Experiences",
                            MapEntry = entry,
                            OriginalIndex = index++
                        };
                        mapViewModel.SelectionChanged += OnMapSelectionChanged;
                        StoryExperiences.Add(mapViewModel);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowMessageAsync("Error", $"Failed to fetch maps: {ex.Message}", DialogHelper.MessageType.Error);
        }
        finally
        {
            IsLoading = false;
            LoadingStatus = "";
        }
    }

    [RelayCommand]
    private async Task SelectMapAsync(MapViewModel? map)
    {
        if (map == null)
            return;

        SelectedMap = map;
        var entry = map.MapEntry;

        if (entry == null)
            return;

        IsMapDetailsLoading = true;
        ShowMapDetails = false;
        IsSpeedrunExpanded = false;
        HasSpeedrunStats = false;
        IsSpeedrunLoading = false;
        NoSpeedrunsFound = false;

        SelectedMapName = entry.DisplayName;
        SelectedMapAuthor = $"Author: {entry.Author ?? "N/A"}";
        SelectedMapDate = $"Date: {entry.Date ?? "N/A"}";
        SelectedMapDescription = entry.PackDescription ?? entry.Description ?? "No description available.";

        if (!string.IsNullOrEmpty(entry.ImageUrl))
        {
            try
            {
                var imageData = await _mapService.DownloadImageAsync(entry.ImageUrl);
                if (imageData != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(imageData);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    SelectedMapImage = bitmap;
                }
                else
                {
                    SelectedMapImage = null;
                }
            }
            catch
            {
                SelectedMapImage = null;
            }
        }
        else
        {
            SelectedMapImage = null;
        }

        IsMapDetailsLoading = false;
        ShowMapDetails = true;

        if (!string.IsNullOrEmpty(entry.SpeedrunId))
        {
            IsSpeedrunLoading = true;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    var stats = await _speedrunService.GetLevelStatsAsync(entry.SpeedrunId);
                    if (stats != null && stats.TopRuns.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SpeedrunTopRuns.Clear();
                            foreach (var run in stats.TopRuns)
                            {
                                var placeStr = run.Place switch
                                {
                                    1 => "1st",
                                    2 => "2nd",
                                    3 => "3rd",
                                    _ => $"{run.Place}th"
                                };
                                SpeedrunTopRuns.Add($"{placeStr} — {run.PlayerName}");
                                SpeedrunTopRuns.Add($"Time: {run.Time}");
                                SpeedrunTopRuns.Add($"Date: {run.Date}");
                                if (run.Place < 3)
                                {
                                    SpeedrunTopRuns.Add("");
                                }
                            }
                            SpeedrunLeaderboardUrl = stats.LeaderboardUrl;
                            HasSpeedrunStats = true;
                            IsSpeedrunLoading = false;
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            HasSpeedrunStats = false;
                            IsSpeedrunLoading = false;
                            NoSpeedrunsFound = true;
                        });
                    }
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HasSpeedrunStats = false;
                        IsSpeedrunLoading = false;
                        NoSpeedrunsFound = true;
                    });
                }
            });
        }
    }

    [RelayCommand]
    private void ToggleMapSelection(MapViewModel? map)
    {
        if (IsMemmDisabled)
            return;

        if (map == null)
            return;

        map.IsSelected = !map.IsSelected;
        UpdateSelectedMapsList();
    }

    [RelayCommand]
    private void SelectAllMaps(string category)
    {
        if (IsMemmDisabled)
            return;

        var collection = category switch
        {
            "Custom Maps" => CustomMaps,
            "Custom Time Trials" => CustomTimeTrials,
            "Story Experiences" => StoryExperiences,
            _ => null
        };

        if (collection == null)
            return;

        var allSelected = collection.All(m => m.IsSelected);
        foreach (var map in collection)
        {
            map.IsSelected = !allSelected;
        }

        UpdateSelectedMapsList();
    }

    [RelayCommand]
    private void SortMaps((string category, string sortType) parameters)
    {
        var collection = parameters.category switch
        {
            "Custom Maps" => CustomMaps,
            "Custom Time Trials" => CustomTimeTrials,
            "Story Experiences" => StoryExperiences,
            _ => null
        };

        if (collection == null)
            return;

        List<MapViewModel> sortedList;

        if (parameters.sortType == "Alpha")
        {
            sortedList = collection.OrderBy(m => m.Name).ToList();
        }
        else
        {
            sortedList = collection.OrderBy(m => m.OriginalIndex).ToList();
        }

        collection.Clear();
        foreach (var map in sortedList)
        {
            collection.Add(map);
        }
    }

    private void UpdateSelectedMapsList()
    {
        SelectedMapsList.Clear();
        long totalSize = 0;
        var processedPacks = new HashSet<string>();

        var allMaps = CustomMaps.Concat(CustomTimeTrials).Concat(StoryExperiences);

        foreach (var map in allMaps.Where(m => m.IsSelected))
        {
            var entry = map.MapEntry;
            if (entry == null)
                continue;

            if (entry.IsMapPack && entry.PackedMaps != null)
            {
                foreach (var packed in entry.PackedMaps)
                {
                    SelectedMapsList.Add(packed.FriendlyName ?? "Unknown");
                }
                if (!processedPacks.Contains(entry.PackName ?? ""))
                {
                    if (long.TryParse(entry.ZipSize, out var size))
                        totalSize += size;
                    processedPacks.Add(entry.PackName ?? "");
                }
            }
            else
            {
                SelectedMapsList.Add(entry.FriendlyName ?? "Unknown");
                if (long.TryParse(entry.ZipSize, out var size))
                    totalSize += size;
            }
        }

        var count = SelectedMapsList.Count;
        SelectedMapsCount = $"Selected maps: {count}";

        if (totalSize > 0)
        {
            SelectedMapsSize = $"Download size: {_pathService.FormatFileSize(totalSize)}";
        }
        else
        {
            SelectedMapsSize = "";
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedMapsAsync()
    {
        if (IsMemmDisabled)
            return;

        if (!IsValidGamePath)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please specify the correct game install folder path first.", DialogHelper.MessageType.Error);
            return;
        }

        if (!IsDocumentsPathValid)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please correct the issue with the Mirror's Edge Documents directory first.", DialogHelper.MessageType.Error);
            return;
        }

        if (!AreDependenciesInstalled)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please install the MEMM dependencies first.", DialogHelper.MessageType.Error);
            return;
        }

        var selectedMaps = CustomMaps.Concat(CustomTimeTrials).Concat(StoryExperiences).Where(m => m.IsSelected).ToList();
        if (selectedMaps.Count == 0)
        {
            await DialogHelper.ShowMessageAsync("Error", "No maps selected.", DialogHelper.MessageType.Error);
            return;
        }

        _downloadCts = new CancellationTokenSource();
        IsDownloading = true;

        var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
        var targetPath = Path.GetDirectoryName(memmPath) ?? memmPath;
        var downloadedUrls = new HashSet<string>();
        var allSuccess = true;

        try
        {
            var progress = new Progress<(int percentage, string status)>(p =>
            {
                DownloadProgress = p.percentage;
                DownloadStatus = p.status;
            });

            foreach (var map in selectedMaps)
            {
                if (_downloadCts.Token.IsCancellationRequested)
                    break;

                var entry = map.MapEntry;
                if (entry?.Url == null || downloadedUrls.Contains(entry.Url))
                    continue;

                var result = await _mapService.DownloadMapAsync(
                    entry.Url,
                    entry.DisplayName,
                    targetPath,
                    progress,
                    _downloadCts.Token);

                if (result.success)
                {
                    downloadedUrls.Add(entry.Url);

                    await RegisterMapAsync(entry, map.Category);
                }
                else
                {
                    allSuccess = false;
                    if (!result.message.Contains("cancelled"))
                    {
                        await DialogHelper.ShowMessageAsync("Download Error", result.message, DialogHelper.MessageType.Error);
                    }
                }
            }

            if (allSuccess && !_downloadCts.Token.IsCancellationRequested)
            {
                await DialogHelper.ShowMessageAsync("Success", "All maps downloaded successfully!", DialogHelper.MessageType.Success);
            }
            else if (_downloadCts.Token.IsCancellationRequested)
            {
                await DialogHelper.ShowMessageAsync("Cancelled", "Download cancelled.", DialogHelper.MessageType.Information);
            }
        }
        finally
        {
            IsDownloading = false;
            DownloadStatus = "";
            DownloadProgress = 0;
            _downloadCts?.Dispose();
            _downloadCts = null;

            await RefreshMapsAsync();
            CalculateMapsSize();
            UpdateSelectedMapsList();
        }
    }

    private async Task RegisterMapAsync(MapEntry entry, string category)
    {
        await Task.Run(() =>
        {
            try
            {
                var customMapsIniPath = _pathService.GetDefaultCustomMapsIniPath(GameInstallPath);

                if (category == "Custom Maps")
                {
                    if (entry.IsMapPack && entry.PackedMaps != null)
                    {
                        foreach (var packed in entry.PackedMaps)
                        {
                            RegisterLrStretch(customMapsIniPath, packed.FriendlyName ?? "", entry);
                        }
                    }
                    else
                    {
                        RegisterLrStretch(customMapsIniPath, entry.FriendlyName ?? "", entry);
                    }
                }
                else if (category == "Custom Time Trials")
                {
                    if (entry.IsMapPack && entry.PackedMaps != null)
                    {
                        if (entry.PackName == "Pure Time Trials (DLC maps)")
                        {
                            // DLC maps go to DefaultGame.ini
                            var gameIniPath = _pathService.GetDefaultGameIniPath(GameInstallPath);
                            foreach (var packed in entry.PackedMaps)
                            {
                                RegisterTtStretchToGameIni(gameIniPath, packed);
                            }
                        }
                        else
                        {
                            foreach (var packed in entry.PackedMaps)
                            {
                                RegisterTtStretch(customMapsIniPath, packed.FriendlyName ?? "", packed);
                            }
                        }
                    }
                    else
                    {
                        RegisterTtStretch(customMapsIniPath, entry.FriendlyName ?? "", entry);
                    }
                }
                else if (category == "Story Experiences")
                {
                    var storyIniPath = _pathService.GetDefaultStoryExperiencesIniPath(GameInstallPath);
                    var tdGameUiPath = _pathService.GetTdGameUiPath(GameInstallPath);

                    if (!string.IsNullOrEmpty(entry.ConfigUrl))
                    {
                        var configContent = _mapService.FetchConfigContentAsync(entry.ConfigUrl).Result;
                        if (!string.IsNullOrEmpty(configContent))
                        {
                            _iniService.AppendContentIfNotExists(storyIniPath, configContent);
                        }
                    }

                    if (!string.IsNullOrEmpty(entry.TdGameUiScene))
                    {
                        _iniService.InsertLineAfterMarker(
                            tdGameUiPath,
                            "CustomSpeedRunDescText=Race against the clock through a full chapter from Story Mode",
                            entry.TdGameUiScene);
                    }
                }
            }
            catch
            {
            }
        });
    }

    private void RegisterLrStretch(string iniPath, string friendlyName, MapEntry entry)
    {
        if (_iniService.SectionContainsFriendlyName(iniPath, friendlyName))
            return;

        var nextNum = _iniService.GetNextStretchNumber(iniPath, "LR_STRETCH");
        var sectionName = $"LR_STRETCH{nextNum} UIDataProvider_TdCustomLevelRaceStretch";

        var values = new Dictionary<string, string>
        {
            ["FriendlyName"] = friendlyName
        };

        if (!string.IsNullOrEmpty(entry.Description))
            values["Description"] = entry.Description;
        if (!string.IsNullOrEmpty(entry.MapFileName))
            values["MapFileName"] = entry.MapFileName;

        if (!string.IsNullOrEmpty(entry.NumberOfCheckpoints))
            values["NumberOfCheckpoints"] = entry.NumberOfCheckpoints;

        if (!string.IsNullOrEmpty(entry.CheckpointAFriendlyName))
            values["CheckpointAFriendlyName"] = entry.CheckpointAFriendlyName;
        if (!string.IsNullOrEmpty(entry.CheckpointAFileName))
            values["CheckpointAFileName"] = entry.CheckpointAFileName;
        if (!string.IsNullOrEmpty(entry.CheckpointADescription))
            values["CheckpointADescription"] = entry.CheckpointADescription;

        if (!string.IsNullOrEmpty(entry.CheckpointBFriendlyName))
            values["CheckpointBFriendlyName"] = entry.CheckpointBFriendlyName;
        if (!string.IsNullOrEmpty(entry.CheckpointBFileName))
            values["CheckpointBFileName"] = entry.CheckpointBFileName;
        if (!string.IsNullOrEmpty(entry.CheckpointBDescription))
            values["CheckpointBDescription"] = entry.CheckpointBDescription;

        if (!string.IsNullOrEmpty(entry.CheckpointCFriendlyName))
            values["CheckpointCFriendlyName"] = entry.CheckpointCFriendlyName;
        if (!string.IsNullOrEmpty(entry.CheckpointCFileName))
            values["CheckpointCFileName"] = entry.CheckpointCFileName;
        if (!string.IsNullOrEmpty(entry.CheckpointCDescription))
            values["CheckpointCDescription"] = entry.CheckpointCDescription;

        if (!string.IsNullOrEmpty(entry.CheckpointDFriendlyName))
            values["CheckpointDFriendlyName"] = entry.CheckpointDFriendlyName;
        if (!string.IsNullOrEmpty(entry.CheckpointDFileName))
            values["CheckpointDFileName"] = entry.CheckpointDFileName;
        if (!string.IsNullOrEmpty(entry.CheckpointDDescription))
            values["CheckpointDDescription"] = entry.CheckpointDDescription;

        if (!string.IsNullOrEmpty(entry.CheckpointEFriendlyName))
            values["CheckpointEFriendlyName"] = entry.CheckpointEFriendlyName;
        if (!string.IsNullOrEmpty(entry.CheckpointEFileName))
            values["CheckpointEFileName"] = entry.CheckpointEFileName;
        if (!string.IsNullOrEmpty(entry.CheckpointEDescription))
            values["CheckpointEDescription"] = entry.CheckpointEDescription;

        if (!string.IsNullOrEmpty(entry.CheckpointFFriendlyName))
            values["CheckpointFFriendlyName"] = entry.CheckpointFFriendlyName;
        if (!string.IsNullOrEmpty(entry.CheckpointFFileName))
            values["CheckpointFFileName"] = entry.CheckpointFFileName;
        if (!string.IsNullOrEmpty(entry.CheckpointFDescription))
            values["CheckpointFDescription"] = entry.CheckpointFDescription;

        if (!string.IsNullOrEmpty(entry.CheckpointGFriendlyName))
            values["CheckpointGFriendlyName"] = entry.CheckpointGFriendlyName;
        if (!string.IsNullOrEmpty(entry.CheckpointGFileName))
            values["CheckpointGFileName"] = entry.CheckpointGFileName;
        if (!string.IsNullOrEmpty(entry.CheckpointGDescription))
            values["CheckpointGDescription"] = entry.CheckpointGDescription;

        _iniService.AppendToIniFile(iniPath, sectionName, values);
        _iniService.ReorganiseSections(iniPath);
    }

    private void RegisterTtStretch(string iniPath, string friendlyName, object entryData)
    {
        if (_iniService.SectionContainsFriendlyName(iniPath, friendlyName))
            return;

        var nextNum = _iniService.GetNextStretchNumber(iniPath, "TT_STRETCH");
        var sectionName = $"TT_STRETCH{nextNum} UIDataProvider_TdCustomTimeTrialStretch";

        var values = new Dictionary<string, string>
        {
            ["FriendlyName"] = friendlyName
        };

        if (entryData is MapEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.Description))
                values["Description"] = entry.Description;
            if (!string.IsNullOrEmpty(entry.MapFileName))
                values["MapFileName"] = entry.MapFileName;
            if (!string.IsNullOrEmpty(entry.StretchNameId))
                values["StretchNameId"] = entry.StretchNameId;
            if (!string.IsNullOrEmpty(entry.QualifyingTime))
                values["QualifyingTime"] = entry.QualifyingTime;
            if (!string.IsNullOrEmpty(entry.Rating1Time))
                values["Rating1Time"] = entry.Rating1Time;
            if (!string.IsNullOrEmpty(entry.Rating2Time))
                values["Rating2Time"] = entry.Rating2Time;
            if (!string.IsNullOrEmpty(entry.Rating3Time))
                values["Rating3Time"] = entry.Rating3Time;
        }
        else if (entryData is PackedMapEntry packed)
        {
            if (!string.IsNullOrEmpty(packed.Description))
                values["Description"] = packed.Description;
            if (!string.IsNullOrEmpty(packed.MapFileName))
                values["MapFileName"] = packed.MapFileName;
            if (!string.IsNullOrEmpty(packed.StretchNameId))
                values["StretchNameId"] = packed.StretchNameId;
            if (!string.IsNullOrEmpty(packed.QualifyingTime))
                values["QualifyingTime"] = packed.QualifyingTime;
            if (!string.IsNullOrEmpty(packed.Rating1Time))
                values["Rating1Time"] = packed.Rating1Time;
            if (!string.IsNullOrEmpty(packed.Rating2Time))
                values["Rating2Time"] = packed.Rating2Time;
            if (!string.IsNullOrEmpty(packed.Rating3Time))
                values["Rating3Time"] = packed.Rating3Time;
        }

        _iniService.AppendToIniFile(iniPath, sectionName, values);
        _iniService.ReorganiseSections(iniPath);
    }

    private void RegisterTtStretchToGameIni(string iniPath, PackedMapEntry packed)
    {
        // for DLC Pure Time Trials that go to DefaultGame.ini
        var nextNum = _iniService.GetNextStretchNumber(iniPath, "TT_STRETCH");
        var sectionName = $"TT_STRETCH{nextNum} UIDataProvider_TdTimeTrialStretch";

        var values = new Dictionary<string, string>
        {
            ["FriendlyName"] = packed.FriendlyName ?? ""
        };

        if (!string.IsNullOrEmpty(packed.Description))
            values["Description"] = packed.Description;
        if (!string.IsNullOrEmpty(packed.MapFileName))
            values["MapFileName"] = packed.MapFileName;
        if (!string.IsNullOrEmpty(packed.StretchNameId))
            values["StretchNameId"] = packed.StretchNameId;
        if (!string.IsNullOrEmpty(packed.QualifyingTime))
            values["QualifyingTime"] = packed.QualifyingTime;
        if (!string.IsNullOrEmpty(packed.Rating1Time))
            values["Rating1Time"] = packed.Rating1Time;
        if (!string.IsNullOrEmpty(packed.Rating2Time))
            values["Rating2Time"] = packed.Rating2Time;
        if (!string.IsNullOrEmpty(packed.Rating3Time))
            values["Rating3Time"] = packed.Rating3Time;

        _iniService.AppendToIniFile(iniPath, sectionName, values);
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCts?.Cancel();
    }

    [RelayCommand]
    private async Task UninstallSelectedMapsAsync()
    {
        if (IsMemmDisabled)
            return;

        var selectedMaps = CustomMaps.Concat(CustomTimeTrials).Concat(StoryExperiences).Where(m => m.IsSelected).ToList();
        if (selectedMaps.Count == 0)
        {
            await DialogHelper.ShowMessageAsync("Error", "No maps selected.", DialogHelper.MessageType.Error);
            return;
        }

        var result = await DialogHelper.ShowConfirmationAsync(
            "Confirm Uninstall",
            "Are you sure you want to uninstall the selected maps?");

        if (!result)
            return;

        var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
        var customMapsIniPath = _pathService.GetDefaultCustomMapsIniPath(GameInstallPath);
        var gameIniPath = _pathService.GetDefaultGameIniPath(GameInstallPath);
        var storyIniPath = _pathService.GetDefaultStoryExperiencesIniPath(GameInstallPath);
        var tdGameUiPath = _pathService.GetTdGameUiPath(GameInstallPath);
        var allSuccess = true;

        foreach (var map in selectedMaps)
        {
            var mapName = map.MapEntry?.PackName ?? map.MapEntry?.FriendlyName ?? map.Name;
            var uninstallResult = _mapService.UninstallMap(mapName, memmPath);

            if (!uninstallResult.success)
            {
                allSuccess = false;
            }

            if (map.MapEntry != null)
            {
                // handle DLC maps in DefaultGame.ini
                if (map.Category == "Custom Time Trials" && 
                    map.MapEntry.IsMapPack && 
                    map.MapEntry.PackName == "Pure Time Trials (DLC maps)" &&
                    map.MapEntry.PackedMaps != null)
                {
                    foreach (var packed in map.MapEntry.PackedMaps)
                    {
                        _iniService.RemoveSectionByFriendlyName(gameIniPath, packed.FriendlyName ?? "");
                    }
                }
                else if (map.Category == "Story Experiences")
                {
                    if (map.MapEntry.IsMapPack && map.MapEntry.PackedMaps != null)
                    {
                        foreach (var packed in map.MapEntry.PackedMaps)
                        {
                            _iniService.RemoveStoryExperienceByFriendlyName(storyIniPath, packed.FriendlyName ?? "");
                        }
                    }
                    else
                    {
                        _iniService.RemoveStoryExperienceByFriendlyName(storyIniPath, map.MapEntry.FriendlyName ?? "");
                    }

                    if (!string.IsNullOrEmpty(map.MapEntry.TdGameUiScene))
                    {
                        _iniService.RemoveLine(tdGameUiPath, map.MapEntry.TdGameUiScene);
                    }
                }
                else
                {
                    if (map.MapEntry.IsMapPack && map.MapEntry.PackedMaps != null)
                    {
                        foreach (var packed in map.MapEntry.PackedMaps)
                        {
                            _iniService.RemoveSectionByFriendlyName(customMapsIniPath, packed.FriendlyName ?? "");
                        }
                    }
                    else
                    {
                        _iniService.RemoveSectionByFriendlyName(customMapsIniPath, map.MapEntry.FriendlyName ?? "");
                    }
                }
            }
        }

        _iniService.ReorganiseSections(customMapsIniPath);

        if (allSuccess)
        {
            await DialogHelper.ShowMessageAsync("Success", "Selected maps uninstalled successfully.", DialogHelper.MessageType.Success);
        }
        else
        {
            await DialogHelper.ShowMessageAsync("Warning", "Some maps failed to uninstall.", DialogHelper.MessageType.Warning);
        }

        await RefreshMapsAsync();
        CalculateMapsSize();
        UpdateSelectedMapsList();
    }

    [RelayCommand]
    private async Task ToggleConfigPatchAsync()
    {
        if (!IsValidGamePath)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please specify the correct game install folder path first.", DialogHelper.MessageType.Error);
            return;
        }

        IsLoading = true;
        var progress = new Progress<(int percentage, string status)>(p =>
        {
            LoadingProgress = p.percentage;
            LoadingStatus = p.status;
        });

        var result = await _gameService.ToggleConfigPatchAsync(GameInstallPath, progress);

        IsLoading = false;
        LoadingStatus = "";

        if (result.success)
        {
            await DialogHelper.ShowMessageAsync("Success", result.message, DialogHelper.MessageType.Success);
        }
        else
        {
            await DialogHelper.ShowMessageAsync("Error", result.message, DialogHelper.MessageType.Error);
        }

        UpdatePatchStatus();
    }

    [RelayCommand]
    private async Task OpenDependenciesWindow()
    {
        if (IsMemmDisabled)
            return;

        if (!IsValidGamePath)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please specify the correct game install folder path first.", DialogHelper.MessageType.Error);
            return;
        }

        if (!IsDocumentsPathValid)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please correct the issue with the Mirror's Edge Documents directory first.", DialogHelper.MessageType.Error);
            return;
        }

        await DialogHelper.ShowDependenciesDialogAsync(this);
        UpdateDependenciesStatus();
    }

    public async Task InstallDependencyAsync(string dependencyType, IProgress<(int percentage, string status)> progress)
    {
        if (IsMemmDisabled)
            return;

        (bool success, string message) result;

        if (dependencyType == "ConfigFiles")
        {
            result = _gameService.InstallConfigFiles(GameInstallPath, progress);
        }
        else
        {
            var targetPath = dependencyType switch
            {
                "CustomMapMenuMod" => _pathService.GetPublishedPath(),
                "CustomMapMenuModTweaksUI" => _pathService.GetPublishedPath(),
                "CommonAssets" => _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation),
                "ShaderCache" => _pathService.GetPublishedPath(),
                _ => throw new ArgumentException("Unknown dependency type")
            };

            result = await _gameService.InstallDependencyAsync(dependencyType, targetPath, progress, default, GameInstallPath);
        }

        if (!result.success)
        {
            await DialogHelper.ShowMessageAsync("Error", result.message, DialogHelper.MessageType.Error);
        }
        else
        {
            await DialogHelper.ShowMessageAsync("Success", result.message, DialogHelper.MessageType.Success);
        }

        UpdateDependenciesStatus();
        CalculateMapsSize();
    }

    public DependencyStatus GetDependencyStatus()
    {
        var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
        var cookedPcPath = _pathService.GetCookedPcPath();
        return _gameService.GetDependencyStatus(GameInstallPath, memmPath, cookedPcPath);
    }

    [RelayCommand]
    private async Task UninstallMemmAsync()
    {
        if (IsLoading || IsDownloading)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please wait for current operations to finish before uninstalling MEMM.", DialogHelper.MessageType.Error);
            return;
        }

        if (!IsValidGamePath)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please specify the correct game install folder path first.", DialogHelper.MessageType.Error);
            return;
        }

        if (!IsDocumentsPathValid)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please correct the issue with the Mirror's Edge Documents directory first.", DialogHelper.MessageType.Error);
            return;
        }

        var result = await DialogHelper.ShowConfirmationAsync(
            "Uninstall MEMM?",
            "This will permanently remove all MEMM-related files and folders, including downloaded maps and dependency files.\n\n" +
            "Do you wish to continue?");

        if (!result)
            return;

        IsLoading = true;
        LoadingStatus = "Uninstalling MEMM...";

        var operationResult = await Task.Run(UninstallMemmInternal);

        IsLoading = false;
        LoadingStatus = "";

        if (!operationResult.success)
        {
            await DialogHelper.ShowMessageAsync("Error", $"Error uninstalling MEMM: {operationResult.message}", DialogHelper.MessageType.Error);
            return;
        }

        MemmLocation = "Published";
        SaveConfiguration();

        CheckMemmDisabledState();
        UpdateDependenciesStatus();
        await RefreshMapsAsync();
        CalculateMapsSize();
        UpdateSelectedMapsList();

        await DialogHelper.ShowMessageAsync("Success", "MEMM has been uninstalled and stock configs were restored.", DialogHelper.MessageType.Success);
    }

    [RelayCommand]
    private async Task ToggleMemmAsync(bool? requestedDisabledState)
    {
        if (!IsValidGamePath)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please specify the correct game install folder path first.", DialogHelper.MessageType.Error);
            CheckMemmDisabledState();
            return;
        }

        if (!IsDocumentsPathValid)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please correct the issue with the Mirror's Edge Documents directory first.", DialogHelper.MessageType.Error);
            CheckMemmDisabledState();
            return;
        }

        CheckMemmDisabledState();
        var currentlyDisabled = IsMemmDisabled;
        var targetDisabledState = requestedDisabledState ?? !currentlyDisabled;

        if (targetDisabledState == currentlyDisabled)
        {
            IsMemmDisabled = currentlyDisabled;
            return;
        }

        var success = targetDisabledState
            ? await DisableMemmAsync()
            : await EnableMemmAsync();

        if (!success)
        {
            CheckMemmDisabledState();
        }
    }

    private async Task<bool> DisableMemmAsync()
    {
        var result = await DialogHelper.ShowConfirmationAsync(
            "Disable MEMM?",
            "When disabled, the Custom Map Menu UI and any downloaded maps will no longer appear in-game.\n\n" +
            "This can be useful if you want to temporarily play Mirror's Edge without MEMM being active, and can improve load times if many maps have been downloaded.\n\n" +
            "Do you wish to disable MEMM?");

        if (!result)
        {
            CheckMemmDisabledState();
            return false;
        }

        IsLoading = true;
        LoadingStatus = "Disabling MEMM...";

        var operationResult = await Task.Run(DisableMemmInternal);
        IsLoading = false;
        LoadingStatus = "";

        if (!operationResult.success)
        {
            CheckMemmDisabledState();
            await DialogHelper.ShowMessageAsync("Error", $"Error disabling MEMM: {operationResult.message}", DialogHelper.MessageType.Error);
            return false;
        }

        CheckMemmDisabledState();
        await RefreshMapsAsync();
        CalculateMapsSize();
        UpdateDependenciesStatus();

        await DialogHelper.ShowMessageAsync("Success", "MEMM has been disabled.", DialogHelper.MessageType.Success);
        return true;
    }

    private async Task<bool> EnableMemmAsync()
    {
        IsLoading = true;
        LoadingStatus = "Enabling MEMM...";

        var operationResult = await Task.Run(EnableMemmInternal);
        IsLoading = false;
        LoadingStatus = "";

        if (!operationResult.success)
        {
            CheckMemmDisabledState();
            await DialogHelper.ShowMessageAsync("Error", $"Error enabling MEMM: {operationResult.message}", DialogHelper.MessageType.Error);
            return false;
        }

        CheckMemmDisabledState();
        await RefreshMapsAsync();
        CalculateMapsSize();
        UpdateDependenciesStatus();

        await DialogHelper.ShowMessageAsync("Success", "MEMM has been enabled.", DialogHelper.MessageType.Success);
        return true;
    }

    private (bool success, string message) UninstallMemmInternal()
    {
        try
        {
            var publishedMemmPath = _pathService.GetMemmFolderPath(GameInstallPath, "Published");
            var gameMemmPath = _pathService.GetMemmFolderPath(GameInstallPath, "Game");
            var publishedInactivePath = GetPublishedMemmInactiveFolderPath();
            var gameInactivePath = _pathService.GetMemmInactiveFolderPath(GameInstallPath, "Game");
            var publishedCookedPcPath = _pathService.GetCookedPcPath();

            var defaultEnginePath = _pathService.GetDefaultEngineIniPath(GameInstallPath);
            var defaultGamePath = _pathService.GetDefaultGameIniPath(GameInstallPath);
            var defaultCustomMapsPath = _pathService.GetDefaultCustomMapsIniPath(GameInstallPath);
            var defaultStoryExperiencesPath = _pathService.GetDefaultStoryExperiencesIniPath(GameInstallPath);
            var defaultGameStockBackupPath = _pathService.GetDefaultGameStockBackupPath(GameInstallPath);
            var defaultGameModdedBackupPath = _pathService.GetDefaultGameModdedBackupPath(GameInstallPath);

            RemoveReadOnlyAttributesRecursive(publishedMemmPath);
            RemoveReadOnlyAttributesRecursive(gameMemmPath);
            RemoveReadOnlyAttributesRecursive(publishedInactivePath);
            RemoveReadOnlyAttributesRecursive(gameInactivePath);
            RemoveReadOnlyAttributesRecursive(Path.Combine(publishedCookedPcPath, "Maps"));
            RemoveReadOnlyAttributesRecursive(Path.Combine(publishedCookedPcPath, "UI"));
            RemoveReadOnlyAttributesRecursive(Path.Combine(publishedInactivePath, "Maps"));
            RemoveReadOnlyAttributesRecursive(Path.Combine(publishedInactivePath, "UI"));
            RemoveReadOnlyAttributesRecursive(Path.Combine(gameInactivePath, "Maps"));
            RemoveReadOnlyAttributesRecursive(Path.Combine(gameInactivePath, "UI"));

            EnsureFileWritable(defaultEnginePath);
            EnsureFileWritable(defaultGamePath);
            EnsureFileWritable(defaultCustomMapsPath);
            EnsureFileWritable(defaultStoryExperiencesPath);
            EnsureFileWritable(defaultGameStockBackupPath);
            EnsureFileWritable(defaultGameModdedBackupPath);
            EnsureFileWritable(Path.Combine(publishedCookedPcPath, "CustomStretches.u"));
            EnsureFileWritable(Path.Combine(publishedCookedPcPath, "Fp.u"));
            EnsureFileWritable(Path.Combine(publishedCookedPcPath, "LocalShaderCache-PC-D3D-SM3.upk"));
            EnsureFileWritable(Path.Combine(publishedInactivePath, "CustomStretches.u"));
            EnsureFileWritable(Path.Combine(publishedInactivePath, "Fp.u"));
            EnsureFileWritable(Path.Combine(gameInactivePath, "CustomStretches.u"));
            EnsureFileWritable(Path.Combine(gameInactivePath, "Fp.u"));

            if (!TrySwapDefaultGameToStock(GameInstallPath, out var defaultGameError))
            {
                if (!File.Exists(defaultGamePath))
                {
                    return (false, $"Failed to restore DefaultGame.ini: {defaultGameError}");
                }

                try
                {
                    EnsureFileWritable(defaultGamePath);
                    RestoreDefaultGameForVanillaMenu(defaultGamePath);
                }
                catch (Exception ex)
                {
                    return (false, $"Failed to restore DefaultGame.ini: {defaultGameError}. Fallback restore failed: {ex.Message}");
                }
            }

            if (!TrySwapTdEngineToStock(out var tdEngineError))
                return (false, tdEngineError);

            RestoreDefaultEngineForVanillaMenu(defaultEnginePath);

            if (!TryRestoreTdGameUiToStock(out var tdGameUiError))
                return (false, tdGameUiError);

            DeletePathIfExists(Path.Combine(publishedCookedPcPath, "Maps"));
            DeletePathIfExists(Path.Combine(publishedCookedPcPath, "UI"));
            DeletePathIfExists(Path.Combine(publishedCookedPcPath, "CustomStretches.u"));
            DeletePathIfExists(Path.Combine(publishedCookedPcPath, "Fp.u"));
            DeletePathIfExists(Path.Combine(publishedCookedPcPath, "LocalShaderCache-PC-D3D-SM3.upk"));

            DeletePathIfExists(Path.Combine(publishedInactivePath, "Maps"));
            DeletePathIfExists(Path.Combine(publishedInactivePath, "UI"));
            DeletePathIfExists(Path.Combine(publishedInactivePath, "CustomStretches.u"));
            DeletePathIfExists(Path.Combine(publishedInactivePath, "Fp.u"));

            DeletePathIfExists(Path.Combine(gameInactivePath, "Maps"));
            DeletePathIfExists(Path.Combine(gameInactivePath, "UI"));
            DeletePathIfExists(Path.Combine(gameInactivePath, "CustomStretches.u"));
            DeletePathIfExists(Path.Combine(gameInactivePath, "Fp.u"));

            DeletePathIfExists(publishedMemmPath);
            DeletePathIfExists(gameMemmPath);
            DeletePathIfExists(publishedInactivePath);
            DeletePathIfExists(gameInactivePath);

            DeletePathIfExists(defaultCustomMapsPath);
            DeletePathIfExists(defaultStoryExperiencesPath);
            DeletePathIfExists(defaultGameModdedBackupPath);
            DeletePathIfExists(defaultGameStockBackupPath);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private (bool success, string message) DisableMemmInternal()
    {
        try
        {
            var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
            var inactivePath = _pathService.GetMemmInactiveFolderPath(GameInstallPath, MemmLocation);
            var publishedCookedPcPath = _pathService.GetCookedPcPath();
            var publishedInactivePath = GetPublishedMemmInactiveFolderPath();
            var gameConfigPath = _pathService.GetConfigFolderPath(GameInstallPath);

            _pathService.EnsureDirectoryExists(inactivePath);
            _pathService.EnsureDirectoryExists(publishedInactivePath);

            RemoveReadOnlyAttributesRecursive(memmPath);
            RemoveReadOnlyAttributesRecursive(inactivePath);
            RemoveReadOnlyAttributesRecursive(publishedCookedPcPath);
            RemoveReadOnlyAttributesRecursive(publishedInactivePath);
            RemoveReadOnlyAttributesRecursive(gameConfigPath);

            if (!TrySwapDefaultGameToStock(GameInstallPath, out var swapError))
                return (false, swapError);

            if (!TrySwapTdEngineToStock(out swapError))
                return (false, swapError);

            MoveDirectoryContents(memmPath, inactivePath);

            MovePathToDirectory(Path.Combine(publishedCookedPcPath, "Maps"), publishedInactivePath);
            MovePathToDirectory(Path.Combine(publishedCookedPcPath, "UI"), publishedInactivePath);
            MovePathToDirectory(Path.Combine(publishedCookedPcPath, "CustomStretches.u"), publishedInactivePath);
            MovePathToDirectory(Path.Combine(publishedCookedPcPath, "Fp.u"), publishedInactivePath);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private (bool success, string message) EnableMemmInternal()
    {
        try
        {
            var memmPath = _pathService.GetMemmFolderPath(GameInstallPath, MemmLocation);
            var inactivePath = _pathService.GetMemmInactiveFolderPath(GameInstallPath, MemmLocation);
            var publishedCookedPcPath = _pathService.GetCookedPcPath();
            var publishedInactivePath = GetPublishedMemmInactiveFolderPath();
            var gameConfigPath = _pathService.GetConfigFolderPath(GameInstallPath);

            _pathService.EnsureDirectoryExists(memmPath);
            _pathService.EnsureDirectoryExists(publishedCookedPcPath);

            RemoveReadOnlyAttributesRecursive(memmPath);
            RemoveReadOnlyAttributesRecursive(inactivePath);
            RemoveReadOnlyAttributesRecursive(publishedCookedPcPath);
            RemoveReadOnlyAttributesRecursive(publishedInactivePath);
            RemoveReadOnlyAttributesRecursive(gameConfigPath);

            if (!TrySwapDefaultGameToModded(GameInstallPath, out var swapError))
                return (false, swapError);

            if (!TrySwapTdEngineToModded(out swapError))
                return (false, swapError);

            MovePathToDirectory(Path.Combine(publishedInactivePath, "Maps"), publishedCookedPcPath);
            MovePathToDirectory(Path.Combine(publishedInactivePath, "UI"), publishedCookedPcPath);
            MovePathToDirectory(Path.Combine(publishedInactivePath, "CustomStretches.u"), publishedCookedPcPath);
            MovePathToDirectory(Path.Combine(publishedInactivePath, "Fp.u"), publishedCookedPcPath);

            MoveDirectoryContents(inactivePath, memmPath);
            TryDeleteDirectoryIfEmpty(inactivePath);

            if (!string.Equals(inactivePath, publishedInactivePath, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteDirectoryIfEmpty(publishedInactivePath);
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private bool TrySwapDefaultGameToStock(string gameInstallPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        var defaultGamePath = _pathService.GetDefaultGameIniPath(gameInstallPath);
        var stockBackupPath = _pathService.GetDefaultGameStockBackupPath(gameInstallPath);
        var moddedBackupPath = _pathService.GetDefaultGameModdedBackupPath(gameInstallPath);

        EnsureStockDefaultGameBackupIfPossible(defaultGamePath, stockBackupPath);

        if (File.Exists(defaultGamePath))
        {
            EnsureFileWritable(defaultGamePath);
            if (LooksLikeMemmModifiedDefaultGame(defaultGamePath))
            {
                File.Copy(defaultGamePath, moddedBackupPath, true);
            }
        }

        if (File.Exists(stockBackupPath))
        {
            EnsureFileWritable(defaultGamePath);
            File.Copy(stockBackupPath, defaultGamePath, true);
            return true;
        }

        if (File.Exists(moddedBackupPath))
        {
            EnsureFileWritable(defaultGamePath);
            File.Copy(moddedBackupPath, defaultGamePath, true);
            RestoreDefaultGameForVanillaMenu(defaultGamePath);
            File.Copy(defaultGamePath, stockBackupPath, true);
            return true;
        }

        errorMessage = "DefaultGame.ini backup not found. Reinstall config files to recreate backup.";
        return false;
    }

    private bool TrySwapDefaultGameToModded(string gameInstallPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        var defaultGamePath = _pathService.GetDefaultGameIniPath(gameInstallPath);
        var stockBackupPath = _pathService.GetDefaultGameStockBackupPath(gameInstallPath);
        var moddedBackupPath = _pathService.GetDefaultGameModdedBackupPath(gameInstallPath);

        if (!File.Exists(stockBackupPath) && File.Exists(defaultGamePath))
        {
            EnsureStockDefaultGameBackupIfPossible(defaultGamePath, stockBackupPath);
        }

        if (File.Exists(moddedBackupPath) && LooksLikeMemmModifiedDefaultGame(moddedBackupPath))
        {
            EnsureFileWritable(defaultGamePath);
            File.Copy(moddedBackupPath, defaultGamePath, true);
            return true;
        }

        var result = _gameService.InstallConfigFiles(gameInstallPath);
        if (!result.success)
        {
            errorMessage = result.message;
            return false;
        }

        if (File.Exists(defaultGamePath) && LooksLikeMemmModifiedDefaultGame(defaultGamePath))
        {
            File.Copy(defaultGamePath, moddedBackupPath, true);
            return true;
        }

        errorMessage = "Failed to rebuild a valid MEMM-modified DefaultGame.ini. Please reinstall config files and dependencies.";
        return false;
    }

    private bool TrySwapTdEngineToStock(out string errorMessage)
    {
        errorMessage = string.Empty;
        var tdEnginePath = _pathService.GetDocumentsTdEngineIniPath();

        try
        {
            if (!File.Exists(tdEnginePath))
            {
                return true;
            }

            EnsureFileWritable(tdEnginePath);

            RestoreTdEngineForVanillaMenu(tdEnginePath);

            if (File.Exists(tdEnginePath))
            {
                var updatedAttributes = File.GetAttributes(tdEnginePath);
                File.SetAttributes(tdEnginePath, updatedAttributes | FileAttributes.ReadOnly);
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to update TdEngine.ini for stock menu: {ex.Message}";
            return false;
        }
    }

    private bool TrySwapTdEngineToModded(out string errorMessage)
    {
        errorMessage = string.Empty;
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
                return true;
            }

            EnsureFileWritable(tdEnginePath);

            RestoreTdEngineForMemm(tdEnginePath);

            if (File.Exists(tdEnginePath))
            {
                var updatedAttributes = File.GetAttributes(tdEnginePath);
                File.SetAttributes(tdEnginePath, updatedAttributes | FileAttributes.ReadOnly);
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to update TdEngine.ini for MEMM: {ex.Message}";
            return false;
        }
    }

    private bool TryRestoreTdGameUiToStock(out string errorMessage)
    {
        errorMessage = string.Empty;

        var tdGameUiPath = _pathService.GetTdGameUiPath(GameInstallPath);
        if (!File.Exists(tdGameUiPath))
            return true;

        try
        {
            EnsureFileWritable(tdGameUiPath);
            if (!ConfigTemplateProvider.WriteTemplateToFile("TdGameUI.int", tdGameUiPath, out var writeError))
            {
                errorMessage = $"Failed to restore TdGameUI.int: {writeError}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to restore TdGameUI.int: {ex.Message}";
            return false;
        }
    }

    private static void RestoreDefaultGameForVanillaMenu(string defaultGamePath)
    {
        Helpers.IniFileHelper.RemoveSectionFromIniFile(defaultGamePath, "Fp.UIDataStore_TdCustomMapsGameData");
        Helpers.IniFileHelper.RemoveSectionFromIniFile(defaultGamePath, "Fp.UIDataStore_TdCustomMapsTimeTrialData");
        Helpers.IniFileHelper.RemoveSectionFromIniFile(defaultGamePath, "Fp.UIDataStore_TdCustomMapsRaceData");
        Helpers.IniFileHelper.RemoveSectionFromIniFile(defaultGamePath, "TdGame.UIDataStore_TdCustomTabData");

        var tempFilePath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFilePath, DefaultGameVanillaDataStoreSeed);
            Helpers.IniFileHelper.MergeIniFiles(tempFilePath, defaultGamePath);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private static void RestoreDefaultEngineForVanillaMenu(string defaultEnginePath)
    {
        if (!File.Exists(defaultEnginePath))
            return;

        var entriesToRemove = new (string section, string value)[]
        {
            ("Editor.EditorEngine", "+EditPackages=CustomStretches"),
            ("Editor.EditorEngine", "EditPackages=CustomStretches"),

            ("Engine.StartupPackages", "+Package=CustomStretches"),
            ("Engine.StartupPackages", "Package=CustomStretches"),

            ("Engine.PackagesToNeverCompress", "+Package=CustomStretches"),
            ("Engine.PackagesToNeverCompress", "Package=CustomStretches"),

            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=\"Fp.UIDataStore_TdCustomMapsGameData\""),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=\"Fp.UIDataStore_TdCustomMapsGameData\""),
            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=\"Fp.UIDataStore_TdCustomMapsRaceData\""),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=\"Fp.UIDataStore_TdCustomMapsRaceData\""),
            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=Fp.UIDataStore_TdCustomMapsGameData"),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=Fp.UIDataStore_TdCustomMapsGameData"),
            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=Fp.UIDataStore_TdCustomMapsRaceData"),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=Fp.UIDataStore_TdCustomMapsRaceData"),

            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomTimeTrialData\""),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomTimeTrialData\""),
            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomMapCheckpoints\""),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=\"CustomStretches.UIDataStore_TdCustomMapCheckpoints\""),
            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=CustomStretches.UIDataStore_TdCustomTimeTrialData"),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=CustomStretches.UIDataStore_TdCustomTimeTrialData"),
            ("Engine.DataStoreClient", "+GlobalDataStoreClasses=CustomStretches.UIDataStore_TdCustomMapCheckpoints"),
            ("Engine.DataStoreClient", "GlobalDataStoreClasses=CustomStretches.UIDataStore_TdCustomMapCheckpoints")
        };

        foreach (var (section, value) in entriesToRemove)
        {
            Helpers.IniFileHelper.RemoveKeyValuePairFromIniFile(defaultEnginePath, section, value);
        }
    }

    private static void RestoreTdEngineForVanillaMenu(string tdEnginePath)
    {
        Helpers.IniFileHelper.RewriteTdEngineDataStoreClientSection(tdEnginePath, useMemmDataStores: false);
    }

    private static void RestoreTdEngineForMemm(string tdEnginePath)
    {
        Helpers.IniFileHelper.RewriteTdEngineDataStoreClientSection(tdEnginePath, useMemmDataStores: true);
    }

    private static void EnsureStockDefaultGameBackupIfPossible(string defaultGamePath, string stockBackupPath)
    {
        if (File.Exists(stockBackupPath) || !File.Exists(defaultGamePath))
            return;

        if (LooksLikeMemmModifiedDefaultGame(defaultGamePath))
            return;

        File.Copy(defaultGamePath, stockBackupPath, false);
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

    private static void DeletePathIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            RemoveReadOnlyAttributesRecursive(path);
            Directory.Delete(path, true);
            return;
        }

        if (File.Exists(path))
        {
            EnsureFileWritable(path);
            File.Delete(path);
        }
    }

    private static void MoveDirectoryContents(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
            return;

        Directory.CreateDirectory(destinationPath);

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            MovePathToDirectory(dir, destinationPath);
        }

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            MovePathToDirectory(file, destinationPath);
        }
    }

    private static void MovePathToDirectory(string sourcePath, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        if (Directory.Exists(sourcePath))
        {
            var directoryName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(directoryName))
                return;

            var destinationPath = Path.Combine(destinationDirectory, directoryName);
            if (Directory.Exists(destinationPath))
            {
                RemoveReadOnlyAttributesRecursive(destinationPath);
                Directory.Delete(destinationPath, true);
            }

            if (PathsShareRoot(sourcePath, destinationPath))
            {
                Directory.Move(sourcePath, destinationPath);
            }
            else
            {
                CopyDirectoryRecursive(sourcePath, destinationPath);
                RemoveReadOnlyAttributesRecursive(sourcePath);
                Directory.Delete(sourcePath, true);
            }
            return;
        }

        if (File.Exists(sourcePath))
        {
            var destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
            if (File.Exists(destinationPath))
            {
                File.SetAttributes(destinationPath, FileAttributes.Normal);
                File.Delete(destinationPath);
            }

            if (PathsShareRoot(sourcePath, destinationPath))
            {
                File.Move(sourcePath, destinationPath);
            }
            else
            {
                EnsureFileWritable(sourcePath);
                File.Copy(sourcePath, destinationPath, true);
                File.Delete(sourcePath);
            }
        }
    }

    private static bool PathsShareRoot(string sourcePath, string destinationPath)
    {
        var sourceRoot = Path.GetPathRoot(Path.GetFullPath(sourcePath));
        var destinationRoot = Path.GetPathRoot(Path.GetFullPath(destinationPath));
        return string.Equals(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyDirectoryRecursive(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var filePath in Directory.GetFiles(sourceDirectory))
        {
            var destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
            File.Copy(filePath, destinationFilePath, true);
        }

        foreach (var directoryPath in Directory.GetDirectories(sourceDirectory))
        {
            var destinationSubDirectory = Path.Combine(destinationDirectory, Path.GetFileName(directoryPath));
            CopyDirectoryRecursive(directoryPath, destinationSubDirectory);
        }
    }

    private static void RemoveReadOnlyAttributesRecursive(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attributes = File.GetAttributes(dir);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(dir, attributes & ~FileAttributes.ReadOnly);
                }
            }
            catch
            {
            }
        }

        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                }
            }
            catch
            {
            }
        }
    }

    private static void TryDeleteDirectoryIfEmpty(string path)
    {
        if (!Directory.Exists(path))
            return;

        if (!Directory.EnumerateFileSystemEntries(path).Any())
        {
            Directory.Delete(path);
        }
    }

    [RelayCommand]
    private async Task LaunchGame()
    {
        if (!IsValidGamePath)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please specify the correct game install folder path first.", DialogHelper.MessageType.Error);
            return;
        }

        if (!IsDocumentsPathValid)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please correct the issue with the Mirror's Edge Documents directory first.", DialogHelper.MessageType.Error);
            return;
        }

        if (!IsPatchActive)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please apply the config modification patch first.", DialogHelper.MessageType.Error);
            return;
        }

        if (!AreDependenciesInstalled)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please install the MEMM dependencies first.", DialogHelper.MessageType.Error);
            return;
        }

        var isSteam = _gameService.IsSteamVersion(GameInstallPath);

        if (isSteam)
        {
            var result = await DialogHelper.ShowConfirmationAsync(
                "Steam Version Detected",
                "The Steam version of Mirror's Edge has been detected.\n\n" +
                "Some users may encounter an 'Application load error' when launching outside of Steam.\n\n" +
                "Do you wish to try launching outside of Steam anyway?");

            if (!result)
                return;
        }

        var launchResult = _gameService.LaunchGame(GameInstallPath);
        if (!launchResult.success)
        {
            await DialogHelper.ShowMessageAsync("Error", launchResult.message, DialogHelper.MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task MoveMapLocationAsync()
    {
        if (IsMemmDisabled)
            return;

        if (IsDownloading)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please wait for map downloads to finish before moving map location.", DialogHelper.MessageType.Error);
            return;
        }

        if (!IsValidGamePath)
        {
            await DialogHelper.ShowMessageAsync("Error", "Please specify the correct game install folder path first.", DialogHelper.MessageType.Error);
            return;
        }

        var currentLocation = MemmLocation;
        var newLocation = currentLocation == "Published" ? "Game" : "Published";

        var currentPath = _pathService.GetMemmFolderPath(GameInstallPath, currentLocation);
        var newPath = _pathService.GetMemmFolderPath(GameInstallPath, newLocation);

        var normalisedCurrentPath = Path.GetFullPath(currentPath);
        var normalisedNewPath = Path.GetFullPath(newPath);
        if (string.Equals(normalisedCurrentPath, normalisedNewPath, StringComparison.OrdinalIgnoreCase))
        {
            await DialogHelper.ShowMessageAsync("Error", "Source and destination locations are identical. No move is required.", DialogHelper.MessageType.Error);
            return;
        }

        if (IsSameOrChildPath(normalisedNewPath, normalisedCurrentPath) ||
            IsSameOrChildPath(normalisedCurrentPath, normalisedNewPath))
        {
            await DialogHelper.ShowMessageAsync(
                "Error",
                "Map source and destination locations overlap. Please use a non-overlapping setup.",
                DialogHelper.MessageType.Error);
            return;
        }

        var result = await DialogHelper.ShowConfirmationAsync(
            "Move map location?",
            $"Maps are currently stored in the {currentLocation} folder location.\n\n" +
            $"Move them to the {newLocation} folder location?\n\n" +
            $"From: {currentPath}\n\n" +
            $"To: {newPath}" +
            $"\n\nNote: Moving maps to the game install location may be preferable if your game install is on a different drive and you do not want maps occupying storage on C: drive.");

        if (!result)
            return;

        IsLoading = true;
        LoadingStatus = "Moving maps...";
        var moveResult = await Task.Run(() =>
        {
            try
            {
                _pathService.EnsureDirectoryExists(newPath);

                if (Directory.Exists(currentPath))
                {
                    if (!PathsShareRoot(currentPath, newPath))
                    {
                        var sourceSizeBytes = _pathService.GetDirectorySize(currentPath);
                        var destinationFreeBytes = _pathService.GetDriveFreeSpace(newPath);
                        if (sourceSizeBytes > destinationFreeBytes)
                        {
                            var required = _pathService.FormatFileSize(sourceSizeBytes);
                            var available = _pathService.FormatFileSize(destinationFreeBytes);
                            return (success: false, message: $"Not enough free space on destination drive. Required: {required}, available: {available}.");
                        }
                    }

                    MoveDirectoryContentsForLocationSwitch(currentPath, newPath);
                    TryDeleteDirectoryIfEmpty(currentPath);
                }

                return (success: true, message: string.Empty);
            }
            catch (Exception ex)
            {
                return (success: false, message: ex.Message);
            }
        });

        IsLoading = false;
        LoadingStatus = "";

        if (!moveResult.success)
        {
            await DialogHelper.ShowMessageAsync("Error", $"Error moving maps: {moveResult.message}", DialogHelper.MessageType.Error);
            return;
        }

        MemmLocation = newLocation;
        SaveConfiguration();

        await RefreshMapsAsync();
        CalculateMapsSize();

        await DialogHelper.ShowMessageAsync("Success", $"Maps moved to {newLocation} location successfully.", DialogHelper.MessageType.Success);
    }

    private static bool IsSameOrChildPath(string potentialChildPath, string basePath)
    {
        var normalisedChild = Path.GetFullPath(potentialChildPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalisedBase = Path.GetFullPath(basePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalisedChild, normalisedBase, StringComparison.OrdinalIgnoreCase))
            return true;

        return normalisedChild.StartsWith(normalisedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void MoveDirectoryContentsForLocationSwitch(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
            return;

        Directory.CreateDirectory(destinationPath);

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            MovePathForLocationSwitch(dir, destinationPath);
        }

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            MovePathForLocationSwitch(file, destinationPath);
        }
    }

    private static void MovePathForLocationSwitch(string sourcePath, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        if (Directory.Exists(sourcePath))
        {
            var directoryName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(directoryName))
                return;

            var destinationPath = Path.Combine(destinationDirectory, directoryName);
            if (Directory.Exists(destinationPath))
            {
                MoveDirectoryContentsForLocationSwitch(sourcePath, destinationPath);
                TryDeleteDirectoryIfEmpty(sourcePath);
            }
            else if (PathsShareRoot(sourcePath, destinationPath))
            {
                Directory.Move(sourcePath, destinationPath);
            }
            else
            {
                CopyDirectoryRecursive(sourcePath, destinationPath);
                RemoveReadOnlyAttributesRecursive(sourcePath);
                Directory.Delete(sourcePath, true);
            }

            return;
        }

        if (File.Exists(sourcePath))
        {
            var destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
            if (File.Exists(destinationPath))
            {
                File.SetAttributes(destinationPath, FileAttributes.Normal);
                File.Delete(destinationPath);
            }

            if (PathsShareRoot(sourcePath, destinationPath))
            {
                File.Move(sourcePath, destinationPath);
            }
            else
            {
                EnsureFileWritable(sourcePath);
                File.Copy(sourcePath, destinationPath, true);
                File.Delete(sourcePath);
            }
        }
    }

    [RelayCommand]
    private async Task ShowAbout()
    {
        await DialogHelper.ShowMessageAsync(
            "About MEMM",
            "Mirror's Edge Map Manager (MEMM) — Version 2.1.0\n" +
            "Developed by softsoundd\n\n" +
            "Credits:\n\n" +
            "• Keku for creating the Custom Map Menu mod\n\n" +
            "• Toyro, BlackbeltGingaNinja, Heki, and Phoenix for moderating the custom map speedrun leaderboards\n\n" +
            "• The many talented map creators over the years!",
            DialogHelper.MessageType.Information);
    }

    [RelayCommand]
    private void ToggleSpeedrunStats()
    {
        IsSpeedrunExpanded = !IsSpeedrunExpanded;
    }

    [RelayCommand]
    private void OpenSpeedrunLeaderboard()
    {
        if (!string.IsNullOrEmpty(SpeedrunLeaderboardUrl))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = SpeedrunLeaderboardUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }
    }

    private void OnMapSelectionChanged(object? sender, EventArgs e)
    {
        UpdateSelectedMapsList();
    }
}

