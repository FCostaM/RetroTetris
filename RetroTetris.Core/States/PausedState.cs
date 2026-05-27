using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.States;

/// <summary>
/// Paused state — suspends automatic drop and lock delay, hides board content.
/// Transitions back to PlayingState on Resume().
/// </summary>
public class PausedState : IGameState
{
    public void Enter(IGameEngine engine) { }
    public void Update(IGameEngine engine, TimeSpan delta) { }
    public void Exit(IGameEngine engine) { }
}
