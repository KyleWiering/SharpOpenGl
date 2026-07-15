namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Resolves mesh asset keys to on-disk OBJ paths via <see cref="MeshManifest"/>,
/// with procedural fallback when files are absent.
/// </summary>
public sealed class MeshAssetService
{
    private readonly string _gameDataRoot;
    private readonly MeshManifest _manifest;

    public MeshAssetService(string gameDataRoot, MeshManifest? manifest = null)
    {
        _gameDataRoot = gameDataRoot;
        _manifest = manifest ?? MeshManifest.Load(gameDataRoot);
    }

    public MeshManifest Manifest => _manifest;

    public string GameDataRoot => _gameDataRoot;

    public bool TryResolvePath(string meshKey, out string fullPath)
    {
        fullPath = _manifest.ResolveMeshPath(_gameDataRoot, meshKey);
        return File.Exists(fullPath);
    }

    public bool TryResolveShip(string raceId, string modelId, out string meshKey, out string fullPath)
    {
        meshKey = MeshManifest.ShipKey(raceId, modelId);
        return TryResolvePath(meshKey, out fullPath);
    }

    public bool TryResolveStation(string raceId, string modelId, out string meshKey, out string fullPath)
    {
        meshKey = MeshManifest.StationKey(raceId, modelId);
        return TryResolvePath(meshKey, out fullPath);
    }

    public ObjMeshData? TryLoadObj(string meshKey)
    {
        if (!TryResolvePath(meshKey, out string path))
            return null;
        return ObjMeshLoader.Parse(path);
    }

    public string ResolveShipMeshKey(string raceId, string definitionId) =>
        MeshManifest.ShipKey(raceId, definitionId);

    public IReadOnlyList<MeshManifestEntry> ListShipsForRace(string raceId) =>
        _manifest.Entries
            .Where(e => e.Category == "ship" && e.RaceId == raceId)
            .OrderBy(e => e.ModelId)
            .ToList();
}