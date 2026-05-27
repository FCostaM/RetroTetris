using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SRSRotationSystem"/>.
/// Covers wall-kick scenarios from the Tetris Wiki (SRS), rotation failure when all
/// offsets are blocked, O-piece no-kick behaviour, and both rotation directions.
/// </summary>
public class SRSRotationSystemTests
{
    // ─── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills a single cell on the board by locking a 1-cell-wide piece at that position.
    /// We use an O-piece (relative cells (0,0),(0,1),(1,0),(1,1)) placed so that
    /// (row, col) maps to the (0,0) relative cell.
    /// </summary>
    private static void FillCell(Board board, int row, int col)
    {
        // Lock an O-piece whose top-left cell lands on (row, col).
        // O-piece relative cells: (0,0),(0,1),(1,0),(1,1)
        // We only care that (row, col) becomes occupied; the other three cells
        // may also be filled — that is acceptable for these tests.
        var o = Tetromino.Create(TetrominoType.O);
        o.Row = row;
        o.Col = col;
        board.LockPiece(o);
    }

    /// <summary>
    /// Fills a specific cell by directly locking a T-piece positioned so that
    /// exactly the desired cell is covered by the T's top-centre cell (relative (0,1)).
    /// This avoids accidentally filling neighbouring cells when precision matters.
    /// </summary>
    private static void FillCellPrecise(Board board, int row, int col)
    {
        // T-piece spawn relative cells: (0,1),(1,0),(1,1),(1,2)
        // Place T so that (0,1) maps to (row, col) → piece.Row = row, piece.Col = col - 1
        // But this also fills (row+1, col-1), (row+1, col), (row+1, col+1).
        // For a truly single-cell fill we lock an I-piece vertically (Right rotation).
        // I-Right relative cells: (0,2),(1,2),(2,2),(3,2)
        // Place I so that (0,2) maps to (row, col) → piece.Row = row, piece.Col = col - 2
        var i = Tetromino.Create(TetrominoType.I);
        // Rotate to Right state (vertical)
        i = i.RotateClockwise(); // Spawn → Right
        i.Row = row;
        i.Col = col - 2; // (0,2) → (row, col)
        board.LockPiece(i);
    }

    // ─── 1. Basic rotation without wall-kick ───────────────────────────────────

    /// <summary>
    /// A T-piece in open space should rotate clockwise using the first offset (0,0)
    /// without needing any wall-kick.
    /// T-Spawn relative: (0,1),(1,0),(1,1),(1,2)
    /// T-Right relative: (0,1),(1,1),(1,2),(2,1)
    /// At Row=10, Col=3 all cells are in-bounds and unoccupied → (0,0) succeeds.
    /// </summary>
    [Fact]
    public void TryRotate_OpenSpace_SucceedsAtZeroOffset()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 3;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Right, result!.Rotation);
        // No kick applied — position unchanged
        Assert.Equal(10, result.Row);
        Assert.Equal(3, result.Col);
    }

    // ─── 2. Wall-kick for J/L/S/T/Z ───────────────────────────────────────────

    /// <summary>
    /// T-piece at Row=10, Col=8 rotating clockwise (Spawn→Right).
    /// JLSTZ Spawn→Right kick table: (0,0),(0,-1),(-1,-1),(2,0),(2,-1)
    ///
    /// T-Right relative cells: (0,1),(1,1),(1,2),(2,1)
    /// At (10,8): absolute (10,9),(11,9),(11,10),(12,9) — col 10 is out of bounds → offset (0,0) fails.
    /// Offset (0,-1): testRow=10, testCol=7 → T-Right at (10,7): (10,8),(11,8),(11,9),(12,8) → valid!
    /// </summary>
    [Fact]
    public void TryRotate_TpieceNearRightWall_UsesKickOffset()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 8;

        // No cells need to be filled — offset (0,0) fails because col 10 is out of bounds.
        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Right, result!.Rotation);
        // Kick offset (0,-1) was applied: testRow=10, testCol=7
        Assert.Equal(10, result.Row);
        Assert.Equal(7, result.Col);
    }

    // ─── 3. Wall-kick for I piece ──────────────────────────────────────────────

    /// <summary>
    /// I-piece at Row=5, Col=8 rotating clockwise (Spawn→Right).
    /// I Spawn→Right kick table: (0,0),(0,-2),(0,1),(1,-2),(-2,1)
    ///
    /// I-Right relative cells: (0,2),(1,2),(2,2),(3,2)
    /// At (5,8): absolute (5,10),(6,10),(7,10),(8,10) → col 10 out of bounds → offset (0,0) fails.
    /// Offset (0,-2): testRow=5, testCol=6 → I-Right at (5,6): (5,8),(6,8),(7,8),(8,8) → valid!
    /// </summary>
    [Fact]
    public void TryRotate_IpieceNearRightWall_UsesIPieceKickOffset()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.I);
        piece.Row = 5;
        piece.Col = 8;

        // No cells need to be filled — offset (0,0) fails because col 10 is out of bounds.
        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Right, result!.Rotation);
        // Kick offset (0,-2) was applied: testRow=5, testCol=6
        Assert.Equal(5, result.Row);
        Assert.Equal(6, result.Col);
    }

    // ─── 4. T-spin scenario ────────────────────────────────────────────────────

    /// <summary>
    /// T-spin scenario: T-piece in Right rotation rotating clockwise to Two,
    /// squeezed into a tight space using a wall-kick.
    ///
    /// JLSTZ Right→Two kick table: (0,0),(0,1),(1,1),(-2,0),(-2,1)
    /// T-Two relative cells: (1,0),(1,1),(1,2),(2,1)
    ///
    /// T piece at Row=15, Col=7 in Right rotation.
    /// T-Two at (15,7): absolute (16,7),(16,8),(16,9),(17,8) — block all to fail (0,0).
    /// Offset (0,1): testCol=8 → T-Two at (15,8): (16,8),(16,9),(16,10),(17,9) — col 10 OOB → fails.
    /// Offset (1,1): testRow=16, testCol=8 → T-Two at (16,8): (17,8),(17,9),(17,10),(18,9) — col 10 OOB → fails.
    /// Offset (-2,0): testRow=13, testCol=7 → T-Two at (13,7): (14,7),(14,8),(14,9),(15,8) → valid!
    /// </summary>
    [Fact]
    public void TryRotate_TspinScenario_UsesWallKickIntoTightSpace()
    {
        var board = new Board();

        // Create T piece already in Right rotation
        var piece = Tetromino.Create(TetrominoType.T);
        piece = piece.RotateClockwise(); // Spawn → Right
        piece.Row = 15;
        piece.Col = 7;

        // Block offset (0,0): T-Two at (15,7) → cells (16,7),(16,8),(16,9),(17,8)
        FillCellPrecise(board, 16, 7); // fills (16,7),(17,7),(18,7),(19,7)
        FillCellPrecise(board, 16, 8); // fills (16,8),(17,8),(18,8),(19,8) — also blocks (17,8)
        FillCellPrecise(board, 16, 9); // fills (16,9),(17,9),(18,9),(19,9)

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Two, result!.Rotation);
        // Kick offset (-2,0) was applied: testRow=13, testCol=7
        Assert.Equal(13, result.Row);
        Assert.Equal(7, result.Col);
    }

    // ─── 5. Rotation fails when all offsets are blocked ────────────────────────

    /// <summary>
    /// When all 5 kick offsets for a T-piece are blocked, TryRotate returns null.
    ///
    /// T piece at Row=10, Col=3, Spawn→Right.
    /// JLSTZ Spawn→Right kick table: (0,0),(0,-1),(-1,-1),(2,0),(2,-1)
    /// T-Right relative cells: (0,1),(1,1),(1,2),(2,1)
    ///
    /// We block each of the 5 candidate positions:
    ///   (0,0)  → T-Right at (10,3): cells (10,4),(11,4),(11,5),(12,4)
    ///   (0,-1) → T-Right at (10,2): cells (10,3),(11,3),(11,4),(12,3)
    ///   (-1,-1)→ T-Right at (9,2):  cells (9,3),(10,3),(10,4),(11,3)
    ///   (2,0)  → T-Right at (12,3): cells (12,4),(13,4),(13,5),(14,4)
    ///   (2,-1) → T-Right at (12,2): cells (12,3),(13,3),(13,4),(14,3)
    /// </summary>
    [Fact]
    public void TryRotate_AllOffsetsBlocked_ReturnsNull()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 3;

        // Block offset (0,0): T-Right at (10,3) → (10,4),(11,4),(11,5),(12,4)
        FillCell(board, 10, 4);

        // Block offset (0,-1): T-Right at (10,2) → (10,3),(11,3),(11,4),(12,3)
        FillCell(board, 10, 3);

        // Block offset (-1,-1): T-Right at (9,2) → (9,3),(10,3),(10,4),(11,3)
        // (9,3) is not yet filled — fill it
        FillCell(board, 9, 3);

        // Block offset (2,0): T-Right at (12,3) → (12,4),(13,4),(13,5),(14,4)
        FillCell(board, 12, 4);

        // Block offset (2,-1): T-Right at (12,2) → (12,3),(13,3),(13,4),(14,3)
        FillCell(board, 12, 3);

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.Null(result);
    }

    // ─── 6. O piece never wall-kicks ──────────────────────────────────────────

    /// <summary>
    /// O-piece only tries the (0,0) offset (no wall-kick table).
    /// When (0,0) is blocked, TryRotate returns null immediately without trying
    /// any additional offsets.
    /// O relative cells (all rotations): (0,0),(0,1),(1,0),(1,1)
    /// </summary>
    [Fact]
    public void TryRotate_OPiece_OnlyTriesZeroOffset_ReturnsNullWhenBlocked()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.O);
        piece.Row = 10;
        piece.Col = 3;

        // Block the (0,0) position: O at (10,3) → cells (10,3),(10,4),(11,3),(11,4)
        FillCell(board, 10, 3);

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.Null(result);
    }

    /// <summary>
    /// O-piece in open space succeeds at (0,0) offset.
    /// </summary>
    [Fact]
    public void TryRotate_OPiece_OpenSpace_Succeeds()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.O);
        piece.Row = 10;
        piece.Col = 3;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        // O piece rotation state advances even though shape is identical
        Assert.Equal(RotationState.Right, result!.Rotation);
        Assert.Equal(10, result.Row);
        Assert.Equal(3, result.Col);
    }

    // ─── 7. Clockwise and counter-clockwise both work ─────────────────────────

    /// <summary>
    /// Clockwise rotation: Spawn → Right.
    /// </summary>
    [Fact]
    public void TryRotate_Clockwise_SpawnToRight()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 3;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Right, result!.Rotation);
    }

    /// <summary>
    /// Counter-clockwise rotation: Spawn → Left.
    /// </summary>
    [Fact]
    public void TryRotate_CounterClockwise_SpawnToLeft()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 3;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: false);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Left, result!.Rotation);
    }

    /// <summary>
    /// Four clockwise rotations return to Spawn state.
    /// </summary>
    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.O)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    public void TryRotate_FourClockwiseRotations_ReturnToSpawn(TetrominoType type)
    {
        var board = new Board();
        var piece = Tetromino.Create(type);
        piece.Row = 10;
        piece.Col = 3;

        var r1 = SRSRotationSystem.TryRotate(piece, board, clockwise: true);
        Assert.NotNull(r1);

        var r2 = SRSRotationSystem.TryRotate(r1!, board, clockwise: true);
        Assert.NotNull(r2);

        var r3 = SRSRotationSystem.TryRotate(r2!, board, clockwise: true);
        Assert.NotNull(r3);

        var r4 = SRSRotationSystem.TryRotate(r3!, board, clockwise: true);
        Assert.NotNull(r4);

        Assert.Equal(RotationState.Spawn, r4!.Rotation);
    }

    /// <summary>
    /// Counter-clockwise rotation near the right wall uses the correct kick table.
    /// T piece at Row=10, Col=7, Spawn→Left (counter-clockwise).
    /// JLSTZ Spawn→Left kick table: (0,0),(0,1),(-1,1),(2,0),(2,1)
    /// T-Left relative cells: (0,1),(1,0),(1,1),(2,1)
    /// At (10,7): absolute (10,8),(11,7),(11,8),(12,8) — all in bounds on empty board → (0,0) succeeds.
    /// </summary>
    [Fact]
    public void TryRotate_CounterClockwise_NearRightWall_Succeeds()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 7;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: false);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Left, result!.Rotation);
    }

    // ─── 8. GetKickOffsets returns correct count ───────────────────────────────

    /// <summary>
    /// JLSTZ pieces have 5 kick offsets per transition.
    /// </summary>
    [Theory]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.Z)]
    public void GetKickOffsets_JLSTZPieces_ReturnFiveOffsets(TetrominoType type)
    {
        var offsets = SRSRotationSystem.GetKickOffsets(type, RotationState.Spawn, RotationState.Right);
        Assert.Equal(5, offsets.Count);
    }

    /// <summary>
    /// I piece has 5 kick offsets per transition (its own table).
    /// </summary>
    [Fact]
    public void GetKickOffsets_IPiece_ReturnsFiveOffsets()
    {
        var offsets = SRSRotationSystem.GetKickOffsets(TetrominoType.I, RotationState.Spawn, RotationState.Right);
        Assert.Equal(5, offsets.Count);
    }

    /// <summary>
    /// O piece has exactly 1 kick offset (identity only, no wall-kick).
    /// </summary>
    [Fact]
    public void GetKickOffsets_OPiece_ReturnsOneOffset()
    {
        var offsets = SRSRotationSystem.GetKickOffsets(TetrominoType.O, RotationState.Spawn, RotationState.Right);
        Assert.Single(offsets);
        Assert.Equal((0, 0), offsets[0]);
    }

    /// <summary>
    /// O piece returns (0,0) for all rotation transitions.
    /// </summary>
    [Theory]
    [InlineData(RotationState.Spawn, RotationState.Right)]
    [InlineData(RotationState.Right, RotationState.Two)]
    [InlineData(RotationState.Two, RotationState.Left)]
    [InlineData(RotationState.Left, RotationState.Spawn)]
    [InlineData(RotationState.Spawn, RotationState.Left)]
    [InlineData(RotationState.Right, RotationState.Spawn)]
    public void GetKickOffsets_OPiece_AlwaysReturnsIdentityOffset(RotationState from, RotationState to)
    {
        var offsets = SRSRotationSystem.GetKickOffsets(TetrominoType.O, from, to);
        Assert.Single(offsets);
        Assert.Equal((0, 0), offsets[0]);
    }

    /// <summary>
    /// All 8 transitions for JLSTZ return exactly 5 offsets.
    /// </summary>
    [Theory]
    [InlineData(RotationState.Spawn, RotationState.Right)]
    [InlineData(RotationState.Right, RotationState.Spawn)]
    [InlineData(RotationState.Right, RotationState.Two)]
    [InlineData(RotationState.Two, RotationState.Right)]
    [InlineData(RotationState.Two, RotationState.Left)]
    [InlineData(RotationState.Left, RotationState.Two)]
    [InlineData(RotationState.Left, RotationState.Spawn)]
    [InlineData(RotationState.Spawn, RotationState.Left)]
    public void GetKickOffsets_AllTransitions_JLSTZReturnFiveOffsets(RotationState from, RotationState to)
    {
        var offsets = SRSRotationSystem.GetKickOffsets(TetrominoType.T, from, to);
        Assert.Equal(5, offsets.Count);
    }

    /// <summary>
    /// All 8 transitions for I piece return exactly 5 offsets.
    /// </summary>
    [Theory]
    [InlineData(RotationState.Spawn, RotationState.Right)]
    [InlineData(RotationState.Right, RotationState.Spawn)]
    [InlineData(RotationState.Right, RotationState.Two)]
    [InlineData(RotationState.Two, RotationState.Right)]
    [InlineData(RotationState.Two, RotationState.Left)]
    [InlineData(RotationState.Left, RotationState.Two)]
    [InlineData(RotationState.Left, RotationState.Spawn)]
    [InlineData(RotationState.Spawn, RotationState.Left)]
    public void GetKickOffsets_AllTransitions_IPieceReturnsFiveOffsets(RotationState from, RotationState to)
    {
        var offsets = SRSRotationSystem.GetKickOffsets(TetrominoType.I, from, to);
        Assert.Equal(5, offsets.Count);
    }

    // ─── Additional: verify first kick offset is always (0,0) for JLSTZ and I ─

    /// <summary>
    /// The first kick offset for any piece type and transition is always (0,0) —
    /// meaning the system first tries the rotation in place before attempting kicks.
    /// </summary>
    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.O)]
    public void GetKickOffsets_FirstOffsetIsAlwaysZeroZero(TetrominoType type)
    {
        var offsets = SRSRotationSystem.GetKickOffsets(type, RotationState.Spawn, RotationState.Right);
        Assert.Equal((0, 0), offsets[0]);
    }

    // ─── Additional: TryRotate returns new immutable instance ─────────────────

    /// <summary>
    /// TryRotate returns a new Tetromino instance; the original piece is not mutated.
    /// </summary>
    [Fact]
    public void TryRotate_DoesNotMutateOriginalPiece()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 10;
        piece.Col = 3;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        Assert.NotSame(piece, result);
        // Original piece unchanged
        Assert.Equal(RotationState.Spawn, piece.Rotation);
        Assert.Equal(10, piece.Row);
        Assert.Equal(3, piece.Col);
    }

    // ─── Additional: I-piece near left wall uses kick ─────────────────────────

    /// <summary>
    /// I-piece at Row=5, Col=0 rotating counter-clockwise (Spawn→Left).
    /// I Spawn→Left kick table: (0,0),(0,-1),(0,2),(-2,-1),(1,2)
    /// I-Left relative cells: (0,1),(1,1),(2,1),(3,1)
    /// At (5,0): absolute (5,1),(6,1),(7,1),(8,1) — all valid on empty board → (0,0) succeeds.
    /// </summary>
    [Fact]
    public void TryRotate_IpieceNearLeftWall_CounterClockwise_Succeeds()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.I);
        piece.Row = 5;
        piece.Col = 0;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: false);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Left, result!.Rotation);
    }

    // ─── Additional: J-piece wall-kick near right wall ────────────────────────

    /// <summary>
    /// J-piece at Row=10, Col=7 rotating clockwise (Spawn→Right).
    /// JLSTZ Spawn→Right kick table: (0,0),(0,-1),(-1,-1),(2,0),(2,-1)
    /// J-Right relative cells: (0,1),(0,2),(1,1),(2,1)
    /// At (10,7): absolute (10,8),(10,9),(11,8),(12,8) — all in bounds → (0,0) succeeds.
    /// </summary>
    [Fact]
    public void TryRotate_JpieceNearRightWall_SucceedsAtZeroOffset()
    {
        var board = new Board();
        var piece = Tetromino.Create(TetrominoType.J);
        piece.Row = 10;
        piece.Col = 7;

        var result = SRSRotationSystem.TryRotate(piece, board, clockwise: true);

        Assert.NotNull(result);
        Assert.Equal(RotationState.Right, result!.Rotation);
        Assert.Equal(10, result.Row);
        Assert.Equal(7, result.Col);
    }
}
