using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Builds static nebula veil quads for fog-of-war cells in view.
/// Visible cells have no veil; unexplored and explored use distinct layered fills.
/// </summary>
public sealed class FogNebulaOverlay
{
    public const int ChunkCells = 10;
    public const int MaxActiveChunks = 400;

    /// <summary>Veil tuning shared with minimap fog tiles.</summary>
    public static class Config
    {
        public const float UnexploredVeilAlpha = 0.72f;
        public const float ExploredVeilAlpha = 0.38f;
        public const float WorldVeilHeight = 0.32f;

        // Legacy names kept for palette/tests that referenced particle-era constants.
        public const float UnexploredStartAlpha = UnexploredVeilAlpha;
        public const float ExploredStartAlpha = ExploredVeilAlpha;
    }

    private readonly List<FogVeilQuad> _quads = new();
    private FogOfWar? _lastFog;
    private GridSystem? _lastGrid;
    private int _lastPlayerId;

    public IReadOnlyList<FogVeilQuad> VeilQuads => _quads;
    public int ActiveVeilCount => _quads.Count;

    public void Clear()
    {
        _quads.Clear();
        _lastFog = null;
        _lastGrid = null;
    }

    /// <summary>Rebuild stationary fog veils for cells inside optional camera bounds.</summary>
    public void Sync(
        FogOfWar fog,
        GridSystem grid,
        int playerId,
        float chunkWorldSize,
        Vector4? cameraBoundsXZ = null)
    {
        _ = chunkWorldSize;
        _quads.Clear();
        _lastFog = fog;
        _lastGrid = grid;
        _lastPlayerId = playerId;

        if (!TryResolveGridBounds(grid, cameraBoundsXZ, out int minGx, out int minGy, out int maxGx, out int maxGy))
            return;

        for (int gy = minGy; gy <= maxGy; gy++)
        for (int gx = minGx; gx <= maxGx; gx++)
        {
            FogState state = fog.GetState(playerId, gx, gy);
            if (state == FogState.Visible)
                continue;

            Vector3 center = grid.GridToWorld(gx, gy);
            AppendCellVeils(center, grid.CellSize, state, gx, gy);
        }
    }

    /// <summary>No-op — fog veils are static; retained for host call-site compatibility.</summary>
    public void Update(float deltaTime) { }

    /// <summary>
    /// Resolves fog overlay state for a chunk. <c>null</c> means no veil (visible territory).
    /// </summary>
    public static FogState? ResolveOverlayState(
        FogOfWar fog,
        GridSystem grid,
        int playerId,
        int chunkX,
        int chunkY,
        int chunkCells = ChunkCells)
    {
        bool anyVisible = false;
        bool anyExplored = false;

        for (int dx = 0; dx < chunkCells; dx++)
        for (int dy = 0; dy < chunkCells; dy++)
        {
            int gx = chunkX * chunkCells + dx;
            int gy = chunkY * chunkCells + dy;
            if (gx >= grid.Width || gy >= grid.Height)
                continue;

            FogState state = fog.GetState(playerId, gx, gy);
            if (state == FogState.Visible)
                anyVisible = true;
            if (state != FogState.Unexplored)
                anyExplored = true;
        }

        if (anyVisible)
            return null;

        return anyExplored ? FogState.Explored : FogState.Unexplored;
    }

    public bool HasEmitterForChunk(int chunkX, int chunkY) =>
        _lastFog != null && _lastGrid != null
        && ResolveOverlayState(_lastFog, _lastGrid, _lastPlayerId, chunkX, chunkY) is not null;

    public FogState? GetEmitterStateForChunk(int chunkX, int chunkY) =>
        _lastFog != null && _lastGrid != null
            ? ResolveOverlayState(_lastFog, _lastGrid, _lastPlayerId, chunkX, chunkY)
            : null;

    private void AppendCellVeils(Vector3 cellCenter, float cellSize, FogState state, int gridX, int gridY)
    {
        IReadOnlyList<FogNebulaStyle.VeilLayer> layers = state == FogState.Unexplored
            ? FogNebulaStyle.UnexploredLayers(gridX, gridY)
            : FogNebulaStyle.ExploredLayers(gridX, gridY);

        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            FogNebulaStyle.VeilLayer layer = layers[layerIndex];
            float sizeX = cellSize * layer.SizeFactor.X;
            float sizeZ = cellSize * layer.SizeFactor.Y;
            Vector3 offset = new(
                layer.OffsetFactor.X * cellSize,
                Config.WorldVeilHeight,
                layer.OffsetFactor.Z * cellSize);

            _quads.Add(new FogVeilQuad(
                cellCenter + offset,
                sizeX,
                sizeZ,
                FogNebulaStyle.LayerColor(layer.Rgb, layer.Alpha, gridX, gridY, layerIndex),
                gridX,
                gridY));
        }
    }

    private static bool TryResolveGridBounds(
        GridSystem grid,
        Vector4? cameraBoundsXZ,
        out int minGx,
        out int minGy,
        out int maxGx,
        out int maxGy)
    {
        minGx = minGy = 0;
        maxGx = grid.Width - 1;
        maxGy = grid.Height - 1;

        if (!cameraBoundsXZ.HasValue)
            return grid.Width > 0 && grid.Height > 0;

        if (!grid.WorldToGrid(new Vector3(cameraBoundsXZ.Value.X, 0f, cameraBoundsXZ.Value.Z), out minGx, out minGy))
            minGx = minGy = 0;
        if (!grid.WorldToGrid(new Vector3(cameraBoundsXZ.Value.Y, 0f, cameraBoundsXZ.Value.W), out maxGx, out maxGy))
        {
            maxGx = grid.Width - 1;
            maxGy = grid.Height - 1;
        }

        minGx = Math.Clamp(minGx, 0, grid.Width - 1);
        minGy = Math.Clamp(minGy, 0, grid.Height - 1);
        maxGx = Math.Clamp(maxGx, 0, grid.Width - 1);
        maxGy = Math.Clamp(maxGy, 0, grid.Height - 1);

        if (minGx > maxGx) (minGx, maxGx) = (maxGx, minGx);
        if (minGy > maxGy) (minGy, maxGy) = (maxGy, minGy);

        return true;
    }
}

/// <summary>One blended ground quad in the fog veil stack.</summary>
public readonly record struct FogVeilQuad(
    Vector3 Center,
    float SizeX,
    float SizeZ,
    Vector4 Color,
    int GridX,
    int GridY);