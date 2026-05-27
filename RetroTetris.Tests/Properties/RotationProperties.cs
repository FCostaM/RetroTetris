using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 2: SRS rotation preserves board integrity.
///
/// For any piece and board state, after a successful rotation (with or without wall-kick),
/// all cells of the rotated piece must be within the board bounds and not overlap occupied cells.
///
/// Validates: Requirements 4.5, 4.6, 4.7
/// </summary>
public class RotationProperties
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

    // ─── Property 2a ──────────────────────────────────────────────────────────

    /// <summary>
    /// Property 2a: Successful rotation places piece within bounds and on empty cells.
    ///
    /// For any seed (used to build a random board and pick a random piece position/type/rotation),
    /// when SRSRotationSystem.TryRotate returns a non-null result, ALL cells of the returned piece must:
    ///   1. Be within board bounds (board.IsInBounds(row, col))
    ///   2. Not overlap any occupied cell (!board.IsOccupied(row, col))
    ///
    /// Validates: Requirements 4.5, 4.6, 4.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property SRSRotation_SuccessfulResult_AllCellsInBoundsAndEmpty()
    {
        var seedArb = Arb.From(Gen.Choose(0, int.MaxValue));

        return Prop.ForAll(seedArb, seed =>
        {
            var rng = new Random(seed);
            var board = BuildBoardFromPieces(rng);

            // Pick a random tetromino type
            var types = Enum.GetValues<TetrominoType>();
            var type = types[rng.Next(types.Length)];
            var piece = Tetromino.Create(type);

            // Apply 0–3 random clockwise rotations to get a random starting rotation
            int rotCount = rng.Next(4);
            for (int r = 0; r < rotCount; r++)
                piece = piece.RotateClockwise();

            // Set a random position
            piece.Row = rng.Next(2, Board.TotalRows - 4);
            piece.Col = rng.Next(0, Board.Columns - 2);

            // Try both clockwise and counter-clockwise rotations
            foreach (bool clockwise in new[] { true, false })
            {
                var result = SRSRotationSystem.TryRotate(piece, board, clockwise);
                if (result == null)
                    continue;

                // All cells must be in bounds
                foreach (var (r, c) in result.GetCells())
                {
                    if (!board.IsInBounds(r, c))
                        return false;
                }

                // All cells must be on empty (unoccupied) cells
                foreach (var (r, c) in result.GetCells())
                {
                    if (board.IsOccupied(r, c))
                        return false;
                }
            }

            return true;
        });
    }

    // ─── Property 2b ──────────────────────────────────────────────────────────

    /// <summary>
    /// Property 2b: TryRotate never returns a piece that overlaps occupied cells.
    ///
    /// If TryRotate returns non-null, then board.CanPlace(result, result.Row, result.Col) must be true.
    ///
    /// Validates: Requirements 4.5, 4.6, 4.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property SRSRotation_NonNullResult_CanAlwaysBePlace()
    {
        var seedArb = Arb.From(Gen.Choose(0, int.MaxValue));

        return Prop.ForAll(seedArb, seed =>
        {
            var rng = new Random(seed);
            var board = BuildBoardFromPieces(rng);

            var types = Enum.GetValues<TetrominoType>();
            var type = types[rng.Next(types.Length)];
            var piece = Tetromino.Create(type);

            int rotCount = rng.Next(4);
            for (int r = 0; r < rotCount; r++)
                piece = piece.RotateClockwise();

            piece.Row = rng.Next(2, Board.TotalRows - 4);
            piece.Col = rng.Next(0, Board.Columns - 2);

            foreach (bool clockwise in new[] { true, false })
            {
                var result = SRSRotationSystem.TryRotate(piece, board, clockwise);
                if (result == null)
                    continue;

                // CanPlace must return true for the returned piece at its position
                if (!board.CanPlace(result, result.Row, result.Col))
                    return false;
            }

            return true;
        });
    }

    // ─── Property 2c ──────────────────────────────────────────────────────────

    /// <summary>
    /// Property 2c: Rotation result is consistent with CanPlace.
    ///
    /// For any piece and board, if TryRotate returns non-null,
    /// then CanPlace on the returned piece at its position must return true.
    /// This is the combined invariant: in-bounds AND not overlapping occupied cells.
    ///
    /// Validates: Requirements 4.5, 4.6, 4.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property SRSRotation_Result_ConsistentWithCanPlace()
    {
        var seedArb = Arb.From(Gen.Choose(0, int.MaxValue));

        return Prop.ForAll(seedArb, seed =>
        {
            var rng = new Random(seed);
            var board = BuildBoardFromPieces(rng);

            var types = Enum.GetValues<TetrominoType>();
            var type = types[rng.Next(types.Length)];
            var piece = Tetromino.Create(type);

            int rotCount = rng.Next(4);
            for (int r = 0; r < rotCount; r++)
                piece = piece.RotateClockwise();

            piece.Row = rng.Next(2, Board.TotalRows - 4);
            piece.Col = rng.Next(0, Board.Columns - 2);

            foreach (bool clockwise in new[] { true, false })
            {
                var result = SRSRotationSystem.TryRotate(piece, board, clockwise);
                if (result == null)
                    continue;

                // The combined invariant: CanPlace must be true
                // (which implies both in-bounds and not overlapping)
                if (!board.CanPlace(result, result.Row, result.Col))
                    return false;

                // Additionally verify each cell individually for clarity
                foreach (var (r, c) in result.GetCells())
                {
                    if (!board.IsInBounds(r, c))
                        return false;
                    if (board.IsOccupied(r, c))
                        return false;
                }
            }

            return true;
        });
    }
}
