using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Outcome of a <see cref="WorldLoadService"/> restore operation.
/// </summary>
public sealed class WorldLoadResult
{
    /// <summary>Number of entities restored from the save file.</summary>
    public int EntityCount { get; init; }

    /// <summary>Hero entity after restore, or <see cref="Entity.Null"/> when absent.</summary>
    public Entity HeroEntity { get; init; } = Entity.Null;

    /// <summary>Command-center entity when present.</summary>
    public Entity CommandCenterEntity { get; init; } = Entity.Null;

    /// <summary>Maps saved entity indices to newly created entities.</summary>
    public IReadOnlyDictionary<int, Entity> EntityIdMap { get; init; } =
        new Dictionary<int, Entity>();
}