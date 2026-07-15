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

    /// <summary>Fraction of cells to mark as debris (0–1).</summary>
    public float DebrisDensity { get; set; } = 0.05f;

    /// <summary>Fraction of cells to mark as ion storm (0–1).</summary>
    public float IonStormDensity { get; set; } = 0.04f;

    /// <summary>Fraction of cells to mark as wormhole remnant (0–1).</summary>
    public float WormholeRemnantDensity { get; set; } = 0.02f;

    /// <summary>Number of resource nodes to place.</summary>
    public int ResourceNodeCount { get; set; } = 4;

    /// <summary>Number of player spawn points.</summary>
    public int SpawnPointCount { get; set; } = 2;

    /// <summary>Minimum distance between resource nodes (in cells).</summary>
    public int MinResourceDistance { get; set; } = 8;

    /// <summary>Number of harvestable planets to scatter (mineable map features).</summary>
    public int HarvestablePlanetCount { get; set; } = 1;

    /// <summary>Number of neutral waypoint planets to scatter.</summary>
    public int NeutralPlanetCount { get; set; } = 1;

    /// <summary>Minimum distance in cells between any economy feature (nodes, planets, spawns).</summary>
    public int MinFeatureDistance { get; set; } = 10;

    /// <summary>When true, place scenery markers at asteroid/nebula terrain region centroids.</summary>
    public bool ScatterSceneryFromTerrain { get; set; } = true;
}

/// <summary>
/// Generates randomised maps procedurally from a seed value.
/// Produces maps with asteroid fields, nebulae, and open-space corridors.
/// </summary>
public sealed class MapGenerator
{
    private readonly Random _rng;
    private readonly int _seed;

    private static readonly string[] ResourceTypes = ["energy", "minerals", "data", "crew"];

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
                               float nebulaDensity   = 0.08f,
                               float debrisDensity   = 0.05f,
                               float ionStormDensity = 0.04f,
                               float wormholeDensity = 0.02f)
    {
        var grid = new GridSystem(width, height, cellSize);

        PlaceNebulae(grid, nebulaDensity);
        PlaceAsteroidFields(grid, asteroidDensity);
        ScatterDebris(grid, debrisDensity);
        ScatterIonStorms(grid, ionStormDensity);
        ScatterWormholeRemnants(grid, wormholeDensity);
        EnsureClearCorners(grid);

        return grid;
    }

    /// <summary>
    /// Generate a single sandbox chunk (64×64 cells) with light terrain and economy scatter.
    /// Deterministic per <paramref name="worldSeed"/> and chunk indices via <see cref="SandboxChunkCoords.ChunkSeed"/>.
    /// </summary>
    /// <param name="chunkX">Chunk column index.</param>
    /// <param name="chunkY">Chunk row index.</param>
    /// <param name="worldSeed">Session world seed.</param>
    /// <param name="cellSize">World-space cell edge length.</param>
    public MapDefinition GenerateChunk(int chunkX, int chunkY, int worldSeed, float cellSize = 10f)
    {
        int chunkSeed = SandboxChunkCoords.ChunkSeed(worldSeed, chunkX, chunkY);
        var chunkRng = new Random(chunkSeed);

        int resourceCount = chunkRng.Next(2, 5);
        int harvestablePlanets = chunkRng.Next(0, 2);
        (float asteroid, float nebula, float debris, float ionStorm, float wormhole) =
            ResolveChunkTerrainDensities(chunkRng, chunkX, chunkY);

        var chunkGen = new MapGenerator(chunkSeed);
        var config = new MapGeneratorConfig
        {
            Width = SandboxChunkCoords.ChunkCells,
            Height = SandboxChunkCoords.ChunkCells,
            CellSize = cellSize,
            ResourceNodeCount = resourceCount,
            HarvestablePlanetCount = harvestablePlanets,
            NeutralPlanetCount = 0,
            SpawnPointCount = 0,
            AsteroidDensity = asteroid,
            NebulaDensity = nebula,
            DebrisDensity = debris,
            IonStormDensity = ionStorm,
            WormholeRemnantDensity = wormhole,
            MinFeatureDistance = 8,
            MinResourceDistance = 8,
            ScatterSceneryFromTerrain = true,
        };

        MapDefinition def = chunkGen.Generate(config);
        def.Id = $"chunk_{worldSeed}_{chunkX}_{chunkY}";
        def.DisplayName = $"Sector ({chunkX}, {chunkY})";
        return def;
    }

    /// <summary>
    /// Generate a complete map from a configuration object and return a
    /// <see cref="MapDefinition"/> suitable for JSON serialization.
    /// </summary>
    public MapDefinition Generate(MapGeneratorConfig config)
    {
        var grid = Generate(config.Width, config.Height, config.CellSize,
                            config.AsteroidDensity, config.NebulaDensity,
                            config.DebrisDensity, config.IonStormDensity, config.WormholeRemnantDensity);

        var spawns = PlaceSpawnPoints(grid, config.SpawnPointCount);
        var occupied = BuildOccupiedEconomyCells(spawns);
        int minDist = Math.Max(config.MinResourceDistance, config.MinFeatureDistance);

        var resources = PlaceResourceNodes(grid, config, spawns, occupied, minDist);
        var features = PlaceMapFeatures(grid, config, occupied, minDist);

        if (config.ScatterSceneryFromTerrain)
            features.AddRange(PlaceSceneryFromTerrain(grid, features, resources, spawns));

        return BuildDefinition(grid, config, spawns, resources, features);
    }

    /// <summary>
    /// Derives per-chunk terrain densities so adjacent sectors read with distinct biome identity.
    /// Deterministic from chunk seed and coordinates.
    /// </summary>
    private static (float Asteroid, float Nebula, float Debris, float IonStorm, float Wormhole)
        ResolveChunkTerrainDensities(Random chunkRng, int chunkX, int chunkY)
    {
        int biome = Math.Abs(chunkX * 17 + chunkY * 31) % 5;
        float jitter = chunkRng.NextSingle() * 0.02f;

        return biome switch
        {
            0 => (0.04f + jitter, 0.08f + jitter, 0.03f + jitter, 0.02f, 0.01f),
            1 => (0.08f + jitter, 0.04f + jitter, 0.05f + jitter, 0.03f, 0.005f),
            2 => (0.05f + jitter, 0.06f + jitter, 0.07f + jitter, 0.04f + jitter, 0.015f),
            3 => (0.03f + jitter, 0.05f + jitter, 0.04f + jitter, 0.06f + jitter, 0.02f),
            _ => (0.06f + jitter, 0.07f + jitter, 0.03f + jitter, 0.025f, 0.025f),
        };
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
        GridSystem grid,
        MapGeneratorConfig config,
        List<(int X, int Y)> spawns,
        List<(int X, int Y)> occupied,
        int minDistance)
    {
        var nodes = new List<(int X, int Y, string Type)>();
        int count = config.ResourceNodeCount;
        if (count <= 0) return nodes;

        int guaranteedTypes = Math.Min(count, ResourceTypes.Length);
        for (int typeIndex = 0; typeIndex < guaranteedTypes; typeIndex++)
        {
            if (TryPlaceResourceNode(grid, spawns, occupied, minDistance, ResourceTypes[typeIndex],
                    out int x, out int y))
            {
                nodes.Add((x, y, ResourceTypes[typeIndex]));
                occupied.Add((x, y));
            }
        }

        int attempts = 0;
        int maxAttempts = count * 50;
        while (nodes.Count < count && attempts < maxAttempts)
        {
            attempts++;
            string type = ResourceTypes[nodes.Count % ResourceTypes.Length];
            if (!TryPlaceResourceNode(grid, spawns, occupied, minDistance, type, out int x, out int y))
                continue;

            nodes.Add((x, y, type));
            occupied.Add((x, y));
        }

        return nodes;
    }

    private bool TryPlaceResourceNode(
        GridSystem grid,
        List<(int X, int Y)> spawns,
        List<(int X, int Y)> occupied,
        int minDistance,
        string type,
        out int x,
        out int y)
    {
        x = y = 0;
        int marginX = Math.Max(1, (int)(grid.Width * 0.1));
        int marginY = Math.Max(1, (int)(grid.Height * 0.1));
        int maxX = Math.Max(marginX + 1, grid.Width - marginX);
        int maxY = Math.Max(marginY + 1, grid.Height - marginY);

        for (int attempt = 0; attempt < 30; attempt++)
        {
            x = _rng.Next(marginX, maxX);
            y = _rng.Next(marginY, maxY);

            if (!IsEconomyCellValid(grid, x, y, occupied, minDistance))
                continue;

            if (!IsFarFromSpawns(x, y, spawns, minSpawnDistance: 6))
                continue;

            return true;
        }

        return false;
    }

    // ── Map feature placement ─────────────────────────────────────────────────

    private List<MapFeatureDefinition> PlaceMapFeatures(
        GridSystem grid,
        MapGeneratorConfig config,
        List<(int X, int Y)> occupied,
        int minDistance)
    {
        var features = new List<MapFeatureDefinition>();
        int harvestableIndex = 0;

        for (int i = 0; i < config.HarvestablePlanetCount; i++)
        {
            if (!TryPlaceEconomyFeature(grid, occupied, minDistance, out int x, out int y))
                break;

            occupied.Add((x, y));
            harvestableIndex++;
            string resourceType = ResourceTypes[i % ResourceTypes.Length];

            features.Add(new MapFeatureDefinition
            {
                Kind = "harvestable_planet",
                Name = $"Sector {_seed}-P{harvestableIndex}",
                Position = [x, y],
                Scale = _rng.Next(8, 15),
                ResourceType = resourceType,
                Amount = _rng.Next(4000, 6001),
            });
        }

        for (int i = 0; i < config.NeutralPlanetCount; i++)
        {
            if (!TryPlaceEconomyFeature(grid, occupied, minDistance, out int x, out int y))
                break;

            occupied.Add((x, y));

            features.Add(new MapFeatureDefinition
            {
                Kind = "neutral_planet",
                Name = $"Waypoint {i + 1}",
                Subtitle = "Neutral hub",
                Position = [x, y],
                Scale = _rng.Next(8, 15),
            });
        }

        return features;
    }

    private List<MapFeatureDefinition> PlaceSceneryFromTerrain(
        GridSystem grid,
        List<MapFeatureDefinition> existingFeatures,
        List<(int X, int Y, string Type)> resources,
        List<(int X, int Y)> spawns)
    {
        var scenery = new List<MapFeatureDefinition>();
        const int sceneryCap = 10;
        const int economyBuffer = 4;

        var economyCells = new List<(int X, int Y)>();
        economyCells.AddRange(spawns);
        economyCells.AddRange(resources.Select(r => (r.X, r.Y)));
        economyCells.AddRange(existingFeatures
            .Where(f => f.Kind is "harvestable_planet" or "neutral_planet")
            .Select(f => (f.Position[0], f.Position[1])));

        var regionCells = CollectTerrainRegionCells(grid);
        foreach (var (regionType, cells) in regionCells)
        {
            if (scenery.Count >= sceneryCap)
                break;

            if (regionType is not ("asteroid_field" or "nebula" or "debris" or "ion_storm" or "wormhole_remnant"))
                continue;

            if (cells.Count == 0)
                continue;

            int centroidX = (int)Math.Round(cells.Average(c => c.X));
            int centroidY = (int)Math.Round(cells.Average(c => c.Y));

            if (!IsEconomyCellValid(grid, centroidX, centroidY, economyCells, economyBuffer))
                continue;

            scenery.Add(new MapFeatureDefinition
            {
                Kind = "scenery",
                FeatureType = regionType,
                Position = [centroidX, centroidY],
                Scale = _rng.Next(6, 13),
            });
            economyCells.Add((centroidX, centroidY));
        }

        return scenery;
    }

    private static List<(string Type, List<(int X, int Y)> Cells)> CollectTerrainRegionCells(GridSystem grid)
    {
        var nebulaCells = new List<(int X, int Y)>();
        var asteroidCells = new List<(int X, int Y)>();
        var debrisCells = new List<(int X, int Y)>();
        var ionStormCells = new List<(int X, int Y)>();
        var wormholeCells = new List<(int X, int Y)>();

        foreach (GridCell cell in grid.AllCells())
        {
            switch (cell.Terrain)
            {
                case TerrainType.Nebula:
                    nebulaCells.Add((cell.X, cell.Y));
                    break;
                case TerrainType.AsteroidField:
                    asteroidCells.Add((cell.X, cell.Y));
                    break;
                case TerrainType.Debris:
                    debrisCells.Add((cell.X, cell.Y));
                    break;
                case TerrainType.IonStorm:
                    ionStormCells.Add((cell.X, cell.Y));
                    break;
                case TerrainType.WormholeRemnant:
                    wormholeCells.Add((cell.X, cell.Y));
                    break;
            }
        }

        var regions = new List<(string Type, List<(int X, int Y)> Cells)>();
        if (asteroidCells.Count > 0)
            regions.Add(("asteroid_field", asteroidCells));
        if (nebulaCells.Count > 0)
            regions.Add(("nebula", nebulaCells));
        if (debrisCells.Count > 0)
            regions.Add(("debris", debrisCells));
        if (ionStormCells.Count > 0)
            regions.Add(("ion_storm", ionStormCells));
        if (wormholeCells.Count > 0)
            regions.Add(("wormhole_remnant", wormholeCells));
        return regions;
    }

    private bool TryPlaceEconomyFeature(
        GridSystem grid,
        List<(int X, int Y)> occupied,
        int minDistance,
        out int x,
        out int y)
    {
        x = y = 0;
        int marginX = Math.Max(1, (int)(grid.Width * 0.1));
        int marginY = Math.Max(1, (int)(grid.Height * 0.1));
        int maxX = Math.Max(marginX + 1, grid.Width - marginX);
        int maxY = Math.Max(marginY + 1, grid.Height - marginY);

        for (int attempt = 0; attempt < 80; attempt++)
        {
            x = _rng.Next(marginX, maxX);
            y = _rng.Next(marginY, maxY);

            if (IsEconomyCellValid(grid, x, y, occupied, minDistance))
                return true;
        }

        return false;
    }

    private static List<(int X, int Y)> BuildOccupiedEconomyCells(List<(int X, int Y)> spawns) =>
        spawns.Select(s => s).ToList();

    private static bool IsEconomyCellValid(
        GridSystem grid,
        int x,
        int y,
        List<(int X, int Y)> occupied,
        int minDist)
    {
        GridCell? cell = grid.GetCell(x, y);
        if (cell == null || !cell.IsPassable)
            return false;

        foreach (var (ox, oy) in occupied)
        {
            int dx = x - ox, dy = y - oy;
            if (dx * dx + dy * dy < minDist * minDist)
                return false;
        }

        return true;
    }

    private static bool IsFarFromSpawns(int x, int y, List<(int X, int Y)> spawns, int minSpawnDistance)
    {
        foreach (var (sx, sy) in spawns)
        {
            int dx = x - sx, dy = y - sy;
            if (dx * dx + dy * dy < minSpawnDistance * minSpawnDistance)
                return false;
        }

        return true;
    }

    // ── MapDefinition builder ─────────────────────────────────────────────────

    private MapDefinition BuildDefinition(
        GridSystem grid, MapGeneratorConfig config,
        List<(int X, int Y)> spawns,
        List<(int X, int Y, string Type)> resources,
        List<MapFeatureDefinition> mapFeatures)
    {
        // Collect terrain regions
        var regions = new List<MapTerrainRegion>();
        var nebulaCells = new List<int[]>();
        var asteroidCells = new List<int[]>();
        var debrisCells = new List<int[]>();
        var ionStormCells = new List<int[]>();
        var wormholeCells = new List<int[]>();

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
                case TerrainType.IonStorm:
                    ionStormCells.Add([cell.X, cell.Y]);
                    break;
                case TerrainType.WormholeRemnant:
                    wormholeCells.Add([cell.X, cell.Y]);
                    break;
            }
        }

        if (nebulaCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "nebula", Cells = nebulaCells.ToArray() });
        if (asteroidCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "asteroid_field", Cells = asteroidCells.ToArray() });
        if (debrisCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "debris", Cells = debrisCells.ToArray() });
        if (ionStormCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "ion_storm", Cells = ionStormCells.ToArray() });
        if (wormholeCells.Count > 0)
            regions.Add(new MapTerrainRegion { Type = "wormhole_remnant", Cells = wormholeCells.ToArray() });

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
            ResourceNodes = resourceNodes,
            MapFeatures = mapFeatures.ToArray()
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

    /// <summary>Scatter loose debris drifts between asteroid belts.</summary>
    private void ScatterDebris(GridSystem grid, float density)
    {
        ScatterTerrainPatches(grid, density, TerrainType.Debris, minW: 2, maxW: 7, minH: 2, maxH: 7);
    }

    /// <summary>Scatter volatile ion storm cells with jagged footprints.</summary>
    private void ScatterIonStorms(GridSystem grid, float density)
    {
        ScatterTerrainPatches(grid, density, TerrainType.IonStorm, minW: 2, maxW: 6, minH: 2, maxH: 6);
    }

    /// <summary>Scatter rare collapsed wormhole remnant sites.</summary>
    private void ScatterWormholeRemnants(GridSystem grid, float density)
    {
        ScatterTerrainPatches(grid, density, TerrainType.WormholeRemnant, minW: 1, maxW: 3, minH: 1, maxH: 3);
    }

    private void ScatterTerrainPatches(
        GridSystem grid,
        float density,
        TerrainType terrain,
        int minW,
        int maxW,
        int minH,
        int maxH)
    {
        int targetCells = (int)(grid.Width * grid.Height * density);
        int placed = 0;
        int attempts = 0;
        int maxAttempts = targetCells * 20 + 40;

        while (placed < targetCells && attempts < maxAttempts)
        {
            attempts++;
            int ox = _rng.Next(grid.Width);
            int oy = _rng.Next(grid.Height);
            int w = _rng.Next(minW, maxW + 1);
            int h = _rng.Next(minH, maxH + 1);

            for (int x = ox; x < ox + w && x < grid.Width; x++)
            for (int y = oy; y < oy + h && y < grid.Height; y++)
            {
                GridCell? cell = grid.GetCell(x, y);
                if (cell != null && cell.Terrain == TerrainType.Space)
                {
                    cell.Terrain = terrain;
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