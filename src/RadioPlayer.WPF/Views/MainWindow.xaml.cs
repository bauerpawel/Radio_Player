using System.Windows;
using System.Windows.Input;
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

        // Load top stations on startup
        Loaded += async (s, e) =>
        {
            await _viewModel.LoadTopStationsAsync();
        };
    }

    private async void StationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Play station on double-click
        if (_viewModel.PlayStationCommand.CanExecute(null))
        {
            await _viewModel.PlayStationAsync();
        }
    }
}
