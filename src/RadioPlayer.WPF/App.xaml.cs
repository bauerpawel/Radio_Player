using System;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RadioPlayer.WPF.Services;
using RadioPlayer.WPF.ViewModels;
using RadioPlayer.WPF.Views;

namespace RadioPlayer.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the current ILanguageService instance for accessing translations
    /// </summary>
    public static ILanguageService? CurrentLanguageService { get; private set; }

    public App()
    {
        // Global exception handler
        this.DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show(
                $"Unhandled exception:\n\n{e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}",
                "Radio Player - Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Initialize database
            try
            {
                var repository = _serviceProvider.GetRequiredService<IRadioStationRepository>();
                await repository.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize database: {ex.Message}\n\nThe application will continue, but favorites and history may not work.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // Initialize language service
            try
            {
                var languageService = _serviceProvider.GetRequiredService<ILanguageService>();
                await languageService.InitializeAsync();
                CurrentLanguageService = languageService;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize language service: {ex.Message}\n\nThe application will use default language.",
                    "Language Service Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // Create and show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start application:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                "Radio Player - Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register HttpClient for services
        services.AddSingleton<HttpClient>();

        // Register services
        services.AddSingleton<IRadioBrowserService, RadioBrowserService>();
        services.AddSingleton<IRadioStationRepository, RadioStationRepository>();
        services.AddSingleton<IRadioPlayer, NAudioRadioPlayer>();
        services.AddSingleton<ILanguageService, LanguageService>();

        // Register ViewModels
        services.AddTransient<MainViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
