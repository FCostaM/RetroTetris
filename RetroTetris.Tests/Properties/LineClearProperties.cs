using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 5: Line clearing preserves non-cleared cells.
///
/// For any board state with one or more complete lines, after clearing,
/// all cells that were in incomplete lines must be present in the resulting board,
/// shifted down by the number of lines cleared below them.
///
/// Validates: Requirements 6.1, 6.2
/// </summary>
public class LineClearProperties
{
    // ─── Board builder ─────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a board by locking random tetromino pieces at random positions.
    /// This produces realistic board states (only valid piece shapes).
    /// </summary>
    private static Board BuildBoardFromPieces(Random rng)
    {
        var board = new Board();
        var types = Enum.GetValues<TetrominoType>();

        // Lock between 5 and 20 random pieces
        int pieceCount = rng.Next(5, 21);

        for (int i = 0; i < pieceCount; i++)
        {
            var type = types[rng.Next(types.Length)];
            var piece = Tetromino.Create(type);

            // Apply a random rotation
            int rotCount = rng.Next(4);
            for (int r = 0; r < rotCount; r++)
                piece = piece.RotateClockwise();

            // Try a random position
            int row = rng.Next(0, Board.TotalRows);
            int col = rng.Next(0, Board.Columns);
            piece.Row = row;
            piece.Col = col;

            // Only lock if all cells are in bounds
            bool inBounds = piece.GetCells().All(c => board.IsInBounds(c.Row, c.Col));
            if (inBounds)
                board.LockPiece(piece);
        }

        return board;
    }

    // ─── Property 5 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Property 5: Line clearing preserves non-cleared cells.
    ///
    /// For any board state with one or more complete lines, after clearing,
    /// all cells that were in incomplete lines must be present in the resulting board,
    /// shifted down by the number of lines cleared below them.
    ///
    /// Validates: Requirements 6.1, 6.2
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LineClear_PreservesNonClearedCells()
    {
        // Use an int seed to drive the random board generation
        var seedArb = Arb.From(Gen.Choose(0, int.MaxValue));

        return Prop.ForAll(seedArb, seed =>
        {
            var rng = new Random(seed);
            var board = BuildBoardFromPieces(rng);

            var completeLines = board.FindCompleteLines();

            // If no complete lines, the property is vacuously true
            if (completeLines.Count == 0)
                return true;

            // Snapshot the incomplete rows before clearing
            var incompleteRowsBefore = new Dictionary<(int row, int col), TetrominoType>();
            var completeLineSet = new HashSet<int>(completeLines);

            for (int row = 0; row < Board.TotalRows; row++)
            {
                if (completeLineSet.Contains(row)) continue;
                for (int col = 0; col < Board.Columns; col++)
                {
                    var cell = board.GetCell(row, col);
                    if (cell.HasValue)
                        incompleteRowsBefore[(row, col)] = cell.Value;
                }
            }

            // Clear the complete lines
            board.ClearLines(completeLines);

            // Verify: each cell from an incomplete row must appear in the board
            // shifted down by the number of cleared lines that were strictly below it.
            foreach (var ((origRow, origCol), cellType) in incompleteRowsBefore)
            {
                // Count how many cleared lines were strictly below this row
                int clearedBelow = completeLines.Count(cl => cl > origRow);
                int newRow = origRow + clearedBelow;

                if (newRow >= Board.TotalRows)
                    return false; // shifted out of bounds — shouldn't happen

                var actualCell = board.GetCell(newRow, origCol);
                if (!actualCell.HasValue || actualCell.Value != cellType)
                    return false;
            }

            return true;
        });
    }

    /// <summary>
    /// Additional property: after clearing N complete lines, the top N rows are empty.
    /// This verifies that cleared lines are replaced by empty rows at the top.
    ///
    /// Validates: Requirements 6.1, 6.2
    /// </summary>
    [Property(MaxTest = 300)]
    public Property LineClear_TopRowsAreEmpty_AfterClear()
    {
        var seedArb = Arb.From(Gen.Choose(0, int.MaxValue));

        return Prop.ForAll(seedArb, seed =>
        {
            var rng = new Random(seed);
            var board = BuildBoardFromPieces(rng);

            var completeLines = board.FindCompleteLines();
            if (completeLines.Count == 0)
                return true;

            board.ClearLines(completeLines);

            // The top N rows (0 to N-1) should be empty after clearing N lines
            int n = completeLines.Count;
            for (int row = 0; row < n; row++)
                for (int col = 0; col < Board.Columns; col++)
                    if (board.IsOccupied(row, col))
                        return false;

            return true;
        });
    }
}
