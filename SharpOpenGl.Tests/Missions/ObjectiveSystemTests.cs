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
        var player    = resources.AddPlayer(1);
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
        var player    = resources.AddPlayer(1);
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

    [Fact]
    public void Collect_minerals_completes_when_threshold_reached()
    {
        var resources = new ResourceManager();
        var player    = resources.AddPlayer(1);
        player.Add(ResourceType.Minerals, 1000f);

        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "harvest", Type = "collect", Target = "Minerals:1000",
                Description = "Gather 1000 minerals",
            }
        ], resources: resources);

        system.Update(world, 0.016f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Collect_minerals_incomplete_below_threshold()
    {
        var resources = new ResourceManager();
        var player    = resources.AddPlayer(1);
        player.Add(ResourceType.Minerals, 999f);

        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "harvest", Type = "collect", Target = "Minerals:1000"
            }
        ], resources: resources);

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    // ── construct ─────────────────────────────────────────────────────────────

    [Fact]
    public void Construct_buildings_incomplete_when_below_count()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "build_turrets",
                Type = "construct",
                Target = "defense_turret:5",
                Description = "Build 5 defense turrets",
            }
        ]);

        // 4 completed player turrets — not enough.
        for (int i = 0; i < 4; i++)
        {
            Entity e = world.CreateEntity();
            world.AddComponent(e, new BuildingComponent
            {
                BuildingType = "defense_turret",
                PlayerId = 1,
            });
        }

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Construct_buildings_completes_when_count_met()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "build_turrets",
                Type = "construct",
                Target = "defense_turret:5",
            }
        ]);

        for (int i = 0; i < 5; i++)
        {
            Entity e = world.CreateEntity();
            world.AddComponent(e, new BuildingComponent
            {
                BuildingType = "defense_turret",
                PlayerId = 1,
            });
        }

        system.Update(world, 0.016f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Construct_buildings_ignores_under_construction()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "build_turrets",
                Type = "construct",
                Target = "defense_turret:2",
            }
        ]);

        // One completed + one under construction = only 1 counts.
        Entity done = world.CreateEntity();
        world.AddComponent(done, new BuildingComponent
        {
            BuildingType = "defense_turret",
            PlayerId = 1,
        });

        Entity building = world.CreateEntity();
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = "defense_turret",
            PlayerId = 1,
        });
        world.AddComponent(building, new UnderConstructionComponent
        {
            DefinitionId = "defense_turret",
            PlayerId = 1,
            TotalBuildTime = 10f,
        });

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Construct_buildings_ignores_other_player_and_type()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "build_hq",
                Type = "construct",
                Target = "command_center:1",
            }
        ]);

        Entity enemy = world.CreateEntity();
        world.AddComponent(enemy, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 2,
        });

        Entity wrongType = world.CreateEntity();
        world.AddComponent(wrongType, new BuildingComponent
        {
            BuildingType = "power_reactor",
            PlayerId = 1,
        });

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Construct_unit_form_completes_when_player_unit_present()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "produce_fighter",
                Type = "construct",
                Target = "unit:fighter_basic:1",
                Description = "Produce one fighter",
            }
        ]);

        Entity fighter = world.CreateEntity();
        world.AddComponent(fighter, new EntityNameComponent
        {
            DefinitionId = "fighter_basic",
            DisplayName = "Fighter",
        });
        world.AddComponent(fighter, new CombatTargetComponent { Faction = 1 });
        world.AddComponent(fighter, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });

        system.Update(world, 0.016f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Construct_unit_form_incomplete_without_matching_unit()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "produce_fighter",
                Type = "construct",
                Target = "unit:fighter_basic:1",
            }
        ]);

        // Building with same definition id must not count for unit form.
        Entity building = world.CreateEntity();
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = "fighter_basic",
            PlayerId = 1,
        });
        world.AddComponent(building, new EntityNameComponent
        {
            DefinitionId = "fighter_basic",
        });
        world.AddComponent(building, new CombatTargetComponent { Faction = 1 });

        // Enemy fighter should not count.
        Entity enemy = world.CreateEntity();
        world.AddComponent(enemy, new EntityNameComponent { DefinitionId = "fighter_basic" });
        world.AddComponent(enemy, new CombatTargetComponent { Faction = 2 });
        world.AddComponent(enemy, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void Construct_completion_triggers_victory_and_positive_elapsed_time()
    {
        // Avoid hero_destroyed defeat so multi-tick ElapsedTime can accumulate.
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "build_hq",
                Type = "construct",
                Target = "command_center:1",
            }
        ],
        defeat: new EndConditionDefinition { Type = "none" });

        // Simulate mission progress before completion so ElapsedTime accumulates.
        system.Update(world, 1.5f);
        Assert.Equal(MissionPhase.InProgress, state.Phase);
        Assert.Equal(1.5f, state.ElapsedTime, precision: 3);

        Entity hq = world.CreateEntity();
        world.AddComponent(hq, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        MissionVictoryEvent? evt = null;
        bus.Subscribe<MissionVictoryEvent>(e => evt = e);

        system.Update(world, 0.5f);

        Assert.NotNull(evt);
        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.True(state.ElapsedTime > 0f);
        Assert.Equal(2.0f, state.ElapsedTime, precision: 3);
        Assert.True(state.PrimaryObjectives[0].IsCompleted);
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

    // ── repair_target ─────────────────────────────────────────────────────────

    [Fact]
    public void RepairTarget_completes_when_tagged_entity_reaches_threshold()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "salvage",
                Type = "repair_target",
                Target = "derelict_1",
                Condition = "healthPercent >= 0.50",
            }
        ]);

        Entity derelict = world.CreateEntity();
        world.AddComponent(derelict, new HealthComponent { MaxHP = 100f, CurrentHP = 55f });
        state.EntityTags["derelict_1"] = derelict;

        system.Update(world, 0.016f);

        Assert.True(state.PrimaryObjectives[0].IsCompleted);
    }

    [Fact]
    public void RepairTarget_incomplete_below_threshold()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "salvage",
                Type = "repair_target",
                Target = "derelict_1",
            }
        ]);

        Entity derelict = world.CreateEntity();
        world.AddComponent(derelict, new HealthComponent { MaxHP = 100f, CurrentHP = 40f });
        state.EntityTags["derelict_1"] = derelict;

        system.Update(world, 0.016f);

        Assert.False(state.PrimaryObjectives[0].IsCompleted);
    }

    // ── unit_destroyed defeat ─────────────────────────────────────────────────

    [Fact]
    public void Defeat_event_fired_when_tagged_unit_destroyed()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "survive", Type = "survive_time", Target = "9999"
            }
        ],
        defeat: new EndConditionDefinition
        {
            Type = "unit_destroyed",
            Target = "player_support",
        });

        Entity support = world.CreateEntity();
        world.AddComponent(support, new HealthComponent { MaxHP = 100f, CurrentHP = 0f });
        state.EntityTags["player_support"] = support;
        world.DestroyEntity(support);

        MissionDefeatEvent? evt = null;
        bus.Subscribe<MissionDefeatEvent>(e => evt = e);

        system.Update(world, 0.016f);

        Assert.NotNull(evt);
        Assert.Equal(MissionPhase.Defeat, state.Phase);
    }

    [Fact]
    public void No_defeat_when_tagged_unit_alive()
    {
        var (state, bus, system, world) = Setup(
        [
            new ObjectiveDefinition
            {
                Id = "survive", Type = "survive_time", Target = "9999"
            }
        ],
        defeat: new EndConditionDefinition
        {
            Type = "unit_destroyed",
            Target = "player_support",
        });

        Entity support = world.CreateEntity();
        world.AddComponent(support, new HealthComponent { MaxHP = 100f, CurrentHP = 80f });
        state.EntityTags["player_support"] = support;

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
