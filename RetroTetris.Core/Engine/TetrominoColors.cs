namespace RetroTetris.Core.Engine;

/// <summary>
/// Maps each TetrominoType to its fixed retro color as a hex string.
/// Uses string hex values (not Microsoft.Maui.Graphics.Color) so that
/// RetroTetris.Core has no MAUI dependency.
/// </summary>
public static class TetrominoColors
{
    public static readonly IReadOnlyDictionary<TetrominoType, string> Primary =
        new Dictionary<TetrominoType, string>
        {
            [TetrominoType.I] = "#00F0F0",  // cyan
            [TetrominoType.O] = "#F0F000",  // yellow
            [TetrominoType.T] = "#A000F0",  // purple
            [TetrominoType.S] = "#00F000",  // green
            [TetrominoType.Z] = "#F00000",  // red
            [TetrominoType.J] = "#0000F0",  // blue
            [TetrominoType.L] = "#F0A000",  // orange
        };
}
