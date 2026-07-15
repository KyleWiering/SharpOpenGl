using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

/// <summary>P03-D10 — sandbox world scale exceeds legacy 200×200 board; camera bounds clamp to loaded chunks.</summary>
public class SandboxWorldScaleBoundsTests
{
    private const float CellSize = 10f;
    private static readonly Vector2 Origin = new(-1000f, -1000f);

    [Fact]
    public void Loaded_world_bounds_exceed_legacy_200x200_board_when_exploring_far()
    {
        var grid = new SandboxChunkGrid(CellSize, Origin);
        grid.EnsureChunksAround(new Vector3(2500f, 0f, 2500f), radiusChunks: 2, worldSeed: 42, CellSize);

        var (minX, maxX, minZ, maxZ) = grid.LoadedWorldBounds;
        Assert.True(maxX > SandboxWorldScale.LegacyBoardHalfExtent);
        Assert.True(maxZ > SandboxWorldScale.LegacyBoardHalfExtent);
        Assert.True(SandboxWorldScale.ExceedsLegacyBoard(minX, maxX, minZ, maxZ));
    }

    [Fact]
    public void World_position_beyond_legacy_board_maps_to_valid_global_cell()
    {
        var grid = new SandboxChunkGrid(CellSize, Origin);
        grid.EnsureChunksAround(new Vector3(1800f, 0f, -1200f), radiusChunks: 0, worldSeed: 7, CellSize);

        Assert.True(grid.WorldToGlobalCell(new Vector3(1800f, 0f, -1200f), out int gx, out int gy));
        Assert.True(gx > 200);
        Assert.NotEqual(0, gy);
        Assert.True(grid.TryGetCellWorld(new Vector3(1800f, 0f, -1200f), out _));
    }

    [Fact]
    public void ClampCameraBounds_intersects_loaded_chunk_aabb()
    {
        var grid = new SandboxChunkGrid(CellSize, Origin);
        grid.EnsureChunksAround(Vector3.Zero, radiusChunks: 1, worldSeed: 3, CellSize);
        var (minX, maxX, minZ, maxZ) = grid.LoadedWorldBounds;

        var (cMinX, cMaxX, cMinZ, cMaxZ) = SandboxWorldScale.ClampCameraBounds(
            camMinX: -5000f, camMaxX: 5000f, camMinZ: -5000f, camMaxZ: 5000f,
            loadedMinX: minX, loadedMaxX: maxX, loadedMinZ: minZ, loadedMaxZ: maxZ);

        Assert.Equal(minX, cMinX, 3);
        Assert.Equal(maxX, cMaxX, 3);
        Assert.Equal(minZ, cMinZ, 3);
        Assert.Equal(maxZ, cMaxZ, 3);
    }
}