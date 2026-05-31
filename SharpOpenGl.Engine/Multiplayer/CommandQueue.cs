namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// A thread-safe, ordered queue of <see cref="IGameCommand"/> instances.
/// Commands are grouped by simulation tick so that the game loop can dequeue
/// exactly the commands scheduled for the current tick each frame.
/// </summary>
public sealed class CommandQueue
{
    // Sorted dictionary: tick → list of commands for that tick
    private readonly SortedDictionary<long, List<IGameCommand>> _pending = new();
    private readonly object _lock = new();

    /// <summary>Total number of commands currently enqueued across all ticks.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                int total = 0;
                foreach (var list in _pending.Values)
                    total += list.Count;
                return total;
            }
        }
    }

    /// <summary>
    /// Enqueue a command for execution at its <see cref="IGameCommand.Tick"/>.
    /// Safe to call from any thread.
    /// </summary>
    public void Enqueue(IGameCommand command)
    {
        lock (_lock)
        {
            if (!_pending.TryGetValue(command.Tick, out var list))
            {
                list = new List<IGameCommand>();
                _pending[command.Tick] = list;
            }
            list.Add(command);
        }
    }

    /// <summary>
    /// Dequeue and return all commands whose tick is &lt;= <paramref name="currentTick"/>.
    /// Returns an empty array when no commands are ready.
    /// </summary>
    public IGameCommand[] DrainUpTo(long currentTick)
    {
        lock (_lock)
        {
            var result = new List<IGameCommand>();
            var toRemove = new List<long>();

            foreach (var (tick, list) in _pending)
            {
                if (tick > currentTick) break;
                result.AddRange(list);
                toRemove.Add(tick);
            }

            foreach (long tick in toRemove)
                _pending.Remove(tick);

            return result.ToArray();
        }
    }

    /// <summary>Remove all pending commands from the queue.</summary>
    public void Clear()
    {
        lock (_lock) _pending.Clear();
    }
}
