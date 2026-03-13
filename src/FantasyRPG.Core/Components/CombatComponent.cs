namespace FantasyRPG.Core.Components;

/// <summary>
/// Core combat state for a fighter entity. Tracks the current action's
/// frame-data progression, parry windows, and invincibility.
///
/// This is a MUTABLE struct stored in a flat array by the World.
/// Systems read/write it by ref — zero copies, zero allocations.
///
/// Frame counter convention:
///   - Incremented once per Update tick (fixed 60fps).
///   - State transitions reset CurrentFrame to 0.
///   - Systems compare CurrentFrame against FrameData thresholds.
/// </summary>
public struct CombatComponent
{
    // ── Current State ────────────────────────────────────
    public FighterStateId State;

    /// <summary>
    /// Frame counter within the current state. Reset to 0 on state transition.
    /// Incremented by CombatSystem each tick. This is the single source of truth
    /// for "where am I in this action?"
    /// </summary>
    public int CurrentFrame;

    // ── Active Action Data ───────────────────────────────
    /// <summary>
    /// Frame data of the action currently being executed.
    /// Set when entering an attack/parry/dash state. Read-only during execution.
    /// </summary>
    public FrameData ActiveFrameData;

    /// <summary>
    /// Index into the character's move set array. Identifies WHICH attack
    /// (jab, heavy, aerial, special) without allocating a string or enum per hit.
    /// </summary>
    public int ActiveMoveIndex;

    // ── Parry / Defense ──────────────────────────────────
    /// <summary>
    /// Number of frames the parry window is open. Loaded from character config.
    /// Typical fighting game value: 3 frames (50ms at 60fps).
    /// </summary>
    public int ParryWindowFrames;

    /// <summary>True if the entity is currently invincible (dash i-frames, parry success freeze).</summary>
    public bool IsInvincible;

    // ── Hit Tracking ─────────────────────────────────────
    /// <summary>
    /// Bitfield or simple flag to prevent the same attack from hitting
    /// the same opponent multiple times during its active frames.
    /// Reset when entering a new attack state.
    /// </summary>
    public int HitConfirmMask;

    /// <summary>Facing direction. +1 = right, -1 = left. Used to flip hitbox offsets.</summary>
    public int FacingDirection;

    // ── Stun / Recovery ──────────────────────────────────
    /// <summary>Remaining stun frames (hitstun or blockstun). Decremented each tick.</summary>
    public int StunFramesRemaining;

    // ── Helper Methods (pure computation, no allocation) ─

    /// <summary>Resets frame counter and transitions to a new state.</summary>
    public void TransitionTo(FighterStateId newState)
    {
        State = newState;
        CurrentFrame = 0;
        HitConfirmMask = 0;
        IsInvincible = false;
    }

    /// <summary>Enters an attack state with associated frame data.</summary>
    public void BeginAttack(int moveIndex, in FrameData frameData)
    {
        ActiveMoveIndex = moveIndex;
        ActiveFrameData = frameData;
        TransitionTo(FighterStateId.AttackStartup);
    }

    /// <summary>Enters the parry window state.</summary>
    public void BeginParry(int windowFrames)
    {
        ParryWindowFrames = windowFrames;
        TransitionTo(FighterStateId.ParryWindow);
    }

    /// <summary>True if CurrentFrame is within the active hitbox window of the current attack.</summary>
    public readonly bool IsInActiveFrames =>
        State == FighterStateId.AttackActive;

    /// <summary>True if in the parry window (the sacred perfect-defense frames).</summary>
    public readonly bool IsInParryWindow =>
        State == FighterStateId.ParryWindow
        && CurrentFrame < ParryWindowFrames;

    /// <summary>True if currently blocking (held block after parry window expired).</summary>
    public readonly bool IsBlocking =>
        State == FighterStateId.Blocking;
}
