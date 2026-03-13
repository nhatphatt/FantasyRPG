using Microsoft.Xna.Framework;

namespace FantasyRPG.Core.Components;

/// <summary>
/// ATTACK collision volume. Attached to a fighter entity.
/// Only active during <see cref="FighterStateId.AttackActive"/> frames.
///
/// Hitboxes are defined as LOCAL offsets from the entity's TransformComponent
/// position. The CombatSystem resolves them to world-space each frame using
/// the entity's position + facing direction. Zero allocation — pure math.
///
/// Design rule: Hitboxes and Hurtboxes are SEPARATE from environment/physics
/// colliders. They exist only for the combat system.
/// </summary>
public struct HitboxComponent
{
    // ── Shape Definition (local space, relative to entity origin) ─────

    /// <summary>Offset from entity center (flipped by FacingDirection).</summary>
    public Vector2 LocalOffset;

    /// <summary>Half-extents of the AABB (width/2, height/2).</summary>
    public Vector2 HalfSize;

    /// <summary>Whether this hitbox is currently active (set by CombatSystem based on frame data).</summary>
    public bool IsActive;

    // ── Move Set: Multiple hitbox definitions per character ───────────

    /// <summary>
    /// Pre-allocated array of hitbox shapes per move.
    /// Index matches CombatComponent.ActiveMoveIndex.
    /// Loaded from JSON during LoadContent. Never modified at runtime.
    ///
    /// Each entry defines the hitbox shape for that specific attack.
    /// The CombatSystem copies the relevant entry into LocalOffset/HalfSize
    /// when an attack enters its active frames.
    /// </summary>
    public HitboxDefinition[] MoveHitboxes;

    /// <summary>
    /// Resolves this hitbox to world-space AABB given entity position and facing.
    /// Pure computation — no allocation.
    /// </summary>
    public readonly AABB ToWorldAABB(in Vector2 entityPosition, int facingDirection)
    {
        float offsetX = LocalOffset.X * facingDirection; // Flip for left-facing
        Vector2 center = new(
            entityPosition.X + offsetX,
            entityPosition.Y + LocalOffset.Y);

        return new AABB(center, HalfSize);
    }
}

/// <summary>
/// Immutable hitbox shape definition for a single move. Loaded from data.
/// </summary>
public readonly struct HitboxDefinition
{
    public readonly Vector2 Offset;
    public readonly Vector2 HalfSize;

    public HitboxDefinition(Vector2 offset, Vector2 halfSize)
    {
        Offset = offset;
        HalfSize = halfSize;
    }
}
