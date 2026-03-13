namespace FantasyRPG.Core.Components;

/// <summary>
/// Animation playback state for a fighter entity.
/// Tracks which animation is playing, current frame, and elapsed time.
///
/// The AnimationSystem reads FighterStateId from CombatComponent to
/// auto-select the correct AnimationId, then advances frames here.
///
/// The animation lookup table (AnimationData[]) is stored as a reference —
/// shared across all entities of the same character type. Set once at spawn.
///
/// Zero-allocation: all fields are value types except the lookup array reference.
/// </summary>
public struct AnimatorComponent
{
    /// <summary>
    /// Pre-allocated animation definition table. Indexed by (int)AnimationId.
    /// Shared reference across all entities of same character type.
    /// Set once during spawn. Array length = (int)AnimationId.Count.
    /// </summary>
    public AnimationData[] Animations;

    /// <summary>Currently playing animation.</summary>
    public AnimationId CurrentAnimation;

    /// <summary>Current frame index within the animation (0-based, relative to StartFrame).</summary>
    public int CurrentFrame;

    /// <summary>Accumulated time on this frame. When >= FrameDuration, advance frame.</summary>
    public float FrameTimer;

    /// <summary>True if the animation has reached its last frame and is not looping.</summary>
    public bool IsFinished;

    /// <summary>
    /// Switches to a new animation. Resets frame counter and timer.
    /// No-ops if already playing the same animation (prevents restart flicker).
    /// </summary>
    public void Play(AnimationId id)
    {
        if (CurrentAnimation == id && !IsFinished)
            return;

        CurrentAnimation = id;
        CurrentFrame = 0;
        FrameTimer = 0f;
        IsFinished = false;
    }

    /// <summary>
    /// Force-plays an animation even if it's the same one (restarts from frame 0).
    /// Use for attacks that must restart on re-press.
    /// </summary>
    public void ForcePlay(AnimationId id)
    {
        CurrentAnimation = id;
        CurrentFrame = 0;
        FrameTimer = 0f;
        IsFinished = false;
    }
}
