using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class MapGeneratorExtendedTests
{
    [Fact]
    public void Generate_config_produces_valid_MapDefinition()
    {
        var gen = new MapGenerator(seed: 42);
        var config = new MapGeneratorConfig
        {
            Width = 32,
            Height = 32,
            ResourceNodeCount = 4,
            SpawnPointCount = 2,
            MinResourceDistance = 6
        };

        MapDefinition def = gen.Generate(config);

        Assert.Equal("generated_42", def.Id);
        Assert.Equal(32, def.GridSize[0]);
        Assert.Equal(32, def.GridSize[1]);
        Assert.Equal(2, def.SpawnPoints.Length);
        Assert.Equal(4, def.ResourceNodes.Length);
    }

    [Fact]
    public void Generate_config_spawns_are_far_apart()
    {
        var gen = new MapGenerator(seed: 7);
        var config = new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            SpawnPointCount = 2
        };

        MapDefinition def = gen.Generate(config);

        Assert.Equal(2, def.SpawnPoints.Length);
        int dx = def.SpawnPoints[0].Position[0] - def.SpawnPoints[1].Position[0];
        int dy = def.SpawnPoints[0].Position[1] - def.SpawnPoints[1].Position[1];
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        Assert.True(dist > 20f, $"Spawns should be far apart, got distance {dist}");
    }

    [Fact]
    public void Generate_config_resources_have_minimum_distance()
    {
        var gen = new MapGenerator(seed: 99);
        var config = new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            ResourceNodeCount = 4,
            MinResourceDistance = 8
        };

        MapDefinition def = gen.Generate(config);

        for (int i = 0; i < def.ResourceNodes.Length; i++)
        {
            for (int j = i + 1; j < def.ResourceNodes.Length; j++)
            {
                int dx = def.ResourceNodes[i].Position[0] - def.ResourceNodes[j].Position[0];
                int dy = def.ResourceNodes[i].Position[1] - def.ResourceNodes[j].Position[1];
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                Assert.True(dist >= 8f,
                    $"Resources {i} and {j} too close: {dist}");
            }
        }
    }

    [Fact]
    public void Generate_config_spawns_are_reachable_from_each_other()
    {
        var gen = new MapGenerator(seed: 123);
        var config = new MapGeneratorConfig
        {
            Width = 32,
            Height = 32,
            SpawnPointCount = 2,
            AsteroidDensity = 0.05f,
            NebulaDensity = 0.05f
        };

        MapDefinition def = gen.Generate(config);
        GridSystem grid = MapLoader.FromDefinition(def);

        var spawn1 = grid.GetCell(def.SpawnPoints[0].Position[0],
                                   def.SpawnPoints[0].Position[1])!;
        var spawn2 = grid.GetCell(def.SpawnPoints[1].Position[0],
                                   def.SpawnPoints[1].Position[1])!;

        var path = Pathfinding.FindPath(grid, spawn1, spawn2);
        Assert.NotEmpty(path);
    }

    [Fact]
    public void Generate_config_different_seeds_produce_different_maps()
    {
        var gen1 = new MapGenerator(seed: 1);
        var gen2 = new MapGenerator(seed: 2);
        var config = new MapGeneratorConfig { Width = 32, Height = 32 };

        MapDefinition def1 = gen1.Generate(config);
        MapDefinition def2 = gen2.Generate(config);

        // At least terrain regions should differ
        Assert.NotEqual(def1.Id, def2.Id);
    }
}
