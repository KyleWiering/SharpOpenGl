namespace SharpOpenGl.Engine.Events;

/// <summary>
/// Lightweight, synchronous, typed event bus for decoupled system communication.
/// Subscribe with <see cref="Subscribe{T}"/> and fire events with <see cref="Publish{T}"/>.
/// </summary>
public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    /// <summary>
    /// Subscribe <paramref name="handler"/> to events of type <typeparamref name="T"/>.
    /// Returns an unsubscribe token (call <see cref="Unsubscribe{T}"/> with the same delegate to remove).
    /// </summary>
    public void Subscribe<T>(Action<T> handler)
    {
        Type t = typeof(T);
        if (!_handlers.TryGetValue(t, out List<Delegate>? list))
        {
            list = new List<Delegate>();
            _handlers[t] = list;
        }
        list.Add(handler);
    }

    /// <summary>Remove a previously subscribed handler.</summary>
    public void Unsubscribe<T>(Action<T> handler)
    {
        if (_handlers.TryGetValue(typeof(T), out List<Delegate>? list))
            list.Remove(handler);
    }

    /// <summary>
    /// Dispatch an event to all subscribers. Handlers are called synchronously
    /// in subscription order. Exceptions in one handler do not prevent others.
    /// </summary>
    public void Publish<T>(T evt)
    {
        if (!_handlers.TryGetValue(typeof(T), out List<Delegate>? list))
            return;

        // Snapshot to avoid modification-during-iteration issues
        var snapshot = list.ToArray();
        foreach (Delegate d in snapshot)
        {
            try
            {
                ((Action<T>)d)(evt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EventBus] Handler threw for {typeof(T).Name}: {ex.Message}");
            }
        }
    }

    /// <summary>Remove all subscriptions.</summary>
    public void Clear() => _handlers.Clear();
}
