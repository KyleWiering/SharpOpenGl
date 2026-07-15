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

    /// <summary>
    /// Building definition ids to spawn completed at mission start near <see cref="PlayerSpawn"/>.
    /// When non-empty, the default free CC/shipyard base is not spawned.
    /// </summary>
    public string[] StartingBuildings { get; set; } = [];

    /// <summary>
    /// When true (default), spawn the free command center (+ optional shipyard) if
    /// <see cref="StartingBuildings"/> is empty. Set false for builder-only starts (e.g. training HQ).
    /// </summary>
    public bool SpawnDefaultBase { get; set; } = true;

    public ResourceAmounts? StartingResources { get; set; }

    /// <summary>
    /// When true, spend operations always succeed without draining the player pool
    /// (training / tutorial missions that must not soft-lock on economy).
    /// </summary>
    public bool UnlimitedResources { get; set; }
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

/// <summary>
/// A single mission objective.
/// <para>
/// Supported <see cref="Type"/> values include:
/// <c>destroy_target</c>, <c>survive_time</c>, <c>reach_area</c>,
/// <c>collect</c> (<c>Target</c> = <c>ResourceType:amount</c>, e.g. <c>Minerals:1000</c>),
/// <c>construct</c> (building: <c>definitionId:count</c> e.g. <c>defense_turret:5</c>;
/// unit: <c>unit:definitionId:count</c> e.g. <c>unit:fighter_basic:1</c>),
/// <c>condition</c>, <c>repair_target</c>.
/// </para>
/// </summary>
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

    /// <summary>Optional spawn HP fraction (0–1) applied after unit creation.</summary>
    public float? HealthPercent { get; set; }

    /// <summary>When true, spawned units receive <see cref="ECS.DisabledComponent"/> until repaired.</summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Combat faction for <c>spawn_units</c>. When omitted, defaults to <c>2</c>
    /// (hostile / P2 red via <c>PlayerColorPalette.GetTint(2)</c>). Use <c>1</c> for allied scripted spawns.
    /// </summary>
    public int? Faction { get; set; }
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

    /// <summary>Optional entity tag for tag-based defeat conditions (e.g. unit_destroyed).</summary>
    public string? Target { get; set; }
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

    /// <summary>Display name of the planet node on the galactic star map.</summary>
    public string PlanetName { get; set; } = string.Empty;

    /// <summary>Normalized [x, y] position on the star map (0–1).</summary>
    public float[] StarMapPosition { get; set; } = [];

    /// <summary>Planet accent colour as a hex string (e.g. <c>#4DA6FF</c>).</summary>
    public string PlanetColor { get; set; } = "#4DA6FF";

    /// <summary>Mission ID that must be completed before this system unlocks.</summary>
    public string? PrerequisiteMissionId { get; set; }

    /// <summary>Deterministic playthrough script for demo recordings.</summary>
    public DemoScriptStepDefinition[] DemoScript { get; set; } = [];
}