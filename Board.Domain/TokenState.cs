namespace Board.Domain;

public readonly record struct TokenRow(uint RowIndex);

public readonly record struct TokenColumn(uint ColumnIndex);

public readonly record struct TokenPosition(TokenRow Row, TokenColumn Column);

public enum TokenColor
{
    Red,
    Yellow
}

/// <summary>
/// Base type for token states representing the lifecycle of a Connect Four token.
/// 
/// Token State Transitions:
/// <code>
///     ┌─────────┐
///     │  Empty  │ ◄─────┐
///     └────┬────┘       │
///          │            │
///          │ (hover)    │ (clear)
///          │            │
///          ▼            │
///     ┌─────────┐       │
///     │ Preview │───────┘
///     └────┬────┘
///          │
///          │ (click)
///          │
///          ▼
///     ┌─────────┐
///     │ Placed  │ (terminal state)
///     └─────────┘
/// </code>
/// 
/// This sealed class hierarchy enforces at compile time that:
/// - A token starts as Empty
/// - Preview tokens can be shown during hover and cleared back to Empty
/// - Placed tokens are the terminal state (no further transitions)
/// - Empty and Preview tokens have no permanent color
/// - Placed tokens must have a Red or Yellow color
/// </summary>
public abstract record class TokenState;

/// <summary>
/// An empty token at a specific board position.
/// Can be transitioned to a PreviewTokenState
/// </summary>
public sealed record class EmptyTokenState : TokenState
{
    public TokenPosition Position { get; }

    private EmptyTokenState(TokenPosition position)
    {
        Position = position;
    }

    /// <summary>Creates an empty token at the given position.</summary>
    public static EmptyTokenState Create(TokenPosition position) => 
        new(position);

    public static EmptyTokenState Create(PreviewTokenState previewToken) => 
        new(previewToken.Position);
}

/// <summary>
/// A preview token at a specific board position.
/// Can be transitioned to PlacedTokenState via Place().
/// </summary>
public sealed record class PreviewTokenState : TokenState
{
    public TokenPosition Position { get; }

    public TokenColor PreviewColor { get; }

    private PreviewTokenState(TokenPosition position, TokenColor previewColor)
    {
        Position = position;
        PreviewColor = previewColor;
    }

    /// <summary>Creates a preview token at the given position.</summary>
    public static PreviewTokenState Create(EmptyTokenState emptyToken, TokenColor previewColor) => 
        new(emptyToken.Position, previewColor);
}

/// <summary>
/// A token that has been placed with a color (Red or Yellow).
/// Created by calling Place() on an EmptyTokenState.
/// </summary>
public sealed record class PlacedTokenState : TokenState
{
    public TokenPosition  Position { get; }

    public TokenColor Color { get; }

    private PlacedTokenState(TokenPosition position, TokenColor color)
    {
        Position = position;
        Color = color;
    }

    /// <summary>
    /// Places a token by consuming a preview token and applying a color.
    /// This allows placing over a preview token that was shown during hover.
    /// </summary>
    public static PlacedTokenState Place(PreviewTokenState preview, TokenColor color) =>
        new(preview.Position, color);
}

/// <summary>
/// Extension methods for TokenState pattern matching.
/// </summary>
public static class TokenStateExtensions
{
    /// <summary>
    /// Pattern matches on a token state, returning a result.
    /// Exactly one handler will be called based on the actual state type.
    /// </summary>
    public static TResult Match<TResult>(
        this TokenState token,
        Func<EmptyTokenState, TResult> onEmpty,
        Func<PreviewTokenState, TResult> onPreview,
        Func<PlacedTokenState, TResult> onPlaced) =>
        token switch
        {
            EmptyTokenState empty => onEmpty(empty),
            PreviewTokenState preview => onPreview(preview),
            PlacedTokenState placed => onPlaced(placed),
            _ => throw new InvalidOperationException($"Unknown token state type: {token.GetType()}")
        };

    /// <summary>
    /// Pattern matches on a token state with side effects.
    /// Exactly one handler will be called based on the actual state type.
    /// </summary>
    public static void Match(
        this TokenState token,
        Action<EmptyTokenState> onEmpty,
        Action<PreviewTokenState> onPreview,
        Action<PlacedTokenState> onPlaced)
    {
        switch (token)
        {
            case EmptyTokenState empty:
                onEmpty(empty);
                break;
            case PreviewTokenState preview:
                onPreview(preview);
                break;
            case PlacedTokenState placed:   
                onPlaced(placed);
                break;
            default:
                throw new InvalidOperationException($"Unknown token state type: {token.GetType()}");
        }
    }
}


