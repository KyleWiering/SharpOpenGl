using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Chunk coordinate helpers for sandbox procedural space.
/// <see cref="ChunkCells"/> aligns with fog render chunks (10-cell tiles) × reasonable LOD granularity.
/// </summary>
public static class SandboxChunkCoords
{
    /// <summary>Cells per sandbox chunk edge (64×64 local maps).</summary>
    public const int ChunkCells = 64;

    /// <summary>
    /// Convert a world position to the chunk index containing it.
    /// Uses the same bottom-left grid origin as <see cref="GridSystem.Origin"/>.
    /// </summary>
    public static (int chunkX, int chunkY) WorldToChunk(Vector3 worldPos, float cellSize, Vector2 origin)
    {
        if (cellSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");

        int globalX = GlobalCellX(worldPos.X, cellSize, origin.X);
        int globalY = GlobalCellY(worldPos.Z, cellSize, origin.Y);
        return (FloorDiv(globalX, ChunkCells), FloorDiv(globalY, ChunkCells));
    }

    /// <summary>World-space bottom-left corner of a chunk in XZ.</summary>
    public static Vector2 ChunkOriginWorld(int chunkX, int chunkY, float cellSize, Vector2 gridOrigin)
    {
        float worldX = gridOrigin.X + chunkX * ChunkCells * cellSize;
        float worldZ = gridOrigin.Y + chunkY * ChunkCells * cellSize;
        return new Vector2(worldX, worldZ);
    }

    /// <summary>Deterministic per-chunk seed derived from world seed and chunk indices.</summary>
    public static int ChunkSeed(int worldSeed, int chunkX, int chunkY)
    {
        unchecked
        {
            return worldSeed * 397 ^ chunkX * 1013 ^ chunkY;
        }
    }

    /// <summary>Global cell column from world X.</summary>
    public static int GlobalCellX(float worldX, float cellSize, float originX) =>
        (int)MathF.Floor((worldX - originX) / cellSize);

    /// <summary>Global cell row from world Z.</summary>
    public static int GlobalCellY(float worldZ, float cellSize, float originY) =>
        (int)MathF.Floor((worldZ - originY) / cellSize);

    /// <summary>World-space centre of a global cell on the XZ plane.</summary>
    public static Vector3 GlobalCellToWorld(int globalCellX, int globalCellY, float cellSize, Vector2 gridOrigin)
    {
        float worldX = gridOrigin.X + (globalCellX + 0.5f) * cellSize;
        float worldZ = gridOrigin.Y + (globalCellY + 0.5f) * cellSize;
        return new Vector3(worldX, 0f, worldZ);
    }

    /// <summary>Floor division that behaves correctly for negative dividends.</summary>
    internal static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) ^ (divisor < 0)))
            quotient--;
        return quotient;
    }
}