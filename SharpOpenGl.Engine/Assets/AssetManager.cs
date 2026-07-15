using System.Text.Json;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Assets;

/// <summary>
/// Manages loading and caching of game assets from the GameData directory.
/// Assets are identified by a string key (e.g. "Ships/hero_default").
/// Missing assets fall back to defaults rather than throwing.
/// </summary>
public class AssetManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _rootPath;
    private readonly IAssetTextSource _textSource;
    private readonly Dictionary<string, object> _cache = new();
    private readonly HashSet<string> _proceduralMeshes = new(StringComparer.OrdinalIgnoreCase);
    private MeshManifest? _meshManifest;

    public AssetManager(string rootPath, IAssetTextSource? textSource = null)
    {
        _rootPath = rootPath;
        _textSource = textSource ?? new FileAssetTextSource();
    }

    /// <summary>Root path for GameData (filesystem path or URL prefix).</summary>
    public string RootPath => _rootPath;

    public MeshManifest MeshManifest => _meshManifest ??= MeshManifest.Load(_rootPath);

    /// <summary>
    /// Load a JSON asset by relative key (without extension).
    /// Cached after first load. Returns <c>null</c> on failure.
    /// </summary>
    public T? Load<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out object? cached))
            return cached as T;

        string path = Path.Combine(_rootPath, key + ".json");
        string? json = _textSource.ReadAllText(path);
        if (json == null)
            return default;

        try
        {
            T? result = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (result != null)
                _cache[key] = result;
            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[AssetManager] Parse error in '{path}': {ex.Message}");
            return default;
        }
    }

    public void Invalidate(string key) => _cache.Remove(key);

    public void InvalidateAll() => _cache.Clear();

    public bool Exists(string key) =>
        _textSource.Exists(Path.Combine(_rootPath, key + ".json"));

    public void RegisterProceduralMesh(string meshKey)
    {
        if (string.IsNullOrWhiteSpace(meshKey)) return;
        _proceduralMeshes.Add(NormalizeMeshKey(meshKey));
    }

    public bool MeshExists(string meshKey)
    {
        if (string.IsNullOrWhiteSpace(meshKey)) return false;
        string normalized = NormalizeMeshKey(meshKey);
        if (_proceduralMeshes.Contains(normalized)) return true;
        return _textSource.Exists(ResolveMeshPath(normalized));
    }

    public string ResolveMeshPath(string meshKey)
        => MeshManifest.ResolveMeshPath(_rootPath, meshKey);

    /// <summary>Returns mission ids discovered under Missions/ (desktop only).</summary>
    public IEnumerable<string> ListMissionIds()
    {
        string missionsDir = Path.Combine(_rootPath, "Missions");
        if (!Directory.Exists(missionsDir))
            yield break;

        foreach (string file in Directory.GetFiles(missionsDir, "*.json"))
        {
            string filename = Path.GetFileNameWithoutExtension(file);
            if (!filename.StartsWith('_'))
                yield return filename;
        }
    }

    private static string NormalizeMeshKey(string meshKey)
    {
        string normalized = meshKey.Replace('\\', '/').Trim();
        if (normalized.StartsWith("meshes/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["meshes/".Length..];
        return normalized;
    }
}