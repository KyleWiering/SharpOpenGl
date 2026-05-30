namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Lightweight entity identifier composed of a numeric index and a generation counter.
/// The generation counter allows detecting references to destroyed entities (stale handles).
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    /// <summary>Index into the entity slot array.</summary>
    public uint Index { get; }

    /// <summary>Generation counter incremented each time the slot is recycled.</summary>
    public ushort Generation { get; }

    /// <summary>A sentinel value that represents "no entity".</summary>
    public static readonly Entity Null = new(0, 0);

    internal Entity(uint index, ushort generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <inheritdoc/>
    public bool Equals(Entity other) => Index == other.Index && Generation == other.Generation;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Entity e && Equals(e);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    /// <inheritdoc/>
    public override string ToString() => $"Entity({Index}:{Generation})";

    public static bool operator ==(Entity a, Entity b) => a.Equals(b);
    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
}
