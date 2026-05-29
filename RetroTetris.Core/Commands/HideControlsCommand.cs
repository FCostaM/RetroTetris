using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;

namespace RetroTetris.Core.Commands;

/// <summary>
/// Fecha a Controls_Screen e retorna ao PreviousState armazenado.
/// No-op se não estiver em ControlsScreenState.
/// </summary>
public sealed class HideControlsCommand : IGameCommand
{
    public void Execute(IGameEngine engine)
    {
        if (engine.CurrentState is ControlsScreenState)
            engine.HideControls();
    }
}
