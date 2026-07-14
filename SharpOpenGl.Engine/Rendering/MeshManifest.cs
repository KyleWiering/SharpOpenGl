using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Manifest entry for a disk-backed mesh asset.</summary>
public sealed class MeshManifestEntry
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("raceId")]
    public string? RaceId { get; set; }

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = string.Empty;
}

/// <summary>
/// Loads and queries <c>GameData/Config/mesh_manifest.json</c>.
/// </summary>
public sealed class MeshManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, MeshManifestEntry> _byKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MeshManifestEntry> _byRaceModel = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MeshManifestEntry> Entries { get; }

    public MeshManifest(IEnumerable<MeshManifestEntry> entries)
    {
        var list = entries.ToList();
        Entries = list;
        foreach (var entry in list)
        {
            _byKey[NormalizeKey(entry.Key)] = entry;
            if (!string.IsNullOrWhiteSpace(entry.RaceId))
                _byRaceModel[CompositeKey(entry.Category, entry.RaceId, entry.ModelId)] = entry;
        }
    }

    public static MeshManifest Load(string gameDataRoot)
    {
        string path = Path.Combine(gameDataRoot, "Config", "mesh_manifest.json");
        if (!File.Exists(path))
            return new MeshManifest([]);

        string json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("entries", out JsonElement entriesEl))
        {
            var entries = JsonSerializer.Deserialize<List<MeshManifestEntry>>(entriesEl.GetRawText(), JsonOptions) ?? [];
            return new MeshManifest(entries);
        }

        var flat = JsonSerializer.Deserialize<List<MeshManifestEntry>>(json, JsonOptions) ?? [];
        return new MeshManifest(flat);
    }

    public static void Save(string gameDataRoot, IEnumerable<MeshManifestEntry> entries)
    {
        string path = Path.Combine(gameDataRoot, "Config", "mesh_manifest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var wrapper = new { version = 1, entries = entries.OrderBy(e => e.Key).ToList() };
        File.WriteAllText(path, JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool TryGetByKey(string key, out MeshManifestEntry? entry) =>
        _byKey.TryGetValue(NormalizeKey(key), out entry);

    public bool TryResolve(string category, string? raceId, string modelId, out MeshManifestEntry? entry)
    {
        entry = null;
        if (!string.IsNullOrWhiteSpace(raceId) &&
            _byRaceModel.TryGetValue(CompositeKey(category, raceId, modelId), out entry))
            return true;

        string flatKey = $"meshes/{modelId}.obj";
        return _byKey.TryGetValue(flatKey, out entry);
    }

    public string ResolveMeshPath(string gameDataRoot, string key)
    {
        if (TryResolveEntry(key, out MeshManifestEntry? entry))
            return Path.Combine(gameDataRoot, "Meshes", entry!.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        string normalized = StripMeshesPrefix(key);
        if (!normalized.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            normalized += ".obj";
        normalized = ApplyMeshFolderCasing(normalized);
        return Path.Combine(gameDataRoot, "Meshes", normalized.Replace('/', Path.DirectorySeparatorChar));
    }

    private bool TryResolveEntry(string key, out MeshManifestEntry? entry)
    {
        if (TryGetByKey(key, out entry))
            return true;

        string stripped = StripMeshesPrefix(key);
        if (!stripped.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            stripped += ".obj";
        return TryGetByKey($"meshes/{stripped}", out entry);
    }

    private static string StripMeshesPrefix(string key)
    {
        string normalized = key.Replace('\\', '/').Trim();
        if (normalized.StartsWith("meshes/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["meshes/".Length..];
        return normalized;
    }

    private static string ApplyMeshFolderCasing(string relativePath)
    {
        int slash = relativePath.IndexOf('/');
        if (slash < 0)
            return relativePath;

        string folder = relativePath[..slash];
        string rest = relativePath[slash..];
        string cased = folder.ToLowerInvariant() switch
        {
            "ships" => "Ships",
            "designs" => "Designs",
            "stations" => "Stations",
            "environment" => "Environment",
            "projectiles" => "Projectiles",
            "effects" => "Effects",
            "units" => "Units",
            _ => folder,
        };
        return cased + rest;
    }

    public static string ShipKey(string raceId, string modelId) =>
        $"meshes/ships/{raceId}/{modelId}.obj";

    public static string DesignKey(string raceId, string designId) =>
        $"meshes/designs/{raceId}/{designId}.obj";

    public static string StationKey(string raceId, string modelId) =>
        $"meshes/stations/{raceId}/{modelId}.obj";

    public static string EnvironmentKey(string modelId) =>
        $"meshes/environment/{modelId}.obj";

    public static string ProjectileKey(string modelId) =>
        $"meshes/projectiles/{modelId}.obj";

    public static string EffectKey(string modelId) =>
        $"meshes/effects/{modelId}.obj";

    private static string NormalizeKey(string key)
    {
        string normalized = key.Replace('\\', '/').Trim();
        if (!normalized.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            normalized += ".obj";
        return normalized;
    }

    private static string CompositeKey(string category, string raceId, string modelId) =>
        $"{category}:{raceId}:{modelId}".ToLowerInvariant();
}