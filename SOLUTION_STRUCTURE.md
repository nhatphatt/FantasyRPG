# Fantasy RPG — Solution Structure

```
FantasyRPG/
│
├── .gitattributes                    # Git LFS tracking rules
├── .gitignore                        # .NET + MonoGame ignores
├── AGENTS.md                         # AI coding assistant rules
├── FantasyRPG.sln                    # Solution file
├── Directory.Build.props             # Shared MSBuild properties
│
├── src/
│   ├── FantasyRPG.Core/              # *** SHARED CLASS LIBRARY (net8.0) ***
│   │   ├── FantasyRPG.Core.csproj
│   │   │
│   │   ├── GameRoot.cs               # Main Game class (inherits Game)
│   │   │
│   │   ├── Engine/                   # Framework-level systems (reusable)
│   │   │   ├── ECS/
│   │   │   │   ├── Entity.cs                # Entity = int ID + component bag
│   │   │   │   ├── IComponent.cs            # Marker interface
│   │   │   │   ├── ComponentPool`1.cs       # Generic object pool per component type
│   │   │   │   └── World.cs                 # Entity registry + query
│   │   │   │
│   │   │   ├── StateMachine/
│   │   │   │   ├── IState.cs                # Enter / Execute / Exit
│   │   │   │   ├── StateMachine`1.cs        # Generic FSM<TOwner>
│   │   │   │   └── Transition.cs            # Condition-based transition
│   │   │   │
│   │   │   ├── Graphics/
│   │   │   │   ├── PixelScaler.cs           # RenderTarget2D + PointClamp upscale
│   │   │   │   ├── Camera2D.cs              # Transform-based camera with bounds
│   │   │   │   ├── SpriteAnimation.cs       # Frame data (struct, zero-alloc)
│   │   │   │   └── SpriteSheet.cs           # Atlas region lookup
│   │   │   │
│   │   │   ├── Input/
│   │   │   │   ├── InputManager.cs          # Keyboard + Gamepad + Touch abstraction
│   │   │   │   └── InputAction.cs           # Named action bindings
│   │   │   │
│   │   │   ├── Audio/
│   │   │   │   └── AudioManager.cs          # SoundEffect + Song management
│   │   │   │
│   │   │   ├── Content/
│   │   │   │   ├── AssetManager.cs          # Centralized ContentManager wrapper
│   │   │   │   └── JsonLoader.cs            # System.Text.Json deserializer
│   │   │   │
│   │   │   ├── Physics/
│   │   │   │   ├── AABB.cs                  # Axis-aligned bounding box (struct)
│   │   │   │   └── CollisionResolver.cs     # Spatial grid / sweep
│   │   │   │
│   │   │   └── Tiling/
│   │   │       ├── TileMap.cs               # 2D tile array
│   │   │       └── TileMapRenderer.cs       # Culled tile rendering
│   │   │
│   │   ├── Components/               # Game-specific components (structs preferred)
│   │   │   ├── TransformComponent.cs        # Position, Rotation, Scale
│   │   │   ├── SpriteComponent.cs           # Texture region + flip + layer
│   │   │   ├── AnimationComponent.cs        # Current anim, frame index, timer
│   │   │   ├── HealthComponent.cs           # HP, MaxHP, Invincibility timer
│   │   │   ├── ColliderComponent.cs         # AABB offset + size
│   │   │   ├── VelocityComponent.cs         # Dx, Dy
│   │   │   └── CombatComponent.cs           # Attack damage, cooldown, range
│   │   │
│   │   ├── Systems/                   # Systems that operate on component queries
│   │   │   ├── MovementSystem.cs
│   │   │   ├── AnimationSystem.cs
│   │   │   ├── CollisionSystem.cs
│   │   │   ├── CombatSystem.cs
│   │   │   ├── RenderSystem.cs
│   │   │   └── AISystem.cs
│   │   │
│   │   ├── GameStates/                # FSM states for the game loop
│   │   │   ├── IGameState.cs
│   │   │   ├── MainMenuState.cs
│   │   │   ├── GameplayState.cs
│   │   │   ├── PauseState.cs
│   │   │   └── GameOverState.cs
│   │   │
│   │   ├── Data/                      # Data definitions (POCOs for JSON deserialization)
│   │   │   ├── EntityDefinition.cs
│   │   │   ├── WaveDefinition.cs
│   │   │   └── DialogueDefinition.cs
│   │   │
│   │   └── Utilities/
│   │       ├── MathHelper2D.cs              # Fixed-point / pixel-snap helpers
│   │       ├── ObjectPool`1.cs              # Generic zero-alloc object pool
│   │       └── Timer.cs                     # Cooldown / delay (struct)
│   │
│   ├── FantasyRPG.DesktopGL/         # *** PLATFORM: Windows / Mac / Linux ***
│   │   ├── FantasyRPG.DesktopGL.csproj      # References Core + MonoGame.Framework.DesktopGL
│   │   ├── Program.cs                       # Entry point → new GameRoot()
│   │   ├── Icon.ico
│   │   └── Content/
│   │       └── Content.mgcb                 # MGCB pipeline definition
│   │
│   ├── FantasyRPG.Android/           # *** PLATFORM: Android ***
│   │   ├── FantasyRPG.Android.csproj        # References Core + MonoGame.Framework.Android
│   │   ├── Activity1.cs                     # Android entry point
│   │   ├── AndroidManifest.xml
│   │   └── Content/
│   │       └── Content.mgcb
│   │
│   └── FantasyRPG.iOS/               # *** PLATFORM: iOS ***
│       ├── FantasyRPG.iOS.csproj            # References Core + MonoGame.Framework.iOS
│       ├── Program.cs                       # iOS entry point
│       ├── Info.plist
│       └── Content/
│           └── Content.mgcb
│
├── content/                           # *** RAW ASSETS (tracked by Git LFS) ***
│   ├── sprites/
│   │   ├── knight/                          # Player character sprite sheets
│   │   ├── witch/                           # Player character sprite sheets
│   │   ├── enemies/
│   │   └── ui/
│   ├── tilesets/
│   ├── audio/
│   │   ├── sfx/
│   │   └── music/
│   ├── fonts/
│   └── data/                                # JSON definitions
│       ├── entities.json
│       ├── waves.json
│       └── dialogues.json
│
├── tests/
│   └── FantasyRPG.Tests/             # Unit tests (xUnit)
│       └── FantasyRPG.Tests.csproj
│
└── tools/
    └── ContentPipeline/               # Custom MGCB importers/processors (if needed)
        └── FantasyRPG.Pipeline.csproj
```

## Key Architectural Decisions

| Decision | Rationale |
|---|---|
| `Core` is a plain `net8.0` class library | 100% of gameplay code lives here. Platform projects are thin shells. |
| Components are `struct` where possible | Cache-friendly, zero-GC, stored in contiguous `ComponentPool<T>` arrays. |
| `Engine/` vs `Systems/` separation | Engine = framework-agnostic plumbing. Systems = game-specific logic. |
| `content/` (raw) vs `Content/` (compiled) | Raw assets in LFS. MGCB compiles them into each platform's `Content/` folder. |
| `Directory.Build.props` | Enforces `<LangVersion>12</LangVersion>`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` globally. |
