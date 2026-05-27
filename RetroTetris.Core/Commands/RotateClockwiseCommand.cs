using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class RotateClockwiseCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.RotateClockwise();
}
