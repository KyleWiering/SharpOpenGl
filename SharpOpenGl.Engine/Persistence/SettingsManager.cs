using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Reads and writes <see cref="GameSettings"/> to a JSON file on disk.
/// On platforms without a writable filesystem (e.g. browser / WASM) the
/// path may be left as <c>null</c> and in-memory defaults are used.
/// </summary>
public sealed class SettingsManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private readonly string? _filePath;
    private GameSettings _current;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create a manager backed by <paramref name="filePath"/>.
    /// Pass <c>null</c> to use in-memory defaults only (no file I/O).
    /// </summary>
    public SettingsManager(string? filePath = null)
    {
        _filePath = filePath;
        _current  = new GameSettings();
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>The active settings (always clamped to valid ranges).</summary>
    public GameSettings Current => _current;

    // ── Load / Save ───────────────────────────────────────────────────────────

    /// <summary>
    /// Load settings from <see cref="_filePath"/>.
    /// If the file does not exist, or <see cref="_filePath"/> is <c>null</c>,
    /// the manager retains the current (default) settings.
    /// </summary>
    /// <returns><c>true</c> if a file was read; <c>false</c> if defaults were kept.</returns>
    public bool Load()
    {
        if (_filePath is null || !File.Exists(_filePath))
            return false;

        try
        {
            string json = File.ReadAllText(_filePath);
            GameSettings? loaded = JsonSerializer.Deserialize<GameSettings>(json, _jsonOptions);
            if (loaded != null)
                _current = loaded.Clamped();
            return true;
        }
        catch
        {
            // Corrupt or unreadable file — fall back to defaults silently.
            return false;
        }
    }

    /// <summary>
    /// Persist the current settings to <see cref="_filePath"/>.
    /// No-op when <see cref="_filePath"/> is <c>null</c>.
    /// </summary>
    /// <returns><c>true</c> if the file was written successfully.</returns>
    public bool Save()
    {
        if (_filePath is null) return false;

        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (dir is { Length: > 0 } && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(_current, _jsonOptions);
            File.WriteAllText(_filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Replace the active settings, clamp values, and persist to disk.
    /// </summary>
    public bool Apply(GameSettings settings)
    {
        _current = settings.Clamped();
        return Save();
    }

    /// <summary>Reset in-memory settings to defaults and save.</summary>
    public bool ResetToDefaults()
    {
        _current = new GameSettings();
        return Save();
    }
}
