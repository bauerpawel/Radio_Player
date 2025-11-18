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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
                $"Failed to initialize database: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        // Create and show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register HttpClient for services
        services.AddSingleton<HttpClient>();

        // Register services
        services.AddSingleton<IRadioBrowserService, RadioBrowserService>();
        services.AddSingleton<IRadioStationRepository, RadioStationRepository>();
        services.AddSingleton<IRadioPlayer, NAudioRadioPlayer>();

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
