using Microsoft.Xna.Framework;

namespace FantasyRPG.Core.Engine.StateMachine;

/// <summary>
/// State contract for entity-level FSMs (Idle, Run, Attack, etc.).
/// TOwner is the entity or controller that owns this state machine.
/// All implementations must be pre-allocated — no allocations on transition.
/// </summary>
public interface IState<in TOwner>
{
    void Enter(TOwner owner);
    void Execute(TOwner owner, GameTime gameTime);
    void Exit(TOwner owner);
}
