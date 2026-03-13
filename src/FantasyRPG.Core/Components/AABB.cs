using System;
using Microsoft.Xna.Framework;

namespace FantasyRPG.Core.Components;

/// <summary>
/// Axis-Aligned Bounding Box for combat collision (hitbox/hurtbox).
/// Center + HalfSize representation for efficient overlap tests.
///
/// This is a VALUE TYPE (struct). Zero heap allocation.
/// Used exclusively by the combat collision system — NOT for environment physics.
/// </summary>
public readonly struct AABB
{
    public readonly Vector2 Center;
    public readonly Vector2 HalfSize;

    public float Left => Center.X - HalfSize.X;
    public float Right => Center.X + HalfSize.X;
    public float Top => Center.Y - HalfSize.Y;
    public float Bottom => Center.Y + HalfSize.Y;

    public AABB(Vector2 center, Vector2 halfSize)
    {
        Center = center;
        HalfSize = halfSize;
    }

    /// <summary>
    /// Tests if two AABBs overlap. The fundamental operation of the combat system.
    /// Pure math — no branching on reference types, no allocation.
    /// </summary>
    public readonly bool Intersects(in AABB other)
    {
        // Separating Axis Theorem for AABB: if separated on ANY axis, no collision.
        float dx = MathF.Abs(Center.X - other.Center.X);
        float dy = MathF.Abs(Center.Y - other.Center.Y);

        return dx < (HalfSize.X + other.HalfSize.X)
            && dy < (HalfSize.Y + other.HalfSize.Y);
    }
}
