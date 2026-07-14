namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Orders one or more repair-capable entities to restore a specific friendly target.
/// </summary>
public sealed class RepairCommand : IGameCommand
{
    /// <inheritdoc/>
    public CommandType Type => CommandType.Repair;

    /// <inheritdoc/>
    public int PlayerId { get; init; }

    /// <inheritdoc/>
    public long Tick { get; init; }

    /// <summary>Entity index values of the repairers receiving this order.</summary>
    public uint[] RepairerIds { get; init; } = Array.Empty<uint>();

    /// <summary>Entity index of the hull to repair.</summary>
    public uint TargetEntityId { get; init; }
}