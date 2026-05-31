namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Runtime progress tracking for a single mission objective.
/// Wraps the static <see cref="ObjectiveDefinition"/> with live state.
/// </summary>
public sealed class ObjectiveProgress
{
    /// <summary>The static definition loaded from JSON.</summary>
    public ObjectiveDefinition Definition { get; }

    /// <summary><c>true</c> when the objective's success condition has been met.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// For objectives that count towards a target (e.g. kill 5 units),
    /// tracks current progress value.
    /// </summary>
    public float CurrentCount { get; set; }

    /// <summary>
    /// For time-based objectives (e.g. survive for 60 s), tracks elapsed seconds.
    /// </summary>
    public float ElapsedTime { get; set; }

    /// <summary>
    /// Whether this is a primary (required) objective.
    /// Secondary objectives are optional bonuses.
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <param name="definition">Static definition loaded from JSON.</param>
    /// <param name="isPrimary">True for primary objectives, false for secondary.</param>
    public ObjectiveProgress(ObjectiveDefinition definition, bool isPrimary)
    {
        Definition = definition;
        IsPrimary  = isPrimary;
    }

    /// <summary>Human-readable description from the definition.</summary>
    public string Description => Definition.Description;

    /// <summary>Unique ID from the definition.</summary>
    public string Id => Definition.Id;
}
