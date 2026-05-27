using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.States;

/// <summary>
/// Active gameplay state — handles automatic piece drop, lock delay, and line clearing.
/// Transitions to PausedState on Pause() and GameOverState when a piece spawns on occupied cells.
/// </summary>
public class PlayingState : IGameState
{
    public void Enter(IGameEngine engine) { }
    public void Update(IGameEngine engine, TimeSpan delta) { }
    public void Exit(IGameEngine engine) { }
}
