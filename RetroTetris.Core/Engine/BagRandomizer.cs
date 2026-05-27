namespace RetroTetris.Core.Engine;

/// <summary>
/// Implements the "7-bag" randomizer system used in modern Tetris.
/// Each bag contains all 7 tetromino types in a random order.
/// A new shuffled bag is generated automatically when the queue runs low.
/// This guarantees that each type appears exactly once per 7-piece cycle.
/// </summary>
public class BagRandomizer
{
    private readonly Queue<TetrominoType> _queue;
    private readonly Random _rng;

    /// <summary>
    /// Creates a new BagRandomizer and fills the initial bag.
    /// </summary>
    public BagRandomizer() : this(new Random()) { }

    /// <summary>
    /// Creates a new BagRandomizer with a specific Random instance (for testing).
    /// </summary>
    public BagRandomizer(Random rng)
    {
        _rng = rng;
        _queue = new Queue<TetrominoType>();
        FillBag();
    }

    /// <summary>
    /// Peeks at the Nth upcoming piece without removing it from the queue.
    /// index 0 = next, 1 = second, 2 = third.
    /// Refills with a new shuffled bag if the queue doesn't have enough pieces.
    /// </summary>
    public TetrominoType Peek(int index)
    {
        // Ensure the queue has at least (index + 1) items
        while (_queue.Count <= index)
            FillBag();

        return _queue.ElementAt(index);
    }

    /// <summary>
    /// Removes and returns the next piece from the queue.
    /// Refills with a new shuffled bag if the queue is empty.
    /// </summary>
    public TetrominoType Dequeue()
    {
        if (_queue.Count == 0)
            FillBag();

        return _queue.Dequeue();
    }

    /// <summary>
    /// Resets the randomizer by clearing the queue and filling a fresh bag.
    /// </summary>
    public void Reset()
    {
        _queue.Clear();
        FillBag();
    }

    #region Private helpers

    /// <summary>
    /// Generates a shuffled set of all 7 tetromino types and enqueues them.
    /// Uses Fisher-Yates shuffle for uniform distribution.
    /// </summary>
    private void FillBag()
    {
        var bag = Enum.GetValues<TetrominoType>(); // I, O, T, S, Z, J, L
        // Fisher-Yates shuffle
        for (int i = bag.Length - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }

        foreach (var type in bag)
            _queue.Enqueue(type);
    }

    #endregion
}
