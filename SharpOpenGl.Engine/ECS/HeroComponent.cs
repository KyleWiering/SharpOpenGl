namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marks an entity as the player's hero ship and tracks its progression state.
/// Only one entity should carry this component at a time.
/// </summary>
public sealed class HeroComponent
{
    /// <summary>Current hero level (1-based).</summary>
    public int Level { get; set; } = 1;

    /// <summary>Accumulated experience points toward the next level.</summary>
    public int XP { get; set; }

    /// <summary>
    /// Asset key for the upgrade tech tree JSON
    /// (e.g. "tech_trees/hero_vanguard").
    /// </summary>
    public string UpgradeTreeKey { get; set; } = string.Empty;

    /// <summary>
    /// Ability slot bindings: index → ability definition key.
    /// Populated from JSON at spawn time.
    /// </summary>
    public Dictionary<int, string> AbilitySlots { get; set; } = new();

    /// <summary>
    /// Per-ability cooldown remaining in seconds, keyed by slot index.
    /// </summary>
    public Dictionary<int, float> AbilityCooldowns { get; set; } = new();
}
