using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class SandboxChunkGridTests
{
    private const float CellSize = 10f;
    private static readonly Vector2 Origin = new(-1000f, -1000f);

    [Fact]
    public void ChunkSeed_is_deterministic()
    {
        int seedA = SandboxChunkCoords.ChunkSeed(42, 3, -2);
        int seedB = SandboxChunkCoords.ChunkSeed(42, 3, -2);
        int seedC = SandboxChunkCoords.ChunkSeed(42, 4, -2);

        Assert.Equal(seedA, seedB);
        Assert.NotEqual(seedA, seedC);

        int expected;
        unchecked
        {
            expected = 42 * 397 ^ 3 * 1013 ^ -2;
        }

        Assert.Equal(expected, seedA);
    }

    [Fact]
    public void EnsureChunksAround_loads_neighbor_chunks()
    {
        var grid = new SandboxChunkGrid(CellSize, Origin);
        var worldCenter = Vector3.Zero;

        var (centerCx, centerCy) = SandboxChunkCoords.WorldToChunk(worldCenter, CellSize, Origin);

        var first = grid.EnsureChunksAround(worldCenter, radiusChunks: 0, worldSeed: 7, CellSize);
        Assert.Single(first);
        Assert.Equal((centerCx, centerCy, centerCx, centerCy), grid.LoadedChunkBounds);

        var expanded = grid.EnsureChunksAround(worldCenter, radiusChunks: 1, worldSeed: 7, CellSize);
        Assert.Equal(8, expanded.Count);
        Assert.Equal((centerCx - 1, centerCy - 1, centerCx + 1, centerCy + 1), grid.LoadedChunkBounds);
        Assert.Equal(3 * SandboxChunkCoords.ChunkCells, grid.Grid.Width);
        Assert.Equal(3 * SandboxChunkCoords.ChunkCells, grid.Grid.Height);
    }

    [Fact]
    public void GenerateChunk_places_economy_features()
    {
        var gen = new MapGenerator(seed: 0);
        MapDefinition chunk = gen.GenerateChunk(2, -1, worldSeed: 99, CellSize);

        Assert.Equal(SandboxChunkCoords.ChunkCells, chunk.GridSize[0]);
        Assert.Equal(SandboxChunkCoords.ChunkCells, chunk.GridSize[1]);
        Assert.InRange(chunk.ResourceNodes.Length, 2, 4);
        Assert.InRange(
            chunk.MapFeatures.Count(f => f.Kind == "harvestable_planet"),
            0,
            1);
    }

    [Fact]
    public void World_position_outside_200x200_maps_to_valid_cell()
    {
        var grid = new SandboxChunkGrid(CellSize, Origin);
        grid.EnsureChunksAround(new Vector3(2500f, 0f, 2500f), radiusChunks: 0, worldSeed: 5, CellSize);

        Assert.True(grid.WorldToGlobalCell(new Vector3(2500f, 0f, 2500f), out int gx, out int gy));
        Assert.Equal(350, gx);
        Assert.Equal(350, gy);

        Assert.True(grid.TryGetCellWorld(new Vector3(2500f, 0f, 2500f), out GridCell? cell));
        Assert.NotNull(cell);
        Assert.True(cell!.IsPassable);
    }

    [Fact]
    public void GenerateChunk_same_inputs_produce_identical_maps()
    {
        var gen = new MapGenerator(seed: 0);
        MapDefinition a = gen.GenerateChunk(1, 2, worldSeed: 55, CellSize);
        MapDefinition b = gen.GenerateChunk(1, 2, worldSeed: 55, CellSize);

        Assert.Equal(a.ResourceNodes.Length, b.ResourceNodes.Length);
        for (int i = 0; i < a.ResourceNodes.Length; i++)
        {
            Assert.Equal(a.ResourceNodes[i].Type, b.ResourceNodes[i].Type);
            Assert.Equal(a.ResourceNodes[i].Position[0], b.ResourceNodes[i].Position[0]);
            Assert.Equal(a.ResourceNodes[i].Position[1], b.ResourceNodes[i].Position[1]);
        }
    }

    [Fact]
    public void EnsureChunksAround_is_idempotent_when_camera_stationary()
    {
        var grid = new SandboxChunkGrid(CellSize, Origin);
        var center = new Vector3(120f, 0f, -80f);

        var first = grid.EnsureChunksAround(center, radiusChunks: 1, worldSeed: 11, CellSize);
        var second = grid.EnsureChunksAround(center, radiusChunks: 1, worldSeed: 11, CellSize);

        Assert.Equal(9, first.Count);
        Assert.Empty(second);
        Assert.Equal(3, grid.Grid.Width / SandboxChunkCoords.ChunkCells);
    }

    [Fact]
    public void EnsureChunksAround_only_loads_new_chunks_when_camera_moves()
    {
        var grid = new SandboxChunkGrid(CellSize, Origin);
        var start = new Vector3(0f, 0f, 0f);

        grid.EnsureChunksAround(start, radiusChunks: 0, worldSeed: 3, CellSize);
        var (minCx0, _, maxCx0, _) = grid.LoadedChunkBounds;
        int initialChunks = (maxCx0 - minCx0 + 1);

        float step = SandboxChunkCoords.ChunkCells * CellSize;
        var moved = new Vector3(step * 2f, 0f, 0f);
        var newlyLoaded = grid.EnsureChunksAround(moved, radiusChunks: 0, worldSeed: 3, CellSize);

        Assert.Equal(1, initialChunks);
        Assert.NotEmpty(newlyLoaded);
        var (_, _, maxCx1, _) = grid.LoadedChunkBounds;
        Assert.True(maxCx1 > maxCx0);
    }
}