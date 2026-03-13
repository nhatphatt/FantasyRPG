using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FantasyRPG.Core.GameStates;

/// <summary>
/// Manages game state transitions using a fixed-size array indexed by
/// <see cref="GameStateId"/>. Zero allocations during state changes.
/// </summary>
public sealed class GameStateManager
{
    private const int MaxStates = 8;
    private readonly IGameState?[] _states = new IGameState?[MaxStates];
    private IGameState? _currentState;
    private GameStateId _currentId;

    public GameStateId CurrentStateId => _currentId;

    /// <summary>
    /// Register a pre-allocated state. Call during LoadContent only.
    /// </summary>
    public void RegisterState(GameStateId id, IGameState state)
    {
        int index = (int)id;
        if (index < 0 || index >= MaxStates)
            throw new ArgumentOutOfRangeException(nameof(id));

        _states[index] = state;
    }

    /// <summary>
    /// Transition to a new state. Calls Exit() on current, Enter() on next.
    /// Zero allocations — states are pre-registered.
    /// </summary>
    public void ChangeState(GameStateId id)
    {
        int index = (int)id;
        IGameState? next = _states[index]
            ?? throw new InvalidOperationException($"State {id} not registered.");

        _currentState?.Exit();
        _currentId = id;
        _currentState = next;
        _currentState.Enter();
    }

    public void Update(GameTime gameTime)
    {
        _currentState?.Update(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        _currentState?.Draw(gameTime, spriteBatch);
    }
}
