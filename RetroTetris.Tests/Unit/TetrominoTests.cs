using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Unit;

public class TetrominoTests
{
    // ─── TetrominoShapes: verify all 7 pieces × 4 rotations have exactly 4 cells ───

    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.O)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    public void AllRotations_HaveExactlyFourCells(TetrominoType type)
    {
        var shapes = TetrominoShapes.Shapes[type];
        Assert.Equal(4, shapes.Length);
        foreach (var rotation in shapes)
            Assert.Equal(4, rotation.Count);
    }

    // ─── Verify specific spawn shapes (Tetris Guideline) ───

    [Fact]
    public void I_Spawn_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.I][(int)RotationState.Spawn];
        Assert.Equal(new (int, int)[] { (1, 0), (1, 1), (1, 2), (1, 3) }, cells);
    }

    [Fact]
    public void O_AllRotations_SameCells()
    {
        var shapes = TetrominoShapes.Shapes[TetrominoType.O];
        var expected = new (int, int)[] { (0, 0), (0, 1), (1, 0), (1, 1) };
        foreach (var rotation in shapes)
            Assert.Equal(expected, rotation);
    }

    [Fact]
    public void T_Spawn_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.T][(int)RotationState.Spawn];
        Assert.Equal(new (int, int)[] { (0, 1), (1, 0), (1, 1), (1, 2) }, cells);
    }

    [Fact]
    public void T_Right_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.T][(int)RotationState.Right];
        Assert.Equal(new (int, int)[] { (0, 1), (1, 1), (1, 2), (2, 1) }, cells);
    }

    [Fact]
    public void T_Two_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.T][(int)RotationState.Two];
        Assert.Equal(new (int, int)[] { (1, 0), (1, 1), (1, 2), (2, 1) }, cells);
    }

    [Fact]
    public void T_Left_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.T][(int)RotationState.Left];
        Assert.Equal(new (int, int)[] { (0, 1), (1, 0), (1, 1), (2, 1) }, cells);
    }

    [Fact]
    public void S_Spawn_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.S][(int)RotationState.Spawn];
        Assert.Equal(new (int, int)[] { (0, 1), (0, 2), (1, 0), (1, 1) }, cells);
    }

    [Fact]
    public void Z_Spawn_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.Z][(int)RotationState.Spawn];
        Assert.Equal(new (int, int)[] { (0, 0), (0, 1), (1, 1), (1, 2) }, cells);
    }

    [Fact]
    public void J_Spawn_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.J][(int)RotationState.Spawn];
        Assert.Equal(new (int, int)[] { (0, 0), (1, 0), (1, 1), (1, 2) }, cells);
    }

    [Fact]
    public void L_Spawn_CorrectCells()
    {
        var cells = TetrominoShapes.Shapes[TetrominoType.L][(int)RotationState.Spawn];
        Assert.Equal(new (int, int)[] { (0, 2), (1, 0), (1, 1), (1, 2) }, cells);
    }

    // ─── Tetromino.Create ───

    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.O)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    public void Create_SetsSpawnRotationAndCenteredPosition(TetrominoType type)
    {
        var piece = Tetromino.Create(type);

        Assert.Equal(type, piece.Type);
        Assert.Equal(RotationState.Spawn, piece.Rotation);
        Assert.Equal(0, piece.Row);
        Assert.Equal(3, piece.Col);
    }

    // ─── GetCells: absolute positions ───

    [Fact]
    public void GetCells_ReturnsAbsolutePositions()
    {
        // T piece at Row=5, Col=3 in Spawn rotation
        // Relative: (0,1),(1,0),(1,1),(1,2) → Absolute: (5,4),(6,3),(6,4),(6,5)
        var piece = Tetromino.Create(TetrominoType.T);
        piece.Row = 5;
        piece.Col = 3;

        var cells = piece.GetCells();

        Assert.Equal(4, cells.Count);
        Assert.Contains((5, 4), cells);
        Assert.Contains((6, 3), cells);
        Assert.Contains((6, 4), cells);
        Assert.Contains((6, 5), cells);
    }

    // ─── RotateClockwise ───

    [Fact]
    public void RotateClockwise_AdvancesRotationState()
    {
        var piece = Tetromino.Create(TetrominoType.T);
        Assert.Equal(RotationState.Spawn, piece.Rotation);

        var r1 = piece.RotateClockwise();
        Assert.Equal(RotationState.Right, r1.Rotation);

        var r2 = r1.RotateClockwise();
        Assert.Equal(RotationState.Two, r2.Rotation);

        var r3 = r2.RotateClockwise();
        Assert.Equal(RotationState.Left, r3.Rotation);

        var r4 = r3.RotateClockwise();
        Assert.Equal(RotationState.Spawn, r4.Rotation);
    }

    [Fact]
    public void RotateClockwise_ReturnsNewInstance()
    {
        var piece = Tetromino.Create(TetrominoType.T);
        var rotated = piece.RotateClockwise();

        Assert.NotSame(piece, rotated);
        Assert.Equal(RotationState.Spawn, piece.Rotation); // original unchanged
    }

    // ─── RotateCounterClockwise ───

    [Fact]
    public void RotateCounterClockwise_DecrementsRotationState()
    {
        var piece = Tetromino.Create(TetrominoType.T);
        Assert.Equal(RotationState.Spawn, piece.Rotation);

        var r1 = piece.RotateCounterClockwise();
        Assert.Equal(RotationState.Left, r1.Rotation);

        var r2 = r1.RotateCounterClockwise();
        Assert.Equal(RotationState.Two, r2.Rotation);

        var r3 = r2.RotateCounterClockwise();
        Assert.Equal(RotationState.Right, r3.Rotation);

        var r4 = r3.RotateCounterClockwise();
        Assert.Equal(RotationState.Spawn, r4.Rotation);
    }

    [Fact]
    public void RotateCounterClockwise_ReturnsNewInstance()
    {
        var piece = Tetromino.Create(TetrominoType.T);
        var rotated = piece.RotateCounterClockwise();

        Assert.NotSame(piece, rotated);
        Assert.Equal(RotationState.Spawn, piece.Rotation); // original unchanged
    }

    // ─── Round-trip: CW then CCW returns to original state ───

    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.O)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    public void RotateClockwise_ThenCounterClockwise_ReturnsOriginalState(TetrominoType type)
    {
        var original = Tetromino.Create(type);
        var roundTripped = original.RotateClockwise().RotateCounterClockwise();

        Assert.Equal(original.Rotation, roundTripped.Rotation);
        Assert.Equal(original.Type, roundTripped.Type);
        Assert.Equal(original.Row, roundTripped.Row);
        Assert.Equal(original.Col, roundTripped.Col);
    }

    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.O)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    public void RotateCounterClockwise_ThenClockwise_ReturnsOriginalState(TetrominoType type)
    {
        var original = Tetromino.Create(type);
        var roundTripped = original.RotateCounterClockwise().RotateClockwise();

        Assert.Equal(original.Rotation, roundTripped.Rotation);
        Assert.Equal(original.Type, roundTripped.Type);
        Assert.Equal(original.Row, roundTripped.Row);
        Assert.Equal(original.Col, roundTripped.Col);
    }

    // ─── Four clockwise rotations return to spawn ───

    [Theory]
    [InlineData(TetrominoType.I)]
    [InlineData(TetrominoType.O)]
    [InlineData(TetrominoType.T)]
    [InlineData(TetrominoType.S)]
    [InlineData(TetrominoType.Z)]
    [InlineData(TetrominoType.J)]
    [InlineData(TetrominoType.L)]
    public void FourClockwiseRotations_ReturnToSpawn(TetrominoType type)
    {
        var piece = Tetromino.Create(type);
        var result = piece
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise();

        Assert.Equal(RotationState.Spawn, result.Rotation);
    }

    // ─── TetrominoColors ───

    [Fact]
    public void Colors_AllSevenTypesPresent()
    {
        foreach (TetrominoType type in Enum.GetValues<TetrominoType>())
            Assert.True(TetrominoColors.Primary.ContainsKey(type), $"Missing color for {type}");
    }

    [Theory]
    [InlineData(TetrominoType.I, "#00F0F0")]
    [InlineData(TetrominoType.O, "#F0F000")]
    [InlineData(TetrominoType.T, "#A000F0")]
    [InlineData(TetrominoType.S, "#00F000")]
    [InlineData(TetrominoType.Z, "#F00000")]
    [InlineData(TetrominoType.J, "#0000F0")]
    [InlineData(TetrominoType.L, "#F0A000")]
    public void Colors_CorrectHexValues(TetrominoType type, string expectedHex)
    {
        Assert.Equal(expectedHex, TetrominoColors.Primary[type]);
    }
}
