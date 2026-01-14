using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Windows.UI.Notifications;

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

public sealed record PlacePreviewToken(TokenColumn Column) : BoardStateTransition;

public sealed record ClearPreviewToken(TokenColumn Column) : BoardStateTransition;


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

/// <summary>
/// Represents the current status of the game.
/// Discriminated union ensuring exhaustive pattern matching.
/// </summary>
public abstract record GameStatus();
public sealed record InProgress() : GameStatus;
public sealed record Won(Player Winner, TokenColor WinningColor, IReadOnlyList<TokenPosition> WinningPositions) : GameStatus;
public sealed record Draw() : GameStatus; // Board full, no winner

public static class GameStatusExtensions
{
    public static TResult Match<TResult>(this GameStatus status, Func<InProgress, TResult> inProgressFunc, Func<Won, TResult> wonFunc, Func<Draw, TResult> drawFunc)
        => status switch
        {
            InProgress ip => inProgressFunc(ip),
            Won w => wonFunc(w),
            Draw d => drawFunc(d),
            _ => throw new NotSupportedException($"Unknown GameStatus type: {status.GetType()}")
        };
}

public readonly record struct BoardTokenState(IReadOnlyDictionary<TokenPosition, TokenState> BoardTokenLookup);

public readonly record struct BoardTokenStateByColumn(IReadOnlyDictionary<TokenColumn, IOrderedEnumerable<KeyValuePair<TokenPosition, TokenState>>> Columns);

public readonly record struct BoardTokenStateByRow(IReadOnlyDictionary<TokenRow, IOrderedEnumerable<KeyValuePair<TokenPosition, TokenState>>> Rows);

// FIXME: this needs to take a TokenPosition as a key and return the diagonals that pass through that position
// TODO: function to get all ascending diagonals and all descending diagonals
// TODO: function to get all ascending and descending diagonals that pass through a given position. This should also only return diagonals of length >= 4.
// public readonly record struct BoardTokenStateByDiagonal(
//     IReadOnlyList<IOrderedEnumerable<KeyValuePair<TokenPosition, TokenState>>> Diagonals);


public readonly record struct AscendingDiagonalKey
{
    public uint SumIndex { get; init; }

    private AscendingDiagonalKey(uint sumIndex)
    {
        SumIndex = sumIndex;
    }

    public static AscendingDiagonalKey FromTokenPosition(TokenPosition position)
        => new(position.Row.RowIndex + position.Column.ColumnIndex);
}

public readonly record struct DescendingDiagonalKey
{
    // must be signed to accommodate negative indices
    public int DifferenceIndex { get; init; }

    private DescendingDiagonalKey(int differenceIndex)
    {
        DifferenceIndex = differenceIndex;
    }

    // Casting to int is safe here since RowIndex and ColumnIndex are both uints and the difference will always fit in an int due to board size limits
    public static DescendingDiagonalKey FromTokenPosition(TokenPosition position)
        => new((int)position.Row.RowIndex - (int)position.Column.ColumnIndex);
}

/// <summary>
/// Represents the board token states grouped by ascending diagonals (from bottom-left to top-right / ).
/// </summary>
/// <param name="Diagonals"></param>
public readonly record struct BoardTokenStateByAscendingDiagonal(IReadOnlyDictionary<AscendingDiagonalKey, IOrderedEnumerable<KeyValuePair<TokenPosition, TokenState>>> Diagonals);

/// <summary>
/// Represents the board token states grouped by descending diagonals (from top-left to bottom-right \ ).
/// </summary>
/// <param name="Diagonals"></param>
public readonly record struct BoardTokenStateByDescendingDiagonal(IReadOnlyDictionary<DescendingDiagonalKey, IOrderedEnumerable<KeyValuePair<TokenPosition, TokenState>>> Diagonals);

// public readonly record struct BoardTokenStateByDiagonal(
//     IReadOnlyList<IReadOnlyDictionary<TokenPosition, TokenState>> AscendingDiagonals,
//     IReadOnlyList<IReadOnlyDictionary<TokenPosition, TokenState>> DescendingDiagonals);


public static class BoardTokenStateExtensions
{
    public static BoardTokenStateByColumn GroupByColumns(this BoardTokenState boardTokenState)
        => new (boardTokenState.BoardTokenLookup.GroupBy(kvp => kvp.Key.Column)
            .ToDictionary(group => group.Key, group => group.OrderBy(key => key.Key.Row.RowIndex)));

    public static BoardTokenStateByRow GroupByRows(this BoardTokenState boardTokenState)
        => new (boardTokenState.BoardTokenLookup.GroupBy(kvp => kvp.Key.Row)
            .ToDictionary(group => group.Key, group => group.OrderBy(key => key.Key.Column.ColumnIndex)));
    
    public static BoardTokenStateByAscendingDiagonal GroupByAscendingDiagonals(this BoardTokenState boardTokenState)
    => new(boardTokenState.BoardTokenLookup
        .GroupBy(kvp => AscendingDiagonalKey.FromTokenPosition(kvp.Key))
        .ToDictionary(
            group => group.Key,
            group => group.OrderBy(kvp => kvp.Key.Column.ColumnIndex)));

    public static BoardTokenStateByDescendingDiagonal GroupByDescendingDiagonals(this BoardTokenState boardTokenState)
        => new(boardTokenState.BoardTokenLookup
            .GroupBy(kvp => DescendingDiagonalKey.FromTokenPosition(kvp.Key))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(kvp => kvp.Key.Column.ColumnIndex)));
    
    public static bool IsColumnFull(this BoardTokenStateByColumn boardTokenStateByColumn, TokenColumn column)
        => boardTokenStateByColumn.Columns.TryGetValue(column, out var tokensInColumn) && tokensInColumn.Select(token => token.Value).All(tokenState => tokenState is PlacedTokenState);

    /// <summary>
    /// Checks if a player has won at the specified position.
    /// Returns Some(winningPositions) if a win is detected, None otherwise.
    /// </summary>
    /// TODO: WinningPositions should be its own type that enforces length of 4
    public static Option<IReadOnlyList<TokenPosition>> HasPlayerWonAtPosition(
        this BoardTokenState boardTokenState, 
        TokenPosition position, 
        TokenColor color)
    {
        var boardByColumns = boardTokenState.GroupByColumns();
        var boardByRows = boardTokenState.GroupByRows();
        var boardByAscDiagonals = boardTokenState.GroupByAscendingDiagonals();
        var boardByDescDiagonals = boardTokenState.GroupByDescendingDiagonals();

        // Local function to find winning positions in a direction
        Option<IReadOnlyList<TokenPosition>> FindWinningLine(IOrderedEnumerable<KeyValuePair<TokenPosition, TokenState>> tokens)
        {
            var positions = new List<TokenPosition>();
            foreach (var kvp in tokens)
            {
                if (kvp.Value is PlacedTokenState placedToken && placedToken.Color == color)
                {
                    positions.Add(kvp.Key);
                    if (positions.Count >= 4)
                        return Option<IReadOnlyList<TokenPosition>>.Some(positions.TakeLast(4).ToList());
                }
                else
                {
                    positions.Clear();
                }
            }
            return Option<IReadOnlyList<TokenPosition>>.None;
        }

        // Check each direction with early return using | operator
        return FindWinningLine(boardByRows.Rows[position.Row])
            | FindWinningLine(boardByColumns.Columns[position.Column])
            | (boardByAscDiagonals.Diagonals.TryGetValue(AscendingDiagonalKey.FromTokenPosition(position), out var ascDiag) 
                ? FindWinningLine(ascDiag) 
                : Option<IReadOnlyList<TokenPosition>>.None)
            | (boardByDescDiagonals.Diagonals.TryGetValue(DescendingDiagonalKey.FromTokenPosition(position), out var descDiag) 
                ? FindWinningLine(descDiag) 
                : Option<IReadOnlyList<TokenPosition>>.None);
    }

    public static bool IsBoardFull(this BoardTokenState boardTokenState)
        => boardTokenState.BoardTokenLookup.Values.All(token => token is PlacedTokenState);

    /// <summary>
    /// Checks if the game has ended after placing a token at the specified position.
    /// Returns updated GameStatus if win/draw detected.
    /// </summary>
    public static GameStatus CheckGameStatus(this BoardState state, TokenPosition lastPlacedPosition, TokenColor currentColor)
    {
        return state.BoardTokenState.HasPlayerWonAtPosition(lastPlacedPosition, currentColor)
            .Match(
                None: () =>
                {
                    // Check for draw (board full)
                    var isBoardFull = state.BoardTokenState.IsBoardFull();
                    
                    if(isBoardFull)
                    {
                        return (GameStatus)new Draw();
                    }
                    
                    return new InProgress();
                },
                Some: winningPositions => new Won(state.CurrentPlayer, currentColor, winningPositions));
        
    }
}

public readonly record struct BoardState
{
    public BoardTokenState BoardTokenState { get; init; }

    public Player CurrentPlayer { get; init; }

    public ColoringStrategy ColoringStrategy { get; init; }

    public GameStatus CurrentGameStatus { get; init; }

    private BoardState(
        BoardTokenState boardTokenState, 
        Player currentPlayer, 
        ColoringStrategy coloringStrategy,
        GameStatus currentGameStatus)
    {
        BoardTokenState = boardTokenState;
        CurrentPlayer = currentPlayer;
        ColoringStrategy = coloringStrategy;
        CurrentGameStatus = currentGameStatus;
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
    private static BoardTokenState InitializeBoardStateClear()
        => new(Enumerable.Range(0, (int)BoardConstants.NumCells)
            .Select(i => 
                new TokenPosition(
                    new TokenRow((uint)(i / 7)), 
                    new TokenColumn((uint)(i % 7))))
            .ToDictionary(
                key => key,
                value => (TokenState)EmptyTokenState.Create(value)));

    private static Player InitializePlayerStart()
        => Player.Player1;

    public static Func<Player, Player> AdvancePlayer()
        => (currentPlayer) => currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;

    public static BoardState CreateCleanBoard(ColoringStrategy coloringStrategy) 
        => new(InitializeBoardStateClear(), InitializePlayerStart(), coloringStrategy, new InProgress());

    public static BoardState CreateRestoredBoard(Func<(BoardTokenState, Player, ColoringStrategy, GameStatus)> fetchPreviousState)
    {
        var (ts, player, coloringStrategy, status) = fetchPreviousState();
        return new(ts, player, coloringStrategy, status);
    }
}

public static class BoardStateExtensions
{
    public static BoardState WithBoardTokenState(this BoardState state, BoardTokenState boardTokenState)
        => state with { BoardTokenState = boardTokenState };

    public static BoardState WithCurrentPlayer(this BoardState state, Func<Player, Player> updatePlayerFunc)
        => state with { CurrentPlayer = updatePlayerFunc(state.CurrentPlayer) };

    /// <summary>
    /// Updates a single token at the specified position by applying a transformation function.
    /// </summary>
    public static BoardState UpdateTokenAtPosition(this BoardState state, TokenPosition position, Func<TokenState, TokenState> updateFunc)
    {
        var newTokens = state.BoardTokenState.BoardTokenLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (newTokens.TryGetValue(position, out var token))
        {
            newTokens[position] = updateFunc(token);
        }
        return state.WithBoardTokenState(new BoardTokenState(newTokens));
    }

    private static Option<EmptyTokenState> GetLowestEmptyTokenFromColumn(this BoardState state, TokenColumn column)
        => state.BoardTokenState.BoardTokenLookup
            .Where(kvp => kvp.Key.Column.ColumnIndex == column.ColumnIndex)
            .OrderByDescending(kvp => kvp.Key.Row.RowIndex)
            .FirstOrDefault(kvp => kvp.Value is EmptyTokenState).Value is EmptyTokenState empty ? 
                Option<EmptyTokenState>.Some(empty) : 
                Option<EmptyTokenState>.None;
    
    // FIXME: reuse code from GetLowestEmptyTokenFromColumn
    private static Option<PreviewTokenState> GetLowestPreviewTokenFromColumn(this BoardState state, TokenColumn column)
        => state.BoardTokenState.BoardTokenLookup
            .Where(kvp => kvp.Key.Column.ColumnIndex == column.ColumnIndex)
            .OrderByDescending(kvp => kvp.Key.Row.RowIndex)
            .FirstOrDefault(kvp => kvp.Value is PreviewTokenState).Value is PreviewTokenState preview ? 
                Option<PreviewTokenState>.Some(preview) : 
                Option<PreviewTokenState>.None;

    /// <summary>
    /// Places a token in the specified column, filling the lowest available preview position.
    /// Uses the provided coloring strategy to determine the token color.
    /// </summary>
    private static BoardState PlaceTokenInColumn(this BoardState state, TokenColumn column, ColoringStrategy coloringStrategy)
    {
        if(state.CurrentGameStatus is not InProgress)
        {
            // Game is over, no more tokens can be placed
            return state;
        }

        var color = coloringStrategy(state.CurrentPlayer);
        
        // First check if there's a preview token (from hovering)
        var maybePreviewToken = state.GetLowestPreviewTokenFromColumn(column);
        return maybePreviewToken.Match(
            None: () => state,
            Some: previewToken => 
            {
                var position = previewToken.Position;
                var newState = state.UpdateTokenAtPosition(position, _ => PlacedTokenState.Place(previewToken, color));
                // Check win condition after placement
                var newStatus = newState.CheckGameStatus(position, color);
                return newState with { CurrentGameStatus = newStatus };
            });
    }

    private static BoardState PlacePreviewTokenInColumn(this BoardState state, TokenColumn column, ColoringStrategy coloringStrategy)
    {
        var color = coloringStrategy(state.CurrentPlayer);
        
        var maybeEmptyToken = state.GetLowestEmptyTokenFromColumn(column);

        return maybeEmptyToken.Match(
            // Column is full, return state unchanged
            None: () => state,
            Some: emptyToken => state.UpdateTokenAtPosition(emptyToken.Position, _ => PreviewTokenState.Create(emptyToken, color)));
    }

    private static BoardState ClearPreviewTokenFromColumn(this BoardState state, TokenColumn column)
    {
        var maybePreviewToken = state.GetLowestPreviewTokenFromColumn(column);

        return maybePreviewToken.Match(
            // Column is full, return state unchanged
            None: () => state,
            Some: previewToken => state.UpdateTokenAtPosition(previewToken.Position, _ => EmptyTokenState.Create(previewToken)));
    }

    /// <summary>
    /// Applies a state transition to the board state.
    /// Uses the board's own coloring strategy for token placement.
    /// </summary>
    public static BoardState ApplyTransition(this BoardState state, BoardStateTransition transition)
        => transition switch
        {
            PlaceToken placeToken => state.PlaceTokenInColumn(placeToken.Column, state.ColoringStrategy),
            PlacePreviewToken previewToken => state.PlacePreviewTokenInColumn(previewToken.Column, state.ColoringStrategy),
            ClearPreviewToken clearPreviewToken => state.ClearPreviewTokenFromColumn(clearPreviewToken.Column),
            SwitchPlayer => state.WithCurrentPlayer(BoardState.AdvancePlayer()),
            _ => throw new InvalidOperationException($"Unknown transition type: {transition.GetType()}")
        };

}
