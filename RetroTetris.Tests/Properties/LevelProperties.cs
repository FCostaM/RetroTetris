using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 10: Level increments every 10 lines cleared.
///
/// For any total number of lines cleared L, the level must be
/// min(15, 1 + floor(L / 10)).
///
/// Validates: Requirements 7.2, 7.5
/// </summary>
public class LevelProperties
{
    private static int ExpectedLevel(int totalLines) =>
        Math.Min(15, 1 + totalLines / 10);

    /// <summary>
    /// Property 10a: Level equals min(15, 1 + floor(L / 10)) for any L in [0, 200].
    ///
    /// For any total lines L in [0, 200], after AddLines(L),
    /// Level must equal Math.Min(15, 1 + L / 10).
    ///
    /// Validates: Requirements 7.2, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_Level_MatchesFormula()
    {
        var arb = Arb.From(Gen.Choose(0, 200));

        return Prop.ForAll(arb, lines =>
        {
            var ls = new LevelSystem();
            ls.AddLines(lines);
            int expected = ExpectedLevel(lines);
            return ls.Level == expected;
        });
    }

    /// <summary>
    /// Property 10b: Level is always between 1 and 15.
    ///
    /// For any L in [0, 200], Level is in [1, 15].
    ///
    /// Validates: Requirements 7.2, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_Level_AlwaysBetween1And15()
    {
        var arb = Arb.From(Gen.Choose(0, 200));

        return Prop.ForAll(arb, lines =>
        {
            var ls = new LevelSystem();
            ls.AddLines(lines);
            return ls.Level >= 1 && ls.Level <= 15;
        });
    }

    /// <summary>
    /// Property 10c: Level never decreases when adding more lines.
    ///
    /// For any L1 and L2 where L2 > L1, the level after L2 lines >= level after L1 lines.
    ///
    /// Validates: Requirements 7.2, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_Level_NeverDecreases()
    {
        var gen = from l1 in Gen.Choose(0, 199)
                  from l2 in Gen.Choose(l1 + 1, 200)
                  select (l1, l2);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (l1, l2) = tuple;
            var ls1 = new LevelSystem();
            ls1.AddLines(l1);

            var ls2 = new LevelSystem();
            ls2.AddLines(l2);

            return ls2.Level >= ls1.Level;
        });
    }

    /// <summary>
    /// Property 10d: Adding lines in batches equals adding them all at once.
    ///
    /// For any split (a, b) where a + b = total,
    /// AddLines(a) then AddLines(b) gives the same level as AddLines(a + b) on a fresh system.
    ///
    /// Validates: Requirements 7.2, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_Level_BatchEqualsAllAtOnce()
    {
        var gen = from total in Gen.Choose(0, 200)
                  from a in Gen.Choose(0, total)
                  select (a, total - a, total);
        var arb = Arb.From(gen);

        return Prop.ForAll(arb, tuple =>
        {
            var (a, b, total) = tuple;

            var lsBatch = new LevelSystem();
            lsBatch.AddLines(a);
            lsBatch.AddLines(b);

            var lsAll = new LevelSystem();
            lsAll.AddLines(total);

            return lsBatch.Level == lsAll.Level;
        });
    }

    /// <summary>
    /// Property 10e: Level 15 is reached at exactly 140 lines.
    ///
    /// After AddLines(140), Level == 15.
    ///
    /// Validates: Requirements 7.2, 7.5
    /// </summary>
    [Property(MaxTest = 1)]
    public Property LevelSystem_Level15_ReachedAt140Lines()
    {
        var arb = Arb.From(Gen.Constant(140));

        return Prop.ForAll(arb, _ =>
        {
            var ls = new LevelSystem();
            ls.AddLines(140);
            return ls.Level == 15;
        });
    }

    /// <summary>
    /// Property 10f: Level stays at 15 beyond 140 lines.
    ///
    /// For any L >= 140, Level == 15.
    ///
    /// Validates: Requirements 7.2, 7.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property LevelSystem_Level_StaysAt15Beyond140Lines()
    {
        var arb = Arb.From(Gen.Choose(140, 500));

        return Prop.ForAll(arb, lines =>
        {
            var ls = new LevelSystem();
            ls.AddLines(lines);
            return ls.Level == 15;
        });
    }
}
