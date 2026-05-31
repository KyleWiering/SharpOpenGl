namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Runtime state for a single mission trigger.
/// Wraps <see cref="TriggerDefinition"/> with accumulated counters and a fired flag.
/// </summary>
public sealed class TriggerProgress
{
    /// <summary>The static definition loaded from JSON.</summary>
    public TriggerDefinition Definition { get; }

    /// <summary>
    /// <c>true</c> once the trigger's condition has been satisfied and its actions executed.
    /// For one-shot triggers the condition will no longer be evaluated once this is set.
    /// </summary>
    public bool HasFired { get; set; }

    /// <summary>
    /// Elapsed real-time seconds since the mission started.
    /// Used to evaluate <c>timer</c> trigger conditions.
    /// </summary>
    public float ElapsedSeconds { get; set; }

    /// <summary>
    /// Accumulated count for triggers with count-based conditions
    /// (e.g. <c>kill_count</c>).
    /// </summary>
    public int Count { get; set; }

    /// <param name="definition">Static trigger definition from JSON.</param>
    public TriggerProgress(TriggerDefinition definition)
    {
        Definition = definition;
    }
}
