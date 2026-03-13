using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FantasyRPG.Core.Engine.Input;
using FantasyRPG.Core.Systems;

namespace FantasyRPG.Core.GameStates;

/// <summary>
/// Main menu screen with background, animated title, and interactive options.
/// Navigated with Up/Down arrows, confirmed with Space.
/// Zero allocation in Update/Draw — all strings are const literals.
/// </summary>
public sealed class MainMenuState : IGameState
{
    // ── Menu Options ─────────────────────────────────────────────────
    private const int OptionCount = 2;
    private const int OptionStartGame = 0;
    private const int OptionExit = 1;

    // ── Layout Constants (virtual resolution 480×270) ────────────────
    private const float TitleY = 40f;
    private const float TitleScale = 3.0f;
    private const float TitleShadowOffset = 2f;
    private const float OptionsStartY = 155f;
    private const float OptionSpacing = 24f;
    private const float OptionScale = 1.0f;
    private const float HintScale = 1.0f;

    // ── Title Pulse Animation ────────────────────────────────────────
    private const float PulseSpeed = 2.5f;
    private const float PulseMin = 0.92f;
    private const float PulseMax = 1.0f;

    // ── Dependencies ─────────────────────────────────────────────────
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;
    private readonly Texture2D _bgTexture;
    private readonly Action _exitGame;

    // ── State ────────────────────────────────────────────────────────
    private int _selectedIndex;
    private float _pulseTimer;

    // ── Pre-computed layout (set once in Enter, no per-frame alloc) ──
    private Rectangle _bgDestRect;

    public MainMenuState(
        InputManager input,
        GameStateManager stateManager,
        Texture2D bgTexture,
        Action exitGame)
    {
        _input = input;
        _stateManager = stateManager;
        _bgTexture = bgTexture;
        _exitGame = exitGame;

        _bgDestRect = new Rectangle(
            0, 0, GameSettings.VirtualWidth, GameSettings.VirtualHeight);
    }

    public void Enter()
    {
        _selectedIndex = 0;
        _pulseTimer = 0f;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _pulseTimer += dt * PulseSpeed;

        // ── Navigation ──────────────────────────────────────────────
        if (_input.IsKeyPressed(Keys.Up))
            _selectedIndex = (_selectedIndex - 1 + OptionCount) % OptionCount;
        else if (_input.IsKeyPressed(Keys.Down))
            _selectedIndex = (_selectedIndex + 1) % OptionCount;

        // ── Confirm ─────────────────────────────────────────────────
        if (_input.IsKeyPressed(Keys.Space))
        {
            switch (_selectedIndex)
            {
                case OptionStartGame:
                    _stateManager.ChangeState(GameStateId.CharacterSelect);
                    break;
                case OptionExit:
                    _exitGame();
                    break;
            }
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // ── Background (fill entire virtual resolution) ─────────────
        spriteBatch.Draw(_bgTexture, _bgDestRect, Color.White);

        var font = DebugDrawSystem.Font;
        if (font is null) return;

        int centerX = GameSettings.VirtualWidth / 2;

        // ── Title with pulse animation + drop shadow ────────────────
        const string title = "FantasyRPG";
        float pulse = PulseMin + (PulseMax - PulseMin) *
            (0.5f + 0.5f * MathF.Sin(_pulseTimer));
        float titleFinalScale = TitleScale * pulse;

        var titleSize = font.MeasureString(title) * titleFinalScale;
        float titleX = centerX - titleSize.X / 2f;

        // Drop shadow (dark, offset down-right)
        spriteBatch.DrawString(font, title,
            new Vector2(titleX + TitleShadowOffset, TitleY + TitleShadowOffset),
            new Color(0, 0, 0, 200),
            0f, Vector2.Zero, titleFinalScale, SpriteEffects.None, 0f);

        // Main title (golden gradient feel)
        spriteBatch.DrawString(font, title,
            new Vector2(titleX, TitleY),
            Color.Gold,
            0f, Vector2.Zero, titleFinalScale, SpriteEffects.None, 0f);

        // ── Menu Options ────────────────────────────────────────────
        DrawOption(spriteBatch, font, centerX, 0, "Start Game");
        DrawOption(spriteBatch, font, centerX, 1, "Exit");

        // ── Navigation hint ─────────────────────────────────────────
        const string hint = "UP/DOWN: SELECT    SPACE: CONFIRM";
        var hintSize = font.MeasureString(hint) * HintScale;
        spriteBatch.DrawString(font, hint,
            new Vector2(centerX - hintSize.X / 2f, GameSettings.VirtualHeight - 20f),
            new Color(180, 180, 180),
            0f, Vector2.Zero, HintScale, SpriteEffects.None, 0f);
    }

    public void Exit() { }

    // ── Helpers ──────────────────────────────────────────────────────

    private void DrawOption(
        SpriteBatch spriteBatch, SpriteFont font,
        int centerX, int index, string label)
    {
        bool selected = index == _selectedIndex;
        float y = OptionsStartY + index * OptionSpacing;

        // Build display text: selected = "> Label <", unselected = "Label"
        string display = selected ? $"> {label} <" : label;
        Color color = selected ? Color.Yellow : Color.White;

        var textSize = font.MeasureString(display) * OptionScale;
        float x = centerX - textSize.X / 2f;

        // Shadow for readability
        spriteBatch.DrawString(font, display,
            new Vector2(x + 1f, y + 1f),
            new Color(0, 0, 0, 160),
            0f, Vector2.Zero, OptionScale, SpriteEffects.None, 0f);

        // Main text
        spriteBatch.DrawString(font, display,
            new Vector2(x, y),
            color,
            0f, Vector2.Zero, OptionScale, SpriteEffects.None, 0f);
    }

}
