namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Central ECS container. Owns all entities, component pools, and systems.
/// </summary>
/// <remarks>
/// Entity lifecycle:
/// <list type="number">
///   <item>Call <see cref="CreateEntity"/> to obtain a live <see cref="Entity"/>.</item>
///   <item>Attach components with <see cref="AddComponent{T}"/>.</item>
///   <item>Call <see cref="DestroyEntity"/> to recycle the slot (generation is bumped).</item>
/// </list>
/// </remarks>
public sealed class World : IDisposable
{
    // ── Entity slots ──────────────────────────────────────────────────────────

    private readonly List<ushort> _generations = new();   // one per slot index
    private readonly Queue<uint> _freeSlots = new();
    private int _liveCount;

    // ── Component storage ─────────────────────────────────────────────────────

    private readonly Dictionary<Type, IComponentPool> _pools = new();

    // ── Systems ───────────────────────────────────────────────────────────────

    private readonly List<GameSystem> _systems = new();

    // ─────────────────────────────────────────────────────────────────────────
    // Entity management
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Create a new live entity and return its handle.</summary>
    public Entity CreateEntity()
    {
        uint index;
        ushort gen;

        if (_freeSlots.TryDequeue(out index))
        {
            gen = _generations[(int)index];
        }
        else
        {
            index = (uint)_generations.Count;
            _generations.Add(1);
            gen = 1;
        }

        _liveCount++;
        return new Entity(index, gen);
    }

    /// <summary>
    /// Destroy an entity, removing all its components and recycling the slot.
    /// Silently ignores stale/null handles.
    /// </summary>
    public void DestroyEntity(Entity entity)
    {
        if (!IsAlive(entity)) return;

        // Remove every component that belongs to this entity.
        foreach (IComponentPool pool in _pools.Values)
            pool.Remove(entity.Index);

        // Bump generation so old handles become stale.
        _generations[(int)entity.Index] =
            (ushort)(_generations[(int)entity.Index] + 1);

        _freeSlots.Enqueue(entity.Index);
        _liveCount--;
    }

    /// <summary>Returns <c>true</c> if the entity is alive (not destroyed or stale).</summary>
    public bool IsAlive(Entity entity)
    {
        if (entity == Entity.Null) return false;
        int idx = (int)entity.Index;
        return idx < _generations.Count && _generations[idx] == entity.Generation;
    }

    /// <summary>Number of currently live entities.</summary>
    public int LiveCount => _liveCount;

    // ─────────────────────────────────────────────────────────────────────────
    // Component management
    // ─────────────────────────────────────────────────────────────────────────

    private ComponentPool<T> GetOrCreatePool<T>() where T : class
    {
        Type t = typeof(T);
        if (!_pools.TryGetValue(t, out IComponentPool? p))
        {
            p = new ComponentPool<T>();
            _pools[t] = p;
        }
        return (ComponentPool<T>)p;
    }

    /// <summary>Attach or replace a component on an entity.</summary>
    /// <exception cref="ArgumentException">Thrown if the entity is not alive.</exception>
    public void AddComponent<T>(Entity entity, T component) where T : class
    {
        if (!IsAlive(entity))
            throw new ArgumentException($"Entity {entity} is not alive.", nameof(entity));
        GetOrCreatePool<T>().Set(entity.Index, component);
    }

    /// <summary>Retrieve a component, or <c>null</c> if absent.</summary>
    public T? GetComponent<T>(Entity entity) where T : class
    {
        if (!IsAlive(entity)) return null;
        return _pools.TryGetValue(typeof(T), out IComponentPool? p)
            ? ((ComponentPool<T>)p).Get(entity.Index)
            : null;
    }

    /// <summary>Returns <c>true</c> if the entity has a component of type <typeparamref name="T"/>.</summary>
    public bool HasComponent<T>(Entity entity) where T : class
    {
        if (!IsAlive(entity)) return false;
        return _pools.TryGetValue(typeof(T), out IComponentPool? p)
            && ((ComponentPool<T>)p).Has(entity.Index);
    }

    /// <summary>Remove a component from an entity. No-op if absent.</summary>
    public void RemoveComponent<T>(Entity entity) where T : class
    {
        if (!IsAlive(entity)) return;
        if (_pools.TryGetValue(typeof(T), out IComponentPool? p))
            p.Remove(entity.Index);
    }

    /// <summary>
    /// Enumerate all live entities that have component <typeparamref name="T"/>.
    /// Returns (Entity, component) pairs.
    /// </summary>
    public IEnumerable<(Entity Entity, T Component)> Query<T>() where T : class
    {
        if (!_pools.TryGetValue(typeof(T), out IComponentPool? p))
            yield break;

        foreach (var (index, component) in ((ComponentPool<T>)p).All())
        {
            var entity = new Entity(index, _generations[(int)index]);
            if (IsAlive(entity))
                yield return (entity, component);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // System management
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Register a system. Systems are ticked in registration order.</summary>
    public void AddSystem(GameSystem system) => _systems.Add(system);

    /// <summary>Remove a previously registered system.</summary>
    public void RemoveSystem(GameSystem system) => _systems.Remove(system);

    /// <summary>
    /// Tick all registered systems in order.
    /// </summary>
    /// <param name="deltaTime">Elapsed time in seconds since the last frame.</param>
    public void Update(float deltaTime)
    {
        foreach (GameSystem system in _systems)
            system.Tick(this, deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cleanup
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Destroy all entities, clear all pools, and dispose all systems.</summary>
    public void Dispose()
    {
        foreach (GameSystem s in _systems)
            s.Dispose();
        _systems.Clear();

        foreach (IComponentPool pool in _pools.Values)
            pool.Clear();
        _pools.Clear();

        _generations.Clear();
        _freeSlots.Clear();
        _liveCount = 0;
    }
}
