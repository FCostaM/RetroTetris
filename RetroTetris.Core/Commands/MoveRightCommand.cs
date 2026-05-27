using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class MoveRightCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.MoveRight();
}
