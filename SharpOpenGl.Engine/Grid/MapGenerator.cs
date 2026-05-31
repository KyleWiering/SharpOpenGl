namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Configuration for procedural map generation.
/// </summary>
public sealed class MapGeneratorConfig
{
    /// <summary>Number of columns.</summary>
    public int Width { get; set; } = 64;

    /// <summary>Number of rows.</summary>
    public int Height { get; set; } = 64;

    /// <summary>World-space size of each cell edge.</summary>
    public float CellSize { get; set; } = 1.0f;

    /// <summary>Fraction of cells to mark as asteroid field (0–1).</summary>
    public float AsteroidDensity { get; set; } = 0.10f;

    /// <summary>Fraction of cells to mark as nebula (0–1).</summary>
    public float NebulaDensity { get; set; } = 0.08f;

    /// <summary>Number of resource nodes to place.</summary>
    public int ResourceNodeCount { get; set; } = 4;

    /// <summary>Number of player spawn points.</summary>
    public int SpawnPointCount { get; set; } = 2;

    /// <summary>Minimum distance between resource nodes (in cells).</summary>
    public int MinResourceDistance { get; set; } = 8;
}

/// <summary>
/// Generates randomised maps procedurally from a seed value.
/// Produces maps with asteroid fields, nebulae, and open-space corridors.
/// </summary>
public sealed class MapGenerator
{
    private readonly Random _rng;
    private readonly int _seed;

    /// <param name="seed">
    /// Deterministic seed. The same seed always produces the same map.
    /// Pass <c>0</c> to use a time-based random seed.
    /// </param>
    public MapGenerator(int seed = 0)
    {
        _seed = seed;
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

    /// <summary>
    /// Generate a complete map from a configuration object and return a
    /// <see cref="MapDefinition"/> suitable for JSON serialization.
    /// </summary>
    public MapDefinition Generate(MapGeneratorConfig config)
    {
        var grid = Generate(config.Width, config.Height, config.CellSize,
                            config.AsteroidDensity, config.NebulaDensity);

        var spawns = PlaceSpawnPoints(grid, config.SpawnPointCount);
        var resources = PlaceResourceNodes(grid, config.ResourceNodeCount,
                                            config.MinResourceDistance);

        return BuildDefinition(grid, config, spawns, resources);
    }

    // ── Spawn point placement ─────────────────────────────────────────────────

    private List<(int X, int Y)> PlaceSpawnPoints(GridSystem grid, int count)
    {
        var spawns = new List<(int X, int Y)>();
        if (count <= 0) return spawns;

        // Place spawns in corners/edges maximally spread apart
        var candidates = new List<(int X, int Y)>
        {
            (2, 2),
            (grid.Width - 3, grid.Height - 3),
            (2, grid.Height - 3),
            (grid.Width - 3, 2),
        };

        for (int i = 0; i < count && i < candidates.Count; i++)
        {
            var (x, y) = candidates[i];
            // Ensure spawn area is clear
            ClearRect(grid, x - 2, y - 2, x + 2, y + 2);
            spawns.Add((x, y));
        }

        return spawns;
    }

    // ── Resource placement ────────────────────────────────────────────────────

    private List<(int X, int Y, string Type)> PlaceResourceNodes(
        GridSystem grid, int count, int minDistance)
    {
        var nodes = new List<(int X, int Y, string Type)>();
        string[] types = ["energy", "minerals", "data", "crew"];
        int attempts = 0;
        int maxAttempts = count * 50;

        while (nodes.Count < count && attempts < maxAttempts)
        {
            attempts++;
            int x = _rng.Next(4, grid.Width - 4);
            int y = _rng.Next(4, grid.Height - 4);

            GridCell? cell = grid.GetCell(x, y);
            if (cell == null || !cell.IsPassable) continue;

            // Check minimum distance from other resources
            bool tooClose = false;
            foreach (var existing in nodes)
            {
                int dx = x - existing.X, dy = y - existing.Y;
                if (dx * dx + dy * dy < minDistance * minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            string type = types[nodes.Count % types.Length];
            nodes.Add((x, y, type));
        }

        return nodes;
    }

    // ── MapDefinition builder ─────────────────────────────────────────────────

    private MapDefinition BuildDefinition(
        GridSystem grid, MapGeneratorConfig config,
        List<(int X, int Y)> spawns,
        List<(int X, int Y, string Type)> resources)
    {
        // Collect terrain regions
        var regions = new List<MapTerrainRegion>();
        var nebulaCells = new List<int[]>();
        var asteroidCells = new List<int[]>();
        var debrisCells = new List<int[]>();

        foreach (GridCell cell in grid.AllCells())
        {
            switch (cell.Terrain)
            {
                case TerrainType.Nebula:
                    nebulaCells.Add([cell.X, cell.Y]);
                    break;
                case TerrainType.AsteroidField:
                    asteroidCells.Add([cell.X, cell.Y]);
                    break;
                case TerrainType.Debris:
                    debrisCells.Add([cell.X, cell.Y]);
                    break;
            }
        }

        if (nebulaCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "nebula", Cells = nebulaCells.ToArray() });
        if (asteroidCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "asteroid_field", Cells = asteroidCells.ToArray() });
        if (debrisCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "debris", Cells = debrisCells.ToArray() });

        var spawnPoints = spawns.Select((s, i) => new MapSpawnPoint
        {
            Player = i + 1,
            Position = [s.X, s.Y],
            Layer = "surface"
        }).ToArray();

        var resourceNodes = resources.Select(r => new MapResourceNode
        {
            Type = r.Type,
            Position = [r.X, r.Y],
            Amount = r.Type switch
            {
                "energy" => 5000,
                "minerals" => 3000,
                "data" => 2000,
                "crew" => 1500,
                _ => 1000
            }
        }).ToArray();

        return new MapDefinition
        {
            Id = $"generated_{_seed}",
            DisplayName = $"Generated Sector (seed {_seed})",
            GridSize = [config.Width, config.Height],
            CellSize = config.CellSize,
            Layers = ["surface"],
            Terrain = new MapTerrain
            {
                Default = "space",
                Regions = regions.ToArray()
            },
            SpawnPoints = spawnPoints,
            ResourceNodes = resourceNodes
        };
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

        // Each corner clears exactly pad×pad cells (pad-1 is the inclusive upper bound).
        ClearRect(grid, 0,       0,       pad-1, pad-1);
        ClearRect(grid, w-pad,   0,       w-1,   pad-1);
        ClearRect(grid, 0,       h-pad,   pad-1, h-1);
        ClearRect(grid, w-pad,   h-pad,   w-1,   h-1);
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
