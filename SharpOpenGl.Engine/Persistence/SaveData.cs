namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Snapshot of a single entity's transform and health for save-file purposes.
/// </summary>
public sealed class EntitySaveRecord
{
    /// <summary>Logical entity ID.</summary>
    public int EntityId { get; set; }

    /// <summary>JSON-template ID used to recreate the entity (e.g. "hero_default").</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>World-space X position.</summary>
    public float X { get; set; }

    /// <summary>World-space Y position.</summary>
    public float Y { get; set; }

    /// <summary>Current health points.</summary>
    public float Health { get; set; }

    /// <summary>Current shield points.</summary>
    public float Shields { get; set; }

    /// <summary>Owning player ID (0 = neutral, 1 = player 1, etc.).</summary>
    public int PlayerId { get; set; }

    /// <summary>Any mission-registered tag assigned to this entity (may be empty).</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Combat stance for this entity (null if entity has no stance).</summary>
    public string? Stance { get; set; }
}

/// <summary>
/// Snapshot of one player's resources at save time.
/// </summary>
public sealed class PlayerResourceRecord
{
    /// <summary>Player ID.</summary>
    public int PlayerId { get; set; }

    /// <summary>Current energy.</summary>
    public float Energy { get; set; }

    /// <summary>Current minerals.</summary>
    public float Minerals { get; set; }

    /// <summary>Current data points.</summary>
    public float Data { get; set; }

    /// <summary>Current crew.</summary>
    public float Crew { get; set; }
}

/// <summary>
/// Top-level save-file data model.  Serialised to/from JSON by <see cref="SaveManager"/>.
/// </summary>
public sealed class SaveData
{
    /// <summary>Human-readable save slot name (e.g. "Slot 1" or an auto-save label).</summary>
    public string SlotName { get; set; } = "Autosave";

    /// <summary>ISO-8601 timestamp recorded when the save was created.</summary>
    public string SavedAt { get; set; } = string.Empty;

    /// <summary>Save-file format version for future migration support.</summary>
    public int Version { get; set; } = 1;

    /// <summary>ID of the mission currently in progress (empty when in free-play).</summary>
    public string MissionId { get; set; } = string.Empty;

    /// <summary>Total in-game seconds elapsed since the mission began.</summary>
    public float ElapsedMissionTime { get; set; }

    /// <summary>Current camera position X.</summary>
    public float CameraX { get; set; }

    /// <summary>Current camera position Y.</summary>
    public float CameraY { get; set; }

    /// <summary>Current camera zoom level.</summary>
    public float CameraZoom { get; set; } = 1f;

    /// <summary>All player resource snapshots.</summary>
    public List<PlayerResourceRecord> PlayerResources { get; set; } = new();

    /// <summary>All entity snapshots (units, buildings, NPCs).</summary>
    public List<EntitySaveRecord> Entities { get; set; } = new();

    /// <summary>IDs of completed objectives in the active mission.</summary>
    public List<string> CompletedObjectiveIds { get; set; } = new();

    /// <summary>IDs of triggers that have already fired.</summary>
    public List<string> FiredTriggerIds { get; set; } = new();

    /// <summary>Per-player fog-of-war state for persistence. Key is "playerId:x:y" → fog state ordinal.</summary>
    public Dictionary<string, int> FogStates { get; set; } = new();
}
