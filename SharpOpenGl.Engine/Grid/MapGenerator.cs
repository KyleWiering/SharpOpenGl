namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Generates randomised maps procedurally from a seed value.
/// Produces maps with asteroid fields, nebulae, and open-space corridors.
/// </summary>
public sealed class MapGenerator
{
    private readonly Random _rng;

    /// <param name="seed">
    /// Deterministic seed. The same seed always produces the same map.
    /// Pass <c>0</c> to use a time-based random seed.
    /// </param>
    public MapGenerator(int seed = 0)
    {
        _rng = seed == 0 ? new Random() : new Random(seed);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generate a map of the given size.
    /// </summary>
    /// <param name="width">Number of columns.</param>
    /// <param name="height">Number of rows.</param>
    /// <param name="cellSize">World-space size of each cell.</param>
    /// <param name="asteroidDensity">Fraction of cells to mark as asteroid field (0–1).</param>
    /// <param name="nebulaDensity">Fraction of cells to mark as nebula (0–1).</param>
    /// <returns>A new <see cref="GridSystem"/> with terrain applied.</returns>
    public GridSystem Generate(int width, int height, float cellSize = 1.0f,
                               float asteroidDensity = 0.10f,
                               float nebulaDensity   = 0.08f)
    {
        var grid = new GridSystem(width, height, cellSize);

        PlaceNebulae(grid, nebulaDensity);
        PlaceAsteroidFields(grid, asteroidDensity);
        EnsureClearCorners(grid);

        return grid;
    }

    // ── Generation helpers ────────────────────────────────────────────────────

    /// <summary>Scatter rectangular nebula patches across the map.</summary>
    private void PlaceNebulae(GridSystem grid, float density)
    {
        int targetCells = (int)(grid.Width * grid.Height * density);
        int placed = 0;

        while (placed < targetCells)
        {
            int ox = _rng.Next(grid.Width);
            int oy = _rng.Next(grid.Height);
            int w  = _rng.Next(3, 10);
            int h  = _rng.Next(3, 10);

            for (int x = ox; x < ox + w && x < grid.Width; x++)
            for (int y = oy; y < oy + h && y < grid.Height; y++)
            {
                GridCell? cell = grid.GetCell(x, y);
                if (cell != null && cell.Terrain == TerrainType.Space)
                {
                    cell.Terrain = TerrainType.Nebula;
                    placed++;
                }
            }
        }
    }

    /// <summary>Scatter smaller, denser asteroid clusters.</summary>
    private void PlaceAsteroidFields(GridSystem grid, float density)
    {
        int targetCells = (int)(grid.Width * grid.Height * density);
        int placed = 0;

        while (placed < targetCells)
        {
            int ox = _rng.Next(grid.Width);
            int oy = _rng.Next(grid.Height);
            int w  = _rng.Next(1, 5);
            int h  = _rng.Next(1, 5);

            for (int x = ox; x < ox + w && x < grid.Width; x++)
            for (int y = oy; y < oy + h && y < grid.Height; y++)
            {
                GridCell? cell = grid.GetCell(x, y);
                if (cell != null && cell.Terrain == TerrainType.Space)
                {
                    cell.Terrain = TerrainType.AsteroidField;
                    placed++;
                }
            }
        }
    }

    /// <summary>
    /// Clear the 3×3 corners so player spawns always have open space around them.
    /// </summary>
    private static void EnsureClearCorners(GridSystem grid)
    {
        int w = grid.Width, h = grid.Height;
        int pad = Math.Min(3, Math.Min(w, h));

        ClearRect(grid, 0,     0,     pad, pad);
        ClearRect(grid, w-pad, 0,     w-1, pad);
        ClearRect(grid, 0,     h-pad, pad, h-1);
        ClearRect(grid, w-pad, h-pad, w-1, h-1);
    }

    private static void ClearRect(GridSystem grid, int x0, int y0, int x1, int y1)
    {
        for (int x = x0; x <= x1 && x < grid.Width; x++)
        for (int y = y0; y <= y1 && y < grid.Height; y++)
        {
            GridCell? cell = grid.GetCell(x, y);
            if (cell != null) cell.Terrain = TerrainType.Space;
        }
    }
}
