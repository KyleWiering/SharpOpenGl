namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Orders one or more entities to attack a specific target entity.
/// Attackers will move into weapon range if necessary and then engage until the
/// target is destroyed or a new order is issued.
/// </summary>
public sealed class AttackCommand : IGameCommand
{
    /// <inheritdoc/>
    public CommandType Type => CommandType.Attack;

    /// <inheritdoc/>
    public int PlayerId { get; init; }

    /// <inheritdoc/>
    public long Tick { get; init; }

    /// <summary>Entity index values of the attacking units.</summary>
    public uint[] AttackerIds { get; init; } = Array.Empty<uint>();

    /// <summary>Entity index of the target to attack.</summary>
    public uint TargetEntityId { get; init; }
}
