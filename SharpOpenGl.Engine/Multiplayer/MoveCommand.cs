namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Instructs one or more entities to navigate toward a world-space position.
/// Entities listed in <see cref="EntityIds"/> will path-find to <see cref="TargetX"/>/<see cref="TargetZ"/>.
/// </summary>
public sealed class MoveCommand : IGameCommand
{
    /// <inheritdoc/>
    public CommandType Type => CommandType.Move;

    /// <inheritdoc/>
    public int PlayerId { get; init; }

    /// <inheritdoc/>
    public long Tick { get; init; }

    /// <summary>Entity index values of the units receiving this order.</summary>
    public uint[] EntityIds { get; init; } = Array.Empty<uint>();

    /// <summary>World-space X coordinate of the move destination.</summary>
    public float TargetX { get; init; }

    /// <summary>World-space Z coordinate of the move destination.</summary>
    public float TargetZ { get; init; }

    /// <summary>
    /// When <c>true</c> the units will attack any enemy encountered along the route
    /// (attack-move); when <c>false</c> units ignore enemies and move straight to the target.
    /// </summary>
    public bool AttackMove { get; init; }
}
