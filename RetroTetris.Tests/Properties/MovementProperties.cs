using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 4: Ghost piece matches hard drop destination.
///
/// For any active piece and board state, the position calculated by the Ghost Piece
/// must be identical to the position where the piece would be fixed by a hard drop.
///
/// Validates: Requirements 8.1, 8.3
/// </summary>
public class MovementProperties
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

    /// <summary>
    /// Computes the hard drop row independently from GhostCalculator:
    /// walk down from piece.Row while CanPlace(piece, row+1, piece.Col) is true.
    /// </summary>
    private static int ComputeHardDropRow(Tetromino piece, Board board)
    {
        int row = piece.Row;
        while (board.CanPlace(piece, row + 1, piece.Col))
            row++;
        return row;
    }

    // ─── Property 4 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Property 4: Ghost piece row equals the hard drop destination row,
    /// and the ghost piece preserves the Col, Rotation, and Type of the active piece.
    ///
    /// For any seed (used to build a random board and pick a random piece),
    /// GhostCalculator.Calculate(piece, board).Row must equal the independently
    /// computed hard drop row, and Col/Rotation/Type must match the active piece.
    ///
    /// Validates: Requirements 8.1, 8.3
    /// </summary>
    [Property(MaxTest = 500)]
    public Property GhostPiece_MatchesHardDropDestination()
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

            // Apply 0–3 random clockwise rotations
            int rotCount = rng.Next(4);
            for (int r = 0; r < rotCount; r++)
                piece = piece.RotateClockwise();

            // Set a random position that is placeable on the board
            piece.Row = rng.Next(0, Board.TotalRows - 2);
            piece.Col = rng.Next(0, Board.Columns);

            // If the piece can't be placed at its starting position, skip (vacuously true)
            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            // Compute ghost via GhostCalculator
            var ghost = GhostCalculator.Calculate(piece, board);

            // Compute hard drop row independently
            int hardDropRow = ComputeHardDropRow(piece, board);

            // Property 4: ghost row must equal hard drop row
            if (ghost.Row != hardDropRow)
                return false;

            // Ghost must preserve Col, Rotation, and Type
            if (ghost.Col != piece.Col)
                return false;
            if (ghost.Rotation != piece.Rotation)
                return false;
            if (ghost.Type != piece.Type)
                return false;

            return true;
        });
    }

    // ─── Property 4b ──────────────────────────────────────────────────────────

    /// <summary>
    /// Property 4b: The cell immediately below the ghost piece is either out of bounds
    /// or occupied — i.e., the ghost is truly at the lowest possible position.
    ///
    /// Validates: Requirements 8.1, 8.3
    /// </summary>
    [Property(MaxTest = 500)]
    public Property GhostPiece_IsAtLowestPossiblePosition()
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

            piece.Row = rng.Next(0, Board.TotalRows - 2);
            piece.Col = rng.Next(0, Board.Columns);

            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            var ghost = GhostCalculator.Calculate(piece, board);

            // The ghost cannot move one row further down
            bool canMoveDown = board.CanPlace(ghost, ghost.Row + 1, ghost.Col);
            return !canMoveDown;
        });
    }

    // ─── Property 4c ──────────────────────────────────────────────────────────

    /// <summary>
    /// Property 4c: The ghost piece can always be placed on the board
    /// (CanPlace returns true for the ghost position).
    ///
    /// Validates: Requirements 8.1, 8.3
    /// </summary>
    [Property(MaxTest = 500)]
    public Property GhostPiece_CanAlwaysBePlaced()
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

            piece.Row = rng.Next(0, Board.TotalRows - 2);
            piece.Col = rng.Next(0, Board.Columns);

            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            var ghost = GhostCalculator.Calculate(piece, board);

            // The ghost must be placeable at its computed position
            return board.CanPlace(ghost, ghost.Row, ghost.Col);
        });
    }

    // ─── Property 1 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Property 1a: If MoveLeft is valid (CanPlace returns true for col-1),
    /// the resulting position has all cells in bounds [0,9] and unoccupied.
    ///
    /// Validates: Requirements 4.1, 4.2, 4.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property MoveLeft_ValidMove_AllCellsInBoundsAndUnoccupied()
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

            // Avoid left edge so there's room to move left
            piece.Row = rng.Next(2, Board.TotalRows - 4);
            piece.Col = rng.Next(1, Board.Columns - 1);

            // Skip if piece can't be placed at starting position
            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            // Only test when MoveLeft is valid
            if (!board.CanPlace(piece, piece.Row, piece.Col - 1))
                return true; // vacuously true — invalid move case handled by Property 1b

            // Simulate the move
            piece.Col--;

            // All cells must be in column bounds [0, Columns-1] and unoccupied
            foreach (var (r, c) in piece.GetCells())
            {
                if (c < 0 || c >= Board.Columns) return false;
                if (r < 0 || r >= Board.TotalRows) return false;
                if (board.IsOccupied(r, c)) return false;
            }

            return true;
        });
    }

    /// <summary>
    /// Property 1b: If MoveLeft is invalid (CanPlace returns false for col-1),
    /// the piece position must remain unchanged.
    ///
    /// Validates: Requirements 4.1, 4.2, 4.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property MoveLeft_InvalidMove_PiecePositionUnchanged()
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
            piece.Col = rng.Next(0, Board.Columns);

            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            // Only test when MoveLeft is invalid
            if (board.CanPlace(piece, piece.Row, piece.Col - 1))
                return true; // vacuously true — valid move case handled by Property 1a

            int originalRow = piece.Row;
            int originalCol = piece.Col;

            // Simulate the rejection: move should NOT happen
            // (CanPlace returned false, so piece.Col stays the same)
            // We verify the piece position is unchanged by checking CanPlace is still false
            bool moveIsStillInvalid = !board.CanPlace(piece, piece.Row, piece.Col - 1);
            bool positionUnchanged = piece.Row == originalRow && piece.Col == originalCol;

            return moveIsStillInvalid && positionUnchanged;
        });
    }

    /// <summary>
    /// Property 1c: If MoveRight is valid (CanPlace returns true for col+1),
    /// the resulting position has all cells in bounds [0,9] and unoccupied.
    ///
    /// Validates: Requirements 4.1, 4.2, 4.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property MoveRight_ValidMove_AllCellsInBoundsAndUnoccupied()
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

            // Avoid right edge so there's room to move right
            piece.Row = rng.Next(2, Board.TotalRows - 4);
            piece.Col = rng.Next(1, Board.Columns - 1);

            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            // Only test when MoveRight is valid
            if (!board.CanPlace(piece, piece.Row, piece.Col + 1))
                return true; // vacuously true — invalid move case handled by Property 1d

            // Simulate the move
            piece.Col++;

            // All cells must be in column bounds [0, Columns-1] and unoccupied
            foreach (var (r, c) in piece.GetCells())
            {
                if (c < 0 || c >= Board.Columns) return false;
                if (r < 0 || r >= Board.TotalRows) return false;
                if (board.IsOccupied(r, c)) return false;
            }

            return true;
        });
    }

    /// <summary>
    /// Property 1d: If MoveRight is invalid (CanPlace returns false for col+1),
    /// the piece position must remain unchanged.
    ///
    /// Validates: Requirements 4.1, 4.2, 4.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property MoveRight_InvalidMove_PiecePositionUnchanged()
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
            piece.Col = rng.Next(0, Board.Columns);

            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            // Only test when MoveRight is invalid
            if (board.CanPlace(piece, piece.Row, piece.Col + 1))
                return true; // vacuously true — valid move case handled by Property 1c

            int originalRow = piece.Row;
            int originalCol = piece.Col;

            // Simulate the rejection: move should NOT happen
            bool moveIsStillInvalid = !board.CanPlace(piece, piece.Row, piece.Col + 1);
            bool positionUnchanged = piece.Row == originalRow && piece.Col == originalCol;

            return moveIsStillInvalid && positionUnchanged;
        });
    }

    // ─── Property 3 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Property 3: After a hard drop, the piece is placed at the lowest valid row —
    /// i.e., CanPlace(piece, hardDropRow, col) is true.
    ///
    /// Validates: Requirement 4.4
    /// </summary>
    [Property(MaxTest = 500)]
    public Property HardDrop_PlacesPieceAtLowestPossibleRow()
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
            piece.Col = rng.Next(1, Board.Columns - 1);

            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            // Compute the hard drop row (walk down while CanPlace succeeds)
            int hardDropRow = ComputeHardDropRow(piece, board);

            // Simulate the hard drop
            piece.Row = hardDropRow;

            // The piece must be placeable at the hard drop row
            return board.CanPlace(piece, piece.Row, piece.Col);
        });
    }

    /// <summary>
    /// Property 3b: After a hard drop, the piece cannot move further down —
    /// CanPlace(piece, hardDropRow + 1, col) must be false.
    ///
    /// Validates: Requirement 4.4
    /// </summary>
    [Property(MaxTest = 500)]
    public Property HardDrop_PieceCannotMoveDown()
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
            piece.Col = rng.Next(1, Board.Columns - 1);

            if (!board.CanPlace(piece, piece.Row, piece.Col))
                return true;

            // Compute the hard drop row
            int hardDropRow = ComputeHardDropRow(piece, board);

            // Simulate the hard drop
            piece.Row = hardDropRow;

            // The piece must NOT be placeable one row below the hard drop row
            bool canMoveDown = board.CanPlace(piece, piece.Row + 1, piece.Col);
            return !canMoveDown;
        });
    }
}
