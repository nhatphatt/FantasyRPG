using Microsoft.Xna.Framework;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Data;

/// <summary>
/// Pre-defined frame data, hitbox geometry, animation clips, and physics
/// tuning for "The Wizard" archetype — a ranged Glass Cannon.
///
/// All data is statically allocated. Zero runtime allocation.
/// Move indices are integer constants for direct array indexing.
///
/// Sprite sheet layout (5 rows × 8 columns):
///   Row 0, cols 0-7: Idle         — 8 frames (standing bob with staff)
///   Row 1, cols 0-3: Dash/Flight  — 4 frames (forward lean over staff)
///   Row 1, cols 5-7: Jump/Fall    — 3 frames (airborne, arms raised)
///   Row 2, cols 4-7: AttackJab    — 4 frames (staff thrust, blue energy)
///   Row 3, cols 0-3: AttackHeavy  — 4 frames (channeled blast, big blue beam)
///   Row 3, cols 4-5: Teleport     — 2 frames (dissolve into particles)
///   Row 4, cols 4-5: Block        — 2 frames (energy shield conjure)
///   Row 4, cols 6-7: Hitstun      — 2 frames (recoil, staggered)
///
/// The Wizard's identity:
///   - Very fast staff jab for close pokes (14 frames total)
///   - Devastating ranged blast with long-range hitbox (38 frames total)
///   - Teleport dash with extra-long i-frames (28 frames total)
///   - 4-frame parry window — tighter than the Knight (20 frames total)
///   - Low HP (glass cannon): 70 HP vs Knight's 100 HP
/// </summary>
public static class WizardData
{
    // ── Sprite Sheet Grid ────────────────────────────────────────────
    public const int SheetColumns = 8;
    public const int SheetRows = 5;

    // ── Move Indices (used by InputBuffer → CombatComponent) ─────────
    public const int MoveJab = 0;
    public const int MoveHeavy = 1;
    public const int MoveCount = 2;

    // ── Dash Tuning (frames) — Teleport: longer i-frames ─────────────
    public const int DashStartupFrames = 2;
    public const int DashActiveFrames = 22;    // generous i-frame teleport
    public const int DashRecoveryFrames = 4;
    public const int DashTotalFrames = DashStartupFrames + DashActiveFrames + DashRecoveryFrames; // 28

    // ── Parry Tuning (frames) — tighter window, glass cannon risk ────
    public const int ParryWindowFrames = 4;
    public const int ParryTotalDuration = 20;

    // ── Physics Override ─────────────────────────────────────────────
    public const float DashSpeed = 1400f;      // teleport is fast
    public const float WizardHP = 70f;         // glass cannon

    /// <summary>
    /// Returns the Wizard's complete move set as a pre-allocated FrameData array.
    /// Index 0 = Staff Jab, Index 1 = Heavy Blast.
    /// Call once during PlayState construction. Never call at runtime.
    /// </summary>
    public static FrameData[] CreateMoveSet()
    {
        return new FrameData[]
        {
            // ── Staff Jab (Key 1) ───────────────────────────────────
            // Very fast poke. 3f startup → 3f active → 8f recovery = 14f total.
            // Low damage but extremely safe. Zoning tool.
            new(
                startupFrames:  3,
                activeFrames:   3,
                recoveryFrames: 8,
                damage:         6f,
                hitstunFrames:  10,
                blockstunFrames: 4,
                knockbackForce: 100f,
                knockbackAngle: 15f),

            // ── Heavy Blast (Key 2) ─────────────────────────────────
            // Channeled ranged attack. 10f startup → 8f active → 20f recovery = 38f total.
            // High damage, massive range (hitbox offset 150px+). Punishable up close.
            new(
                startupFrames:  10,
                activeFrames:   8,
                recoveryFrames: 20,
                damage:         28f,
                hitstunFrames:  30,
                blockstunFrames: 16,
                knockbackForce: 350f,
                knockbackAngle: 35f),
        };
    }

    /// <summary>
    /// Returns the Wizard's animation clip table, indexed by (int)AnimationId.
    /// Maps the 5×8 sprite sheet to all combat states.
    /// Call once during PlayState construction.
    /// </summary>
    public static AnimationData[] CreateAnimations()
    {
        var anims = new AnimationData[(int)AnimationId.Count];

        // Row 0, cols 0-7: Idle — 8 frames, looping, gentle staff bob
        anims[(int)AnimationId.Idle] = new(row: 0, startFrame: 0, frameCount: 8, frameDuration: 0.12f, looping: true);

        // Row 0, cols 0-3: Run — reuse first 4 idle frames as walk cycle
        anims[(int)AnimationId.Run] = new(row: 0, startFrame: 0, frameCount: 4, frameDuration: 0.08f, looping: true);

        // Row 1, cols 5-7: Jump / Fall — 3 frames, non-looping
        anims[(int)AnimationId.Jump] = new(row: 1, startFrame: 5, frameCount: 3, frameDuration: 0.12f, looping: false);
        anims[(int)AnimationId.Fall] = new(row: 1, startFrame: 5, frameCount: 3, frameDuration: 0.12f, looping: false);

        // Row 2, cols 4-7: AttackJab — staff thrust with blue energy (4 frames)
        //   Jab: 14 combat frames / 60fps = 0.233s → 0.233 / 4 ≈ 0.058s per anim frame
        anims[(int)AnimationId.AttackJab] = new(row: 2, startFrame: 4, frameCount: 4, frameDuration: 0.058f, looping: false);

        // Row 3, cols 0-3: AttackHeavy — channeled blast (4 frames)
        //   Heavy: 38 combat frames / 60fps = 0.633s → 0.633 / 4 ≈ 0.158s per anim frame
        anims[(int)AnimationId.AttackHeavy] = new(row: 3, startFrame: 0, frameCount: 4, frameDuration: 0.158f, looping: false);

        // Row 1, cols 0-3: Dash/Teleport — forward flight lean (4 frames, looping)
        anims[(int)AnimationId.Dash] = new(row: 1, startFrame: 0, frameCount: 4, frameDuration: 0.07f, looping: true);

        // Row 4, cols 4-5: Block — energy shield conjure (2 frames, hold last)
        anims[(int)AnimationId.Block] = new(row: 4, startFrame: 4, frameCount: 2, frameDuration: 0.06f, looping: false);

        // Row 4, cols 4-5: Parry — same energy shield (consistent with block)
        anims[(int)AnimationId.Parry] = new(row: 4, startFrame: 4, frameCount: 2, frameDuration: 0.04f, looping: false);

        // Row 4, cols 6-7: Hitstun — recoil stagger (2 frames)
        anims[(int)AnimationId.Hitstun] = new(row: 4, startFrame: 6, frameCount: 2, frameDuration: 0.10f, looping: false);

        return anims;
    }

    /// <summary>
    /// Returns the Wizard's hitbox shapes per move. Index matches move index.
    /// Jab is close-range. Heavy Blast has massive forward offset (ranged).
    /// Call once during PlayState construction.
    /// </summary>
    public static HitboxDefinition[] CreateHitboxDefs()
    {
        return new HitboxDefinition[]
        {
            // ── Staff Jab Hitbox ────────────────────────────────────
            // Close range poke. Offset 24px forward, small area.
            new(
                offset:   new Vector2(24f, -10f),
                halfSize: new Vector2(14f, 10f)),

            // ── Heavy Blast Hitbox ──────────────────────────────────
            // RANGED: Offset 160px forward — the fireball/explosion
            // detonates far ahead. HalfSize 30×16 = 60×32 px impact zone.
            new(
                offset:   new Vector2(160f, -8f),
                halfSize: new Vector2(30f, 16f)),
        };
    }
}
