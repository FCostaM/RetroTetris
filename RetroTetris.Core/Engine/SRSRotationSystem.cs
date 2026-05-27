namespace RetroTetris.Core.Engine;

/// <summary>
/// Implements the Super Rotation System (SRS) wall-kick tables from the Tetris Guideline.
/// Reference: https://tetris.wiki/Super_Rotation_System
///
/// Coordinate convention: dr = row delta (positive = down), dc = column delta (positive = right).
/// The Tetris Wiki uses (x, y) where x = column offset and y = row offset with positive y = up.
/// Conversion: dc = x, dr = -y (negate because our rows increase downward).
/// </summary>
public static class SRSRotationSystem
{
    // Key: (from, to) rotation state transition
    // Value: list of (dr, dc) offsets to test in order
    private static readonly IReadOnlyDictionary<(RotationState from, RotationState to), IReadOnlyList<(int dr, int dc)>>
        _jlstzKicks;

    private static readonly IReadOnlyDictionary<(RotationState from, RotationState to), IReadOnlyList<(int dr, int dc)>>
        _iKicks;

    private static readonly IReadOnlyList<(int dr, int dc)> _noKick =
        new (int, int)[] { (0, 0) };

    static SRSRotationSystem()
    {
        // J, L, S, T, Z shared wall-kick table
        // Wiki (x, y) → our (dr, dc): dc = x, dr = -y
        _jlstzKicks = new Dictionary<(RotationState, RotationState), IReadOnlyList<(int dr, int dc)>>
        {
            // 0 → R  (Spawn → Right, clockwise)
            // Wiki: (0,0), (-1,0), (-1,+1), (0,-2), (-1,-2)
            // dr = -y, dc = x
            [(RotationState.Spawn, RotationState.Right)] = new (int, int)[]
            {
                (0, 0), (0, -1), (-1, -1), (2, 0), (2, -1)
            },

            // R → 0  (Right → Spawn, counter-clockwise)
            // Wiki: (0,0), (+1,0), (+1,-1), (0,+2), (+1,+2)
            [(RotationState.Right, RotationState.Spawn)] = new (int, int)[]
            {
                (0, 0), (0, 1), (1, 1), (-2, 0), (-2, 1)
            },

            // R → 2  (Right → Two, clockwise)
            // Wiki: (0,0), (+1,0), (+1,-1), (0,+2), (+1,+2)
            [(RotationState.Right, RotationState.Two)] = new (int, int)[]
            {
                (0, 0), (0, 1), (1, 1), (-2, 0), (-2, 1)
            },

            // 2 → R  (Two → Right, counter-clockwise)
            // Wiki: (0,0), (-1,0), (-1,+1), (0,-2), (-1,-2)
            [(RotationState.Two, RotationState.Right)] = new (int, int)[]
            {
                (0, 0), (0, -1), (-1, -1), (2, 0), (2, -1)
            },

            // 2 → L  (Two → Left, clockwise)
            // Wiki: (0,0), (+1,0), (+1,+1), (0,-2), (+1,-2)
            [(RotationState.Two, RotationState.Left)] = new (int, int)[]
            {
                (0, 0), (0, 1), (-1, 1), (2, 0), (2, 1)
            },

            // L → 2  (Left → Two, counter-clockwise)
            // Wiki: (0,0), (-1,0), (-1,-1), (0,+2), (-1,+2)
            [(RotationState.Left, RotationState.Two)] = new (int, int)[]
            {
                (0, 0), (0, -1), (1, -1), (-2, 0), (-2, -1)
            },

            // L → 0  (Left → Spawn, clockwise)
            // Wiki: (0,0), (-1,0), (-1,-1), (0,+2), (-1,+2)
            [(RotationState.Left, RotationState.Spawn)] = new (int, int)[]
            {
                (0, 0), (0, -1), (1, -1), (-2, 0), (-2, -1)
            },

            // 0 → L  (Spawn → Left, counter-clockwise)
            // Wiki: (0,0), (+1,0), (+1,+1), (0,-2), (+1,-2)
            [(RotationState.Spawn, RotationState.Left)] = new (int, int)[]
            {
                (0, 0), (0, 1), (-1, 1), (2, 0), (2, 1)
            },
        };

        // I piece wall-kick table
        // Wiki (x, y) → our (dr, dc): dc = x, dr = -y
        _iKicks = new Dictionary<(RotationState, RotationState), IReadOnlyList<(int dr, int dc)>>
        {
            // 0 → R  (Spawn → Right, clockwise)
            // Wiki: (0,0), (-2,0), (+1,0), (-2,-1), (+1,+2)
            [(RotationState.Spawn, RotationState.Right)] = new (int, int)[]
            {
                (0, 0), (0, -2), (0, 1), (1, -2), (-2, 1)
            },

            // R → 0  (Right → Spawn, counter-clockwise)
            // Wiki: (0,0), (+2,0), (-1,0), (+2,+1), (-1,-2)
            [(RotationState.Right, RotationState.Spawn)] = new (int, int)[]
            {
                (0, 0), (0, 2), (0, -1), (-1, 2), (2, -1)
            },

            // R → 2  (Right → Two, clockwise)
            // Wiki: (0,0), (-1,0), (+2,0), (-1,+2), (+2,-1)
            [(RotationState.Right, RotationState.Two)] = new (int, int)[]
            {
                (0, 0), (0, -1), (0, 2), (-2, -1), (1, 2)
            },

            // 2 → R  (Two → Right, counter-clockwise)
            // Wiki: (0,0), (+1,0), (-2,0), (+1,-2), (-2,+1)
            [(RotationState.Two, RotationState.Right)] = new (int, int)[]
            {
                (0, 0), (0, 1), (0, -2), (2, 1), (-1, -2)
            },

            // 2 → L  (Two → Left, clockwise)
            // Wiki: (0,0), (+2,0), (-1,0), (+2,+1), (-1,-2)
            [(RotationState.Two, RotationState.Left)] = new (int, int)[]
            {
                (0, 0), (0, 2), (0, -1), (-1, 2), (2, -1)
            },

            // L → 2  (Left → Two, counter-clockwise)
            // Wiki: (0,0), (-2,0), (+1,0), (-2,-1), (+1,+2)
            [(RotationState.Left, RotationState.Two)] = new (int, int)[]
            {
                (0, 0), (0, -2), (0, 1), (1, -2), (-2, 1)
            },

            // L → 0  (Left → Spawn, clockwise)
            // Wiki: (0,0), (+1,0), (-2,0), (+1,-2), (-2,+1)
            [(RotationState.Left, RotationState.Spawn)] = new (int, int)[]
            {
                (0, 0), (0, 1), (0, -2), (2, 1), (-1, -2)
            },

            // 0 → L  (Spawn → Left, counter-clockwise)
            // Wiki: (0,0), (-1,0), (+2,0), (-1,+2), (+2,-1)
            [(RotationState.Spawn, RotationState.Left)] = new (int, int)[]
            {
                (0, 0), (0, -1), (0, 2), (-2, -1), (1, 2)
            },
        };
    }

    /// <summary>
    /// Returns the list of (dr, dc) offsets to test for the given piece type and rotation transition,
    /// in the order they should be tried. The first offset that results in a valid placement wins.
    /// </summary>
    /// <param name="type">The tetromino type.</param>
    /// <param name="from">The current rotation state.</param>
    /// <param name="to">The target rotation state after rotation.</param>
    /// <returns>An ordered list of (dr, dc) offsets to test.</returns>
    public static IReadOnlyList<(int dr, int dc)> GetKickOffsets(
        TetrominoType type,
        RotationState from,
        RotationState to)
    {
        // O piece: no wall-kick, only the identity offset
        if (type == TetrominoType.O)
            return _noKick;

        var key = (from, to);

        if (type == TetrominoType.I)
        {
            if (_iKicks.TryGetValue(key, out var iOffsets))
                return iOffsets;
        }
        else
        {
            // J, L, S, T, Z share the same table
            if (_jlstzKicks.TryGetValue(key, out var offsets))
                return offsets;
        }

        // Fallback: identity offset only (should not happen for valid transitions)
        return _noKick;
    }

    /// <summary>
    /// Attempts to rotate the piece on the board using SRS wall-kick logic.
    /// Tests each kick offset in order and returns the first valid rotated piece,
    /// or null if no offset produces a valid placement.
    /// </summary>
    /// <param name="piece">The current piece to rotate.</param>
    /// <param name="board">The board to check placement against.</param>
    /// <param name="clockwise">True for clockwise rotation, false for counter-clockwise.</param>
    /// <returns>A new rotated <see cref="Tetromino"/> at the first valid offset, or null if rotation fails.</returns>
    public static Tetromino? TryRotate(Tetromino piece, Board board, bool clockwise)
    {
        // Get the rotated piece (new immutable instance)
        var rotated = clockwise
            ? piece.RotateClockwise()
            : piece.RotateCounterClockwise();

        // Get the kick offsets for this transition
        var offsets = GetKickOffsets(piece.Type, piece.Rotation, rotated.Rotation);

        // Try each offset in order
        foreach (var (dr, dc) in offsets)
        {
            int testRow = piece.Row + dr;
            int testCol = piece.Col + dc;

            if (board.CanPlace(rotated, testRow, testCol))
            {
                rotated.Row = testRow;
                rotated.Col = testCol;
                return rotated;
            }
        }

        // No valid placement found
        return null;
    }
}
