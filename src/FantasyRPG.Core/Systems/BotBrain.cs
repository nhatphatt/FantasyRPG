using System;
using FantasyRPG.Core.Components;

namespace FantasyRPG.Core.Systems;

/// <summary>
/// Simple fighting game AI that writes actions into an entity's InputBufferComponent.
/// Evaluates distance, opponent state, and its own state to pick actions.
/// Uses frame-based cooldowns and a seeded RNG — zero allocation, deterministic.
///
/// Decision flow each frame:
///   1. If in hitstun/blockstun/attack/dash → do nothing (locked out).
///   2. If opponent is attacking and bot is idle → chance to block or dash away.
///   3. If in attack range → chance to attack (jab or heavy).
///   4. If too far → walk toward opponent.
///   5. Random dash-in or jump to vary behavior.
/// </summary>
public struct BotBrain
{
    // ── Tuning Constants ─────────────────────────────────────────────
    private const float AttackRange = 55f;       // px — close enough to jab
    private const float HeavyRange = 45f;        // px — very close for heavy
    private const float ApproachRange = 180f;    // px — start walking toward
    private const float DashInRange = 140f;      // px — might dash to close gap
    private const float TooCloseRange = 30f;     // px — might back off
    private const int DecisionCooldown = 12;     // frames between decisions
    private const int ActionCooldownMin = 20;    // min frames between attacks
    private const int ActionCooldownMax = 45;    // max frames between attacks
    private const int BlockDuration = 30;        // max frames to hold block

    // ── Per-Bot State (no heap allocation) ───────────────────────────
    private int _decisionTimer;
    private int _actionCooldown;
    private int _blockTimer;
    private int _moveDirection;       // -1, 0, +1
    private bool _wantsBlock;
    private uint _rngState;

    /// <summary>
    /// Initialize the bot with a seed for its RNG. Call once during spawn.
    /// </summary>
    public static BotBrain Create(uint seed)
    {
        return new BotBrain
        {
            _rngState = seed == 0 ? 12345u : seed,
            _decisionTimer = 0,
            _actionCooldown = 0,
            _blockTimer = 0,
            _moveDirection = 0,
            _wantsBlock = false
        };
    }

    /// <summary>
    /// Evaluate the situation and write actions into the bot's input buffer.
    /// Called once per frame, BEFORE ConsumeBufferedActions.
    /// </summary>
    public void Update(
        ref InputBufferComponent buffer,
        in CombatComponent self,
        in TransformComponent selfTransform,
        in CombatComponent opponent,
        in TransformComponent opponentTransform,
        in FrameData[] selfMoveSet,
        in PhysicsComponent selfPhys)
    {
        buffer.AgeBuffer();

        // ── Locked-out states: let the combat system handle these ────
        if (IsLockedOut(self.State))
        {
            // If blocking and timer expired, stop blocking (handled by ConsumeBot)
            if (self.State == FighterStateId.Blocking)
            {
                _blockTimer--;
                if (_blockTimer <= 0)
                    _wantsBlock = false;
            }
            return;
        }

        // ── Tick cooldowns ───────────────────────────────────────────
        if (_decisionTimer > 0) _decisionTimer--;
        if (_actionCooldown > 0) _actionCooldown--;

        // ── Compute situation ────────────────────────────────────────
        float dx = opponentTransform.Position.X - selfTransform.Position.X;
        float dist = MathF.Abs(dx);
        int dirToOpponent = dx >= 0f ? 1 : -1;
        bool opponentAttacking = IsAttacking(opponent.State);
        bool grounded = selfTransform.IsGrounded;

        // ── Reset movement each frame ────────────────────────────────
        _moveDirection = 0;
        _wantsBlock = false;

        // ── REACTIVE: opponent is attacking → block or dash ──────────
        if (opponentAttacking && dist < ApproachRange && _decisionTimer <= 0)
        {
            int roll = NextRandom(100);
            if (roll < 50)
            {
                // Block
                buffer.RecordInput(InputAction.Block);
                _wantsBlock = true;
                _blockTimer = BlockDuration;
                _decisionTimer = DecisionCooldown;
                return;
            }
            else if (roll < 75 && grounded)
            {
                // Dash away
                buffer.RecordInput(InputAction.Dash);
                _moveDirection = -dirToOpponent;
                _decisionTimer = DecisionCooldown * 2;
                return;
            }
        }

        // ── OFFENSIVE: in attack range and cooldown expired ──────────
        if (dist < AttackRange && _actionCooldown <= 0 && grounded && _decisionTimer <= 0)
        {
            int roll = NextRandom(100);
            if (roll < 45)
            {
                buffer.RecordInput(InputAction.Attack);
                _actionCooldown = ActionCooldownMin + NextRandom(ActionCooldownMax - ActionCooldownMin);
                _decisionTimer = DecisionCooldown;
                return;
            }
            else if (roll < 65 && dist < HeavyRange)
            {
                buffer.RecordInput(InputAction.Special);
                _actionCooldown = ActionCooldownMin + NextRandom(ActionCooldownMax - ActionCooldownMin);
                _decisionTimer = DecisionCooldown;
                return;
            }
        }

        // ── APPROACH: walk toward opponent ───────────────────────────
        if (dist > AttackRange)
        {
            _moveDirection = dirToOpponent;

            // Chance to dash in from mid-range
            if (dist > DashInRange && grounded && _decisionTimer <= 0 && NextRandom(100) < 8)
            {
                buffer.RecordInput(InputAction.Dash);
                _moveDirection = dirToOpponent;
                _decisionTimer = DecisionCooldown * 2;
                return;
            }

            // Chance to jump in
            if (grounded && _decisionTimer <= 0 && NextRandom(100) < 5)
            {
                buffer.RecordInput(InputAction.Jump);
                _decisionTimer = DecisionCooldown;
                return;
            }
        }

        // ── TOO CLOSE: sometimes back off ───────────────────────────
        if (dist < TooCloseRange && _decisionTimer <= 0 && NextRandom(100) < 20)
        {
            _moveDirection = -dirToOpponent;
        }
    }

    /// <summary>
    /// Returns the desired movement direction for this frame (-1, 0, +1).
    /// Called by PlayState to apply velocity to the bot entity.
    /// </summary>
    public readonly int GetMoveDirection() => _moveDirection;

    /// <summary>
    /// Returns true if the bot wants to keep holding block this frame.
    /// </summary>
    public readonly bool WantsBlock() => _wantsBlock;

    // ── Helpers ──────────────────────────────────────────────────────

    private static bool IsLockedOut(FighterStateId state)
    {
        return state == FighterStateId.Hitstun
            || state == FighterStateId.Blockstun
            || state == FighterStateId.AttackStartup
            || state == FighterStateId.AttackActive
            || state == FighterStateId.AttackRecovery
            || state == FighterStateId.DashStartup
            || state == FighterStateId.DashActive
            || state == FighterStateId.DashRecovery
            || state == FighterStateId.ParryWindow
            || state == FighterStateId.ParrySuccess
            || state == FighterStateId.Blocking;
    }

    private static bool IsAttacking(FighterStateId state)
    {
        return state == FighterStateId.AttackStartup
            || state == FighterStateId.AttackActive;
    }

    /// <summary>
    /// Xorshift32 PRNG — fast, zero-allocation, deterministic.
    /// </summary>
    private int NextRandom(int max)
    {
        _rngState ^= _rngState << 13;
        _rngState ^= _rngState >> 17;
        _rngState ^= _rngState << 5;
        return (int)(_rngState % (uint)max);
    }
}
