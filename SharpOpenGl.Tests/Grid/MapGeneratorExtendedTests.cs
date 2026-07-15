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

    [Fact]
    public void Generate_config_includes_harvestable_and_neutral_planets()
    {
        var gen = new MapGenerator(seed: 42);
        var config = new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            HarvestablePlanetCount = 1,
            NeutralPlanetCount = 1,
        };

        MapDefinition def = gen.Generate(config);

        Assert.Contains(def.MapFeatures, f => f.Kind == "harvestable_planet");
        Assert.Contains(def.MapFeatures, f => f.Kind == "neutral_planet");
    }

    [Fact]
    public void Generate_config_economy_features_respect_min_feature_distance()
    {
        var gen = new MapGenerator(seed: 55);
        const int minDist = 10;
        var config = new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            ResourceNodeCount = 4,
            HarvestablePlanetCount = 1,
            NeutralPlanetCount = 1,
            MinFeatureDistance = minDist,
            ScatterSceneryFromTerrain = false,
        };

        MapDefinition def = gen.Generate(config);

        var economyPositions = def.ResourceNodes
            .Select(n => (n.Position[0], n.Position[1]))
            .Concat(def.MapFeatures
                .Where(f => f.Kind is "harvestable_planet" or "neutral_planet")
                .Select(f => (f.Position[0], f.Position[1])))
            .ToList();

        for (int i = 0; i < economyPositions.Count; i++)
        {
            for (int j = i + 1; j < economyPositions.Count; j++)
            {
                int dx = economyPositions[i].Item1 - economyPositions[j].Item1;
                int dy = economyPositions[i].Item2 - economyPositions[j].Item2;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                Assert.True(dist >= minDist,
                    $"Economy features {i} and {j} too close: {dist}");
            }
        }
    }

    [Fact]
    public void Generate_config_same_seed_produces_identical_features()
    {
        var config = new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            ResourceNodeCount = 4,
            HarvestablePlanetCount = 1,
            NeutralPlanetCount = 1,
        };

        MapDefinition def1 = new MapGenerator(seed: 99).Generate(config);
        MapDefinition def2 = new MapGenerator(seed: 99).Generate(config);

        Assert.Equal(def1.MapFeatures.Length, def2.MapFeatures.Length);
        for (int i = 0; i < def1.MapFeatures.Length; i++)
        {
            Assert.Equal(def1.MapFeatures[i].Kind, def2.MapFeatures[i].Kind);
            Assert.Equal(def1.MapFeatures[i].Position[0], def2.MapFeatures[i].Position[0]);
            Assert.Equal(def1.MapFeatures[i].Position[1], def2.MapFeatures[i].Position[1]);
        }

        Assert.Equal(def1.ResourceNodes.Length, def2.ResourceNodes.Length);
        for (int i = 0; i < def1.ResourceNodes.Length; i++)
        {
            Assert.Equal(def1.ResourceNodes[i].Type, def2.ResourceNodes[i].Type);
            Assert.Equal(def1.ResourceNodes[i].Position[0], def2.ResourceNodes[i].Position[0]);
            Assert.Equal(def1.ResourceNodes[i].Position[1], def2.ResourceNodes[i].Position[1]);
        }
    }

    [Fact]
    public void Generate_config_spawn_produces_mineable_content()
    {
        var gen = new MapGenerator(seed: 77);
        var config = new MapGeneratorConfig();

        MapDefinition def = gen.Generate(config);

        Assert.Empty(SkirmishMapLogic.ValidateEconomy(def));
    }

    [Fact]
    public void GenerateChunk_is_deterministic_for_same_seed_and_coords()
    {
        var gen = new MapGenerator(seed: 0);

        MapDefinition first = gen.GenerateChunk(-2, 4, worldSeed: 1234, cellSize: 10f);
        MapDefinition second = gen.GenerateChunk(-2, 4, worldSeed: 1234, cellSize: 10f);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.ResourceNodes.Length, second.ResourceNodes.Length);
        Assert.Equal(first.MapFeatures.Length, second.MapFeatures.Length);

        for (int i = 0; i < first.ResourceNodes.Length; i++)
        {
            Assert.Equal(first.ResourceNodes[i].Position[0], second.ResourceNodes[i].Position[0]);
            Assert.Equal(first.ResourceNodes[i].Position[1], second.ResourceNodes[i].Position[1]);
            Assert.Equal(first.ResourceNodes[i].Type, second.ResourceNodes[i].Type);
        }
    }

    [Fact]
    public void GenerateChunk_differs_across_chunk_coordinates()
    {
        var gen = new MapGenerator(seed: 0);

        MapDefinition a = gen.GenerateChunk(0, 0, worldSeed: 50, cellSize: 10f);
        MapDefinition b = gen.GenerateChunk(1, 0, worldSeed: 50, cellSize: 10f);

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void GenerateChunk_includes_varied_terrain_regions()
    {
        var gen = new MapGenerator(seed: 0);
        var regionTypes = new HashSet<string>();

        for (int cx = -2; cx <= 2; cx++)
        for (int cy = -2; cy <= 2; cy++)
        {
            MapDefinition chunk = gen.GenerateChunk(cx, cy, worldSeed: 77, cellSize: 10f);
            foreach (var region in chunk.Terrain.Regions)
                regionTypes.Add(region.Type);
        }

        Assert.Contains("asteroid_field", regionTypes);
        Assert.Contains("nebula", regionTypes);
        Assert.True(
            regionTypes.Count >= 3,
            $"Expected at least three terrain region types across chunks, got: {string.Join(", ", regionTypes)}");
    }
}
