using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FantasyRPG.Core.Engine.Graphics;
using FantasyRPG.Core.Engine.Input;
using FantasyRPG.Core.GameStates;
using FantasyRPG.Core.Systems;

namespace FantasyRPG.Core;

/// <summary>
/// The root Game class. Owns the render pipeline, game state manager,
/// and the master Update/Draw loop. Platform projects instantiate this.
/// </summary>
public sealed class GameRoot : Game
{
    // ── Graphics plumbing (created once, never reallocated) ──────────
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _renderTarget = null!;

    // ── Core systems (initialized in Initialize/LoadContent) ─────────
    private PixelScaler _pixelScaler = null!;
    private InputManager _inputManager = null!;
    private GameStateManager _gameStateManager = null!;

    // ── Pre-computed draw destination (avoids per-frame Rectangle alloc) ─
    private Rectangle _destinationRect;

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameSettings.WindowWidth,
            PreferredBackBufferHeight = GameSettings.WindowHeight,
            SynchronizeWithVerticalRetrace = true,
            GraphicsProfile = GraphicsProfile.HiDef,
            PreferMultiSampling = false
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / GameSettings.TargetFps);

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  INITIALIZE — Create systems, pre-allocate everything
    // ═══════════════════════════════════════════════════════════════════
    protected override void Initialize()
    {
        _inputManager = new InputManager();
        _gameStateManager = new GameStateManager();

        base.Initialize();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LOAD CONTENT — One-time asset loading, RenderTarget creation
    // ═══════════════════════════════════════════════════════════════════
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create the low-resolution render target (the "virtual screen")
        _renderTarget = new RenderTarget2D(
            GraphicsDevice,
            GameSettings.VirtualWidth,
            GameSettings.VirtualHeight,
            mipMap: false,
            preferredFormat: SurfaceFormat.Color,
            preferredDepthFormat: DepthFormat.None,
            preferredMultiSampleCount: 0,
            usage: RenderTargetUsage.DiscardContents);

        _pixelScaler = new PixelScaler(_renderTarget, GraphicsDevice);

        // Initialize debug draw system (creates 1×1 pixel texture)
        DebugDrawSystem.Initialize(GraphicsDevice);

        // Load debug SpriteFont for on-screen text labels
        DebugDrawSystem.LoadFont(Content.Load<SpriteFont>("Arial"));

        // Compute initial letterboxed destination
        RecalculateDestinationRect();

        // ── Register all game states (pre-allocated, zero future allocs) ──
        var playState = new PlayState(_inputManager, Content);

        _gameStateManager.RegisterState(GameStateId.Gameplay, playState);

        _gameStateManager.RegisterState(GameStateId.CharacterSelect,
            new CharacterSelectState(
                _inputManager, _gameStateManager, playState,
                playState.KnightTexture, playState.KnightFrameW, playState.KnightFrameH,
                playState.WizardTexture, playState.WizardFrameW, playState.WizardFrameH));

        _gameStateManager.ChangeState(GameStateId.CharacterSelect);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UPDATE — The sacred loop. ZERO allocations allowed here.
    // ═══════════════════════════════════════════════════════════════════
    protected override void Update(GameTime gameTime)
    {
        _inputManager.Update();
        _gameStateManager.Update(gameTime);

        base.Update(gameTime);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DRAW — Pixel-perfect two-pass rendering pipeline.
    // ═══════════════════════════════════════════════════════════════════
    protected override void Draw(GameTime gameTime)
    {
        // ── PASS 1: Draw the game world to the low-res render target ──
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(GameSettings.ClearColor);

        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            depthStencilState: DepthStencilState.None,
            rasterizerState: RasterizerState.CullCounterClockwise,
            effect: null,
            transformMatrix: null);

        _gameStateManager.Draw(gameTime, _spriteBatch);

        _spriteBatch.End();

        // ── PASS 2: Upscale the render target to the backbuffer ───────
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black); // Letterbox bars

        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.Opaque,
            samplerState: SamplerState.PointClamp, // CRISP pixel upscale
            depthStencilState: DepthStencilState.None,
            rasterizerState: RasterizerState.CullCounterClockwise);

        _spriteBatch.Draw(
            texture: _renderTarget,
            destinationRectangle: _destinationRect,
            sourceRectangle: null,
            color: Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UNLOAD — Dispose GPU resources
    // ═══════════════════════════════════════════════════════════════════
    protected override void UnloadContent()
    {
        DebugDrawSystem.Dispose();
        _renderTarget.Dispose();
        _spriteBatch.Dispose();

        base.UnloadContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  WINDOW RESIZE — Recalculate letterbox (event, not per-frame)
    // ═══════════════════════════════════════════════════════════════════
    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        RecalculateDestinationRect();
    }

    /// <summary>
    /// Calculates the largest integer-scaled (or aspect-correct) rectangle
    /// that fits the virtual resolution into the current window, with
    /// letterboxing on the remaining edges. Result is cached in
    /// <see cref="_destinationRect"/> — NO per-frame allocation.
    /// </summary>
    private void RecalculateDestinationRect()
    {
        int windowWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int windowHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        float scaleX = (float)windowWidth / GameSettings.VirtualWidth;
        float scaleY = (float)windowHeight / GameSettings.VirtualHeight;
        float scale = MathF.Min(scaleX, scaleY);

        int scaledWidth = (int)(GameSettings.VirtualWidth * scale);
        int scaledHeight = (int)(GameSettings.VirtualHeight * scale);

        int offsetX = (windowWidth - scaledWidth) / 2;
        int offsetY = (windowHeight - scaledHeight) / 2;

        _destinationRect = new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);
    }
}
