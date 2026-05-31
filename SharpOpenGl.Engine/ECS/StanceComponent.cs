namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Combat stance that governs a ship's automatic engagement behavior.
/// </summary>
public enum Stance
{
    /// <summary>Ship does not auto-engage enemies.</summary>
    Neutral,

    /// <summary>Ship auto-attacks enemies within defensive radius or that attack it.</summary>
    Defensive,

    /// <summary>Ship pursues any enemy within weapon range.</summary>
    Aggressive
}

/// <summary>
/// Configures the combat stance for an entity.
/// </summary>
public sealed class StanceComponent
{
    /// <summary>Current combat stance.</summary>
    public Stance CurrentStance { get; set; } = Stance.Defensive;
}
