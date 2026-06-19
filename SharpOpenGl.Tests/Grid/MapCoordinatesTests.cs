using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class MapCoordinatesTests
{
    [Fact]
    public void GridToWorld_centers_player_spawn_cell()
    {
        Vector3 world = MapCoordinates.GridToWorld(3, 3);
        Assert.Equal(-965f, world.X, precision: 1);
        Assert.Equal(0f, world.Y, precision: 3);
        Assert.Equal(-965f, world.Z, precision: 1);
    }

    [Fact]
    public void GridToWorld_centers_sector_midpoint()
    {
        Vector3 world = MapCoordinates.GridToWorld(32, 32);
        Assert.Equal(-675f, world.X, precision: 1);
        Assert.Equal(-675f, world.Z, precision: 1);
    }

    [Fact]
    public void TryParseReachArea_converts_grid_condition_to_world()
    {
        bool ok = MapCoordinates.TryParseReachArea("32,32,5", out Vector3 center, out float radius);
        Assert.True(ok);
        Assert.Equal(-675f, center.X, precision: 1);
        Assert.Equal(-675f, center.Z, precision: 1);
        Assert.Equal(5f, radius);
    }

    [Fact]
    public void Reach_area_objective_completes_at_converted_world_position()
    {
        var def = new MissionDefinition
        {
            Id = "test",
            Map = "test_map",
            Objectives = new ObjectivesDefinition
            {
                Primary =
                [
                    new ObjectiveDefinition
                    {
                        Id = "reach",
                        Type = "reach_area",
                        Position = [32f, 32f],
                        Radius = 10f,
                    }
                ],
                Secondary = [],
            },
            Triggers = [],
            Victory = new EndConditionDefinition { Type = "all_primary_complete" },
            Defeat = new EndConditionDefinition { Type = "hero_destroyed" },
        };

        var state = new MissionState(def) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var system = new ObjectiveSystem(state, bus);
        var world = new World();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(32, 32),
        });

        system.Update(world, 0.016f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Area_enter_trigger_fires_at_converted_world_position()
    {
        var trigger = new TriggerDefinition
        {
            Id = "enemy_patrol",
            Condition = new TriggerConditionDefinition
            {
                Type = "area_enter",
                Position = [30f, 30f],
                Radius = 10f,
            },
            Actions = [],
            OneShot = true,
        };

        var def = new MissionDefinition
        {
            Id = "test",
            Map = "test_map",
            Triggers = [trigger],
            Objectives = new ObjectivesDefinition { Primary = [], Secondary = [] },
            Victory = new EndConditionDefinition { Type = "all_primary_complete" },
            Defeat = new EndConditionDefinition { Type = "hero_destroyed" },
        };

        var state = new MissionState(def) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var system = new TriggerSystem(state, bus);
        var world = new World();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(30, 30),
        });

        system.Update(world, 0.016f);

        Assert.True(state.Triggers[0].HasFired);
    }
}