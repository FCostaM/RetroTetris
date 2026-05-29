using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;

namespace RetroTetris.Core.Commands;

/// <summary>
/// Alterna a Controls_Screen: fecha se estiver aberta, abre se estiver em StartScreenState ou PausedState.
/// No-op em qualquer outro estado (ex: PlayingState, GameOverState).
/// Mapeado para GameKey.H no InputHandler.
/// </summary>
public sealed class ToggleControlsCommand : IGameCommand
{
    public void Execute(IGameEngine engine)
    {
        if (engine.CurrentState is ControlsScreenState)
            engine.HideControls();
        else if (engine.CurrentState is StartScreenState or PausedState)
            engine.ShowControls();
    }
}
