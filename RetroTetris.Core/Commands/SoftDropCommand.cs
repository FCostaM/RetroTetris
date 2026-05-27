using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class SoftDropCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.SoftDrop();
}
