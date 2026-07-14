using System.Text.Json;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Persists which campaign missions the player has completed.
/// Stored as JSON under the user data directory.
/// </summary>
public sealed class CampaignProgressManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string? _filePath;
    private readonly HashSet<string> _completed = new(StringComparer.Ordinal);

    /// <summary>Create a manager backed by <paramref name="filePath"/> (may be null for in-memory only).</summary>
    public CampaignProgressManager(string? filePath = null)
    {
        _filePath = filePath;
    }

    /// <summary>Completed mission IDs loaded from disk or marked during this session.</summary>
    public IReadOnlySet<string> CompletedMissionIds => _completed;

    /// <summary>Load progress from disk. Returns false when defaults are kept.</summary>
    public bool Load()
    {
        if (_filePath is null || !File.Exists(_filePath))
            return false;

        try
        {
            string json = File.ReadAllText(_filePath);
            CampaignProgressData? data = JsonSerializer.Deserialize<CampaignProgressData>(json, JsonOptions);
            if (data?.CompletedMissionIds is not { Count: > 0 })
                return true;

            _completed.Clear();
            foreach (string id in data.CompletedMissionIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _completed.Add(id);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Mark a mission completed and persist immediately.</summary>
    public bool MarkCompleted(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId))
            return false;

        if (!_completed.Add(missionId))
            return true;

        return Save();
    }

    /// <summary>Write current progress to disk. No-op when no file path is configured.</summary>
    public bool Save()
    {
        if (_filePath is null)
            return false;

        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (dir is { Length: > 0 } && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var data = new CampaignProgressData
            {
                CompletedMissionIds = _completed.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            };
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(_filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class CampaignProgressData
    {
        public List<string> CompletedMissionIds { get; set; } = [];
    }
}