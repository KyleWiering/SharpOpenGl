using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Loads mission definitions from JSON files in the GameData/Missions directory
/// and converts them into runtime <see cref="MissionDefinition"/> instances.
/// </summary>
public sealed class MissionLoader
{
    private readonly AssetManager _assets;
    private readonly Dictionary<string, MissionDefinition> _cache = new();

    public MissionLoader(AssetManager assets)
    {
        _assets = assets;
    }

    /// <summary>
    /// Load a mission by its ID (filename without extension under Missions/).
    /// Returns <c>null</c> if the file does not exist or cannot be parsed.
    /// </summary>
    public MissionDefinition? Load(string missionId)
    {
        if (_cache.TryGetValue(missionId, out var cached))
            return cached;

        var definition = _assets.Load<MissionDefinition>($"Missions/{missionId}");
        if (definition == null)
            return null;

        Validate(definition, missionId);
        _cache[missionId] = definition;
        return definition;
    }

    /// <summary>
    /// Load all mission definitions found in the Missions data folder.
    /// Skips files that start with underscore (templates).
    /// </summary>
    public IReadOnlyList<MissionDefinition> LoadAll(string missionsPath)
    {
        var results = new List<MissionDefinition>();

        if (!Directory.Exists(missionsPath))
            return results;

        foreach (string file in Directory.GetFiles(missionsPath, "*.json"))
        {
            string filename = Path.GetFileNameWithoutExtension(file);
            if (filename.StartsWith('_'))
                continue;

            var definition = Load(filename);
            if (definition != null)
                results.Add(definition);
        }

        return results;
    }

    /// <summary>
    /// Returns true if a mission JSON exists for the given ID.
    /// </summary>
    public bool Exists(string missionId) =>
        _assets.Exists($"Missions/{missionId}");

    /// <summary>Evict a cached mission so it reloads on next access.</summary>
    public void Invalidate(string missionId)
    {
        _cache.Remove(missionId);
        _assets.Invalidate($"Missions/{missionId}");
    }

    /// <summary>Clear all cached missions and invalidate underlying assets.</summary>
    public void InvalidateAll()
    {
        foreach (string key in _cache.Keys)
            _assets.Invalidate($"Missions/{key}");
        _cache.Clear();
    }

    /// <summary>
    /// Basic validation of a loaded mission definition.
    /// Logs warnings for missing required fields but does not throw.
    /// </summary>
    private static void Validate(MissionDefinition mission, string missionId)
    {
        if (string.IsNullOrWhiteSpace(mission.Id))
            mission.Id = missionId;

        if (string.IsNullOrWhiteSpace(mission.Map))
            Console.WriteLine($"[MissionLoader] Warning: Mission '{missionId}' has no map defined.");

        if (mission.Objectives == null || mission.Objectives.Primary.Length == 0)
            Console.WriteLine($"[MissionLoader] Warning: Mission '{missionId}' has no primary objectives.");

        if (mission.Victory == null)
            Console.WriteLine($"[MissionLoader] Warning: Mission '{missionId}' has no victory condition.");

        if (mission.Defeat == null)
            Console.WriteLine($"[MissionLoader] Warning: Mission '{missionId}' has no defeat condition.");
    }
}
