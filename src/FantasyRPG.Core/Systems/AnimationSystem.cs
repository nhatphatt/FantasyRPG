using System;
using Microsoft.Xna.Framework;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// Drives sprite animation for all entities. Two responsibilities:
///
///   1. STATE SYNC: Reads FighterStateId from CombatComponent → selects
///      the correct AnimationId on the AnimatorComponent.
///   2. FRAME ADVANCE: Ticks the AnimatorComponent timer → advances frames
///      → updates SpriteComponent.SourceRect for rendering.
///
/// Runs AFTER CombatSystem (state is finalized) and BEFORE rendering.
/// Zero allocations — all writes are to pre-existing struct fields.
/// </summary>
public static class AnimationSystem
{
    /// <summary>
    /// Full animation tick: sync state → advance frames → update source rects.
    /// </summary>
    public static void Update(
        Span<AnimatorComponent> animators,
        Span<SpriteComponent> sprites,
        ReadOnlySpan<CombatComponent> combats,
        int entityCount,
        float deltaTime)
    {
        for (int i = 0; i < entityCount; i++)
        {
            ref AnimatorComponent animator = ref animators[i];
            ref SpriteComponent sprite = ref sprites[i];
            ref readonly CombatComponent combat = ref combats[i];

            // ── 1. State → Animation mapping ─────────────────────────
            AnimationId targetAnim = MapStateToAnimation(in combat);
            animator.Play(targetAnim); // No-ops if already playing

            // ── 2. Advance frame timer ───────────────────────────────
            if (animator.Animations is null)
                continue;

            int animIndex = (int)animator.CurrentAnimation;
            if (animIndex < 0 || animIndex >= animator.Animations.Length)
                continue;

            ref readonly AnimationData data = ref animator.Animations[animIndex];

            if (data.FrameCount <= 0)
                continue;

            if (!animator.IsFinished)
            {
                animator.FrameTimer += deltaTime;

                if (animator.FrameTimer >= data.FrameDuration)
                {
                    animator.FrameTimer -= data.FrameDuration;
                    animator.CurrentFrame++;

                    if (animator.CurrentFrame >= data.FrameCount)
                    {
                        if (data.Looping)
                        {
                            animator.CurrentFrame = 0;
                        }
                        else
                        {
                            animator.CurrentFrame = data.FrameCount - 1;
                            animator.IsFinished = true;
                        }
                    }
                }
            }

            // ── 3. Compute SourceRect from frame data ────────────────
            int column = data.StartFrame + animator.CurrentFrame;
            sprite.SourceRect.X = column * sprite.FrameWidth;
            sprite.SourceRect.Y = data.Row * sprite.FrameHeight;
            sprite.SourceRect.Width = sprite.FrameWidth;
            sprite.SourceRect.Height = sprite.FrameHeight;
        }
    }

    /// <summary>
    /// Maps a FighterStateId to the appropriate AnimationId.
    /// Pure function — no side effects, no allocation.
    ///
    /// This is the single point of truth for "what animation plays in what state."
    /// </summary>
    private static AnimationId MapStateToAnimation(in CombatComponent combat)
    {
        return combat.State switch
        {
            FighterStateId.Idle => AnimationId.Idle,
            FighterStateId.Run => AnimationId.Run,

            FighterStateId.JumpSquat => AnimationId.Jump,
            FighterStateId.Airborne => AnimationId.Jump,
            FighterStateId.Landing => AnimationId.Idle,

            FighterStateId.AttackStartup => combat.ActiveMoveIndex == 0
                ? AnimationId.AttackJab
                : AnimationId.AttackHeavy,
            FighterStateId.AttackActive => combat.ActiveMoveIndex == 0
                ? AnimationId.AttackJab
                : AnimationId.AttackHeavy,
            FighterStateId.AttackRecovery => combat.ActiveMoveIndex == 0
                ? AnimationId.AttackJab
                : AnimationId.AttackHeavy,

            FighterStateId.ParryWindow => AnimationId.Parry,
            FighterStateId.ParrySuccess => AnimationId.Parry,
            FighterStateId.Blocking => AnimationId.Block,
            FighterStateId.Blockstun => AnimationId.Block,

            FighterStateId.DashStartup => AnimationId.Dash,
            FighterStateId.DashActive => AnimationId.Dash,
            FighterStateId.DashRecovery => AnimationId.Dash,

            FighterStateId.Hitstun => AnimationId.Hitstun,
            FighterStateId.Knockback => AnimationId.Hitstun,
            FighterStateId.KnockedDown => AnimationId.Hitstun,

            _ => AnimationId.Idle
        };
    }
}
