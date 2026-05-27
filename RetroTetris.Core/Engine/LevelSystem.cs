namespace RetroTetris.Core.Engine;

/// <summary>
/// Tracks the current level and total lines cleared.
/// Level advances every 10 lines, capped at 15.
/// Drop interval decreases with each level.
/// </summary>
public class LevelSystem
{
    public int Level { get; private set; } = 1;
    public int TotalLinesCleared { get; private set; } = 0;

    /// <summary>
    /// Adds <paramref name="count"/> cleared lines and recalculates the current level.
    /// Level = min(15, 1 + TotalLinesCleared / 10)
    /// </summary>
    public void AddLines(int count)
    {
        TotalLinesCleared += count;
        Level = Math.Min(15, 1 + TotalLinesCleared / 10);
    }

    /// <summary>
    /// Returns the drop interval in milliseconds for the current level.
    /// Formula: max(100, 1000 - (Level - 1) * 90)
    /// Level 1 → 1000ms, Level 2 → 910ms, ..., Level 11+ → 100ms (capped)
    /// </summary>
    public int GetDropIntervalMs()
    {
        return Math.Max(100, 1000 - (Level - 1) * 90);
    }

    /// <summary>
    /// Resets level to 1 and total lines cleared to 0.
    /// </summary>
    public void Reset()
    {
        Level = 1;
        TotalLinesCleared = 0;
    }
}
