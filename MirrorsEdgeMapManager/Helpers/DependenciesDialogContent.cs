using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MirrorsEdgeMapManager.ViewModels;

namespace MirrorsEdgeMapManager.Helpers
{
    public class DependenciesDialogContent : UserControl
    {
        private readonly MainViewModel _viewModel;
        private TextBlock _customMapMenuModStatus = null!;
        private TextBlock _commonAssetsStatus = null!;
        private TextBlock _shaderCacheStatus = null!;
        private Button _menuModInstallButton = null!;
        private Button _commonAssetsButton = null!;
        private Button _shaderCacheButton = null!;
        private RadioButton _menuModStandardRadio = null!;
        private RadioButton _menuModTweaksUIRadio = null!;
        private readonly ProgressBar _progressBar;
        private readonly TextBlock _progressText;

        public DependenciesDialogContent(MainViewModel viewModel)
        {
            _viewModel = viewModel;

            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = Brushes.White,
                Padding = new Thickness(20),
                MinWidth = 500,
                MaxWidth = 600
            };

            var mainStack = new StackPanel();

            var titlePanel = new DockPanel { Margin = new Thickness(0, 0, 0, 20) };
            
            var title = new TextBlock
            {
                Text = "Install MEMM Dependencies",
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            DockPanel.SetDock(title, Dock.Left);
            titlePanel.Children.Add(title);

            var closeButton = new Button
            {
                Content = new PackIcon { Kind = PackIconKind.Close, Width = 20, Height = 20 },
                Style = (Style)Application.Current.FindResource("MaterialDesignIconButton"),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeButton.Click += (s, e) => DialogHost.CloseDialogCommand.Execute(null, closeButton);
            DockPanel.SetDock(closeButton, Dock.Right);
            titlePanel.Children.Add(closeButton);

            mainStack.Children.Add(titlePanel);

            var menuModSection = CreateMenuModSection();
            mainStack.Children.Add(menuModSection);

            var assetsGrid = CreateDependencyRow(
                "Common Assets",
                "Recommended",
                "Common assets containing shared textures, materials, and other resources used by multiple custom maps.\n\n" +
                "While not strictly required, many custom maps depend on these assets and won't work properly without them.",
                out _commonAssetsStatus,
                out _commonAssetsButton,
                async () => await InstallDependencyAsync("CommonAssets"));
            mainStack.Children.Add(assetsGrid);

            var shaderGrid = CreateDependencyRow(
                "Shader Cache",
                "Recommended",
                "Precompiled shader cache which significantly reduces loading times and\n" +
                "prevents stuttering/crashing when playing custom maps for the first time.",
                out _shaderCacheStatus,
                out _shaderCacheButton,
                async () => await InstallDependencyAsync("ShaderCache"));
            mainStack.Children.Add(shaderGrid);

            var progressStack = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };
            
            _progressText = new TextBlock
            {
                Text = "",
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 0, 5)
            };
            progressStack.Children.Add(_progressText);

            _progressBar = new ProgressBar
            {
                Height = 6,
                Visibility = Visibility.Collapsed
            };
            progressStack.Children.Add(_progressBar);

            mainStack.Children.Add(progressStack);

            border.Child = mainStack;
            Content = border;

            UpdateStatusDisplay();
            SetButtonsEnabled(true);
        }

        private Grid CreateDependencyRow(
            string title,
            string subtitle,
            string infoTooltip,
            out TextBlock statusText,
            out Button installButton,
            Func<Task> onInstall)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var infoStack = new StackPanel();
            Grid.SetColumn(infoStack, 0);

            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            };
            infoStack.Children.Add(titleText);

            var subtitleText = new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Opacity = 0.7
            };
            infoStack.Children.Add(subtitleText);

            grid.Children.Add(infoStack);

            statusText = new TextBlock
            {
                Text = "Checking...",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };
            Grid.SetColumn(statusText, 1);
            grid.Children.Add(statusText);

            var infoIcon = new PackIcon
            {
                Kind = PackIconKind.Information,
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(96, 96, 96)),
                ToolTip = infoTooltip
            };
            Grid.SetColumn(infoIcon, 2);
            grid.Children.Add(infoIcon);

            installButton = new Button
            {
                Content = "Install",
                Style = (Style)Application.Current.FindResource("MaterialDesignRaisedButton"),
                FontSize = 13,
                Width = 80
            };
            installButton.Click += async (s, e) => await onInstall();
            Grid.SetColumn(installButton, 3);
            grid.Children.Add(installButton);

            return grid;
        }

        private Border CreateMenuModSection()
        {
            var section = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 15),
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            var mainStack = new StackPanel();

            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerStack = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = "Custom Map Menu Mod",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            };
            headerStack.Children.Add(titleText);

            var subtitleText = new TextBlock
            {
                Text = "Required - Choose one variant",
                FontSize = 12,
                Opacity = 0.7,
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40))
            };
            headerStack.Children.Add(subtitleText);

            Grid.SetColumn(headerStack, 0);
            headerGrid.Children.Add(headerStack);

            _customMapMenuModStatus = new TextBlock
            {
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_customMapMenuModStatus, 1);
            headerGrid.Children.Add(_customMapMenuModStatus);

            mainStack.Children.Add(headerGrid);

            var optionsStack = new StackPanel { Margin = new Thickness(0, 5, 0, 10) };

            _menuModStandardRadio = new RadioButton
            {
                Content = "Standard",
                FontSize = 13,
                GroupName = "MenuModVariant",
                Margin = new Thickness(0, 0, 0, 5)
            };
            optionsStack.Children.Add(_menuModStandardRadio);

            var tweaksUIStack = new StackPanel();
            _menuModTweaksUIRadio = new RadioButton
            {
                Content = "Tweaks Scripts UI",
                FontSize = 13,
                GroupName = "MenuModVariant"
            };
            tweaksUIStack.Children.Add(_menuModTweaksUIRadio);

            var tweaksUIDesc = new TextBlock
            {
                Text = "Includes the Tweaks Scripts UI interface",
                FontSize = 11,
                Opacity = 0.6,
                Margin = new Thickness(20, 2, 0, 0)
            };
            tweaksUIStack.Children.Add(tweaksUIDesc);
            optionsStack.Children.Add(tweaksUIStack);

            mainStack.Children.Add(optionsStack);

            var installStack = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var infoIcon = new PackIcon
            {
                Kind = PackIconKind.Information,
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(96, 96, 96)),
                ToolTip = "The Custom Map Menu Mod adds support for custom maps and time trials in Mirror's Edge.\n\n" +
                         "The Tweaks Scripts UI variant includes menu buttons for interacting with Tweaks Scripts, if installed."
            };
            installStack.Children.Add(infoIcon);

            _menuModInstallButton = new Button
            {
                Content = "Install Selected Variant",
                Style = (Style)Application.Current.FindResource("MaterialDesignRaisedButton"),
                FontSize = 13
            };
            _menuModInstallButton.Click += async (s, e) =>
            {
                var dependencyType = _menuModTweaksUIRadio.IsChecked == true
                    ? "CustomMapMenuModTweaksUI"
                    : "CustomMapMenuMod";
                await InstallDependencyAsync(dependencyType);
            };
            installStack.Children.Add(_menuModInstallButton);

            mainStack.Children.Add(installStack);

            section.Child = mainStack;
            return section;
        }

        private void UpdateStatusDisplay()
        {
            var status = _viewModel.GetDependencyStatus();

            if (status.CustomMapMenuModInstalled)
            {
                var variant = status.IsTweaksUIVariant ? "Tweaks UI" : "Standard";
                _customMapMenuModStatus.Text = $"Installed ({variant})";
                _customMapMenuModStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                
                _menuModTweaksUIRadio.IsChecked = status.IsTweaksUIVariant;
                _menuModStandardRadio.IsChecked = !status.IsTweaksUIVariant;
            }
            else
            {
                _customMapMenuModStatus.Text = "Not installed";
                _customMapMenuModStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                
                _menuModStandardRadio.IsChecked = true;
                _menuModTweaksUIRadio.IsChecked = false;
            }

            _commonAssetsStatus.Text = status.CommonAssetsInstalled ? "Installed" : "Not installed";
            _commonAssetsStatus.Foreground = status.CommonAssetsInstalled
                ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                : new SolidColorBrush(Color.FromRgb(198, 40, 40));

            _shaderCacheStatus.Text = status.ShaderCacheInstalled ? "Installed" : "Not installed";
            _shaderCacheStatus.Foreground = status.ShaderCacheInstalled
                ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                : new SolidColorBrush(Color.FromRgb(198, 40, 40));
        }

        private async Task InstallDependencyAsync(string dependencyType)
        {
            if (_viewModel.IsMemmDisabled)
                return;

            SetButtonsEnabled(false);
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.Value = 0;

            var progress = new Progress<(int percentage, string status)>(p =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _progressBar.Value = p.percentage;
                    _progressText.Text = p.status;
                });
            });

            try
            {
                await _viewModel.InstallDependencyAsync(dependencyType, progress);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _progressBar.Visibility = Visibility.Collapsed;
                    _progressText.Text = "";
                    SetButtonsEnabled(true);
                    UpdateStatusDisplay();
                });
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            var canInteract = enabled && !_viewModel.IsMemmDisabled;
            _menuModInstallButton.IsEnabled = canInteract;
            _commonAssetsButton.IsEnabled = canInteract;
            _shaderCacheButton.IsEnabled = canInteract;
            _menuModStandardRadio.IsEnabled = canInteract;
            _menuModTweaksUIRadio.IsEnabled = canInteract;
        }
    }
}



