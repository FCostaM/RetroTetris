using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.States;

/// <summary>
/// Estado que representa a Controls_Screen ativa.
/// Suspende o game loop e armazena o estado anterior para retorno exato.
/// </summary>
public class ControlsScreenState : IGameState
{
    /// <summary>
    /// Estado imediatamente anterior à abertura da Controls_Screen.
    /// Sempre StartScreenState ou PausedState — nunca outro ControlsScreenState.
    /// </summary>
    public IGameState PreviousState { get; }

    public ControlsScreenState(IGameState previousState)
    {
        // Invariante: previousState nunca pode ser ControlsScreenState
        if (previousState is ControlsScreenState)
            throw new ArgumentException(
                "ControlsScreenState não pode ser aninhado.", nameof(previousState));

        PreviousState = previousState;
    }

    public void Enter(IGameEngine engine) { }

    /// <summary>
    /// Update é no-op: o game loop permanece suspenso enquanto a Controls_Screen está ativa.
    /// </summary>
    public void Update(IGameEngine engine, TimeSpan delta) { }

    public void Exit(IGameEngine engine) { }
}
