using System.Diagnostics;
using RetroTetris.Core.Commands;
using RetroTetris.Core.Interfaces;

namespace RetroTetris.Core.Engine;

/// <summary>
/// Dedicated game loop running on its own background thread.
/// Measures real delta time with Stopwatch and separates the three phases:
/// ProcessInput → Update → Render, capped at ~60 fps via Thread.Sleep(1).
/// </summary>
public class GameLoop
{
    private readonly IGameEngine _engine;
    private readonly CommandQueue _commandQueue;
    private Thread? _thread;
    private volatile bool _running;

    /// <summary>
    /// Optional input handler whose Update(delta) is called each frame for DAS/ARR.
    /// Set this from the MAUI layer after construction.
    /// </summary>
    public IInputHandler? InputHandler { get; set; }

    /// <summary>
    /// Optional callback invoked each frame to trigger UI redraw.
    /// Set this from the MAUI layer to call canvas.Invalidate() on the dispatcher.
    /// </summary>
    public Action? OnRender { get; set; }

    /// <summary>
    /// Whether the game loop is currently running.
    /// </summary>
    public bool IsRunning => _running;

    public GameLoop(IGameEngine engine, CommandQueue commandQueue)
    {
        _engine = engine;
        _commandQueue = commandQueue;
    }

    /// <summary>
    /// Starts the game loop on a background thread.
    /// Idempotent — does nothing if already running.
    /// </summary>
    public void Start()
    {
        if (_running) return;

        _running = true;
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "GameLoop"
        };
        _thread.Start();
    }

    /// <summary>
    /// Stops the game loop and waits for the thread to finish (up to 500ms).
    /// </summary>
    public void Stop()
    {
        _running = false;
        _thread?.Join(500);
        _thread = null;
    }

    #region Private helpers

    private void Loop()
    {
        var stopwatch = Stopwatch.StartNew();
        var previous = stopwatch.Elapsed;

        while (_running)
        {
            var current = stopwatch.Elapsed;
            var delta = current - previous;
            previous = current;

            ProcessInput(delta);
            _engine.Update(delta);
            Render();

            Thread.Sleep(1); // cap at ~60 fps, avoid 100% CPU
        }
    }

    private void ProcessInput(TimeSpan delta)
    {
        // Update DAS/ARR for held lateral keys before draining the command queue
        InputHandler?.Update(delta);

        while (_commandQueue.TryDequeue(out var command))
            command!.Execute(_engine);
    }

    private void Render()
    {
        OnRender?.Invoke();
    }

    #endregion
}
