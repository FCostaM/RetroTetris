using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Unit;

public class BoardTests
{
    // ─── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills a row using only cells in that row by locking I-pieces in spawn rotation.
    /// I-spawn relative cells: (1,0),(1,1),(1,2),(1,3) → place piece at (row-1, col).
    /// </summary>
    private static void FillRowClean(Board board, int row)
    {
        // I-piece spawn: relative row offset is +1, so piece.Row = row - 1
        var iPiece = Tetromino.Create(TetrominoType.I);

        // cols 0-3
        iPiece.Row = row - 1; iPiece.Col = 0;
        board.LockPiece(iPiece);

        // cols 4-7
        iPiece.Row = row - 1; iPiece.Col = 4;
        board.LockPiece(iPiece);

        // cols 6-9 (overlaps 6,7 — already set, just overwrites with same type)
        iPiece.Row = row - 1; iPiece.Col = 6;
        board.LockPiece(iPiece);
    }

    // ─── IsInBounds ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(21, 9, true)]
    [InlineData(0, 9, true)]
    [InlineData(21, 0, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(22, 0, false)]
    [InlineData(0, 10, false)]
    [InlineData(-1, -1, false)]
    [InlineData(22, 10, false)]
    public void IsInBounds_CorrectForBoundaryValues(int row, int col, bool expected)
    {
        var board = new Board();
        Assert.Equal(expected, board.IsInBounds(row, col));
    }

    // ─── IsOccupied / GetCell ───────────────────────────────────────────────────

    [Fact]
    public void NewBoard_AllCellsEmpty()
    {
        var board = new Board();
        for (int r = 0; r < Board.TotalRows; r++)
            for (int c = 0; c < Board.Columns; c++)
            {
                Assert.False(board.IsOccupied(r, c));
                Assert.Null(board.GetCell(r, c));
            }
    }

    [Fact]
    public void LockPiece_SetsCorrectCells()
    {
        var board = new Board();
        // T-piece at Row=10, Col=3 in Spawn rotation
        // Relative: (0,1),(1,0),(1,1),(1,2) → Absolute: (10,4),(11,3),(11,4),(11,5)
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 3;

        board.LockPiece(piece);

        Assert.Equal(TetrominoType.T, board.GetCell(10, 4));
        Assert.Equal(TetrominoType.T, board.GetCell(11, 3));
        Assert.Equal(TetrominoType.T, board.GetCell(11, 4));
        Assert.Equal(TetrominoType.T, board.GetCell(11, 5));
        // Surrounding cells should still be empty
        Assert.Null(board.GetCell(10, 3));
        Assert.Null(board.GetCell(10, 5));
    }

    // ─── CanPlace ──────────────────────────────────────────────────────────────

    [Fact]
    public void CanPlace_EmptyBoard_CenterPosition_ReturnsTrue()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        Assert.True(board.CanPlace(piece, 10, 3));
    }

    [Fact]
    public void CanPlace_OutOfBoundsLeft_ReturnsFalse()
    {
        var board = new Board();
        // T-piece spawn: relative (0,1),(1,0),(1,1),(1,2)
        // At col=-1: absolute cols 0,-1,0,1 → col -1 is out of bounds
        var piece = Tetromino.Create(TetrominoType.T);
        Assert.False(board.CanPlace(piece, 10, -1));
    }

    [Fact]
    public void CanPlace_OutOfBoundsRight_ReturnsFalse()
    {
        var board = new Board();
        // T-piece spawn: relative (0,1),(1,0),(1,1),(1,2)
        // At col=9: absolute cols 10,9,10,11 → col 10,11 out of bounds
        var piece = Tetromino.Create(TetrominoType.T);
        Assert.False(board.CanPlace(piece, 10, 9));
    }

    [Fact]
    public void CanPlace_OutOfBoundsBottom_ReturnsFalse()
    {
        var board = new Board();
        // T-piece spawn: relative (0,1),(1,0),(1,1),(1,2)
        // At row=21: absolute rows 21,22 → row 22 out of bounds
        var piece = Tetromino.Create(TetrominoType.T);
        Assert.False(board.CanPlace(piece, 21, 3));
    }

    [Fact]
    public void CanPlace_AtRightEdge_ValidPosition_ReturnsTrue()
    {
        var board = new Board();
        // I-piece spawn: relative (1,0),(1,1),(1,2),(1,3)
        // At col=6: absolute cols 6,7,8,9 → all in bounds
        var piece = Tetromino.Create(TetrominoType.I);
        Assert.True(board.CanPlace(piece, 10, 6));
    }

    [Fact]
    public void CanPlace_AtLeftEdge_ValidPosition_ReturnsTrue()
    {
        var board = new Board();
        // I-piece spawn: relative (1,0),(1,1),(1,2),(1,3)
        // At col=0: absolute cols 0,1,2,3 → all in bounds
        var piece = Tetromino.Create(TetrominoType.I);
        Assert.True(board.CanPlace(piece, 10, 0));
    }

    [Fact]
    public void CanPlace_OverOccupiedCell_ReturnsFalse()
    {
        var board = new Board();
        // Lock a T-piece at (10,3)
        var existing = Tetromino.Create(TetrominoType.T);
        existing.Row = 10; existing.Col = 3;
        board.LockPiece(existing);

        // Try to place another T-piece at the same position
        var newPiece = Tetromino.Create(TetrominoType.T);
        Assert.False(board.CanPlace(newPiece, 10, 3));
    }

    [Fact]
    public void CanPlace_AdjacentToOccupiedCell_ReturnsTrue()
    {
        var board = new Board();
        // Lock a T-piece at (10,3)
        var existing = Tetromino.Create(TetrominoType.T);
        existing.Row = 10; existing.Col = 3;
        board.LockPiece(existing);

        // Place another T-piece far away — should succeed
        var newPiece = Tetromino.Create(TetrominoType.T);
        Assert.True(board.CanPlace(newPiece, 5, 0));
    }

    [Fact]
    public void CanPlace_DoesNotMutatePiecePosition()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 5;
        piece.Col = 3;

        board.CanPlace(piece, 10, 7);

        // Original position must be unchanged
        Assert.Equal(5, piece.Row);
        Assert.Equal(3, piece.Col);
    }

    // ─── FindCompleteLines ─────────────────────────────────────────────────────

    [Fact]
    public void FindCompleteLines_EmptyBoard_ReturnsEmpty()
    {
        var board = new Board();
        Assert.Empty(board.FindCompleteLines());
    }

    [Fact]
    public void FindCompleteLines_OneCompleteRow_ReturnsIt()
    {
        var board = new Board();
        FillRowClean(board, 21); // bottom row

        var lines = board.FindCompleteLines();
        Assert.Single(lines);
        Assert.Equal(21, lines[0]);
    }

    [Fact]
    public void FindCompleteLines_TwoCompleteRows_ReturnsBoth()
    {
        var board = new Board();
        FillRowClean(board, 20);
        FillRowClean(board, 21);

        var lines = board.FindCompleteLines();
        Assert.Equal(2, lines.Count);
        Assert.Contains(20, lines);
        Assert.Contains(21, lines);
    }

    [Fact]
    public void FindCompleteLines_ThreeCompleteRows_ReturnsAll()
    {
        var board = new Board();
        FillRowClean(board, 19);
        FillRowClean(board, 20);
        FillRowClean(board, 21);

        var lines = board.FindCompleteLines();
        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public void FindCompleteLines_FourCompleteRows_ReturnsAll()
    {
        var board = new Board();
        FillRowClean(board, 18);
        FillRowClean(board, 19);
        FillRowClean(board, 20);
        FillRowClean(board, 21);

        var lines = board.FindCompleteLines();
        Assert.Equal(4, lines.Count);
    }

    [Fact]
    public void FindCompleteLines_PartialRow_NotIncluded()
    {
        var board = new Board();
        // Fill only 9 of 10 columns in row 21
        var iPiece = Tetromino.Create(TetrominoType.I);
        iPiece.Row = 20; iPiece.Col = 0;
        board.LockPiece(iPiece); // cols 0-3
        iPiece.Row = 20; iPiece.Col = 4;
        board.LockPiece(iPiece); // cols 4-7
        // col 8 and 9 are empty → row 21 is not complete

        Assert.Empty(board.FindCompleteLines());
    }

    // ─── ClearLines ────────────────────────────────────────────────────────────

    [Fact]
    public void ClearLines_EmptyList_BoardUnchanged()
    {
        var board = new Board();
        FillRowClean(board, 21);

        board.ClearLines(Array.Empty<int>());

        // Row 21 should still be full
        for (int col = 0; col < Board.Columns; col++)
            Assert.True(board.IsOccupied(21, col));
    }

    [Fact]
    public void ClearLines_OneRow_ShiftsAboveRowDown()
    {
        var board = new Board();

        // Place a T-piece at row 19 (above the cleared row 21)
        var tPiece = Tetromino.Create(TetrominoType.T);
        tPiece.Row = 18; tPiece.Col = 3;
        board.LockPiece(tPiece);
        // T-piece cells: (18,4),(19,3),(19,4),(19,5)

        // Fill row 21 completely
        FillRowClean(board, 21);

        board.ClearLines(new[] { 21 });

        // The T-piece cells should have shifted down by 1
        Assert.Equal(TetrominoType.T, board.GetCell(19, 4)); // was (18,4)
        Assert.Equal(TetrominoType.T, board.GetCell(20, 3)); // was (19,3)
        Assert.Equal(TetrominoType.T, board.GetCell(20, 4)); // was (19,4)
        Assert.Equal(TetrominoType.T, board.GetCell(20, 5)); // was (19,5)

        // Row 21 should now be empty (was the cleared row, now filled with shifted content)
        // Actually row 21 gets the content of row 20 (which was empty), so it's empty
        Assert.Null(board.GetCell(21, 4));

        // Top row should be empty
        Assert.Null(board.GetCell(0, 0));
    }

    [Fact]
    public void ClearLines_TwoRows_ShiftsAboveDown()
    {
        var board = new Board();

        // Place an I-piece at row 18 (above the cleared rows 20 and 21)
        // I-piece spawn: relative (1,0),(1,1),(1,2),(1,3) → at (17,0): (18,0),(18,1),(18,2),(18,3)
        var iPiece = Tetromino.Create(TetrominoType.I);
        iPiece.Row = 17; iPiece.Col = 0;
        board.LockPiece(iPiece);
        // Cells at row 18: cols 0,1,2,3

        // Fill rows 20 and 21
        FillRowClean(board, 20);
        FillRowClean(board, 21);

        board.ClearLines(new[] { 20, 21 });

        // I-piece cells should shift down by 2
        Assert.Equal(TetrominoType.I, board.GetCell(20, 0));
        Assert.Equal(TetrominoType.I, board.GetCell(20, 1));
        Assert.Equal(TetrominoType.I, board.GetCell(20, 2));
        Assert.Equal(TetrominoType.I, board.GetCell(20, 3));

        // Top 2 rows should be empty
        Assert.Null(board.GetCell(0, 0));
        Assert.Null(board.GetCell(1, 0));
    }

    [Fact]
    public void ClearLines_FourRows_TopRowsBecomesEmpty()
    {
        var board = new Board();

        // Fill rows 18-21
        FillRowClean(board, 18);
        FillRowClean(board, 19);
        FillRowClean(board, 20);
        FillRowClean(board, 21);

        board.ClearLines(new[] { 18, 19, 20, 21 });

        // All rows should now be empty
        for (int row = 0; row < Board.TotalRows; row++)
            for (int col = 0; col < Board.Columns; col++)
                Assert.Null(board.GetCell(row, col));
    }

    [Fact]
    public void ClearLines_NonAdjacentRows_ShiftsCorrectly()
    {
        var board = new Board();

        // Place a marker at row 17 (above both cleared rows)
        // Use O-piece at (16,0): cells (16,0),(16,1),(17,0),(17,1)
        var oPiece = Tetromino.Create(TetrominoType.O);
        oPiece.Row = 16; oPiece.Col = 0;
        board.LockPiece(oPiece);

        // Fill rows 19 and 21 (non-adjacent)
        FillRowClean(board, 19);
        FillRowClean(board, 21);

        board.ClearLines(new[] { 19, 21 });

        // O-piece cells (16,0),(16,1),(17,0),(17,1) should shift down by 2
        Assert.Equal(TetrominoType.O, board.GetCell(18, 0));
        Assert.Equal(TetrominoType.O, board.GetCell(18, 1));
        Assert.Equal(TetrominoType.O, board.GetCell(19, 0));
        Assert.Equal(TetrominoType.O, board.GetCell(19, 1));
    }

    // ─── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsAllCells()
    {
        var board = new Board();
        FillRowClean(board, 21);
        FillRowClean(board, 20);

        board.Reset();

        for (int row = 0; row < Board.TotalRows; row++)
            for (int col = 0; col < Board.Columns; col++)
                Assert.Null(board.GetCell(row, col));
    }

    [Fact]
    public void Reset_AllowsPlacingPiecesAgain()
    {
        var board = new Board();
        FillRowClean(board, 21);
        board.Reset();

        var piece = Tetromino.Create(TetrominoType.T);
        Assert.True(board.CanPlace(piece, 10, 3));
    }
}
