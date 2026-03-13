using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FantasyRPG.Core.Components;

/// <summary>
/// Rendering data for a sprite entity. References a pre-loaded Texture2D
/// (the sprite sheet) and holds the current source rectangle for drawing.
///
/// This is a mutable struct. The AnimationSystem updates SourceRect each frame.
/// The RenderSystem reads it to draw via SpriteBatch.
///
/// The Texture2D reference is set once during spawn and never changed.
/// SpriteEffects is flipped by the render system based on FacingDirection.
/// </summary>
public struct SpriteComponent
{
    /// <summary>The sprite sheet texture. Set once during LoadContent/spawn. Null = debug-only mode.</summary>
    public Texture2D? Texture;

    /// <summary>Width of a single frame in the sprite sheet, in pixels.</summary>
    public int FrameWidth;

    /// <summary>Height of a single frame in the sprite sheet, in pixels.</summary>
    public int FrameHeight;

    /// <summary>
    /// Current source rectangle into the sprite sheet.
    /// Updated by AnimationSystem each frame. Read by RenderSystem.
    /// Pre-allocated struct — zero allocation on update.
    /// </summary>
    public Rectangle SourceRect;

    /// <summary>Tint color. Default = Color.White (no tint).</summary>
    public Color Tint;

    /// <summary>
    /// Draw origin offset from entity Position (feet = bottom-center).
    /// Typically (FrameWidth/2, FrameHeight) so the sprite is centered
    /// horizontally and positioned with feet at Position.Y.
    /// </summary>
    public Vector2 Origin;

    /// <summary>
    /// Uniform scale applied at draw time. 1.0 = native sheet resolution.
    /// For oversized sprite sheets on a low-res render target, use &lt; 1.0.
    /// </summary>
    public float Scale;
}
