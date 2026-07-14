using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ShipyardPreviewTests
{
    private static EntityDefinition FighterDef(float buildTime = 10f) => new()
    {
        Id = "fighter_basic",
        Category = "fighter",
        BuildTime = buildTime,
        Components = new ComponentsDefinition
        {
            Movement = new MovementDefinition { Speed = 80f, Acceleration = 120f, TurnRate = 180f },
        },
    };

    private static (World world, ShipyardPreviewSystem previewSystem, Entity building) CreateShipyardWorld(
        string buildingType = "shipyard_small",
        float buildProgress = 5f,
        float buildTime = 10f)
    {
        var world = new World();
        var def = FighterDef(buildTime);
        var factory = new UnitFactory();
        world.AddSystem(new BuildSystem(factory, _ => def));
        var previewSystem = new ShipyardPreviewSystem(_ => def)
        {
            ResolveFactionRaceId = _ => "terran",
        };
        world.AddSystem(previewSystem);

        var building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = Vector3.Zero });
        var buildingComp = new BuildingComponent
        {
            BuildingType = buildingType,
            ProductionRate = 1f,
            RallyPoint = new Vector3(100f, 0f, 0f),
            PlayerId = 1,
            BuildProgress = buildProgress,
        };
        buildingComp.BuildQueue.Enqueue("fighter_basic");
        world.AddComponent(building, buildingComp);

        return (world, previewSystem, building);
    }

    [Fact]
    public void Preview_appears_when_build_progress_positive()
    {
        var (world, _, building) = CreateShipyardWorld(buildProgress: 2f);

        world.Update(0.016f);

        var preview = world.GetComponent<ShipyardPreviewComponent>(building);
        Assert.NotNull(preview);
        Assert.True(preview.IsActive);
        Assert.NotEqual(Entity.Null, preview.PreviewEntity);
        Assert.True(world.IsAlive(preview.PreviewEntity));
        Assert.Equal("fighter_basic", preview.QueuedDefinitionId);
    }

    [Fact]
    public void Preview_removed_when_queue_empty()
    {
        var (world, _, building) = CreateShipyardWorld(buildProgress: 2f);
        world.Update(0.016f);

        var buildingComp = world.GetComponent<BuildingComponent>(building)!;
        while (buildingComp.BuildQueue.Count > 0)
            buildingComp.BuildQueue.Dequeue();
        buildingComp.BuildProgress = 0f;

        world.Update(0.016f);

        var preview = world.GetComponent<ShipyardPreviewComponent>(building);
        Assert.NotNull(preview);
        Assert.False(preview.IsActive);
        Assert.Equal(Entity.Null, preview.PreviewEntity);
    }

    [Fact]
    public void Preview_fraction_tracks_build_progress()
    {
        const float buildTime = 20f;
        var (world, _, building) = CreateShipyardWorld(buildProgress: 8f, buildTime: buildTime);

        world.Update(0.016f);

        var preview = world.GetComponent<ShipyardPreviewComponent>(building);
        Assert.NotNull(preview);
        Assert.Equal(0.4f, preview.BuildFraction, 2);
    }

    [Fact]
    public void Preview_mesh_vertex_count_increases_with_fraction()
    {
        const string raceId = "terran";
        const string hullKey = "fighter_basic";

        float[] early = ShipyardBuildPreviewMeshes.BuildPartial(raceId, hullKey, 0.30f);
        float[] late = ShipyardBuildPreviewMeshes.BuildPartial(raceId, hullKey, 0.80f);

        int earlyVerts = ProceduralMeshes.VertexCount(early);
        int lateVerts = ProceduralMeshes.VertexCount(late);

        Assert.True(earlyVerts > 0);
        Assert.True(lateVerts > earlyVerts);
    }

    [Theory]
    [InlineData("shipyard_small")]
    [InlineData("shipyard_medium")]
    [InlineData("shipyard_large")]
    [InlineData("shipyard")]
    public void Preview_active_for_all_shipyard_sizes(string buildingType)
    {
        var (world, _, building) = CreateShipyardWorld(buildingType, buildProgress: 3f);

        world.Update(0.016f);

        var preview = world.GetComponent<ShipyardPreviewComponent>(building);
        Assert.NotNull(preview);
        Assert.True(preview.IsActive);
        Assert.True(world.IsAlive(preview.PreviewEntity));
    }

    [Fact]
    public void Preview_entity_excluded_from_selection_components()
    {
        var (world, _, building) = CreateShipyardWorld(buildProgress: 4f);
        world.Update(0.016f);

        var preview = world.GetComponent<ShipyardPreviewComponent>(building);
        Assert.NotNull(preview);

        Assert.True(world.HasComponent<ShipyardPreviewTagComponent>(preview.PreviewEntity));
        Assert.False(world.HasComponent<SelectionComponent>(preview.PreviewEntity));
        Assert.False(world.HasComponent<AIControlledComponent>(preview.PreviewEntity));
        Assert.False(world.HasComponent<SightRadiusComponent>(preview.PreviewEntity));
        Assert.False(world.HasComponent<BuildingComponent>(preview.PreviewEntity));
    }
}