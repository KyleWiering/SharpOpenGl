namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Stores a single component type for all entities using a dictionary keyed by entity index.
/// Implements <see cref="IComponentPool"/> for type-erased disposal by <see cref="World"/>.
/// </summary>
internal interface IComponentPool
{
    /// <summary>Remove the component for the given entity index, if present.</summary>
    void Remove(uint entityIndex);

    /// <summary>Remove all components.</summary>
    void Clear();

    /// <summary>Returns <c>true</c> when the entity index has a component in this pool.</summary>
    bool Has(uint entityIndex);
}

/// <summary>
/// Generic sparse-dictionary component pool.
/// Each entity may have at most one component of type <typeparamref name="T"/>.
/// </summary>
public sealed class ComponentPool<T> : IComponentPool where T : class
{
    private readonly Dictionary<uint, T> _data = new();

    /// <summary>Store or replace the component for the given entity.</summary>
    public void Set(uint entityIndex, T component) => _data[entityIndex] = component;

    /// <summary>Retrieve the component, or <c>null</c> if absent.</summary>
    public T? Get(uint entityIndex) => _data.GetValueOrDefault(entityIndex);

    /// <summary>Returns <c>true</c> if the entity has a component of this type.</summary>
    public bool Has(uint entityIndex) => _data.ContainsKey(entityIndex);

    /// <inheritdoc/>
    public void Remove(uint entityIndex) => _data.Remove(entityIndex);

    /// <inheritdoc/>
    public void Clear() => _data.Clear();

    /// <summary>Iterate over all (entityIndex, component) pairs.</summary>
    public IEnumerable<(uint Index, T Component)> All()
    {
        foreach (var kv in _data)
            yield return (kv.Key, kv.Value);
    }
}
