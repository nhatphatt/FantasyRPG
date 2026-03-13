using Microsoft.Xna.Framework;

namespace FantasyRPG.Core.Engine.StateMachine;

/// <summary>
/// Generic finite state machine for entities. Transitions are instant,
/// zero-allocation, and type-safe. States are pre-allocated externally
/// and passed in via <see cref="ChangeState"/>.
/// </summary>
public sealed class StateMachine<TOwner>
{
    private readonly TOwner _owner;
    private IState<TOwner>? _currentState;
    private IState<TOwner>? _previousState;

    public IState<TOwner>? CurrentState => _currentState;
    public IState<TOwner>? PreviousState => _previousState;

    public StateMachine(TOwner owner)
    {
        _owner = owner;
    }

    public void ChangeState(IState<TOwner> newState)
    {
        if (ReferenceEquals(_currentState, newState))
            return;

        _previousState = _currentState;
        _currentState?.Exit(_owner);
        _currentState = newState;
        _currentState.Enter(_owner);
    }

    public void Update(GameTime gameTime)
    {
        _currentState?.Execute(_owner, gameTime);
    }

    /// <summary>
    /// Reverts to the previous state. Useful for temporary states like Hurt → Idle.
    /// </summary>
    public void RevertToPreviousState()
    {
        if (_previousState is not null)
            ChangeState(_previousState);
    }
}
