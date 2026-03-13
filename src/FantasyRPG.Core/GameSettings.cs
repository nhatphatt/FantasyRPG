using Microsoft.Xna.Framework;

namespace FantasyRPG.Core;

/// <summary>
/// Immutable game-wide constants. Defined once, referenced everywhere.
/// Modify these values to change the game's base configuration.
/// </summary>
public static class GameSettings
{
    // ── Virtual (internal) resolution — the "pixel art canvas" ────────
    public const int VirtualWidth = 480;
    public const int VirtualHeight = 270;

    // ── Default window size (virtual × scale factor) ─────────────────
    public const int DefaultScale = 4;
    public const int WindowWidth = VirtualWidth * DefaultScale;   // 1920
    public const int WindowHeight = VirtualHeight * DefaultScale; // 1080

    // ── Timing ───────────────────────────────────────────────────────
    public const int TargetFps = 60;

    // ── Rendering ────────────────────────────────────────────────────
    public static readonly Color ClearColor = new(24, 20, 37); // Dark purple

    // ── ECS limits (pre-allocation sizing) ───────────────────────────
    public const int MaxEntities = 2048;
    public const int MaxProjectiles = 512;
    public const int MaxParticles = 1024;

    // ── Physics ──────────────────────────────────────────────────────
    public const float Gravity = 980f;       // pixels/s²
    public const int TileSize = 16;          // pixels

    // ── Fighting Game: Combat Tuning ─────────────────────────────────
    public const int MaxFighters = 8;        // Max simultaneous fighters
    public const int ParryWindowFrames = 3;  // 50ms at 60fps — the sacred window
    public const int InputBufferFrames = 8;  // 133ms of input leniency
    public const int HitFreezeFrames = 4;    // Brief hitstop on clean hit
    public const int ParryFreezeFrames = 12; // Parry success dramatic pause
    public const int ParryPunishFrames = 20; // Attacker stun on parry
    public const float BlockKnockbackScale = 0.25f;
}
