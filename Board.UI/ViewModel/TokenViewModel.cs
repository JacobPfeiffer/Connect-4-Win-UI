using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media;
using Board.Domain;
using ReactiveUI;

namespace Board.UI.ViewModel;

public sealed class TokenViewModel : ReactiveObject
{
    private readonly ObservableAsPropertyHelper<Brush> _tokenBrush;

    private readonly ObservableAsPropertyHelper<bool> _tokenReserved;

    public Brush TokenBrush => _tokenBrush.Value;

    public bool TokenReserved => _tokenReserved.Value;

    public TokenPosition Position { get; }

    /// <summary>
    /// Creates a TokenViewModel with initial state and subscribes to updates from the observable.
    /// Accepts any TokenState to support restoring games with placed tokens.
    /// </summary>
    public TokenViewModel(TokenState initialState, IObservable<TokenState> tokenObservable)
    {
        Position = initialState.Match(empty => empty.Position, placed => placed.Position);

        // TokenBrush derives from Color changes
        _tokenBrush = tokenObservable
            .Select(token => token.Match(
                onEmpty: _ => Brushes.RoyalBlue,
                onPlaced: placedToken => placedToken.Color switch
                {
                    TokenColor.Yellow => Brushes.Yellow,
                    TokenColor.Red => Brushes.Red,
                    _ => throw new NotImplementedException(),
                }))
            .ToProperty(this, tvm => tvm.TokenBrush, scheduler: RxApp.MainThreadScheduler);

        // TokenReserved derives from Color changes
        _tokenReserved = tokenObservable
            .Select(token => token.Match(
                onEmpty: _ => true,
                onPlaced: _ => false))
            .ToProperty(this, tvm => tvm.TokenReserved, scheduler: RxApp.MainThreadScheduler);
    }
}
