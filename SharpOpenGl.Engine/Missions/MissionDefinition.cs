namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// JSON-deserialisable data class representing a complete mission definition.
/// Load via <see cref="MissionLoader"/> — do not mutate at runtime.
/// </summary>
public sealed class MissionDefinition
{
    /// <summary>Unique machine-readable identifier (matches the JSON filename).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable mission title shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short description shown on the mission-select screen.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Map asset key to load (e.g. "sector_alpha").</summary>
    public string Map { get; set; } = string.Empty;

    /// <summary>Pre-mission briefing data.</summary>
    public BriefingDefinition Briefing { get; set; } = new();

    /// <summary>Starting state for the player at mission begin.</summary>
    public StartConditions StartConditions { get; set; } = new();

    /// <summary>Primary and secondary objectives.</summary>
    public ObjectivesBlock Objectives { get; set; } = new();

    /// <summary>Event triggers evaluated each frame.</summary>
    public List<TriggerDefinition> Triggers { get; set; } = new();

    /// <summary>Defines how the mission is won.</summary>
    public VictoryCondition Victory { get; set; } = new();

    /// <summary>Defines how the mission is lost.</summary>
    public DefeatCondition Defeat { get; set; } = new();

    /// <summary>Resources and XP awarded on victory.</summary>
    public RewardsDefinition Rewards { get; set; } = new();
}

/// <summary>Pre-mission briefing shown before gameplay begins.</summary>
public sealed class BriefingDefinition
{
    public string Text { get; set; } = string.Empty;
    public List<string> ObjectivesPreview { get; set; } = new();
}

/// <summary>Initial game state applied when the mission starts.</summary>
public sealed class StartConditions
{
    public int[] PlayerSpawn { get; set; } = [0, 0];
    public List<string> StartingUnits { get; set; } = new();
    public Dictionary<string, float> StartingResources { get; set; } = new();
}

/// <summary>Container for primary and secondary objective lists.</summary>
public sealed class ObjectivesBlock
{
    public List<ObjectiveDefinition> Primary { get; set; } = new();
    public List<ObjectiveDefinition> Secondary { get; set; } = new();
}

/// <summary>Win-condition descriptor.</summary>
public sealed class VictoryCondition
{
    /// <summary>
    /// Recognised types: "all_primary_complete".
    /// </summary>
    public string Type { get; set; } = "all_primary_complete";
}

/// <summary>Defeat-condition descriptor.</summary>
public sealed class DefeatCondition
{
    /// <summary>
    /// Recognised types: "hero_destroyed", "base_destroyed", "time_expired".
    /// </summary>
    public string Type { get; set; } = "hero_destroyed";

    /// <summary>Optional target entity id used by "base_destroyed".</summary>
    public string? Target { get; set; }

    /// <summary>Optional time limit in seconds used by "time_expired".</summary>
    public float? TimeLimit { get; set; }
}

/// <summary>Completion rewards granted when the mission is won.</summary>
public sealed class RewardsDefinition
{
    public Dictionary<string, float> Resources { get; set; } = new();
    public int Xp { get; set; }
    public List<string> Unlocks { get; set; } = new();
}
