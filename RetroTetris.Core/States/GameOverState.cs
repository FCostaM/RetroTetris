using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.States;

/// <summary>
/// Game over state — fires GameOverOccurred event, displays final score.
/// Transitions back to PlayingState on StartNewGame() (Try Again).
/// The GameEngine fires the GameOverOccurred event when transitioning to this state.
/// </summary>
public class GameOverState : IGameState
{
    public void Enter(IGameEngine engine) { }
    public void Update(IGameEngine engine, TimeSpan delta) { }
    public void Exit(IGameEngine engine) { }
}
