namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// A trigger loaded from JSON: a condition that, when met, fires a list of scripted actions.
/// </summary>
public sealed class TriggerDefinition
{
    /// <summary>Unique id within the mission (e.g. "spawn_wave_1").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Whether the trigger fires once (default) or every time the condition is true.</summary>
    public bool Repeatable { get; set; } = false;

    /// <summary>Condition that must be satisfied for the trigger to fire.</summary>
    public TriggerCondition Condition { get; set; } = new();

    /// <summary>Ordered list of actions executed when the trigger fires.</summary>
    public List<ScriptedAction> Actions { get; set; } = new();
}

/// <summary>
/// Trigger condition descriptor.
/// </summary>
public sealed class TriggerCondition
{
    /// <summary>
    /// Condition type. Recognised values:
    /// "timer", "area_enter", "kill_count", "resource_threshold", "objective_complete", "always".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Elapsed-time threshold in seconds (used by "timer").</summary>
    public float? Seconds { get; set; }

    /// <summary>Grid rectangle [x1, y1, x2, y2] the player must enter (used by "area_enter").</summary>
    public int[]? Area { get; set; }

    /// <summary>Number of enemy kills required (used by "kill_count").</summary>
    public int? Count { get; set; }

    /// <summary>Resource type key (used by "resource_threshold").</summary>
    public string? ResourceType { get; set; }

    /// <summary>Resource threshold value (used by "resource_threshold").</summary>
    public float? Threshold { get; set; }

    /// <summary>Objective id that must be completed (used by "objective_complete").</summary>
    public string? ObjectiveId { get; set; }
}

/// <summary>
/// A single scripted action executed when a trigger fires.
/// </summary>
public sealed class ScriptedAction
{
    /// <summary>
    /// Action type. Recognised values:
    /// "spawn_units", "dialog", "camera_pan", "set_objective_active", "end_mission".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Unit definition keys to spawn (used by "spawn_units").</summary>
    public List<string> Units { get; set; } = new();

    /// <summary>Grid position [x, y] for the spawn or camera pan target (used by "spawn_units", "camera_pan").</summary>
    public int[]? Position { get; set; }

    /// <summary>Speaker label shown in the dialog box (used by "dialog").</summary>
    public string? Speaker { get; set; }

    /// <summary>Dialog line text (used by "dialog").</summary>
    public string? Text { get; set; }

    /// <summary>Objective id to activate (used by "set_objective_active").</summary>
    public string? ObjectiveId { get; set; }

    /// <summary>Mission outcome when ending early: "victory" or "defeat" (used by "end_mission").</summary>
    public string? Outcome { get; set; }
}
