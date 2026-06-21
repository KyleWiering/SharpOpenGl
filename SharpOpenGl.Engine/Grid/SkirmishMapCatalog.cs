using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Engine.Grid;

/// <summary>Entry for a skirmish map shown in multiplayer setup.</summary>
public sealed record SkirmishMapEntry(string Id, string DisplayName, int PlayerCount);

/// <summary>Loads and validates skirmish maps from <c>GameData/Maps/</c>.</summary>
public sealed class SkirmishMapCatalog
{
    public static readonly SkirmishMapEntry[] FallbackMaps =
    [
        new("duel_frontier", "Duel Frontier", 2),
        new("four_corners", "Four Corners", 4),
        new("octagon_rim", "Octagon Rim", 8),
    ];

    private readonly AssetManager? _assets;
    private readonly Dictionary<string, MapDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SkirmishMapCatalog(AssetManager? assets = null)
    {
        _assets = assets;
    }

    /// <summary>Resolve skirmish maps from disk, or built-in fallbacks when unavailable.</summary>
    public static SkirmishMapEntry[] ResolveEntries(string? gameDataRoot = null)
    {
        string? mapsPath = ResolveMapsPath(gameDataRoot);
        if (mapsPath == null || !Directory.Exists(mapsPath))
            return FallbackMaps;

        var entries = new List<SkirmishMapEntry>();
        var catalog = new SkirmishMapCatalog(new AssetManager(Path.GetDirectoryName(mapsPath)!));

        foreach (string file in Directory.GetFiles(mapsPath, "*.json"))
        {
            string id = Path.GetFileNameWithoutExtension(file);
            if (id.StartsWith("_", StringComparison.Ordinal)) continue;

            var definition = catalog.Load(id);
            if (definition == null || !definition.Skirmish) continue;
            if (SkirmishMapLogic.Validate(definition).Count > 0) continue;

            entries.Add(new SkirmishMapEntry(
                definition.Id,
                definition.DisplayName,
                definition.PlayerCount));
        }

        if (entries.Count == 0)
            return FallbackMaps;

        return entries
            .OrderBy(e => e.PlayerCount)
            .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Load a skirmish map definition by id.</summary>
    public MapDefinition? Load(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId)) return null;

        if (_cache.TryGetValue(mapId, out var cached))
            return cached;

        MapDefinition? definition = _assets?.Load<MapDefinition>($"Maps/{mapId}");
        if (definition == null)
            return null;

        if (string.IsNullOrWhiteSpace(definition.Id))
            definition.Id = mapId;

        _cache[mapId] = definition;
        return definition;
    }

    /// <summary>Load all skirmish map definitions from a maps directory.</summary>
    public IReadOnlyList<MapDefinition> LoadAll(string mapsPath)
    {
        var results = new List<MapDefinition>();
        if (!Directory.Exists(mapsPath)) return results;

        foreach (string file in Directory.GetFiles(mapsPath, "*.json"))
        {
            string id = Path.GetFileNameWithoutExtension(file);
            if (id.StartsWith("_", StringComparison.Ordinal)) continue;

            var definition = Load(id);
            if (definition != null && definition.Skirmish)
                results.Add(definition);
        }

        return results;
    }

    private static string? ResolveMapsPath(string? gameDataRoot)
    {
        if (!string.IsNullOrWhiteSpace(gameDataRoot))
            return Path.Combine(gameDataRoot, "Maps");

        string fromBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", "Maps");
        if (Directory.Exists(fromBase)) return fromBase;

        string fromCwd = Path.Combine(Directory.GetCurrentDirectory(), "GameData", "Maps");
        return Directory.Exists(fromCwd) ? fromCwd : null;
    }
}