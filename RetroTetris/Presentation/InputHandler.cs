using RetroTetris.Core.Commands;
using RetroTetris.Core.Interfaces;
using CoreSwipeDirection = RetroTetris.Core.Interfaces.SwipeDirection;

namespace RetroTetris.Presentation;

/// <summary>
/// Translates keyboard and touch input into IGameCommand objects enqueued on the CommandQueue.
/// Implements DAS (Delayed Auto Shift) and ARR (Auto Repeat Rate) for lateral movement:
///   - After holding Left/Right for 170ms, repeat every 50ms.
/// Touch: swipe left/right/down/up and tap left/right half of screen.
/// </summary>
public class InputHandler : IInputHandler
{
    private const double DasDelayMs = 170.0;
    private const double ArrIntervalMs = 50.0;

    private readonly CommandQueue _commandQueue;

    // DAS/ARR state
    private GameKey? _heldKey;
    private double _dasAccumulatorMs;
    private double _arrAccumulatorMs;
    private bool _dasTriggered;

    public InputHandler(CommandQueue commandQueue)
    {
        _commandQueue = commandQueue;
    }

    // ─── IInputHandler ─────────────────────────────────────────────────────────

    public void OnKeyDown(GameKey key)
    {
        // Enqueue the immediate command
        var command = MapKeyToCommand(key);
        if (command is not null)
            _commandQueue.Enqueue(command);

        // Start DAS tracking for lateral keys
        if (key is GameKey.Left or GameKey.Right)
        {
            _heldKey = key;
            _dasAccumulatorMs = 0;
            _arrAccumulatorMs = 0;
            _dasTriggered = false;
        }
    }

    public void OnKeyUp(GameKey key)
    {
        if (_heldKey == key)
        {
            _heldKey = null;
            _dasTriggered = false;
        }
    }

    public void OnSwipe(CoreSwipeDirection direction)
    {
        var command = direction switch
        {
            CoreSwipeDirection.Left  => (IGameCommand)new MoveLeftCommand(),
            CoreSwipeDirection.Right => new MoveRightCommand(),
            CoreSwipeDirection.Down  => new SoftDropCommand(),
            CoreSwipeDirection.Up    => new HardDropCommand(),
            _ => null
        };
        if (command is not null)
            _commandQueue.Enqueue(command);
    }

    public void OnTap(TapSide side)
    {
        IGameCommand command = side == TapSide.Left
            ? new RotateCounterClockwiseCommand()
            : new RotateClockwiseCommand();
        _commandQueue.Enqueue(command);
    }

    /// <summary>
    /// Called each frame by the GameLoop to handle DAS/ARR repeat.
    /// </summary>
    public void Update(TimeSpan elapsed)
    {
        if (_heldKey is null) return;

        double ms = elapsed.TotalMilliseconds;

        if (!_dasTriggered)
        {
            _dasAccumulatorMs += ms;
            if (_dasAccumulatorMs >= DasDelayMs)
            {
                _dasTriggered = true;
                _arrAccumulatorMs = 0;
            }
        }
        else
        {
            _arrAccumulatorMs += ms;
            while (_arrAccumulatorMs >= ArrIntervalMs)
            {
                _arrAccumulatorMs -= ArrIntervalMs;
                var cmd = _heldKey == GameKey.Left
                    ? (IGameCommand)new MoveLeftCommand()
                    : new MoveRightCommand();
                _commandQueue.Enqueue(cmd);
            }
        }
    }

    // ─── Key mapping ───────────────────────────────────────────────────────────

    private static IGameCommand? MapKeyToCommand(GameKey key) => key switch
    {
        GameKey.Left    => new MoveLeftCommand(),
        GameKey.Right   => new MoveRightCommand(),
        GameKey.Down    => new SoftDropCommand(),
        GameKey.Space   => new HardDropCommand(),
        GameKey.Up      => new RotateClockwiseCommand(),
        GameKey.Z       => new RotateClockwiseCommand(),
        GameKey.X       => new RotateCounterClockwiseCommand(),
        GameKey.C       => new HoldCommand(),
        GameKey.Shift   => new HoldCommand(),
        GameKey.Escape  => new TogglePauseCommand(),
        GameKey.P       => new TogglePauseCommand(),
        GameKey.H       => new ToggleControlsCommand(),
        _ => null
    };
}
