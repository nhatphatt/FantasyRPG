using Microsoft.Xna.Framework;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Data;

/// <summary>
/// Pre-defined frame data, hitbox geometry, animation clips, and physics
/// tuning for "The Knight" archetype — the first playable character.
///
/// All data is statically allocated. Zero runtime allocation.
/// Move indices are integer constants for direct array indexing.
///
/// Sprite sheet layout (4 rows × 8 columns):
///   Row 0, cols 0-3: Idle         — 4 frames (standing breath cycle)
///   Row 0, cols 4-7: Dash         — 4 frames (walking with shield)
///   Row 1, cols 0-5: Run          — 6 frames (full locomotion)
///   Row 2, cols 0-3: Jump / Fall  — 4 frames (also used as AttackJab fallback)
///   Row 3, cols 0-3: AttackHeavy  — 4 frames (sword slash)
///   Row 3, col  5:   Block/Parry  — 1 frame  (shield up)
///   Row 3, col  7:   Hitstun      — 1 frame  (hit reaction)
///
/// The Knight's identity:
///   - Fast jab for close-range pokes (18 frames total)
///   - Massive heavy slash with huge commitment (43 frames total)
///   - Quick dash with generous i-frames (20 frames total)
///   - 6-frame parry window for reads (25 frames total)
/// </summary>
public static class KnightData
{
    // ── Sprite Sheet Grid ────────────────────────────────────────────
    public const int SheetColumns = 8;
    public const int SheetRows = 4;

    // ── Move Indices (used by InputBuffer → CombatComponent) ─────────
    public const int MoveJab = 0;
    public const int MoveHeavy = 1;
    public const int MoveCount = 2;

    // ── Dash Tuning (frames) ─────────────────────────────────────────
    public const int DashStartupFrames = 3;
    public const int DashActiveFrames = 20;   // i-frame window
    public const int DashRecoveryFrames = 5;
    public const int DashTotalFrames = DashStartupFrames + DashActiveFrames + DashRecoveryFrames; // 28

    // ── Parry Tuning (frames) ────────────────────────────────────────
    public const int ParryWindowFrames = 6;   // frames 1-6 are the sacred window
    public const int ParryTotalDuration = 25; // total commitment if nothing connects

    // ── Physics Override ─────────────────────────────────────────────
    public const float DashSpeed = 1260f;     // 3× dash distance

    /// <summary>
    /// Returns the Knight's complete move set as a pre-allocated FrameData array.
    /// Index 0 = Jab, Index 1 = Heavy Slash.
    /// Call once during PlayState construction. Never call at runtime.
    /// </summary>
    public static FrameData[] CreateMoveSet()
    {
        return new FrameData[]
        {
            // ── Jab (Key Z) ─────────────────────────────────────────
            // Fast poke. 4f startup → 4f active → 10f recovery = 18f total.
            // Low damage, low hitstun. Safe on whiff if spaced well.
            new(
                startupFrames:  4,
                activeFrames:   4,
                recoveryFrames: 10,
                damage:         8f,
                hitstunFrames:  12,
                blockstunFrames: 6,
                knockbackForce: 120f,
                knockbackAngle: 20f),

            // ── Heavy Slash (Key A) ─────────────────────────────────
            // Big commitment. 15f startup → 8f active → 20f recovery = 43f total.
            // High damage, high hitstun. Extremely punishable on whiff.
            new(
                startupFrames:  15,
                activeFrames:   8,
                recoveryFrames: 20,
                damage:         24f,
                hitstunFrames:  28,
                blockstunFrames: 14,
                knockbackForce: 300f,
                knockbackAngle: 40f),
        };
    }

    /// <summary>
    /// Returns the Knight's animation clip table, indexed by (int)AnimationId.
    /// Maps the 4×8 sprite sheet to all combat states.
    /// Call once during PlayState construction.
    /// </summary>
    public static AnimationData[] CreateAnimations()
    {
        var anims = new AnimationData[(int)AnimationId.Count];

        // Row 0, cols 0-3: Idle — 4 frames, looping, gentle breathing pace
        anims[(int)AnimationId.Idle] = new(row: 0, startFrame: 0, frameCount: 4, frameDuration: 0.15f, looping: true);

        // Row 1, cols 0-3: Run — 4 frames, looping, fast cycle
        anims[(int)AnimationId.Run] = new(row: 1, startFrame: 0, frameCount: 4, frameDuration: 0.10f, looping: true);

        // Row 1, cols 4-7: Jump / Fall — 4 frames, non-looping
        anims[(int)AnimationId.Jump] = new(row: 1, startFrame: 4, frameCount: 4, frameDuration: 0.12f, looping: false);
        anims[(int)AnimationId.Fall] = new(row: 1, startFrame: 4, frameCount: 4, frameDuration: 0.12f, looping: false);

        // Row 2, cols 0-4: AttackJab — overhead sword swing (5 frames incl. recovery)
        //   Jab:   18 combat frames / 60fps = 0.3s   → 0.3 / 5 = 0.06s per anim frame
        anims[(int)AnimationId.AttackJab] = new(row: 2, startFrame: 0, frameCount: 5, frameDuration: 0.06f, looping: false);

        // Row 2, cols 0-4: AttackHeavy — same overhead swing, slower
        //   Heavy: 43 combat frames / 60fps = 0.717s  → 0.717 / 5 ≈ 0.143s per anim frame
        anims[(int)AnimationId.AttackHeavy] = new(row: 2, startFrame: 0, frameCount: 5, frameDuration: 0.143f, looping: false);

        // Row 2, cols 5-7: Dash — forward lunge (3 frames, looping during dash)
        anims[(int)AnimationId.Dash] = new(row: 2, startFrame: 5, frameCount: 3, frameDuration: 0.11f, looping: true);

        // Row 3, cols 3-6: Block — shield brace (4 frames, hold last)
        anims[(int)AnimationId.Block] = new(row: 3, startFrame: 3, frameCount: 4, frameDuration: 0.06f, looping: false);

        // Row 3, cols 3-6: Parry — same shield brace (consistent with block)
        anims[(int)AnimationId.Parry] = new(row: 3, startFrame: 3, frameCount: 4, frameDuration: 0.04f, looping: false);

        // Row 3, col 7: Hitstun — hit reaction (single frame)
        anims[(int)AnimationId.Hitstun] = new(row: 3, startFrame: 7, frameCount: 1, frameDuration: 0.10f, looping: false);

        return anims;
    }

    /// <summary>
    /// Returns the Knight's hitbox shapes per move. Index matches move index.
    /// Deliberately oversized (25×25 / 35×25 half-extents) so the red debug
    /// rectangles are highly visible during gray-box testing.
    /// Call once during PlayState construction.
    /// </summary>
    public static HitboxDefinition[] CreateHitboxDefs()
    {
        return new HitboxDefinition[]
        {
            // ── Jab Hitbox ──────────────────────────────────────────
            // Offset 28px forward from entity center so the hitbox
            // spawns clearly in front. HalfSize 18×14 = 36×28 px rect.
            // Left edge at entity.X + 10 when facing right (no body overlap).
            new(
                offset:   new Vector2(28f, -12f),
                halfSize: new Vector2(18f, 14f)),

            // ── Heavy Slash Hitbox ──────────────────────────────────
            // Offset 38px forward — the big sword swing reaches far.
            // HalfSize 25×18 = 50×36 px rect. Very visible red box.
            // Left edge at entity.X + 13 when facing right.
            new(
                offset:   new Vector2(38f, -14f),
                halfSize: new Vector2(25f, 18f)),
        };
    }
}
