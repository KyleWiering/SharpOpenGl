using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>Unit tests for <see cref="ObjectiveSystem"/>.</summary>
public class ObjectiveSystemTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (MissionState state, EventBus bus, ObjectiveSystem system, World world)
        Setup(ObjectiveDefinition[] primary, ObjectiveDefinition[]? secondary = null,
              EndConditionDefinition? victory = null, EndConditionDefinition? defeat = null,
              ResourceManager? resources = null)
    {
        var def = new MissionDefinition
        {
            Id         = "test",
            Map        = "test_map",
            Objectives = new ObjectivesDefinition
            {
                Primary   = primary,
                Secondary = secondary ?? [],
            },
            Triggers = [],
            Victory  = victory ?? new EndConditionDefinition { Type = "all_primary_complete" },
            Defeat   = defeat  ?? new EndConditionDefinition { Type = "hero_destroyed" },
        };

        var state  = new MissionState(def) { Phase = MissionPhase.InProgress };
        var bus    = new EventBus();
        var system = new ObjectiveSystem(state, bus, resources);
        var world  = new World();

        return (state, bus, system, world);
    }

    // ── destroy_target ────────────────────────────────────────────────────────

    [Fact]
    public void DestroyTarget_incomplete_when_entity_not_in_registry()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "kill_it", Type = "destroy_target", Target = "enemy_1"
            }
        ]);

        // No entity registered yet → objective should stay incomplete.
        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void DestroyTarget_incomplete_while_entity_alive()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "kill_it", Type = "destroy_target", Target = "enemy_1"
            }
        ]);

        Entity enemy = world.CreateEntity();
        state.EntityTags["enemy_1"] = enemy;

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void DestroyTarget_completes_when_tagged_entity_destroyed()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "kill_it", Type = "destroy_target", Target = "enemy_1"
            }
        ]);

        Entity enemy = world.CreateEntity();
        state.EntityTags["enemy_1"] = enemy;

        world.DestroyEntity(enemy);
        system.Update(world, 0.016f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    // ── survive_time ──────────────────────────────────────────────────────────

    [Fact]
    public void SurviveTime_incomplete_before_duration()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "survive", Type = "survive_time", Target = "10"
            }
        ]);

        system.Update(world, 5f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void SurviveTime_completes_after_duration()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "survive", Type = "survive_time", Target = "10"
            }
        ]);

        system.Update(world, 11f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    // ── collect ───────────────────────────────────────────────────────────────

    [Fact]
    public void Collect_completes_when_resource_threshold_reached()
    {
        var resources = new ResourceManager();
        var player    = resources.AddPlayer(0);
        player.Add(ResourceType.Energy, 600f);

        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "gather", Type = "collect", Target = "energy:500"
            }
        ], resources: resources);

        system.Update(world, 0.016f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Collect_incomplete_when_below_threshold()
    {
        var resources = new ResourceManager();
        var player    = resources.AddPlayer(0);
        player.Add(ResourceType.Energy, 100f);

        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "gather", Type = "collect", Target = "energy:500"
            }
        ], resources: resources);

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    [Fact]
    public void ObjectiveChanged_event_fired_on_completion()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "kill_it", Type = "destroy_target", Target = "enemy_1"
            }
        ]);

        Entity enemy = world.CreateEntity();
        state.EntityTags["enemy_1"] = enemy;
        world.DestroyEntity(enemy);

         → completes immediately.
        ObjectiveChangedEvent? evt = null;
        bus.Subscribe<ObjectiveChangedEvent>(e => evt = e);

        system.Update(world, 0.016f);

        Assert.NotNull(evt);
        Assert.Equal("kill_it", evt!.ObjectiveId);
        Assert.True(evt.Completed);
    }

    // ── Victory / Defeat ──────────────────────────────────────────────────────

    [Fact]
    public void Victory_event_fired_when_all_primary_complete()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "kill_it", Type = "destroy_target", Target = "enemy_1"
            }
        ]);

        Entity enemy = world.CreateEntity();
        state.EntityTags["enemy_1"] = enemy;
        world.DestroyEntity(enemy);

        MissionVictoryEvent? evt = null;
        bus.Subscribe<MissionVictoryEvent>(e => evt = e);

        system.Update(world, 0.016f); // completes objective → triggers victory check

        Assert.NotNull(evt);
        Assert.Equal("test", evt!.MissionId);
        Assert.Equal(MissionPhase.Victory, state.Phase);
    }

    [Fact]
    public void Defeat_event_fired_when_hero_destroyed()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "survive", Type = "survive_time", Target = "9999"
            }
        ]);

        // No hero entity in world → defeat condition met.
        MissionDefeatEvent? evt = null;
        bus.Subscribe<MissionDefeatEvent>(e => evt = e);

        system.Update(world, 0.016f);

        Assert.NotNull(evt);
        Assert.Equal(MissionPhase.Defeat, state.Phase);
    }

    [Fact]
    public void No_defeat_event_when_hero_alive()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "survive", Type = "survive_time", Target = "9999"
            }
        ]);

        // Add a hero entity.
        Entity hero = world.CreateEntity();
        world.AddComponent(hero, new HeroComponent());

        MissionDefeatEvent? evt = null;
        bus.Subscribe<MissionDefeatEvent>(e => evt = e);

        system.Update(world, 0.016f);

        Assert.Null(evt);
        Assert.Equal(MissionPhase.InProgress, state.Phase);
    }

    // ── Phase guard ───────────────────────────────────────────────────────────

    [Fact]
    public void System_does_not_update_when_not_InProgress()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "survive", Type = "survive_time", Target = "1"
            }
        ]);

        state.Phase = MissionPhase.Victory; // already won

        system.Update(world, 5f); // more than enough time

        // Elapsed time on objective should remain 0 because system skipped.
        Assert.Equal(0f, state.PrimaryObjectives[0].ElapsedTime);
    }
}
