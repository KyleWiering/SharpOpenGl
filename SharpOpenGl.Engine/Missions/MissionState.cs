namespace SharpOpenGl.Engine.Missions;

/// <summary>State of a single runtime objective.</summary>
public enum ObjectiveStatus
{
    Inactive,
    Active,
    Completed,
    Failed,
}

/// <summary>Overall mission outcome.</summary>
public enum MissionOutcome
{
    InProgress,
    Victory,
    Defeat,
}

/// <summary>
/// Runtime state for a single mission instance.
/// Created by <see cref="MissionLoader"/>; mutated by <see cref="ObjectiveSystem"/>
/// and <see cref="TriggerSystem"/>.
/// </summary>
public sealed class MissionState
{
    // ── Definition (immutable) ────────────────────────────────────────────────

    /// <summary>The loaded mission definition this state is tracking.</summary>
    public MissionDefinition Definition { get; }

    // ── Objective states ──────────────────────────────────────────────────────

    private readonly Dictionary<string, ObjectiveStatus> _objectiveStatuses = new();

    // ── Trigger tracking ──────────────────────────────────────────────────────

    /// <summary>Set of trigger ids that have already fired (prevents re-firing non-repeatable triggers).</summary>
    public HashSet<string> FiredTriggers { get; } = new();

    // ── Time ──────────────────────────────────────────────────────────────────

    /// <summary>Elapsed time in seconds since the mission started.</summary>
    public float ElapsedSeconds { get; private set; }

    // ── Outcome ───────────────────────────────────────────────────────────────

    /// <summary>Current outcome — changes to Victory or Defeat when conditions are met.</summary>
    public MissionOutcome Outcome { get; private set; } = MissionOutcome.InProgress;

    /// <summary>True once the mission has ended (victory or defeat).</summary>
    public bool IsEnded => Outcome != MissionOutcome.InProgress;

    // ── Kill count ────────────────────────────────────────────────────────────

    /// <summary>Total enemy kills so far (incremented by TriggerSystem on UnitDiedEvent).</summary>
    public int KillCount { get; private set; }

    // ── Construction ─────────────────────────────────────────────────────────

    public MissionState(MissionDefinition definition)
    {
        Definition = definition;

        // Primary objectives start Active; secondary start Inactive.
        foreach (var obj in definition.Objectives.Primary)
            _objectiveStatuses[obj.Id] = ObjectiveStatus.Active;

        foreach (var obj in definition.Objectives.Secondary)
            _objectiveStatuses[obj.Id] = ObjectiveStatus.Inactive;
    }

    // ── Objective API ─────────────────────────────────────────────────────────

    /// <summary>Returns the status of an objective by id, or <c>Inactive</c> if unknown.</summary>
    public ObjectiveStatus GetObjectiveStatus(string id) =>
        _objectiveStatuses.TryGetValue(id, out var s) ? s : ObjectiveStatus.Inactive;

    /// <summary>Set an objective status directly (used by ObjectiveSystem).</summary>
    public void SetObjectiveStatus(string id, ObjectiveStatus status)
    {
        if (_objectiveStatuses.ContainsKey(id))
            _objectiveStatuses[id] = status;
    }

    /// <summary>Returns true if all primary objectives are completed.</summary>
    public bool AllPrimaryComplete()
    {
        foreach (var obj in Definition.Objectives.Primary)
        {
            if (GetObjectiveStatus(obj.Id) != ObjectiveStatus.Completed)
                return false;
        }
        return true;
    }

    // ── Time ─────────────────────────────────────────────────────────────────

    /// <summary>Advance the mission clock.</summary>
    public void AdvanceTime(float deltaTime) => ElapsedSeconds += deltaTime;

    // ── Kill tracking ─────────────────────────────────────────────────────────

    /// <summary>Record a kill.</summary>
    public void RecordKill() => KillCount++;

    // ── Outcome ───────────────────────────────────────────────────────────────

    /// <summary>Set the final mission outcome.  Only allowed once.</summary>
    public void SetOutcome(MissionOutcome outcome)
    {
        if (!IsEnded)
            Outcome = outcome;
    }

    // ── Read-only objective enumeration ──────────────────────────────────────

    /// <summary>Enumerate all tracked objective ids and their statuses.</summary>
    public IEnumerable<(string Id, ObjectiveStatus Status)> AllObjectiveStatuses()
    {
        foreach (var kv in _objectiveStatuses)
            yield return (kv.Key, kv.Value);
    }
}
