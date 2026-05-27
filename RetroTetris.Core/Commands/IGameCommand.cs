using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.Commands;

public interface IGameCommand
{
    void Execute(IGameEngine engine);
}
