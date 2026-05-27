namespace RetroTetris.Core.Engine;

/// <summary>
/// Tracks the player's score based on lines cleared and current level.
/// Scoring table: 1 line → 100×L, 2 lines → 300×L, 3 lines → 500×L, 4 lines → 800×L
/// </summary>
public class ScoreSystem
{
    // Index 0 is unused (0 lines cleared yields no score)
    private static readonly int[] _multipliers = { 0, 100, 300, 500, 800 };

    public int Score { get; private set; }

    /// <summary>
    /// Adds score based on the number of lines cleared and the current level.
    /// </summary>
    /// <param name="linesCleared">Number of lines cleared (1–4). Values outside this range are ignored.</param>
    /// <param name="level">Current level multiplier.</param>
    public void AddLinesClear(int linesCleared, int level)
    {
        if (linesCleared <= 0 || linesCleared >= _multipliers.Length)
            return;

        Score += _multipliers[linesCleared] * level;
    }

    /// <summary>
    /// Resets the score back to 0.
    /// </summary>
    public void Reset()
    {
        Score = 0;
    }
}
