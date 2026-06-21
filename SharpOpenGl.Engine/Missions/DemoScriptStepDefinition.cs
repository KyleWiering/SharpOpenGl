namespace SharpOpenGl.Engine.Missions;

/// <summary>A single timed step in a mission <c>demoScript</c> playthrough.</summary>
public sealed class DemoScriptStepDefinition
{
    /// <summary>
    /// Step kind: <c>select_units</c>, <c>move_to</c>, <c>attack_move</c>,
    /// <c>attack_target</c>, <c>wait</c>, <c>wait_objective</c>, <c>camera_pan</c>,
    /// <c>build_unit</c>, <c>place_building</c>.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Duration for <c>wait</c> steps (seconds).</summary>
    public float Seconds { get; set; }

    /// <summary>Grid [x, y] position for move, attack-move, and camera pan steps.</summary>
    public float[]? Position { get; set; }

    /// <summary>
    /// Unit selection filter for <c>select_units</c>:
    /// <c>all</c>, <c>hero</c>, a definition id, or a mission entity tag.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>Mission entity tag for <c>attack_target</c>.</summary>
    public string? TargetTag { get; set; }

    /// <summary>Objective id for <c>wait_objective</c>.</summary>
    public string? ObjectiveId { get; set; }

    /// <summary>Unit definition id for <c>build_unit</c>.</summary>
    public string? UnitId { get; set; }

    /// <summary>Building tag or building type for <c>build_unit</c>.</summary>
    public string? BuildingTag { get; set; }

    /// <summary>Structure definition id for <c>place_building</c> (e.g. <c>shipyard_small</c>).</summary>
    public string? BuildingId { get; set; }

    /// <summary>Optional camera height override for <c>camera_pan</c>.</summary>
    public float Height { get; set; }
}