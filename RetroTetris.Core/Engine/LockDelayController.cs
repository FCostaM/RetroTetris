namespace RetroTetris.Core.Engine;

/// <summary>
/// Manages the lock delay mechanic: a 500ms timer that starts when the active piece
/// lands on a surface. The timer can be reset up to 15 times (e.g., on movement/rotation).
/// When the timer expires, the piece is locked in place.
/// </summary>
public class LockDelayController
{
    public const int DelayMs = 500;
    public const int MaxResets = 15;

    private bool _isActive;
    private TimeSpan _accumulated;
    private int _resetCount;

    /// <summary>
    /// Whether the lock delay is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Activates the lock delay. Resets the elapsed accumulator and reset counter.
    /// </summary>
    public void Start()
    {
        _isActive = true;
        _accumulated = TimeSpan.Zero;
        _resetCount = 0;
    }

    /// <summary>
    /// Restarts the elapsed timer. Only works if active and the reset count is below MaxResets.
    /// If MaxResets has been reached, this is a no-op — the timer continues from where it was.
    /// </summary>
    public void Reset()
    {
        if (!_isActive)
            return;

        if (_resetCount < MaxResets)
        {
            _accumulated = TimeSpan.Zero;
            _resetCount++;
        }
        // If _resetCount >= MaxResets, do nothing — timer keeps running toward expiry
    }

    /// <summary>
    /// Deactivates the lock delay.
    /// </summary>
    public void Cancel()
    {
        _isActive = false;
    }

    /// <summary>
    /// Accumulates the given elapsed time and returns true if the total accumulated time
    /// has reached or exceeded DelayMs (500ms). Returns false if not active.
    /// </summary>
    /// <param name="elapsed">The delta time since the last call.</param>
    public bool HasExpired(TimeSpan elapsed)
    {
        if (!_isActive)
            return false;

        _accumulated += elapsed;
        return _accumulated.TotalMilliseconds >= DelayMs;
    }
}
