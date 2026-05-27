using System.Diagnostics;
using System.Text.Json;
using RetroTetris.Core.Interfaces;
using RetroTetris.Core.Models;

namespace RetroTetris.Services;

/// <summary>
/// Persists the high score to a JSON file in the app's data directory.
/// Uses MAUI's FileSystem.AppDataDirectory for cross-platform path resolution.
/// </summary>
public class HighScoreRepository : IHighScoreRepository
{
    private const string FileName = "highscore.json";

    private string FilePath => Path.Combine(FileSystem.AppDataDirectory, FileName);

    /// <summary>
    /// Loads the high score from disk. Returns 0 if the file doesn't exist or on any error.
    /// </summary>
    public async Task<int> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
                return 0;

            var json = await File.ReadAllTextAsync(FilePath);
            var data = JsonSerializer.Deserialize<HighScoreData>(json);
            return data?.Score ?? 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HighScoreRepository] LoadAsync failed: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Saves the high score to disk. Logs and swallows any I/O errors.
    /// </summary>
    public async Task SaveAsync(int score)
    {
        try
        {
            var data = new HighScoreData(score, DateTime.UtcNow);
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HighScoreRepository] SaveAsync failed: {ex.Message}");
        }
    }
}
