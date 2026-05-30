namespace SharpOpenGl.Engine.Assets;

/// <summary>
/// Fallback asset definitions used when a requested asset is missing.
/// Any system loading game data should call these helpers before erroring.
/// </summary>
public static class DefaultAssets
{
    /// <summary>Key used to reference the default ship mesh.</summary>
    public const string DefaultShipKey = "default_ship";

    /// <summary>Key used to reference the default base mesh.</summary>
    public const string DefaultBaseKey = "default_base";

    /// <summary>Key used to reference the default projectile mesh.</summary>
    public const string DefaultProjectileKey = "default_projectile";

    /// <summary>
    /// If <paramref name="key"/> exists in the asset manager, return it.
    /// Otherwise log a warning and return <paramref name="fallbackKey"/>.
    /// </summary>
    public static string ResolveKey(AssetManager assets, string key, string fallbackKey)
    {
        if (assets.Exists(key))
            return key;

        Console.WriteLine($"[DefaultAssets] Asset '{key}' not found, using fallback '{fallbackKey}'.");
        return fallbackKey;
    }

    /// <summary>
    /// Try to load an asset; if not found or null, return the provided default value.
    /// </summary>
    public static T LoadOrDefault<T>(AssetManager assets, string key, T defaultValue)
        where T : class
    {
        return assets.Load<T>(key) ?? defaultValue;
    }
}
