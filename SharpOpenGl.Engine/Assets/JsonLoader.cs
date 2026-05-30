using System.Text.Json;

namespace SharpOpenGl.Engine.Assets;

/// <summary>
/// Lightweight helper for loading and deserializing JSON files from disk.
/// </summary>
public static class JsonLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Load and deserialize a JSON file at <paramref name="path"/> into <typeparamref name="T"/>.
    /// Returns <c>null</c> if the file does not exist or cannot be parsed.
    /// </summary>
    public static T? Load<T>(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[JsonLoader] File not found: {path}");
            return default;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[JsonLoader] Parse error in '{path}': {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Load and deserialize a JSON file, throwing on error (strict mode).
    /// </summary>
    public static T LoadStrict<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Required asset not found: {path}", path);

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options)
               ?? throw new InvalidDataException($"Null result deserializing: {path}");
    }

    /// <summary>Serialize <paramref name="value"/> to a JSON file at <paramref name="path"/>.</summary>
    public static void Save<T>(string path, T value)
    {
        string json = JsonSerializer.Serialize(value,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
