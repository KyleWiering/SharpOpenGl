namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Temporarily prevents an entity from attacking or moving (EMP, gravity rift, freeze field).
/// Ticked down by <see cref="AbilitySystem"/>.
/// </summary>
public sealed class DisabledComponent
{
    /// <summary>Seconds remaining before the unit can act again.</summary>
    public float RemainingSeconds { get; set; }

    /// <summary>Returns <c>true</c> while the disable effect is active.</summary>
    public bool IsActive => RemainingSeconds > 0f;
}