using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.States;

/// <summary>
/// Initial state — waits for StartNewGame() to be called.
/// The game loop is inactive while in this state.
/// </summary>
public class StartScreenState : IGameState
{
    public void Enter(IGameEngine engine) { }
    public void Update(IGameEngine engine, TimeSpan delta) { }
    public void Exit(IGameEngine engine) { }
}
