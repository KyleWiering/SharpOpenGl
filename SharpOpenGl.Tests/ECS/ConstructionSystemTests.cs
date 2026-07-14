using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ConstructionSystemTests
{
    private static EntityDefinition StructureDef(
        string id = "power_reactor",
        float buildTime = 30f,
        float productionRate = 1.5f) => new()
    {
        Id = id,
        Category = "building",
        BuildTime = buildTime,
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 1000f, Armor = 50f },
            Building = new BuildingDefinition
            {
                BuildingType = id,
                ProductionRate = productionRate,
                Footprint = [2, 2],
            },
            SightRadius = 12,
        },
    };

    private static (World world, Entity building) CreateUnderConstructionBuilding(
        float progress = 0f,
        float totalTime = 30f,
        float productionRate = 0f)
    {
        var world = new World();
        world.AddSystem(new ConstructionSystem(_ => StructureDef()));

        var building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = "power_reactor",
            ProductionRate = productionRate,
            PlayerId = 1,
        });
        world.AddComponent(building, new UnderConstructionComponent
        {
            DefinitionId = "power_reactor",
            BuildProgress = progress,
            TotalBuildTime = totalTime,
            PlayerId = 1,
        });

        return (world, building);
    }

    [Fact]
    public void Progress_advances_each_tick()
    {
        var (world, building) = CreateUnderConstructionBuilding();
        var underConstruction = world.GetComponent<UnderConstructionComponent>(building)!;
        float before = underConstruction.BuildProgress;

        world.Update(2.5f);

        underConstruction = world.GetComponent<UnderConstructionComponent>(building)!;
        Assert.Equal(before + 2.5f, underConstruction.BuildProgress, 0.001f);
    }

    [Fact]
    public void Completes_when_progress_reaches_total()
    {
        var (world, building) = CreateUnderConstructionBuilding(progress: 28f, totalTime: 30f);
        var buildingComp = world.GetComponent<BuildingComponent>(building)!;
        buildingComp.ProductionRate = 0f;

        world.Update(2.5f);

        Assert.Null(world.GetComponent<UnderConstructionComponent>(building));
        buildingComp = world.GetComponent<BuildingComponent>(building)!;
        Assert.True(buildingComp.ProductionRate > 0f);
    }

    [Fact]
    public void Instant_path_skips_under_construction()
    {
        var world = new World();
        var def = StructureDef(buildTime: 0f);
        world.AddSystem(new ConstructionSystem(_ => def));

        var building = world.CreateEntity();
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = "power_reactor",
            ProductionRate = def.Components!.Building!.ProductionRate,
            PlayerId = 1,
        });

        world.Update(1f);

        Assert.Null(world.GetComponent<UnderConstructionComponent>(building));
    }

    [Fact]
    public void BuildSystem_skips_under_construction()
    {
        var world = new World();
        var unitDef = new EntityDefinition
        {
            Id = "fighter_basic",
            Category = "fighter",
            BuildTime = 10f,
            Components = new ComponentsDefinition
            {
                Movement = new MovementDefinition { Speed = 50f, Acceleration = 80f, TurnRate = 120f },
            },
        };

        world.AddSystem(new BuildSystem(new UnitFactory(), _ => unitDef));

        var building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = Vector3.Zero });
        var buildingComp = new BuildingComponent
        {
            BuildingType = "shipyard_small",
            ProductionRate = 10f,
            RallyPoint = new Vector3(100f, 0f, 0f),
            PlayerId = 1,
            BuildProgress = 3f,
        };
        buildingComp.BuildQueue.Enqueue("fighter_basic");
        world.AddComponent(building, buildingComp);
        world.AddComponent(building, new UnderConstructionComponent
        {
            DefinitionId = "shipyard_small",
            BuildProgress = 5f,
            TotalBuildTime = 40f,
            PlayerId = 1,
        });

        world.Update(1f);

        buildingComp = world.GetComponent<BuildingComponent>(building)!;
        Assert.Equal(3f, buildingComp.BuildProgress, 0.001f);
        Assert.Single(buildingComp.BuildQueue);
    }

    [Fact]
    public void GetBuiltTypes_excludes_incomplete()
    {
        var world = new World();

        Entity complete = world.CreateEntity();
        world.AddComponent(complete, new BuildingComponent
        {
            BuildingType = "power_reactor",
            PlayerId = 1,
        });

        Entity incomplete = world.CreateEntity();
        world.AddComponent(incomplete, new BuildingComponent
        {
            BuildingType = "resource_refinery",
            PlayerId = 1,
        });
        world.AddComponent(incomplete, new UnderConstructionComponent
        {
            DefinitionId = "resource_refinery",
            BuildProgress = 10f,
            TotalBuildTime = 35f,
            PlayerId = 1,
        });

        var built = BuildingFootprint.GetBuiltTypes(world, playerId: 1);

        Assert.Contains("power_reactor", built);
        Assert.DoesNotContain("resource_refinery", built);
    }
}