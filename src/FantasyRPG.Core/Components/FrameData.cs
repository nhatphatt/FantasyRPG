namespace FantasyRPG.Core.Components;

/// <summary>
/// Immutable frame data definition for a single combat action.
/// Loaded from JSON at startup. NEVER mutated during gameplay.
///
/// Fighting game anatomy of an attack:
///   [Startup]  →  [Active]  →  [Recovery]
///    Can't hit     Hitbox ON    Vulnerable, no hitbox
///
/// At 60fps, 1 frame = 16.67ms.
/// A "3f startup, 4f active, 8f recovery" attack is 15 frames total (250ms).
/// </summary>
public readonly struct FrameData
{
    /// <summary>Frames before the hitbox activates. Fighter is committed but harmless.</summary>
    public readonly int StartupFrames;

    /// <summary>Frames the hitbox is active and can deal damage.</summary>
    public readonly int ActiveFrames;

    /// <summary>Frames after active ends. Fighter is vulnerable, hitbox is off.</summary>
    public readonly int RecoveryFrames;

    /// <summary>Base damage dealt if the hitbox connects during active frames.</summary>
    public readonly float Damage;

    /// <summary>Hitstun frames inflicted on the victim. Determines combo potential.</summary>
    public readonly int HitstunFrames;

    /// <summary>Blockstun frames inflicted if the defender is blocking.</summary>
    public readonly int BlockstunFrames;

    /// <summary>Knockback force applied as a pixel velocity impulse.</summary>
    public readonly float KnockbackForce;

    /// <summary>Knockback angle in degrees (0 = horizontal, 90 = straight up).</summary>
    public readonly float KnockbackAngle;

    /// <summary>Total duration of this action in frames.</summary>
    public readonly int TotalFrames;

    public FrameData(
        int startupFrames,
        int activeFrames,
        int recoveryFrames,
        float damage,
        int hitstunFrames,
        int blockstunFrames,
        float knockbackForce,
        float knockbackAngle)
    {
        StartupFrames = startupFrames;
        ActiveFrames = activeFrames;
        RecoveryFrames = recoveryFrames;
        Damage = damage;
        HitstunFrames = hitstunFrames;
        BlockstunFrames = blockstunFrames;
        KnockbackForce = knockbackForce;
        KnockbackAngle = knockbackAngle;
        TotalFrames = startupFrames + activeFrames + recoveryFrames;
    }
}
