using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Board.Domain;
using ReactiveUI;

namespace Board.UI.ViewModel;

public sealed partial class TokenViewModel : ReactiveObject
{
    private static readonly SolidColorBrush RoyalBlueBrush = new(Windows.UI.Color.FromArgb(255, 65, 105, 225));
    private static readonly SolidColorBrush YellowBrush = new(Colors.Yellow);
    private static readonly SolidColorBrush RedBrush = new(Colors.Red);

    private readonly ObservableAsPropertyHelper<Brush> _tokenBrush;

    public Brush TokenBrush => _tokenBrush.Value;

    public TokenPosition Position { get; private set; }
    
    /// <summary>
    /// Creates a TokenViewModel with initial state and subscribes to updates from the observable.
    /// Accepts any TokenState to support restoring games with placed tokens.
    /// </summary>
    public TokenViewModel(TokenPosition position, IObservable<TokenState> tokenObservable)
    {
        Position = position;

        // TokenBrush derives from Color changes
        _tokenBrush = tokenObservable
            .Select(token => token.Match(
                onEmpty: _ => (Brush)RoyalBlueBrush,
                onPlaced: placedToken => placedToken.Color switch
                {
                    TokenColor.Yellow => YellowBrush,
                    TokenColor.Red => RedBrush,
                    _ => throw new NotImplementedException(),
                }))
            .ToProperty(this, tvm => tvm.TokenBrush, scheduler: RxApp.MainThreadScheduler);
    }
}
