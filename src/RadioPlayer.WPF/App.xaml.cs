using System;
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
        // Register services (implementations will be added later)
        // services.AddSingleton<IRadioBrowserService, RadioBrowserService>();
        // services.AddSingleton<IRadioStationRepository, RadioStationRepository>();
        // services.AddSingleton<IRadioPlayer, NAudioRadioPlayer>();

        // Register ViewModels
        // services.AddTransient<MainViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
