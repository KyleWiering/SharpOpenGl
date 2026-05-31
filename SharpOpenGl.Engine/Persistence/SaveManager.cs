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

    private readonly string _saveDirectory;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create a manager that reads/writes saves under <paramref name="saveDirectory"/>.
    /// The directory is created on first write if it does not exist.
    /// </summary>
    public SaveManager(string saveDirectory)
    {
        _saveDirectory = saveDirectory;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Return the paths of all <c>*.sav.json</c> files in the save directory,
    /// sorted by last-write time (newest first).
    /// Returns an empty array when the directory does not exist.
    /// </summary>
    public string[] ListSaveFiles()
    {
        if (!Directory.Exists(_saveDirectory))
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
            EnsureDirectory();
            data.SavedAt = DateTime.UtcNow.ToString("o");
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
        string path = SlotPath(slotName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string SlotPath(string slotName) =>
        Path.Combine(_saveDirectory, $"{slotName}.sav.json");

    private void EnsureDirectory()
    {
        if (!Directory.Exists(_saveDirectory))
            Directory.CreateDirectory(_saveDirectory);
    }
}
