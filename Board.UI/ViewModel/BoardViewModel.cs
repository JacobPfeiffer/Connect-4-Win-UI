using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Board.Domain;
using Board.IO.Services;
using ReactiveUI;

namespace Board.UI.ViewModel;

public sealed partial class BoardViewModel : ReactiveObject
{
    public ObservableCollection<BoardColumnViewModel> BoardColumns { get; }

    // Derived from the Active Player
    private readonly ObservableAsPropertyHelper<string> _activePlayerString;

    public string ActivePlayerString => _activePlayerString.Value;


    public BoardViewModel(ObservableCollection<BoardColumnViewModel> boardColumns, IObservable<Player> activePlayerObservable)
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
    }

}