using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;

namespace RetroTetris.Core.Commands;

/// <summary>
/// Abre a Controls_Screen a partir de StartScreenState ou PausedState.
/// No-op em qualquer outro estado.
/// </summary>
public sealed class ShowControlsCommand : IGameCommand
{
    public void Execute(IGameEngine engine)
    {
        if (engine.CurrentState is StartScreenState or PausedState)
            engine.ShowControls();
    }
}
