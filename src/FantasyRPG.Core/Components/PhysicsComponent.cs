namespace FantasyRPG.Core.Components;

/// <summary>
/// Physics tuning parameters for a fighter entity.
/// Mutable state (Position, Velocity, IsGrounded) lives in <see cref="TransformComponent"/>.
/// This struct holds the per-entity configuration that governs how physics affects them.
///
/// Values are set once during entity spawn and may be modified by power-ups or
/// state changes (e.g., reduced gravity during aerial combo). Never allocates.
/// </summary>
public struct PhysicsComponent
{
    /// <summary>Gravity acceleration in pixels/s². Pulled from GameSettings.Gravity by default.</summary>
    public float GravityScale;

    /// <summary>Maximum downward velocity in pixels/s. Prevents infinite falling speed.</summary>
    public float MaxFallSpeed;

    /// <summary>
    /// Horizontal friction coefficient (0..1). Applied each frame as a multiplier.
    /// 0.85 = slippery (ice). 0.70 = snappy (fighting game). 0.0 = instant stop.
    /// Lower = more friction = faster deceleration.
    /// </summary>
    public float Friction;

    /// <summary>Upward velocity applied on jump (negative = upward in screen coords).</summary>
    public float JumpForce;

    /// <summary>Horizontal movement speed in pixels/s when running.</summary>
    public float MoveSpeed;

    /// <summary>Horizontal speed applied during a dash.</summary>
    public float DashSpeed;

    /// <summary>
    /// Returns a default physics config tuned for a platform fighter character.
    /// Called once per entity spawn during initialization.
    /// </summary>
    public static PhysicsComponent CreateDefault()
    {
        return new PhysicsComponent
        {
            GravityScale = 1.0f,
            MaxFallSpeed = 400f,
            Friction = 0.72f,
            JumpForce = -280f,    // Negative = upward
            MoveSpeed = 120f,
            DashSpeed = 220f
        };
    }
}
