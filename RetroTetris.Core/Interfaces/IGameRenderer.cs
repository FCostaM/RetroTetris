namespace RetroTetris.Core.Interfaces;

public interface IGameRenderer
{
    void SetGameEngine(IGameEngine engine);
    void SetFlashingLines(IReadOnlyList<int> lines);
}
