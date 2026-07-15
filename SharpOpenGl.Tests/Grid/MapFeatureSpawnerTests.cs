using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class MapFeatureSpawnerTests
{
    private static MapFeatureSpawner.MeshHandles TestMeshes => new()
    {
        PlanetMeshId = 1,
        PlanetVertCount = 60,
        AsteroidFieldMeshId = 10,
        AsteroidFieldVertCount = 300,
        NebulaMeshId = 11,
        NebulaVertCount = 900,
        SceneryMeshId = 2,
        SceneryVertCount = 36,
        IonStormMeshId = 12,
        IonStormVertCount = 420,
        WormholeRemnantMeshId = 13,
        WormholeRemnantVertCount = 860,
        ResourceNodeMeshId = 3,
        ResourceNodeVertCount = 6,
    };

    [Fact]
    public void SpawnAll_creates_resource_nodes_from_map()
    {
        using var world = new World();
        var map = new MapDefinition
        {
            ResourceNodes =
            [
                new MapResourceNode { Type = "energy", Position = [10, 10], Amount = 2500 },
            ],
        };

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        var nodes = world.Query<ResourceNodeComponent>().ToList();
        Assert.Single(nodes);
        Assert.Equal(ResourceType.Energy, nodes[0].Component.ResourceType);
        Assert.Equal(2500, nodes[0].Component.Amount);
    }

    [Fact]
    public void SpawnAll_creates_neutral_planet_with_map_feature()
    {
        using var world = new World();
        var map = new MapDefinition
        {
            MapFeatures =
            [
                new MapFeatureDefinition
                {
                    Kind = "neutral_planet",
                    Name = "Waypoint Prime",
                    Subtitle = "Neutral hub",
                    Position = [32, 32],
                    Scale = 12,
                },
            ],
        };

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        var features = world.Query<MapFeatureComponent>().ToList();
        Assert.Single(features);
        Assert.Equal(MapFeatureKind.NeutralPlanet, features[0].Component.Kind);
        Assert.Equal("Waypoint Prime", world.GetComponent<EntityNameComponent>(features[0].Entity)!.DisplayName);
        Assert.Equal(EntityDisplayKind.Neutral, GameplayEntityDisplay.Classify(world, features[0].Entity));
    }

    [Fact]
    public void SpawnAll_creates_harvestable_planet_as_resource_node()
    {
        using var world = new World();
        var map = new MapDefinition
        {
            MapFeatures =
            [
                new MapFeatureDefinition
                {
                    Kind = "harvestable_planet",
                    Name = "Kethos Minor",
                    Position = [48, 28],
                    ResourceType = "minerals",
                    Amount = 8000,
                },
            ],
        };

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        var nodes = world.Query<ResourceNodeComponent>().ToList();
        Assert.Single(nodes);
        Assert.Equal(ResourceType.Minerals, nodes[0].Component.Amount > 0 ? nodes[0].Component.ResourceType : ResourceType.Energy);
        Assert.Equal(8000, nodes[0].Component.Amount);
        Assert.Equal(EntityDisplayKind.Harvestable, GameplayEntityDisplay.Classify(world, nodes[0].Entity));
        Assert.False(world.HasComponent<MapFeatureComponent>(nodes[0].Entity));
    }

    [Fact]
    public void SpawnAll_creates_scenery_with_formatted_name()
    {
        using var world = new World();
        var map = new MapDefinition
        {
            MapFeatures =
            [
                new MapFeatureDefinition
                {
                    Kind = "scenery",
                    FeatureType = "nebula",
                    Position = [25, 25],
                },
            ],
        };

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        var features = world.Query<MapFeatureComponent>().ToList();
        Assert.Single(features);
        Assert.Equal(MapFeatureKind.Scenery, features[0].Component.Kind);
        Assert.Equal("Nebula", world.GetComponent<EntityNameComponent>(features[0].Entity)!.DisplayName);
        Assert.Equal(EntityDisplayKind.Scenery, GameplayEntityDisplay.Classify(world, features[0].Entity));
    }

    [Fact]
    public void ResolveSceneryAppearance_assigns_distinct_mesh_per_feature_type()
    {
        var asteroid = MapFeatureSpawner.ResolveSceneryAppearance("asteroid_field", TestMeshes);
        var nebula = MapFeatureSpawner.ResolveSceneryAppearance("nebula", TestMeshes);
        var debris = MapFeatureSpawner.ResolveSceneryAppearance("debris", TestMeshes);

        Assert.Equal(TestMeshes.AsteroidFieldMeshId, asteroid.MeshId);
        Assert.Equal(TestMeshes.AsteroidFieldVertCount, asteroid.VertexCount);
        Assert.Equal(MapFeatureSpawner.AsteroidFieldTint, asteroid.Color);

        Assert.Equal(TestMeshes.NebulaMeshId, nebula.MeshId);
        Assert.Equal(TestMeshes.NebulaVertCount, nebula.VertexCount);
        Assert.Equal(MapFeatureSpawner.NebulaTint, nebula.Color);

        Assert.Equal(TestMeshes.SceneryMeshId, debris.MeshId);
        Assert.Equal(TestMeshes.SceneryVertCount, debris.VertexCount);
        Assert.Equal(MapFeatureSpawner.DebrisTint, debris.Color);
    }

    [Fact]
    public void SpawnAll_creates_three_distinct_scenery_anomaly_types()
    {
        using var world = new World();
        var map = new MapDefinition
        {
            MapFeatures =
            [
                new MapFeatureDefinition { Kind = "scenery", FeatureType = "nebula", Position = [10, 10] },
                new MapFeatureDefinition { Kind = "scenery", FeatureType = "asteroid_field", Position = [20, 20] },
                new MapFeatureDefinition { Kind = "scenery", FeatureType = "debris", Position = [30, 30] },
                new MapFeatureDefinition { Kind = "scenery", FeatureType = "ion_storm", Position = [40, 40] },
            ],
        };

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        var scenery = world.Query<MapFeatureComponent>()
            .Where(q => q.Component.Kind == MapFeatureKind.Scenery)
            .Select(q => q.Component.FeatureType)
            .Distinct()
            .ToList();

        Assert.True(scenery.Count >= 3);
        Assert.Contains("nebula", scenery);
        Assert.Contains("asteroid_field", scenery);
        Assert.Contains("debris", scenery);

        var appearances = scenery
            .Select(t => MapFeatureSpawner.ResolveSceneryAppearance(t, TestMeshes))
            .ToList();
        Assert.Equal(appearances.Count, appearances.Select(a => a.MeshId).Distinct().Count());
    }

    [Fact]
    public void ResolveSceneryAppearance_ion_storm_and_wormhole_are_distinct()
    {
        var debris = MapFeatureSpawner.ResolveSceneryAppearance("debris", TestMeshes);
        var ionStorm = MapFeatureSpawner.ResolveSceneryAppearance("ion_storm", TestMeshes);
        var wormhole = MapFeatureSpawner.ResolveSceneryAppearance("wormhole_remnant", TestMeshes);

        Assert.Equal(TestMeshes.IonStormMeshId, ionStorm.MeshId);
        Assert.Equal(TestMeshes.WormholeRemnantMeshId, wormhole.MeshId);
        Assert.NotEqual(debris.MeshId, ionStorm.MeshId);
        Assert.NotEqual(ionStorm.MeshId, wormhole.MeshId);
        Assert.NotEqual(MapFeatureSpawner.DefaultSceneryTint, ionStorm.Color);
        Assert.NotEqual(MapFeatureSpawner.DefaultSceneryTint, wormhole.Color);
        Assert.NotEqual(ionStorm.Color, wormhole.Color);
    }

    [Fact]
    public void MapGenerator_places_multiple_anomaly_types()
    {
        var map = new MapGenerator(seed: 4242).Generate(new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            AsteroidDensity = 0.12f,
            NebulaDensity = 0.1f,
            DebrisDensity = 0.08f,
            IonStormDensity = 0.06f,
            WormholeRemnantDensity = 0.03f,
            ResourceNodeCount = 0,
            HarvestablePlanetCount = 0,
            NeutralPlanetCount = 0,
            SpawnPointCount = 0,
            ScatterSceneryFromTerrain = true,
        });

        var sceneryTypes = map.MapFeatures
            .Where(f => f.Kind == "scenery")
            .Select(f => f.FeatureType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(sceneryTypes.Count >= 3, $"Expected >=3 scenery types, got: {string.Join(", ", sceneryTypes)}");
        Assert.Contains(map.Terrain.Regions, r => r.Type == "ion_storm");
        Assert.Contains(map.Terrain.Regions, r => r.Type == "wormhole_remnant");
    }

    [Fact]
    public void SpawnAll_assigns_asteroid_and_nebula_meshes_to_scenery_entities()
    {
        using var world = new World();
        var map = new MapDefinition
        {
            MapFeatures =
            [
                new MapFeatureDefinition
                {
                    Kind = "scenery",
                    FeatureType = "asteroid_field",
                    Position = [11, 10],
                },
                new MapFeatureDefinition
                {
                    Kind = "scenery",
                    FeatureType = "nebula",
                    Position = [25, 25],
                },
            ],
        };

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        var renders = world.Query<RenderComponent>().ToList();
        Assert.Equal(2, renders.Count);

        var asteroidRender = renders.Single(r =>
            world.GetComponent<MapFeatureComponent>(r.Entity)!.FeatureType == "asteroid_field").Component;
        var nebulaRender = renders.Single(r =>
            world.GetComponent<MapFeatureComponent>(r.Entity)!.FeatureType == "nebula").Component;

        Assert.Equal(TestMeshes.AsteroidFieldMeshId, asteroidRender.MeshId);
        Assert.Equal(TestMeshes.NebulaMeshId, nebulaRender.MeshId);
        Assert.NotEqual(asteroidRender.MeshId, nebulaRender.MeshId);
        Assert.Equal(MapFeatureSpawner.AsteroidFieldTint, asteroidRender.Color);
        Assert.Equal(MapFeatureSpawner.NebulaTint, nebulaRender.Color);
    }

    [Fact]
    public void ParseResourceType_maps_known_types()
    {
        Assert.Equal(ResourceType.Minerals, MapFeatureSpawner.ParseResourceType("minerals"));
        Assert.Equal(ResourceType.Data, MapFeatureSpawner.ParseResourceType("data"));
        Assert.Equal(ResourceType.Crew, MapFeatureSpawner.ParseResourceType("crew"));
        Assert.Equal(ResourceType.Energy, MapFeatureSpawner.ParseResourceType("energy"));
    }

    [Fact]
    public void SpawnResourceNode_uses_map_gridSize_and_cellSize()
    {
        using var world = new World();
        var map = new MapDefinition
        {
            GridSize = [64, 64],
            CellSize = 1.0f,
            ResourceNodes =
            [
                new MapResourceNode { Type = "energy", Position = [32, 32], Amount = 2500 },
            ],
        };

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        var node = world.Query<ResourceNodeComponent>().Single();
        var transform = world.GetComponent<TransformComponent>(node.Entity)!;
        Vector3 expected = MapCoordinates.GridToWorld(32, 32, 64, 1.0f);

        Assert.Equal(expected.X, transform.Position.X, precision: 2);
        Assert.Equal(expected.Z, transform.Position.Z, precision: 2);
        Assert.NotEqual(-675f, transform.Position.X, precision: 1);
    }

    [Fact]
    public void SpawnAll_sector_alpha_positions_match_grid_coords()
    {
        string root = GetGameDataPath();
        var assets = new AssetManager(root);
        var map = assets.Load<MapDefinition>("Maps/sector_alpha");
        Assert.NotNull(map);

        using var world = new World();
        MapFeatureSpawner.SpawnAll(world, map!, TestMeshes);

        int gridExtent = MapCoordinates.ResolveGridExtent(map!);
        float cellSize = MapCoordinates.ResolveCellSize(map!);

        var transforms = world.Query<TransformComponent>()
            .Where(q => world.HasComponent<ResourceNodeComponent>(q.Entity))
            .Select(q => q.Component.Position)
            .ToList();

        Assert.Equal(map!.ResourceNodes.Length + 1, transforms.Count);

        foreach (var node in map.ResourceNodes)
        {
            Vector3 expected = MapCoordinates.GridToWorld(
                node.Position[0], node.Position[1], gridExtent, cellSize);
            Assert.Contains(transforms, p =>
                MathF.Abs(p.X - expected.X) < 0.5f && MathF.Abs(p.Z - expected.Z) < 0.5f);
        }

        var harvestable = map.MapFeatures.Single(f => f.Kind == "harvestable_planet");
        Vector3 planetExpected = MapCoordinates.GridToWorld(
            harvestable.Position[0], harvestable.Position[1], gridExtent, cellSize);
        Assert.Contains(transforms, p =>
            MathF.Abs(p.X - planetExpected.X) < 1f && MathF.Abs(p.Z - planetExpected.Z) < 1f);
    }

    [Fact]
    public void SpawnAll_procedural_map_spawns_planets_and_nodes()
    {
        using var world = new World();
        var map = new MapGenerator(seed: 42).Generate(new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            ResourceNodeCount = 4,
            HarvestablePlanetCount = 1,
            NeutralPlanetCount = 1,
            ScatterSceneryFromTerrain = false,
        });

        MapFeatureSpawner.SpawnAll(world, map, TestMeshes);

        int resourceNodes = world.Query<ResourceNodeComponent>().Count();
        int harvestablePlanets = map.MapFeatures.Count(f => f.Kind == "harvestable_planet");
        int neutralPlanets = world.Query<MapFeatureComponent>()
            .Count(q => q.Component.Kind == MapFeatureKind.NeutralPlanet);

        Assert.Equal(map.ResourceNodes.Length + harvestablePlanets, resourceNodes);
        Assert.Equal(map.MapFeatures.Count(f => f.Kind == "neutral_planet"), neutralPlanets);
    }

    [Fact]
    public void Sector_alpha_json_deserializes_map_features()
    {
        string root = GetGameDataPath();
        var assets = new AssetManager(root);
        var map = assets.Load<MapDefinition>("Maps/sector_alpha");

        Assert.NotNull(map);
        Assert.Equal(6, map!.MapFeatures.Length);
        Assert.Contains(map.MapFeatures, f => f.Kind == "neutral_planet" && f.Name == "Waypoint Prime");
        Assert.Contains(map.MapFeatures, f => f.Kind == "harvestable_planet");
        Assert.Contains(map.MapFeatures, f => f.Kind == "scenery" && f.FeatureType == "nebula");
    }

    private static string GetGameDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }
}