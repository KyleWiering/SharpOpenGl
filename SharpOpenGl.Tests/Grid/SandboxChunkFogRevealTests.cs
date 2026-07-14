using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class SandboxChunkFogRevealTests
{
    [Fact]
    public void SpawnChunkEconomy_without_reveal_leaves_nodes_unexplored()
    {
        const float cellSize = 10f;
        var origin = new Vector2(-1000f, -1000f);
        var generator = new MapGenerator();
        MapDefinition map = generator.GenerateChunk(0, 0, worldSeed: 42, cellSize);
        var grid = new GridSystem(SandboxChunkCoords.ChunkCells, SandboxChunkCoords.ChunkCells, cellSize, origin);
        var fog = new FogOfWar(grid, playerCount: 1);
        using var world = new World();

        var meshes = new MapFeatureSpawner.MeshHandles
        {
            ResourceNodeMeshId = 1,
            ResourceNodeVertCount = 12,
            PlanetMeshId = 1,
            PlanetVertCount = 12,
            PrimitiveTriangles = 4,
        };

        MapFeatureSpawner.SpawnChunkEconomy(
            world, map, chunkX: 0, chunkY: 0, cellSize, origin, meshes, revealArea: null);

        int nodeCount = 0;
        foreach (var (entity, _) in world.Query<ResourceNodeComponent>())
        {
            nodeCount++;
            var transform = world.GetComponent<TransformComponent>(entity);
            Assert.NotNull(transform);
            Assert.True(grid.WorldToGrid(transform!.Position, out int gx, out int gy));
            Assert.Equal(FogState.Unexplored, fog.GetState(0, gx, gy));
        }

        Assert.True(nodeCount > 0, "Expected procedural chunk to spawn at least one resource node.");
    }
}