using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FantasyRPG.Core.Engine.Input;
using FantasyRPG.Core.Systems;

namespace FantasyRPG.Core.GameStates;

/// <summary>
/// Character selection screen. Player picks Knight (1) or Wizard (2).
/// The opponent automatically gets the other character.
/// Renders preview sprites side-by-side with selection indicator.
/// </summary>
public sealed class CharacterSelectState : IGameState
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;
    private readonly PlayState _playState;

    private readonly Texture2D _knightTexture;
    private readonly Texture2D _wizardTexture;
    private readonly int _knightFrameW;
    private readonly int _knightFrameH;
    private readonly int _wizardFrameW;
    private readonly int _wizardFrameH;

    private int _selectedIndex;  // 0 = Knight, 1 = Wizard
    private int _modeIndex;      // 0 = Dummy, 1 = Bot

    public CharacterSelectState(
        InputManager input,
        GameStateManager stateManager,
        PlayState playState,
        Texture2D knightTexture, int knightFrameW, int knightFrameH,
        Texture2D wizardTexture, int wizardFrameW, int wizardFrameH)
    {
        _input = input;
        _stateManager = stateManager;
        _playState = playState;
        _knightTexture = knightTexture;
        _knightFrameW = knightFrameW;
        _knightFrameH = knightFrameH;
        _wizardTexture = wizardTexture;
        _wizardFrameW = wizardFrameW;
        _wizardFrameH = wizardFrameH;
    }

    public void Enter()
    {
        _selectedIndex = 0;
        _modeIndex = 1; // Default to Bot mode
    }

    public void Update(GameTime gameTime)
    {
        // ── Character selection (Left/Right or D1/D2) ───────────────
        if (_input.IsKeyPressed(Keys.Left) || _input.IsKeyPressed(Keys.D1))
            _selectedIndex = 0;
        else if (_input.IsKeyPressed(Keys.Right) || _input.IsKeyPressed(Keys.D2))
            _selectedIndex = 1;

        // ── Mode toggle (Up/Down or D3/D4) ──────────────────────────
        if (_input.IsKeyPressed(Keys.Up) || _input.IsKeyPressed(Keys.D3))
            _modeIndex = 0;
        else if (_input.IsKeyPressed(Keys.Down) || _input.IsKeyPressed(Keys.D4))
            _modeIndex = 1;

        if (_input.IsKeyPressed(Keys.Space))
        {
            _playState.SetPlayerCharacter(_selectedIndex);
            _playState.SetBotEnabled(_modeIndex == 1);
            _stateManager.ChangeState(GameStateId.Gameplay);
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var font = DebugDrawSystem.Font;
        if (font is null) return;

        int centerX = GameSettings.VirtualWidth / 2;
        int centerY = GameSettings.VirtualHeight / 2;

        // Title — scale 1.0 = native 14pt, clearly readable
        const string title = "SELECT CHARACTER";
        var titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title,
            new Vector2(centerX - titleSize.X / 2f, 20f),
            Color.Gold);

        // Character preview slots
        float previewScale = 0.35f;
        int slotSpacing = 140;
        int knightSlotX = centerX - slotSpacing / 2;
        int wizardSlotX = centerX + slotSpacing / 2;
        int slotY = centerY + 20;

        // Knight preview (first idle frame)
        var knightSrc = new Rectangle(0, 0, _knightFrameW, _knightFrameH);
        var knightOrigin = new Vector2(_knightFrameW / 2f, _knightFrameH);
        spriteBatch.Draw(_knightTexture, new Vector2(knightSlotX, slotY),
            knightSrc, Color.White, 0f, knightOrigin, previewScale, SpriteEffects.None, 0f);

        // Wizard preview (scaled to match knight height)
        var wizardSrc = new Rectangle(0, 0, _wizardFrameW, _wizardFrameH);
        var wizardOrigin = new Vector2(_wizardFrameW / 2f, _wizardFrameH);
        float wizardPreviewScale = previewScale * _knightFrameH / (float)_wizardFrameH;
        spriteBatch.Draw(_wizardTexture, new Vector2(wizardSlotX, slotY),
            wizardSrc, Color.White, 0f, wizardOrigin, wizardPreviewScale, SpriteEffects.None, 0f);

        // Labels below characters
        const string knightLabel = "1: KNIGHT";
        const string wizardLabel = "2: WIZARD";
        float labelScale = 0.7f;
        var knightLabelSize = font.MeasureString(knightLabel) * labelScale;
        var wizardLabelSize = font.MeasureString(wizardLabel) * labelScale;

        spriteBatch.DrawString(font, knightLabel,
            new Vector2(knightSlotX - knightLabelSize.X / 2f, slotY + 6f),
            Color.White, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, wizardLabel,
            new Vector2(wizardSlotX - wizardLabelSize.X / 2f, slotY + 6f),
            Color.White, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);

        // Selection indicator (yellow box around selected)
        int selX = _selectedIndex == 0 ? knightSlotX : wizardSlotX;
        int boxW = 60;
        int boxH = 80;
        DebugDrawSystem.DrawHollowRect(spriteBatch,
            selX - boxW / 2, slotY - boxH, boxW, boxH, Color.Yellow, 2);

        // Mode selector (above prompt)
        string modeText = _modeIndex == 0 ? "3: DUMMY" : "4: BOT";
        float modeScale = 0.7f;
        var modeSize = font.MeasureString(modeText) * modeScale;
        Color modeColor = _modeIndex == 1 ? Color.OrangeRed : Color.Gray;
        spriteBatch.DrawString(font, modeText,
            new Vector2(centerX - modeSize.X / 2f, GameSettings.VirtualHeight - 50f),
            modeColor, 0f, Vector2.Zero, modeScale, SpriteEffects.None, 0f);

        // Prompt at bottom
        const string prompt = "SPACE: SELECT";
        float promptScale = 0.7f;
        var promptSize = font.MeasureString(prompt) * promptScale;
        spriteBatch.DrawString(font, prompt,
            new Vector2(centerX - promptSize.X / 2f, GameSettings.VirtualHeight - 30f),
            Color.LightGray, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
    }

    public void Exit() { }
}
