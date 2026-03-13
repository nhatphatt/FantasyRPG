using System;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// Bridges the gap between PhysicsSystem (which sets IsGrounded) and
/// CombatSystem (which tracks FighterStateId).
///
/// CRITICAL RULE: This system ONLY transitions Airborne → Idle on landing.
/// It must NEVER override action states (Attacking, Dashing, Parrying, Hitstun).
/// Those states own their own lifecycle and return to Idle when their frame
/// data expires — the CombatSystem handles that.
/// </summary>
public static class GroundingSyncSystem
{
    /// <summary>
    /// Returns true if the given state is a "free movement" state that
    /// should respond to grounding changes. ALL other states are owned
    /// by the CombatSystem and must NOT be overridden.
    /// </summary>
    private static bool IsMovementState(FighterStateId state)
    {
        return state == FighterStateId.Idle
            || state == FighterStateId.Run
            || state == FighterStateId.Airborne
            || state == FighterStateId.JumpSquat
            || state == FighterStateId.Landing;
    }

    public static void Sync(
        Span<TransformComponent> transforms,
        Span<CombatComponent> combats,
        int entityCount)
    {
        for (int i = 0; i < entityCount; i++)
        {
            ref TransformComponent transform = ref transforms[i];
            ref CombatComponent combat = ref combats[i];

            // ── SKIP: Combat-owned states must NEVER be overridden ────
            // Attacking, Dashing, Parrying, Blocking, Hitstun, etc.
            // These states have their own frame-data-driven lifecycle
            // managed exclusively by CombatSystem.AdvanceFrames().
            if (!IsMovementState(combat.State))
                continue;

            // ── LANDING: Physics grounded + Combat airborne → Idle ────
            if (transform.IsGrounded && combat.State == FighterStateId.Airborne)
            {
                combat.TransitionTo(FighterStateId.Idle);
            }

            // ── FALLING OFF LEDGE: Not grounded + ground state → Airborne
            if (!transform.IsGrounded)
            {
                if (combat.State == FighterStateId.Idle
                    || combat.State == FighterStateId.Run)
                {
                    combat.TransitionTo(FighterStateId.Airborne);
                }
            }
        }
    }
}
