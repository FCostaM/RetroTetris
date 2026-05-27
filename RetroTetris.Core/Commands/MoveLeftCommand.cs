using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class MoveLeftCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.MoveLeft();
}
