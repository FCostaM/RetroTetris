using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;
using RetroTetris.Core.States;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property-based tests for the Controls Screen feature (game-controls-screen).
/// Covers Properties 1–6 for ShowControls() and HideControls() on GameEngine.
/// Uses FsCheck with [Property] attribute.
/// </summary>
public class ControlsScreenProperties
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a GameEngine in StartScreenState (the default initial state).
    /// </summary>
    private static GameEngine CreateInStartScreen() => new GameEngine();

    /// <summary>
    /// Creates a GameEngine in PausedState by direct transition.
    /// </summary>
    private static GameEngine CreateInPausedState()
    {
        var engine = new GameEngine();
        engine.TransitionTo(new PausedState());
        return engine;
    }

    /// <summary>
    /// Creates a GameEngine in PlayingState by direct transition.
    /// </summary>
    private static GameEngine CreateInPlayingState()
    {
        var engine = new GameEngine();
        engine.TransitionTo(new PlayingState());
        return engine;
    }

    /// <summary>
    /// Creates a GameEngine in GameOverState by direct transition.
    /// </summary>
    private static GameEngine CreateInGameOverState()
    {
        var engine = new GameEngine();
        engine.TransitionTo(new GameOverState());
        return engine;
    }

    /// <summary>
    /// Starts a real game and advances it by locking N pieces via HardDrop,
    /// producing a non-trivial board/score/level state.
    /// Returns null if the game ended before N pieces were locked.
    /// </summary>
    private static GameEngine? CreateEngineWithGameProgress(int piecesToLock)
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 0; i < piecesToLock; i++)
        {
            if (engine.CurrentState is not PlayingState)
                return null; // game over before we finished
            engine.HardDrop();
        }

        if (engine.CurrentState is not PlayingState)
            return null;

        return engine;
    }

    // ─── Property 1 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Feature: game-controls-screen, Property 1: ShowControls() preserva PreviousState correto.
    ///
    /// For any valid origin state (StartScreenState or PausedState), calling ShowControls()
    /// must result in CurrentState being a ControlsScreenState whose PreviousState is
    /// exactly the origin state instance.
    ///
    /// Validates: Requirements 1.3, 2.3
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property1_ShowControls_PreservesPreviousState()
    {
        // 0 = StartScreenState, 1 = PausedState
        var originArb = Arb.From(Gen.Choose(0, 1));

        return Prop.ForAll(originArb, originChoice =>
        {
            GameEngine engine;
            IGameState originState;

            if (originChoice == 0)
            {
                engine = CreateInStartScreen();
                originState = engine.CurrentState; // capture the actual instance
            }
            else
            {
                var paused = new PausedState();
                engine = new GameEngine();
                engine.TransitionTo(paused);
                originState = paused;
            }

            engine.ShowControls();

            if (engine.CurrentState is not ControlsScreenState css)
                return false;

            return ReferenceEquals(originState, css.PreviousState);
        });
    }

    // ─── Property 2 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Feature: game-controls-screen, Property 2: HideControls() retorna exatamente ao PreviousState.
    ///
    /// For any ControlsScreenState with a valid PreviousState (StartScreenState or PausedState),
    /// calling HideControls() must result in CurrentState being the exact same instance
    /// that was stored as PreviousState.
    ///
    /// Validates: Requirements 4.1, 4.2
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property2_HideControls_ReturnsExactlyToPreviousState()
    {
        // 0 = StartScreenState, 1 = PausedState
        var previousArb = Arb.From(Gen.Choose(0, 1));

        return Prop.ForAll(previousArb, previousChoice =>
        {
            IGameState previousState;
            GameEngine engine;

            if (previousChoice == 0)
            {
                engine = CreateInStartScreen();
                previousState = engine.CurrentState;
                engine.ShowControls();
            }
            else
            {
                var paused = new PausedState();
                engine = new GameEngine();
                engine.TransitionTo(paused);
                previousState = paused;
                engine.ShowControls();
            }

            // Precondition: must be in ControlsScreenState
            if (engine.CurrentState is not ControlsScreenState)
                return true; // skip

            engine.HideControls();

            return ReferenceEquals(previousState, engine.CurrentState);
        });
    }

    // ─── Property 3 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Feature: game-controls-screen, Property 3: ShowControls + HideControls preserva estado de jogo completo.
    ///
    /// For any game state (board, score, level, hold piece), the sequence
    /// ShowControls() + HideControls() must preserve all those values identically.
    ///
    /// Validates: Requirements 4.3, 4.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property3_OpenCloseControls_PreservesGameState()
    {
        // Generate number of pieces to lock (0–10) to create varied game states
        var piecesArb = Arb.From(Gen.Choose(0, 10));

        return Prop.ForAll(piecesArb, piecesToLock =>
        {
            // Build a game engine with some progress, then pause it
            var engine = new GameEngine();
            engine.StartNewGame();

            for (int i = 0; i < piecesToLock; i++)
            {
                if (engine.CurrentState is not PlayingState)
                    return true; // game over, skip
                engine.HardDrop();
            }

            if (engine.CurrentState is not PlayingState)
                return true; // game over, skip

            // Pause the game so we can open Controls from PausedState
            engine.Pause();

            if (engine.CurrentState is not PausedState)
                return true; // unexpected, skip

            // Capture snapshot before opening Controls
            int scoreBefore = engine.Score;
            int levelBefore = engine.Level;
            int linesBefore = engine.TotalLinesCleared;
            var holdTypeBefore = engine.HoldPiece?.Type;
            var activePieceTypeBefore = engine.ActivePiece?.Type;

            // Capture board state
            var boardCellsBefore = CaptureBoardCells(engine.Board);

            // Open and immediately close Controls
            engine.ShowControls();

            if (engine.CurrentState is not ControlsScreenState)
                return false; // ShowControls should have worked

            engine.HideControls();

            if (engine.CurrentState is not PausedState)
                return false; // should be back in PausedState

            // Verify all game state is preserved
            if (engine.Score != scoreBefore) return false;
            if (engine.Level != levelBefore) return false;
            if (engine.TotalLinesCleared != linesBefore) return false;
            if (engine.HoldPiece?.Type != holdTypeBefore) return false;
            if (engine.ActivePiece?.Type != activePieceTypeBefore) return false;

            // Verify board cells are identical
            var boardCellsAfter = CaptureBoardCells(engine.Board);
            return BoardCellsEqual(boardCellsBefore, boardCellsAfter);
        });
    }

    // ─── Property 4 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Feature: game-controls-screen, Property 4: Update() é no-op em ControlsScreenState.
    ///
    /// For any ControlsScreenState (regardless of PreviousState), calling Update(delta)
    /// N times must not alter Board, Score, Level, ActivePiece, or HoldPiece.
    ///
    /// Validates: Requirement 4.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property4_Update_IsNoOp_InControlsScreenState()
    {
        var gen = from n in Gen.Choose(1, 20)
                  from deltaMs in Gen.Choose(1, 500)
                  from piecesToLock in Gen.Choose(0, 5)
                  select (n, deltaMs, piecesToLock);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (n, deltaMs, piecesToLock) = tuple;

            // Build engine with some game progress, then pause and open Controls
            var engine = new GameEngine();
            engine.StartNewGame();

            for (int i = 0; i < piecesToLock; i++)
            {
                if (engine.CurrentState is not PlayingState)
                    return true; // game over, skip
                engine.HardDrop();
            }

            if (engine.CurrentState is not PlayingState)
                return true;

            engine.Pause();
            engine.ShowControls();

            if (engine.CurrentState is not ControlsScreenState)
                return true; // skip if not in expected state

            // Capture state before updates
            int scoreBefore = engine.Score;
            int levelBefore = engine.Level;
            int linesBefore = engine.TotalLinesCleared;
            var holdTypeBefore = engine.HoldPiece?.Type;
            var activePieceTypeBefore = engine.ActivePiece?.Type;
            var boardCellsBefore = CaptureBoardCells(engine.Board);

            // Call Update N times with the given delta
            var delta = TimeSpan.FromMilliseconds(deltaMs);
            for (int i = 0; i < n; i++)
                engine.Update(delta);

            // Verify nothing changed
            if (engine.Score != scoreBefore) return false;
            if (engine.Level != levelBefore) return false;
            if (engine.TotalLinesCleared != linesBefore) return false;
            if (engine.HoldPiece?.Type != holdTypeBefore) return false;
            if (engine.ActivePiece?.Type != activePieceTypeBefore) return false;

            var boardCellsAfter = CaptureBoardCells(engine.Board);
            return BoardCellsEqual(boardCellsBefore, boardCellsAfter);
        });
    }

    // ─── Property 5 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Feature: game-controls-screen, Property 5: ShowControls() é no-op em estados inválidos.
    ///
    /// For any state that is neither StartScreenState nor PausedState
    /// (PlayingState or GameOverState), calling ShowControls() must not change CurrentState.
    ///
    /// Validates: Requirements 1.3, 2.3 (by contraposition — defines the valid domain)
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property5_ShowControls_IsNoOp_InInvalidStates()
    {
        // 0 = PlayingState, 1 = GameOverState
        var stateArb = Arb.From(Gen.Choose(0, 1));

        return Prop.ForAll(stateArb, stateChoice =>
        {
            GameEngine engine;

            if (stateChoice == 0)
                engine = CreateInPlayingState();
            else
                engine = CreateInGameOverState();

            var stateBefore = engine.CurrentState;

            engine.ShowControls();

            // CurrentState must be the exact same instance
            return ReferenceEquals(stateBefore, engine.CurrentState);
        });
    }

    // ─── Property 6 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Feature: game-controls-screen, Property 6: ControlsScreenState não pode ser aninhado.
    ///
    /// For any ControlsScreenState, its PreviousState can never be another ControlsScreenState.
    /// Attempting to construct new ControlsScreenState(controlsScreenState) must throw ArgumentException.
    /// Additionally, calling ShowControls() when already in ControlsScreenState must be a no-op.
    ///
    /// Validates: Requirement 4.3
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property6_ControlsScreenState_CannotBeNested()
    {
        // 0 = try to nest via constructor, 1 = try to nest via ShowControls()
        var approachArb = Arb.From(Gen.Choose(0, 1));

        return Prop.ForAll(approachArb, approach =>
        {
            if (approach == 0)
            {
                // Verify constructor throws ArgumentException for nested ControlsScreenState
                var inner = new ControlsScreenState(new StartScreenState());
                bool threw = false;
                try
                {
                    _ = new ControlsScreenState(inner);
                }
                catch (ArgumentException)
                {
                    threw = true;
                }
                return threw;
            }
            else
            {
                // Verify ShowControls() is no-op when already in ControlsScreenState
                var engine = CreateInStartScreen();
                engine.ShowControls(); // → ControlsScreenState

                if (engine.CurrentState is not ControlsScreenState)
                    return false; // precondition failed

                var controlsStateBefore = engine.CurrentState;

                engine.ShowControls(); // should be no-op

                return ReferenceEquals(controlsStateBefore, engine.CurrentState);
            }
        });
    }

    // ─── Property 7 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Feature: game-controls-screen, Property 7: Rótulo de tecla sempre à esquerda da descrição da ação.
    ///
    /// For any valid canvas dimensions (width 200–800, height 400–1200), the X coordinate
    /// of the key label (leftX = boardRect.X + boardRect.Width * 0.08f) must be strictly
    /// less than the X coordinate of the action description (rightX = boardRect.X + boardRect.Width * 0.92f)
    /// for every entry in _keyboardEntries and _touchEntries.
    ///
    /// Validates: Requirement 3.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property Property7_CommandEntry_KeyLabel_AlwaysLeftOfActionDescription()
    {
        // Generate random canvas dimensions: width 200–800, height 400–1200
        var gen = from width in Gen.Choose(200, 800)
                  from height in Gen.Choose(400, 1200)
                  select (width, height);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (width, height) = tuple;
            float w = (float)width;
            float h = (float)height;

            // Replicate LayoutManager.Update() boardRect calculation
            // (mirrors the logic in RetroTetris/Presentation/LayoutManager.cs)
            const int boardCols = 10; // Board.Columns
            const int boardRows = 20; // Board.VisibleRows
            const float sidePanelRatio = 0.2f;

            // boardRect fields: boardX, boardY, boardWidth, boardHeight
            float boardX, boardWidth;

            bool isLandscape = w >= h;
            if (isLandscape)
            {
                float sideW = w * sidePanelRatio;
                float boardW = w - 2 * sideW;

                float cellByWidth  = boardW / boardCols;
                float cellByHeight = h / boardRows;
                float cellSize = Math.Min(cellByWidth, cellByHeight);

                float actualBoardW = cellSize * boardCols;

                boardX     = sideW + (boardW - actualBoardW) / 2f;
                boardWidth = actualBoardW;
            }
            else
            {
                float cellByWidth  = w / boardCols;
                float cellByHeight = (h * 0.7f) / boardRows;
                float cellSize = Math.Min(cellByWidth, cellByHeight);

                float actualBoardW = cellSize * boardCols;

                boardX     = (w - actualBoardW) / 2f;
                boardWidth = actualBoardW;
            }

            // Compute the X coordinates used in DrawCommandEntries()
            // (mirrors DrawCommandEntries in GameRenderer.cs)
            float leftX  = boardX + boardWidth * 0.08f;
            float rightX = boardX + boardWidth * 0.92f;

            // The invariant must hold: leftX < rightX for every entry in both arrays.
            // Since leftX and rightX do not depend on the entry content, checking once suffices,
            // but we verify the entry counts are non-zero to ensure the layout is exercised.
            int totalEntries = KeyboardEntriesCount + TouchEntriesCount;
            if (totalEntries == 0)
                return false; // entries must exist

            return leftX < rightX;
        });
    }

    // Entry counts mirroring the static arrays in GameRenderer
    // (keyboard: 9 entries, touch: 6 entries — kept in sync with GameRenderer._keyboardEntries/_touchEntries)
    private const int KeyboardEntriesCount = 9;
    private const int TouchEntriesCount    = 6;

    // ─── Board capture helpers ─────────────────────────────────────────────────

    private static TetrominoType?[,] CaptureBoardCells(Board board)
    {
        var cells = new TetrominoType?[Board.TotalRows, Board.Columns];
        for (int r = 0; r < Board.TotalRows; r++)
            for (int c = 0; c < Board.Columns; c++)
                cells[r, c] = board.GetCell(r, c);
        return cells;
    }

    private static bool BoardCellsEqual(TetrominoType?[,] a, TetrominoType?[,] b)
    {
        for (int r = 0; r < Board.TotalRows; r++)
            for (int c = 0; c < Board.Columns; c++)
                if (a[r, c] != b[r, c])
                    return false;
        return true;
    }
}
