using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// Debug visualization system for the Gray Box Playground.
/// Renders hitboxes, hurtboxes, physics colliders, and parry windows
/// as colored hollow rectangles using a single 1×1 pixel texture.
///
/// All rectangles are drawn via SpriteBatch stretching — zero geometry allocation.
/// The 1×1 pixel texture is created once during initialization.
///
/// Color coding:
///   Yellow  = Physics body collider (environment)
///   Green   = Hurtbox (vulnerable area, active)
///   Red     = Hitbox (attack area, active)
///   Cyan    = Parry window (active during parry frames)
///   Magenta = Blocking state
///   Orange  = Hitstun/Blockstun
/// </summary>
public static class DebugDrawSystem
{
    private static Texture2D _pixel = null!;
    private static SpriteFont _font = null!;
    private static bool _enabled = true;
    private static bool _fontLoaded;

    // ── Pre-allocated color constants (no allocation per frame) ──────
    private static readonly Color ColorPhysicsBody = Color.Yellow;
    private static readonly Color ColorHurtbox = Color.Lime;
    private static readonly Color ColorHitbox = new(255, 50, 50, 200);
    private static readonly Color ColorParry = Color.Cyan;
    private static readonly Color ColorBlocking = Color.Magenta;
    private static readonly Color ColorStun = Color.Orange;
    private static readonly Color ColorTextShadow = new(0, 0, 0, 200);

    // ── Pre-allocated rectangles for DrawHollowRect (avoid new per call) ──
    private static Rectangle _rectTop;
    private static Rectangle _rectBottom;
    private static Rectangle _rectLeft;
    private static Rectangle _rectRight;
    private static Rectangle _rectFilled;

    // ── Pre-allocated char buffers for zero-alloc text rendering ─────
    // "State: AttackRecovery (F: 999)" = ~35 chars max
    private static readonly char[] _textBuffer = new char[48];
    private static Vector2 _textPos;
    private static Vector2 _shadowOffset = new(0.5f, 0.5f);

    /// <summary>Toggle debug rendering on/off at runtime.</summary>
    public static bool Enabled { get => _enabled; set => _enabled = value; }

    /// <summary>The 1×1 white pixel texture for drawing rectangles.</summary>
    public static Texture2D Pixel => _pixel;

    /// <summary>The loaded SpriteFont (null if not yet loaded).</summary>
    public static SpriteFont? Font => _fontLoaded ? _font : null;

    /// <summary>
    /// Creates the 1×1 white pixel texture. Call once during LoadContent.
    /// This is the ONLY allocation in the entire debug system.
    /// </summary>
    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Loads the debug SpriteFont. Call once during LoadContent after
    /// content pipeline is ready. Font is optional — text drawing
    /// gracefully no-ops if never loaded.
    /// </summary>
    public static void LoadFont(SpriteFont font)
    {
        _font = font;
        _fontLoaded = true;
    }

    /// <summary>
    /// Disposes the pixel texture. Call during UnloadContent.
    /// </summary>
    public static void Dispose()
    {
        _pixel?.Dispose();
    }

    /// <summary>
    /// Main draw entry point. Renders all debug overlays for all entities.
    /// Must be called INSIDE an active SpriteBatch.Begin/End block.
    /// Zero allocations — all rectangles are pre-computed structs.
    /// </summary>
    public static void Draw(
        SpriteBatch spriteBatch,
        ReadOnlySpan<TransformComponent> transforms,
        ReadOnlySpan<CombatComponent> combats,
        ReadOnlySpan<HitboxComponent> hitboxes,
        ReadOnlySpan<HurtboxComponent> hurtboxes,
        ReadOnlySpan<PhysicsComponent> physics,
        int entityCount)
    {
        if (!_enabled)
            return;

        for (int i = 0; i < entityCount; i++)
        {
            ref readonly TransformComponent transform = ref transforms[i];
            ref readonly CombatComponent combat = ref combats[i];
            ref readonly HitboxComponent hitbox = ref hitboxes[i];
            ref readonly HurtboxComponent hurtbox = ref hurtboxes[i];

            // ── 1. Physics Body (Yellow) ─────────────────────────────
            // Represents the entity's physical presence for environment collision
            const float bodyHalfW = 8f;
            const float bodyH = 24f;
            DrawHollowRect(spriteBatch,
                (int)(transform.Position.X - bodyHalfW),
                (int)(transform.Position.Y - bodyH),
                (int)(bodyHalfW * 2f),
                (int)bodyH,
                ColorPhysicsBody, 1);

            // ── 2. Hurtbox (Green) ───────────────────────────────────
            if (hurtbox.IsActive)
            {
                AABB hurtAABB = hurtbox.ToWorldAABB(in transform.Position);
                Color hurtColor = combat.State switch
                {
                    FighterStateId.ParryWindow => ColorParry,
                    FighterStateId.ParrySuccess => ColorParry,
                    FighterStateId.Blocking => ColorBlocking,
                    FighterStateId.Hitstun => ColorStun,
                    FighterStateId.Blockstun => ColorStun,
                    _ => ColorHurtbox
                };

                DrawHollowRect(spriteBatch,
                    (int)hurtAABB.Left,
                    (int)hurtAABB.Top,
                    (int)(hurtAABB.HalfSize.X * 2f),
                    (int)(hurtAABB.HalfSize.Y * 2f),
                    hurtColor, 1);
            }

            // ── 3. Hitbox (Red) ──────────────────────────────────────
            if (hitbox.IsActive)
            {
                AABB hitAABB = hitbox.ToWorldAABB(in transform.Position, combat.FacingDirection);
                DrawFilledRect(spriteBatch,
                    (int)hitAABB.Left,
                    (int)hitAABB.Top,
                    (int)(hitAABB.HalfSize.X * 2f),
                    (int)(hitAABB.HalfSize.Y * 2f),
                    ColorHitbox);
            }

            // ── 4. Facing Direction Indicator ────────────────────────
            int indicatorX = (int)transform.Position.X + (combat.FacingDirection * 10);
            int indicatorY = (int)(transform.Position.Y - bodyH - 4);
            DrawFilledRect(spriteBatch, indicatorX - 1, indicatorY - 1, 3, 3, Color.White);
        }
    }

    /// <summary>
    /// Draws the static platforms as white filled rectangles.
    /// Call inside SpriteBatch.Begin/End.
    /// </summary>
    public static void DrawPlatforms(
        SpriteBatch spriteBatch,
        ReadOnlySpan<AABB> platforms,
        int platformCount)
    {
        for (int i = 0; i < platformCount; i++)
        {
            ref readonly AABB plat = ref platforms[i];
            DrawFilledRect(spriteBatch,
                (int)plat.Left,
                (int)plat.Top,
                (int)(plat.HalfSize.X * 2f),
                (int)(plat.HalfSize.Y * 2f),
                new Color(60, 60, 70));
        }
    }

    /// <summary>
    /// Draws a hollow (outline-only) rectangle using 4 stretched 1×1 pixel draws.
    /// Zero allocation — writes to pre-allocated static Rectangle fields.
    /// </summary>
    public static void DrawHollowRect(
        SpriteBatch spriteBatch,
        int x, int y, int width, int height,
        Color color, int thickness)
    {
        // Top edge
        _rectTop.X = x; _rectTop.Y = y;
        _rectTop.Width = width; _rectTop.Height = thickness;
        spriteBatch.Draw(_pixel, _rectTop, color);

        // Bottom edge
        _rectBottom.X = x; _rectBottom.Y = y + height - thickness;
        _rectBottom.Width = width; _rectBottom.Height = thickness;
        spriteBatch.Draw(_pixel, _rectBottom, color);

        // Left edge
        _rectLeft.X = x; _rectLeft.Y = y;
        _rectLeft.Width = thickness; _rectLeft.Height = height;
        spriteBatch.Draw(_pixel, _rectLeft, color);

        // Right edge
        _rectRight.X = x + width - thickness; _rectRight.Y = y;
        _rectRight.Width = thickness; _rectRight.Height = height;
        spriteBatch.Draw(_pixel, _rectRight, color);
    }

    /// <summary>
    /// Draws a filled rectangle (semi-transparent for overlays).
    /// </summary>
    public static void DrawFilledRect(
        SpriteBatch spriteBatch,
        int x, int y, int width, int height,
        Color color)
    {
        _rectFilled.X = x; _rectFilled.Y = y;
        _rectFilled.Width = width; _rectFilled.Height = height;
        spriteBatch.Draw(_pixel, _rectFilled, color);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ON-SCREEN TEXT LABELS — SpriteFont-based debug text
    // ═══════════════════════════════════════════════════════════════════

    // ── Pre-cached state name strings (zero allocation at runtime) ────
    private static readonly string[] StateNames = BuildStateNameCache();

    private static string[] BuildStateNameCache()
    {
        // Pre-allocate all enum name strings once at static init.
        // FighterStateId max value is ~52, allocate 64 slots.
        var names = new string[64];
        for (int i = 0; i < names.Length; i++)
            names[i] = "???";

        names[(int)FighterStateId.Idle] = "Idle";
        names[(int)FighterStateId.Run] = "Run";
        names[(int)FighterStateId.JumpSquat] = "JumpSquat";
        names[(int)FighterStateId.Airborne] = "Airborne";
        names[(int)FighterStateId.Landing] = "Landing";
        names[(int)FighterStateId.AttackStartup] = "AtkStartup";
        names[(int)FighterStateId.AttackActive] = "AtkActive";
        names[(int)FighterStateId.AttackRecovery] = "AtkRecovery";
        names[(int)FighterStateId.ParryWindow] = "Parry";
        names[(int)FighterStateId.ParrySuccess] = "ParryOK!";
        names[(int)FighterStateId.Blocking] = "Blocking";
        names[(int)FighterStateId.Blockstun] = "Blockstun";
        names[(int)FighterStateId.DashStartup] = "DashStart";
        names[(int)FighterStateId.DashActive] = "Dashing";
        names[(int)FighterStateId.DashRecovery] = "DashRecov";
        names[(int)FighterStateId.Hitstun] = "Hitstun";
        names[(int)FighterStateId.Knockback] = "Knockback";
        names[(int)FighterStateId.KnockedDown] = "KnockDown";

        return names;
    }

    // ── Pre-cached frame number strings: "0" through "99" ────────────
    private static readonly string[] FrameNumberStrings = BuildFrameNumberCache();

    private static string[] BuildFrameNumberCache()
    {
        var nums = new string[100];
        for (int i = 0; i < 100; i++)
            nums[i] = i.ToString();
        return nums;
    }

    // ── Pre-allocated label strings (avoids per-frame string concat) ──
    private static readonly string LabelPrefix = "F:";
    private static readonly string LabelDummy = "DUMMY";

    /// <summary>
    /// Draws on-screen text labels above each entity showing their
    /// current FighterStateId and frame counter. Uses SpriteFont.
    ///
    /// Player label:  "StateName" + "F:##"
    /// Dummy label:   "DUMMY" + "StateName"
    ///
    /// All strings are pre-cached — zero allocation per frame.
    /// Gracefully no-ops if font was never loaded.
    /// </summary>
    public static void DrawEntityLabels(
        SpriteBatch spriteBatch,
        ReadOnlySpan<TransformComponent> transforms,
        ReadOnlySpan<CombatComponent> combats,
        int entityCount,
        int playerIndex)
    {
        if (!_enabled || !_fontLoaded)
            return;

        // 14pt font × 0.3 = ~4.2pt effective in virtual res.
        // At 480×270 upscaled 4× to 1920×1080, this reads as ~17pt on screen.
        const float scale = 0.3f;
        const float lineHeight = 6f;    // vertical spacing between lines
        const float baseOffsetY = 52f;  // pixels above entity feet (above health bar)

        for (int i = 0; i < entityCount; i++)
        {
            ref readonly TransformComponent t = ref transforms[i];
            ref readonly CombatComponent c = ref combats[i];

            int stateIndex = (int)c.State;
            string stateName = stateIndex < StateNames.Length
                ? StateNames[stateIndex]
                : StateNames[0];

            Color labelColor = GetLabelColor(c.State);

            if (i == playerIndex)
            {
                // ── Player Line 1: State name ───────────────────────
                Vector2 measure = _font.MeasureString(stateName) * scale;
                _textPos.X = MathF.Floor(t.Position.X - measure.X * 0.5f);
                _textPos.Y = MathF.Floor(t.Position.Y - baseOffsetY);

                spriteBatch.DrawString(_font, stateName,
                    _textPos + _shadowOffset, ColorTextShadow,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, stateName,
                    _textPos, labelColor,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // ── Player Line 2: Frame counter "F:##" ─────────────
                string frameNum = c.CurrentFrame < FrameNumberStrings.Length
                    ? FrameNumberStrings[c.CurrentFrame]
                    : FrameNumberStrings[99];

                float prefixW = _font.MeasureString(LabelPrefix).X * scale;
                float numW = _font.MeasureString(frameNum).X * scale;
                float totalW = prefixW + numW;

                _textPos.X = MathF.Floor(t.Position.X - totalW * 0.5f);
                _textPos.Y = MathF.Floor(t.Position.Y - baseOffsetY + lineHeight);

                spriteBatch.DrawString(_font, LabelPrefix,
                    _textPos + _shadowOffset, ColorTextShadow,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, LabelPrefix,
                    _textPos, Color.Gray,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                _textPos.X += prefixW;

                spriteBatch.DrawString(_font, frameNum,
                    _textPos + _shadowOffset, ColorTextShadow,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, frameNum,
                    _textPos, Color.White,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            else
            {
                // ── Dummy Line 1: "DUMMY" ───────────────────────────
                Vector2 dMeasure = _font.MeasureString(LabelDummy) * scale;
                _textPos.X = MathF.Floor(t.Position.X - dMeasure.X * 0.5f);
                _textPos.Y = MathF.Floor(t.Position.Y - baseOffsetY);

                spriteBatch.DrawString(_font, LabelDummy,
                    _textPos + _shadowOffset, ColorTextShadow,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, LabelDummy,
                    _textPos, Color.LightGray,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // ── Dummy Line 2: State name ────────────────────────
                Vector2 sMeasure = _font.MeasureString(stateName) * scale;
                _textPos.X = MathF.Floor(t.Position.X - sMeasure.X * 0.5f);
                _textPos.Y = MathF.Floor(t.Position.Y - baseOffsetY + lineHeight);

                spriteBatch.DrawString(_font, stateName,
                    _textPos + _shadowOffset, ColorTextShadow,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, stateName,
                    _textPos, labelColor,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
    }

    private static Color GetLabelColor(FighterStateId state)
    {
        return state switch
        {
            FighterStateId.Idle => Color.White,
            FighterStateId.Run => Color.Lime,
            FighterStateId.Airborne or FighterStateId.JumpSquat => Color.SkyBlue,
            FighterStateId.AttackStartup => new Color(255, 160, 0),
            FighterStateId.AttackActive => Color.Red,
            FighterStateId.AttackRecovery => new Color(180, 60, 0),
            FighterStateId.ParryWindow or FighterStateId.ParrySuccess => Color.Cyan,
            FighterStateId.Blocking => Color.Magenta,
            FighterStateId.DashStartup or FighterStateId.DashActive
                or FighterStateId.DashRecovery => Color.Yellow,
            FighterStateId.Hitstun or FighterStateId.Blockstun => Color.Orange,
            _ => Color.Gray
        };
    }
}
