using Microsoft.Xna.Framework;

namespace FantasyRPG.Core.Components;

/// <summary>
/// Position and basic spatial data for an entity. Used by ALL systems.
/// Pixel coordinates in virtual resolution space (480×270).
/// </summary>
public struct TransformComponent
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool IsGrounded;
}
