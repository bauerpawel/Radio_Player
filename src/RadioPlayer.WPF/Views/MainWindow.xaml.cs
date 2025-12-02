using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using RadioPlayer.WPF.Helpers;
using RadioPlayer.WPF.Services;
using RadioPlayer.WPF.ViewModels;
using Forms = System.Windows.Forms;

namespace RadioPlayer.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IGlobalHotkeyService _hotkeyService;
    private Forms.NotifyIcon? _notifyIcon;
    private bool _isClosing = false;

    public MainWindow(MainViewModel viewModel, IGlobalHotkeyService hotkeyService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = _viewModel;

        // Subscribe to property changes for station logo updates
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Initialize system tray icon
        InitializeNotifyIcon();

        // Handle window state changes for minimize to tray
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;

        // Load top stations on startup
        Loaded += (s, e) =>
        {
            _viewModel.LoadTopStationsCommand.Execute(null);

            // Register global hotkeys after window is loaded
            InitializeGlobalHotkeys();
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

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadApplicationIcon(),
            Text = "Radio Player",
            Visible = false
        };

        // Create context menu for tray icon
        var contextMenu = new Forms.ContextMenuStrip();

        var restoreMenuItem = new Forms.ToolStripMenuItem("Restore", null, (s, e) => RestoreWindow());
        var exitMenuItem = new Forms.ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());

        contextMenu.Items.Add(restoreMenuItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Double-click to restore
        _notifyIcon.DoubleClick += (s, e) => RestoreWindow();
    }

    private Icon LoadApplicationIcon()
    {
        try
        {
            // Try to load custom icon from Resources folder
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // If loading fails, fall back to system icon
        }

        // Fall back to default system icon
        return SystemIcons.Application;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // Only minimize to tray if the setting is enabled
        if (WindowState == WindowState.Minimized && AppConstants.UI.MinimizeToTray)
        {
            Hide();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(2000, "Radio Player", "Application minimized to tray", Forms.ToolTipIcon.Info);
            }
        }
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }
    }

    private void ExitApplication()
    {
        _isClosing = true;
        Close();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // If minimize to tray is enabled and user clicks X, minimize instead of closing
        if (!_isClosing && AppConstants.UI.MinimizeToTray)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
        else
        {
            // Clean up global hotkeys
            _hotkeyService?.Dispose();

            // Clean up notify icon
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }

    private void InitializeGlobalHotkeys()
    {
        try
        {
            _hotkeyService.RegisterHotkeys(
                onPlayPause: () => Dispatcher.Invoke(() =>
                {
                    if (_viewModel.TogglePlayPauseCommand.CanExecute(null))
                    {
                        _viewModel.TogglePlayPauseCommand.Execute(null);
                    }
                }),
                onStop: () => Dispatcher.Invoke(() =>
                {
                    if (_viewModel.StopStationCommand.CanExecute(null))
                    {
                        _viewModel.StopStationCommand.Execute(null);
                    }
                }),
                onNextStation: () => Dispatcher.Invoke(() =>
                {
                    if (_viewModel.NextStationCommand.CanExecute(null))
                    {
                        _viewModel.NextStationCommand.Execute(null);
                    }
                }),
                onPreviousStation: () => Dispatcher.Invoke(() =>
                {
                    if (_viewModel.PreviousStationCommand.CanExecute(null))
                    {
                        _viewModel.PreviousStationCommand.Execute(null);
                    }
                }),
                onVolumeUp: () => Dispatcher.Invoke(() =>
                {
                    if (_viewModel.VolumeUpCommand.CanExecute(null))
                    {
                        _viewModel.VolumeUpCommand.Execute(null);
                    }
                }),
                onVolumeDown: () => Dispatcher.Invoke(() =>
                {
                    if (_viewModel.VolumeDownCommand.CanExecute(null))
                    {
                        _viewModel.VolumeDownCommand.Execute(null);
                    }
                }),
                onMute: () => Dispatcher.Invoke(() =>
                {
                    if (_viewModel.ToggleMuteCommand.CanExecute(null))
                    {
                        _viewModel.ToggleMuteCommand.Execute(null);
                    }
                })
            );

            System.Diagnostics.Debug.WriteLine("[MainWindow] Global hotkeys registered successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to register global hotkeys: {ex.Message}");
            // Don't show error to user - hotkeys are optional feature
        }
    }
}
