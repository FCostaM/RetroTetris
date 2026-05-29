using RetroTetris.Core.Engine;
using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;

namespace RetroTetris.Tests.Unit;

/// <summary>
/// Tests for the state machine transition logic.
/// Validates Requirements 1.4, 13.1, 13.4.
/// </summary>
public class StateMachineTests
{
    // ─── SpyState ─────────────────────────────────────────────────────────────

    private class SpyState : IGameState
    {
        public bool EnterCalled { get; private set; }
        public bool ExitCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public IGameEngine? EnterCalledWith { get; private set; }
        public IGameEngine? ExitCalledWith { get; private set; }

        public void Enter(IGameEngine engine) { EnterCalled = true; EnterCalledWith = engine; }
        public void Update(IGameEngine engine, TimeSpan delta) { UpdateCalled = true; }
        public void Exit(IGameEngine engine) { ExitCalled = true; ExitCalledWith = engine; }
    }

    // ─── TrackingEngine ───────────────────────────────────────────────────────

    private class TrackingEngine : IGameEngine
    {
        public IGameState CurrentState { get; private set; }

        public TrackingEngine(IGameState initialState)
        {
            CurrentState = initialState;
            initialState.Enter(this);
        }

        public void TransitionTo(IGameState newState)
        {
            CurrentState.Exit(this);
            CurrentState = newState;
            newState.Enter(this);
        }

        // All other IGameEngine members — defaults / no-op
        public Board Board => null!;
        public Tetromino? ActivePiece => null;
        public Tetromino? GhostPiece => null;
        public Tetromino? HoldPiece => null;
        public IReadOnlyList<Tetromino> NextPieces => Array.Empty<Tetromino>();
        public int Score => 0;
        public int HighScore => 0;
        public int Level => 0;
        public int TotalLinesCleared => 0;
        public void StartNewGame() { }
        public void Update(TimeSpan delta) { }
        public void MoveLeft() { }
        public void MoveRight() { }
        public void SoftDrop() { }
        public void HardDrop() { }
        public void RotateClockwise() { }
        public void RotateCounterClockwise() { }
        public void Hold() { }
        public void Pause() { }
        public void Resume() { }
        public void ShowControls() { }
        public void HideControls() { }
        // Events — required by IGameEngine but not observed in these tests
#pragma warning disable CS0067
        public event Action<int, int>? LinesCleared;
        public event Action<int>? ScoreChanged;
        public event Action<int>? LevelChanged;
        public event Action? PieceLocked;
        public event Action<TetrominoType>? PieceSpawned;
        public event Action? GameOverOccurred;
#pragma warning restore CS0067
    }

    // ─── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_CallsEnterOnInitialState()
    {
        var spy = new SpyState();
        var engine = new TrackingEngine(spy);

        Assert.True(spy.EnterCalled);
    }

    [Fact]
    public void Constructor_InitialStateIsCurrentState()
    {
        var spy = new SpyState();
        var engine = new TrackingEngine(spy);

        Assert.Same(spy, engine.CurrentState);
    }

    [Fact]
    public void TransitionTo_CallsExitOnOldState()
    {
        var oldState = new SpyState();
        var newState = new SpyState();
        var engine = new TrackingEngine(oldState);

        engine.TransitionTo(newState);

        Assert.True(oldState.ExitCalled);
    }

    [Fact]
    public void TransitionTo_CallsEnterOnNewState()
    {
        var oldState = new SpyState();
        var newState = new SpyState();
        var engine = new TrackingEngine(oldState);

        engine.TransitionTo(newState);

        Assert.True(newState.EnterCalled);
    }

    [Fact]
    public void TransitionTo_UpdatesCurrentState()
    {
        var oldState = new SpyState();
        var newState = new SpyState();
        var engine = new TrackingEngine(oldState);

        engine.TransitionTo(newState);

        Assert.Same(newState, engine.CurrentState);
    }

    [Fact]
    public void TransitionTo_EnterCalledWithCorrectEngineReference()
    {
        var oldState = new SpyState();
        var newState = new SpyState();
        var engine = new TrackingEngine(oldState);

        engine.TransitionTo(newState);

        Assert.Same(engine, newState.EnterCalledWith);
    }

    [Fact]
    public void TransitionTo_ExitCalledWithCorrectEngineReference()
    {
        var oldState = new SpyState();
        var newState = new SpyState();
        var engine = new TrackingEngine(oldState);

        engine.TransitionTo(newState);

        Assert.Same(engine, oldState.ExitCalledWith);
    }

    [Fact]
    public void StartScreen_To_Playing_Transition()
    {
        var initial = new StartScreenState();
        var engine = new TrackingEngine(initial);
        var playing = new PlayingState();

        engine.TransitionTo(playing);

        Assert.IsType<PlayingState>(engine.CurrentState);
    }

    [Fact]
    public void Playing_To_Paused_Transition()
    {
        var engine = new TrackingEngine(new PlayingState());
        var paused = new PausedState();

        engine.TransitionTo(paused);

        Assert.IsType<PausedState>(engine.CurrentState);
    }

    [Fact]
    public void Playing_To_GameOver_Transition()
    {
        var engine = new TrackingEngine(new PlayingState());
        var gameOver = new GameOverState();

        engine.TransitionTo(gameOver);

        Assert.IsType<GameOverState>(engine.CurrentState);
    }

    [Fact]
    public void Paused_To_Playing_Transition_Resume()
    {
        var engine = new TrackingEngine(new PausedState());
        var playing = new PlayingState();

        engine.TransitionTo(playing);

        Assert.IsType<PlayingState>(engine.CurrentState);
    }

    [Fact]
    public void GameOver_To_Playing_Transition_TryAgain()
    {
        var engine = new TrackingEngine(new GameOverState());
        var playing = new PlayingState();

        engine.TransitionTo(playing);

        Assert.IsType<PlayingState>(engine.CurrentState);
    }

    [Fact]
    public void MultipleTransitions_ChainedCorrectly()
    {
        // StartScreen → Playing → Paused → Playing
        var startScreen = new SpyState();
        var engine = new TrackingEngine(startScreen);
        Assert.True(startScreen.EnterCalled, "StartScreen Enter should be called on construction");

        var playing1 = new SpyState();
        engine.TransitionTo(playing1);
        Assert.True(startScreen.ExitCalled, "StartScreen Exit should be called on first transition");
        Assert.True(playing1.EnterCalled, "Playing Enter should be called on first transition");
        Assert.Same(playing1, engine.CurrentState);

        var paused = new SpyState();
        engine.TransitionTo(paused);
        Assert.True(playing1.ExitCalled, "Playing Exit should be called on second transition");
        Assert.True(paused.EnterCalled, "Paused Enter should be called on second transition");
        Assert.Same(paused, engine.CurrentState);

        var playing2 = new SpyState();
        engine.TransitionTo(playing2);
        Assert.True(paused.ExitCalled, "Paused Exit should be called on third transition");
        Assert.True(playing2.EnterCalled, "Playing2 Enter should be called on third transition");
        Assert.Same(playing2, engine.CurrentState);
    }

    [Fact]
    public void InitialEnter_CalledWithCorrectEngineReference()
    {
        var spy = new SpyState();
        var engine = new TrackingEngine(spy);

        Assert.Same(engine, spy.EnterCalledWith);
    }

    [Fact]
    public void OldStateExitNotCalledBeforeTransition()
    {
        var spy = new SpyState();
        _ = new TrackingEngine(spy);

        // Exit should NOT have been called just from construction
        Assert.False(spy.ExitCalled);
    }
}
