using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MinimapFeatureBindingTests
{
    [Fact]
    public void Collect_includes_discovered_resource_node_and_neutral_planet()
    {
        using var world = new World();

        var resource = world.CreateEntity();
        world.AddComponent(resource, new TransformComponent { Position = new Vector3(10f, 0f, 20f) });
        world.AddComponent(resource, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Energy,
            Amount = 1000f,
            MaxAmount = 1000f,
        });

        var neutral = world.CreateEntity();
        world.AddComponent(neutral, new TransformComponent { Position = new Vector3(-5f, 0f, 8f) });
        world.AddComponent(neutral, new MapFeatureComponent { Kind = MapFeatureKind.NeutralPlanet });

        var markers = MinimapFeatureBinder.Collect(
            world,
            _ => FogState.Explored,
            pos => new Vector2(pos.X, pos.Z));

        Assert.Equal(2, markers.Count);
        Assert.Contains(markers, m => m.Kind == MinimapFeatureKind.ResourceEnergy);
        Assert.Contains(markers, m => m.Kind == MinimapFeatureKind.NeutralPlanet);
    }

    [Fact]
    public void Collect_skips_unexplored_entities()
    {
        using var world = new World();

        var hidden = world.CreateEntity();
        world.AddComponent(hidden, new TransformComponent { Position = new Vector3(30f, 0f, 30f) });
        world.AddComponent(hidden, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 500f,
            MaxAmount = 500f,
        });

        var markers = MinimapFeatureBinder.Collect(
            world,
            _ => FogState.Unexplored,
            pos => new Vector2(pos.X, pos.Z));

        Assert.Empty(markers);
    }

    [Fact]
    public void Collect_maps_harvestable_planet_to_planet_marker()
    {
        using var world = new World();

        var planet = world.CreateEntity();
        world.AddComponent(planet, new TransformComponent { Position = new Vector3(0f, 0f, 0f) });
        world.AddComponent(planet, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 8000f,
            MaxAmount = 8000f,
        });
        world.AddComponent(planet, new EntityNameComponent { DefinitionId = "harvestable_planet" });

        var markers = MinimapFeatureBinder.Collect(
            world,
            _ => FogState.Visible,
            pos => new Vector2(pos.X, pos.Z));

        Assert.Single(markers);
        Assert.Equal(MinimapFeatureKind.HarvestablePlanet, markers[0].Kind);
        Assert.Equal(GameplayEntityDisplay.HarvestableColor, markers[0].Color);
    }
}