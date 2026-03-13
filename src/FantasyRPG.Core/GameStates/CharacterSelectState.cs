using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FantasyRPG.Core.Components;
using FantasyRPG.Core.Engine.Input;
using FantasyRPG.Core.Systems;

namespace FantasyRPG.Core.GameStates;

/// <summary>
/// Character selection screen with animated idle sprites on pedestals.
/// Player picks Knight (1) or Wizard (2), toggles Dummy/Bot mode,
/// then confirms with Space. Zero allocation in Update/Draw loops.
/// </summary>
public sealed class CharacterSelectState : IGameState
{
    // ── Dependencies ─────────────────────────────────────────────────
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;
    private readonly PlayState _playState;

    // ── Textures ─────────────────────────────────────────────────────
    private readonly Texture2D _bgTexture;
    private readonly Texture2D _knightTexture;
    private readonly Texture2D _wizardTexture;

    // ── Sprite Sheet Dimensions ──────────────────────────────────────
    private readonly int _knightFrameW;
    private readonly int _knightFrameH;
    private readonly int _wizardFrameW;
    private readonly int _wizardFrameH;

    // ── Knight Idle Animation (Row 0, 4 frames, 0.15s each) ─────────
    private const int KnightIdleRow = 0;
    private const int KnightIdleStart = 0;
    private const int KnightIdleFrames = 4;
    private const float KnightIdleDuration = 0.15f;

    // ── Wizard Idle Animation (Row 0, 8 frames, 0.12s each) ─────────
    private const int WizardIdleRow = 0;
    private const int WizardIdleStart = 0;
    private const int WizardIdleFrames = 8;
    private const float WizardIdleDuration = 0.12f;

    // ── Responsive Layout (percentages of virtual resolution) ─────────
    private const float LeftPedestalPct = 0.30f;   // 30% from left
    private const float RightPedestalPct = 0.70f;  // 70% from left
    private const float PedestalYPct = 0.72f;      // 72% from top
    private const float LabelYPct = 0.78f;         // character labels
    private const float ModeYPct = 0.89f;          // DUMMY / BOT
    private const float PromptYPct = 0.95f;        // "SPACE: SELECT"

    // ── Character Render Scale (target ~70px tall on pedestal) ───────
    private const float TargetCharHeight = 70f;

    // ── Layout ───────────────────────────────────────────────────────
    private const float TitleY = 10f;
    private const float TitleScale = 2.0f;
    private const float TitleShadowOffset = 1.5f;
    private const float LabelScale = 1.0f;

    // ── State (mutable, no allocation) ───────────────────────────────
    private int _selectedIndex;  // 0 = Knight, 1 = Wizard
    private int _modeIndex;      // 0 = Dummy, 1 = Bot
    private float _animTimer;    // shared animation timer (seconds)

    // ── Pre-allocated structs ────────────────────────────────────────
    private Rectangle _bgDestRect;
    private Rectangle _knightSrcRect;
    private Rectangle _wizardSrcRect;

    public CharacterSelectState(
        InputManager input,
        GameStateManager stateManager,
        PlayState playState,
        Texture2D bgTexture,
        Texture2D knightTexture, int knightFrameW, int knightFrameH,
        Texture2D wizardTexture, int wizardFrameW, int wizardFrameH)
    {
        _input = input;
        _stateManager = stateManager;
        _playState = playState;
        _bgTexture = bgTexture;
        _knightTexture = knightTexture;
        _knightFrameW = knightFrameW;
        _knightFrameH = knightFrameH;
        _wizardTexture = wizardTexture;
        _wizardFrameW = wizardFrameW;
        _wizardFrameH = wizardFrameH;

        _bgDestRect = new Rectangle(0, 0, GameSettings.VirtualWidth, GameSettings.VirtualHeight);
    }

    public void Enter()
    {
        _selectedIndex = 0;
        _modeIndex = 1; // Default to Bot mode
        _animTimer = 0f;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _animTimer += dt;

        // ── Character selection (Left/Right or D1/D2) ───────────────
        if (_input.IsKeyPressed(Keys.Left) || _input.IsKeyPressed(Keys.D1))
            _selectedIndex = 0;
        else if (_input.IsKeyPressed(Keys.Right) || _input.IsKeyPressed(Keys.D2))
            _selectedIndex = 1;

        // ── Mode toggle (Up/Down) ────────────────────────────────────
        if (_input.IsKeyPressed(Keys.Up) || _input.IsKeyPressed(Keys.Down))
            _modeIndex = _modeIndex == 0 ? 1 : 0;

        // ── Confirm ──────────────────────────────────────────────────
        if (_input.IsKeyPressed(Keys.Space) || _input.IsKeyPressed(Keys.Enter))
        {
            _playState.SetPlayerCharacter(_selectedIndex);
            _playState.SetBotEnabled(_modeIndex == 1);
            _stateManager.ChangeState(GameStateId.Gameplay);
        }

        // ── Compute current animation source rects (zero alloc) ─────
        int knightFrame = KnightIdleStart +
            ((int)(_animTimer / KnightIdleDuration) % KnightIdleFrames);
        _knightSrcRect.X = knightFrame * _knightFrameW;
        _knightSrcRect.Y = KnightIdleRow * _knightFrameH;
        _knightSrcRect.Width = _knightFrameW;
        _knightSrcRect.Height = _knightFrameH;

        int wizardFrame = WizardIdleStart +
            ((int)(_animTimer / WizardIdleDuration) % WizardIdleFrames);
        _wizardSrcRect.X = wizardFrame * _wizardFrameW;
        _wizardSrcRect.Y = WizardIdleRow * _wizardFrameH;
        _wizardSrcRect.Width = _wizardFrameW;
        _wizardSrcRect.Height = _wizardFrameH;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // ── 0. Responsive screen metrics ─────────────────────────────
        // We render to the 480×270 virtual canvas (RenderTarget2D), so
        // all positions are percentages of VirtualWidth / VirtualHeight.
        float sw = GameSettings.VirtualWidth;
        float sh = GameSettings.VirtualHeight;

        float leftPedX = sw * LeftPedestalPct;   // 144
        float rightPedX = sw * RightPedestalPct;  // 336
        float pedY = sh * PedestalYPct;           // 194
        float labelY = sh * LabelYPct;            // 210
        float modeY = sh * ModeYPct;              // 240
        float promptY = sh * PromptYPct;          // 256
        float centerX = sw * 0.5f;

        // ── 1. Background (fill entire virtual canvas) ───────────────
        spriteBatch.Draw(_bgTexture, _bgDestRect, Color.White);

        var font = DebugDrawSystem.Font;
        if (font is null) return;

        // ── 2. Title "SELECT CHARACTER" with drop shadow ─────────────
        const string title = "SELECT CHARACTER";
        var titleSize = font.MeasureString(title) * TitleScale;
        float titleX = centerX - titleSize.X / 2f;

        spriteBatch.DrawString(font, title,
            new Vector2(titleX + TitleShadowOffset, TitleY + TitleShadowOffset),
            new Color(0, 0, 0, 200),
            0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, title,
            new Vector2(titleX, TitleY),
            Color.Gold,
            0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

        // ── 3. Animated characters on pedestals ──────────────────────
        float knightScale = TargetCharHeight / _knightFrameH;
        float wizardScale = TargetCharHeight / _wizardFrameH;

        // Knight — bottom-center origin, feet land exactly on pedY
        var knightOrigin = new Vector2(_knightFrameW / 2f, _knightFrameH);
        spriteBatch.Draw(_knightTexture,
            new Vector2(leftPedX, pedY),
            _knightSrcRect, Color.White, 0f, knightOrigin, knightScale,
            SpriteEffects.None, 0f);

        // Wizard — bottom-center origin, feet land exactly on pedY
        var wizardOrigin = new Vector2(_wizardFrameW / 2f, _wizardFrameH);
        spriteBatch.Draw(_wizardTexture,
            new Vector2(rightPedX, pedY),
            _wizardSrcRect, Color.White, 0f, wizardOrigin, wizardScale,
            SpriteEffects.None, 0f);

        // ── 4. Character labels (centered below pedestals) ────────────
        const string knightLabel = "1: KNIGHT";
        const string wizardLabel = "2: WIZARD";

        Color knightLabelColor = _selectedIndex == 0 ? Color.Yellow : Color.Gray;
        Color wizardLabelColor = _selectedIndex == 1 ? Color.Yellow : Color.Gray;

        var knightLabelSize = font.MeasureString(knightLabel) * LabelScale;
        var wizardLabelSize = font.MeasureString(wizardLabel) * LabelScale;

        // Knight label — shadow + text
        spriteBatch.DrawString(font, knightLabel,
            new Vector2(leftPedX - knightLabelSize.X / 2f + 1f, labelY + 1f),
            new Color(0, 0, 0, 160),
            0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, knightLabel,
            new Vector2(leftPedX - knightLabelSize.X / 2f, labelY),
            knightLabelColor,
            0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);

        // Wizard label — shadow + text
        spriteBatch.DrawString(font, wizardLabel,
            new Vector2(rightPedX - wizardLabelSize.X / 2f + 1f, labelY + 1f),
            new Color(0, 0, 0, 160),
            0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, wizardLabel,
            new Vector2(rightPedX - wizardLabelSize.X / 2f, labelY),
            wizardLabelColor,
            0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);

        // ── 5. Mode selector (DUMMY / BOT) ───────────────────────────
        const string modeDummy = "DUMMY";
        const string modeBot = "BOT";
        const string modeSep = " / ";

        var dummySize = font.MeasureString(modeDummy);
        var sepSize = font.MeasureString(modeSep);
        var botSize = font.MeasureString(modeBot);
        float totalModeW = dummySize.X + sepSize.X + botSize.X;
        float modeStartX = centerX - totalModeW / 2f;

        Color dummyColor = _modeIndex == 0 ? Color.Yellow : Color.DarkGray;
        Color botColor = _modeIndex == 1 ? Color.OrangeRed : Color.DarkGray;

        spriteBatch.DrawString(font, modeDummy,
            new Vector2(modeStartX, modeY), dummyColor);
        spriteBatch.DrawString(font, modeSep,
            new Vector2(modeStartX + dummySize.X, modeY), Color.Gray);
        spriteBatch.DrawString(font, modeBot,
            new Vector2(modeStartX + dummySize.X + sepSize.X, modeY), botColor);

        // ── 6. Confirm prompt (bottom of screen) ─────────────────────
        const string prompt = "SPACE: SELECT";
        var promptSize = font.MeasureString(prompt);
        spriteBatch.DrawString(font, prompt,
            new Vector2(centerX - promptSize.X / 2f, promptY),
            Color.LightGray);
    }

    public void Exit() { }
}
