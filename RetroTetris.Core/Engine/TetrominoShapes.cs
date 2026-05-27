namespace RetroTetris.Core.Engine;

/// <summary>
/// Defines the relative (row, col) cell coordinates for each tetromino type
/// in each of the 4 rotation states, following the standard Tetris Guideline (SRS).
///
/// Index: Shapes[TetrominoType][(int)RotationState] → list of (row, col) relative offsets
/// </summary>
public static class TetrominoShapes
{
    public static readonly IReadOnlyDictionary<TetrominoType, IReadOnlyList<(int, int)>[]> Shapes;

    static TetrominoShapes()
    {
        Shapes = new Dictionary<TetrominoType, IReadOnlyList<(int, int)>[]>
        {
            // I piece — 4×1 bounding box, spawns in row 1
            [TetrominoType.I] = new IReadOnlyList<(int, int)>[]
            {
                // Spawn (0)
                new (int, int)[] { (1, 0), (1, 1), (1, 2), (1, 3) },
                // Right (1)
                new (int, int)[] { (0, 2), (1, 2), (2, 2), (3, 2) },
                // Two (2)
                new (int, int)[] { (2, 0), (2, 1), (2, 2), (2, 3) },
                // Left (3)
                new (int, int)[] { (0, 1), (1, 1), (2, 1), (3, 1) },
            },

            // O piece — 2×2 square, all rotations identical
            [TetrominoType.O] = new IReadOnlyList<(int, int)>[]
            {
                // Spawn (0)
                new (int, int)[] { (0, 0), (0, 1), (1, 0), (1, 1) },
                // Right (1)
                new (int, int)[] { (0, 0), (0, 1), (1, 0), (1, 1) },
                // Two (2)
                new (int, int)[] { (0, 0), (0, 1), (1, 0), (1, 1) },
                // Left (3)
                new (int, int)[] { (0, 0), (0, 1), (1, 0), (1, 1) },
            },

            // T piece — 3×3 bounding box
            [TetrominoType.T] = new IReadOnlyList<(int, int)>[]
            {
                // Spawn (0)
                new (int, int)[] { (0, 1), (1, 0), (1, 1), (1, 2) },
                // Right (1)
                new (int, int)[] { (0, 1), (1, 1), (1, 2), (2, 1) },
                // Two (2)
                new (int, int)[] { (1, 0), (1, 1), (1, 2), (2, 1) },
                // Left (3)
                new (int, int)[] { (0, 1), (1, 0), (1, 1), (2, 1) },
            },

            // S piece — 3×3 bounding box
            [TetrominoType.S] = new IReadOnlyList<(int, int)>[]
            {
                // Spawn (0)
                new (int, int)[] { (0, 1), (0, 2), (1, 0), (1, 1) },
                // Right (1)
                new (int, int)[] { (0, 1), (1, 1), (1, 2), (2, 2) },
                // Two (2)
                new (int, int)[] { (1, 1), (1, 2), (2, 0), (2, 1) },
                // Left (3)
                new (int, int)[] { (0, 0), (1, 0), (1, 1), (2, 1) },
            },

            // Z piece — 3×3 bounding box
            [TetrominoType.Z] = new IReadOnlyList<(int, int)>[]
            {
                // Spawn (0)
                new (int, int)[] { (0, 0), (0, 1), (1, 1), (1, 2) },
                // Right (1)
                new (int, int)[] { (0, 2), (1, 1), (1, 2), (2, 1) },
                // Two (2)
                new (int, int)[] { (1, 0), (1, 1), (2, 1), (2, 2) },
                // Left (3)
                new (int, int)[] { (0, 1), (1, 0), (1, 1), (2, 0) },
            },

            // J piece — 3×3 bounding box
            [TetrominoType.J] = new IReadOnlyList<(int, int)>[]
            {
                // Spawn (0)
                new (int, int)[] { (0, 0), (1, 0), (1, 1), (1, 2) },
                // Right (1)
                new (int, int)[] { (0, 1), (0, 2), (1, 1), (2, 1) },
                // Two (2)
                new (int, int)[] { (1, 0), (1, 1), (1, 2), (2, 2) },
                // Left (3)
                new (int, int)[] { (0, 1), (1, 1), (2, 0), (2, 1) },
            },

            // L piece — 3×3 bounding box
            [TetrominoType.L] = new IReadOnlyList<(int, int)>[]
            {
                // Spawn (0)
                new (int, int)[] { (0, 2), (1, 0), (1, 1), (1, 2) },
                // Right (1)
                new (int, int)[] { (0, 1), (1, 1), (2, 1), (2, 2) },
                // Two (2)
                new (int, int)[] { (1, 0), (1, 1), (1, 2), (2, 0) },
                // Left (3)
                new (int, int)[] { (0, 0), (0, 1), (1, 1), (2, 1) },
            },
        };
    }
}
