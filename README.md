# FantasyRPG - 2D Fighting Game

A 2D pixel-art fighting game built with **MonoGame** and **C# .NET 8**, using an ECS-Lite architecture.

## Screenshots

![Character Select](img/bg_stage.png)

## Features

- **Character Selection** — Choose between Knight or Wizard before battle
- **2 Playable Characters**
  - **Knight** — Sword fighter with fast jab, heavy slash, dash, and parry
  - **Wizard** — Glass cannon with staff jab, ranged blast, teleport dash, and energy shield
- **Combat System** — Attack, block, parry, dash, jump with input buffering
- **ECS-Lite Architecture** — Struct-of-Arrays layout, zero runtime allocations
- **Pixel-Perfect Rendering** — 480×270 virtual resolution upscaled with PointClamp
- **Debug Overlay** — Toggle hitbox/hurtbox visualization with F1

## Controls

| Key | Action |
|-----|--------|
| ← → | Move |
| Space | Jump |
| 1 | Attack (Jab) |
| 2 | Special (Heavy) |
| 3 | Block / Parry |
| 4 | Dash |
| F1 | Toggle debug overlay |
| F2 | Toggle state logger |

## Build & Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MonoGame 3.8.2](https://monogame.net/)

### Build
```bash
dotnet build src/FantasyRPG.DesktopGL/FantasyRPG.DesktopGL.csproj
```

### Run
```bash
dotnet run --project src/FantasyRPG.DesktopGL/FantasyRPG.DesktopGL.csproj
```

## Project Structure

```
FantasyRPG/
├── src/
│   ├── FantasyRPG.Core/          # Game logic (platform-independent)
│   │   ├── Components/           # ECS components (structs)
│   │   ├── Data/                 # Character data (Knight, Wizard)
│   │   ├── Engine/               # Input, graphics, state machine
│   │   ├── GameStates/           # Character select, gameplay
│   │   └── Systems/              # Physics, combat, animation, render
│   ├── FantasyRPG.DesktopGL/     # Desktop launcher + content
│   └── FantasyRPG.Android/       # Android launcher
└── img/                          # Source sprite sheets
```

## License

This project is for educational purposes.
