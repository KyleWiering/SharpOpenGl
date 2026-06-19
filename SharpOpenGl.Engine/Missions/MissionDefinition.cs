namespace SharpOpenGl.Engine.Missions;

// ── Nested DTOs ────────────────────────────────────────────────────────────────

/// <summary>Pre-mission briefing text and objectives preview.</summary>
public sealed class BriefingDefinition
{
    public string Text { get; set; } = string.Empty;
    public string[] ObjectivesPreview { get; set; } = [];
}

/// <summary>Starting conditions when a mission begins.</summary>
public sealed class StartConditionsDefinition
{
    public int[] PlayerSpawn { get; set; } = [0, 0];
    public string[] StartingUnits { get; set; } = [];
    public ResourceAmounts? StartingResources { get; set; }
}

/// <summary>Resource amounts keyed by type name.</summary>
public sealed class ResourceAmounts
{
    public float Energy { get; set; }
    public float Minerals { get; set; }
    public float Data { get; set; }
    public float Crew { get; set; }
}

/// <summary>Circular area used by reach-area objectives.</summary>
public sealed class AreaDefinition
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Radius { get; set; }
}

/// <summary>A single mission objective.</summary>
public sealed class ObjectiveDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? Condition { get; set; }
    public string Description { get; set; } = string.Empty;
    public float[]? Position { get; set; }
    public float Radius { get; set; }
    public AreaDefinition? Area { get; set; }
}

/// <summary>Groups of primary and secondary objectives.</summary>
public sealed class ObjectivesDefinition
{
    public ObjectiveDefinition[] Primary { get; set; } = [];
    public ObjectiveDefinition[] Secondary { get; set; } = [];
}

/// <summary>A condition that activates a trigger.</summary>
public sealed class TriggerConditionDefinition
{
    public string Type { get; set; } = string.Empty;
    public float Seconds { get; set; }
    public int? Count { get; set; }
    public string? Target { get; set; }
    public float[] Position { get; set; } = [];
    public float Radius { get; set; }
    public string? ResourceType { get; set; }
    public float Threshold { get; set; }
}

/// <summary>An action executed when a trigger fires.</summary>
public sealed class TriggerActionDefinition
{
    public string Type { get; set; } = string.Empty;
    public string[]? Units { get; set; }
    public int[]? Position { get; set; }
    public string? Tag { get; set; }
    public string? Speaker { get; set; }
    public string? Text { get; set; }
    public float[]? CameraTarget { get; set; }
}

/// <summary>A trigger that fires actions when its condition is met.</summary>
public sealed class TriggerDefinition
{
    public string Id { get; set; } = string.Empty;
    public TriggerConditionDefinition? Condition { get; set; }
    public TriggerActionDefinition[] Actions { get; set; } = [];
    public bool OneShot { get; set; } = true;
}

/// <summary>Victory or defeat condition.</summary>
public sealed class EndConditionDefinition
{
    public string Type { get; set; } = string.Empty;
}

/// <summary>Rewards granted on mission completion.</summary>
public sealed class RewardsDefinition
{
    public ResourceAmounts? Resources { get; set; }
    public int Xp { get; set; }
    public string[] Unlocks { get; set; } = [];
}

// ── Root DTO ──────────────────────────────────────────────────────────────────

/// <summary>
/// Top-level JSON definition for a mission.
/// Loaded from <c>GameData/Missions/*.json</c>.
/// </summary>
public sealed class MissionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public BriefingDefinition? Briefing { get; set; }
    public StartConditionsDefinition? StartConditions { get; set; }
    public ObjectivesDefinition? Objectives { get; set; }
    public TriggerDefinition[] Triggers { get; set; } = [];
    public EndConditionDefinition? Victory { get; set; }
    public EndConditionDefinition? Defeat { get; set; }
    public RewardsDefinition? Rewards { get; set; }
}