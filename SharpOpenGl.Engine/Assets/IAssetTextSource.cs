namespace SharpOpenGl.Engine.Assets;

/// <summary>
/// Platform abstraction for reading asset files as text (disk or HTTP).
/// </summary>
public interface IAssetTextSource
{
    bool Exists(string absolutePath);
    string? ReadAllText(string absolutePath);
}