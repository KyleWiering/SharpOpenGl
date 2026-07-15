using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Coordinates procedural sandbox chunks for unbounded exploration.
/// <para>
/// <b>Approach (v1):</b> sparse chunk dictionary keyed by chunk indices, merged into a single
/// expanding <see cref="GridSystem"/> for ECS pathfinding, fog, and building placement.
/// Global cell indices are unbounded <c>int</c> values; the merged grid uses a sliding local
/// origin so pathfinding works across chunk borders without per-chunk routing seams.
/// </para>
/// <para>
/// <b>Trade-offs:</b> merging copies terrain (and preserved fog/occupancy) whenever the loaded
/// bounding box grows — acceptable while chunks are never unloaded (v1 only expands). A pure
/// sparse cell store would avoid rebuilds but would require retooling every grid consumer.
/// </para>
/// <para>
/// Players can move and build beyond the legacy 200×200 desktop board (±1000 world units);
/// newly explored sectors load additional 64×64 chunks on demand.
/// </para>
/// </summary>
public sealed class SandboxChunkGrid
{
    private readonly Dictionary<(int Cx, int Cy), LoadedChunk> _chunks = new();
    private readonly MapGenerator _generator = new();
    private readonly float _cellSize;
    private readonly Vector2 _gridOrigin;

    private GridSystem? _mergedGrid;
    private int _globalOriginCellX;
    private int _globalOriginCellY;

    /// <param name="cellSize">World-space edge length of one cell.</param>
    /// <param name="gridOrigin">Bottom-left world position of global cell (0, 0).</param>
    public SandboxChunkGrid(float cellSize, Vector2 gridOrigin)
    {
        if (cellSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");

        _cellSize = cellSize;
        _gridOrigin = gridOrigin;
    }

    /// <summary>Merged grid backing ECS systems. Rebuilt when new chunks load.</summary>
    public GridSystem Grid =>
        _mergedGrid ?? throw new InvalidOperationException("No chunks loaded yet.");

    /// <summary>True when at least one chunk has been generated.</summary>
    public bool HasLoadedChunks => _chunks.Count > 0;

    /// <summary>Loaded chunk index bounds (inclusive).</summary>
    public (int MinChunkX, int MinChunkY, int MaxChunkX, int MaxChunkY) LoadedChunkBounds
    {
        get
        {
            if (_chunks.Count == 0)
                return (0, 0, 0, 0);

            int minCx = int.MaxValue, minCy = int.MaxValue;
            int maxCx = int.MinValue, maxCy = int.MinValue;
            foreach (var key in _chunks.Keys)
            {
                minCx = Math.Min(minCx, key.Cx);
                minCy = Math.Min(minCy, key.Cy);
                maxCx = Math.Max(maxCx, key.Cx);
                maxCy = Math.Max(maxCy, key.Cy);
            }

            return (minCx, minCy, maxCx, maxCy);
        }
    }

    /// <summary>Axis-aligned world bounds of all loaded chunks.</summary>
    public (float MinX, float MaxX, float MinZ, float MaxZ) LoadedWorldBounds
    {
        get
        {
            var (minCx, minCy, maxCx, maxCy) = LoadedChunkBounds;
            if (_chunks.Count == 0)
                return (0f, 0f, 0f, 0f);

            Vector2 minCorner = ChunkOriginWorld(minCx, minCy);
            Vector2 maxCorner = ChunkOriginWorld(maxCx + 1, maxCy + 1);
            return (minCorner.X, maxCorner.X, minCorner.Y, maxCorner.Y);
        }
    }

    /// <summary>Global cell bounds covered by loaded chunks (inclusive).</summary>
    public (int MinGlobalX, int MinGlobalY, int MaxGlobalX, int MaxGlobalY) LoadedGlobalCellBounds
    {
        get
        {
            var (minCx, minCy, maxCx, maxCy) = LoadedChunkBounds;
            if (_chunks.Count == 0)
                return (0, 0, 0, 0);

            return (
                minCx * SandboxChunkCoords.ChunkCells,
                minCy * SandboxChunkCoords.ChunkCells,
                (maxCx + 1) * SandboxChunkCoords.ChunkCells - 1,
                (maxCy + 1) * SandboxChunkCoords.ChunkCells - 1);
        }
    }

    /// <summary>Normalize a world position against <see cref="LoadedWorldBounds"/> for minimap use.</summary>
    public Vector2 WorldToNormalized(Vector3 worldPos)
    {
        var (minX, maxX, minZ, maxZ) = LoadedWorldBounds;
        float width = MathF.Max(1f, maxX - minX);
        float height = MathF.Max(1f, maxZ - minZ);
        return new Vector2(
            (worldPos.X - minX) / width,
            (worldPos.Z - minZ) / height);
    }

    /// <summary>
    /// Ensure all chunks within <paramref name="radiusChunks"/> of <paramref name="worldCenter"/> are loaded.
    /// Returns metadata for chunks generated this call (economy spawning is handled by the caller).
    /// </summary>
    public IReadOnlyList<LoadedChunkInfo> EnsureChunksAround(
        Vector3 worldCenter,
        int radiusChunks,
        int worldSeed,
        float cellSize)
    {
        if (radiusChunks < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusChunks));

        float size = cellSize > 0f ? cellSize : _cellSize;
        var (centerCx, centerCy) = SandboxChunkCoords.WorldToChunk(worldCenter, size, _gridOrigin);
        var newlyLoaded = new List<LoadedChunkInfo>();

        for (int dx = -radiusChunks; dx <= radiusChunks; dx++)
        for (int dy = -radiusChunks; dy <= radiusChunks; dy++)
        {
            int cx = centerCx + dx;
            int cy = centerCy + dy;
            var key = (cx, cy);
            if (_chunks.ContainsKey(key))
                continue;

            MapDefinition map = _generator.GenerateChunk(cx, cy, worldSeed, size);
            GridSystem localGrid = MapLoader.FromDefinition(map);
            _chunks[key] = new LoadedChunk(localGrid, map);
            newlyLoaded.Add(new LoadedChunkInfo(cx, cy, map));
        }

        if (newlyLoaded.Count > 0)
            RebuildMergedGrid();

        return newlyLoaded;
    }

    /// <summary>Resolve a world position to a cell in the merged grid, if within loaded bounds.</summary>
    public bool TryGetCellWorld(Vector3 worldPos, out GridCell? cell)
    {
        cell = null;
        if (_mergedGrid == null)
            return false;

        if (!_mergedGrid.WorldToGrid(worldPos, out int lx, out int ly))
            return false;

        cell = _mergedGrid.GetCell(lx, ly);
        return cell != null;
    }

    /// <summary>Convert global cell indices to world-space cell centre.</summary>
    public Vector3 GridToWorld(int globalCellX, int globalCellY) =>
        SandboxChunkCoords.GlobalCellToWorld(globalCellX, globalCellY, _cellSize, _gridOrigin);

    /// <summary>Convert a world position to unbounded global cell indices.</summary>
    public bool WorldToGlobalCell(Vector3 worldPos, out int gx, out int gy)
    {
        gx = SandboxChunkCoords.GlobalCellX(worldPos.X, _cellSize, _gridOrigin.X);
        gy = SandboxChunkCoords.GlobalCellY(worldPos.Z, _cellSize, _gridOrigin.Y);
        return true;
    }

    /// <summary>Mark a chunk's economy entities as spawned.</summary>
    public void MarkEconomySpawned(int chunkX, int chunkY)
    {
        if (_chunks.TryGetValue((chunkX, chunkY), out LoadedChunk? chunk))
            chunk.EconomySpawned = true;
    }

    /// <summary>Chunks that still need economy entity spawning.</summary>
    public IEnumerable<LoadedChunkInfo> GetPendingEconomyChunks()
    {
        foreach (var ((cx, cy), chunk) in _chunks)
        {
            if (!chunk.EconomySpawned)
                yield return new LoadedChunkInfo(cx, cy, chunk.MapDefinition);
        }
    }

    private Vector2 ChunkOriginWorld(int chunkX, int chunkY) =>
        SandboxChunkCoords.ChunkOriginWorld(chunkX, chunkY, _cellSize, _gridOrigin);

    private void RebuildMergedGrid()
    {
        var (minCx, minCy, maxCx, maxCy) = LoadedChunkBounds;
        int minGlobalX = minCx * SandboxChunkCoords.ChunkCells;
        int minGlobalY = minCy * SandboxChunkCoords.ChunkCells;
        int maxGlobalX = (maxCx + 1) * SandboxChunkCoords.ChunkCells - 1;
        int maxGlobalY = (maxCy + 1) * SandboxChunkCoords.ChunkCells - 1;

        int width = maxGlobalX - minGlobalX + 1;
        int height = maxGlobalY - minGlobalY + 1;
        var newOrigin = new Vector2(
            _gridOrigin.X + minGlobalX * _cellSize,
            _gridOrigin.Y + minGlobalY * _cellSize);

        var newGrid = new GridSystem(width, height, _cellSize, newOrigin);
        GridSystem? oldGrid = _mergedGrid;

        foreach (var ((cx, cy), chunk) in _chunks)
        {
            int baseGx = cx * SandboxChunkCoords.ChunkCells - minGlobalX;
            int baseGy = cy * SandboxChunkCoords.ChunkCells - minGlobalY;

            foreach (GridCell local in chunk.LocalGrid.AllCells())
            {
                GridCell? target = newGrid.GetCell(baseGx + local.X, baseGy + local.Y);
                if (target != null)
                    target.Terrain = local.Terrain;
            }
        }

        if (oldGrid != null)
            PreserveDynamicCellState(oldGrid, newGrid);

        _mergedGrid = newGrid;
        _globalOriginCellX = minGlobalX;
        _globalOriginCellY = minGlobalY;
    }

    private static void PreserveDynamicCellState(GridSystem oldGrid, GridSystem newGrid)
    {
        foreach (GridCell oldCell in oldGrid.AllCells())
        {
            Vector3 world = oldGrid.GridToWorld(oldCell.X, oldCell.Y);
            if (!newGrid.WorldToGrid(world, out int nx, out int ny))
                continue;

            GridCell? newCell = newGrid.GetCell(nx, ny);
            if (newCell == null)
                continue;

            for (int playerId = 0; playerId < 8; playerId++)
                newCell.SetFog(playerId, oldCell.GetFog(playerId));

            if (oldCell.Occupant != ECS.Entity.Null)
                newCell.Occupant = oldCell.Occupant;

            if (oldCell.ResourceEntity != ECS.Entity.Null)
                newCell.ResourceEntity = oldCell.ResourceEntity;
        }
    }

    /// <summary>One loaded chunk's terrain and procedural definition.</summary>
    public sealed class LoadedChunk
    {
        public LoadedChunk(GridSystem localGrid, MapDefinition mapDefinition)
        {
            LocalGrid = localGrid;
            MapDefinition = mapDefinition;
        }

        public GridSystem LocalGrid { get; }
        public MapDefinition MapDefinition { get; }
        public bool EconomySpawned { get; set; }
    }

    /// <summary>Chunk coordinates plus generated map for economy spawning.</summary>
    public readonly record struct LoadedChunkInfo(int ChunkX, int ChunkY, MapDefinition Map);
}