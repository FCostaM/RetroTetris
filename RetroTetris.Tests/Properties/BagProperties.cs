using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using RetroTetris.Core.Engine;

namespace RetroTetris.Tests.Properties;

/// <summary>
/// Property 7: Bag randomizer guarantees uniform distribution.
///
/// For any N complete cycles (N×7 pieces), each of the 7 tetromino types
/// must appear exactly N times.
///
/// Validates: Requirement 3.5
/// </summary>
public class BagProperties
{
    /// <summary>
    /// Property 7: For any N complete cycles (N×7 pieces dequeued),
    /// each of the 7 tetromino types appears exactly N times.
    ///
    /// Validates: Requirement 3.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property BagRandomizer_UniformDistribution_OverNCompleteCycles()
    {
        // N ranges from 1 to 20 complete cycles
        var nArb = Arb.From(Gen.Choose(1, 20));

        return Prop.ForAll(nArb, n =>
        {
            var bag = new BagRandomizer();
            int totalPieces = n * 7;

            var counts = new Dictionary<TetrominoType, int>();
            foreach (TetrominoType type in Enum.GetValues<TetrominoType>())
                counts[type] = 0;

            for (int i = 0; i < totalPieces; i++)
                counts[bag.Dequeue()]++;

            // Each type must appear exactly N times
            return counts.Values.All(count => count == n);
        });
    }

    /// <summary>
    /// Property: Each individual bag of 7 pieces contains all 7 types exactly once.
    /// This is the per-cycle invariant that underlies the uniform distribution property.
    ///
    /// Validates: Requirement 3.5
    /// </summary>
    [Property(MaxTest = 500)]
    public Property BagRandomizer_EachBagContainsAllSevenTypes()
    {
        var nArb = Arb.From(Gen.Choose(1, 50)); // test up to 50 bags

        return Prop.ForAll(nArb, bagCount =>
        {
            var bag = new BagRandomizer();
            var allTypes = Enum.GetValues<TetrominoType>();

            for (int b = 0; b < bagCount; b++)
            {
                var seenInBag = new HashSet<TetrominoType>();
                for (int i = 0; i < 7; i++)
                {
                    var type = bag.Dequeue();
                    if (!seenInBag.Add(type))
                        return false; // duplicate in same bag
                }

                // All 7 types must be present
                if (seenInBag.Count != 7)
                    return false;

                foreach (var type in allTypes)
                    if (!seenInBag.Contains(type))
                        return false;
            }

            return true;
        });
    }

    /// <summary>
    /// Property: Peek(index) returns the same value as Dequeue() for the same position.
    /// Peeking should not consume items from the queue.
    ///
    /// Validates: Requirement 3.5
    /// </summary>
    [Property(MaxTest = 300)]
    public Property BagRandomizer_Peek_DoesNotConsumeItems()
    {
        var indexArb = Arb.From(Gen.Choose(0, 2)); // peek indices 0, 1, 2

        return Prop.ForAll(indexArb, index =>
        {
            var bag = new BagRandomizer();

            // Peek at position `index`
            var peeked = bag.Peek(index);

            // Dequeue `index` items to get to that position
            for (int i = 0; i < index; i++)
                bag.Dequeue();

            // The next dequeue should match what we peeked
            var dequeued = bag.Dequeue();
            return peeked == dequeued;
        });
    }

    /// <summary>
    /// Property: After Reset(), the bag produces a fresh sequence of 7 complete cycles
    /// with uniform distribution.
    ///
    /// Validates: Requirement 3.5
    /// </summary>
    [Property(MaxTest = 200)]
    public Property BagRandomizer_Reset_ProducesFreshUniformDistribution()
    {
        var nArb = Arb.From(Gen.Choose(1, 10));

        return Prop.ForAll(nArb, n =>
        {
            var bag = new BagRandomizer();

            // Consume some pieces, then reset
            for (int i = 0; i < 3; i++)
                bag.Dequeue();

            bag.Reset();

            // After reset, N complete cycles should still be uniform
            int totalPieces = n * 7;
            var counts = new Dictionary<TetrominoType, int>();
            foreach (TetrominoType type in Enum.GetValues<TetrominoType>())
                counts[type] = 0;

            for (int i = 0; i < totalPieces; i++)
                counts[bag.Dequeue()]++;

            return counts.Values.All(count => count == n);
        });
    }
}
