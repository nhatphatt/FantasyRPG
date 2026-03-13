using System;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// The heart of the fighting game. Processes combat each frame in strict order:
///
///   1. Advance frame counters & manage state transitions (frame data FSM)
///   2. Activate/deactivate hitboxes based on frame data
///   3. Resolve hitbox↔hurtbox intersections for all entity pairs
///   4. Classify each intersection as HIT / BLOCKED / PARRIED
///   5. Apply damage, hitstun, knockback, or parry effects
///
/// This system is DETERMINISTIC: given identical input + state, output is identical.
/// Zero allocations. All results written to pre-existing component arrays.
/// </summary>
public static class CombatSystem
{
    // ── Pre-allocated result buffer (avoids allocation during resolution) ──
    private static readonly HitResult[] _hitResults = new HitResult[64];
    private static int _hitResultCount;

    // ═══════════════════════════════════════════════════════════════════
    //  PHASE 1: Advance Frame Counters — Called once per Update tick
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Steps each fighter's frame counter forward by 1 and handles
    /// automatic state transitions driven by frame data.
    /// </summary>
    public static void AdvanceFrames(
        Span<CombatComponent> combats,
        Span<HitboxComponent> hitboxes,
        Span<HurtboxComponent> hurtboxes,
        int entityCount)
    {
        for (int i = 0; i < entityCount; i++)
        {
            ref CombatComponent combat = ref combats[i];
            ref HitboxComponent hitbox = ref hitboxes[i];
            ref HurtboxComponent hurtbox = ref hurtboxes[i];

            combat.CurrentFrame++;

            switch (combat.State)
            {
                // ── Attack State Machine: Startup → Active → Recovery → Idle ──
                case FighterStateId.AttackStartup:
                    hitbox.IsActive = false;
                    if (combat.CurrentFrame >= combat.ActiveFrameData.StartupFrames)
                    {
                        combat.TransitionTo(FighterStateId.AttackActive);
                        ActivateHitbox(ref hitbox, ref combat);
                    }
                    break;

                case FighterStateId.AttackActive:
                    hitbox.IsActive = true;
                    if (combat.CurrentFrame >= combat.ActiveFrameData.ActiveFrames)
                    {
                        combat.TransitionTo(FighterStateId.AttackRecovery);
                        hitbox.IsActive = false;
                    }
                    break;

                case FighterStateId.AttackRecovery:
                    hitbox.IsActive = false;
                    if (combat.CurrentFrame >= combat.ActiveFrameData.RecoveryFrames)
                        combat.TransitionTo(FighterStateId.Idle);
                    break;

                // ── Parry Window → Blocking (if held) or Idle (if released) ──
                case FighterStateId.ParryWindow:
                    if (combat.CurrentFrame >= combat.ParryWindowFrames)
                        combat.TransitionTo(FighterStateId.Blocking);
                    break;

                // ── Parry Success (brief freeze) ─────────────────────────────
                case FighterStateId.ParrySuccess:
                    combat.IsInvincible = true;
                    if (combat.CurrentFrame >= ParryFreezeDuration)
                    {
                        combat.IsInvincible = false;
                        combat.TransitionTo(FighterStateId.Idle);
                    }
                    break;

                // ── Dash: Startup → Active (i-frames) → Recovery ─────────────
                case FighterStateId.DashStartup:
                    if (combat.CurrentFrame >= _dashStartupFrames)
                    {
                        combat.TransitionTo(FighterStateId.DashActive);
                        combat.IsInvincible = true;
                        hurtbox.IsActive = false; // i-frames: can't be hit
                    }
                    break;

                case FighterStateId.DashActive:
                    if (combat.CurrentFrame >= _dashActiveFrames)
                    {
                        combat.TransitionTo(FighterStateId.DashRecovery);
                        combat.IsInvincible = false;
                        hurtbox.IsActive = true;
                    }
                    break;

                case FighterStateId.DashRecovery:
                    if (combat.CurrentFrame >= _dashRecoveryFrames)
                        combat.TransitionTo(FighterStateId.Idle);
                    break;

                // ── Hitstun / Blockstun countdown ────────────────────────────
                case FighterStateId.Hitstun:
                case FighterStateId.Blockstun:
                    combat.StunFramesRemaining--;
                    if (combat.StunFramesRemaining <= 0)
                        combat.TransitionTo(FighterStateId.Idle);
                    break;
            }

            // Ensure hurtbox is active in all normal states (not i-frame states)
            if (combat.State != FighterStateId.DashActive
                && combat.State != FighterStateId.ParrySuccess)
            {
                hurtbox.IsActive = true;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PHASE 2: Resolve Hitbox ↔ Hurtbox Collisions
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// For every entity pair (A, B), tests if A's hitbox overlaps B's hurtbox.
    /// Classifies each intersection and writes results to the pre-allocated buffer.
    ///
    /// Collision matrix (who can hit whom):
    ///   - A fighter's hitbox CANNOT hit their own hurtbox.
    ///   - A fighter's hitbox CAN hit any other fighter's hurtbox.
    ///   - Already-hit targets (HitConfirmMask) are skipped.
    /// </summary>
    public static void ResolveCollisions(
        Span<CombatComponent> combats,
        Span<HitboxComponent> hitboxes,
        Span<HurtboxComponent> hurtboxes,
        Span<TransformComponent> transforms,
        int entityCount)
    {
        _hitResultCount = 0;

        for (int attacker = 0; attacker < entityCount; attacker++)
        {
            ref CombatComponent atkCombat = ref combats[attacker];
            ref HitboxComponent atkHitbox = ref hitboxes[attacker];
            ref TransformComponent atkTransform = ref transforms[attacker];

            // Skip if this entity has no active hitbox
            if (!atkHitbox.IsActive)
                continue;

            // Resolve hitbox to world space
            AABB atkAABB = atkHitbox.ToWorldAABB(
                in atkTransform.Position,
                atkCombat.FacingDirection);

            for (int defender = 0; defender < entityCount; defender++)
            {
                // Can't hit yourself
                if (attacker == defender)
                    continue;

                // Already hit this target during this attack?
                if ((atkCombat.HitConfirmMask & (1 << defender)) != 0)
                    continue;

                ref CombatComponent defCombat = ref combats[defender];
                ref HurtboxComponent defHurtbox = ref hurtboxes[defender];
                ref TransformComponent defTransform = ref transforms[defender];

                // Skip if defender's hurtbox is inactive (i-frames)
                if (!defHurtbox.IsActive)
                    continue;

                // Skip if defender is invincible
                if (defCombat.IsInvincible)
                    continue;

                // Resolve hurtbox to world space
                AABB defAABB = defHurtbox.ToWorldAABB(in defTransform.Position);

                // ── THE INTERSECTION TEST ────────────────────────────
                if (!atkAABB.Intersects(in defAABB))
                    continue;

                // ═══════════════════════════════════════════════════════
                //  CLASSIFICATION: Hit vs Block vs Parry
                // ═══════════════════════════════════════════════════════
                HitResultType resultType = ClassifyHit(in defCombat);

                // Mark hit confirmed so we don't double-hit
                atkCombat.HitConfirmMask |= (1 << defender);

                // Write result to pre-allocated buffer
                if (_hitResultCount < _hitResults.Length)
                {
                    _hitResults[_hitResultCount++] = new HitResult(
                        attackerIndex: attacker,
                        defenderIndex: defender,
                        resultType: resultType,
                        frameData: atkCombat.ActiveFrameData);
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PHASE 3: Apply Results
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads the pre-allocated hit result buffer and applies damage,
    /// hitstun, blockstun, knockback, or parry effects.
    /// </summary>
    public static void ApplyResults(
        Span<CombatComponent> combats,
        Span<HealthComponent> healths,
        Span<TransformComponent> transforms)
    {
        for (int i = 0; i < _hitResultCount; i++)
        {
            ref readonly HitResult result = ref _hitResults[i];
            ref CombatComponent defCombat = ref combats[result.DefenderIndex];
            ref CombatComponent atkCombat = ref combats[result.AttackerIndex];

            switch (result.ResultType)
            {
                // ────────────────────────────────────────────────────
                //  CLEAN HIT — Full damage + hitstun + knockback
                // ────────────────────────────────────────────────────
                case HitResultType.Hit:
                    ref HealthComponent defHealth = ref healths[result.DefenderIndex];
                    defHealth.Current -= result.FrameData.Damage;

                    defCombat.StunFramesRemaining = result.FrameData.HitstunFrames;
                    defCombat.TransitionTo(FighterStateId.Hitstun);

                    ApplyKnockback(
                        ref transforms[result.DefenderIndex],
                        in result.FrameData,
                        atkCombat.FacingDirection);
                    break;

                // ────────────────────────────────────────────────────
                //  BLOCKED — No damage, reduced pushback, blockstun
                // ────────────────────────────────────────────────────
                case HitResultType.Blocked:
                    defCombat.StunFramesRemaining = result.FrameData.BlockstunFrames;
                    defCombat.TransitionTo(FighterStateId.Blockstun);

                    // Chip pushback (1/4 of normal knockback, horizontal only)
                    ref TransformComponent defTransBlocked = ref transforms[result.DefenderIndex];
                    defTransBlocked.Velocity.X +=
                        result.FrameData.KnockbackForce * 0.25f * atkCombat.FacingDirection;
                    break;

                // ────────────────────────────────────────────────────
                //  PERFECT PARRY — No damage, attacker gets stunned!
                // ────────────────────────────────────────────────────
                case HitResultType.Parried:
                    // Defender: enter parry success (brief invincible freeze)
                    defCombat.TransitionTo(FighterStateId.ParrySuccess);
                    defCombat.IsInvincible = true;

                    // Attacker: punished — forced into hitstun
                    atkCombat.StunFramesRemaining = ParryPunishFrames;
                    atkCombat.TransitionTo(FighterStateId.Hitstun);
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CLASSIFICATION LOGIC — The core of the Parry system
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Determines the outcome when an attacker's hitbox intersects
    /// a defender's hurtbox. Priority order:
    ///
    ///   1. PARRY  — Defender is in ParryWindow state AND within parry frames
    ///   2. BLOCK  — Defender is in Blocking state (held block)
    ///   3. HIT    — Everything else (default)
    ///
    /// This is the SINGLE point of truth for combat outcome classification.
    /// Pure function — reads only the defender's CombatComponent, no side effects.
    /// </summary>
    private static HitResultType ClassifyHit(in CombatComponent defender)
    {
        // ── PERFECT PARRY CHECK ──────────────────────────────────
        // The defender pressed Block and is still within the sacred
        // parry window frames (typically 3 frames = 50ms at 60fps).
        // This is the highest-priority defensive outcome.
        if (defender.IsInParryWindow)
            return HitResultType.Parried;

        // ── BLOCK CHECK ──────────────────────────────────────────
        // The defender is holding Block but the parry window has expired.
        // They still block the attack (no damage) but take blockstun.
        if (defender.IsBlocking)
            return HitResultType.Blocked;

        // ── CLEAN HIT ────────────────────────────────────────────
        // Default outcome. Full damage, full hitstun, full knockback.
        return HitResultType.Hit;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private static void ActivateHitbox(ref HitboxComponent hitbox, ref CombatComponent combat)
    {
        if (combat.ActiveMoveIndex >= 0
            && hitbox.MoveHitboxes is not null
            && combat.ActiveMoveIndex < hitbox.MoveHitboxes.Length)
        {
            ref readonly HitboxDefinition def = ref hitbox.MoveHitboxes[combat.ActiveMoveIndex];
            hitbox.LocalOffset = def.Offset;
            hitbox.HalfSize = def.HalfSize;
        }

        hitbox.IsActive = true;
    }

    private static void ApplyKnockback(
        ref TransformComponent transform,
        in FrameData frameData,
        int attackerFacing)
    {
        float rad = frameData.KnockbackAngle * (MathF.PI / 180f);
        transform.Velocity.X += MathF.Cos(rad) * frameData.KnockbackForce * attackerFacing;
        transform.Velocity.Y -= MathF.Sin(rad) * frameData.KnockbackForce; // Y-up = negative
    }

    // ── Tuning Constants ────────────────────────────────────────────────
    private const int ParryFreezeDuration = 12;  // frames of parry success freeze
    private const int ParryPunishFrames = 20;    // frames the attacker is stunned on parry

    // ── Per-Character Dash Tuning (set via ConfigureDash) ────────────
    private static int _dashStartupFrames = 3;
    private static int _dashActiveFrames = 12;   // i-frame window
    private static int _dashRecoveryFrames = 5;

    /// <summary>
    /// Configures dash frame data. Call once during character setup.
    /// </summary>
    public static void ConfigureDash(int startup, int active, int recovery)
    {
        _dashStartupFrames = startup;
        _dashActiveFrames = active;
        _dashRecoveryFrames = recovery;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RESULT TYPES — Zero-allocation value types
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// The three possible outcomes when an attack connects.
/// </summary>
public enum HitResultType : byte
{
    Hit = 0,      // Clean hit — full damage
    Blocked = 1,  // Blocked — no damage, blockstun + chip pushback
    Parried = 2   // Perfect parry — no damage, ATTACKER gets punished
}

/// <summary>
/// A single combat interaction result. Stored in pre-allocated buffer.
/// Read by ApplyResults() to mutate components.
/// </summary>
public readonly struct HitResult
{
    public readonly int AttackerIndex;
    public readonly int DefenderIndex;
    public readonly HitResultType ResultType;
    public readonly FrameData FrameData;

    public HitResult(int attackerIndex, int defenderIndex, HitResultType resultType, FrameData frameData)
    {
        AttackerIndex = attackerIndex;
        DefenderIndex = defenderIndex;
        ResultType = resultType;
        FrameData = frameData;
    }
}
