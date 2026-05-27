using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class HardDropCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.HardDrop();
}
