using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// Logs fighter state transitions to the console window for debugging.
/// Also renders the current state name + frame counter on screen using
/// the 1×1 pixel texture (no SpriteFont required).
///
/// Tracks the previous state to detect transitions and log them
/// at the exact frame they occur. Zero allocation — compares enum values.
/// </summary>
public static class DebugStateLogger
{
    private static bool _enabled = false;
    private static FighterStateId _prevState;
    private static int _prevFrame;
    private static int _tickCount;

    public static bool Enabled { get => _enabled; set => _enabled = value; }

    /// <summary>
    /// Call once per frame after all systems have run.
    /// Logs state transitions to Console and tracks frame counts.
    /// entityIndex = which entity to watch (0 = player).
    /// </summary>
    public static void LogState(
        ReadOnlySpan<CombatComponent> combats,
        ReadOnlySpan<TransformComponent> transforms,
        ReadOnlySpan<InputBufferComponent> inputBuffers,
        int entityIndex)
    {
        if (!_enabled)
            return;

        ref readonly CombatComponent combat = ref combats[entityIndex];
        ref readonly TransformComponent transform = ref transforms[entityIndex];

        _tickCount++;

        // ── Detect and log state transitions ─────────────────────────
        if (combat.State != _prevState)
        {
            Console.WriteLine(
                $"[F{_tickCount,5}] STATE: {_prevState} → {combat.State}  " +
                $"(frame={combat.CurrentFrame}, " +
                $"grounded={transform.IsGrounded}, " +
                $"vel=({transform.Velocity.X:F0},{transform.Velocity.Y:F0}))");

            _prevState = combat.State;
        }

        // ── Log frame data for attack states ─────────────────────────
        if (combat.State == FighterStateId.AttackStartup
            || combat.State == FighterStateId.AttackActive
            || combat.State == FighterStateId.AttackRecovery)
        {
            if (combat.CurrentFrame != _prevFrame)
            {
                Console.WriteLine(
                    $"[F{_tickCount,5}]   {combat.State} frame {combat.CurrentFrame}" +
                    $"/{combat.ActiveFrameData.StartupFrames}+{combat.ActiveFrameData.ActiveFrames}+{combat.ActiveFrameData.RecoveryFrames}");
                _prevFrame = combat.CurrentFrame;
            }
        }
        else
        {
            _prevFrame = -1;
        }
    }

    /// <summary>
    /// Draws a colored state indicator bar and current state info on screen.
    /// Uses DebugDrawSystem pixel texture — no SpriteFont needed.
    /// Shows: [STATE NAME] as a colored bar + frame counter as dots.
    /// </summary>
    public static void DrawStateHUD(
        SpriteBatch spriteBatch,
        ReadOnlySpan<CombatComponent> combats,
        ReadOnlySpan<HealthComponent> healths,
        int entityCount)
    {
        if (!_enabled)
            return;

        // Draw a state panel at the top of the screen for each entity
        for (int i = 0; i < entityCount; i++)
        {
            ref readonly CombatComponent combat = ref combats[i];
            ref readonly HealthComponent health = ref healths[i];

            int panelX = 4 + i * 160;
            int panelY = 4;

            // Background
            DebugDrawSystem.DrawFilledRect(spriteBatch,
                panelX, panelY, 150, 20, new Color(0, 0, 0, 160));

            // State color bar (width proportional to frame progress)
            Color stateColor = GetStateColor(combat.State);
            int barWidth = 148;

            // For frame-counted states, show progress as a filling bar
            int progress = barWidth;
            if (combat.State >= FighterStateId.AttackStartup
                && combat.State <= FighterStateId.AttackRecovery)
            {
                int totalFrames = combat.ActiveFrameData.TotalFrames;
                if (totalFrames > 0)
                    progress = Math.Min(barWidth, barWidth * combat.CurrentFrame / totalFrames);
            }

            DebugDrawSystem.DrawFilledRect(spriteBatch,
                panelX + 1, panelY + 1, progress, 8, stateColor);

            // Frame counter as tick marks
            int maxTicks = Math.Min(combat.CurrentFrame, 30);
            for (int t = 0; t < maxTicks; t++)
            {
                DebugDrawSystem.DrawFilledRect(spriteBatch,
                    panelX + 1 + t * 5, panelY + 12, 3, 5, Color.White);
            }

            // Health under the panel
            int hpWidth = (int)(148 * health.Percentage);
            Color hpColor = health.Percentage > 0.5f ? Color.Lime : Color.Red;
            DebugDrawSystem.DrawFilledRect(spriteBatch,
                panelX + 1, panelY + 18, hpWidth, 2, hpColor);
        }
    }

    private static Color GetStateColor(FighterStateId state)
    {
        return state switch
        {
            FighterStateId.Idle => new Color(100, 100, 100),
            FighterStateId.Run => Color.Lime,
            FighterStateId.Airborne => Color.SkyBlue,
            FighterStateId.AttackStartup => new Color(255, 100, 0),  // Orange
            FighterStateId.AttackActive => Color.Red,
            FighterStateId.AttackRecovery => new Color(180, 60, 0),
            FighterStateId.ParryWindow => Color.Cyan,
            FighterStateId.ParrySuccess => new Color(0, 255, 255),
            FighterStateId.Blocking => Color.Magenta,
            FighterStateId.Blockstun => new Color(200, 100, 0),
            FighterStateId.DashStartup or FighterStateId.DashActive
                or FighterStateId.DashRecovery => Color.Yellow,
            FighterStateId.Hitstun => Color.Orange,
            _ => Color.Gray
        };
    }
}
