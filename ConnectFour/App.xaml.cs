using Board.Domain;
using Board.IO.Services;
using Board.UI.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace ConnectFour;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Create and activate the main window from DI
        _window = _serviceProvider.GetRequiredService<ConnectFourWindow>();
        _window.Activate();
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
}

