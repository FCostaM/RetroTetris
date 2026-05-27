namespace RetroTetris.Core.Engine;

public enum TetrominoType { I, O, T, S, Z, J, L }

public enum RotationState { Spawn = 0, Right = 1, Two = 2, Left = 3 }

public class Tetromino
{
    public TetrominoType Type { get; }
    public RotationState Rotation { get; private set; }
    public int Row { get; set; }
    public int Col { get; set; }

    private Tetromino(TetrominoType type, RotationState rotation, int row, int col)
    {
        Type = type;
        Rotation = rotation;
        Row = row;
        Col = col;
    }

    /// <summary>
    /// Returns the 4 occupied cells as absolute (row, col) positions on the board.
    /// </summary>
    public IReadOnlyList<(int Row, int Col)> GetCells()
    {
        var relCells = TetrominoShapes.Shapes[Type][(int)Rotation];
        var result = new (int Row, int Col)[relCells.Count];
        for (int i = 0; i < relCells.Count; i++)
        {
            result[i] = (Row + relCells[i].Item1, Col + relCells[i].Item2);
        }
        return result;
    }

    /// <summary>
    /// Returns a new Tetromino instance rotated 90 degrees clockwise.
    /// </summary>
    public Tetromino RotateClockwise()
    {
        var nextRotation = (RotationState)(((int)Rotation + 1) % 4);
        return new Tetromino(Type, nextRotation, Row, Col);
    }

    /// <summary>
    /// Returns a new Tetromino instance rotated 90 degrees counter-clockwise.
    /// </summary>
    public Tetromino RotateCounterClockwise()
    {
        var nextRotation = (RotationState)(((int)Rotation + 3) % 4);
        return new Tetromino(Type, nextRotation, Row, Col);
    }

    /// <summary>
    /// Creates a new Tetromino of the given type at the standard spawn position (Row=0, Col=3).
    /// </summary>
    public static Tetromino Create(TetrominoType type)
    {
        return new Tetromino(type, RotationState.Spawn, 0, 3);
    }
}
