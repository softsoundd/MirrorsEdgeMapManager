using CommunityToolkit.Mvvm.ComponentModel;

namespace MirrorsEdgeMapManager.Models;

public partial class MapViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private MapEntry? _mapEntry;

    [ObservableProperty]
    private int _originalIndex;

    public string InstallStatus => IsInstalled ? "✓" : "✗";
    public string InstallStatusColor => IsInstalled ? "#4CAF50" : "#9E9E9E";

    partial void OnIsInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(InstallStatus));
        OnPropertyChanged(nameof(InstallStatusColor));
    }

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SelectionChanged;
}

