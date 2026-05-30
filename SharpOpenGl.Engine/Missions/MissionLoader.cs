using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Loads <see cref="MissionState"/> instances from JSON files via an
/// <see cref="AssetManager"/> or directly from a <see cref="MissionDefinition"/>.
/// </summary>
public sealed class MissionLoader
{
    private readonly AssetManager? _assets;

    /// <param name="assets">
    /// Optional <see cref="AssetManager"/> used to load mission files by key.
    /// Pass <c>null</c> when constructing for tests or manual use.
    /// </param>
    public MissionLoader(AssetManager? assets = null)
    {
        _assets = assets;
    }

    // ── Load by key ───────────────────────────────────────────────────────────

    /// <summary>
    /// Load a mission by asset key (e.g. <c>"Missions/tutorial_01"</c>).
    /// Returns <c>null</c> if the file is missing or cannot be parsed.
    /// </summary>
    public MissionState? Load(string key)
    {
        MissionDefinition? def = _assets?.Load<MissionDefinition>(key);
        return def == null ? null : FromDefinition(def);
    }

    // ── Build from definition ─────────────────────────────────────────────────

    /// <summary>
    /// Build a fresh <see cref="MissionState"/> from an already-parsed definition.
    /// </summary>
    public static MissionState FromDefinition(MissionDefinition def)
    {
        List<ObjectiveRecord> objectives = BuildObjectiveRecords(def);
        List<TriggerRecord>   triggers   = BuildTriggerRecords(def);
        return new MissionState(def, objectives, triggers);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<ObjectiveRecord> BuildObjectiveRecords(MissionDefinition def)
    {
        var records = new List<ObjectiveRecord>();

        foreach (MissionObjectiveDefinition o in def.Objectives.Primary)
            records.Add(new ObjectiveRecord(o, isPrimary: true));

        foreach (MissionObjectiveDefinition o in def.Objectives.Secondary)
            records.Add(new ObjectiveRecord(o, isPrimary: false));

        return records;
    }

    private static List<TriggerRecord> BuildTriggerRecords(MissionDefinition def)
    {
        var records = new List<TriggerRecord>();

        foreach (MissionTriggerDefinition t in def.Triggers)
            records.Add(new TriggerRecord(t));

        return records;
    }
}
