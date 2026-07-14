using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Economy;

/// <summary>
/// Central economy service.  Owns per-player <see cref="PlayerResources"/> pools
/// and publishes <see cref="ResourceChangedEvent"/> via the event bus whenever an
/// amount changes.
/// </summary>
public sealed class ResourceManager
{
    private readonly Dictionary<int, PlayerResources> _players = new();
    private readonly EventBus? _bus;

    public ResourceManager(EventBus? bus = null) => _bus = bus;

    /// <summary>
    /// When true, <see cref="TrySpend"/> / <see cref="TrySpendCost"/> always succeed
    /// without reducing resource pools (mission <c>unlimitedResources</c> start flag).
    /// </summary>
    public bool UnlimitedResources { get; set; }

    // ── Player registration ──────────────────────────────────────────────────

    /// <summary>
    /// Register a player and return their resource pool.
    /// If <paramref name="resources"/> is <c>null</c> a default pool is created.
    /// Replaces any previously registered pool for that id.
    /// </summary>
    public PlayerResources AddPlayer(int playerId, PlayerResources? resources = null)
    {
        var pr = resources ?? new PlayerResources();
        _players[playerId] = pr;
        return pr;
    }

    /// <summary>Returns the resource pool for <paramref name="playerId"/>, or <c>null</c>.</summary>
    public PlayerResources? GetPlayer(int playerId) =>
        _players.TryGetValue(playerId, out var p) ? p : null;

    /// <summary>Enumerate all registered player resource pools.</summary>
    public IEnumerable<(int PlayerId, PlayerResources Resources)> AllPlayers() =>
        _players.Select(kv => (kv.Key, kv.Value));

    // ── Single-resource operations ───────────────────────────────────────────

    /// <summary>
    /// Try to spend <paramref name="amount"/> of <paramref name="type"/> for a player.
    /// Returns <c>true</c> and fires a change event on success.
    /// When <see cref="UnlimitedResources"/> is set, always succeeds without deducting.
    /// </summary>
    public bool TrySpend(int playerId, ResourceType type, float amount)
    {
        if (!_players.TryGetValue(playerId, out PlayerResources? pr)) return false;
        if (UnlimitedResources) return true;
        if (!pr.TrySpend(type, amount)) return false;
        Publish(playerId, type, pr.GetAmount(type));
        return true;
    }

    /// <summary>
    /// Add <paramref name="amount"/> of <paramref name="type"/> for a player.
    /// Returns the actual amount added (may be less when storage is full).
    /// Fires a change event when the amount is non-zero.
    /// </summary>
    public float Add(int playerId, ResourceType type, float amount)
    {
        if (!_players.TryGetValue(playerId, out PlayerResources? pr)) return 0f;
        float actual = pr.Add(type, amount);
        if (actual > 0f) Publish(playerId, type, pr.GetAmount(type));
        return actual;
    }

    // ── Multi-resource operations ────────────────────────────────────────────

    /// <summary>
    /// Atomically spend all four resource costs.  All costs must be affordable
    /// simultaneously; if any single type is insufficient the call returns
    /// <c>false</c> and no resources are changed.
    /// When <see cref="UnlimitedResources"/> is set, always succeeds without deducting.
    /// </summary>
    public bool TrySpendCost(int playerId, int energy, int minerals, int data, int crew)
    {
        if (!_players.TryGetValue(playerId, out PlayerResources? pr)) return false;

        if (UnlimitedResources) return true;

        // Check all before deducting any.
        if (pr.GetAmount(ResourceType.Energy)   < energy)   return false;
        if (pr.GetAmount(ResourceType.Minerals) < minerals) return false;
        if (pr.GetAmount(ResourceType.Data)     < data)     return false;
        if (pr.GetAmount(ResourceType.Crew)     < crew)     return false;

        pr.TrySpend(ResourceType.Energy,   energy);
        pr.TrySpend(ResourceType.Minerals, minerals);
        pr.TrySpend(ResourceType.Data,     data);
        pr.TrySpend(ResourceType.Crew,     crew);

        PublishAll(playerId, pr);
        return true;
    }

    /// <summary>
    /// Refund all four resource types (e.g. build cancellation).
    /// Calls <see cref="Add"/> for each non-zero value.
    /// </summary>
    public void Refund(int playerId, int energy, int minerals, int data, int crew)
    {
        if (energy   > 0) Add(playerId, ResourceType.Energy,   energy);
        if (minerals > 0) Add(playerId, ResourceType.Minerals, minerals);
        if (data     > 0) Add(playerId, ResourceType.Data,     data);
        if (crew     > 0) Add(playerId, ResourceType.Crew,     crew);
    }

    // ── Income tick ──────────────────────────────────────────────────────────

    /// <summary>
    /// Advance all player income rates by <paramref name="deltaTime"/> seconds.
    /// Fires change events only for resources whose amount actually changed.
    /// </summary>
    public void Tick(float deltaTime)
    {
        foreach (var (id, pr) in _players)
        {
            // Snapshot amounts before tick to detect changes.
            float e  = pr.GetAmount(ResourceType.Energy);
            float m  = pr.GetAmount(ResourceType.Minerals);
            float d  = pr.GetAmount(ResourceType.Data);
            float cr = pr.GetAmount(ResourceType.Crew);

            pr.Tick(deltaTime);

            MaybePublish(id, ResourceType.Energy,   e,  pr);
            MaybePublish(id, ResourceType.Minerals, m,  pr);
            MaybePublish(id, ResourceType.Data,     d,  pr);
            MaybePublish(id, ResourceType.Crew,     cr, pr);
        }
    }

    // ── UI data ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Return a UI-ready snapshot of one resource type for one player.
    /// Returns a zero-filled display if the player is not registered.
    /// </summary>
    public ResourceDisplay GetDisplay(int playerId, ResourceType type)
    {
        if (!_players.TryGetValue(playerId, out PlayerResources? pr))
            return new ResourceDisplay(type, 0f, 0f, 0f);
        return pr.GetDisplay(type);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void Publish(int playerId, ResourceType type, float newAmount) =>
        _bus?.Publish(new ResourceChangedEvent(playerId, type.ToString(), newAmount));

    private void PublishAll(int playerId, PlayerResources pr)
    {
        foreach (ResourceType rt in Enum.GetValues<ResourceType>())
            Publish(playerId, rt, pr.GetAmount(rt));
    }

    private void MaybePublish(int playerId, ResourceType type, float before, PlayerResources pr)
    {
        float after = pr.GetAmount(type);
        if (Math.Abs(after - before) > 0.0001f)
            Publish(playerId, type, after);
    }
}
