using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;

namespace RetroTetris.Core.Commands;

/// <summary>
/// Toggles between paused and playing states.
/// Calls Pause() when in PlayingState, Resume() when in PausedState.
/// </summary>
public sealed class TogglePauseCommand : IGameCommand
{
    public void Execute(IGameEngine e)
    {
        if (e.CurrentState is PlayingState)
            e.Pause();
        else if (e.CurrentState is PausedState)
            e.Resume();
    }
}
