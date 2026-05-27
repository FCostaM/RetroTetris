using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 8: Drop interval follows the level formula.
///
/// For any level between 1 and 15, the drop interval calculated must be
/// max(100, 1000 - (level - 1) × 90) milliseconds.
///
/// Validates: Requirements 7.1, 7.3, 7.5
/// </summary>
public class SpeedProperties
{
    private static int ExpectedInterval(int level) =>
        Math.Max(100, 1000 - (level - 1) * 90);

    private static LevelSystem CreateAtLevel(int targetLevel)
    {
        var ls = new LevelSystem();
        ls.AddLines((targetLevel - 1) * 10);
        return ls;
    }

    /// <summary>
    /// Property 8a: Drop interval matches formula for any level 1–15.
    ///
    /// For any level in [1, 15], GetDropIntervalMs() must equal
    /// max(100, 1000 - (level - 1) * 90).
    ///
    /// Validates: Requirements 7.1, 7.3, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_DropInterval_MatchesFormula()
    {
        var arb = Arb.From(Gen.Choose(1, 15));

        return Prop.ForAll(arb, level =>
        {
            var ls = CreateAtLevel(level);
            int expected = ExpectedInterval(level);
            return ls.GetDropIntervalMs() == expected;
        });
    }

    /// <summary>
    /// Property 8b: Drop interval is always at least 100ms.
    ///
    /// For any level in [1, 15], GetDropIntervalMs() >= 100.
    ///
    /// Validates: Requirements 7.1, 7.3, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_DropInterval_AtLeast100Ms()
    {
        var arb = Arb.From(Gen.Choose(1, 15));

        return Prop.ForAll(arb, level =>
        {
            var ls = CreateAtLevel(level);
            return ls.GetDropIntervalMs() >= 100;
        });
    }

    /// <summary>
    /// Property 8c: Drop interval is at most 1000ms.
    ///
    /// For any level in [1, 15], GetDropIntervalMs() <= 1000.
    ///
    /// Validates: Requirements 7.1, 7.3, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_DropInterval_AtMost1000Ms()
    {
        var arb = Arb.From(Gen.Choose(1, 15));

        return Prop.ForAll(arb, level =>
        {
            var ls = CreateAtLevel(level);
            return ls.GetDropIntervalMs() <= 1000;
        });
    }

    /// <summary>
    /// Property 8d: Drop interval is monotonically non-increasing with level.
    ///
    /// For any two levels l1 &lt; l2 in [1, 15],
    /// GetDropIntervalMs(l1) >= GetDropIntervalMs(l2).
    ///
    /// Validates: Requirements 7.1, 7.3, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_DropInterval_MonotonicallyNonIncreasing()
    {
        var gen = from l1 in Gen.Choose(1, 14)
                  from l2 in Gen.Choose(l1 + 1, 15)
                  select (l1, l2);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (l1, l2) = tuple;
            var ls1 = CreateAtLevel(l1);
            var ls2 = CreateAtLevel(l2);
            return ls1.GetDropIntervalMs() >= ls2.GetDropIntervalMs();
        });
    }

    /// <summary>
    /// Property 8e: Level 1 has exactly 1000ms interval.
    ///
    /// GetDropIntervalMs() at level 1 returns exactly 1000.
    ///
    /// Validates: Requirements 7.1, 7.3, 7.5
    /// </summary>
    [Property(MaxTest = 1)]
    public Property LevelSystem_Level1_Has1000MsInterval()
    {
        var arb = Arb.From(Gen.Constant(1));

        return Prop.ForAll(arb, _ =>
        {
            var ls = new LevelSystem();
            return ls.GetDropIntervalMs() == 1000;
        });
    }

    /// <summary>
    /// Property 8f: Levels 11–15 all return exactly 100ms (minimum cap).
    ///
    /// For any level in [11, 15], GetDropIntervalMs() returns exactly 100.
    ///
    /// Validates: Requirements 7.1, 7.3, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_HighLevels_ReturnMinimumCap()
    {
        var arb = Arb.From(Gen.Choose(11, 15));

        return Prop.ForAll(arb, level =>
        {
            var ls = CreateAtLevel(level);
            return ls.GetDropIntervalMs() == 100;
        });
    }
}
