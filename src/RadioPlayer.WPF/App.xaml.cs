using System;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RadioPlayer.WPF.Services;
using RadioPlayer.WPF.ViewModels;

namespace RadioPlayer.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // TODO: Initialize services when implementations are ready
        // var repository = _serviceProvider.GetRequiredService<IRadioStationRepository>();
        // await repository.InitializeDatabaseAsync();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register HttpClient for services
        services.AddSingleton<HttpClient>();

        // Register services
        services.AddSingleton<IRadioBrowserService, RadioBrowserService>();

        // TODO: Register when implementations are ready
        // services.AddSingleton<IRadioStationRepository, RadioStationRepository>();
        // services.AddSingleton<IRadioPlayer, NAudioRadioPlayer>();

        // Register ViewModels
        services.AddTransient<MainViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
