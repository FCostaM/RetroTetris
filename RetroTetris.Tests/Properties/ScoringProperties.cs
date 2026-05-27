using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 6: Line scoring is calculated correctly.
///
/// For any level and number of lines cleared simultaneously (1–4), the score
/// increment must be exactly multiplier[n] × level, where multiplier = [100, 300, 500, 800].
///
/// Validates: Requirements 6.4, 6.5, 6.6, 6.7
/// </summary>
public class ScoringProperties
{
    private static readonly int[] Multipliers = { 0, 100, 300, 500, 800 };

    /// <summary>
    /// Property 6a: Score increment matches multiplier × level.
    ///
    /// For any linesCleared in [1, 4] and any level in [1, 15],
    /// a fresh ScoreSystem after AddLinesClear must have Score == multipliers[linesCleared] * level.
    ///
    /// Validates: Requirements 6.4, 6.5, 6.6, 6.7
    /// </summary>
    [Property(MaxTest = 500)]
    public Property ScoreSystem_SingleClear_MatchesMultiplierTimesLevel()
    {
        var gen = from lines in Gen.Choose(1, 4)
                  from level in Gen.Choose(1, 15)
                  select (lines, level);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (linesCleared, level) = tuple;
            var score = new ScoreSystem();
            score.AddLinesClear(linesCleared, level);
            int expected = Multipliers[linesCleared] * level;
            return score.Score == expected;
        });
    }

    /// <summary>
    /// Property 6b: Score accumulates correctly over multiple clears.
    ///
    /// For any sequence of (linesCleared, level) pairs, the total score must equal
    /// the sum of multipliers[n] × level for each pair.
    ///
    /// Validates: Requirements 6.4, 6.5, 6.6, 6.7
    /// </summary>
    [Property(MaxTest = 300)]
    public Property ScoreSystem_MultipleClear_AccumulatesCorrectly()
    {
        // Generate a list of (linesCleared, level) pairs
        var pairGen = from lines in Gen.Choose(1, 4)
                      from level in Gen.Choose(1, 15)
                      select Tuple.Create(lines, level);
        var listGen = Gen.ListOf(pairGen);
        var arb = Arb.From(listGen);

        return Prop.ForAll(arb, pairs =>
        {
            var score = new ScoreSystem();
            int expected = 0;

            foreach (var pair in pairs)
            {
                score.AddLinesClear(pair.Item1, pair.Item2);
                expected += Multipliers[pair.Item1] * pair.Item2;
            }

            return score.Score == expected;
        });
    }

    /// <summary>
    /// Property 6c: Reset brings score back to zero.
    ///
    /// For any sequence of clears, after Reset(), Score == 0.
    ///
    /// Validates: Requirements 6.4, 6.5, 6.6, 6.7
    /// </summary>
    [Property(MaxTest = 300)]
    public Property ScoreSystem_AfterReset_ScoreIsZero()
    {
        var gen = from lines in Gen.Choose(1, 4)
                  from level in Gen.Choose(1, 15)
                  from count in Gen.Choose(1, 10)
                  select (lines, level, count);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (linesCleared, level, count) = tuple;
            var score = new ScoreSystem();

            for (int i = 0; i < count; i++)
                score.AddLinesClear(linesCleared, level);

            score.Reset();
            return score.Score == 0;
        });
    }

    /// <summary>
    /// Property 6d: Zero or invalid linesCleared does not change score.
    ///
    /// For linesCleared = 0 or linesCleared = 5, AddLinesClear must not change the score.
    ///
    /// Validates: Requirements 6.4, 6.5, 6.6, 6.7
    /// </summary>
    [Property(MaxTest = 300)]
    public Property ScoreSystem_InvalidLinesCleared_DoesNotChangeScore()
    {
        // Generate invalid linesCleared values: 0 or 5
        var invalidLinesGen = Gen.Elements(0, 5);
        var gen = from lines in invalidLinesGen
                  from level in Gen.Choose(1, 15)
                  select (lines, level);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (linesCleared, level) = tuple;
            var score = new ScoreSystem();
            int before = score.Score;
            score.AddLinesClear(linesCleared, level);
            return score.Score == before;
        });
    }
}
