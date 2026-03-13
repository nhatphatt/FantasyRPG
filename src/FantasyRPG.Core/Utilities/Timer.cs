namespace FantasyRPG.Core.Utilities;

/// <summary>
/// Lightweight countdown timer. A struct — zero heap allocation.
/// Use for cooldowns, invincibility frames, animation delays, etc.
/// </summary>
public struct Timer
{
    public float Duration;
    public float Remaining;

    public readonly bool IsFinished => Remaining <= 0f;
    public readonly float Progress => Duration > 0f ? 1f - (Remaining / Duration) : 1f;

    public Timer(float duration)
    {
        Duration = duration;
        Remaining = duration;
    }

    /// <summary>
    /// Ticks the timer down by deltaTime. Returns true when it hits zero
    /// (fires exactly once per reset cycle).
    /// </summary>
    public bool Tick(float deltaTime)
    {
        if (Remaining <= 0f)
            return false;

        Remaining -= deltaTime;
        return Remaining <= 0f;
    }

    public void Reset()
    {
        Remaining = Duration;
    }

    public void Reset(float newDuration)
    {
        Duration = newDuration;
        Remaining = newDuration;
    }
}
