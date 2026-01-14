using System.Collections.ObjectModel;
using Board.Domain;
using Board.IO.Services;
using Board.UI;
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
    private ConnectFourWindow? _window;

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
        services.AddSingleton(ColoringStrategies.Player1Red);
        services.AddSingleton<TokenStateToBrushConverter>(TokenColorToBrushExtensions.GetBrushForTokenState);
        services.AddSingleton<TokenColorToBrushConverter>(TokenColorToBrushExtensions.ConvertFromTokenColorToBrush);

        // Register the BoardStateStore as singleton with coloring strategy
        services.AddSingleton<BoardStateStore>(sp =>
            new BoardStateStore(BoardState.CreateCleanBoard(sp.GetRequiredService<ColoringStrategy>())));

        // Register the BoardStateStoreService from the store
        services.AddSingleton<BoardStateStoreService>(sp =>
            sp.GetRequiredService<BoardStateStore>().GetService());

        // Register ViewModels
        services.AddTransient<BoardViewModel>(sp => 
            new BoardViewModel(
                new ObservableCollection<BoardColumnViewModel>(
                    sp.GetRequiredService<BoardStateStoreService>().GetBoardState().BoardTokenState.GroupByColumns().Columns
                        .OrderBy(kvp => kvp.Key.ColumnIndex)
                        .Select(kvp => 
                            new BoardColumnViewModel(
                                new ObservableCollection<TokenViewModel>(
                                    kvp.Value.Select(token => 
                                        new TokenViewModel(
                                            token.Key, 
                                            sp.GetRequiredService<BoardStateStoreService>().GetTokenObservable(token.Key),
                                            sp.GetRequiredService<TokenStateToBrushConverter>()))), 
                                kvp.Key, 
                                sp.GetRequiredService<BoardStateStoreService>().ColumnFullObservable(kvp.Key),
                                sp.GetRequiredService<BoardStateStoreService>().GameStatus,
                                tokenColumn => 
                                {
                                    sp.GetRequiredService<BoardStateStoreService>().UpdateBoardStateBatch(
                                        new PlaceToken(tokenColumn),
                                        new SwitchPlayer());
                                },
                                previewTokenColumn =>
                                {
                                    sp.GetRequiredService<BoardStateStoreService>().UpdateBoardState(
                                        new PlacePreviewToken(previewTokenColumn));
                                },
                                previewTokenColumn =>
                                {
                                    sp.GetRequiredService<BoardStateStoreService>().UpdateBoardState(
                                        new ClearPreviewToken(previewTokenColumn));
                                }))),
            sp.GetRequiredService<BoardStateStoreService>().PlayerChanged,
            sp.GetRequiredService<BoardStateStoreService>().GameStatus,
            sp.GetRequiredService<ColoringStrategy>(),
            sp.GetRequiredService<TokenColorToBrushConverter>()));

        services.AddTransient<ConnectFourWindow>();
    }
}

