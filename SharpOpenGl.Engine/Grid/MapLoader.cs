using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Loads a <see cref="GridSystem"/> from a <see cref="MapDefinition"/> JSON file.
/// </summary>
public sealed class MapLoader
{
    private readonly AssetManager? _assets;

    /// <param name="assets">
    /// Optional <see cref="AssetManager"/> used to load map files by key.
    /// Pass <c>null</c> when constructing the loader manually for testing.
    /// </param>
    public MapLoader(AssetManager? assets = null)
    {
        _assets = assets;
    }

    /// <summary>
    /// Load a map by asset key (e.g. <c>"Maps/sector_alpha"</c>) and return
    /// an initialised <see cref="GridSystem"/>, or <c>null</c> on failure.
    /// </summary>
    public GridSystem? Load(string key)
    {
        MapDefinition? def = _assets?.Load<MapDefinition>(key);
        if (def == null) return null;
        return FromDefinition(def);
    }

    /// <summary>Load a map from an already-parsed <see cref="MapDefinition"/>.</summary>
    public static GridSystem FromDefinition(MapDefinition def)
    {
        int width  = def.GridSize.Length > 0 ? def.GridSize[0] : 64;
        int height = def.GridSize.Length > 1 ? def.GridSize[1] : 64;
        float cellSize = def.CellSize > 0 ? def.CellSize : 1.0f;

        var grid = new GridSystem(width, height, cellSize);

        MapTerrainApplicator.ApplyRegions(grid, def.Terrain);

        return grid;
    }
}
