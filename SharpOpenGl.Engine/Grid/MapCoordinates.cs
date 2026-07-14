using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Converts mission JSON grid coordinates to world-space positions.
/// Matches the 200×200 grid centred on the origin used by the desktop renderer.
/// </summary>
public static class MapCoordinates
{
    /// <summary>Default grid extent (columns/rows) for the desktop map.</summary>
    public const int DefaultGridExtent = 200;

    /// <summary>Default world-space size of one grid cell.</summary>
    public const float DefaultCellSize = 10f;

    /// <summary>Resolve grid extent from map metadata (max of width/height).</summary>
    public static int ResolveGridExtent(MapDefinition map)
    {
        if (map.GridSize is { Length: >= 2 })
            return Math.Max(map.GridSize[0], map.GridSize[1]);

        if (map.GridSize is { Length: 1 })
            return map.GridSize[0];

        return DefaultGridExtent;
    }

    /// <summary>Resolve cell size from map metadata, falling back to default.</summary>
    public static float ResolveCellSize(MapDefinition map) =>
        map.CellSize > 0 ? map.CellSize : DefaultCellSize;

    /// <summary>
    /// Convert a grid cell index to the world-space centre of that cell on the XZ plane.
    /// </summary>
    public static Vector3 GridToWorld(int gridX, int gridY,
        int gridExtent = DefaultGridExtent, float cellSize = DefaultCellSize)
    {
        float half = gridExtent * cellSize * 0.5f;
        float worldX = gridX * cellSize + cellSize * 0.5f - half;
        float worldZ = gridY * cellSize + cellSize * 0.5f - half;
        return new Vector3(worldX, 0f, worldZ);
    }

    /// <summary>
    /// Convert fractional grid coordinates to world-space centre.
    /// </summary>
    public static Vector3 GridToWorld(float gridX, float gridY,
        int gridExtent = DefaultGridExtent, float cellSize = DefaultCellSize)
    {
        float half = gridExtent * cellSize * 0.5f;
        float worldX = gridX * cellSize + cellSize * 0.5f - half;
        float worldZ = gridY * cellSize + cellSize * 0.5f - half;
        return new Vector3(worldX, 0f, worldZ);
    }

    /// <summary>
    /// Parse a reach-area condition string (<c>gridX,gridY,radius</c>) into world centre + radius.
    /// </summary>
    public static bool TryParseReachArea(string? condition, out Vector3 center, out float radius,
        int gridExtent = DefaultGridExtent, float cellSize = DefaultCellSize)
    {
        center = Vector3.Zero;
        radius = 5f;

        if (string.IsNullOrEmpty(condition)) return false;

        var parts = condition.Split(',');
        if (parts.Length < 2) return false;

        if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float gridX)) return false;
        if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float gridY)) return false;

        if (parts.Length >= 3)
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out radius);

        center = GridToWorld(gridX, gridY, gridExtent, cellSize);
        return true;
    }
}