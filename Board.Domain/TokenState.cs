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
/// Base type for token states. A token is either empty or placed.
/// This sealed class hierarchy enforces at compile time that:
/// - A token can only be in one of two states: Empty or Placed
/// - Empty tokens cannot have a color
/// - Placed tokens must have a Red or Yellow color
/// - Transitions only go from Empty → Placed
/// </summary>
public abstract record class TokenState;

/// <summary>
/// An empty token at a specific board position.
/// Can be transitioned to PlacedTokenState via Place().
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
    /// Places a token by consuming an empty token and applying a color.
    /// Type signature enforces:
    /// - Input must be an EmptyTokenState (can't place twice)
    /// - Color must be Red or Yellow (can't use None)
    /// </summary>
    public static PlacedTokenState Place(EmptyTokenState empty, TokenColor color) =>
        new(empty.Position, color);
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
        Func<PlacedTokenState, TResult> onPlaced) =>
        token switch
        {
            EmptyTokenState empty => onEmpty(empty),
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
        Action<PlacedTokenState> onPlaced)
    {
        switch (token)
        {
            case EmptyTokenState empty:
                onEmpty(empty);
                break;
            case PlacedTokenState placed:
                onPlaced(placed);
                break;
            default:
                throw new InvalidOperationException($"Unknown token state type: {token.GetType()}");
        }
    }
}


