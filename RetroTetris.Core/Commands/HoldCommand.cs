using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class HoldCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.Hold();
}
