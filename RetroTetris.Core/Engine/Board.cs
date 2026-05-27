namespace RetroTetris.Core.Engine;

/// <summary>
/// Represents the 10×22 game grid (20 visible rows + 2 buffer rows at the top).
/// Rows are indexed 0 (top buffer) to 21 (bottom visible row).
/// Columns are indexed 0 (left) to 9 (right).
/// A null cell is empty; a non-null value is the type of the tetromino occupying it.
/// </summary>
public class Board
{
    public const int Columns = 10;
    public const int VisibleRows = 20;
    public const int BufferRows = 2;
    public const int TotalRows = VisibleRows + BufferRows; // 22

    private readonly TetrominoType?[,] _cells; // [row, col]

    public Board()
    {
        _cells = new TetrominoType?[TotalRows, Columns];
    }

    /// <summary>
    /// Returns the cell value at (row, col), or null if empty.
    /// </summary>
    public TetrominoType? GetCell(int row, int col) => _cells[row, col];

    /// <summary>
    /// Returns true if the cell at (row, col) is occupied by a tetromino.
    /// </summary>
    public bool IsOccupied(int row, int col) => _cells[row, col].HasValue;

    /// <summary>
    /// Returns true if (row, col) is within the board boundaries.
    /// </summary>
    public bool IsInBounds(int row, int col) =>
        row >= 0 && row < TotalRows && col >= 0 && col < Columns;

    /// <summary>
    /// Returns true if the piece can be placed at the given (row, col) offset —
    /// i.e., all cells of the piece at that position are in-bounds and unoccupied.
    /// </summary>
    public bool CanPlace(Tetromino piece, int row, int col)
    {
        // Temporarily adjust piece position to test
        int originalRow = piece.Row;
        int originalCol = piece.Col;
        piece.Row = row;
        piece.Col = col;

        try
        {
            foreach (var (r, c) in piece.GetCells())
            {
                if (!IsInBounds(r, c)) return false;
                if (IsOccupied(r, c)) return false;
            }
            return true;
        }
        finally
        {
            piece.Row = originalRow;
            piece.Col = originalCol;
        }
    }

    /// <summary>
    /// Locks the piece onto the board at its current Row/Col position,
    /// writing the piece's type into each occupied cell.
    /// </summary>
    public void LockPiece(Tetromino piece)
    {
        foreach (var (r, c) in piece.GetCells())
        {
            if (IsInBounds(r, c))
                _cells[r, c] = piece.Type;
        }
    }

    /// <summary>
    /// Returns the row indices of all complete lines (all 10 columns occupied),
    /// sorted in ascending order (top to bottom).
    /// </summary>
    public IReadOnlyList<int> FindCompleteLines()
    {
        var result = new List<int>();
        for (int row = 0; row < TotalRows; row++)
        {
            bool complete = true;
            for (int col = 0; col < Columns; col++)
            {
                if (!IsOccupied(row, col))
                {
                    complete = false;
                    break;
                }
            }
            if (complete) result.Add(row);
        }
        return result;
    }

    /// <summary>
    /// Removes the specified rows from the board and shifts all rows above them down.
    /// The <paramref name="lines"/> list must be sorted in ascending order.
    /// </summary>
    public void ClearLines(IReadOnlyList<int> lines)
    {
        if (lines.Count == 0) return;

        // Use a set for O(1) lookup
        var lineSet = new HashSet<int>(lines);

        // Build new grid: iterate from bottom to top, skipping cleared rows
        // writeRow starts at the bottom and moves up
        int writeRow = TotalRows - 1;

        for (int readRow = TotalRows - 1; readRow >= 0; readRow--)
        {
            if (lineSet.Contains(readRow))
                continue; // skip cleared lines

            // Copy row from readRow to writeRow
            for (int col = 0; col < Columns; col++)
                _cells[writeRow, col] = _cells[readRow, col];

            writeRow--;
        }

        // Fill remaining top rows with empty cells
        while (writeRow >= 0)
        {
            for (int col = 0; col < Columns; col++)
                _cells[writeRow, col] = null;
            writeRow--;
        }
    }

    /// <summary>
    /// Clears all cells to null (empty board).
    /// </summary>
    public void Reset()
    {
        for (int row = 0; row < TotalRows; row++)
            for (int col = 0; col < Columns; col++)
                _cells[row, col] = null;
    }
}
