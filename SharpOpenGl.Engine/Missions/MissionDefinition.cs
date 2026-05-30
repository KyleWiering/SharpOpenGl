using System.Text.Json.Serialization;

namespace SharpOpenGl.Engine.Missions;

// ── Top-level ─────────────────────────────────────────────────────────────────

/// <summary>
/// JSON-serialisable data model for a single mission file.
/// Mirrors the mission template defined in <c>GameData/Missions/_template.json</c>.
/// </summary>
public sealed class MissionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("map")]
    public string Map { get; set; } = string.Empty;

    [JsonPropertyName("briefing")]
    public MissionBriefing Briefing { get; set; } = new();

    [JsonPropertyName("startConditions")]
    public MissionStartConditions StartConditions { get; set; } = new();

    [JsonPropertyName("objectives")]
    public MissionObjectivesBlock Objectives { get; set; } = new();

    [JsonPropertyName("triggers")]
    public List<MissionTriggerDefinition> Triggers { get; set; } = new();

    [JsonPropertyName("victory")]
    public MissionEndCondition Victory { get; set; } = new();

    [JsonPropertyName("defeat")]
    public MissionEndCondition Defeat { get; set; } = new();

    [JsonPropertyName("rewards")]
    public MissionRewardDefinition Rewards { get; set; } = new();
}

// ── Briefing ──────────────────────────────────────────────────────────────────

/// <summary>Pre-mission briefing screen content.</summary>
public sealed class MissionBriefing
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("objectives_preview")]
    public List<string> ObjectivesPreview { get; set; } = new();
}

// ── Start conditions ──────────────────────────────────────────────────────────

/// <summary>Initial state when a mission begins.</summary>
public sealed class MissionStartConditions
{
    [JsonPropertyName("playerSpawn")]
    public int[] PlayerSpawn { get; set; } = [0, 0];

    [JsonPropertyName("startingUnits")]
    public List<string> StartingUnits { get; set; } = new();

    [JsonPropertyName("startingResources")]
    public MissionStartResources StartingResources { get; set; } = new();
}

/// <summary>Per-resource starting amounts.</summary>
public sealed class MissionStartResources
{
    [JsonPropertyName("energy")]
    public float Energy { get; set; }

    [JsonPropertyName("minerals")]
    public float Minerals { get; set; }

    [JsonPropertyName("data")]
    public float Data { get; set; }

    [JsonPropertyName("crew")]
    public float Crew { get; set; }
}

// ── Objectives ────────────────────────────────────────────────────────────────

/// <summary>Container for primary and secondary objectives.</summary>
public sealed class MissionObjectivesBlock
{
    [JsonPropertyName("primary")]
    public List<MissionObjectiveDefinition> Primary { get; set; } = new();

    [JsonPropertyName("secondary")]
    public List<MissionObjectiveDefinition> Secondary { get; set; } = new();
}

/// <summary>Definition of a single objective.</summary>
public sealed class MissionObjectiveDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>destroy_target | escort | survive_time | collect | reach_area | condition</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Entity tag for destroy_target / escort objectives.</summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>Seconds to survive (survive_time).</summary>
    [JsonPropertyName("seconds")]
    public float? Seconds { get; set; }

    /// <summary>Resource type to collect (collect).</summary>
    [JsonPropertyName("resource")]
    public string? Resource { get; set; }

    /// <summary>Amount required (collect).</summary>
    [JsonPropertyName("amount")]
    public float? Amount { get; set; }

    /// <summary>Grid position [x, y] to reach (reach_area).</summary>
    [JsonPropertyName("position")]
    public int[]? Position { get; set; }

    /// <summary>Radius in cells for reach_area check.</summary>
    [JsonPropertyName("radius")]
    public float? Radius { get; set; }

    /// <summary>Arbitrary condition expression string (condition type).</summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
}

// ── Triggers ──────────────────────────────────────────────────────────────────

/// <summary>A trigger that fires scripted actions when its condition is met.</summary>
public sealed class MissionTriggerDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public TriggerConditionDefinition Condition { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<TriggerActionDefinition> Actions { get; set; } = new();

    /// <summary>When true the trigger only fires once (default).</summary>
    [JsonPropertyName("once")]
    public bool Once { get; set; } = true;
}

/// <summary>
/// Condition that must be satisfied before a trigger fires.
/// </summary>
public sealed class TriggerConditionDefinition
{
    /// <summary>timer | kill_count | resource_threshold | area_enter</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Seconds elapsed for timer triggers.</summary>
    [JsonPropertyName("seconds")]
    public float? Seconds { get; set; }

    /// <summary>Number of kills needed (kill_count).</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>Resource type for resource_threshold.</summary>
    [JsonPropertyName("resource")]
    public string? Resource { get; set; }

    /// <summary>Threshold value for resource_threshold.</summary>
    [JsonPropertyName("threshold")]
    public float? Threshold { get; set; }

    /// <summary>Grid position [x, y] for area_enter.</summary>
    [JsonPropertyName("position")]
    public int[]? Position { get; set; }

    /// <summary>Radius in cells for area_enter.</summary>
    [JsonPropertyName("radius")]
    public float? Radius { get; set; }
}

/// <summary>A single action executed when a trigger fires.</summary>
public sealed class TriggerActionDefinition
{
    /// <summary>spawn_units | dialog | camera_pan | complete_objective | fail_mission</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("units")]
    public List<string> Units { get; set; } = new();

    [JsonPropertyName("position")]
    public int[]? Position { get; set; }

    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("objectiveId")]
    public string? ObjectiveId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

// ── End conditions ────────────────────────────────────────────────────────────

/// <summary>Victory or defeat condition.</summary>
public sealed class MissionEndCondition
{
    /// <summary>all_primary_complete | timer | hero_destroyed | custom</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("seconds")]
    public float? Seconds { get; set; }
}

// ── Rewards ───────────────────────────────────────────────────────────────────

/// <summary>Rewards granted on mission completion.</summary>
public sealed class MissionRewardDefinition
{
    [JsonPropertyName("resources")]
    public MissionStartResources Resources { get; set; } = new();

    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    [JsonPropertyName("unlocks")]
    public List<string> Unlocks { get; set; } = new();
}
