using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

public class MissionPlaythroughTests
{
    private static string GetTestDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }

    private static GameCommandContext CreateContext(World world, MissionState? state = null)
    {
        return new GameCommandContext
        {
            World = world,
            PlayerId = 1,
            MissionState = state,
        };
    }

    // ── Script parsing ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("tutorial_01")]
    [InlineData("example_scenario")]
    [InlineData("mission_02")]
    [InlineData("mission_03")]
    [InlineData("mission_04")]
    [InlineData("mission_05")]
    public void Campaign_missions_parse_demoScript_with_move_and_combat_steps(string missionId)
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load(missionId);

        Assert.NotNull(mission);
        Assert.True(mission.DemoScript.Length >= 8);

        bool hasMove = mission.DemoScript.Any(s =>
            s.Type is "move_to" or "attack_move" or "camera_pan");
        bool hasCombat = mission.DemoScript.Any(s =>
            s.Type is "attack_move" or "attack_target");

        Assert.True(hasMove);
        Assert.True(hasCombat);
    }

    [Fact]
    public void Example_scenario_demoScript_includes_briefing_travel_combat_and_victory_beats()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("example_scenario")!;

        Assert.Contains(mission.DemoScript, s => s.Type == "camera_pan");
        Assert.Contains(mission.DemoScript, s => s.Type == "wait");
        Assert.Contains(mission.DemoScript, s => s.Type == "attack_move");
        Assert.Contains(mission.DemoScript, s => s.Type == "attack_target");
        Assert.Contains(mission.DemoScript, s => s.Type == "harvest");
        Assert.Contains(mission.DemoScript, s => s.Type == "move_to");
        Assert.DoesNotContain(mission.DemoScript, s => s.Type == "wait_objective");
        Assert.DoesNotContain(mission.DemoScript, s => s.Type == "wait_for_construction");
        Assert.Contains(mission.DemoScript, s => s.Type == "place_building" && s.BuildingId == "power_reactor");
        Assert.Contains(mission.DemoScript, s => s.Type == "place_building" && s.BuildingId == "shipyard_small");
        Assert.Contains(mission.DemoScript, s => s.Type == "build_unit");
        Assert.Contains(mission.StartConditions.StartingUnits, u => u == "miner_basic");
    }

    [Fact]
    public void Example_scenario_demoScript_wait_sum_is_ci_watchable()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("example_scenario")!;

        float waitSum = mission.DemoScript
            .Where(s => s.Type == "wait")
            .Sum(s => s.Seconds);
        float harvestSum = mission.DemoScript
            .Where(s => s.Type == "harvest")
            .Sum(s => s.Seconds);

        // Multi-act fixed waits + harvest; at 2× sim ≈ wait/2 + hold ≪ DemoMaxDurationSeconds 40
        Assert.True(waitSum >= 20f, $"example_scenario wait sum {waitSum} is too short for a watchable multi-act demo.");
        Assert.True(waitSum <= 32f, $"example_scenario wait sum {waitSum} exceeds CI watchable cap of 32 sim-seconds.");
        Assert.True(harvestSum >= 2f, "demo should include a harvest beat of at least 2 sim-seconds.");
        Assert.DoesNotContain(mission.DemoScript, s => s.Type == "wait_objective");
        Assert.DoesNotContain(mission.DemoScript, s => s.Type == "wait_for_construction");
        Assert.Contains(mission.DemoScript, s => s.Type == "attack_move");
        Assert.Contains(mission.DemoScript, s => s.Type == "attack_target");
        Assert.Contains(mission.DemoScript, s => s.Type == "place_building" && s.BuildingId == "power_reactor");
        Assert.Contains(mission.DemoScript, s => s.Type == "place_building" && s.BuildingId == "shipyard_small");
        Assert.Contains(mission.DemoScript, s => s.Type == "build_unit");
        Assert.Contains(mission.DemoScript, s => s.Type == "camera_pan");
        Assert.True(mission.DemoScript.Count(s => s.Type == "camera_pan") >= 5,
            "multi-act demo should pan the camera across fleet, combat, harvest, base, and close.");
    }

    [Fact]
    public void Example_scenario_place_building_steps_follow_prerequisite_order()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("example_scenario")!;

        var placeSteps = mission.DemoScript
            .Where(s => s.Type == "place_building")
            .ToArray();

        Assert.Equal(2, placeSteps.Length);
        Assert.Equal("power_reactor", placeSteps[0].BuildingId);
        Assert.Equal(96f, placeSteps[0].Position![0]);
        Assert.Equal(92f, placeSteps[0].Position[1]);
        Assert.Equal("shipyard_small", placeSteps[1].BuildingId);
        Assert.Equal(99f, placeSteps[1].Position![0]);
        Assert.Equal(95f, placeSteps[1].Position[1]);
    }

    // ── mission_abandoned_salvage ─────────────────────────────────────────────

    [Fact]
    public void Abandoned_salvage_demoScript_includes_repair_and_move_steps()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("mission_abandoned_salvage")!;

        Assert.True(mission.DemoScript.Length >= 10);
        Assert.Contains(mission.DemoScript, s => s.Type == "camera_pan");
        Assert.Contains(mission.DemoScript, s => s.Type == "move_to");
        Assert.Contains(mission.DemoScript, s => s.Type == "repair_target" && s.TargetTag == "derelict_1");
        Assert.Contains(mission.DemoScript, s => s.Type == "repair_target" && s.TargetTag == "derelict_2");
        Assert.Contains(mission.DemoScript, s => s.Type == "wait_objective" && s.ObjectiveId == "repair_derelict_1");
        Assert.Contains(mission.DemoScript, s => s.Type == "wait_objective" && s.ObjectiveId == "repair_derelict_2");
        Assert.Contains(mission.DemoScript, s => s.Type == "select_units" && s.Filter == "support_repair");
    }

    [Fact]
    public void Abandoned_salvage_loads_primary_repair_objectives()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("mission_abandoned_salvage")!;

        Assert.NotNull(mission.Objectives);
        Assert.Equal(4, mission.Objectives.Primary.Length);

        var repairObjectives = mission.Objectives.Primary
            .Where(o => o.Type == "repair_target")
            .ToArray();
        Assert.Equal(2, repairObjectives.Length);
        Assert.Equal("derelict_1", repairObjectives[0].Target);
        Assert.Equal("derelict_2", repairObjectives[1].Target);
    }

    [Fact]
    public void Abandoned_salvage_spawn_positions_are_demo_safe()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("mission_abandoned_salvage")!;

        Assert.Equal(new[] { 8, 12 }, mission.StartConditions!.PlayerSpawn);

        var spawnTrigger = mission.Triggers.First(t => t.Id == "spawn_derelicts");
        var derelictSpawns = spawnTrigger.Actions
            .Where(a => a.Type == "spawn_units")
            .ToArray();
        Assert.Equal(2, derelictSpawns.Length);
        Assert.Equal(new[] { 28, 18 }, derelictSpawns[0].Position);
        Assert.Equal(new[] { 44, 30 }, derelictSpawns[1].Position);
    }

    // ── mission_build_tree ────────────────────────────────────────────────────

    [Fact]
    public void Mission_build_tree_demoScript_includes_tiered_place_building_steps()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("mission_build_tree")!;

        var placeSteps = mission.DemoScript
            .Where(s => s.Type == "place_building")
            .ToArray();

        Assert.True(placeSteps.Length >= 15);
        Assert.Contains(placeSteps, s => s.BuildingId == "power_reactor");
        Assert.Contains(placeSteps, s => s.BuildingId == "shipyard_small");
        Assert.Contains(placeSteps, s => s.BuildingId == "orbital_uplink");
        Assert.Contains(placeSteps, s => s.BuildingId == "fortress_core");
    }

    [Fact]
    public void Mission_build_tree_place_building_order_respects_prerequisite_chain()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("mission_build_tree")!;
        var catalog = CreateBuildMapCatalog();

        var built = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "command_center" };

        foreach (var step in mission.DemoScript.Where(s => s.Type == "place_building"))
        {
            Assert.False(string.IsNullOrWhiteSpace(step.BuildingId));
            if (step.BuildingId!.Equals("command_center", StringComparison.OrdinalIgnoreCase))
                continue;

            var prerequisites = catalog.GetPrerequisites(step.BuildingId);
            Assert.True(
                BuildMapCatalog.IsUnlocked(prerequisites, built),
                $"Building '{step.BuildingId}' is not unlocked with built set [{string.Join(", ", built)}].");

            built.Add(step.BuildingId);
        }
    }

    [Fact]
    public void Mission_build_tree_demoScript_has_builder_selection_beat()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("mission_build_tree")!;

        Assert.Contains(
            mission.DemoScript,
            s => s.Type == "select_units" && s.Filter == "support_repair");
    }

    [Fact]
    public void Mission_build_tree_demo_covers_all_build_map_ids()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("mission_build_tree")!;
        var config = LoadBuildMapConfig();

        var allBuildMapIds = config.Categories
            .SelectMany(c => c.Buildings)
            .Select(b => b.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var demoPlacementIds = mission.DemoScript
            .Where(s => s.Type == "place_building")
            .Select(s => s.BuildingId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        demoPlacementIds.Add("command_center");

        Assert.Equal(allBuildMapIds, demoPlacementIds);
    }

    [Fact]
    public void Campaign_mission_prerequisite_chain_unchanged()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));

        Assert.Equal("tutorial_01", loader.Load("mission_02")!.PrerequisiteMissionId);
        Assert.Equal("mission_02", loader.Load("mission_03")!.PrerequisiteMissionId);
        Assert.Equal("mission_03", loader.Load("mission_04")!.PrerequisiteMissionId);
        Assert.Equal("mission_04", loader.Load("mission_05")!.PrerequisiteMissionId);
        Assert.Null(loader.Load("mission_build_tree")!.PrerequisiteMissionId);
    }

    [Fact]
    public void Agent_executes_repair_target_step_and_waits_for_objective()
    {
        var def = new MissionDefinition
        {
            Id = "repair_agent",
            Objectives = new ObjectivesDefinition
            {
                Primary =
                [
                    new ObjectiveDefinition
                    {
                        Id = "repair_derelict_1",
                        Type = "repair_target",
                        Target = "derelict_1",
                        Condition = "healthPercent >= 0.50",
                    },
                    new ObjectiveDefinition
                    {
                        Id = "hold_position",
                        Type = "survive_time",
                        Target = "9999",
                    },
                ],
            },
            DemoScript =
            [
                new DemoScriptStepDefinition { Type = "select_units", Filter = "support_repair" },
                new DemoScriptStepDefinition { Type = "repair_target", TargetTag = "derelict_1" },
                new DemoScriptStepDefinition { Type = "wait_objective", ObjectiveId = "repair_derelict_1" },
                new DemoScriptStepDefinition { Type = "wait", Seconds = 0.1f },
            ],
            Triggers = [],
            Victory = new EndConditionDefinition { Type = "all_primary_complete" },
            Defeat = new EndConditionDefinition { Type = "unit_destroyed", Target = "player_support" },
        };

        var state = new MissionState(def) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);
        var repairSystem = new RepairSystem(bus) { PlayerFaction = 1 };

        Entity repairer = world.CreateEntity();
        world.AddComponent(repairer, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(repairer, new CombatTargetComponent { Faction = 1 });
        world.AddComponent(repairer, new SelectionComponent { IsSelected = false });
        world.AddComponent(repairer, new EntityNameComponent { DefinitionId = "support_repair" });
        world.AddComponent(repairer, new ShipRepairComponent
        {
            RepairRange = 60f,
            RepairRate = 50f,
            RepairableCategories = ["fighter"],
        });

        Entity derelict = world.CreateEntity();
        world.AddComponent(derelict, new TransformComponent { Position = new Vector3(10f, 0f, 0f) });
        world.AddComponent(derelict, new HealthComponent { MaxHP = 100f, CurrentHP = 10f });
        world.AddComponent(derelict, new CombatTargetComponent { Faction = 0 });
        world.AddComponent(derelict, new EntityNameComponent { DefinitionId = "fighter_basic" });
        state.EntityTags["derelict_1"] = derelict;
        state.EntityTags["player_support"] = repairer;

        var agent = new MissionPlaythroughAgent(def, CreateContext(world, state));

        agent.Tick(0f);
        agent.Tick(0f);
        Assert.Equal(2, agent.StepIndex);

        float elapsed = 0f;
        while (!state.FindObjective("repair_derelict_1")!.IsCompleted && elapsed < 10f)
        {
            repairSystem.Update(world, 0.5f);
            objectiveSystem.Update(world, 0.5f);
            elapsed += 0.5f;
        }

        Assert.True(state.FindObjective("repair_derelict_1")!.IsCompleted);

        agent.Tick(0f);
        Assert.Equal(3, agent.StepIndex);
    }

    // ── Step sequencing ───────────────────────────────────────────────────────

    [Fact]
    public void Agent_waits_on_wait_steps_then_advances()
    {
        var mission = new MissionDefinition
        {
            Id = "seq",
            DemoScript =
            [
                new DemoScriptStepDefinition { Type = "wait", Seconds = 1f },
                new DemoScriptStepDefinition { Type = "select_units", Filter = "all" },
            ],
        };

        var world = new World();
        var agent = new MissionPlaythroughAgent(mission, CreateContext(world));

        agent.Tick(0.4f);
        Assert.Equal(0, agent.StepIndex);

        agent.Tick(0.7f);
        Assert.Equal(1, agent.StepIndex);

        agent.Tick(0f);
        Assert.True(agent.ScriptFinished);
    }

    [Fact]
    public void Agent_blocks_on_wait_objective_until_complete()
    {
        var def = new MissionDefinition
        {
            Id = "obj_wait",
            Objectives = new ObjectivesDefinition
            {
                Primary =
                [
                    new ObjectiveDefinition
                    {
                        Id = "main",
                        Type = "destroy_target",
                        Target = "boss",
                    },
                ],
                Secondary =
                [
                    new ObjectiveDefinition
                    {
                        Id = "reach",
                        Type = "reach_area",
                        Position = [10f, 10f],
                        Radius = 5f,
                    },
                ],
            },
            DemoScript =
            [
                new DemoScriptStepDefinition { Type = "wait_objective", ObjectiveId = "reach" },
                new DemoScriptStepDefinition { Type = "select_units", Filter = "all" },
            ],
            Triggers = [],
            Victory = new EndConditionDefinition { Type = "all_primary_complete" },
            Defeat = new EndConditionDefinition { Type = "hero_destroyed" },
        };

        var state = new MissionState(def) { Phase = MissionPhase.InProgress };
        var world = new World();
        var agent = new MissionPlaythroughAgent(def, CreateContext(world, state));

        agent.Tick(0f);
        Assert.Equal(0, agent.StepIndex);

        state.SecondaryObjectives[0].IsCompleted = true;
        agent.Tick(0f);
        Assert.Equal(1, agent.StepIndex);
    }

    // ── No-op safety ──────────────────────────────────────────────────────────

    [Fact]
    public void Move_command_noops_safely_when_no_units_selected()
    {
        var world = new World();
        var executor = new GameCommandExecutor();
        var cmd = new MoveCommand
        {
            PlayerId = 1,
            Tick = 1,
            EntityIds = [99],
            TargetX = 10f,
            TargetZ = 10f,
        };

        bool executed = executor.Execute(CreateContext(world), cmd);
        Assert.False(executed);
    }

    [Fact]
    public void Agent_move_step_advances_without_throw_when_no_selection()
    {
        var mission = new MissionDefinition
        {
            Id = "noop",
            DemoScript =
            [
                new DemoScriptStepDefinition { Type = "move_to", Position = [10f, 10f] },
                new DemoScriptStepDefinition { Type = "wait", Seconds = 0.1f },
            ],
        };

        var world = new World();
        var agent = new MissionPlaythroughAgent(mission, CreateContext(world));

        var ex = Record.Exception(() => agent.Tick(0f));
        Assert.Null(ex);
        Assert.Equal(1, agent.StepIndex);
    }

    [Fact]
    public void Attack_target_noops_when_tag_missing()
    {
        var mission = new MissionDefinition
        {
            Id = "attack",
            DemoScript =
            [
                new DemoScriptStepDefinition { Type = "attack_target", TargetTag = "missing_tag" },
            ],
        };

        var world = new World();
        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new SelectionComponent { IsSelected = true, SelectionRadius = 5f });
        world.AddComponent(ship, new MovementComponent());
        world.AddComponent(ship, new WeaponListComponent());

        var agent = new MissionPlaythroughAgent(mission, CreateContext(world));
        var ex = Record.Exception(() => agent.Tick(0f));
        Assert.Null(ex);
        Assert.True(agent.ScriptFinished);
    }

    // ── Command pipeline ──────────────────────────────────────────────────────

    [Fact]
    public void Executor_issues_move_routes_for_selected_player_units()
    {
        var world = new World();
        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new SelectionComponent { IsSelected = true, SelectionRadius = 5f });
        world.AddComponent(ship, new MovementComponent { Speed = 50f });

        var executor = new GameCommandExecutor();
        var target = MapCoordinates.GridToWorld(20, 20);
        var cmd = new MoveCommand
        {
            PlayerId = 1,
            Tick = 1,
            EntityIds = [ship.Index],
            TargetX = target.X,
            TargetZ = target.Z,
        };

        Assert.True(executor.Execute(CreateContext(world), cmd));
        Assert.NotNull(world.GetComponent<WaypointQueueComponent>(ship));
    }

    [Fact]
    public void Example_scenario_destroy_scouts_objective_uses_enemy_scouts_tag()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("example_scenario")!;
        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new SharpOpenGl.Engine.Events.EventBus();
        var system = new ObjectiveSystem(state, bus);
        var world = new World();

        var destroy = state.FindObjective("destroy_scouts")!;
        Assert.Equal("enemy_scouts", destroy.Definition.Target);

        Entity player = world.CreateEntity();
        world.AddComponent(player, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(32, 32),
        });
        world.AddComponent(player, new SelectionComponent { IsSelected = true, SelectionRadius = 5f });
        world.AddComponent(player, new MovementComponent { Speed = 100f });
        world.AddComponent(player, new WeaponListComponent());
        world.AddComponent(player, new HeroComponent());

        system.Update(world, 0f);
        Assert.True(state.FindObjective("reach_waypoint")!.IsCompleted);

        Entity scout = world.CreateEntity();
        state.EntityTags["enemy_scouts"] = scout;
        world.DestroyEntity(scout);
        system.Update(world, 0f);

        Assert.True(destroy.IsCompleted);
    }

    [Fact]
    public void Example_scenario_objectives_complete_within_script_simulation()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load("example_scenario")!;

        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new SharpOpenGl.Engine.Events.EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);

        Entity player = world.CreateEntity();
        world.AddComponent(player, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(32, 32),
        });
        world.AddComponent(player, new SelectionComponent { IsSelected = true, SelectionRadius = 5f });
        world.AddComponent(player, new MovementComponent { Speed = 100f });
        world.AddComponent(player, new WeaponListComponent());
        world.AddComponent(player, new HeroComponent());

        var agent = new MissionPlaythroughAgent(mission, CreateContext(world, state));

        objectiveSystem.Update(world, 0f);
        Assert.True(state.FindObjective("reach_waypoint")!.IsCompleted);

        Entity scout = world.CreateEntity();
        state.EntityTags["enemy_scouts"] = scout;
        world.DestroyEntity(scout);
        objectiveSystem.Update(world, 0f);
        Assert.True(state.FindObjective("destroy_scouts")!.IsCompleted);

        float elapsed = 0f;
        while (!agent.IsFinished && elapsed < 120f)
        {
            agent.Tick(0.5f);
            elapsed += 0.5f;
        }

        Assert.True(state.AllPrimaryComplete);
        Assert.True(agent.MissionObjectivesComplete);
        Assert.True(elapsed <= 120f);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static BuildMapCatalog CreateBuildMapCatalog()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        return new BuildMapCatalog(config, defs);
    }

    private static BuildMapConfig LoadBuildMapConfig()
    {
        string path = Path.Combine(GetTestDataPath(), "Config", "build_map.json");
        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BuildMapConfig>(json, JsonOptions);
        Assert.NotNull(config);
        return config!;
    }

    private static Dictionary<string, EntityDefinition> LoadAllBaseDefinitions()
    {
        var defs = new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);
        string basesPath = Path.Combine(GetTestDataPath(), "Bases");

        foreach (string file in Directory.GetFiles(basesPath, "*.json"))
        {
            if (Path.GetFileName(file).StartsWith("_", StringComparison.Ordinal)) continue;
            string json = File.ReadAllText(file);
            var def = JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions);
            if (def?.Id != null)
                defs[def.Id] = def;
        }

        return defs;
    }
}