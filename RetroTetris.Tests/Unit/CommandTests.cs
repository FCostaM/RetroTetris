using RetroTetris.Core.Commands;
using RetroTetris.Core.Engine;
using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;

namespace RetroTetris.Tests.Unit;

public class CommandTests
{
    // ─── Hand-rolled stub ──────────────────────────────────────────────────────

    private class StubEngine : IGameEngine
    {
        public string? LastCalledMethod { get; private set; }

        // IGameEngine properties — return defaults
        public IGameState CurrentState { get; set; } = null!;
        public Board Board => null!;
        public Tetromino? ActivePiece => null;
        public Tetromino? GhostPiece => null;
        public Tetromino? HoldPiece => null;
        public IReadOnlyList<Tetromino> NextPieces => Array.Empty<Tetromino>();
        public int Score => 0;
        public int HighScore => 0;
        public int Level => 0;
        public int TotalLinesCleared => 0;

        // Events — required by IGameEngine but not observed in these tests
#pragma warning disable CS0067
        public event Action<int, int>? LinesCleared;
        public event Action<int>? ScoreChanged;
        public event Action<int>? LevelChanged;
        public event Action? PieceLocked;
        public event Action<TetrominoType>? PieceSpawned;
        public event Action? GameOverOccurred;
#pragma warning restore CS0067

        // Lifecycle — record call
        public void StartNewGame() => LastCalledMethod = nameof(StartNewGame);
        public void TransitionTo(IGameState s) => LastCalledMethod = nameof(TransitionTo);
        public void Update(TimeSpan d) => LastCalledMethod = nameof(Update);

        // Player actions — record call
        public void MoveLeft() => LastCalledMethod = nameof(MoveLeft);
        public void MoveRight() => LastCalledMethod = nameof(MoveRight);
        public void SoftDrop() => LastCalledMethod = nameof(SoftDrop);
        public void HardDrop() => LastCalledMethod = nameof(HardDrop);
        public void RotateClockwise() => LastCalledMethod = nameof(RotateClockwise);
        public void RotateCounterClockwise() => LastCalledMethod = nameof(RotateCounterClockwise);
        public void Hold() => LastCalledMethod = nameof(Hold);
        public void Pause() => LastCalledMethod = nameof(Pause);
        public void Resume() => LastCalledMethod = nameof(Resume);
    }

    // ─── Command tests ─────────────────────────────────────────────────────────

    [Fact]
    public void MoveLeftCommand_Execute_CallsMoveLeft()
    {
        var stub = new StubEngine();
        new MoveLeftCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.MoveLeft), stub.LastCalledMethod);
    }

    [Fact]
    public void MoveRightCommand_Execute_CallsMoveRight()
    {
        var stub = new StubEngine();
        new MoveRightCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.MoveRight), stub.LastCalledMethod);
    }

    [Fact]
    public void SoftDropCommand_Execute_CallsSoftDrop()
    {
        var stub = new StubEngine();
        new SoftDropCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.SoftDrop), stub.LastCalledMethod);
    }

    [Fact]
    public void HardDropCommand_Execute_CallsHardDrop()
    {
        var stub = new StubEngine();
        new HardDropCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.HardDrop), stub.LastCalledMethod);
    }

    [Fact]
    public void RotateClockwiseCommand_Execute_CallsRotateClockwise()
    {
        var stub = new StubEngine();
        new RotateClockwiseCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.RotateClockwise), stub.LastCalledMethod);
    }

    [Fact]
    public void RotateCounterClockwiseCommand_Execute_CallsRotateCounterClockwise()
    {
        var stub = new StubEngine();
        new RotateCounterClockwiseCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.RotateCounterClockwise), stub.LastCalledMethod);
    }

    [Fact]
    public void HoldCommand_Execute_CallsHold()
    {
        var stub = new StubEngine();
        new HoldCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.Hold), stub.LastCalledMethod);
    }

    [Fact]
    public void TogglePauseCommand_WhenPlaying_CallsPause()
    {
        var stub = new StubEngine { CurrentState = new PlayingState() };
        new TogglePauseCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.Pause), stub.LastCalledMethod);
    }

    [Fact]
    public void TogglePauseCommand_WhenPaused_CallsResume()
    {
        var stub = new StubEngine { CurrentState = new PausedState() };
        new TogglePauseCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.Resume), stub.LastCalledMethod);
    }
}
