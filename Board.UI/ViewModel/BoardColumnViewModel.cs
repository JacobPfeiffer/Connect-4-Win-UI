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

    public ObservableCollection<TokenViewModel> ColumnTokens { get; }

    public ReactiveCommand<BoardColumnViewModel, Unit> PlaceTokenCommand { get; }

    public BoardColumnViewModel(ObservableCollection<TokenViewModel> columnTokens, TokenColumn column, IObservable<Unit> ColumnFull, Action<BoardColumnViewModel> PlaceToken)
    {
        Column = column;
        ColumnTokens = columnTokens;
        PlaceTokenCommand = ReactiveCommand.Create(
            PlaceToken,
            outputScheduler: RxApp.MainThreadScheduler);
        
    }    
}