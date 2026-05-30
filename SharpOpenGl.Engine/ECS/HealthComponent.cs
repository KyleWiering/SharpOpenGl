namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Tracks hit points, shields, and armor for an entity.
/// </summary>
public sealed class HealthComponent
{
    /// <summary>Current hit points. Entity should be destroyed when this reaches 0.</summary>
    public float CurrentHP { get; set; }

    /// <summary>Maximum hit points.</summary>
    public float MaxHP { get; set; }

    /// <summary>
    /// Current shield strength. Shields absorb damage before HP and regenerate over time.
    /// </summary>
    public float CurrentShields { get; set; }

    /// <summary>Maximum shield strength.</summary>
    public float MaxShields { get; set; }

    /// <summary>
    /// Flat damage reduction applied after shields are depleted.
    /// </summary>
    public float Armor { get; set; }

    /// <summary>Returns <c>true</c> when the entity has no remaining hit points.</summary>
    public bool IsDead => CurrentHP <= 0f;

    /// <summary>HP as a value in [0, 1].</summary>
    public float HPFraction => MaxHP > 0f ? CurrentHP / MaxHP : 0f;

    /// <summary>Shield as a value in [0, 1].</summary>
    public float ShieldFraction => MaxShields > 0f ? CurrentShields / MaxShields : 0f;
}
