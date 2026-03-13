# AGENTS.md — Fantasy RPG Platform Fighter (MonoGame) AI Coding Rules

> **This file governs ALL AI-assisted code generation in this repository.**
> Every assistant, agent, or copilot MUST read and obey these rules before writing any code.

---

## 1. PROJECT IDENTITY

- **Game**: 2D Side-Scrolling PvP Platform Fighter (Knight/Witch theme, Smash Bros / Brawlhalla style)
- **Framework**: MonoGame 3.8.2+ on .NET 8+, C# 12+
- **Platforms**: DesktopGL (Windows/Mac/Linux), Android (.NET for Android), iOS (.NET for iOS)
- **Architecture**: Code-First. ECS-Lite. Deterministic frame-based combat. 100% code control.

---

## 2. ABSOLUTE PERFORMANCE RULES (ZERO TOLERANCE)

### 2.1 Zero-Allocation Game Loop
- **NEVER** use `new` to allocate reference types inside `Update(GameTime)` or `Draw(GameTime)`.
- **NEVER** use LINQ (`.Where()`, `.Select()`, `.ToList()`) in the game loop. Use `for`/`foreach` over arrays/lists.
- **NEVER** use `string` concatenation (`+`, `$""`) in the game loop. Pre-allocate `StringBuilder` or use `Span<char>`.
- **NEVER** use `delegate`, `Action<T>`, `Func<T>`, or lambda closures in the game loop (they allocate).
- **ALWAYS** use object pools (`ObjectPool<T>`) for anything that must be created at runtime (projectiles, particles, VFX).
- **ALWAYS** use `struct` for small, frequently-created data (Vectors, Timers, AABB, FrameData).
- **ALWAYS** pass large structs by `ref` or `in` to avoid copy overhead.

### 2.2 Collections
- Use `T[]` (arrays) or `List<T>` pre-allocated with known capacity. Never use `Dictionary<K,V>` in hot paths.
- For lookup-heavy data that is loaded once, `FrozenDictionary<K,V>` or `Dictionary<K,V>` is acceptable during `LoadContent()`.
- Never call `.Add()` on a `List<T>` in the game loop unless it is a pooled collection with pre-allocated capacity.

### 2.3 Draw Call Discipline
- Minimize `SpriteBatch.Begin()` / `End()` pairs. Batch draws by texture atlas.
- Use sprite sheets/atlases, not individual texture files.
- Sort by `SpriteSortMode.Deferred` with manual back-to-front ordering via layer depth.

---

## 3. FIGHTING GAME ARCHITECTURE RULES (NON-NEGOTIABLE)

### 3.1 Deterministic Frame-Based Combat
- The game runs at **fixed 60fps** (`IsFixedTimeStep = true`). One `Update()` = one frame.
- ALL combat timing is expressed in **integer frame counts**, not float seconds.
- Every attack is defined by `FrameData`: `StartupFrames → ActiveFrames → RecoveryFrames`.
- `CombatComponent.CurrentFrame` is the single source of truth for "where am I in this action?"
- State transitions reset `CurrentFrame` to 0. Systems increment it each tick.

### 3.2 Hitbox / Hurtbox Separation (CRITICAL)
```
COLLISION DOMAINS (never mix these):

  ┌─ Environment Collision (Physics) ─┐    ┌─ Combat Collision (Damage) ──────┐
  │  ColliderComponent                 │    │  HitboxComponent  (attack area)  │
  │  Walls, floors, platforms          │    │  HurtboxComponent (vulnerable)   │
  │  Resolved by PhysicsSystem         │    │  Resolved by CombatSystem        │
  └────────────────────────────────────┘    └──────────────────────────────────┘
```
- **Hitboxes** = "Where does this attack deal damage?" Active ONLY during `AttackActive` frames.
- **Hurtboxes** = "Where can this entity be damaged?" Active unless i-frames (dash, parry success).
- Both use `AABB` (struct) intersection. Hitboxes are defined as local offsets, resolved to world-space per frame.
- A fighter's hitbox **CANNOT** hit their own hurtbox.

### 3.3 Combat Resolution Order (per frame)
```
1. InputBufferSystem     — Record raw input, age buffer
2. CombatSystem.AdvanceFrames   — Tick frame counters, auto-transition states
3. CombatSystem.ResolveCollisions — Test all hitbox↔hurtbox pairs
4. CombatSystem.ApplyResults     — Apply damage/hitstun/parry from pre-allocated buffer
5. PhysicsSystem         — Apply velocity, gravity, environment collision
6. RenderSystem          — Draw
```
This order is SACRED. Changing it will break combat determinism.

### 3.4 The Three Combat Outcomes
When an attacker's hitbox intersects a defender's hurtbox, classify:

| Defender State | Outcome | Effect |
|---|---|---|
| `ParryWindow` (within parry frames) | **PARRIED** | 0 damage. Attacker stunned for `ParryPunishFrames`. Defender invincible. |
| `Blocking` (held block, parry expired) | **BLOCKED** | 0 damage. Defender takes `BlockstunFrames`. 25% knockback pushback. |
| Any other state | **HIT** | Full `Damage`. `HitstunFrames` applied. Full `KnockbackForce` at `KnockbackAngle`. |

Priority: Parry > Block > Hit. Always check in this order.

### 3.5 Universal Parry System
- **Parry Window**: Exactly `GameSettings.ParryWindowFrames` frames (default: 3 = 50ms).
- Triggered by pressing Block. The first 3 frames are the parry window.
- If the parry window expires and Block is still held → transition to `Blocking` state.
- On **Parry Success**: Defender enters `ParrySuccess` (invincible freeze). Attacker enters `Hitstun`.
- Parry is the single universal defensive mechanic for ALL characters.

### 3.6 Input Buffering
- Store the last `InputBufferComponent.BufferSize` frames of input (default: 8 = 133ms).
- When a fighter becomes actionable (exits hitstun/recovery), check the buffer for pending actions.
- `ConsumeAction()` returns the MOST RECENT matching input and clears it.
- This ensures attacks/jumps/parries feel responsive even during animation lock.

### 3.7 Invincibility Frames (i-frames)
- During `DashActive` state: `HurtboxComponent.IsActive = false` and `CombatComponent.IsInvincible = true`.
- During `ParrySuccess` state: Same as above.
- The CombatSystem skips intersection tests when hurtbox is inactive or entity is invincible.

---

## 4. ECS-LITE ARCHITECTURE

### 4.1 Composition Over Inheritance
- Entities are integer IDs with attached Components. **Max 1 level of inheritance allowed**.
- Use interfaces (`IUpdatable`, `IDrawable`, `IDamageable`) for polymorphic behavior.

### 4.2 Component-Based Entity System
```
Fighter Entity (int ID)
  ├── TransformComponent    (struct) — Position, Velocity, IsGrounded
  ├── CombatComponent       (struct) — State, CurrentFrame, FrameData, FacingDirection
  ├── HitboxComponent       (struct) — Attack volume, per-move definitions
  ├── HurtboxComponent      (struct) — Vulnerable volume, IsActive toggle
  ├── HealthComponent       (struct) — HP, Stocks
  ├── InputBufferComponent  (struct) — 8-frame ring buffer
  ├── SpriteComponent       (struct) — Texture region, flip, layer
  └── AnimationComponent    (struct) — Current anim, frame index, timer
```
- **Components** = Pure data. No methods beyond simple property access and trivial helpers.
- **Systems** = Stateless processors that iterate over component `Span<T>`. Zero allocation.
- Systems receive entity count and operate on flat arrays via `Span<T>`.

### 4.3 State Machines
- **Game States**: `IGameState` with `Enter()`, `Update(GameTime)`, `Draw(GameTime, SpriteBatch)`, `Exit()`.
- **Fighter Combat States**: Driven by `FighterStateId` enum + `CombatComponent.CurrentFrame`. The CombatSystem IS the state machine — it evaluates frame data and transitions states automatically.
- **Entity Animation States**: `StateMachine<TOwner>` for non-combat state logic.
- All states are pre-allocated during initialization. State transitions MUST NOT allocate.

---

## 5. DATA-DRIVEN DESIGN

### 5.1 Frame Data in JSON
```json
{
  "knight": {
    "jab": { "startup": 4, "active": 3, "recovery": 10, "damage": 8, "hitstun": 12, "blockstun": 6, "knockback": 120, "angle": 30 },
    "heavy": { "startup": 12, "active": 6, "recovery": 18, "damage": 22, "hitstun": 24, "blockstun": 14, "knockback": 280, "angle": 45 }
  }
}
```
- Deserialize with `System.Text.Json` (source-generated `JsonSerializerContext` for AOT).
- All frame data is loaded during `LoadContent()` into `FrameData[]` arrays per character.
- NEVER load or parse data during gameplay.

### 5.2 Hitbox Definitions in JSON
```json
{
  "knight": {
    "jab_hitbox": { "offsetX": 12, "offsetY": -2, "halfW": 14, "halfH": 8 },
    "heavy_hitbox": { "offsetX": 16, "offsetY": -4, "halfW": 20, "halfH": 12 }
  }
}
```

---

## 6. C# NAMING CONVENTIONS

| Element | Convention | Example |
|---|---|---|
| Namespace | `PascalCase`, matches folder | `FantasyRPG.Core.Systems` |
| Class / Struct | `PascalCase` | `CombatSystem`, `FrameData` |
| Interface | `IPascalCase` | `IComponent`, `IGameState` |
| Public Method | `PascalCase` | `ResolveCollisions()`, `ConsumeAction()` |
| Private Method | `PascalCase` | `ClassifyHit()` |
| Public Property | `PascalCase` | `IsInParryWindow`, `CurrentFrame` |
| Private Field | `_camelCase` | `_hitResults`, `_renderTarget` |
| Local Variable | `camelCase` | `deltaTime`, `attackerIndex` |
| Constant | `PascalCase` | `ParryWindowFrames`, `MaxFighters` |
| Enum | `PascalCase` (singular) | `FighterStateId.Idle`, `HitResultType.Parried` |
| File Name | Match primary type | `CombatSystem.cs`, `FrameData.cs` |

---

## 7. FILE & PROJECT RULES

- **One public type per file.** File name matches the type name exactly.
- **No God classes.** If a class exceeds ~300 lines, split into smaller systems.
- **All gameplay code** lives in `FantasyRPG.Core`. Platform projects contain ONLY entry points.
- **Content Pipeline**: Each platform's `Content.mgcb` references shared raw assets via `../../content/`.

---

## 8. MONOGAME-SPECIFIC PATTERNS

### SpriteBatch Usage
```csharp
_spriteBatch.Begin(
    sortMode: SpriteSortMode.Deferred,
    blendState: BlendState.AlphaBlend,
    samplerState: SamplerState.PointClamp,  // ALWAYS for pixel art
    transformMatrix: _camera.TransformMatrix);
```

### GameTime Usage
```csharp
// For physics: use float seconds
float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

// For combat: use INTEGER FRAME COUNTS (not time-based!)
combat.CurrentFrame++;  // 1 frame = 1 tick at fixed 60fps
```

### Content Loading
```csharp
// CORRECT — load during LoadContent or loading screen
_knightAtlas = Content.Load<Texture2D>("sprites/knight/knight_atlas");

// WRONG — never load content during Update/Draw
```

---

## 9. FORBIDDEN PATTERNS (INSTANT REJECTION)

| ❌ Forbidden | ✅ Required Alternative |
|---|---|
| `new List<T>()` in game loop | Pre-allocate in constructor or `Initialize()` |
| `ToString()` in game loop | Pre-built string cache or `Span<char>` |
| `async/await` in game loop | Use frame counters or timers |
| Deep inheritance (>2 levels) | Composition with components |
| Static mutable globals | Pass dependencies via system method parameters |
| `dynamic` keyword | Strong typing always |
| Reflection in game loop | Source generators or pre-computed lookups |
| Magic numbers | Named constants in `GameSettings` or `FrameData` |
| Float-based combat timing | Integer frame counts (deterministic) |
| Mixing hitbox/hurtbox with physics colliders | Separate collision domains |
| Allocating `HitResult` per frame | Pre-allocated static buffer |
| LINQ in combat resolution | `for` loops over `Span<T>` |

---

## 10. GIT & ASSET RULES

- **Git LFS** tracks: `*.png`, `*.wav`, `*.ogg`, `*.mp3`, `*.ttf`, `*.otf`, `*.aseprite`, `*.psd`
- **Never commit** compiled `.xnb` files, `bin/`, `obj/`, or `.vs/` directories.
- **Commit messages**: `type(scope): description` — e.g., `feat(combat): implement parry classification system`

---

## 11. TESTING

- **Unit Tests**: `FantasyRPG.Tests` project using xUnit.
- Test `CombatSystem.ClassifyHit()` with every defender state combination.
- Test `InputBufferComponent.ConsumeAction()` with known buffer states.
- Test `AABB.Intersects()` with edge cases (touching, overlapping, separated).
- Test frame data state transitions: Startup→Active→Recovery→Idle frame counts.
- Do NOT test rendering. Test the data transformations that feed rendering.

---

## 12. WHEN IN DOUBT

1. Does this allocate in the game loop? → **Refactor to pool or pre-allocate.**
2. Is combat timing float-based? → **Convert to integer frame counts.**
3. Am I mixing physics colliders with combat hitboxes? → **Separate them. Always.**
4. Data or behavior? → **Data = Component. Behavior = System.**
5. Should this be a class or struct? → **If < 16 bytes and passed frequently → struct.**
6. Can the parry system detect this edge case? → **Check `ClassifyHit()` priority order.**
7. Does the input feel laggy? → **Check the input buffer. Increase `BufferSize` if needed.**
