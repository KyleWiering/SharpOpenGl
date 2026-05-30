namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// A single mission objective loaded from JSON.
/// </summary>
public sealed class ObjectiveDefinition
{
    /// <summary>Unique id within the mission (e.g. "destroy_scout").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Objective type. Recognised values:
    /// "destroy_target", "escort", "survive_time", "collect_resources", "reach_area", "condition".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Human-readable text shown in the HUD.</summary>
    public string Description { get; set; } = string.Empty;

    // ── Type-specific fields ────────────────────────────────────────────────

    /// <summary>Entity id of the target (used by "destroy_target", "escort").</summary>
    public string? Target { get; set; }

    /// <summary>Duration in seconds (used by "survive_time").</summary>
    public float? DurationSeconds { get; set; }

    /// <summary>Resource type key and required amount (used by "collect_resources").</summary>
    public string? ResourceType { get; set; }
    public float? RequiredAmount { get; set; }

    /// <summary>Grid rectangle [x1, y1, x2, y2] (used by "reach_area").</summary>
    public int[]? Area { get; set; }

    /// <summary>Freeform condition expression string (used by "condition").</summary>
    public string? Condition { get; set; }
}
