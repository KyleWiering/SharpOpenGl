namespace SharpOpenGl.Engine.Economy;

/// <summary>
/// Production queue for a single building entity.
/// Resources are deducted immediately on <see cref="Enqueue"/> and
/// refunded on <see cref="CancelLast"/> / <see cref="CancelCurrent"/> / <see cref="CancelAll"/>.
/// </summary>
public sealed class BuildQueue
{
    private readonly Queue<BuildQueueItem> _items = new();

    /// <summary>Number of items currently queued.</summary>
    public int Count => _items.Count;

    /// <summary>The item currently being built, or <c>null</c> when the queue is empty.</summary>
    public BuildQueueItem? Current => _items.TryPeek(out var item) ? item : null;

    /// <summary>All queued items in order (read-only view).</summary>
    public IEnumerable<BuildQueueItem> Items => _items;

    // ── Enqueue ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to add an item to the queue.
    /// Resources are deducted immediately from <paramref name="resources"/> for <paramref name="playerId"/>.
    /// Returns <c>false</c> without modifying state if the player cannot afford the cost.
    /// </summary>
    public bool Enqueue(
        string definitionId,
        int energy, int minerals, int data, int crew,
        float buildTime,
        ResourceManager resources,
        int playerId)
    {
        if (!resources.TrySpendCost(playerId, energy, minerals, data, crew))
            return false;

        _items.Enqueue(new BuildQueueItem(definitionId, energy, minerals, data, crew, buildTime));
        return true;
    }

    // ── Advance ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Advance the front item by <paramref name="deltaTime"/> × <paramref name="productionRate"/>.
    /// Returns the completed <see cref="BuildQueueItem"/> when one finishes, otherwise <c>null</c>.
    /// </summary>
    public BuildQueueItem? Advance(float deltaTime, float productionRate = 1f)
    {
        if (!_items.TryPeek(out BuildQueueItem? item)) return null;

        item.Progress += deltaTime * productionRate;
        if (!item.IsComplete) return null;

        _items.Dequeue();
        return item;
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Cancel the last item in the queue, refunding its full cost.
    /// Returns <c>false</c> when the queue is empty.
    /// </summary>
    public bool CancelLast(ResourceManager resources, int playerId)
    {
        if (_items.Count == 0) return false;

        // Rebuild queue without the tail element.
        BuildQueueItem[] snapshot = _items.ToArray();
        _items.Clear();
        for (int i = 0; i < snapshot.Length - 1; i++)
            _items.Enqueue(snapshot[i]);

        BuildQueueItem cancelled = snapshot[^1];
        resources.Refund(playerId,
            cancelled.EnergyCost, cancelled.MineralsCost,
            cancelled.DataCost,   cancelled.CrewCost);
        return true;
    }

    /// <summary>
    /// Cancel the currently-building (front) item, refunding its full cost.
    /// Returns <c>false</c> when the queue is empty.
    /// </summary>
    public bool CancelCurrent(ResourceManager resources, int playerId)
    {
        if (!_items.TryDequeue(out BuildQueueItem? item)) return false;
        resources.Refund(playerId,
            item.EnergyCost, item.MineralsCost,
            item.DataCost,   item.CrewCost);
        return true;
    }

    /// <summary>Cancel all queued items, refunding all costs.</summary>
    public void CancelAll(ResourceManager resources, int playerId)
    {
        while (_items.TryDequeue(out BuildQueueItem? item))
            resources.Refund(playerId,
                item.EnergyCost, item.MineralsCost,
                item.DataCost,   item.CrewCost);
    }
}
