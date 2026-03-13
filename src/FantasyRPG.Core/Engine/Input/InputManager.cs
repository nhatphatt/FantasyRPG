using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FantasyRPG.Core.Engine.Input;

/// <summary>
/// Snapshot-based input manager. Captures keyboard and gamepad state once
/// per frame. Provides pressed/released/held queries with zero allocations.
/// Touch input can be layered in via platform-specific extension.
/// </summary>
public sealed class InputManager
{
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;
    private GamePadState _currentGamePad;
    private GamePadState _previousGamePad;

    /// <summary>
    /// Call exactly once per frame, at the TOP of Update(), before any systems read input.
    /// </summary>
    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _currentKeyboard = Keyboard.GetState();

        _previousGamePad = _currentGamePad;
        _currentGamePad = GamePad.GetState(PlayerIndex.One);
    }

    // ── Keyboard ─────────────────────────────────────────────────────

    public bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);
    public bool IsKeyUp(Keys key) => _currentKeyboard.IsKeyUp(key);

    /// <summary>True only on the frame the key transitions from up → down.</summary>
    public bool IsKeyPressed(Keys key) =>
        _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    /// <summary>True only on the frame the key transitions from down → up.</summary>
    public bool IsKeyReleased(Keys key) =>
        _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

    // ── GamePad ──────────────────────────────────────────────────────

    public bool IsButtonDown(Buttons button) => _currentGamePad.IsButtonDown(button);
    public bool IsButtonUp(Buttons button) => _currentGamePad.IsButtonUp(button);

    public bool IsButtonPressed(Buttons button) =>
        _currentGamePad.IsButtonDown(button) && _previousGamePad.IsButtonUp(button);

    public bool IsButtonReleased(Buttons button) =>
        _currentGamePad.IsButtonUp(button) && _previousGamePad.IsButtonDown(button);

    /// <summary>Returns the left thumbstick as a Vector2 (deadzone already applied by MonoGame).</summary>
    public Vector2 LeftStick => _currentGamePad.ThumbSticks.Left;

    /// <summary>
    /// Returns a normalized movement vector from WASD/Arrow keys.
    /// Zero-allocation — constructs a stack-only Vector2.
    /// </summary>
    public Vector2 GetKeyboardMovementAxis()
    {
        float x = 0f;
        float y = 0f;

        if (_currentKeyboard.IsKeyDown(Keys.A) || _currentKeyboard.IsKeyDown(Keys.Left))
            x -= 1f;
        if (_currentKeyboard.IsKeyDown(Keys.D) || _currentKeyboard.IsKeyDown(Keys.Right))
            x += 1f;
        if (_currentKeyboard.IsKeyDown(Keys.W) || _currentKeyboard.IsKeyDown(Keys.Up))
            y -= 1f;
        if (_currentKeyboard.IsKeyDown(Keys.S) || _currentKeyboard.IsKeyDown(Keys.Down))
            y += 1f;

        if (x != 0f || y != 0f)
        {
            Vector2 dir = new(x, y);
            dir.Normalize();
            return dir;
        }

        return Vector2.Zero;
    }
}
