namespace SharpOpenGl.Engine.Missions;

// ── Mission status ────────────────────────────────────────────────────────────

/// <summary>Overall progress state of a loaded mission.</summary>
public enum MissionStatus
{
    /// <summary>Briefing screen is showing; game has not started.</summary>
    Briefing,
    /// <summary>Mission is actively running.</summary>
    InProgress,
    /// <summary>All primary objectives complete — player wins.</summary>
    Victory,
    /// <summary>A defeat condition was triggered — player loses.</summary>
    Defeat,
}

// ── Per-objective runtime record ──────────────────────────────────────────────

/// <summary>Runtime tracking record for one mission objective.</summary>
public sealed class ObjectiveRecord
{
    /// <summary>Source definition.</summary>
    public MissionObjectiveDefinition Definition { get; }

    /// <summary>Whether this objective belongs to the primary set.</summary>
    public bool IsPrimary { get; }

    /// <summary>Current completion state.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Accumulated progress value (used by survive_time and collect types).</summary>
    public float Progress { get; set; }

    public ObjectiveRecord(MissionObjectiveDefinition def, bool isPrimary)
    {
        Definition = def;
        IsPrimary  = isPrimary;
    }

    /// <summary>Mark this objective as completed (idempotent).</summary>
    public void Complete() => IsCompleted = true;

    /// <summary>Reset completion for mission replay.</summary>
    public void ResetCompletion() => IsCompleted = false;
}

// ── Per-trigger runtime record ────────────────────────────────────────────────

/// <summary>Runtime tracking record for one mission trigger.</summary>
public sealed class TriggerRecord
{
    /// <summary>Source definition.</summary>
    public MissionTriggerDefinition Definition { get; }

    /// <summary>Whether this trigger has already fired.</summary>
    public bool HasFired { get; private set; }

    /// <summary>Accumulated time for timer triggers.</summary>
    public float ElapsedSeconds { get; set; }

    /// <summary>Number of kills accumulated for kill_count triggers.</summary>
    public int KillCount { get; set; }

    public TriggerRecord(MissionTriggerDefinition def)
    {
        Definition = def;
    }

    /// <summary>Mark this trigger as fired (idempotent for once-only triggers).</summary>
    public void Fire() => HasFired = true;

    /// <summary>Reset the fired flag so a repeating trigger can fire again.</summary>
    public void Reset() => HasFired = false;
}

// ── Mission state ─────────────────────────────────────────────────────────────

/// <summary>
/// Tracks the complete runtime state of an active mission:
/// status, objective records, trigger records, elapsed time, and kill count.
/// Created by <see cref="MissionLoader"/>.
/// </summary>
public sealed class MissionState
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Loaded mission definition.</summary>
    public MissionDefinition Definition { get; }

    // ── Status ────────────────────────────────────────────────────────────────

    /// <summary>Current lifecycle status of the mission.</summary>
    public MissionStatus Status { get; private set; } = MissionStatus.Briefing;

    /// <summary>Reason string set when the mission is lost.</summary>
    public string DefeatReason { get; private set; } = string.Empty;

    // ── Time ──────────────────────────────────────────────────────────────────

    /// <summary>Total time elapsed since the mission entered <see cref="MissionStatus.InProgress"/>.</summary>
    public float ElapsedSeconds { get; private set; }

    // ── Combat tracking ───────────────────────────────────────────────────────

    /// <summary>Total player-side kills since mission start.</summary>
    public int TotalKills { get; private set; }

    // ── Records ───────────────────────────────────────────────────────────────

    /// <summary>All objective records (primary + secondary).</summary>
    public IReadOnlyList<ObjectiveRecord> Objectives { get; }

    /// <summary>All trigger records.</summary>
    public IReadOnlyList<TriggerRecord> Triggers { get; }

    // ── Derived queries ───────────────────────────────────────────────────────

    /// <summary>True when every primary objective is completed.</summary>
    public bool AllPrimaryComplete =>
        Objectives.All(o => !o.IsPrimary || o.IsCompleted);

    // ── Construction ──────────────────────────────────────────────────────────

    internal MissionState(
        MissionDefinition definition,
        List<ObjectiveRecord> objectives,
        List<TriggerRecord> triggers)
    {
        Definition = definition;
        Objectives = objectives.AsReadOnly();
        Triggers   = triggers.AsReadOnly();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Advance the mission from Briefing to InProgress.</summary>
    public void StartMission()
    {
        if (Status == MissionStatus.Briefing)
            Status = MissionStatus.InProgress;
    }

    /// <summary>Set victory status.</summary>
    public void SetVictory() => Status = MissionStatus.Victory;

    /// <summary>Set defeat status with an optional reason string.</summary>
    public void SetDefeat(string reason = "")
    {
        Status       = MissionStatus.Defeat;
        DefeatReason = reason;
    }

    // ── Frame update ──────────────────────────────────────────────────────────

    /// <summary>Advance the elapsed timer by <paramref name="deltaTime"/> seconds.</summary>
    public void Tick(float deltaTime)
    {
        if (Status == MissionStatus.InProgress)
            ElapsedSeconds += deltaTime;
    }

    // ── Kill tracking ─────────────────────────────────────────────────────────

    /// <summary>Record a player-side kill.</summary>
    public void RecordKill() => TotalKills++;

    // ── Objective helpers ─────────────────────────────────────────────────────

    /// <summary>Find an objective record by id, or null.</summary>
    public ObjectiveRecord? FindObjective(string id) =>
        Objectives.FirstOrDefault(o => o.Definition.Id == id);

    // ── Reset (replay) ────────────────────────────────────────────────────────

    /// <summary>
    /// Reset all runtime state so the mission can be replayed from scratch.
    /// </summary>
    public void Reset()
    {
        Status        = MissionStatus.Briefing;
        DefeatReason  = string.Empty;
        ElapsedSeconds = 0f;
        TotalKills    = 0;

        foreach (ObjectiveRecord obj in Objectives)
        {
            obj.Progress = 0f;
            // Use reflection-free approach via a sub-class trick is too complex;
            // expose a reset method on the record instead.
            obj.ResetCompletion();
        }

        foreach (TriggerRecord tr in Triggers)
        {
            tr.ElapsedSeconds = 0f;
            tr.KillCount = 0;
            tr.Reset();
        }
    }
}
