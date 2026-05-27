using System.Text.Json;
using RetroTetris.Core.Interfaces;
using RetroTetris.Core.Models;

namespace RetroTetris.Tests.Integration;

/// <summary>
/// Integration tests for HighScoreRepository logic using a testable version
/// that accepts a custom directory path instead of MAUI's FileSystem.AppDataDirectory.
/// </summary>
public class HighScoreRepositoryTests
{
    [Fact]
    public async Task LoadAsync_FileDoesNotExist_ReturnsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var repo = new TestableHighScoreRepository(dir);
            var result = await repo.LoadAsync();
            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ReturnsSavedScore()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var repo = new TestableHighScoreRepository(dir);
            await repo.SaveAsync(12345);
            var result = await repo.LoadAsync();
            Assert.Equal(12345, result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_OverwritesPreviousScore()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var repo = new TestableHighScoreRepository(dir);
            await repo.SaveAsync(100);
            await repo.SaveAsync(200);
            var result = await repo.LoadAsync();
            Assert.Equal(200, result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_CorruptFile_ReturnsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var filePath = Path.Combine(dir, "highscore.json");
            await File.WriteAllTextAsync(filePath, "this is not valid json {{{{");
            var repo = new TestableHighScoreRepository(dir);
            var result = await repo.LoadAsync();
            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_CreatesFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var repo = new TestableHighScoreRepository(dir);
            await repo.SaveAsync(42);
            var filePath = Path.Combine(dir, "highscore.json");
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_EmptyFile_ReturnsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var filePath = Path.Combine(dir, "highscore.json");
            await File.WriteAllTextAsync(filePath, string.Empty);
            var repo = new TestableHighScoreRepository(dir);
            var result = await repo.LoadAsync();
            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ZeroScore_SavesAndLoadsCorrectly()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var repo = new TestableHighScoreRepository(dir);
            await repo.SaveAsync(0);
            var result = await repo.LoadAsync();
            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// Testable version of HighScoreRepository that accepts a custom directory path
    /// instead of using FileSystem.AppDataDirectory (MAUI API not available in tests).
    /// </summary>
    private class TestableHighScoreRepository : IHighScoreRepository
    {
        private readonly string _directory;
        private const string FileName = "highscore.json";

        public TestableHighScoreRepository(string directory)
        {
            _directory = directory;
        }

        private string FilePath => Path.Combine(_directory, FileName);

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
            catch
            {
                return 0;
            }
        }

        public async Task SaveAsync(int score)
        {
            try
            {
                var data = new HighScoreData(score, DateTime.UtcNow);
                var json = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(FilePath, json);
            }
            catch { }
        }
    }
}
