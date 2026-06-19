using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class BuildSystemSpawnTests
{
    [Fact]
    public void OnUnitSpawned_fires_when_build_completes()
    {
        var world = new World();
        var factory = new UnitFactory();
        var def = new EntityDefinition
        {
            Id = "fighter_basic",
            Category = "fighter",
            BuildTime = 0.1f,
            Components = new ComponentsDefinition
            {
                Movement = new MovementDefinition { Speed = 50f, Acceleration = 80f, TurnRate = 120f },
            },
        };

        var system = new BuildSystem(factory, _ => def);
        bool fired = false;
        system.OnUnitSpawned = (_, _, _, _, _) => fired = true;
        world.AddSystem(system);

        var building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = Vector3.Zero });
        var buildingComp = new BuildingComponent
        {
            BuildingType = "shipyard",
            ProductionRate = 10f,
            RallyPoint = new Vector3(100f, 0f, 0f),
            PlayerId = 1,
        };
        buildingComp.BuildQueue.Enqueue("fighter_basic");
        world.AddComponent(building, buildingComp);

        world.Update(1f);

        Assert.True(fired);
    }

    [Fact]
    public void Spawned_unit_exits_toward_rally_point()
    {
        var world = new World();
        var factory = new UnitFactory();
        var def = new EntityDefinition
        {
            Id = "fighter_basic",
            Category = "fighter",
            BuildTime = 0.1f,
            Components = new ComponentsDefinition
            {
                Movement = new MovementDefinition { Speed = 50f, Acceleration = 80f, TurnRate = 120f },
            },
        };

        var system = new BuildSystem(factory, _ => def);
        world.AddSystem(system);

        var building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = Vector3.Zero });
        var buildingComp = new BuildingComponent
        {
            BuildingType = "shipyard",
            ProductionRate = 10f,
            RallyPoint = new Vector3(100f, 0f, 0f),
            PlayerId = 1,
        };
        buildingComp.BuildQueue.Enqueue("fighter_basic");
        world.AddComponent(building, buildingComp);

        world.Update(1f);

        Entity spawned = world.Query<MovementComponent>().First(p => p.Entity != building).Entity;
        var tf = world.GetComponent<TransformComponent>(spawned);
        var movement = world.GetComponent<MovementComponent>(spawned);

        Assert.NotNull(tf);
        Assert.True(tf!.Position.Length > 20f);
        Assert.Equal(new Vector3(100f, 0f, 0f), movement!.PathTarget);
    }
}