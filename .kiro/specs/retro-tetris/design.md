# Documento de Design — Retro Tetris

## Visão Geral

O Retro Tetris é uma implementação fiel do Tetris clássico como aplicação desktop nativa, construída sobre .NET 10 e .NET MAUI. A renderização é feita inteiramente via `GraphicsView`/`ICanvas` do MAUI, sem dependências de engines de jogo externas. O áudio é gerenciado pelo `Plugin.Maui.Audio` e a persistência de recordes usa o `FileSystem` do MAUI com JSON.

A arquitetura separa claramente o **núcleo do jogo** (lógica pura, sem dependências de UI) da **camada de apresentação** (renderização MAUI) e da **camada de entrada** (teclado/toque). Isso facilita testes unitários e de propriedade no núcleo sem necessidade de instanciar a UI.

### Decisões de Design Principais

| Decisão | Escolha | Justificativa |
|---|---|---|
| Renderização | `GraphicsView` + `ICanvas` nativo MAUI | Requisito explícito; suficiente para 60 fps em grade 10×20 |
| Game Loop | `GameLoop` dedicado com `Stopwatch` + thread própria (Game Loop Pattern) | Delta time real, separação explícita de fases, cap de 60 fps sem consumir 100% da CPU |
| Input (Windows) | `KeyDown`/`KeyUp` via handler da `Window` nativa WinUI 3 → produz `IGameCommand` | Único mecanismo confiável para teclas de jogo no MAUI Windows; desacoplado via Command Pattern |
| Input (toque) | `TapGestureRecognizer` + `PanGestureRecognizer` no `GraphicsView` → produz `IGameCommand` | API nativa MAUI, sem biblioteca extra; desacoplado via Command Pattern |
| Gerenciamento de estado | State Machine com `IGameState` e classes concretas (State Machine Pattern) | Elimina `if/switch` espalhados; cada estado encapsula seu comportamento e transições válidas |
| Comunicação entre subsistemas | Observer Pattern com eventos C# (`event Action<...>`) | Desacopla `GameEngine` de `ScoreSystem`, `AudioService` e `GameRenderer`; facilita extensão |
| Áudio | `Plugin.Maui.Audio` (IAudioManager) | Requisito explícito; suporta Windows e macOS |
| Persistência | `FileSystem.AppDataDirectory` + `System.Text.Json` | Requisito explícito; sem banco de dados necessário |
| SRS | Tabelas de wall-kick padrão do Tetris Guideline | Fidelidade ao Tetris moderno conforme requisito 4 |

---

## Arquitetura

O sistema é organizado em quatro camadas:

```
┌─────────────────────────────────────────────────────┐
│                  MAUI App Layer                     │
│  AppShell / MainPage / GamePage / StartPage         │
└──────────────────────┬──────────────────────────────┘
                       │ usa
┌──────────────────────▼──────────────────────────────┐
│              Presentation Layer                     │
│  GameRenderer (IDrawable)  │  InputHandler          │
│  AudioService              │  LayoutManager         │
└──────────────────────┬──────────────────────────────┘
                       │ usa
┌──────────────────────▼──────────────────────────────┐
│                 Game Core Layer                     │
│  GameEngine  │  Board  │  Tetromino  │  BagRandom.  │
│  ScoreSystem │ LevelSystem │ HoldSystem │ GhostCalc  │
└──────────────────────┬──────────────────────────────┘
                       │ usa
┌──────────────────────▼──────────────────────────────┐
│              Infrastructure Layer                   │
│  HighScoreRepository  │  AudioPlayer                │
└─────────────────────────────────────────────────────┘
```

### Fluxo de Dados Principal

```
GameLoop thread (Stopwatch + Thread própria):
  1. ProcessInput()  → drena CommandQueue → executa IGameCommand.Execute(engine)
  2. Update(delta)   → _currentState.Update(engine, delta)
                         → Board.TryMoveDown(activePiece)
                         → LockDelay.Tick()
                         → [se expirou] Board.LockPiece()
                              → engine dispara PieceLocked → AudioService reage
                              → LineClearing.Check()
                              → engine dispara LinesCleared(n, level)
                                   → ScoreSystem reage → engine dispara ScoreChanged(score)
                                   → LevelSystem reage → engine dispara LevelChanged(level)
                                   → AudioService reage
  3. Render()        → Dispatcher.Dispatch(() => canvas.Invalidate())
                         → GameRenderer.Draw(canvas, dirtyRect)

Eventos Observer (GameEngine):
  Board.LockPiece()     → PieceLocked        → AudioService.PlaySfxAsync(Lock)
  LineClearing          → LinesCleared(n, L) → ScoreSystem, LevelSystem, AudioService
  ScoreSystem.Add()     → ScoreChanged(s)    → GameRenderer atualiza display
  LevelSystem.AddLines()→ LevelChanged(l)    → GameLoop ajusta intervalo de queda
  GameOver              → GameOverOccurred   → AudioService, HighScoreRepository, UI
  Spawn                 → PieceSpawned(type) → UI atualiza painel Next
```

---

## Componentes e Interfaces

### IGameEngine

Ponto de entrada central. Coordena todos os subsistemas e expõe eventos para comunicação desacoplada (Observer Pattern).

```csharp
public interface IGameEngine
{
    // Estado e dados
    IGameState CurrentState { get; }
    Board Board { get; }
    Tetromino? ActivePiece { get; }
    Tetromino? GhostPiece { get; }
    Tetromino? HoldPiece { get; }
    IReadOnlyList<Tetromino> NextPieces { get; }  // 3 próximas
    int Score { get; }
    int HighScore { get; }
    int Level { get; }
    int LinesCleared { get; }

    // Controle de ciclo de vida
    void StartNewGame();
    void TransitionTo(IGameState newState);
    void Update(TimeSpan delta);  // chamado pelo GameLoop

    // Ações do jogador (chamadas pelos IGameCommand)
    void MoveLeft();
    void MoveRight();
    void SoftDrop();
    void HardDrop();
    void RotateClockwise();
    void RotateCounterClockwise();
    void Hold();
    void Pause();
    void Resume();

    // Eventos Observer — subsistemas se inscrevem em vez de serem chamados diretamente
    event Action<int, int> LinesCleared;      // (quantidade de linhas, nível atual)
    event Action<int> ScoreChanged;           // (novo score)
    event Action<int> LevelChanged;           // (novo nível)
    event Action PieceLocked;                 // peça fixada no board
    event Action<TetrominoType> PieceSpawned; // nova peça gerada
    event Action GameOverOccurred;            // partida encerrada
}
```

> **Nota:** O `enum GameState` é substituído pela State Machine — ver seção [Estados do Jogo (State Machine Pattern)](#estados-do-jogo-state-machine-pattern).

### Board

Representa a grade 10×22 (20 visíveis + 2 buffer).

```csharp
public class Board
{
    public const int Columns = 10;
    public const int VisibleRows = 20;
    public const int BufferRows = 2;
    public const int TotalRows = VisibleRows + BufferRows;

    // Célula nula = vazia; valor = cor do tetrominó que a ocupa
    private readonly TetrominoType?[,] _cells; // [row, col]

    public TetrominoType? GetCell(int row, int col);
    public bool IsOccupied(int row, int col);
    public bool IsInBounds(int row, int col);
    public bool CanPlace(Tetromino piece, int row, int col);
    public void LockPiece(Tetromino piece);
    public IReadOnlyList<int> FindCompleteLines();
    public void ClearLines(IReadOnlyList<int> lines);
    public void Reset();
}
```

### Tetromino

Representa uma peça com tipo, estado de rotação e posição.

```csharp
public class Tetromino
{
    public TetrominoType Type { get; }
    public RotationState Rotation { get; private set; }  // 0, R, 2, L
    public int Row { get; set; }
    public int Col { get; set; }

    // Retorna as 4 células ocupadas relativas à posição atual
    public IReadOnlyList<(int Row, int Col)> GetCells();

    // Retorna nova instância rotacionada (imutável para facilitar testes)
    public Tetromino RotateClockwise();
    public Tetromino RotateCounterClockwise();

    public static Tetromino Create(TetrominoType type);
}

public enum TetrominoType { I, O, T, S, Z, J, L }
public enum RotationState { Spawn = 0, Right = 1, Two = 2, Left = 3 }
```

### SRSRotationSystem

Implementa as tabelas de wall-kick do Tetris Guideline.

```csharp
public static class SRSRotationSystem
{
    // Retorna lista de offsets (deltaRow, deltaCol) a testar, em ordem
    public static IReadOnlyList<(int dr, int dc)> GetKickOffsets(
        TetrominoType type,
        RotationState from,
        RotationState to);

    // Tenta rotacionar no board; retorna peça rotacionada ou null se falhar
    public static Tetromino? TryRotate(
        Tetromino piece,
        Board board,
        bool clockwise);
}
```

**Tabelas SRS embutidas** (conforme [Tetris Wiki — SRS](https://tetris.wiki/Super_Rotation_System)):

- J, L, S, T, Z compartilham a mesma tabela de 8 transições × 5 testes
- I tem tabela própria
- O não realiza wall-kick

### BagRandomizer

Implementa o sistema "7-bag" para geração de peças.

```csharp
public class BagRandomizer
{
    private readonly Queue<TetrominoType> _queue;
    private readonly Random _rng;

    public TetrominoType Peek(int index);  // 0 = próxima, 1 = segunda, 2 = terceira
    public TetrominoType Dequeue();
    public void Reset();
}
```

### GhostCalculator

Calcula a posição de pouso da peça ativa.

```csharp
public static class GhostCalculator
{
    public static Tetromino Calculate(Tetromino activePiece, Board board);
}
```

### LockDelayController

Gerencia o Lock Delay de 500ms com máximo de 15 reinicializações.

```csharp
public class LockDelayController
{
    public const int DelayMs = 500;
    public const int MaxResets = 15;

    public bool IsActive { get; }
    public void Start();
    public void Reset();   // reinicia o timer; respeita MaxResets
    public void Cancel();
    public bool HasExpired(TimeSpan elapsed);
}
```

### ScoreSystem

```csharp
public class ScoreSystem
{
    public int Score { get; private set; }
    public void AddLinesClear(int linesCleared, int level);
    public void Reset();

    // Tabela: 1→100×L, 2→300×L, 3→500×L, 4→800×L
    private static readonly int[] _multipliers = { 0, 100, 300, 500, 800 };
}
```

### LevelSystem

```csharp
public class LevelSystem
{
    public int Level { get; private set; }
    public int TotalLinesCleared { get; private set; }

    public void AddLines(int count);
    public int GetDropIntervalMs();  // max(100, 1000 - (Level-1)*90)
    public void Reset();
}
```

### IGameRenderer (IDrawable)

```csharp
public interface IGameRenderer : IDrawable
{
    void SetGameEngine(IGameEngine engine);
    void SetFlashingLines(IReadOnlyList<int> lines);  // para animação de clear
}
```

### IInputHandler

O `InputHandler` não chama métodos do engine diretamente. Ele traduz eventos de entrada em objetos `IGameCommand` e os enfileira na `CommandQueue` (Command Pattern).

```csharp
public interface IInputHandler
{
    void OnKeyDown(GameKey key);
    void OnKeyUp(GameKey key);
    void OnSwipe(SwipeDirection direction);
    void OnTap(TapSide side);
    void Update(TimeSpan elapsed);  // para DAS/ARR — enfileira MoveLeftCommand/MoveRightCommand repetidos
}

public enum GameKey
{
    Left, Right, Down, Up, Space, Z, X, C, Shift, Escape, P
}
```

---

### GameLoop (Game Loop Pattern)

Classe dedicada que roda em thread própria, mede delta time real com `Stopwatch` e separa explicitamente as três fases do ciclo de jogo. Substitui o `System.Timers.Timer`.

```csharp
public class GameLoop
{
    private readonly IGameEngine _engine;
    private readonly CommandQueue _commandQueue;
    private Thread? _thread;
    private volatile bool _running;

    public GameLoop(IGameEngine engine, CommandQueue commandQueue);

    public void Start();
    public void Stop();

    private void Loop()
    {
        var stopwatch = Stopwatch.StartNew();
        var previous = stopwatch.Elapsed;

        while (_running)
        {
            var current = stopwatch.Elapsed;
            var delta = current - previous;
            previous = current;

            ProcessInput();          // fase 1: drena CommandQueue
            _engine.Update(delta);   // fase 2: atualiza estado via State Machine
            Render();                // fase 3: agenda redesenho na UI thread

            // Cap de ~60 fps — evita consumir 100% da CPU
            Thread.Sleep(1);
        }
    }

    private void ProcessInput()
    {
        while (_commandQueue.TryDequeue(out var command))
            command!.Execute(_engine);
    }

    private void Render()
    {
        // Dispatcher.Dispatch(() => canvas.Invalidate()) — thread-safe
    }
}
```

**Vantagens de estudo:**
- Delta time real elimina dependência de tick fixo; o jogo se comporta corretamente mesmo sob carga
- Separação explícita de fases torna o fluxo de execução legível e testável
- Thread própria evita bloqueio da UI thread

---

### Command Pattern (Comandos de Input)

Cada ação do jogador é encapsulada em um objeto que implementa `IGameCommand`. O `InputHandler` produz comandos; o `GameLoop` os consome na fase `ProcessInput()`.

```csharp
public interface IGameCommand
{
    void Execute(IGameEngine engine);
}
```

**Comandos concretos:**

```csharp
public sealed class MoveLeftCommand               : IGameCommand { public void Execute(IGameEngine e) => e.MoveLeft(); }
public sealed class MoveRightCommand              : IGameCommand { public void Execute(IGameEngine e) => e.MoveRight(); }
public sealed class SoftDropCommand               : IGameCommand { public void Execute(IGameEngine e) => e.SoftDrop(); }
public sealed class HardDropCommand               : IGameCommand { public void Execute(IGameEngine e) => e.HardDrop(); }
public sealed class RotateClockwiseCommand        : IGameCommand { public void Execute(IGameEngine e) => e.RotateClockwise(); }
public sealed class RotateCounterClockwiseCommand : IGameCommand { public void Execute(IGameEngine e) => e.RotateCounterClockwise(); }
public sealed class HoldCommand                   : IGameCommand { public void Execute(IGameEngine e) => e.Hold(); }
public sealed class PauseCommand                  : IGameCommand { public void Execute(IGameEngine e) => e.Pause(); }
```

### CommandQueue

Fila thread-safe de `IGameCommand`. O `InputHandler` enfileira (thread da UI); o `GameLoop` drena (thread do loop).

```csharp
public class CommandQueue
{
    private readonly ConcurrentQueue<IGameCommand> _queue = new();

    public void Enqueue(IGameCommand command) => _queue.Enqueue(command);
    public bool TryDequeue(out IGameCommand? command) => _queue.TryDequeue(out command);
    public void Clear() => _queue.Clear();
}
```

**Vantagens de estudo:**
- Desacopla completamente input de lógica de jogo
- Facilita replay: basta gravar/reproduzir a sequência de comandos
- Facilita testes: injeta comandos diretamente sem simular eventos de teclado

---

### State Machine Pattern (Estados do Jogo)

Substitui o `enum GameState` + `if/switch` espalhados por uma State Machine com classes concretas. O `GameEngine` delega `Update()` para o estado atual e transiciona via `TransitionTo()`.

```csharp
public interface IGameState
{
    void Enter(GameEngine engine);
    void Update(GameEngine engine, TimeSpan delta);
    void Exit(GameEngine engine);
}
```

**Estados concretos:**

```csharp
// Tela inicial — aguarda StartNewGame()
public class StartScreenState : IGameState
{
    public void Enter(GameEngine engine) { /* exibe tela inicial */ }
    public void Update(GameEngine engine, TimeSpan delta) { /* aguarda input */ }
    public void Exit(GameEngine engine) { /* limpa tela inicial */ }
}

// Partida em andamento — processa queda, lock delay, line clear
public class PlayingState : IGameState
{
    public void Enter(GameEngine engine) { /* inicializa peça ativa */ }
    public void Update(GameEngine engine, TimeSpan delta) { /* lógica principal */ }
    public void Exit(GameEngine engine) { /* salva estado se necessário */ }
}

// Jogo pausado — suspende queda e lock delay, oculta board
public class PausedState : IGameState
{
    public void Enter(GameEngine engine) { /* para timers internos */ }
    public void Update(GameEngine engine, TimeSpan delta) { /* aguarda resume */ }
    public void Exit(GameEngine engine) { /* retoma timers */ }
}

// Fim de jogo — exibe score, aguarda Try Again ou Exit
public class GameOverState : IGameState
{
    public void Enter(GameEngine engine) { /* dispara GameOverOccurred */ }
    public void Update(GameEngine engine, TimeSpan delta) { /* aguarda input */ }
    public void Exit(GameEngine engine) { /* limpa estado */ }
}
```

**Diagrama de transições:**

```
                    ┌─────────────────┐
                    │  StartScreen    │
                    │  State          │
                    └────────┬────────┘
                             │ StartNewGame()
                             ▼
                    ┌─────────────────┐
              ┌────►│  Playing        │◄────┐
              │     │  State          │     │
              │     └──┬──────────┬───┘     │
              │        │          │         │
              │  Pause()│    GameOver       │
              │        ▼          ▼         │
              │  ┌──────────┐  ┌──────────┐ │
              │  │ Paused   │  │ GameOver │ │
              │  │ State    │  │ State    │ │
              │  └────┬─────┘  └────┬─────┘ │
              │       │             │       │
              └───────┘    TryAgain()       │
               Resume()        └────────────┘
```

**Vantagens de estudo:**
- Cada estado encapsula seu comportamento — sem `if (state == Playing)` espalhados
- Transições explícitas e auditáveis via `TransitionTo()`
- Fácil adicionar novos estados (ex.: `CountdownState`) sem modificar os existentes

---

### IHighScoreRepository

```csharp
public interface IHighScoreRepository
{
    Task<int> LoadAsync();
    Task SaveAsync(int score);
}
```

### IAudioService

```csharp
public interface IAudioService
{
    Task PlaySfxAsync(SoundEffect effect);
    Task StartMusicAsync();
    Task StopMusicAsync();
    void SetMuted(bool muted);
}

public enum SoundEffect { Move, Rotate, Lock, LineClear, Tetris, GameOver }
```

---

## Modelos de Dados

### GameSnapshot (estado serializável para testes)

```csharp
public record GameSnapshot(
    TetrominoType?[,] Cells,
    TetrominoType ActiveType,
    RotationState ActiveRotation,
    int ActiveRow,
    int ActiveCol,
    TetrominoType? HoldType,
    IReadOnlyList<TetrominoType> NextQueue,
    int Score,
    int Level,
    int LinesCleared
);
```

### HighScoreData (persistência JSON)

```csharp
public record HighScoreData(int Score, DateTime AchievedAt);
```

### Representação das Peças (matrizes de rotação)

Cada tetrominó é definido por suas 4 células relativas ao ponto de origem (row, col) para cada estado de rotação. Exemplo para o T:

```
Estado 0 (spawn):   Estado R:    Estado 2:    Estado L:
 . T .               . T .        . . .        T T .
 T T T               . T T        T T T        T . .
 . . .               . T .        . T .        T . .
```

As coordenadas são armazenadas como arrays estáticos em `TetrominoShapes`:

```csharp
public static class TetrominoShapes
{
    // [TetrominoType][RotationState] → array de (row, col) relativo
    public static readonly IReadOnlyDictionary<TetrominoType,
        IReadOnlyList<(int, int)>[]> Shapes;
}
```

### Cores dos Tetrominós

```csharp
public static class TetrominoColors
{
    public static readonly IReadOnlyDictionary<TetrominoType, Color> Primary = new()
    {
        [TetrominoType.I] = Color.FromArgb("#00F0F0"),  // ciano
        [TetrominoType.O] = Color.FromArgb("#F0F000"),  // amarelo
        [TetrominoType.T] = Color.FromArgb("#A000F0"),  // roxo
        [TetrominoType.S] = Color.FromArgb("#00F000"),  // verde
        [TetrominoType.Z] = Color.FromArgb("#F00000"),  // vermelho
        [TetrominoType.J] = Color.FromArgb("#0000F0"),  // azul
        [TetrominoType.L] = Color.FromArgb("#F0A000"),  // laranja
    };
}
```

### Layout de Renderização

```
┌──────────┬──────────────────┬──────────┐
│  HOLD    │                  │  NEXT    │
│  [peça]  │    BOARD         │  [1]     │
│          │   10 × 20        │  [2]     │
│  SCORE   │                  │  [3]     │
│  99999   │                  │          │
│  HIGH    │                  │  LEVEL   │
│  99999   │                  │   15     │
└──────────┴──────────────────┴──────────┘
```

Em modo retrato (janela mais alta que larga), os painéis laterais migram para cima/baixo do board.

### Estrutura de Arquivos do Projeto

A solution é dividida em **3 projetos** separados. Essa separação garante que o compilador impeça dependências acidentais entre camadas — não é apenas uma convenção, é uma restrição estrutural.

```
RetroTetris.sln
├── RetroTetris.Core/              ← class library — lógica pura, sem dependências MAUI
├── RetroTetris/                   ← MAUI app — UI, renderização, input, serviços
└── RetroTetris.Tests/             ← xUnit + FsCheck — testes unitários e de propriedade
```

**Referências entre projetos:**
```
RetroTetris        → RetroTetris.Core
RetroTetris.Tests  → RetroTetris.Core
RetroTetris.Tests  NÃO referencia RetroTetris (sem dependência de MAUI nos testes)
```

---

#### RetroTetris.Core (class library)

Lógica pura do jogo. Nenhum `using Microsoft.Maui.*` permitido aqui. Testável de forma isolada.

```
RetroTetris.Core/
├── RetroTetris.Core.csproj
├── Interfaces/
│   ├── IGameEngine.cs
│   ├── IGameRenderer.cs
│   ├── IInputHandler.cs
│   ├── IAudioService.cs
│   └── IHighScoreRepository.cs
├── Engine/
│   ├── GameEngine.cs
│   ├── GameLoop.cs                ← Game Loop Pattern
│   ├── Board.cs
│   ├── Tetromino.cs
│   ├── TetrominoShapes.cs
│   ├── TetrominoColors.cs
│   ├── BagRandomizer.cs
│   ├── GhostCalculator.cs
│   ├── SRSRotationSystem.cs
│   ├── LockDelayController.cs
│   ├── ScoreSystem.cs
│   └── LevelSystem.cs
├── Commands/                      ← Command Pattern
│   ├── IGameCommand.cs
│   ├── CommandQueue.cs
│   ├── MoveLeftCommand.cs
│   ├── MoveRightCommand.cs
│   ├── SoftDropCommand.cs
│   ├── HardDropCommand.cs
│   ├── RotateClockwiseCommand.cs
│   ├── RotateCounterClockwiseCommand.cs
│   ├── HoldCommand.cs
│   └── PauseCommand.cs
├── States/                        ← State Machine Pattern
│   ├── IGameState.cs
│   ├── StartScreenState.cs
│   ├── PlayingState.cs
│   ├── PausedState.cs
│   └── GameOverState.cs
└── Models/
    ├── GameSnapshot.cs
    └── HighScoreData.cs
```

---

#### RetroTetris (MAUI app)

Camada de apresentação e infraestrutura. Depende de `RetroTetris.Core` e do SDK do MAUI.

```
RetroTetris/
├── RetroTetris.csproj
├── MauiProgram.cs                 ← registro de dependências (DI)
├── App.xaml / App.xaml.cs
├── AppShell.xaml / AppShell.xaml.cs
├── Pages/
│   ├── StartPage.xaml / .cs       ← tela inicial com "Retro Tetris" e botão Start
│   └── GamePage.xaml / .cs        ← tela de jogo com GraphicsView
├── Presentation/
│   ├── GameRenderer.cs            ← IDrawable — desenha o estado do jogo no ICanvas
│   ├── InputHandler.cs            ← captura teclado/toque, produz IGameCommand
│   └── LayoutManager.cs           ← calcula dimensões e posições dos elementos
├── Services/
│   ├── AudioService.cs            ← Plugin.Maui.Audio, reage a eventos Observer
│   └── HighScoreRepository.cs     ← FileSystem MAUI + System.Text.Json
└── Resources/
    ├── Audio/
    │   ├── bgm.mp3
    │   ├── move.wav
    │   ├── rotate.wav
    │   ├── lock.wav
    │   ├── lineclear.wav
    │   └── gameover.wav
    └── Fonts/
        └── PressStart2P-Regular.ttf
```

---

#### RetroTetris.Tests (xUnit + FsCheck)

Projeto de testes. Referencia apenas `RetroTetris.Core` — sem dependência de MAUI.

```
RetroTetris.Tests/
├── RetroTetris.Tests.csproj
├── Arbitraries/
│   └── GameArbitraries.cs         ← geradores customizados FsCheck
├── Unit/
│   ├── BoardTests.cs
│   ├── TetrominoTests.cs
│   ├── SRSRotationSystemTests.cs
│   ├── BagRandomizerTests.cs
│   ├── LockDelayControllerTests.cs
│   ├── ScoreSystemTests.cs
│   ├── LevelSystemTests.cs
│   ├── CommandTests.cs            ← testa cada IGameCommand
│   ├── StateMachineTests.cs       ← testa transições de estado
│   └── CommandQueueTests.cs       ← testa thread-safety
├── Properties/
│   ├── MovementProperties.cs      ← Propriedades 1, 3, 4
│   ├── RotationProperties.cs      ← Propriedade 2
│   ├── LineClearProperties.cs     ← Propriedade 5
│   ├── ScoringProperties.cs       ← Propriedade 6
│   ├── BagProperties.cs           ← Propriedade 7
│   ├── SpeedProperties.cs         ← Propriedade 8
│   ├── HoldProperties.cs          ← Propriedade 9
│   └── LevelProperties.cs         ← Propriedade 10
└── Integration/
    ├── HighScoreRepositoryTests.cs ← lê/escreve arquivo real
    └── GameCycleTests.cs           ← ciclo completo: start → move → lock → game over
```

---

## Propriedades de Correção

*Uma propriedade é uma característica ou comportamento que deve ser verdadeiro em todas as execuções válidas de um sistema — essencialmente, uma declaração formal sobre o que o sistema deve fazer. As propriedades servem como ponte entre especificações legíveis por humanos e garantias de correção verificáveis por máquina.*

### Propriedade 1: Movimento lateral respeita limites do board

*Para qualquer* estado de board e qualquer peça ativa, mover a peça para a esquerda ou direita nunca deve resultar em uma posição onde alguma célula da peça esteja fora dos limites de coluna [0, 9] ou sobreponha uma célula ocupada.

**Valida: Requisitos 4.1, 4.2, 4.7**

### Propriedade 2: Rotação SRS preserva integridade do board

*Para qualquer* peça e estado de board, após uma rotação bem-sucedida (com ou sem wall-kick), todas as células da peça rotacionada devem estar dentro dos limites do board e não sobrepor células ocupadas.

**Valida: Requisitos 4.5, 4.6, 4.7**

### Propriedade 3: Hard drop posiciona peça na posição mais baixa possível

*Para qualquer* peça ativa e estado de board, após um hard drop, a peça deve estar na linha mais baixa possível tal que nenhuma célula abaixo dela esteja livre (ou seja, a linha imediatamente abaixo estaria ocupada ou fora dos limites).

**Valida: Requisito 4.4**

### Propriedade 4: Ghost piece coincide com destino do hard drop

*Para qualquer* peça ativa e estado de board, a posição calculada pela Ghost Piece deve ser idêntica à posição onde a peça seria fixada por um hard drop.

**Valida: Requisitos 8.1, 8.3**

### Propriedade 5: Eliminação de linhas preserva células não eliminadas

*Para qualquer* estado de board com uma ou mais linhas completas, após a eliminação, todas as células que estavam em linhas incompletas devem estar presentes no board resultante, deslocadas para baixo pelo número de linhas eliminadas abaixo delas.

**Valida: Requisitos 6.1, 6.2**

### Propriedade 6: Pontuação de linhas é calculada corretamente

*Para qualquer* nível e número de linhas eliminadas simultaneamente (1–4), o incremento de pontuação deve ser exatamente `multiplier[n] × level`, onde multiplier = [100, 300, 500, 800].

**Valida: Requisitos 6.4, 6.5, 6.6, 6.7**

### Propriedade 7: Bag randomizer garante distribuição uniforme

*Para qualquer* sequência de N ciclos completos do bag randomizer (N × 7 peças), cada um dos 7 tipos de tetrominó deve aparecer exatamente N vezes.

**Valida: Requisito 3.5**

### Propriedade 8: Intervalo de queda segue a fórmula do nível

*Para qualquer* nível entre 1 e 15, o intervalo de queda calculado deve ser `max(100, 1000 - (level - 1) × 90)` milissegundos.

**Valida: Requisitos 7.1, 7.3, 7.5**

### Propriedade 9: Hold é bloqueado após uso até fixação da peça

*Para qualquer* sequência de ações, se o hold foi acionado para a peça atual, acionar hold novamente antes de a peça ser fixada não deve alterar o estado do hold nem da peça ativa.

**Valida: Requisito 9.4**

### Propriedade 10: Nível incrementa a cada 10 linhas eliminadas

*Para qualquer* quantidade total de linhas eliminadas L, o nível deve ser `min(15, 1 + floor(L / 10))`.

**Valida: Requisitos 7.2, 7.5**

---

## Tratamento de Erros

### Falhas de Áudio

O `AudioService` envolve todas as chamadas ao `Plugin.Maui.Audio` em blocos `try/catch`. Falhas de áudio são silenciosas — o jogo continua normalmente. Um flag `_audioAvailable` é definido como `false` na primeira falha e impede tentativas subsequentes.

### Falhas de Persistência

O `HighScoreRepository` trata exceções de I/O ao carregar (retorna 0 como padrão) e ao salvar (loga o erro, não propaga). O jogo nunca deve travar por falha de persistência.

### Entrada Inválida

O `InputHandler` ignora teclas não mapeadas. O `GameEngine` ignora comandos de movimento/rotação quando o estado atual não é `PlayingState` — cada `IGameState` decide quais comandos são válidos a partir dele.

### Overflow de Pontuação

O `Score` é do tipo `int` (máximo ~2,1 bilhões). Com pontuação máxima de 800 × 15 = 12.000 por Tetris, seriam necessários ~175.000 Tetrises para overflow — improvável em uso normal. Não é necessário tratamento especial.

### Geração de Peça no Topo Bloqueado

Quando uma nova peça é gerada e sua posição inicial já está ocupada (game over), o `GameEngine` transiciona imediatamente para `GameState.GameOver` antes de qualquer renderização da peça.

---

## Estratégia de Testes

### Abordagem Dual

Os testes cobrem dois níveis complementares:

1. **Testes unitários** — exemplos concretos, casos de borda e condições de erro
2. **Testes de propriedade** — propriedades universais verificadas com centenas de entradas geradas aleatoriamente

### Biblioteca de Testes de Propriedade

**FsCheck** (versão 3.x) para .NET — biblioteca madura, integra com xUnit/NUnit, suporta geradores customizados.

```xml
<PackageReference Include="FsCheck.Xunit" Version="3.*" />
```

Cada teste de propriedade executa mínimo de **100 iterações** (padrão FsCheck).

### Testes Unitários (xUnit)

Cobrem:
- Formas corretas de cada tetrominó em cada estado de rotação
- Wall-kick: casos específicos documentados no Tetris Wiki
- Eliminação de linhas: 0, 1, 2, 3, 4 linhas simultâneas
- Lock delay: expiração, reset, limite de 15 resets
- Hold: primeira troca, troca subsequente, bloqueio duplo
- Game over: peça gerada em posição bloqueada
- Persistência: serialização/deserialização do `HighScoreData`
- Cálculo de nível: transições em múltiplos de 10 linhas
- **Command Pattern**: cada `IGameCommand` chama o método correto do engine
- **State Machine**: transições válidas e inválidas entre estados; `Enter`/`Exit` chamados corretamente
- **CommandQueue**: thread-safety com enqueue/dequeue concorrentes
- **Observer**: eventos disparados com os parâmetros corretos nos momentos certos

### Testes de Propriedade (FsCheck)

Cada propriedade do design é implementada como um único teste de propriedade:

```csharp
// Feature: retro-tetris, Property 1: Movimento lateral respeita limites
[Property]
public Property MovimentoLateralRespeitaLimites(BoardState state, TetrominoType type) { ... }

// Feature: retro-tetris, Property 2: Rotação SRS preserva integridade
[Property]
public Property RotacaoSRSPreservaIntegridade(BoardState state, TetrominoType type, bool clockwise) { ... }

// Feature: retro-tetris, Property 3: Hard drop posiciona na posição mais baixa
[Property]
public Property HardDropPosicaoMaisBaixa(BoardState state, TetrominoType type) { ... }

// Feature: retro-tetris, Property 4: Ghost piece coincide com hard drop
[Property]
public Property GhostPieceCoincideComHardDrop(BoardState state, TetrominoType type) { ... }

// Feature: retro-tetris, Property 5: Eliminação preserva células não eliminadas
[Property]
public Property EliminacaoPreservaCelulas(BoardState state) { ... }

// Feature: retro-tetris, Property 6: Pontuação calculada corretamente
[Property]
public Property PontuacaoLinhasCorreta(PositiveInt level, int lines) { ... }

// Feature: retro-tetris, Property 7: Bag randomizer distribuição uniforme
[Property]
public Property BagRandomizerDistribuicaoUniforme(PositiveInt cycles) { ... }

// Feature: retro-tetris, Property 8: Intervalo de queda segue fórmula
[Property]
public Property IntervaloQuedaSegueFórmula(int level) { ... }

// Feature: retro-tetris, Property 9: Hold bloqueado após uso
[Property]
public Property HoldBloqueadoAposUso(BoardState state) { ... }

// Feature: retro-tetris, Property 10: Nível incrementa a cada 10 linhas
[Property]
public Property NivelIncrementaACada10Linhas(NonNegativeInt totalLines) { ... }
```

### Geradores Customizados FsCheck

```csharp
public static class GameArbitraries
{
    // Gera estados de board com células aleatoriamente ocupadas (mas válidos)
    public static Arbitrary<BoardState> BoardState();

    // Gera tetrominós em posições válidas dentro do board
    public static Arbitrary<PlacedTetromino> PlacedTetromino();
}
```

### Testes de Integração

- Carregamento e salvamento do `HighScoreRepository` com arquivo real
- Inicialização do `AudioService` (verifica que não lança exceção)
- Ciclo completo de jogo: start → move → lock → line clear → game over

### O que NÃO é testado com PBT

- Renderização visual (`GameRenderer`) — testada com snapshots visuais manuais
- Animações (flash de linhas, efeitos retrô) — verificação visual
- Input de teclado/toque — testado manualmente em dispositivo
- Áudio — testado manualmente
- `GameLoop` (thread + timing) — testado com testes de integração; o delta time real não é determinístico
