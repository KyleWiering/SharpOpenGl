namespace SharpOpenGl.Engine.Assets;

/// <summary>Reads assets from the local filesystem (desktop).</summary>
public sealed class FileAssetTextSource : IAssetTextSource
{
    public bool Exists(string absolutePath) => File.Exists(absolutePath);

    public string? ReadAllText(string absolutePath)
    {
        if (!File.Exists(absolutePath))
            return null;

        try
        {
            return File.ReadAllText(absolutePath);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[Asset] Read failed '{absolutePath}': {ex.Message}");
            return null;
        }
    }
}