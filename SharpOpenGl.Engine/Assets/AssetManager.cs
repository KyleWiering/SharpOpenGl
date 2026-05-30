namespace SharpOpenGl.Engine.Assets;

/// <summary>
/// Manages loading and caching of game assets from the GameData directory.
/// Assets are identified by a string key (e.g. "Ships/hero_default").
/// Missing assets fall back to defaults rather than throwing.
/// </summary>
public class AssetManager
{
    private readonly string _rootPath;
    private readonly Dictionary<string, object> _cache = new();

    /// <summary>
    /// Create an AssetManager rooted at the given data directory.
    /// </summary>
    /// <param name="rootPath">Path to the GameData folder.</param>
    public AssetManager(string rootPath)
    {
        _rootPath = rootPath;
    }

    /// <summary>
    /// Load a JSON asset by relative key (without extension).
    /// Cached after first load. Returns <c>null</c> on failure.
    /// </summary>
    public T? Load<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out object? cached))
            return cached as T;

        string path = Path.Combine(_rootPath, key + ".json");
        T? result = JsonLoader.Load<T>(path);

        if (result != null)
            _cache[key] = result;

        return result;
    }

    /// <summary>Evict a single asset from the cache so it reloads next access.</summary>
    public void Invalidate(string key) => _cache.Remove(key);

    /// <summary>Clear the entire asset cache.</summary>
    public void InvalidateAll() => _cache.Clear();

    /// <summary>Returns true if the underlying JSON file exists on disk.</summary>
    public bool Exists(string key) =>
        File.Exists(Path.Combine(_rootPath, key + ".json"));
}
