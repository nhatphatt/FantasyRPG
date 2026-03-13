using Microsoft.Xna.Framework;

namespace FantasyRPG.Core.Components;

/// <summary>
/// VULNERABLE collision volume. Attached to a fighter entity.
/// Always active unless the entity has i-frames (dash, parry success).
///
/// The hurtbox represents "where can this fighter be hit?"
/// It is SEPARATE from the physics/environment collider.
///
/// Typically a single AABB that covers the fighter's body.
/// Can support multiple zones (head, body, legs) for advanced hit detection.
/// </summary>
public struct HurtboxComponent
{
    /// <summary>Offset from entity center (does NOT flip — symmetric body).</summary>
    public Vector2 LocalOffset;

    /// <summary>Half-extents of the AABB.</summary>
    public Vector2 HalfSize;

    /// <summary>
    /// Master toggle. Set to false during i-frame states (DashActive, ParrySuccess).
    /// The CombatSystem checks this BEFORE any intersection test.
    /// </summary>
    public bool IsActive;

    /// <summary>
    /// Resolves this hurtbox to world-space AABB.
    /// Hurtboxes are typically symmetric, so no facing flip needed.
    /// </summary>
    public readonly AABB ToWorldAABB(in Vector2 entityPosition)
    {
        Vector2 center = new(
            entityPosition.X + LocalOffset.X,
            entityPosition.Y + LocalOffset.Y);

        return new AABB(center, HalfSize);
    }
}
