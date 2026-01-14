using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using Board.Domain;
using LanguageExt.Pretty;
using Microsoft.UI.Xaml.Media;
using ReactiveUI;

namespace Board.UI.ViewModel;

public sealed partial class BoardViewModel : ReactiveObject
{
    private const string YourTurnText = "Your Turn: ";

    public ObservableCollection<BoardColumnViewModel> BoardColumns { get; }

    // Derived from the Active Player
    private readonly ObservableAsPropertyHelper<string> _activePlayerString;

    private readonly ObservableAsPropertyHelper<Brush> _activePlayerStringColor;

    public string ActivePlayerString => _activePlayerString.Value;

    public Brush ActivePlayerStringColor => _activePlayerStringColor.Value;


    public BoardViewModel(
        ObservableCollection<BoardColumnViewModel> boardColumns, 
        IObservable<Player> activePlayerObservable, 
        IObservable<GameStatus> gameStatusObservable,
        ColoringStrategy coloringStrategy, 
        TokenColorToBrushConverter tokenColorToBrush)
    {
        BoardColumns = boardColumns;

        // ActivePlayerString derives from ActivePlayer changes
        _activePlayerString = activePlayerObservable
            .Select(activePlayer => activePlayer switch
                {
                    Player.Player1 => "Player 1",
                    Player.Player2 => "Player 2",
                    _ => throw new NotSupportedException()
                })
            .ToProperty(this, x => x.ActivePlayerString, scheduler: RxApp.MainThreadScheduler);

        // ActivePlayerStringColor derives from ActivePlayer changes and coloring strategy
        _activePlayerStringColor = activePlayerObservable
            .Select(activePlayer => tokenColorToBrush(coloringStrategy(activePlayer)))
            .ToProperty(this, x => x.ActivePlayerStringColor, scheduler: RxApp.MainThreadScheduler);

        _ = gameStatusObservable
            .Do(status => Debug.WriteLine($"🔔 GameStatus observable emitted: {status}"))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(status =>
            {
                Console.WriteLine($"Game Status Changed: {status}");
                if(status is Won wonStatus)
                {
                    Console.WriteLine($"Game Over! {wonStatus.Winner} wins with color {wonStatus.WinningColor}!");
                }
            });
    }

}