using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class RotateCounterClockwiseCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.RotateCounterClockwise();
}
