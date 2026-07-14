namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Documented sandbox world-scale bounds — players explore beyond the legacy 200×200 desktop board.
/// </summary>
public static class SandboxWorldScale
{
    /// <summary>Legacy fixed-map half-extent in world units (200 cells × 10 u at origin-centered board).</summary>
    public const float LegacyBoardHalfExtent = 1000f;

    /// <summary>Minimum loaded world span (one 64×64 chunk at 10 u/cell).</summary>
    public const float MinChunkWorldSpan = SandboxChunkCoords.ChunkCells * 10f;

    /// <summary>
    /// Returns true when loaded sandbox bounds exceed the legacy ±1000 u board on both axes.
    /// </summary>
    public static bool ExceedsLegacyBoard(
        float minX, float maxX, float minZ, float maxZ)
    {
        float width = maxX - minX;
        float depth = maxZ - minZ;
        return width > LegacyBoardHalfExtent * 2f + MinChunkWorldSpan * 0.5f
            && depth > LegacyBoardHalfExtent * 2f + MinChunkWorldSpan * 0.5f;
    }

    /// <summary>Clamp camera fog/terrain bounds to loaded chunk world AABB.</summary>
    public static (float MinX, float MaxX, float MinZ, float MaxZ) ClampCameraBounds(
        float camMinX, float camMaxX, float camMinZ, float camMaxZ,
        float loadedMinX, float loadedMaxX, float loadedMinZ, float loadedMaxZ)
    {
        return (
            MathF.Max(camMinX, loadedMinX),
            MathF.Min(camMaxX, loadedMaxX),
            MathF.Max(camMinZ, loadedMinZ),
            MathF.Min(camMaxZ, loadedMaxZ));
    }
}