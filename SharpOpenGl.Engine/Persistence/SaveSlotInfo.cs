namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Lightweight metadata for one save slot, used by load/save UI screens.
/// </summary>
public sealed class SaveSlotInfo
{
    /// <summary>Slot identifier (e.g. "Slot1", "Autosave").</summary>
    public string SlotName { get; init; } = string.Empty;

    /// <summary>Absolute path to the save file, or empty when the slot is empty.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>ISO-8601 timestamp from the save file.</summary>
    public string SavedAt { get; init; } = string.Empty;

    /// <summary>Mission id stored in the save, or empty for sandbox/skirmish.</summary>
    public string MissionId { get; init; } = string.Empty;

    /// <summary>Elapsed mission seconds at save time.</summary>
    public float ElapsedMissionTime { get; init; }

    /// <summary><c>true</c> when a save file exists for this slot.</summary>
    public bool HasData { get; init; }
}