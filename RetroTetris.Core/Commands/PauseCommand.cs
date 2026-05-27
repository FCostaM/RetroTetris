using RetroTetris.Core.Interfaces;
namespace RetroTetris.Core.Commands;
public sealed class PauseCommand : IGameCommand
{
    public void Execute(IGameEngine e) => e.Pause();
}
