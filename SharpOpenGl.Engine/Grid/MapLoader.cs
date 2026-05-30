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

        ApplyTerrain(grid, def.Terrain);

        return grid;
    }

    // ── Terrain application ───────────────────────────────────────────────────

    private static void ApplyTerrain(GridSystem grid, MapTerrain? terrain)
    {
        if (terrain == null) return;

        TerrainType defaultType = ParseTerrain(terrain.Default);

        // Apply default to all cells first
        if (defaultType != TerrainType.Space)
        {
            foreach (GridCell cell in grid.AllCells(GridLayer.Surface))
                cell.Terrain = defaultType;
        }

        // Apply regions on top
        foreach (MapTerrainRegion region in terrain.Regions)
        {
            TerrainType regionType = ParseTerrain(region.Type);

            if (region.Cells != null)
            {
                foreach (int[] coord in region.Cells)
                {
                    if (coord.Length < 2) continue;
                    GridCell? cell = grid.GetCell(coord[0], coord[1], GridLayer.Surface);
                    if (cell != null) cell.Terrain = regionType;
                }
            }
            else if (region.Rect != null && region.Rect.Length >= 4)
            {
                int minX = region.Rect[0], minY = region.Rect[1];
                int maxX = region.Rect[2], maxY = region.Rect[3];
                for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                {
                    GridCell? cell = grid.GetCell(x, y, GridLayer.Surface);
                    if (cell != null) cell.Terrain = regionType;
                }
            }
        }
    }

    private static TerrainType ParseTerrain(string? name) => name?.ToLowerInvariant() switch
    {
        "asteroid_field" or "asteroidfield" => TerrainType.AsteroidField,
        "nebula"                            => TerrainType.Nebula,
        "debris"                            => TerrainType.Debris,
        "impassable"                        => TerrainType.Impassable,
        _                                   => TerrainType.Space,
    };
}
