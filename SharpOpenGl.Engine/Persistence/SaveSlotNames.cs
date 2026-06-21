namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Canonical save-slot identifiers. Five manual slots plus one autosave slot.
/// </summary>
public static class SaveSlotNames
{
    /// <summary>Quick-save / autosave slot written without confirmation.</summary>
    public const string Autosave = "Autosave";

    /// <summary>All manual slots in display order.</summary>
    public static readonly string[] ManualSlots =
    [
        "Slot1",
        "Slot2",
        "Slot3",
        "Slot4",
        "Slot5",
    ];

    /// <summary>Every slot the game supports (manual + autosave).</summary>
    public static readonly string[] AllSlots =
    [
        Autosave,
        ..ManualSlots,
    ];

    /// <summary>Human-readable label for UI.</summary>
    public static string DisplayName(string slotName) =>
        slotName switch
        {
            Autosave => "Quick Save",
            "Slot1" => "Slot 1",
            "Slot2" => "Slot 2",
            "Slot3" => "Slot 3",
            "Slot4" => "Slot 4",
            "Slot5" => "Slot 5",
            _ => slotName,
        };
}