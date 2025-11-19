using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using RadioPlayer.WPF.ViewModels;

namespace RadioPlayer.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Subscribe to property changes for station logo updates
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Load top stations on startup
        Loaded += (s, e) =>
        {
            _viewModel.LoadTopStationsCommand.Execute(null);
        };
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Update station logo when currently playing station changes
        if (e.PropertyName == nameof(MainViewModel.CurrentlyPlayingStation))
        {
            UpdateStationLogo();
        }
    }

    private void UpdateStationLogo()
    {
        var station = _viewModel.CurrentlyPlayingStation;

        if (station != null && !string.IsNullOrWhiteSpace(station.Favicon))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(station.Favicon);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 48; // Optimize for display size
                bitmap.EndInit();
                CurrentStationLogoImage.Source = bitmap;
            }
            catch
            {
                // If favicon fails to load, clear the image
                CurrentStationLogoImage.Source = null;
            }
        }
        else
        {
            // No station or no favicon - clear the image
            CurrentStationLogoImage.Source = null;
        }
    }

    private void StationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Play station on double-click
        if (_viewModel.PlayStationCommand.CanExecute(null))
        {
            _viewModel.PlayStationCommand.Execute(null);
        }
    }
}
