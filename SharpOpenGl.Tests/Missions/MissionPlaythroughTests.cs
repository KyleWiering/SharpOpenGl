using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
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
        Assert.Contains(mission.DemoScript, s => s.Type == "wait_objective" && s.ObjectiveId == "reach_waypoint");
        Assert.Contains(mission.DemoScript, s => s.Type == "attack_target");
        Assert.Contains(mission.DemoScript, s => s.Type == "wait_objective" && s.ObjectiveId == "destroy_scouts");
        Assert.Contains(mission.DemoScript, s => s.Type == "place_building" && s.BuildingId == "shipyard_small");
        Assert.Contains(mission.DemoScript, s => s.Type == "build_unit");
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
}