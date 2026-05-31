namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marks an entity as controlled by the computer AI player.
/// The <see cref="AIPlayerSystem"/> will issue commands to these entities.
/// </summary>
public sealed class AIControlledComponent
{
    /// <summary>Player ID of the AI owner (used for faction identification).</summary>
    public int PlayerId { get; set; } = 2;

    /// <summary>Aggressiveness level: 0 = passive patrol, 1 = seek and destroy.</summary>
    public float Aggressiveness { get; set; } = 0.5f;
}
