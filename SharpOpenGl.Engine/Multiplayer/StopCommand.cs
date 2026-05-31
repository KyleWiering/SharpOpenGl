namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Orders one or more entities to immediately halt all movement and actions.
/// Any queued orders for the affected entities are cleared.
/// </summary>
public sealed class StopCommand : IGameCommand
{
    /// <inheritdoc/>
    public CommandType Type => CommandType.Stop;

    /// <inheritdoc/>
    public int PlayerId { get; init; }

    /// <inheritdoc/>
    public long Tick { get; init; }

    /// <summary>Entity index values of the units to stop.</summary>
    public uint[] EntityIds { get; init; } = Array.Empty<uint>();
}
