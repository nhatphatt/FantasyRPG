using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FantasyRPG.Core.Components;
using FantasyRPG.Core.Data;
using FantasyRPG.Core.Engine.Input;
using FantasyRPG.Core.Systems;

namespace FantasyRPG.Core.GameStates;

/// <summary>
/// The Gray Box Playground. A training room with:
///   - Entity 0: Player (keyboard-controlled fighter)
///   - Entity 1: Dummy (stands still, takes hits)
///   - Static floor platform
///
/// FIXED execution order (resolves Stuck Jump + Dead Inputs):
///   1. PhysicsSystem        — gravity, velocity, floor collision → sets IsGrounded
///   2. GroundingSyncSystem   — IsGrounded → combat state Airborne→Idle (THE FIX)
///   3. ProcessPlayerInput    — keyboard → input buffer
///   4. ConsumeBufferedActions — buffer → combat state transitions
///   5. CombatSystem.AdvanceFrames — tick frame counters, auto-transition
///   6. CombatSystem.ResolveCollisions + ApplyResults — hitbox/hurtbox
///   7. AnimationSystem       — state → animation → source rect
///   8. RenderSystem + DebugDraw — draw sprites + optional overlays
/// </summary>
public sealed class PlayState : IGameState
{
    // ── Entity Count ─────────────────────────────────────────────────
    private const int EntityCount = 2;
    private const int PlayerIndex = 0;
    private const int DummyIndex = 1;

    // ── Component Arrays (SOA layout, pre-allocated) ─────────────────
    private readonly TransformComponent[] _transforms = new TransformComponent[EntityCount];
    private readonly PhysicsComponent[] _physics = new PhysicsComponent[EntityCount];
    private readonly CombatComponent[] _combats = new CombatComponent[EntityCount];
    private readonly HitboxComponent[] _hitboxes = new HitboxComponent[EntityCount];
    private readonly HurtboxComponent[] _hurtboxes = new HurtboxComponent[EntityCount];
    private readonly HealthComponent[] _healths = new HealthComponent[EntityCount];
    private readonly InputBufferComponent[] _inputBuffers = new InputBufferComponent[EntityCount];
    private readonly SpriteComponent[] _sprites = new SpriteComponent[EntityCount];
    private readonly AnimatorComponent[] _animators = new AnimatorComponent[EntityCount];

    // ── Stage Platforms ──────────────────────────────────────────────
    private const int MaxPlatforms = 4;
    private readonly AABB[] _platforms = new AABB[MaxPlatforms];
    private int _platformCount;

    // ── Per-Entity Character Data (set during spawn) ───────────────
    private readonly FrameData[][] _moveSets = new FrameData[EntityCount][];
    private readonly HitboxDefinition[][] _hitboxDefs = new HitboxDefinition[EntityCount][];

    // ── Bot AI ──────────────────────────────────────────────────────
    private BotBrain _botBrain;
    private bool _botEnabled;

    // ── Dependencies ─────────────────────────────────────────────────
    private readonly InputManager _input;

    // ── Target rendered height (pixels on virtual canvas) ──────────────
    private const float TargetRenderHeight = 45f;

    // ── Loaded Assets (set once in constructor, never changed) ────────
    private readonly Texture2D _knightTexture;
    private readonly int _knightFrameW;
    private readonly int _knightFrameH;
    private readonly float _knightScale;
    private readonly AnimationData[] _knightAnims;

    private readonly Texture2D _wizardTexture;
    private readonly int _wizardFrameW;
    private readonly int _wizardFrameH;
    private readonly float _wizardScale;
    private readonly AnimationData[] _wizardAnims;

    private readonly Texture2D _bgTexture;

    // ── Character Selection (0 = Knight, 1 = Wizard) ─────────────────
    private int _playerCharacter;

    public Texture2D KnightTexture => _knightTexture;
    public int KnightFrameW => _knightFrameW;
    public int KnightFrameH => _knightFrameH;
    public Texture2D WizardTexture => _wizardTexture;
    public int WizardFrameW => _wizardFrameW;
    public int WizardFrameH => _wizardFrameH;

    public PlayState(InputManager input, ContentManager content)
    {
        _input = input;

        // ── Load Knight assets ──────────────────────────────────────
        _knightTexture = content.Load<Texture2D>("knight_sprite");
        _knightFrameW = _knightTexture.Width / KnightData.SheetColumns;
        _knightFrameH = _knightTexture.Height / KnightData.SheetRows;
        _knightScale = TargetRenderHeight / _knightFrameH;
        _knightAnims = KnightData.CreateAnimations();

        // ── Load Wizard assets ──────────────────────────────────────
        _wizardTexture = content.Load<Texture2D>("wizard_sprite");
        _wizardFrameW = _wizardTexture.Width / WizardData.SheetColumns;
        _wizardFrameH = _wizardTexture.Height / WizardData.SheetRows;
        _wizardScale = TargetRenderHeight / _wizardFrameH;
        _wizardAnims = WizardData.CreateAnimations();

        // ── Load Background ─────────────────────────────────────────
        _bgTexture = content.Load<Texture2D>("bg_stage");
    }

    /// <summary>
    /// Set which character the player controls (0 = Knight, 1 = Wizard).
    /// Called by CharacterSelectState before transitioning to Gameplay.
    /// </summary>
    public void SetPlayerCharacter(int characterIndex)
    {
        _playerCharacter = characterIndex;
    }

    /// <summary>
    /// Enable or disable bot AI for the opponent entity.
    /// Called by CharacterSelectState or ModeSelectState before transitioning.
    /// </summary>
    public void SetBotEnabled(bool enabled)
    {
        _botEnabled = enabled;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ENTER — Spawn entities, configure stage
    // ═══════════════════════════════════════════════════════════════════
    public void Enter()
    {
        _platforms[0] = new AABB(
            center: new Vector2(GameSettings.VirtualWidth / 2f, 230f),
            halfSize: new Vector2(GameSettings.VirtualWidth / 2f, 8f));
        _platformCount = 1;

        PhysicsSystem.SetPlatforms(_platforms, _platformCount);

        // ── Initialize bot AI ───────────────────────────────────────
        _botBrain = BotBrain.Create(42u);

        // ── Spawn based on character selection ──────────────────────
        if (_playerCharacter == 0)
        {
            // Player = Knight, Opponent = Wizard
            SpawnKnight(PlayerIndex, new Vector2(160f, 222f), 1);
            SpawnWizard(DummyIndex, new Vector2(210f, 222f), -1);
            CombatSystem.ConfigureDash(
                KnightData.DashStartupFrames,
                KnightData.DashActiveFrames,
                KnightData.DashRecoveryFrames);
        }
        else
        {
            // Player = Wizard, Opponent = Knight
            SpawnWizard(PlayerIndex, new Vector2(160f, 222f), 1);
            SpawnKnight(DummyIndex, new Vector2(210f, 222f), -1);
            CombatSystem.ConfigureDash(
                WizardData.DashStartupFrames,
                WizardData.DashActiveFrames,
                WizardData.DashRecoveryFrames);
        }
    }

    private void SpawnKnight(int index, Vector2 position, int facingDirection)
    {
        var moveSet = KnightData.CreateMoveSet();
        var hitboxDefs = KnightData.CreateHitboxDefs();
        _moveSets[index] = moveSet;
        _hitboxDefs[index] = hitboxDefs;

        SpawnFighterCore(index, position, facingDirection,
            _knightTexture, _knightFrameW, _knightFrameH, _knightAnims,
            hitboxDefs, KnightData.ParryWindowFrames, 100f, _knightScale);

        _physics[index].DashSpeed = KnightData.DashSpeed;
    }

    private void SpawnWizard(int index, Vector2 position, int facingDirection)
    {
        var moveSet = WizardData.CreateMoveSet();
        var hitboxDefs = WizardData.CreateHitboxDefs();
        _moveSets[index] = moveSet;
        _hitboxDefs[index] = hitboxDefs;

        // Configure dash for wizard when spawning player
        if (index == PlayerIndex)
        {
            CombatSystem.ConfigureDash(
                WizardData.DashStartupFrames,
                WizardData.DashActiveFrames,
                WizardData.DashRecoveryFrames);
        }

        SpawnFighterCore(index, position, facingDirection,
            _wizardTexture, _wizardFrameW, _wizardFrameH, _wizardAnims,
            hitboxDefs, WizardData.ParryWindowFrames, WizardData.WizardHP, _wizardScale);

        // Override dash speed for the Wizard (teleport is faster)
        _physics[index].DashSpeed = WizardData.DashSpeed;
    }

    private void SpawnFighterCore(
        int index, Vector2 position, int facingDirection,
        Texture2D texture, int frameW, int frameH, AnimationData[] anims,
        HitboxDefinition[] hitboxDefs, int parryFrames, float hp, float scale)
    {
        _transforms[index] = new TransformComponent
        {
            Position = position,
            Velocity = Vector2.Zero,
            IsGrounded = true
        };

        _physics[index] = PhysicsComponent.CreateDefault();

        _combats[index] = new CombatComponent
        {
            State = FighterStateId.Idle,
            CurrentFrame = 0,
            FacingDirection = facingDirection,
            ParryWindowFrames = parryFrames
        };

        _hitboxes[index] = new HitboxComponent
        {
            IsActive = false,
            MoveHitboxes = hitboxDefs
        };

        _hurtboxes[index] = new HurtboxComponent
        {
            LocalOffset = new Vector2(0f, -12f),
            HalfSize = new Vector2(7f, 12f),
            IsActive = true
        };

        _healths[index] = new HealthComponent
        {
            Current = hp,
            Max = hp,
            StockCount = 3
        };

        _inputBuffers[index] = default;

        _sprites[index] = new SpriteComponent
        {
            Texture = texture,
            FrameWidth = frameW,
            FrameHeight = frameH,
            SourceRect = new Rectangle(0, 0, frameW, frameH),
            Tint = Color.White,
            Origin = new Vector2(frameW / 2f, frameH),
            Scale = scale
        };

        _animators[index] = new AnimatorComponent
        {
            Animations = anims,
            CurrentAnimation = AnimationId.Idle,
            CurrentFrame = 0,
            FrameTimer = 0f,
            IsFinished = false
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UPDATE — FIXED execution order. ZERO allocations.
    // ═══════════════════════════════════════════════════════════════════
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  PHASE 1: PHYSICS (gravity, movement, floor collision)   ║
        // ║  Sets IsGrounded = true/false based on floor AABB.       ║
        // ╚═══════════════════════════════════════════════════════════╝
        PhysicsSystem.Update(
            _transforms.AsSpan(), _physics.AsSpan(),
            EntityCount, dt);

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  PHASE 2: GROUNDING SYNC (THE BUG FIX)                  ║
        // ║  If physics says grounded but combat says Airborne       ║
        // ║  → transition to Idle so fighter becomes actionable.     ║
        // ╚═══════════════════════════════════════════════════════════╝
        GroundingSyncSystem.Sync(
            _transforms.AsSpan(), _combats.AsSpan(),
            EntityCount);

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  PHASE 3: INPUT (keyboard → buffer → combat actions)     ║
        // ║  Now that state is correct, canAct checks work properly. ║
        // ╚═══════════════════════════════════════════════════════════╝
        ProcessPlayerInput();
        ConsumeBufferedActions();
        if (_botEnabled)
        {
            UpdateBot();
            ConsumeBotActions();
        }

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  PHASE 4: COMBAT (frame advance → hitbox → resolve)      ║
        // ╚═══════════════════════════════════════════════════════════╝
        CombatSystem.AdvanceFrames(
            _combats.AsSpan(), _hitboxes.AsSpan(), _hurtboxes.AsSpan(),
            EntityCount);

        CombatSystem.ResolveCollisions(
            _combats.AsSpan(), _hitboxes.AsSpan(), _hurtboxes.AsSpan(),
            _transforms.AsSpan(), EntityCount);

        CombatSystem.ApplyResults(
            _combats.AsSpan(), _healths.AsSpan(), _transforms.AsSpan());

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  PHASE 5: ANIMATION (state → anim clip → source rect)    ║
        // ╚═══════════════════════════════════════════════════════════╝
        AnimationSystem.Update(
            _animators.AsSpan(), _sprites.AsSpan(),
            _combats.AsSpan(), EntityCount, dt);

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  PHASE 6: HOUSEKEEPING                                   ║
        // ╚═══════════════════════════════════════════════════════════╝
        UpdateFacing();
        ResetDummyIfDead();

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  PHASE 7: DEBUG LOGGING (state transitions to console)    ║
        // ╚═══════════════════════════════════════════════════════════╝
        DebugStateLogger.LogState(
            _combats.AsSpan(), _transforms.AsSpan(),
            _inputBuffers.AsSpan(), PlayerIndex);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DRAW — Sprites first, then optional debug overlay on top
    // ═══════════════════════════════════════════════════════════════════
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Draw background (stretched to virtual resolution, behind everything)
        RenderSystem.DrawBackground(spriteBatch, _bgTexture);

        // Draw entity sprites (skips if Texture is null)
        RenderSystem.Draw(
            spriteBatch,
            _transforms.AsSpan(),
            _sprites.AsSpan(),
            _combats.AsSpan(),
            EntityCount);

        // Draw debug overlays (toggle with F1)
        DebugDrawSystem.Draw(
            spriteBatch,
            _transforms.AsSpan(),
            _combats.AsSpan(),
            _hitboxes.AsSpan(),
            _hurtboxes.AsSpan(),
            _physics.AsSpan(),
            EntityCount);

        DrawHealthBars(spriteBatch);

        // Draw state label for debugging
        DrawStateIndicators(spriteBatch);

        // Draw on-screen text labels (state name + frame counter)
        DebugDrawSystem.DrawEntityLabels(
            spriteBatch,
            _transforms.AsSpan(), _combats.AsSpan(),
            EntityCount, PlayerIndex);

        // Draw debug state HUD panel (top of screen)
        DebugStateLogger.DrawStateHUD(
            spriteBatch,
            _combats.AsSpan(), _healths.AsSpan(),
            EntityCount);
    }

    public void Exit() { }

    // ═══════════════════════════════════════════════════════════════════
    //  INPUT PROCESSING
    // ═══════════════════════════════════════════════════════════════════

    private void ProcessPlayerInput()
    {
        // Toggle debug overlay with F1
        if (_input.IsKeyPressed(Keys.F1))
            DebugDrawSystem.Enabled = !DebugDrawSystem.Enabled;

        // Toggle debug state logger with F2
        if (_input.IsKeyPressed(Keys.F2))
            DebugStateLogger.Enabled = !DebugStateLogger.Enabled;

        ref InputBufferComponent buffer = ref _inputBuffers[PlayerIndex];
        buffer.AgeBuffer();

        InputAction action = InputAction.None;

        if (_input.IsKeyPressed(Keys.D1))
            action = InputAction.Attack;
        else if (_input.IsKeyPressed(Keys.D2))
            action = InputAction.Special;
        else if (_input.IsKeyPressed(Keys.D3))
            action = InputAction.Block;
        else if (_input.IsKeyPressed(Keys.D4))
            action = InputAction.Dash;
        else if (_input.IsKeyPressed(Keys.Space))
            action = InputAction.Jump;

        if (action != InputAction.None)
            buffer.RecordInput(action);
    }

    private void ConsumeBufferedActions()
    {
        ref CombatComponent combat = ref _combats[PlayerIndex];
        ref TransformComponent transform = ref _transforms[PlayerIndex];
        ref PhysicsComponent phys = ref _physics[PlayerIndex];
        ref InputBufferComponent buffer = ref _inputBuffers[PlayerIndex];

        // ── FIX: Exit Blocking state when key released ──────────────
        if (combat.State == FighterStateId.Blocking && !_input.IsKeyDown(Keys.D3))
        {
            combat.TransitionTo(FighterStateId.Idle);
        }

        // ── Movement (continuous, not buffered) ──────────────────────
        bool isActionable = combat.State == FighterStateId.Idle
                         || combat.State == FighterStateId.Run;

        if (isActionable)
        {
            float moveX = 0f;
            if (_input.IsKeyDown(Keys.Left))
                moveX -= 1f;
            if (_input.IsKeyDown(Keys.Right))
                moveX += 1f;

            if (moveX != 0f)
            {
                transform.Velocity.X = moveX * phys.MoveSpeed;
                combat.FacingDirection = moveX > 0f ? 1 : -1;
                if (combat.State == FighterStateId.Idle)
                    combat.State = FighterStateId.Run;
            }
            else if (combat.State == FighterStateId.Run)
            {
                combat.TransitionTo(FighterStateId.Idle);
            }
        }

        // ── Aerial movement (reduced control while airborne) ────────
        if (combat.State == FighterStateId.Airborne)
        {
            float airX = 0f;
            if (_input.IsKeyDown(Keys.Left))
                airX -= 1f;
            if (_input.IsKeyDown(Keys.Right))
                airX += 1f;

            if (airX != 0f)
            {
                transform.Velocity.X = airX * phys.MoveSpeed * 0.7f;
                combat.FacingDirection = airX > 0f ? 1 : -1;
            }
        }

        // ── Buffered Actions (consumed when actionable) ──────────────
        bool canAct = combat.State == FighterStateId.Idle
                   || combat.State == FighterStateId.Run;

        // Allow jump while airborne? No — but allow ground actions.
        if (canAct)
        {
            if (buffer.ConsumeAction(InputAction.Jump))
            {
                if (transform.IsGrounded)
                {
                    transform.Velocity.Y = phys.JumpForce;
                    transform.IsGrounded = false;
                    combat.TransitionTo(FighterStateId.Airborne);
                }
            }
            else if (buffer.ConsumeAction(InputAction.Attack))
            {
                combat.BeginAttack(0, in _moveSets[PlayerIndex][0]);
            }
            else if (buffer.ConsumeAction(InputAction.Special))
            {
                combat.BeginAttack(1, in _moveSets[PlayerIndex][1]);
            }
            else if (buffer.ConsumeAction(InputAction.Block))
            {
                combat.BeginParry(combat.ParryWindowFrames);
            }
            else if (buffer.ConsumeAction(InputAction.Dash))
            {
                combat.TransitionTo(FighterStateId.DashStartup);
                transform.Velocity.X = combat.FacingDirection * phys.DashSpeed;
            }
        }
    }

    private void UpdateFacing()
    {
        ref CombatComponent dummyCombat = ref _combats[DummyIndex];
        float diff = _transforms[PlayerIndex].Position.X - _transforms[DummyIndex].Position.X;
        dummyCombat.FacingDirection = diff >= 0f ? 1 : -1;
    }

    private void ResetDummyIfDead()
    {
        ref HealthComponent dummyHealth = ref _healths[DummyIndex];
        if (dummyHealth.IsDead)
        {
            dummyHealth.Current = dummyHealth.Max;
            _combats[DummyIndex].TransitionTo(FighterStateId.Idle);
            _transforms[DummyIndex].Position = new Vector2(210f, 222f);
            _transforms[DummyIndex].Velocity = Vector2.Zero;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BOT AI — Drives DummyIndex via InputBufferComponent
    // ═══════════════════════════════════════════════════════════════════

    private void UpdateBot()
    {
        _botBrain.Update(
            ref _inputBuffers[DummyIndex],
            in _combats[DummyIndex],
            in _transforms[DummyIndex],
            in _combats[PlayerIndex],
            in _transforms[PlayerIndex],
            _moveSets[DummyIndex],
            in _physics[DummyIndex]);
    }

    private void ConsumeBotActions()
    {
        ref CombatComponent combat = ref _combats[DummyIndex];
        ref TransformComponent transform = ref _transforms[DummyIndex];
        ref PhysicsComponent phys = ref _physics[DummyIndex];
        ref InputBufferComponent buffer = ref _inputBuffers[DummyIndex];

        // ── Release block when bot doesn't want it ──────────────────
        if (combat.State == FighterStateId.Blocking && !_botBrain.WantsBlock())
        {
            combat.TransitionTo(FighterStateId.Idle);
        }

        // ── Movement (continuous, driven by BotBrain) ───────────────
        bool isActionable = combat.State == FighterStateId.Idle
                         || combat.State == FighterStateId.Run;

        if (isActionable)
        {
            int moveDir = _botBrain.GetMoveDirection();
            if (moveDir != 0)
            {
                transform.Velocity.X = moveDir * phys.MoveSpeed;
                combat.FacingDirection = moveDir > 0 ? 1 : -1;
                if (combat.State == FighterStateId.Idle)
                    combat.State = FighterStateId.Run;
            }
            else if (combat.State == FighterStateId.Run)
            {
                combat.TransitionTo(FighterStateId.Idle);
            }
        }

        // ── Aerial drift ────────────────────────────────────────────
        if (combat.State == FighterStateId.Airborne)
        {
            int moveDir = _botBrain.GetMoveDirection();
            if (moveDir != 0)
            {
                transform.Velocity.X = moveDir * phys.MoveSpeed * 0.7f;
                combat.FacingDirection = moveDir > 0 ? 1 : -1;
            }
        }

        // ── Buffered Actions ────────────────────────────────────────
        bool canAct = combat.State == FighterStateId.Idle
                   || combat.State == FighterStateId.Run;

        if (canAct)
        {
            if (buffer.ConsumeAction(InputAction.Jump))
            {
                if (transform.IsGrounded)
                {
                    transform.Velocity.Y = phys.JumpForce;
                    transform.IsGrounded = false;
                    combat.TransitionTo(FighterStateId.Airborne);
                }
            }
            else if (buffer.ConsumeAction(InputAction.Attack))
            {
                combat.BeginAttack(0, in _moveSets[DummyIndex][0]);
            }
            else if (buffer.ConsumeAction(InputAction.Special))
            {
                combat.BeginAttack(1, in _moveSets[DummyIndex][1]);
            }
            else if (buffer.ConsumeAction(InputAction.Block))
            {
                combat.BeginParry(combat.ParryWindowFrames);
            }
            else if (buffer.ConsumeAction(InputAction.Dash))
            {
                combat.TransitionTo(FighterStateId.DashStartup);
                transform.Velocity.X = combat.FacingDirection * phys.DashSpeed;
            }
        }
    }

    private void DrawHealthBars(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < EntityCount; i++)
        {
            ref readonly TransformComponent t = ref _transforms[i];
            ref readonly HealthComponent h = ref _healths[i];

            const int barWidth = 24;
            const int barHeight = 3;
            int barX = (int)t.Position.X - barWidth / 2;
            int barY = (int)(t.Position.Y - 50);

            DebugDrawSystem.DrawFilledRect(spriteBatch,
                barX, barY, barWidth, barHeight, new Color(40, 10, 10));

            int fillWidth = (int)(barWidth * h.Percentage);
            Color fillColor = h.Percentage > 0.5f ? Color.Lime : Color.Red;
            if (fillWidth > 0)
            {
                DebugDrawSystem.DrawFilledRect(spriteBatch,
                    barX, barY, fillWidth, barHeight, fillColor);
            }
        }
    }

    /// <summary>
    /// Draws small colored indicators above each entity showing their current
    /// combat state. Uses DebugDrawSystem colors — zero allocation.
    /// </summary>
    private void DrawStateIndicators(SpriteBatch spriteBatch)
    {
        if (!DebugDrawSystem.Enabled)
            return;

        for (int i = 0; i < EntityCount; i++)
        {
            ref readonly TransformComponent t = ref _transforms[i];
            ref readonly CombatComponent c = ref _combats[i];

            int x = (int)t.Position.X - 12;
            int y = (int)(t.Position.Y - 36);

            // State color indicator bar
            Color stateColor = c.State switch
            {
                FighterStateId.Idle => Color.White,
                FighterStateId.Run => Color.Lime,
                FighterStateId.Airborne => Color.SkyBlue,
                FighterStateId.AttackStartup or FighterStateId.AttackActive
                    or FighterStateId.AttackRecovery => Color.Red,
                FighterStateId.ParryWindow or FighterStateId.ParrySuccess => Color.Cyan,
                FighterStateId.Blocking => Color.Magenta,
                FighterStateId.DashStartup or FighterStateId.DashActive
                    or FighterStateId.DashRecovery => Color.Yellow,
                FighterStateId.Hitstun or FighterStateId.Blockstun => Color.Orange,
                _ => Color.Gray
            };

            DebugDrawSystem.DrawFilledRect(spriteBatch, x, y, 24, 2, stateColor);
        }
    }
}
