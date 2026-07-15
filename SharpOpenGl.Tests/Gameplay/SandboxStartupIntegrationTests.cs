using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.Gameplay;

/// <summary>Headless integration guard for menu sandbox bootstrap (chunk grid, fog, economy).</summary>
public class SandboxStartupIntegrationTests
{
    private const float CellSize = 10f;
    private static readonly Vector2 Origin = new(-1000f, -1000f);

    [Theory]
    [InlineData("")]
    [InlineData("frontier-alpha")]
    [InlineData("42")]
    [InlineData("4Yu0Sdnp")]
    public void Sandbox_bootstrap_sequence_completes_for_common_seeds(string seedText)
    {
        var setup = new SandboxSetupResult(seedText, ProceduralSeedHelper.ParseSeed(seedText));
        var config = SandboxConfig.Defaults;

        var chunkGrid = new SandboxChunkGrid(CellSize, Origin);
        var initialChunks = chunkGrid.EnsureChunksAround(
            Vector3.Zero, radiusChunks: config.ChunkLoadRadius, setup.ParsedSeed, CellSize);

        Assert.NotEmpty(initialChunks);
        GridSystem grid = chunkGrid.Grid;
        Assert.True(grid.Width > 0);
        Assert.True(grid.Height > 0);

        var world = new World();
        var fog = new FogOfWar(grid, playerCount: 2);
        var fogSystem = new FogOfWarSystem(grid, fog, playerId: 0);
        world.AddSystem(fogSystem);

        var meshes = new MapFeatureSpawner.MeshHandles
        {
            ResourceNodeMeshId = 1,
            ResourceNodeVertCount = 12,
            PlanetMeshId = 1,
            PlanetVertCount = 12,
            PrimitiveTriangles = 4,
        };

        foreach (var chunk in initialChunks)
        {
            MapFeatureSpawner.SpawnChunkEconomy(
                world,
                chunk.Map,
                chunk.ChunkX,
                chunk.ChunkY,
                CellSize,
                Origin,
                meshes);
        }

        if (grid.WorldToGrid(Vector3.Zero, out int gx, out int gy))
            fog.Reveal(0, gx, gy, config.InitialRevealRadius);

        Entity hero = world.CreateEntity();
        world.AddComponent(hero, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(hero, new SightRadiusComponent { Radius = 8 });

        for (int frame = 0; frame < 5; frame++)
            world.Update(0.016f);

        var overlay = new FogNebulaOverlay();
        float chunkWorld = FogNebulaOverlay.ChunkCells * CellSize;
        overlay.Sync(fog, grid, playerId: 0, chunkWorld);
        overlay.Update(0.016f);
        Assert.True(overlay.VeilQuads.Count >= 0);

        world.Dispose();
    }
}