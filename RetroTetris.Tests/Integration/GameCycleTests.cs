using RetroTetris.Core.Engine;
using RetroTetris.Core.States;

namespace RetroTetris.Tests.Integration;

/// <summary>
/// Integration tests for the full game cycle.
/// Validates Requirements 5.1–5.5, 6.1–6.8, 7.1–7.5.
/// </summary>
public class GameCycleTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills all 10 columns of the given row on the board by locking I-pieces.
    ///
    /// I-piece in Spawn rotation has relative offsets (1,0),(1,1),(1,2),(1,3).
    /// So to fill board row R, set piece.Row = R-1 (absolute row = R-1+1 = R).
    /// Three I-pieces cover cols 0-3, 4-7, 6-9 (overlap is fine — LockPiece overwrites).
    ///
    /// IMPORTANT: row must be >= 1 (since piece.Row = row-1 must be >= 0).
    /// </summary>
    private static void FillRow(Board board, int row)
    {
        // I-piece at col 0: fills (row, 0), (row, 1), (row, 2), (row, 3)
        var i1 = Tetromino.Create(TetrominoType.I);
        i1.Row = row - 1;
        i1.Col = 0;
        board.LockPiece(i1);

        // I-piece at col 4: fills (row, 4), (row, 5), (row, 6), (row, 7)
        var i2 = Tetromino.Create(TetrominoType.I);
        i2.Row = row - 1;
        i2.Col = 4;
        board.LockPiece(i2);

        // I-piece at col 6: fills (row, 6), (row, 7), (row, 8), (row, 9)
        // Overlaps cols 6-7 with i2, but that's fine — covers cols 8-9
        var i3 = Tetromino.Create(TetrominoType.I);
        i3.Row = row - 1;
        i3.Col = 6;
        board.LockPiece(i3);
    }

    /// <summary>
    /// Fills rows [startRow..endRow] (inclusive) completely.
    /// </summary>
    private static void FillRows(Board board, int startRow, int endRow)
    {
        for (int row = startRow; row <= endRow; row++)
            FillRow(board, row);
    }

    /// <summary>
    /// Fills 9 columns of the given row (cols 1-9, leaving col 0 empty).
    /// This makes the row NOT complete (so it won't be cleared by FindCompleteLines),
    /// but still blocks piece movement since most piece cells land in cols 1-9.
    /// Used to set up game over scenarios without triggering line clears.
    /// </summary>
    private static void FillRowPartial(Board board, int row)
    {
        // I-piece at col 1: fills (row, 1), (row, 2), (row, 3), (row, 4)
        var i1 = Tetromino.Create(TetrominoType.I);
        i1.Row = row - 1;
        i1.Col = 1;
        board.LockPiece(i1);

        // I-piece at col 5: fills (row, 5), (row, 6), (row, 7), (row, 8)
        var i2 = Tetromino.Create(TetrominoType.I);
        i2.Row = row - 1;
        i2.Col = 5;
        board.LockPiece(i2);

        // I-piece at col 6: fills (row, 6), (row, 7), (row, 8), (row, 9)
        var i3 = Tetromino.Create(TetrominoType.I);
        i3.Row = row - 1;
        i3.Col = 6;
        board.LockPiece(i3);
        // Now row has cols 1-9 filled, col 0 empty → NOT a complete line
    }

    /// <summary>
    /// Fills rows [startRow..endRow] (inclusive) with 9 cells each (cols 1-9).
    /// None of these rows will be complete, so no line clears occur.
    /// </summary>
    private static void FillRowsPartial(Board board, int startRow, int endRow)
    {
        for (int row = startRow; row <= endRow; row++)
            FillRowPartial(board, row);
    }

    // ─── Test 1: StartNewGame initializes state correctly ─────────────────────

    [Fact]
    public void StartNewGame_InitializesStateCorrectly()
    {
        var engine = new GameEngine();

        var pieceSpawnedCount = 0;
        engine.PieceSpawned += _ => pieceSpawnedCount++;

        engine.StartNewGame();

        Assert.IsType<PlayingState>(engine.CurrentState);
        Assert.NotNull(engine.ActivePiece);
        Assert.Equal(0, engine.Score);
        Assert.Equal(1, engine.Level);
        Assert.Equal(0, engine.TotalLinesCleared);
        Assert.True(pieceSpawnedCount >= 1, "PieceSpawned should fire at least once during StartNewGame");
    }

    // ─── Test 2: PieceLocked fires on HardDrop ────────────────────────────────

    [Fact]
    public void HardDrop_FiresPieceLockedExactlyOnce()
    {
        var engine = new GameEngine();
        var pieceLockedCount = 0;
        engine.PieceLocked += () => pieceLockedCount++;

        engine.StartNewGame();
        engine.HardDrop();

        Assert.Equal(1, pieceLockedCount);
    }

    // ─── Test 3: PieceSpawned fires after lock ────────────────────────────────

    [Fact]
    public void HardDrop_FiresPieceSpawnedForNextPiece()
    {
        var engine = new GameEngine();
        var pieceSpawnedCount = 0;
        engine.PieceSpawned += _ => pieceSpawnedCount++;

        engine.StartNewGame();   // fires once for initial spawn
        var countAfterStart = pieceSpawnedCount;

        engine.HardDrop();       // fires again for next piece

        Assert.True(pieceSpawnedCount >= 2,
            $"PieceSpawned should fire at least twice total (once on start, once after lock). Got {pieceSpawnedCount}");
        Assert.True(pieceSpawnedCount > countAfterStart,
            "PieceSpawned should fire again after HardDrop locks the piece");
    }

    // ─── Test 4: Correct event order — PieceLocked before PieceSpawned ────────

    [Fact]
    public void HardDrop_PieceLockedFiresBeforePieceSpawned()
    {
        var engine = new GameEngine();
        var eventOrder = new List<string>();

        engine.PieceLocked += () => eventOrder.Add("PieceLocked");
        engine.PieceSpawned += _ => eventOrder.Add("PieceSpawned");

        engine.StartNewGame();
        // After StartNewGame, eventOrder has ["PieceSpawned"] for the initial spawn
        var initialSpawnCount = eventOrder.Count(e => e == "PieceSpawned");

        engine.HardDrop();

        // After HardDrop: PieceLocked fires, then PieceSpawned fires for next piece
        var lockedIndex = eventOrder.IndexOf("PieceLocked");
        // Find the PieceSpawned that comes AFTER PieceLocked
        var spawnedAfterLock = eventOrder
            .Select((e, i) => (e, i))
            .FirstOrDefault(x => x.e == "PieceSpawned" && x.i > lockedIndex);

        Assert.True(lockedIndex >= 0, "PieceLocked should have fired");
        Assert.True(spawnedAfterLock.i > lockedIndex,
            "PieceSpawned (for next piece) should fire after PieceLocked");
    }

    // ─── Test 5: LinesCleared event fires with correct parameters ─────────────

    [Fact]
    public void LinesCleared_FiresWithCorrectCountAndLevel_WhenOneLineCleared()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Strategy: fill row 21 (bottom row) completely.
        // The ghost was calculated on an empty board. For any piece type, the ghost
        // is at the lowest valid row. After HardDrop, the piece locks at the ghost row.
        // Row 21 is pre-filled and complete regardless of where the piece lands
        // (the piece can't land below row 21). After locking, FindCompleteLines
        // finds row 21 → exactly 1 line cleared.
        FillRow(engine.Board, 21);

        // Verify row 21 is complete before the test
        var completeLinesBefore = engine.Board.FindCompleteLines();
        Assert.Contains(21, completeLinesBefore);

        int linesClearedCount = 0;
        int linesClearedArg = 0;
        int levelArg = 0;
        engine.LinesCleared += (count, level) =>
        {
            linesClearedCount++;
            linesClearedArg = count;
            levelArg = level;
        };

        engine.HardDrop();

        Assert.Equal(1, linesClearedCount);
        Assert.Equal(1, linesClearedArg);
        Assert.Equal(1, levelArg);
    }

    // ─── Test 6: ScoreChanged fires with correct score after line clear ────────

    [Fact]
    public void ScoreChanged_FiresWithCorrectScore_AfterOneLineClear()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Fill row 21 only — exactly 1 line will be cleared after HardDrop.
        // Score = 100 × level(1) = 100.
        FillRow(engine.Board, 21);

        int scoreChangedValue = -1;
        engine.ScoreChanged += score => scoreChangedValue = score;

        engine.HardDrop();

        Assert.True(scoreChangedValue >= 0, "ScoreChanged should have fired");
        Assert.Equal(100, scoreChangedValue);
    }

    // ─── Test 7: LevelChanged fires when 10 lines are cleared ─────────────────

    [Fact]
    public void LevelChanged_FiresWithLevel2_AfterTenLinesCleared()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        int levelChangedValue = -1;
        engine.LevelChanged += level => levelChangedValue = level;

        // Clear 10 lines one at a time. Each iteration:
        // 1. Fill row 21 completely
        // 2. HardDrop — piece locks at ghost row, row 21 is cleared
        // 3. Repeat
        // After 10 clears, level advances to 2.
        for (int i = 0; i < 10; i++)
        {
            if (engine.CurrentState is GameOverState)
                break;

            FillRow(engine.Board, 21);
            engine.HardDrop();
        }

        Assert.Equal(2, levelChangedValue);
        Assert.Equal(2, engine.Level);
        Assert.Equal(10, engine.TotalLinesCleared);
    }

    // ─── Test 8: GameOver occurs when spawn position is blocked ───────────────

    [Fact]
    public void GameOverOccurred_FiresAndStateIsGameOver_WhenSpawnBlocked()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        bool gameOverFired = false;
        engine.GameOverOccurred += () => gameOverFired = true;

        // Strategy: fill rows 2-21 with 9 cells each (cols 1-9, col 0 empty).
        // These rows are NOT complete, so no line clears occur after the piece locks.
        // The active piece at Row=0 cannot auto-drop to row 1 because row 2 is
        // filled at cols 1-9 (piece cells land in that range).
        // After lock delay expires, the piece locks at row 0 with cells at row 1.
        // FindCompleteLines finds NO complete rows → no lines cleared.
        // SpawnNextPiece tries to place the next piece at Row=0, Col=3.
        // CanPlace checks cells at row 1 — occupied by the locked piece → GameOver.
        FillRowsPartial(engine.Board, 2, 21);

        // Use Update() to drive the piece naturally (avoids stale ghost issue).
        // Level 1 drop interval = 1000ms. One update > 1000ms triggers auto-drop attempt.
        // Piece can't move down (row 2 filled), so lock delay starts immediately.
        engine.Update(TimeSpan.FromMilliseconds(1100));
        // Lock delay = 500ms. Another update > 500ms expires it → piece locks.
        engine.Update(TimeSpan.FromMilliseconds(600));

        Assert.True(gameOverFired, "GameOverOccurred should have fired");
        Assert.IsType<GameOverState>(engine.CurrentState);
    }

    // ─── Test 8b: HighScore is updated on GameOver ────────────────────────────

    [Fact]
    public void HighScore_UpdatedOnGameOver()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Clear 1 line to get some score
        FillRow(engine.Board, 21);
        engine.HardDrop(); // clears row 21, scores 100 points

        var scoreBeforeGameOver = engine.Score;
        Assert.True(scoreBeforeGameOver > 0, "Should have scored points");

        // Trigger game over: fill rows 2-21 with partial rows (no line clears).
        // Piece locks at row 0, cells at row 1 block next spawn.
        FillRowsPartial(engine.Board, 2, 21);
        engine.Update(TimeSpan.FromMilliseconds(1100));
        engine.Update(TimeSpan.FromMilliseconds(600));

        Assert.IsType<GameOverState>(engine.CurrentState);
        Assert.Equal(scoreBeforeGameOver, engine.HighScore);
    }

    // ─── Test 9: Pause and Resume preserve game state ─────────────────────────

    [Fact]
    public void PauseAndResume_PreserveActivePiece()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        var pieceBeforePause = engine.ActivePiece;
        Assert.NotNull(pieceBeforePause);

        engine.Pause();
        Assert.IsType<PausedState>(engine.CurrentState);

        engine.Resume();
        Assert.IsType<PlayingState>(engine.CurrentState);

        // Active piece should be the same type and position after resume
        Assert.NotNull(engine.ActivePiece);
        Assert.Equal(pieceBeforePause.Type, engine.ActivePiece.Type);
        Assert.Equal(pieceBeforePause.Row, engine.ActivePiece.Row);
        Assert.Equal(pieceBeforePause.Col, engine.ActivePiece.Col);
    }

    // ─── Test 10: Hold mechanics in integration ───────────────────────────────

    [Fact]
    public void Hold_StoresPiece_AndBlocksSecondHold_UntilLock()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        var initialType = engine.ActivePiece!.Type;

        // First hold: stores current piece, spawns next
        engine.Hold();
        Assert.NotNull(engine.HoldPiece);
        Assert.Equal(initialType, engine.HoldPiece!.Type);
        Assert.NotNull(engine.ActivePiece);

        var holdPieceTypeAfterFirstHold = engine.HoldPiece.Type;
        var activeTypeAfterFirstHold = engine.ActivePiece.Type;

        // Second hold immediately: should be blocked (hold already used)
        engine.Hold();
        // HoldPiece should be unchanged — still the initial piece type
        Assert.Equal(holdPieceTypeAfterFirstHold, engine.HoldPiece.Type);
        // Active piece should also be unchanged
        Assert.Equal(activeTypeAfterFirstHold, engine.ActivePiece.Type);

        // HardDrop locks the piece → hold becomes available again
        engine.HardDrop();

        // Now hold should work again — active piece is the newly spawned piece
        Assert.NotNull(engine.ActivePiece);
        var activeTypeBeforeSecondHold = engine.ActivePiece.Type;
        engine.Hold();

        // After Hold: the previously held piece (initialType) becomes active,
        // and the piece that was active before this hold is now in hold.
        Assert.NotNull(engine.HoldPiece);
        Assert.Equal(activeTypeBeforeSecondHold, engine.HoldPiece.Type);
        Assert.NotNull(engine.ActivePiece);
        Assert.Equal(initialType, engine.ActivePiece.Type);
    }

    // ─── Test 11: Multiple HardDrops — sequential piece spawning ─────────────

    [Fact]
    public void MultipleHardDrops_SpawnSequentialPieces_WithoutGameOver()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        int pieceLockedCount = 0;
        engine.PieceLocked += () => pieceLockedCount++;

        // Do 5 hard drops on an empty board — no lines cleared, no game over
        for (int i = 0; i < 5; i++)
        {
            engine.HardDrop();
            if (engine.CurrentState is GameOverState)
                break;
        }

        // On an empty board, pieces stack up. After 5 drops, game should still be playing
        // (pieces land at the bottom and stack, but don't fill rows completely)
        Assert.Equal(0, engine.Score); // No lines cleared
        Assert.Equal(5, pieceLockedCount); // 5 pieces locked
    }

    // ─── Test 12: Full cycle — start → move → lock → line clear → game over ───

    [Fact]
    public void FullCycle_StartToGameOver_AllEventsFireInCorrectOrder()
    {
        var engine = new GameEngine();
        var eventLog = new List<string>();

        engine.PieceLocked += () => eventLog.Add("PieceLocked");
        engine.PieceSpawned += _ => eventLog.Add("PieceSpawned");
        engine.LinesCleared += (count, level) => eventLog.Add($"LinesCleared({count},{level})");
        engine.ScoreChanged += score => eventLog.Add($"ScoreChanged({score})");
        engine.LevelChanged += level => eventLog.Add($"LevelChanged({level})");
        engine.GameOverOccurred += () => eventLog.Add("GameOverOccurred");

        engine.StartNewGame();
        // After StartNewGame: PieceSpawned fired once

        Assert.Contains("PieceSpawned", eventLog);

        // Pre-fill row 21 to set up a line clear
        FillRow(engine.Board, 21);

        // HardDrop: locks piece, clears row 21, spawns next piece
        engine.HardDrop();

        // Verify event ordering: PieceLocked → LinesCleared → ScoreChanged → LevelChanged → PieceSpawned
        var lockedIdx = eventLog.IndexOf("PieceLocked");
        var linesClearedIdx = eventLog.FindIndex(e => e.StartsWith("LinesCleared"));
        var scoreChangedIdx = eventLog.FindIndex(e => e.StartsWith("ScoreChanged"));
        var pieceSpawnedAfterLock = eventLog
            .Select((e, i) => (e, i))
            .LastOrDefault(x => x.e == "PieceSpawned");

        Assert.True(lockedIdx >= 0, "PieceLocked should have fired");
        Assert.True(linesClearedIdx > lockedIdx, "LinesCleared should fire after PieceLocked");
        Assert.True(scoreChangedIdx > lockedIdx, "ScoreChanged should fire after PieceLocked");
        Assert.True(pieceSpawnedAfterLock.i > lockedIdx, "PieceSpawned (next piece) should fire after PieceLocked");

        // Now trigger game over using partial fill (no line clears).
        // Fill rows 2-21 with 9 cells each so the piece locks at row 0 via lock delay.
        // Row 1 gets the piece cells → next spawn blocked → game over.
        FillRowsPartial(engine.Board, 2, 21);
        engine.Update(TimeSpan.FromMilliseconds(1100));
        engine.Update(TimeSpan.FromMilliseconds(600));

        Assert.Contains("GameOverOccurred", eventLog);
        Assert.IsType<GameOverState>(engine.CurrentState);
        Assert.True(engine.Score > 0, "Score should be positive after line clears");
        Assert.Equal(engine.Score, engine.HighScore);
    }

    // ─── Test 13: LinesCleared event parameters — 4 lines (Tetris) ───────────

    [Fact]
    public void LinesCleared_FourLines_ScoreIs800AtLevel1()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Fill 4 rows completely (rows 18-21)
        FillRows(engine.Board, 18, 21);

        int linesClearedCount = 0;
        int scoreAfterClear = 0;
        engine.LinesCleared += (count, level) => linesClearedCount = count;
        engine.ScoreChanged += score => scoreAfterClear = score;

        engine.HardDrop();

        // 4 lines × 800 × level 1 = 800
        Assert.Equal(4, linesClearedCount);
        Assert.Equal(800, scoreAfterClear);
    }

    // ─── Test 14: Update drives auto-drop and lock delay ─────────────────────

    [Fact]
    public void Update_TriggersLockDelay_AndLockspiece()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        int pieceLockedCount = 0;
        engine.PieceLocked += () => pieceLockedCount++;

        // Fill rows 2-21 so the active piece can only land at row 1
        FillRows(engine.Board, 2, 21);

        // Drive auto-drop: piece is at row 0, can move to row 1, then can't move further
        // Update with enough time to auto-drop (level 1 = 1000ms interval)
        engine.Update(TimeSpan.FromMilliseconds(1100)); // triggers auto-drop to row 1

        // Now piece is at row 1 and can't move down (row 2 is filled)
        // Lock delay starts. Update with 600ms to expire the 500ms lock delay
        engine.Update(TimeSpan.FromMilliseconds(600));

        Assert.Equal(1, pieceLockedCount);
    }

    // ─── Test 15: Score accumulates correctly across multiple line clears ─────

    [Fact]
    public void Score_AccumulatesCorrectly_AcrossMultipleLineClears()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Clear 1 line: score = 100
        FillRow(engine.Board, 21);
        engine.HardDrop();
        Assert.Equal(100, engine.Score);

        if (engine.CurrentState is GameOverState) return;

        // Clear 2 lines: score += 300 → total = 400
        FillRows(engine.Board, 20, 21);
        engine.HardDrop();
        Assert.Equal(400, engine.Score);
    }
}
