using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Browser.Assets;

/// <summary>Loads GameData JSON over HTTP for Blazor WebAssembly.</summary>
public sealed class HttpAssetTextSource : IAssetTextSource
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public HttpAssetTextSource(HttpClient http) => _http = http;

    public bool Exists(string absolutePath)
    {
        string url = ToUrl(absolutePath);
        return _cache.ContainsKey(url) && _cache[url] != null;
    }

    public string? ReadAllText(string absolutePath)
    {
        string url = ToUrl(absolutePath);
        if (_cache.TryGetValue(url, out string? cached))
            return cached;

        // Synchronous blocking is avoided — callers must preload via PreloadAsync.
        return null;
    }

    public async Task PreloadAsync(IEnumerable<string> relativeJsonKeys, string gameDataRoot)
    {
        foreach (string key in relativeJsonKeys)
        {
            string path = Path.Combine(gameDataRoot, key + ".json").Replace('\\', '/');
            string url = ToUrl(path);
            if (_cache.ContainsKey(url)) continue;

            try
            {
                string? text = await _http.GetStringAsync(url);
                _cache[url] = text;
            }
            catch (HttpRequestException)
            {
                _cache[url] = null;
            }
        }
    }

    public async Task<bool> TryPreloadFileAsync(string absolutePath)
    {
        string url = ToUrl(absolutePath);
        if (_cache.TryGetValue(url, out string? existing))
            return existing != null;

        try
        {
            string text = await _http.GetStringAsync(url);
            _cache[url] = text;
            return true;
        }
        catch (HttpRequestException)
        {
            _cache[url] = null;
            return false;
        }
    }

    private static string ToUrl(string absolutePath)
    {
        string normalized = absolutePath.Replace('\\', '/');
        while (normalized.Contains("//"))
            normalized = normalized.Replace("//", "/");

        if (normalized.StartsWith("GameData/", StringComparison.OrdinalIgnoreCase))
            return normalized;

        int idx = normalized.IndexOf("GameData/", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? normalized[idx..] : normalized;
    }
}