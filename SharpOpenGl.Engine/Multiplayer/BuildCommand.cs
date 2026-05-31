namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Queues a production or construction order at a building entity.
/// The building identified by <see cref="BuilderEntityId"/> will begin constructing
/// the item referenced by <see cref="ItemId"/> once resources are available.
/// </summary>
public sealed class BuildCommand : IGameCommand
{
    /// <inheritdoc/>
    public CommandType Type => CommandType.Build;

    /// <inheritdoc/>
    public int PlayerId { get; init; }

    /// <inheritdoc/>
    public long Tick { get; init; }

    /// <summary>Entity index of the building or unit that will execute the build order.</summary>
    public uint BuilderEntityId { get; init; }

    /// <summary>
    /// Data-driven identifier of the item to build (maps to a JSON definition under
    /// <c>GameData/Ships/</c>, <c>GameData/Bases/</c>, or <c>GameData/Units/</c>).
    /// </summary>
    public string ItemId { get; init; } = string.Empty;
}
