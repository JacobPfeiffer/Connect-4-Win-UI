using System.Reactive.Linq;
using Microsoft.UI.Xaml.Media;
using Board.Domain;
using ReactiveUI;

namespace Board.UI.ViewModel;

public sealed partial class TokenViewModel : ReactiveObject
{
    private readonly ObservableAsPropertyHelper<Brush> _tokenBrush;

    public Brush TokenBrush => _tokenBrush.Value;

    public TokenPosition Position { get; private set; }
    
    /// <summary>
    /// Creates a TokenViewModel with initial state and subscribes to updates from the observable.
    /// Accepts any TokenState to support restoring games with placed tokens.
    /// </summary>
    public TokenViewModel(TokenPosition position, IObservable<TokenState> tokenObservable, TokenStateToBrushConverter tokenColorToBrush)
    {
        Position = position;

        // TokenBrush derives from Color changes
        _tokenBrush = tokenObservable
            .Select(token => tokenColorToBrush(token))
            .ToProperty(this, tvm => tvm.TokenBrush, scheduler: RxApp.MainThreadScheduler);
    }
}