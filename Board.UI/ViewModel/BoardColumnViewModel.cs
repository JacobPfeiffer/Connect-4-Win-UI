using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Board.Domain;
using Board.IO.Services;
using Board.UI.View;
using ReactiveUI;
using Windows.Media.Capture.Core;

namespace Board.UI.ViewModel;

public sealed partial class BoardColumnViewModel : ReactiveObject, IDisposable
{
    private bool _isColumnHovered;

    private readonly ObservableAsPropertyHelper<bool> _canPlaceInColumn;

    private readonly CompositeDisposable _disposables = new();

    public bool IsColumnHovered
    {
        get => _isColumnHovered;
        set => this.RaiseAndSetIfChanged(ref _isColumnHovered, value);
    }

    public bool CanPlaceInColumn => _canPlaceInColumn.Value;

    public readonly TokenColumn Column;

    public ObservableCollection<TokenViewModel> TokensInColumn { get; }

    public ReactiveCommand<Unit, Unit> PlaceTokenCommand { get; }

    public ViewModelActivator Activator { get; } = new();

    // FIXME: the types should be the io types from the board store service
    public BoardColumnViewModel(
        ObservableCollection<TokenViewModel> tokensInColumn, 
        TokenColumn column, 
        IObservable<Unit> columnFull, 
        Action<TokenColumn> placeToken,
        Action<TokenColumn> previewTokenPlacement,
        Action<TokenColumn> clearTokenPreview)
    {
        // CanPlaceInColumn starts true, becomes false when column is full
        _canPlaceInColumn = columnFull
            .Select(_ => false)
            .StartWith(true)
            .ToProperty(this, bcf => bcf.CanPlaceInColumn, initialValue: true, scheduler: RxApp.MainThreadScheduler);

        Column = column;
        TokensInColumn = tokensInColumn;
        PlaceTokenCommand = ReactiveCommand.Create(
            () => placeToken(Column),
            outputScheduler: RxApp.MainThreadScheduler);

        // Set up preview token behavior and store subscription for disposal
        PreviewTokenPlacementOnHover(previewTokenPlacement, clearTokenPreview)
            .Subscribe()
            .DisposeWith(_disposables);
        
    }    

    public void Dispose()
    {
        _disposables.Dispose();
    }    

    // Side effect stream that finds the lowest empty token in the column and previews placement on hover
    // Store service does the heavy lifting of determining the lowest empty token
    // If the column is full, no preview occurs
    private IObservable<Unit> PreviewTokenPlacementOnHover(Action<TokenColumn> previewTokenPlacement, Action<TokenColumn> clearTokenPreview)
    {
        var onHoverStream = this.WhenAnyValue(
            x => x.IsColumnHovered,
            x => x.CanPlaceInColumn,
            (isHovered, canPlace) => (isHovered, canPlace))
            .DistinctUntilChanged();

        var previewPlacedStream = PlaceTokenCommand.WithLatestFrom(onHoverStream, (_, hoverState) => hoverState)
            .Where(state => state.isHovered && state.canPlace);

        return onHoverStream.Merge(previewPlacedStream)
            .Do(state =>
            {
                if(state.isHovered && state.canPlace)
                {
                    // Preview token placement only if column can accept tokens
                    previewTokenPlacement(Column);
                }
                else
                {
                    // Clear token preview
                    clearTokenPreview(Column);
                }
            })
            .TakeUntil(state => !state.canPlace)
            .Select(_ => Unit.Default);
    }
}