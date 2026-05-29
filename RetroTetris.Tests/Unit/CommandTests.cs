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
        public void ShowControls() => LastCalledMethod = nameof(ShowControls);
        public void HideControls() => LastCalledMethod = nameof(HideControls);
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

    // ─── ShowControlsCommand tests ─────────────────────────────────────────────

    [Fact]
    public void ShowControlsCommand_FromStartScreen_CallsShowControls()
    {
        var stub = new StubEngine { CurrentState = new StartScreenState() };
        new ShowControlsCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.ShowControls), stub.LastCalledMethod);
    }

    [Fact]
    public void ShowControlsCommand_FromPausedState_CallsShowControls()
    {
        var stub = new StubEngine { CurrentState = new PausedState() };
        new ShowControlsCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.ShowControls), stub.LastCalledMethod);
    }

    [Fact]
    public void ShowControlsCommand_FromPlayingState_IsNoOp()
    {
        var stub = new StubEngine { CurrentState = new PlayingState() };
        new ShowControlsCommand().Execute(stub);
        Assert.Null(stub.LastCalledMethod);
    }

    [Fact]
    public void ShowControlsCommand_FromControlsScreenState_IsNoOp()
    {
        var stub = new StubEngine { CurrentState = new ControlsScreenState(new StartScreenState()) };
        new ShowControlsCommand().Execute(stub);
        Assert.Null(stub.LastCalledMethod);
    }

    // ─── HideControlsCommand tests ─────────────────────────────────────────────

    [Fact]
    public void HideControlsCommand_FromControlsScreenState_CallsHideControls()
    {
        var stub = new StubEngine { CurrentState = new ControlsScreenState(new StartScreenState()) };
        new HideControlsCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.HideControls), stub.LastCalledMethod);
    }

    [Fact]
    public void HideControlsCommand_FromOtherState_IsNoOp()
    {
        var stub = new StubEngine { CurrentState = new PlayingState() };
        new HideControlsCommand().Execute(stub);
        Assert.Null(stub.LastCalledMethod);
    }

    // ─── ToggleControlsCommand tests ───────────────────────────────────────────

    [Fact]
    public void ToggleControlsCommand_FromStartScreen_CallsShowControls()
    {
        var stub = new StubEngine { CurrentState = new StartScreenState() };
        new ToggleControlsCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.ShowControls), stub.LastCalledMethod);
    }

    [Fact]
    public void ToggleControlsCommand_FromPausedState_CallsShowControls()
    {
        var stub = new StubEngine { CurrentState = new PausedState() };
        new ToggleControlsCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.ShowControls), stub.LastCalledMethod);
    }

    [Fact]
    public void ToggleControlsCommand_FromControlsScreenState_CallsHideControls()
    {
        var stub = new StubEngine { CurrentState = new ControlsScreenState(new PausedState()) };
        new ToggleControlsCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.HideControls), stub.LastCalledMethod);
    }

    [Fact]
    public void ToggleControlsCommand_FromPlayingState_IsNoOp()
    {
        var stub = new StubEngine { CurrentState = new PlayingState() };
        new ToggleControlsCommand().Execute(stub);
        Assert.Null(stub.LastCalledMethod);
    }

    // ─── TogglePauseCommand — ControlsScreenState branch ──────────────────────

    [Fact]
    public void TogglePauseCommand_FromControlsScreenState_CallsHideControls()
    {
        var stub = new StubEngine { CurrentState = new ControlsScreenState(new PausedState()) };
        new TogglePauseCommand().Execute(stub);
        Assert.Equal(nameof(StubEngine.HideControls), stub.LastCalledMethod);
    }
}
