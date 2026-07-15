using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Runtime state for the currently running mission.
/// Created by <see cref="MissionController"/> when a mission starts and consumed
/// by <see cref="ObjectiveSystem"/> and <see cref="TriggerSystem"/> each frame.
/// </summary>
public sealed class MissionState
{
    // ── Identification ────────────────────────────────────────────────────────

    /// <summary>The loaded definition that this state was created from.</summary>
    public MissionDefinition Definition { get; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Current lifecycle phase of the mission.</summary>
    public MissionPhase Phase { get; set; } = MissionPhase.None;

    /// <summary>Total seconds elapsed since <see cref="MissionPhase.InProgress"/> began.</summary>
    public float ElapsedTime { get; set; }

    /// <summary>Reason string set when the mission enters <see cref="MissionPhase.Defeat"/>.</summary>
    public string DefeatReason { get; set; } = string.Empty;

    // ── Objective tracking ────────────────────────────────────────────────────

    /// <summary>All primary objectives and their live progress.</summary>
    public IReadOnlyList<ObjectiveProgress> PrimaryObjectives { get; }

    /// <summary>All secondary objectives and their live progress.</summary>
    public IReadOnlyList<ObjectiveProgress> SecondaryObjectives { get; }

    /// <summary>Convenience accessor: both primary and secondary.</summary>
    public IEnumerable<ObjectiveProgress> AllObjectives =>
        PrimaryObjectives.Concat(SecondaryObjectives);

    /// <summary>Returns <c>true</c> when every primary objective is complete.</summary>
    public bool AllPrimaryComplete =>
        PrimaryObjectives.Count > 0 && PrimaryObjectives.All(o => o.IsCompleted);

    // ── Trigger tracking ──────────────────────────────────────────────────────

    /// <summary>Runtime state for each trigger defined in the mission.</summary>
    public IReadOnlyList<TriggerProgress> Triggers { get; }

    // ── Entity tag registry ───────────────────────────────────────────────────

    /// <summary>
    /// Maps named entity tags (from mission JSON) to live <see cref="Entity"/> handles.
    /// Populated at mission start and updated as scripted events spawn entities.
    /// </summary>
    public Dictionary<string, Entity> EntityTags { get; } = new();

    /// <summary>Maps tags to all entities sharing that tag (for destroy-all objectives).</summary>
    public Dictionary<string, HashSet<Entity>> EntityGroups { get; } = new();

    /// <summary>Combat and production counters for victory overlay stats (P08-D09).</summary>
    public MissionRunStats RunStats { get; } = new();

    // ── Construction ──────────────────────────────────────────────────────────

    /// <param name="definition">The loaded mission definition.</param>
    public MissionState(MissionDefinition definition)
    {
        Definition = definition;

        var primary   = new List<ObjectiveProgress>();
        var secondary = new List<ObjectiveProgress>();

        if (definition.Objectives != null)
        {
            foreach (var objDef in definition.Objectives.Primary)
            {
                NormalizeObjective(objDef);
                primary.Add(new ObjectiveProgress(objDef, isPrimary: true));
            }

            foreach (var objDef in definition.Objectives.Secondary)
            {
                NormalizeObjective(objDef);
                secondary.Add(new ObjectiveProgress(objDef, isPrimary: false));
            }
        }

        PrimaryObjectives   = primary;
        SecondaryObjectives = secondary;

        var triggers = new List<TriggerProgress>();
        foreach (var trigDef in definition.Triggers)
            triggers.Add(new TriggerProgress(trigDef));
        Triggers = triggers;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Find a primary or secondary objective progress entry by its ID.
    /// Returns <c>null</c> if not found.
    /// </summary>
    public ObjectiveProgress? FindObjective(string id) =>
        AllObjectives.FirstOrDefault(o => o.Id == id);

    /// <summary>
    /// Find a trigger progress entry by its ID.
    /// Returns <c>null</c> if not found.
    /// </summary>
    public TriggerProgress? FindTrigger(string id) =>
        Triggers.FirstOrDefault(t => t.Definition.Id == id);

    /// <summary>Register an entity under a mission tag (supports groups).</summary>
    public void RegisterEntityTag(string tag, Entity entity)
    {
        EntityTags[tag] = entity;
        if (!EntityGroups.TryGetValue(tag, out HashSet<Entity>? group))
        {
            group = new HashSet<Entity>();
            EntityGroups[tag] = group;
        }
        group.Add(entity);
    }

    /// <summary>
    /// Convert JSON position/area fields into the runtime condition string
    /// expected by <see cref="ObjectiveSystem"/>.
    /// </summary>
    public static void NormalizeObjective(ObjectiveDefinition def)
    {
        if (def.Type != "reach_area" || !string.IsNullOrEmpty(def.Condition))
            return;

        if (def.Area != null)
        {
            def.Condition = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2}", def.Area.X, def.Area.Y, def.Area.Radius);
            return;
        }

        if (def.Position != null && def.Position.Length >= 2)
        {
            float radius = def.Radius > 0f ? def.Radius : 5f;
            def.Condition = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2}", def.Position[0], def.Position[1], radius);
        }
    }
}