using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Board.Domain;

/// <summary>
/// Strategy for determining token color based on current player.
/// </summary>
public delegate TokenColor ColoringStrategy(Player player);

/// <summary>
/// Represents all possible state transitions in the Connect Four game.
/// Using a discriminated union pattern with sealed record types ensures exhaustiveness.
/// </summary>
public abstract record BoardStateTransition();

public sealed record SwitchPlayer() : BoardStateTransition;

public sealed record PlaceToken(TokenColumn Column) : BoardStateTransition;


public static class BoardConstants
{
    public static readonly uint NumCells = 42;
}

public enum Player
{
    Player1,
    Player2
}

/// <summary>
/// Standard coloring strategies for Connect Four.
/// </summary>
public static class ColoringStrategies
{
    public static ColoringStrategy Player1Red => player => player == Player.Player1 ? TokenColor.Red : TokenColor.Yellow;
    public static ColoringStrategy Player1Yellow => player => player == Player.Player1 ? TokenColor.Yellow : TokenColor.Red;
}

public readonly record struct BoardState
{
    public IReadOnlyDictionary<TokenPosition, TokenState> BoardTokenState { get; init; }

    public Player CurrentPlayer { get; init; }

    public ColoringStrategy ColoringStrategy { get; init; }

    private BoardState(
        IReadOnlyDictionary<TokenPosition, TokenState> boardTokenState, 
        Player currentPlayer, 
        ColoringStrategy coloringStrategy)
    {
        BoardTokenState = boardTokenState;
        CurrentPlayer = currentPlayer;
        ColoringStrategy = coloringStrategy;
    }

    /*
        Board Layout
        ------------------------
        | 00 01 02 03 04 05 06 |
        | 07 08 09 10 11 12 13 |
        | 14 15 16 17 18 19 20 | 
        | 21 22 23 24 25 26 27 |
        | 28 29 30 31 32 33 34 |
        | 35 36 37 38 39 40 41 |
        ------------------------
    */
    private static IReadOnlyDictionary<TokenPosition, TokenState> InitializeBoardStateClear()
        => Enumerable.Range(0, (int)BoardConstants.NumCells)
            .Select(i => 
                new TokenPosition(
                    new TokenRow((uint)(i / 7)), 
                    new TokenColumn((uint)(i % 7))))
            .ToDictionary(
                key => key,
                value => (TokenState)EmptyTokenState.Create(value));
    private static Player InitializePlayerStart()
        => Player.Player1;

    public static Func<Player, Player> AdvancePlayer()
        => (currentPlayer) => currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;

    public static BoardState CreateCleanBoard(ColoringStrategy coloringStrategy) 
        => new(InitializeBoardStateClear(), InitializePlayerStart(), coloringStrategy);

    public static BoardState CreateRestoredBoard(Func<(IReadOnlyDictionary<TokenPosition, TokenState>, Player, ColoringStrategy)> fetchPreviousState)
    {
        (var ts, var player, var coloringStrategy) = fetchPreviousState();
        return new(ts, player, coloringStrategy);
    }
}

public static class BoardStateExtensions
{
    public static BoardState WithBoardTokenState(this BoardState state, IReadOnlyDictionary<TokenPosition, TokenState> boardTokenState)
        => state with { BoardTokenState = boardTokenState };

    public static BoardState WithCurrentPlayer(this BoardState state, Func<Player, Player> updatePlayerFunc)
        => state with { CurrentPlayer = updatePlayerFunc(state.CurrentPlayer) };

    /// <summary>
    /// Updates a single token at the specified position by applying a transformation function.
    /// </summary>
    public static BoardState UpdateTokenAtPosition(this BoardState state, TokenPosition position, Func<TokenState, TokenState> updateFunc)
    {
        var newTokens = state.BoardTokenState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (newTokens.TryGetValue(position, out var token))
        {
            newTokens[position] = updateFunc(token);
        }
        return state.WithBoardTokenState(newTokens);
    }

    /// <summary>
    /// Places a token in the specified column, filling the lowest available empty position.
    /// Uses the provided coloring strategy to determine the token color.
    /// </summary>
    private static BoardState PlaceTokenInColumn(this BoardState state, TokenColumn column, ColoringStrategy coloringStrategy)
    {
        var color = coloringStrategy(state.CurrentPlayer);
        
        var emptyToken = state.BoardTokenState
            .Where(kvp => kvp.Key.Column.ColumnIndex == column.ColumnIndex)
            .OrderByDescending(kvp => kvp.Key.Row.RowIndex)
            .FirstOrDefault(kvp => kvp.Value is EmptyTokenState);

        if (emptyToken.Value is EmptyTokenState empty)
        {
            return state.UpdateTokenAtPosition(emptyToken.Key, _ => PlacedTokenState.Place(empty, color));
        }

        // Column is full, return state unchanged
        return state;
    }

    /// <summary>
    /// Applies a state transition to the board state.
    /// Uses the board's own coloring strategy for token placement.
    /// </summary>
    public static BoardState ApplyTransition(this BoardState state, BoardStateTransition transition)
        => transition switch
        {
            PlaceToken placeToken => state.PlaceTokenInColumn(placeToken.Column, state.ColoringStrategy),
            SwitchPlayer => state.WithCurrentPlayer(BoardState.AdvancePlayer()),
            _ => throw new InvalidOperationException($"Unknown transition type: {transition.GetType()}")
        };

}
