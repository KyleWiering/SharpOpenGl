namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Applies authored <see cref="MapTerrain"/> regions onto an existing <see cref="GridSystem"/>.
/// Map-local cell indices map 1:1 to gameplay grid cells (matches mission/skirmish JSON convention).
/// </summary>
public static class MapTerrainApplicator
{
    /// <summary>Apply terrain from <paramref name="map"/> onto <paramref name="grid"/>.</summary>
    /// <returns>Number of cells whose terrain was set.</returns>
    public static int ApplyToGrid(GridSystem grid, MapDefinition map) =>
        ApplyRegions(grid, map.Terrain);

    /// <summary>Apply terrain regions onto <paramref name="grid"/>.</summary>
    public static int ApplyRegions(GridSystem grid, MapTerrain? terrain)
    {
        if (terrain == null) return 0;

        int applied = 0;
        TerrainType defaultType = ParseTerrain(terrain.Default);

        if (defaultType != TerrainType.Space)
        {
            foreach (GridCell cell in grid.AllCells(GridLayer.Surface))
            {
                cell.Terrain = defaultType;
                applied++;
            }
        }

        if (terrain.Regions == null) return applied;

        foreach (MapTerrainRegion region in terrain.Regions)
        {
            TerrainType regionType = ParseTerrain(region.Type);
            applied += ApplyRegion(grid, region, regionType);
        }

        return applied;
    }

    private static int ApplyRegion(GridSystem grid, MapTerrainRegion region, TerrainType regionType)
    {
        int applied = 0;

        if (region.Cells != null)
        {
            foreach (int[] coord in region.Cells)
            {
                if (coord.Length < 2) continue;
                if (TrySetTerrain(grid, coord[0], coord[1], regionType))
                    applied++;
            }

            return applied;
        }

        if (region.Rect == null || region.Rect.Length < 4)
            return applied;

        int minX = region.Rect[0];
        int minY = region.Rect[1];
        int maxX = region.Rect[2];
        int maxY = region.Rect[3];

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        {
            if (TrySetTerrain(grid, x, y, regionType))
                applied++;
        }

        return applied;
    }

    private static bool TrySetTerrain(GridSystem grid, int x, int y, TerrainType terrain)
    {
        GridCell? cell = grid.GetCell(x, y, GridLayer.Surface);
        if (cell == null) return false;
        cell.Terrain = terrain;
        return true;
    }

    /// <summary>Parse terrain type strings from map JSON.</summary>
    public static TerrainType ParseTerrain(string? name) => name?.ToLowerInvariant() switch
    {
        "asteroid_field" or "asteroidfield" => TerrainType.AsteroidField,
        "nebula"                            => TerrainType.Nebula,
        "debris"                            => TerrainType.Debris,
        "ion_storm" or "ionstorm"           => TerrainType.IonStorm,
        "wormhole_remnant" or "wormhole"    => TerrainType.WormholeRemnant,
        "impassable"                        => TerrainType.Impassable,
        _                                   => TerrainType.Space,
    };
}