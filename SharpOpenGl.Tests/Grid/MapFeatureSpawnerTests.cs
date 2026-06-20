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
        SceneryMeshId = 2,
        SceneryVertCount = 36,
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
    public void ParseResourceType_maps_known_types()
    {
        Assert.Equal(ResourceType.Minerals, MapFeatureSpawner.ParseResourceType("minerals"));
        Assert.Equal(ResourceType.Data, MapFeatureSpawner.ParseResourceType("data"));
        Assert.Equal(ResourceType.Crew, MapFeatureSpawner.ParseResourceType("crew"));
        Assert.Equal(ResourceType.Energy, MapFeatureSpawner.ParseResourceType("energy"));
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