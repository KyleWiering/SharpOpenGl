namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Activates a named ability on a source entity, optionally aimed at a target entity
/// or world-space position.
/// </summary>
public sealed class UseAbilityCommand : IGameCommand
{
    /// <inheritdoc/>
    public CommandType Type => CommandType.UseAbility;

    /// <inheritdoc/>
    public int PlayerId { get; init; }

    /// <inheritdoc/>
    public long Tick { get; init; }

    /// <summary>Entity index of the unit whose ability is being activated.</summary>
    public uint SourceEntityId { get; init; }

    /// <summary>String key identifying the ability (matches the JSON ability definition).</summary>
    public string AbilityId { get; init; } = string.Empty;

    /// <summary>Optional entity index target. <c>0</c> means no entity target.</summary>
    public uint TargetEntityId { get; init; }

    /// <summary>Optional world-space X coordinate target. Used when targeting a position.</summary>
    public float TargetX { get; init; }

    /// <summary>Optional world-space Z coordinate target. Used when targeting a position.</summary>
    public float TargetZ { get; init; }
}
