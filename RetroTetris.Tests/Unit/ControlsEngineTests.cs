using RetroTetris.Core.Engine;
using RetroTetris.Core.States;

namespace RetroTetris.Tests.Unit;

/// <summary>
/// Unit tests for GameEngine.ShowControls() and GameEngine.HideControls().
/// Validates Requirements 1.3, 2.3, 4.1, 4.2, 4.3, 4.4.
/// </summary>
public class ControlsEngineTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a GameEngine and forces it into the given state by directly
    /// calling TransitionTo, bypassing the normal game flow.
    /// </summary>
    private static GameEngine CreateEngineInState(Func<GameEngine, IGameState> stateFactory)
    {
        var engine = new GameEngine();
        engine.TransitionTo(stateFactory(engine));
        return engine;
    }

    // ─── ShowControls ─────────────────────────────────────────────────────────

    [Fact]
    public void ShowControls_FromStartScreen_TransitionsToControlsScreenState()
    {
        // GameEngine starts in StartScreenState by default
        var engine = new GameEngine();

        engine.ShowControls();

        Assert.IsType<ControlsScreenState>(engine.CurrentState);
    }

    [Fact]
    public void ShowControls_FromStartScreen_StoresStartScreenStateAsPreviousState()
    {
        var engine = new GameEngine();
        var originalState = engine.CurrentState;

        engine.ShowControls();

        var css = Assert.IsType<ControlsScreenState>(engine.CurrentState);
        Assert.Same(originalState, css.PreviousState);
    }

    [Fact]
    public void ShowControls_FromPausedState_TransitionsToControlsScreenState()
    {
        var engine = CreateEngineInState(_ => new PausedState());

        engine.ShowControls();

        Assert.IsType<ControlsScreenState>(engine.CurrentState);
    }

    [Fact]
    public void ShowControls_FromPausedState_StoresPausedStateAsPreviousState()
    {
        var pausedState = new PausedState();
        var engine = new GameEngine();
        engine.TransitionTo(pausedState);

        engine.ShowControls();

        var css = Assert.IsType<ControlsScreenState>(engine.CurrentState);
        Assert.Same(pausedState, css.PreviousState);
    }

    [Fact]
    public void ShowControls_InPlayingState_IsNoOp()
    {
        var engine = CreateEngineInState(_ => new PlayingState());

        engine.ShowControls();

        Assert.IsType<PlayingState>(engine.CurrentState);
    }

    [Fact]
    public void ShowControls_InGameOverState_IsNoOp()
    {
        var engine = CreateEngineInState(_ => new GameOverState());

        engine.ShowControls();

        Assert.IsType<GameOverState>(engine.CurrentState);
    }

    [Fact]
    public void ShowControls_InControlsScreenState_IsNoOp()
    {
        var engine = new GameEngine();
        engine.ShowControls(); // → ControlsScreenState
        var controlsState = engine.CurrentState;

        engine.ShowControls(); // should be no-op

        Assert.Same(controlsState, engine.CurrentState);
    }

    // ─── HideControls ─────────────────────────────────────────────────────────

    [Fact]
    public void HideControls_FromControlsScreen_ReturnsToStartScreen()
    {
        var engine = new GameEngine(); // starts in StartScreenState
        engine.ShowControls();         // → ControlsScreenState(StartScreenState)

        engine.HideControls();

        Assert.IsType<StartScreenState>(engine.CurrentState);
    }

    [Fact]
    public void HideControls_FromControlsScreen_ReturnsToPausedState()
    {
        var engine = CreateEngineInState(_ => new PausedState());
        engine.ShowControls(); // → ControlsScreenState(PausedState)

        engine.HideControls();

        Assert.IsType<PausedState>(engine.CurrentState);
    }

    [Fact]
    public void HideControls_FromControlsScreen_ReturnsSameInstanceAsPreviousState()
    {
        var pausedState = new PausedState();
        var engine = new GameEngine();
        engine.TransitionTo(pausedState);
        engine.ShowControls();

        engine.HideControls();

        Assert.Same(pausedState, engine.CurrentState);
    }

    [Fact]
    public void HideControls_OutsideControlsScreenState_IsNoOp_WhenInStartScreen()
    {
        var engine = new GameEngine(); // StartScreenState
        var stateBefore = engine.CurrentState;

        engine.HideControls();

        Assert.Same(stateBefore, engine.CurrentState);
    }

    [Fact]
    public void HideControls_OutsideControlsScreenState_IsNoOp_WhenInPausedState()
    {
        var engine = CreateEngineInState(_ => new PausedState());
        var stateBefore = engine.CurrentState;

        engine.HideControls();

        Assert.Same(stateBefore, engine.CurrentState);
    }

    [Fact]
    public void HideControls_OutsideControlsScreenState_IsNoOp_WhenInPlayingState()
    {
        var engine = CreateEngineInState(_ => new PlayingState());

        engine.HideControls();

        Assert.IsType<PlayingState>(engine.CurrentState);
    }

    // ─── Round-trip ───────────────────────────────────────────────────────────

    [Fact]
    public void ShowControls_ThenHideControls_ReturnsToExactOriginalInstance_FromStartScreen()
    {
        var engine = new GameEngine();
        var original = engine.CurrentState;

        engine.ShowControls();
        engine.HideControls();

        Assert.Same(original, engine.CurrentState);
    }

    [Fact]
    public void ShowControls_ThenHideControls_ReturnsToExactOriginalInstance_FromPausedState()
    {
        var pausedState = new PausedState();
        var engine = new GameEngine();
        engine.TransitionTo(pausedState);

        engine.ShowControls();
        engine.HideControls();

        Assert.Same(pausedState, engine.CurrentState);
    }
}
