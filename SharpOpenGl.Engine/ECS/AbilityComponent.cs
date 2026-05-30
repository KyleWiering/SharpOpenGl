namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Defines a single hero ability slot (cooldown tracking + metadata key).
/// Stored in a list via <see cref="AbilityListComponent"/>.
/// </summary>
public sealed class AbilityComponent
{
    /// <summary>Data asset key that identifies this ability's definition (e.g. "shield_boost").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Slot index on the hero's ability bar (0-based).</summary>
    public int Slot { get; set; }

    /// <summary>Seconds between uses. Set from the JSON definition.</summary>
    public float MaxCooldown { get; set; }

    /// <summary>Seconds remaining until the ability is ready. Counts down each frame.</summary>
    public float CurrentCooldown { get; set; }

    /// <summary>Returns <c>true</c> when the ability can be activated.</summary>
    public bool IsReady => CurrentCooldown <= 0f;

    /// <summary>
    /// Activate the ability: resets <see cref="CurrentCooldown"/> to <see cref="MaxCooldown"/>.
    /// Does nothing if not ready.
    /// </summary>
    public void Activate()
    {
        if (!IsReady) return;
        CurrentCooldown = MaxCooldown;
    }
}
