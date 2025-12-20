using Board.Domain;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Board.IO.Services;

/// <summary>
/// Delegate for getting the current board state
/// </summary>
public delegate BoardState GetBoardStateDelegate();

/// <summary>
/// Delegate for updating the board state with a state transition
/// </summary>
public delegate void UpdateBoardStateDelegate(BoardStateTransition transition);

/// <summary>
/// Delegate for updating the board state with multiple transitions atomically
/// </summary>
public delegate void UpdateBoardStateBatchDelegate(params BoardStateTransition[] transitions);

/// <summary>
/// Delegate for getting a pre-filtered observable for a specific token
/// </summary>
public delegate IObservable<TokenState> GetTokenObservableDelegate(TokenPosition position);

/// <summary>
/// Thread-safe store for BoardState with Get and Update operations using explicit state transitions
/// </summary>
public record BoardStateStoreService(
    IObservable<BoardState> StateChanged,
    IObservable<Player> PlayerChanged,
    GetBoardStateDelegate GetBoardState,
    UpdateBoardStateDelegate UpdateBoardState,
    UpdateBoardStateBatchDelegate UpdateBoardStateBatch,
    GetTokenObservableDelegate GetTokenObservable);

/// <summary>
/// Implementation of thread-safe BoardState store using BehaviorSubject
/// </summary>
// TODO: Make this an interface of StateStore<T> and have BoardStateStore implement that
public sealed class BoardStateStore : IDisposable
{
    private readonly BehaviorSubject<BoardState> _stateSubject;
    private readonly AsyncLock _lock = new();

    public BoardStateStore(BoardState initialState)
    {
        _stateSubject = new BehaviorSubject<BoardState>(initialState);
    }

    /// <summary>
    /// Gets the current board state in a thread-safe manner
    /// </summary>
    public BoardState GetCurrentState()
    {
        lock (_lock)
        {
            return _stateSubject.Value;
        }
    }

    /// <summary>
    /// Creates a BoardStateStoreService with all delegates wired to this store.
    /// </summary>
    public BoardStateStoreService GetService() => new(
        StateChanged: _stateSubject.AsObservable(),
        PlayerChanged: PlayerChanged,
        GetBoardState: GetCurrentState,
        UpdateBoardState: UpdateBoardState,
        UpdateBoardStateBatch: UpdateBoardStateBatch,
        GetTokenObservable: GetTokenObservable
    );

    /// <summary>
    /// Updates the board state by applying a state transition.
    /// All game logic flows through explicit, type-safe transitions.
    /// </summary>
    public void UpdateBoardState(BoardStateTransition transition)
    {
        lock (_lock)
        {
            var currentState = _stateSubject.Value;
            var newState = currentState.ApplyTransition(transition);
            _stateSubject.OnNext(newState);
        }
    }

    //TODO: we should add semantics to the board state transitions types to indicate if they are batchable or not. 
    // We should prevent non-batchable transitions from being used in UpdateBoardStateBatch since I can't think of any valid use case for that.
    /// <summary>
    /// Updates the board state by applying multiple transitions atomically.
    /// Only emits a single state change after all transitions are applied.
    /// </summary>
    public void UpdateBoardStateBatch(params BoardStateTransition[] transitions)
    {
        lock (_lock)
        {
            var currentState = _stateSubject.Value;
            foreach (var transition in transitions)
            {
                currentState = currentState.ApplyTransition(transition);
            }
            _stateSubject.OnNext(currentState);
        }
    }

    /// <summary>
    /// Observable stream of player changes only
    /// </summary>
    public IObservable<Player> PlayerChanged =>
        _stateSubject
            .DistinctUntilChanged(state => state.CurrentPlayer)
            .Select(state => state.CurrentPlayer);

    /// <summary>
    /// Gets an observable that emits token state changes for a specific position.
    /// Emits the current state immediately, then only when that token changes.
    /// </summary>
    public IObservable<TokenState> GetTokenObservable(TokenPosition position)
        => _stateSubject
            .Select(state => state.BoardTokenState[position])
            .DistinctUntilChanged();

    public void Dispose()
    {
        _stateSubject.Dispose();
    }
}