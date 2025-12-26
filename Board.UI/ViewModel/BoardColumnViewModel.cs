using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Board.Domain;
using Board.IO.Services;
using Board.UI.View;
using ReactiveUI;

namespace Board.UI.ViewModel;

public sealed partial class BoardColumnViewModel : ReactiveObject
{
    public readonly TokenColumn Column;

    public ObservableCollection<TokenViewModel> TokensInColumn { get; }

    public ReactiveCommand<Unit, Unit> PlaceTokenCommand { get; }

    public BoardColumnViewModel(ObservableCollection<TokenViewModel> tokensInColumn, TokenColumn column, IObservable<Unit> ColumnFull, Action<BoardColumnViewModel> PlaceToken)
    {
        Column = column;
        TokensInColumn = tokensInColumn;
        PlaceTokenCommand = ReactiveCommand.Create(
            () => PlaceToken(this),
            outputScheduler: RxApp.MainThreadScheduler);
        
    }    
}