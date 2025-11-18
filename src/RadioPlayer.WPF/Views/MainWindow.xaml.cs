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
        Loaded += (s, e) =>
        {
            _viewModel.LoadTopStationsCommand.Execute(null);
        };
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
