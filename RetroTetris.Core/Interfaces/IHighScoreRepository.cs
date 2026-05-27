namespace RetroTetris.Core.Interfaces;

public interface IHighScoreRepository
{
    Task<int> LoadAsync();
    Task SaveAsync(int score);
}
