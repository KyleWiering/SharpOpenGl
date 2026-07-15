using System.Text.Json;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Serialises and deserialises <see cref="SaveData"/> to/from JSON files.
/// Each save slot is stored as a separate file inside the save directory.
/// </summary>
public sealed class SaveManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string? _saveDirectory;
    private readonly Dictionary<string, SaveData>? _memorySlots;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create a manager that reads/writes saves under <paramref name="saveDirectory"/>.
    /// The directory is created on first write if it does not exist.
    /// </summary>
    public SaveManager(string saveDirectory)
    {
        _saveDirectory = saveDirectory;
    }

    private SaveManager(Dictionary<string, SaveData> memorySlots)
    {
        _memorySlots = memorySlots;
    }

    /// <summary>
    /// Create an in-memory save manager for platforms without writable filesystem (browser / WASM).
    /// </summary>
    public static SaveManager CreateInMemory() => new(new Dictionary<string, SaveData>());

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns <c>true</c> when a save file exists for <paramref name="slotName"/>.</summary>
    public bool SlotExists(string slotName) =>
        _memorySlots != null
            ? _memorySlots.ContainsKey(slotName)
            : File.Exists(SlotPath(slotName));

    /// <summary>
    /// Return metadata for every supported slot, including empty manual slots.
    /// Populated saves are ordered newest-first; empty slots follow.
    /// </summary>
    public IReadOnlyList<SaveSlotInfo> ListSaveSlots()
    {
        var populated = new List<SaveSlotInfo>();
        var empty = new List<SaveSlotInfo>();

        foreach (string slot in SaveSlotNames.AllSlots)
        {
            SaveData? data = Load(slot);
            if (data == null)
            {
                empty.Add(new SaveSlotInfo { SlotName = slot, HasData = false });
                continue;
            }

            populated.Add(new SaveSlotInfo
            {
                SlotName = slot,
                FilePath = _memorySlots != null ? string.Empty : SlotPath(slot),
                SavedAt = data.SavedAt ?? string.Empty,
                MissionId = data.MissionId ?? string.Empty,
                ElapsedMissionTime = data.ElapsedMissionTime,
                HasData = true,
            });
        }

        populated.Sort((a, b) => string.Compare(b.SavedAt, a.SavedAt, StringComparison.Ordinal));
        populated.AddRange(empty);
        return populated;
    }

    /// <summary>
    /// Return the paths of all <c>*.sav.json</c> files in the save directory,
    /// sorted by last-write time (newest first).
    /// Returns an empty array when the directory does not exist.
    /// </summary>
    public string[] ListSaveFiles()
    {
        if (_memorySlots != null)
        {
            return _memorySlots.Keys
                .OrderByDescending(slot => _memorySlots[slot].SavedAt, StringComparer.Ordinal)
                .Select(slot => $"{slot}.sav.json")
                .ToArray();
        }

        if (_saveDirectory == null || !Directory.Exists(_saveDirectory))
            return Array.Empty<string>();

        return Directory.GetFiles(_saveDirectory, "*.sav.json")
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .ToArray();
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Write <paramref name="data"/> to <c>{saveDirectory}/{slotName}.sav.json</c>.
    /// </summary>
    /// <param name="data">The save data to write.</param>
    /// <returns><c>true</c> on success; <c>false</c> if an I/O error occurred.</returns>
    public bool Save(SaveData data)
    {
        try
        {
            data.SavedAt = DateTime.UtcNow.ToString("o");
            if (_memorySlots != null)
            {
                _memorySlots[data.SlotName] = data;
                return true;
            }

            EnsureDirectory();
            string path = SlotPath(data.SlotName);
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Load the save file for <paramref name="slotName"/>.
    /// Returns <c>null</c> if the file does not exist or cannot be parsed.
    /// </summary>
    public SaveData? Load(string slotName)
    {
        if (_memorySlots != null)
            return _memorySlots.TryGetValue(slotName, out SaveData? data) ? data : null;

        string path = SlotPath(slotName);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Load the most recently written save file.
    /// Returns <c>null</c> when no saves exist.
    /// </summary>
    public SaveData? LoadLatest()
    {
        string[] files = ListSaveFiles();
        if (files.Length == 0) return null;

        string slotName = Path.GetFileNameWithoutExtension(
            Path.GetFileNameWithoutExtension(files[0]));
        return Load(slotName);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    /// <summary>Delete the save file for <paramref name="slotName"/>.</summary>
    /// <returns><c>true</c> if the file was deleted; <c>false</c> if it did not exist.</returns>
    public bool Delete(string slotName)
    {
        if (_memorySlots != null)
            return _memorySlots.Remove(slotName);

        string path = SlotPath(slotName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string SlotPath(string slotName) =>
        Path.Combine(_saveDirectory!, $"{slotName}.sav.json");

    private void EnsureDirectory()
    {
        if (_saveDirectory == null)
            return;

        if (!Directory.Exists(_saveDirectory))
            Directory.CreateDirectory(_saveDirectory);
    }
}
