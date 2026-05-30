namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Determines which enemy is selected when a unit acquires a new attack target.
/// </summary>
public enum TargetPriority
{
    /// <summary>Attack the enemy that is closest in world space.</summary>
    Closest,

    /// <summary>Attack the enemy with the fewest remaining hit points.</summary>
    LowestHP,

    /// <summary>Attack the enemy whose <see cref="CombatTargetComponent.Priority"/> value is highest.</summary>
    HighestPriority,
}
