using Board.Domain;
using Board.IO.Services;
using Board.UI.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ConnectFour;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Create and show the main window from DI
        var connectFourWindow = _serviceProvider.GetRequiredService<ConnectFourWindow>();
        connectFourWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register the BoardStateStore as singleton with coloring strategy
        services.AddSingleton<BoardStateStore>(sp =>
            new BoardStateStore(BoardState.CreateCleanBoard(ColoringStrategies.Player1Red)));

        // Register the BoardStateStoreService from the store
        services.AddSingleton<BoardStateStoreService>(sp =>
            sp.GetRequiredService<BoardStateStore>().GetService());

        // Register ViewModels
        services.AddTransient<BoardViewModel>();
        services.AddTransient<ConnectFourWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}

