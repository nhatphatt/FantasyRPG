using System;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// Applies gravity, terminal velocity, friction, and environment collision
/// to all entities with Transform + Physics components.
///
/// Environment collision uses a static floor AABB — no dynamic allocation.
/// This system runs AFTER CombatSystem (knockback writes to Velocity)
/// and BEFORE RenderSystem.
///
/// Zero allocations. All math is on stack-only structs.
/// </summary>
public static class PhysicsSystem
{
    /// <summary>
    /// The static stage floor. Set once during scene init.
    /// Stored as a simple Y-threshold for the gray box playground.
    /// For a full stage, this becomes an array of platform AABBs.
    /// </summary>
    private static AABB[] _platforms = Array.Empty<AABB>();
    private static int _platformCount;

    /// <summary>
    /// Registers the static platforms for environment collision.
    /// Call once during scene initialization. The array is pre-allocated.
    /// </summary>
    public static void SetPlatforms(AABB[] platforms, int count)
    {
        _platforms = platforms;
        _platformCount = count;
    }

    /// <summary>
    /// Full physics tick: gravity → terminal velocity → friction → environment collision.
    /// Called once per frame with the fixed timestep deltaTime.
    /// </summary>
    public static void Update(
        Span<TransformComponent> transforms,
        Span<PhysicsComponent> physics,
        int entityCount,
        float deltaTime)
    {
        float globalGravity = GameSettings.Gravity;

        for (int i = 0; i < entityCount; i++)
        {
            ref TransformComponent transform = ref transforms[i];
            ref PhysicsComponent phys = ref physics[i];

            // ── 1. Gravity ───────────────────────────────────────────
            // Only apply when airborne. Gravity is pixels/s², scaled per-entity.
            if (!transform.IsGrounded)
            {
                transform.Velocity.Y += globalGravity * phys.GravityScale * deltaTime;

                // ── 2. Terminal Velocity ──────────────────────────────
                if (transform.Velocity.Y > phys.MaxFallSpeed)
                    transform.Velocity.Y = phys.MaxFallSpeed;
            }

            // ── 3. Horizontal Friction ───────────────────────────────
            // Multiply velocity by friction factor each frame.
            // Only apply when grounded and no movement input is driving velocity.
            // (CombatSystem/InputSystem sets velocity directly for movement;
            //  friction decelerates when no input is applied.)
            if (transform.IsGrounded)
            {
                transform.Velocity.X *= phys.Friction;

                // Snap to zero to prevent float drift
                if (MathF.Abs(transform.Velocity.X) < 0.5f)
                    transform.Velocity.X = 0f;
            }

            // ── 4. Integrate Position ────────────────────────────────
            transform.Position.X += transform.Velocity.X * deltaTime;
            transform.Position.Y += transform.Velocity.Y * deltaTime;

            // ── 5. Screen Boundary Clamp ─────────────────────────────
            if (transform.Position.X < 0f)
            {
                transform.Position.X = 0f;
                transform.Velocity.X = 0f;
            }
            else if (transform.Position.X > GameSettings.VirtualWidth)
            {
                transform.Position.X = GameSettings.VirtualWidth;
                transform.Velocity.X = 0f;
            }

            // ── 6. Environment Collision (Floor/Platform) ────────────
            transform.IsGrounded = false;

            for (int p = 0; p < _platformCount; p++)
            {
                ref readonly AABB platform = ref _platforms[p];
                ResolveFloorCollision(ref transform, in platform);
            }
        }
    }

    /// <summary>
    /// Resolves collision between an entity's feet and a platform top surface.
    /// Simple top-only collision: if the entity's bottom is inside the platform
    /// and was above it last frame (falling downward), snap to the surface.
    ///
    /// Uses the entity position as a single point at their feet (bottom-center).
    /// For a full character body, extend with an entity AABB.
    /// </summary>
    private static void ResolveFloorCollision(
        ref TransformComponent transform,
        in AABB platform)
    {
        // Entity's foot position (bottom-center)
        const float entityHalfWidth = 8f;

        float entityBottom = transform.Position.Y;
        float entityLeft = transform.Position.X - entityHalfWidth;
        float entityRight = transform.Position.X + entityHalfWidth;

        // Check horizontal overlap with platform
        if (entityRight <= platform.Left || entityLeft >= platform.Right)
            return;

        // Check if entity's bottom penetrates the platform top surface
        // AND entity is falling (or stationary) — prevents "sticking" when jumping up through
        float entityPreviousBottom = entityBottom - transform.Velocity.Y * (1f / GameSettings.TargetFps);
        float surfaceTop = platform.Top;

        if (entityBottom >= surfaceTop
            && entityPreviousBottom <= surfaceTop + 4f // Generous tolerance for stable grounding
            && transform.Velocity.Y >= 0f)
        {
            // Snap to platform surface
            transform.Position.Y = surfaceTop;
            transform.Velocity.Y = 0f;
            transform.IsGrounded = true;
        }
        // Standing exactly on surface — maintain grounding (prevents 1-frame drops)
        else if (MathF.Abs(entityBottom - surfaceTop) < 1f && transform.Velocity.Y >= 0f)
        {
            transform.Position.Y = surfaceTop;
            transform.Velocity.Y = 0f;
            transform.IsGrounded = true;
        }
    }
}
