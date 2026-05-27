using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.States;

public interface IGameState
{
    void Enter(IGameEngine engine);
    void Update(IGameEngine engine, TimeSpan delta);
    void Exit(IGameEngine engine);
}
