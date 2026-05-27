namespace RetroTetris.Core.Engine;

/// <summary>
/// Calculates the ghost piece position — the projection of the active piece
/// at the lowest possible row directly below its current position.
/// </summary>
public static class GhostCalculator
{
    /// <summary>
    /// Returns a new Tetromino representing the ghost piece: same Type, Rotation,
    /// and Col as <paramref name="activePiece"/>, but at the lowest valid row.
    /// If the piece cannot move down at all, the ghost is at the same row as the active piece.
    /// </summary>
    public static Tetromino Calculate(Tetromino activePiece, Board board)
    {
        int ghostRow = activePiece.Row;

        // Move down as far as possible
        while (board.CanPlace(activePiece, ghostRow + 1, activePiece.Col))
            ghostRow++;

        // Create a new instance with the same Type and Rotation as the active piece.
        // Tetromino.Create() starts at RotationState.Spawn (0); apply rotations to match.
        var ghost = Tetromino.Create(activePiece.Type);

        int rotationSteps = (int)activePiece.Rotation; // Spawn=0, Right=1, Two=2, Left=3
        for (int i = 0; i < rotationSteps; i++)
            ghost = ghost.RotateClockwise();

        ghost.Row = ghostRow;
        ghost.Col = activePiece.Col;

        return ghost;
    }
}
