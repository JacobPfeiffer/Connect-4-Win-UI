using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Board.Domain;
using Board.IO.Services;
using ReactiveUI;

namespace Board.UI.ViewModel;

public sealed class BoardViewModel : ReactiveObject
{
    private readonly BoardStateStoreService _storeService;

    public ObservableCollection<TokenViewModel> BoardCells { get; }

    // Derived from the Active Player
    private readonly ObservableAsPropertyHelper<string> _activePlayerString;

    public string ActivePlayerString => _activePlayerString.Value;

    public ReactiveCommand<TokenViewModel, Unit> PlaceTokenCommand { get; }

    public BoardViewModel(BoardStateStoreService storeService)
    {
        _storeService = storeService;

        PlaceTokenCommand = ReactiveCommand.Create<TokenViewModel>(
            PlaceToken, 
            outputScheduler: RxApp.MainThreadScheduler);

        // Initialize BoardCells from store - supports both new and restored games
        var initialState = storeService.GetBoardState();
        BoardCells = new ObservableCollection<TokenViewModel>(
            initialState.BoardTokenState
                .Select(kvp => new TokenViewModel(
                    kvp.Value, 
                    storeService.GetTokenObservable(kvp.Key)
                .ObserveOn(RxApp.MainThreadScheduler))));

        // ActivePlayerString derives from ActivePlayer changes
        _activePlayerString = _storeService.PlayerChanged
            .Select(activePlayer => activePlayer switch
            {
                Player.Player1 => "Player 1",
                Player.Player2 => "Player 2",
                _ => throw new NotSupportedException()
            })
            .ToProperty(this, x => x.ActivePlayerString, scheduler: RxApp.MainThreadScheduler);
    }

    private void PlaceToken(TokenViewModel tvm)
    {
        _storeService.UpdateBoardStateBatch(
            new PlaceToken(tvm.Position.Column),
            new SwitchPlayer());
    }
}