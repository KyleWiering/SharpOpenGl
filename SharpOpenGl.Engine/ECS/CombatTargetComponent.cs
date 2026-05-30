namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Tracks the entity that this combatant is currently attacking.
/// Attached to any entity that can engage in combat.
/// </summary>
public sealed class CombatTargetComponent
{
    /// <summary>Currently locked-on target, or <see cref="Entity.Null"/> if idle.</summary>
    public Entity CurrentTarget { get; set; } = Entity.Null;

    /// <summary>Faction this entity belongs to. Entities with the same faction will not attack each other.</summary>
    public int Faction { get; set; }

    /// <summary>
    /// Manual targeting priority for <see cref="TargetPriority.HighestPriority"/> logic.
    /// Higher numbers are targeted first (e.g. hero = 100, building = 50, fighter = 10).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>Which targeting rule to use when this unit acquires a new target.</summary>
    public TargetPriority TargetingMode { get; set; } = TargetPriority.Closest;

    /// <summary>Seconds until this unit can issue an auto-attack order again.</summary>
    public float AttackCooldown { get; set; }
}
