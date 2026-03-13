using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// Draws all entities with SpriteComponent using their current SourceRect
/// (set by AnimationSystem) and position (from TransformComponent).
///
/// Handles horizontal flipping based on CombatComponent.FacingDirection.
/// Applies SpriteComponent.Scale for oversized sheet → low-res target.
/// Uses SpriteBatch.Draw with pre-computed source rectangles — zero allocation.
/// </summary>
public static class RenderSystem
{
    // ── Pre-allocated to avoid per-frame struct creation ──────────────
    private static Vector2 _drawPosition;

    // ── Cached background destination (set once, zero per-frame alloc) ──
    private static readonly Rectangle _bgDestRect = new(
        0, 0, GameSettings.VirtualWidth, GameSettings.VirtualHeight);

    public static void DrawBackground(SpriteBatch spriteBatch, Texture2D background)
    {
        spriteBatch.Draw(background, _bgDestRect, Color.White);
    }

    public static void Draw(
        SpriteBatch spriteBatch,
        ReadOnlySpan<TransformComponent> transforms,
        ReadOnlySpan<SpriteComponent> sprites,
        ReadOnlySpan<CombatComponent> combats,
        int entityCount)
    {
        for (int i = 0; i < entityCount; i++)
        {
            ref readonly TransformComponent transform = ref transforms[i];
            ref readonly SpriteComponent sprite = ref sprites[i];
            ref readonly CombatComponent combat = ref combats[i];

            if (sprite.Texture is null)
                continue;

            // Position = entity feet (bottom-center). Origin shifts the sprite up.
            _drawPosition.X = transform.Position.X;
            _drawPosition.Y = transform.Position.Y;

            SpriteEffects flip = combat.FacingDirection < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            float scale = sprite.Scale > 0f ? sprite.Scale : 1f;

            spriteBatch.Draw(
                texture: sprite.Texture,
                position: _drawPosition,
                sourceRectangle: sprite.SourceRect,
                color: sprite.Tint,
                rotation: 0f,
                origin: sprite.Origin,
                scale: scale,
                effects: flip,
                layerDepth: 0f);
        }
    }
}
