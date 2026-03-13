# Cross-Platform Gotchas Checklist

> **Do these BEFORE writing any gameplay code.** Each one will bite you later if skipped.

---

## ✅ 1. MGCB Content Pipeline: Shared Source, Per-Platform Build

**Problem**: Each platform (DesktopGL, Android, iOS) compiles assets into different binary formats (`.xnb`). You cannot share compiled content across platforms.

**Solution**:
- Keep **one** raw asset folder: `content/` at repository root (tracked by Git LFS).
- Each platform project has its **own** `Content.mgcb` file inside `src/FantasyRPG.<Platform>/Content/`.
- In each `.mgcb`, reference raw source assets using relative paths **back to the shared folder**:
  ```
  #begin ../../../content/sprites/knight/knight_atlas.png
  /importer:TextureImporter
  /processor:TextureProcessor
  /processorParam:ColorKeyEnabled=False
  /processorParam:GenerateMipmaps=False
  /processorParam:PremultiplyAlpha=True
  /processorParam:TextureFormat=Color
  /build:../../../content/sprites/knight/knight_atlas.png;sprites/knight/knight_atlas
  ```
- The **output alias** (after the `;`) must be identical across all platforms so `Content.Load<T>("sprites/knight/knight_atlas")` works everywhere.
- **Critical**: Set `GenerateMipmaps=False` for ALL pixel art textures. Mipmaps destroy pixel clarity.

### Texture Processor Settings for Pixel Art:
| Parameter | Value | Why |
|---|---|---|
| `ColorKeyEnabled` | `False` | Use alpha channel, not color keying |
| `GenerateMipmaps` | `False` | Mipmaps blur pixel art |
| `PremultiplyAlpha` | `True` | Required for correct `BlendState.AlphaBlend` rendering |
| `TextureFormat` | `Color` | Full 32-bit RGBA, no DXT compression artifacts |

---

## ✅ 2. GraphicsProfile and Texture Size Limits

**Problem**: Mobile GPUs (especially older Android) have strict limits that desktop GPUs silently ignore.

**Action Items**:

| Setting | Desktop | Mobile | Your Rule |
|---|---|---|---|
| `GraphicsProfile` | `HiDef` (4096×4096 max texture) | `Reach` safe (2048×2048 max) | **Use `HiDef` but keep ALL atlases ≤ 2048×2048** for mobile compatibility |
| Max RenderTarget | Unlimited | Often 2048×2048 | Your 480×270 target is fine ✅ |
| Shader Model | SM 4.0+ | SM 2.0 (GLSL ES) | Avoid SM 4.0+ features if targeting Android |
| Non-power-of-two textures | Full support | Some devices: limited | **Make all atlas dimensions power-of-two** (512, 1024, 2048) |

**Specific Steps**:
1. In `GameRoot.cs`, set `GraphicsProfile = GraphicsProfile.HiDef` for all platforms (MonoGame 3.8+ supports this on modern Android/iOS).
2. Enforce a **2048×2048 max atlas size** rule in your sprite packing workflow.
3. If you target very old Android (API < 24), fall back to `GraphicsProfile.Reach` and test thoroughly.

---

## ✅ 3. Platform-Specific Input, Lifecycle, and Storage

### 3A. Input Abstraction
**Problem**: Desktop has keyboard/mouse/gamepad. Mobile has touch only. If you hardcode `Keyboard.GetState()` in systems, mobile builds won't work.

**Solution** (already scaffolded in `InputManager.cs`):
- Systems never call `Keyboard.GetState()` directly — they read from `InputManager`.
- Extend `InputManager` with a `TouchAdapter` for mobile:
  ```
  InputManager
    ├── KeyboardState (Desktop)
    ├── GamePadState  (Desktop + Mobile)
    └── TouchAdapter  (Mobile only — reads TouchPanel.GetState())
         └── Maps touch zones to virtual buttons / virtual joystick
  ```
- Use `#if ANDROID || IOS` preprocessor directives **only** in platform-specific projects, never in Core.

### 3B. App Lifecycle (Critical for Mobile)
**Problem**: Mobile apps get suspended/resumed by the OS. MonoGame fires `Activated` / `Deactivated` events, but you must handle:

| Event | Required Action |
|---|---|
| `Deactivated` (app backgrounded) | Pause game, save state, stop audio |
| `Activated` (app foregrounded) | Resume, reload any lost GPU resources |
| Low memory warning (iOS) | Unload non-essential cached textures |

**Specific Steps**:
```csharp
// In GameRoot constructor:
Activated += OnActivated;
Deactivated += OnDeactivated;

private void OnDeactivated(object? sender, EventArgs e)
{
    // Auto-pause, persist save data, stop SoundEffect instances
    _gameStateManager.ChangeState(GameStateId.Pause);
}

private void OnActivated(object? sender, EventArgs e)
{
    // GPU resources may be lost on Android — re-validate textures
    // MonoGame handles most cases, but custom RenderTarget2D may need recreation
}
```

### 3C. Storage Paths
**Problem**: File system paths differ drastically across platforms.

| Platform | Writable Path |
|---|---|
| Desktop | `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` |
| Android | `Application.Context.FilesDir.AbsolutePath` |
| iOS | `Environment.GetFolderPath(SpecialFolder.MyDocuments)` |

**Solution**: Create a `IPlatformService` interface in Core with a `GetSavePath()` method. Each platform project provides the implementation. Inject it into `GameRoot` at construction time.

---

## 🚨 BONUS: Pre-Flight Verification Steps

Run these commands to validate your setup before writing gameplay code:

```bash
# 1. Restore NuGet packages and verify solution compiles
dotnet restore FantasyRPG.sln
dotnet build src/FantasyRPG.DesktopGL/FantasyRPG.DesktopGL.csproj -c Debug

# 2. Verify MGCB tool is installed (needed for content pipeline)
dotnet tool install -g dotnet-mgcb
dotnet tool install -g dotnet-mgcb-editor  # optional GUI

# 3. Run the DesktopGL project (should open a window with your ClearColor)
dotnet run --project src/FantasyRPG.DesktopGL/FantasyRPG.DesktopGL.csproj

# 4. Initialize Git LFS
cd FantasyRPG
git init
git lfs install
git lfs track "*.png" "*.wav" "*.ogg" "*.mp3" "*.ttf" "*.otf" "*.aseprite"
git add .gitattributes
git add -A
git commit -m "chore(init): scaffold MonoGame RPG solution"
```

---

## Summary Matrix

| # | Gotcha | Risk if Ignored | Fix Difficulty |
|---|---|---|---|
| 1 | MGCB shared content referencing | Content won't load on non-desktop platforms | Medium — must configure per `.mgcb` file |
| 2 | Texture size limits + GraphicsProfile | Crash on mobile, corrupted textures | Easy — enforce 2048×2048 rule now |
| 3 | Input/Lifecycle/Storage abstraction | Mobile builds are DOA | Medium — must design abstraction layer early |
