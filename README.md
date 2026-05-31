# RetroTetris

A faithful implementation of the classic Tetris game as a native desktop application, built with **.NET 10** and **.NET MAUI**. The game runs in a native Windows window and reproduces the original Tetris experience, including all 7 standard tetrominoes, SRS rotation mechanics, line clearing, and a scoring system — with a retro visual style inspired by the classic 80s/90s arcade era.

> 🇧🇷 [Leia em Português](README.pt-BR.md)

---

## Demo

![RetroTetris Gameplay](docs/RetroTetris_Gameplay.gif)

---

## Features

- 🎮 All 7 standard tetrominoes (I, O, T, S, Z, J, L) with distinct retro colors
- 👻 Ghost piece showing where the active piece will land
- 🔄 SRS (Super Rotation System) with wall-kick tables from the Tetris Guideline
- 🎒 7-bag randomizer guaranteeing uniform piece distribution
- ⏱️ Lock delay with up to 15 resets per piece
- 🔒 Hold piece mechanic (one swap per active piece)
- 📋 Next 3 pieces preview panel
- 📈 Score, level, and high score tracking with local persistence
- ⏸️ Pause/resume support
- 📖 In-game controls screen accessible from the start screen and pause overlay
- 🔊 Retro sound effects and background music via Plugin.Maui.Audio
- 📐 Responsive layout that adapts to window size (portrait and landscape)
- ⌨️ Keyboard controls with DAS/ARR (Delayed Auto Shift / Auto Repeat Rate)

---

## Controls

| Key | Action |
|---|---|
| ← → | Move left / right |
| ↓ | Soft drop |
| Space | Hard drop |
| ↑ or Z | Rotate clockwise |
| X | Rotate counter-clockwise |
| C or Shift | Hold piece |
| Escape or P | Pause / Resume |
| H | Controls screen |

---

## Architecture

The project is split into **3 separate projects** to enforce clean layer separation at the compiler level:

```
RetroTetris.slnx
├── RetroTetris.Core/     ← Pure game logic — no MAUI dependencies
├── RetroTetris/          ← MAUI app — rendering, input, services
└── RetroTetris.Tests/    ← xUnit + FsCheck — unit and property-based tests
```

### Design Patterns Applied

| Pattern | Where | Purpose |
|---|---|---|
| **State Machine** | `GameEngine` + `IGameState` | Manages game states (StartScreen, Playing, Paused, ControlsScreen, GameOver) without scattered if/switch |
| **Command** | `IGameCommand` + `CommandQueue` | Decouples input from game logic; enables thread-safe command passing |
| **Observer** | `GameEngine` C# events | Decouples engine from audio, scoring, and UI updates |
| **Game Loop** | `GameLoop` (dedicated thread) | Real delta time via `Stopwatch`; explicit ProcessInput → Update → Render phases |

### Tech Stack

| Component | Technology |
|---|---|
| Framework | .NET 10 + .NET MAUI |
| Rendering | `GraphicsView` / `ICanvas` (native MAUI) |
| Audio | Plugin.Maui.Audio |
| Persistence | `FileSystem.AppDataDirectory` + `System.Text.Json` |
| Tests | xUnit + FsCheck (property-based testing) |
| Platform | Windows (WinUI 3) |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2026 with the following workloads:
  - **".NET Multi-Platform App UI development"**
  - **"Desktop development with C++"** *(required for WinUI 3 native components)*

### Build and Run

```bash
# Build
dotnet build RetroTetris/RetroTetris.csproj

# Run
dotnet run --project RetroTetris/RetroTetris.csproj -f net10.0-windows10.0.19041.0
```

### Run Tests

```bash
dotnet test RetroTetris.Tests/RetroTetris.Tests.csproj
```

### Publish

To generate a self-contained executable (no .NET installation required on the target machine):

```bash
dotnet publish RetroTetris/RetroTetris.csproj -f net10.0-windows10.0.19041.0 -c Release -r win-x64 --self-contained true
```

The output will be in `RetroTetris/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/`.

---

## Project Structure

```
RetroTetris.Core/
├── Engine/          ← GameEngine, Board, Tetromino, BagRandomizer, SRS, etc.
├── Commands/        ← IGameCommand implementations (Command Pattern)
├── States/          ← IGameState implementations (State Machine Pattern)
├── Interfaces/      ← IGameEngine, IAudioService, IGameRenderer, etc.
└── Models/          ← GameSnapshot, HighScoreData

RetroTetris/
├── Pages/           ← GamePage (MAUI XAML)
├── Presentation/    ← GameRenderer, InputHandler, LayoutManager
└── Services/        ← AudioService, HighScoreRepository
```

---

## Correctness Properties (Property-Based Testing)

The game logic is validated through 10 formal correctness properties using FsCheck:

1. **Lateral movement** respects board boundaries and occupied cells
2. **SRS rotation** always produces a valid board position
3. **Hard drop** places the piece at the lowest possible row
4. **Ghost piece** always matches the hard drop destination
5. **Line clearing** preserves all cells from incomplete lines
6. **Scoring** follows the exact formula: `multiplier[n] × level`
7. **Bag randomizer** guarantees each of the 7 types appears exactly once per cycle
8. **Drop interval** follows `max(100, 1000 - (level - 1) × 90)` ms
9. **Hold** is blocked after use until the current piece is locked
10. **Level** increments every 10 lines: `min(15, 1 + floor(lines / 10))`

---

## License

This project is intended for study and portfolio purposes.
