using RetroTetris.Core.Commands;

namespace RetroTetris.Tests.Unit;

public class CommandQueueTests
{
    [Fact]
    public void Enqueue_ThenTryDequeue_ReturnsCommand()
    {
        var queue = new CommandQueue();
        var command = new MoveLeftCommand();

        queue.Enqueue(command);
        bool result = queue.TryDequeue(out var dequeued);

        Assert.True(result);
        Assert.Same(command, dequeued);
    }

    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsFalse()
    {
        var queue = new CommandQueue();

        bool result = queue.TryDequeue(out var dequeued);

        Assert.False(result);
        Assert.Null(dequeued);
    }

    [Fact]
    public void Clear_RemovesAllCommands()
    {
        var queue = new CommandQueue();
        queue.Enqueue(new MoveLeftCommand());
        queue.Enqueue(new MoveRightCommand());
        queue.Enqueue(new HardDropCommand());

        queue.Clear();

        Assert.False(queue.TryDequeue(out _));
    }

    [Fact]
    public void Enqueue_MultipleCommands_DequeuesInFIFOOrder()
    {
        var queue = new CommandQueue();
        var first = new MoveLeftCommand();
        var second = new MoveRightCommand();
        var third = new HardDropCommand();

        queue.Enqueue(first);
        queue.Enqueue(second);
        queue.Enqueue(third);

        queue.TryDequeue(out var d1);
        queue.TryDequeue(out var d2);
        queue.TryDequeue(out var d3);

        Assert.Same(first, d1);
        Assert.Same(second, d2);
        Assert.Same(third, d3);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentEnqueueDequeue_AllCommandsProcessed()
    {
        var queue = new CommandQueue();
        int threadCount = 10;
        int commandsPerThread = 100;

        // Enqueue from multiple threads concurrently
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < commandsPerThread; i++)
                queue.Enqueue(new MoveLeftCommand());
        })).ToArray();

        await Task.WhenAll(tasks);

        // Drain the queue
        int count = 0;
        while (queue.TryDequeue(out _))
            count++;

        Assert.Equal(threadCount * commandsPerThread, count);
    }
}
