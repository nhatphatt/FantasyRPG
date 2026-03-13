namespace FantasyRPG.Core.Components;

/// <summary>
/// Every possible state a fighter can occupy. The CombatSystem uses this
/// as an integer-indexed lookup — no strings, no allocations.
/// The enum ordering reflects priority for combat resolution:
/// higher values = higher-priority states during conflict.
/// </summary>
public enum FighterStateId : byte
{
    // ── Neutral / Movement ───────────────────────────────
    Idle = 0,
    Run = 1,
    JumpSquat = 2,
    Airborne = 3,
    Landing = 4,

    // ── Offense ──────────────────────────────────────────
    AttackStartup = 10,
    AttackActive = 11,
    AttackRecovery = 12,

    // ── Defense ──────────────────────────────────────────
    ParryWindow = 20,      // The sacred 3-frame perfect window
    ParrySuccess = 21,     // Parry confirmed — freeze + reflect
    Blocking = 22,         // Held block after parry window expires
    Blockstun = 23,        // Hit while blocking — reduced pushback, no damage

    // ── Movement Abilities ──────────────────────────────
    DashStartup = 30,
    DashActive = 31,       // i-frames enabled, hurtbox disabled
    DashRecovery = 32,

    // ── Hurt / Reactive ─────────────────────────────────
    Hitstun = 40,
    Knockback = 41,
    KnockedDown = 42,

    // ── Special ─────────────────────────────────────────
    UltimateStartup = 50,
    UltimateActive = 51,
    UltimateRecovery = 52
}
