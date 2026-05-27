using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;
using RetroTetris.Core.States;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 9: Hold is blocked after use until the piece is locked.
///
/// For any sequence of actions, if hold was used for the current piece,
/// using hold again before the piece is locked must not change the hold state
/// or the active piece.
///
/// Validates: Requirement 9.4
/// </summary>
public class HoldProperties
{
    /// <summary>
    /// Property 9a: After using hold once, a second hold call does not change
    /// the active piece type or the hold piece type.
    ///
    /// Validates: Requirement 9.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Hold_BlockedAfterFirstUse_ActiveAndHoldPieceUnchanged()
    {
        var arb = Arb.From(Gen.Constant(0)); // run 100 times with fresh engine each time
        return Prop.ForAll(arb, _ =>
        {
            var engine = new GameEngine();
            engine.StartNewGame();

            // Precondition: game is in playing state with an active piece
            if (engine.CurrentState is not PlayingState || engine.ActivePiece is null)
                return true; // skip if not in expected state

            // First Hold() — should succeed (hold not yet used)
            engine.Hold();

            // After first hold: active piece and hold piece should be set
            if (engine.ActivePiece is null || engine.HoldPiece is null)
                return true; // skip if hold didn't work (e.g., game over on spawn)

            var activePieceTypeAfterFirstHold = engine.ActivePiece.Type;
            var holdPieceTypeAfterFirstHold = engine.HoldPiece.Type;

            // Second Hold() — should be blocked (_holdUsed = true)
            engine.Hold();

            // Verify: neither active piece type nor hold piece type changed
            var activePieceTypeAfterSecondHold = engine.ActivePiece?.Type;
            var holdPieceTypeAfterSecondHold = engine.HoldPiece?.Type;

            return activePieceTypeAfterSecondHold == activePieceTypeAfterFirstHold
                && holdPieceTypeAfterSecondHold == holdPieceTypeAfterFirstHold;
        });
    }

    /// <summary>
    /// Property 9b: Hold is re-enabled after the piece is locked (via HardDrop).
    /// After HardDrop(), a second Hold() call should be effective.
    ///
    /// Validates: Requirement 9.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Hold_ReenabledAfterPieceLocked()
    {
        var arb = Arb.From(Gen.Constant(0));
        return Prop.ForAll(arb, _ =>
        {
            var engine = new GameEngine();
            engine.StartNewGame();

            if (engine.CurrentState is not PlayingState || engine.ActivePiece is null)
                return true;

            // First Hold() — stores the first piece
            engine.Hold();

            if (engine.CurrentState is not PlayingState || engine.ActivePiece is null)
                return true; // game over on spawn, skip

            var holdPieceTypeAfterFirstHold = engine.HoldPiece?.Type;

            // HardDrop() — locks the current piece, resets _holdUsed
            engine.HardDrop();

            if (engine.CurrentState is not PlayingState || engine.ActivePiece is null)
                return true; // game over, skip

            var activePieceBeforeSecondHold = engine.ActivePiece.Type;
            var holdPieceBeforeSecondHold = engine.HoldPiece?.Type;

            // Second Hold() — should now be allowed (hold re-enabled after lock)
            engine.Hold();

            if (engine.CurrentState is not PlayingState)
                return true; // game over, skip

            var activePieceAfterSecondHold = engine.ActivePiece?.Type;
            var holdPieceAfterSecondHold = engine.HoldPiece?.Type;

            // After the second hold, either the active piece or hold piece must have changed
            // (indicating the hold was effective)
            bool holdWasEffective =
                activePieceAfterSecondHold != activePieceBeforeSecondHold
                || holdPieceAfterSecondHold != holdPieceBeforeSecondHold;

            return holdWasEffective;
        });
    }

    /// <summary>
    /// Property 9c: Hold can be used exactly once per piece.
    /// For N pieces (N in [1, 5]), each Hold() followed by HardDrop() results
    /// in the hold piece being set after each cycle.
    ///
    /// Validates: Requirement 9.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Hold_UsableExactlyOncePerPiece()
    {
        var gen = Gen.Choose(1, 5);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, n =>
        {
            var engine = new GameEngine();
            engine.StartNewGame();

            for (int i = 0; i < n; i++)
            {
                if (engine.CurrentState is not PlayingState || engine.ActivePiece is null)
                    return true; // game over or unexpected state, skip

                // Use Hold() once for this piece
                engine.Hold();

                // After hold, hold piece must be set
                if (engine.HoldPiece is null)
                    return false; // hold should have stored a piece

                if (engine.CurrentState is not PlayingState || engine.ActivePiece is null)
                    return true; // game over on spawn, skip

                // Attempt a second Hold() — must be blocked
                var holdPieceTypeBefore = engine.HoldPiece.Type;
                var activePieceTypeBefore = engine.ActivePiece.Type;

                engine.Hold(); // should be no-op

                if (engine.HoldPiece?.Type != holdPieceTypeBefore
                    || engine.ActivePiece?.Type != activePieceTypeBefore)
                    return false; // second hold should have been blocked

                // HardDrop() to lock the piece and re-enable hold for next piece
                engine.HardDrop();
            }

            return true;
        });
    }
}
