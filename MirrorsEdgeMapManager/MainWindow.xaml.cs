using MirrorsEdgeMapManager.Models;
using MirrorsEdgeMapManager.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MirrorsEdgeMapManager;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitialiseAsync();
    }

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BrowseGameFolderCommand.ExecuteAsync(null);
    }

    private void SelectAllCustomMaps_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectAllMapsCommand.Execute("Custom Maps");
    }

    private void SelectAllTimeTrials_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectAllMapsCommand.Execute("Custom Time Trials");
    }

    private void SelectAllStory_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectAllMapsCommand.Execute("Story Experiences");
    }

    private async void MapListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is MapViewModel map)
        {
            await _viewModel.SelectMapCommand.ExecuteAsync(map);
        }
    }

    private void SortCustomMapsDate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SortMapsCommand.Execute(("Custom Maps", "Date"));
    }

    private void SortCustomMapsAlpha_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SortMapsCommand.Execute(("Custom Maps", "Alpha"));
    }

    private void SortTimeTrialsDate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SortMapsCommand.Execute(("Custom Time Trials", "Date"));
    }

    private void SortTimeTrialsAlpha_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SortMapsCommand.Execute(("Custom Time Trials", "Alpha"));
    }

    private void SortStoryDate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SortMapsCommand.Execute(("Story Experiences", "Date"));
    }

    private void SortStoryAlpha_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SortMapsCommand.Execute(("Story Experiences", "Alpha"));
    }

    private async void UnlockedConfigsChip_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        await _viewModel.ToggleConfigPatchCommand.ExecuteAsync(null);
    }

    private async void DependenciesChip_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        await _viewModel.OpenDependenciesWindowCommand.ExecuteAsync(null);
    }
}

