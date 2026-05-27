using System.Collections.Concurrent;

namespace RetroTetris.Core.Commands;

/// <summary>
/// Thread-safe queue of game commands.
/// The InputHandler enqueues from the UI thread; the GameLoop dequeues from the game loop thread.
/// </summary>
public class CommandQueue
{
    private readonly ConcurrentQueue<IGameCommand> _queue = new();

    public void Enqueue(IGameCommand command) => _queue.Enqueue(command);
    public bool TryDequeue(out IGameCommand? command) => _queue.TryDequeue(out command);
    public void Clear() => _queue.Clear();
}
