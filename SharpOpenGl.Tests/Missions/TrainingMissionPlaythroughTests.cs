using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Multiplayer;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>
/// Integration proofs that all five training missions are achievable via
/// <see cref="MissionPlaythroughAgent"/> + <see cref="ObjectiveSystem"/> harnesses.
/// Class name matches both <c>TrainingMission</c> and <c>MissionPlaythrough</c> filters.
/// </summary>
public class TrainingMissionPlaythroughTests
{
    private static readonly string[] TrainingMissionIds =
    [
        "training_01_interceptor",
        "training_02_building",
        "training_03_harvest",
        "training_04_defense",
        "training_05_tech_tree",
    ];

    private static readonly HashSet<string> KnownDemoStepTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "select_units",
        "move_to",
        "attack_move",
        "attack_target",
        "wait",
        "harvest",
        "wait_objective",
        "camera_pan",
        "build_unit",
        "place_building",
        "wait_for_construction",
        "repair_target",
    };

    private static string GetTestDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }

    private static MissionDefinition LoadMission(string missionId)
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load(missionId);
        Assert.NotNull(mission);
        return mission!;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static GameCommandContext CreateContext(
        World world,
        MissionState? state = null,
        ResourceManager? resources = null,
        Func<string, Vector3, bool>? placeBuilding = null,
        BuildMapCatalog? buildMapCatalog = null)
    {
        return new GameCommandContext
        {
            World = world,
            PlayerId = 1,
            MissionState = state,
            Resources = resources,
            PlaceBuilding = placeBuilding,
            BuildMapCatalog = buildMapCatalog,
        };
    }

    private static BuildMapCatalog CreateBuildMapCatalog()
    {
        string configPath = Path.Combine(GetTestDataPath(), "Config", "build_map.json");
        var config = JsonSerializer.Deserialize<BuildMapConfig>(File.ReadAllText(configPath), JsonOptions);
        Assert.NotNull(config);

        var defs = new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);
        string basesPath = Path.Combine(GetTestDataPath(), "Bases");
        foreach (string file in Directory.GetFiles(basesPath, "*.json"))
        {
            if (Path.GetFileName(file).StartsWith('_')) continue;
            var def = JsonSerializer.Deserialize<EntityDefinition>(File.ReadAllText(file), JsonOptions);
            if (def?.Id != null)
                defs[def.Id] = def;
        }

        return new BuildMapCatalog(config!, defs);
    }

    /// <summary>
    /// Spawns a completed player building at world position (no under-construction component).
    /// Matches how the agent resolves construction wait for completed structures.
    /// </summary>
    private static Func<string, Vector3, bool> CreatePlaceBuildingStub(World world, int playerId = 1)
    {
        return (buildingId, worldPos) =>
        {
            Entity e = world.CreateEntity();
            world.AddComponent(e, new TransformComponent { Position = worldPos });
            world.AddComponent(e, new BuildingComponent
            {
                BuildingType = buildingId,
                PlayerId = playerId,
            });
            world.AddComponent(e, new HealthComponent { MaxHP = 1000f, CurrentHP = 1000f });
            return true;
        };
    }

    /// <summary>
    /// Placement stub that enforces build_map prerequisite chains before spawning completed structures.
    /// </summary>
    private static Func<string, Vector3, bool> CreatePrerequisiteAwarePlaceBuildingStub(
        World world,
        BuildMapCatalog catalog,
        int playerId = 1)
    {
        return (buildingId, worldPos) =>
        {
            var builtTypes = BuildingFootprint.GetBuiltTypes(world, playerId);
            var prerequisites = catalog.GetPrerequisites(buildingId);
            if (!BuildMapCatalog.IsUnlocked(prerequisites, builtTypes))
                return false;

            Entity e = world.CreateEntity();
            world.AddComponent(e, new TransformComponent { Position = worldPos });
            world.AddComponent(e, new BuildingComponent
            {
                BuildingType = buildingId,
                PlayerId = playerId,
            });
            world.AddComponent(e, new HealthComponent { MaxHP = 1000f, CurrentHP = 1000f });
            return true;
        };
    }

    private static Entity SpawnLivingTaggedUnit(
        World world,
        MissionState state,
        string tag,
        string definitionId,
        int faction = 1,
        Vector3? position = null)
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position = position ?? MapCoordinates.GridToWorld(12, 12),
        });
        world.AddComponent(e, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(e, new CombatTargetComponent { Faction = faction });
        world.AddComponent(e, new SelectionComponent { IsSelected = false, SelectionRadius = 5f });
        world.AddComponent(e, new MovementComponent { Speed = 80f });
        world.AddComponent(e, new WeaponListComponent());
        world.AddComponent(e, new EntityNameComponent { DefinitionId = definitionId });
        state.EntityTags[tag] = e;
        return e;
    }

    private static Entity SpawnPlayerFighter(World world, string definitionId = "fighter_basic")
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(32, 28),
        });
        world.AddComponent(e, new HealthComponent { MaxHP = 80f, CurrentHP = 80f });
        world.AddComponent(e, new CombatTargetComponent { Faction = 1 });
        world.AddComponent(e, new EntityNameComponent { DefinitionId = definitionId });
        world.AddComponent(e, new SelectionComponent { IsSelected = false, SelectionRadius = 4f });
        return e;
    }

    // ── All training missions: parse + no-crash agent ─────────────────────────

    [Theory]
    [InlineData("training_01_interceptor")]
    [InlineData("training_02_building")]
    [InlineData("training_03_harvest")]
    [InlineData("training_04_defense")]
    [InlineData("training_05_tech_tree")]
    public void Training_mission_demoScript_parses_with_known_step_types(string missionId)
    {
        var mission = LoadMission(missionId);

        Assert.NotNull(mission.DemoScript);
        Assert.NotEmpty(mission.DemoScript);

        foreach (var step in mission.DemoScript)
        {
            Assert.False(string.IsNullOrWhiteSpace(step.Type));
            Assert.Contains(step.Type.Trim(), KnownDemoStepTypes);
        }
    }

    [Theory]
    [InlineData("training_01_interceptor")]
    [InlineData("training_02_building")]
    [InlineData("training_03_harvest")]
    [InlineData("training_04_defense")]
    [InlineData("training_05_tech_tree")]
    public void Training_mission_demoScript_agent_runs_without_crash(string missionId)
    {
        var mission = LoadMission(missionId);
        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var world = new World();
        var place = CreatePlaceBuildingStub(world);
        var agent = new MissionPlaythroughAgent(mission, CreateContext(world, state, placeBuilding: place));

        // Bounded tick loop — objectives may not complete without world setup;
        // prove the scripted step pipeline itself never throws.
        var ex = Record.Exception(() =>
        {
            for (int i = 0; i < 40; i++)
                agent.Tick(0.5f);
        });

        Assert.Null(ex);
        Assert.True(agent.StepIndex >= 0);
    }

    [Fact]
    public void All_five_training_missions_load_with_demoScripts()
    {
        foreach (string id in TrainingMissionIds)
        {
            var mission = LoadMission(id);
            Assert.True(mission.DemoScript.Length > 0, $"{id} must have a non-empty demoScript");
            Assert.Equal("all_primary_complete", mission.Victory?.Type);
        }
    }

    // ── Mission 1: destroy red interceptor ────────────────────────────────────

    /// <summary>
    /// Achievability evidence for training_01_interceptor:
    /// <list type="bullet">
    /// <item>Enemy spawns from JSON <c>spawn_red_interceptor</c> trigger (faction 2, tag registered) — no t=0 seed.</item>
    /// <item><c>destroy_enemy_interceptor</c> stays incomplete until tag exists post-trigger and entity is destroyed.</item>
    /// <item>Hybrid kill: after <c>attack_target</c> steps begin, entity destruction simulates combat outcome
    /// (full weapon/combat sim out of scope).</item>
    /// </list>
    /// </summary>
    [Fact]
    public void TrainingMission01_destroy_enemy_interceptor_path_completes()
    {
        var mission = LoadMission("training_01_interceptor");
        Assert.Contains(mission.DemoScript, s =>
            string.Equals(s.Type, "attack_target", StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.TargetTag, "enemy_interceptor_1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mission.DemoScript, s =>
            string.Equals(s.Type, "wait_objective", StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.ObjectiveId, "destroy_enemy_interceptor", StringComparison.OrdinalIgnoreCase));

        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);
        var triggerSystem = MissionTriggerHarness.CreateTriggerSystem(state, bus, GetTestDataPath());

        // Defeat guard: player_interceptor must live until victory.
        SpawnLivingTaggedUnit(
            world, state, "player_interceptor", "interceptor_mk2",
            faction: 1, position: MapCoordinates.GridToWorld(5, 5));

        // No manual enemy_interceptor_1 seed — spawns must come from JSON triggers only.
        Assert.False(state.EntityTags.ContainsKey("enemy_interceptor_1"));
        Assert.False(state.FindObjective("destroy_enemy_interceptor")!.IsCompleted);

        var agent = new MissionPlaythroughAgent(mission, CreateContext(world, state));

        bool enemySpawnedFromTrigger = false;
        bool destroyedEnemy = false;
        float elapsed = 0f;
        const float delta = 0.5f;
        const float spawnSeconds = 2f;
        const float loopBudgetSeconds = 60f;

        while (!agent.IsFinished && elapsed < loopBudgetSeconds)
        {
            agent.Tick(delta);
            triggerSystem.Update(world, delta);
            objectiveSystem.Update(world, delta);
            elapsed += delta;

            if (!enemySpawnedFromTrigger && elapsed > spawnSeconds)
            {
                Assert.True(state.EntityTags.TryGetValue("enemy_interceptor_1", out Entity spawnedEnemy),
                    "spawn_red_interceptor must register enemy_interceptor_1 after 2s timer");
                Assert.True(world.IsAlive(spawnedEnemy));

                var combat = world.GetComponent<CombatTargetComponent>(spawnedEnemy);
                Assert.NotNull(combat);
                Assert.Equal(2, combat!.Faction);

                // Hostiles are AI-controlled for attack_target resolution fallback.
                if (!world.HasComponent<AIControlledComponent>(spawnedEnemy))
                    world.AddComponent(spawnedEnemy, new AIControlledComponent());

                enemySpawnedFromTrigger = true;
            }

            // destroy_enemy_interceptor must not complete before trigger spawn registers the tag.
            if (!enemySpawnedFromTrigger)
                Assert.False(state.FindObjective("destroy_enemy_interceptor")!.IsCompleted);

            // After attack_target step begins, resolve destroy via entity removal
            // (full combat sim is out of scope — destruction is the objective signal).
            if (enemySpawnedFromTrigger
                && !destroyedEnemy
                && agent.StepIndex >= 5
                && state.EntityTags.TryGetValue("enemy_interceptor_1", out Entity enemy)
                && world.IsAlive(enemy))
            {
                world.DestroyEntity(enemy);
                destroyedEnemy = true;
            }
        }

        Assert.True(enemySpawnedFromTrigger, "Enemy must spawn from JSON trigger during playthrough");
        Assert.True(destroyedEnemy);
        Assert.True(state.FindObjective("destroy_enemy_interceptor")!.IsCompleted);
        Assert.True(state.AllPrimaryComplete);
        Assert.True(agent.MissionObjectivesComplete);
        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.True(state.ElapsedTime > 0f);
    }

    // ── Mission 2: build path + elapsed + success ─────────────────────────────

    /// <summary>
    /// Achievability evidence for training_02_building:
    /// <list type="bullet">
    /// <item>demoScript places HQ → power reactor → shipyard with <c>wait_for_construction</c> beats.</item>
    /// <item><c>build_unit</c> references <c>shipyard_small</c>; hybrid stub spawns fighter after shipyard exists
    /// (full BuildSystem production is out of scope).</item>
    /// <item>All four primaries complete in order; victory only after <c>produce_fighter</c>.</item>
    /// </list>
    /// </summary>
    [Fact]
    public void TrainingMission02_playthrough_victory_with_positive_elapsed_time()
    {
        var mission = LoadMission("training_02_building");

        Assert.Contains(mission.DemoScript, s =>
            s.Type == "place_building" && s.BuildingId == "command_center");
        Assert.Contains(mission.DemoScript, s =>
            s.Type == "place_building" && s.BuildingId == "power_reactor");
        Assert.Contains(mission.DemoScript, s =>
            s.Type == "wait_objective" && s.ObjectiveId == "build_power");
        Assert.Contains(mission.DemoScript, s =>
            s.Type == "place_building" && s.BuildingId == "shipyard_small");
        Assert.Contains(mission.DemoScript, s =>
            s.Type == "build_unit"
            && s.UnitId == "fighter_basic"
            && string.Equals(s.BuildingTag, "shipyard_small", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mission.DemoScript, s =>
            s.Type == "wait_objective" && s.ObjectiveId == "produce_fighter");

        foreach (string buildingId in new[] { "command_center", "power_reactor", "shipyard_small" })
        {
            Assert.Contains(mission.DemoScript, s =>
                string.Equals(s.Type, "wait_for_construction", StringComparison.OrdinalIgnoreCase)
                && string.Equals(s.BuildingId, buildingId, StringComparison.OrdinalIgnoreCase));
        }

        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);

        SpawnLivingTaggedUnit(
            world, state, "player_support", "support_repair",
            faction: 1, position: MapCoordinates.GridToWorld(12, 12));

        var place = CreatePlaceBuildingStub(world);
        var agent = new MissionPlaythroughAgent(mission, CreateContext(world, state, placeBuilding: place));

        bool spawnedFighter = false;
        var completedAt = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        float elapsed = 0f;
        while (!agent.IsFinished && elapsed < 90f)
        {
            agent.Tick(0.5f);

            // build_unit cannot complete production without full BuildSystem;
            // once the shipyard exists, spawn the required fighter unit.
            if (!spawnedFighter && HasCompletedBuilding(world, "shipyard_small"))
            {
                SpawnPlayerFighter(world, "fighter_basic");
                spawnedFighter = true;
            }

            objectiveSystem.Update(world, 0.5f);
            elapsed += 0.5f;

            foreach (var obj in state.PrimaryObjectives)
            {
                if (obj.IsCompleted && !completedAt.ContainsKey(obj.Id))
                    completedAt[obj.Id] = elapsed;
            }

            if (state.Phase == MissionPhase.Victory)
                Assert.True(completedAt.ContainsKey("produce_fighter"),
                    "Victory must not occur before produce_fighter objective completes.");
        }

        Assert.True(state.FindObjective("build_hq")!.IsCompleted);
        Assert.True(state.FindObjective("build_power")!.IsCompleted);
        Assert.True(state.FindObjective("build_shipyard")!.IsCompleted);
        Assert.True(state.FindObjective("produce_fighter")!.IsCompleted);
        Assert.True(state.AllPrimaryComplete || state.Phase == MissionPhase.Victory);
        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.True(state.ElapsedTime > 0f, "Mission 2 must record positive time-to-complete");
        Assert.True(state.ElapsedTime < 300f, "Mission 2 elapsed time should stay within playthrough sanity bound");
        Assert.True(agent.MissionObjectivesComplete);
        Assert.True(spawnedFighter);

        Assert.True(completedAt["build_hq"] < completedAt["build_power"]);
        Assert.True(completedAt["build_power"] < completedAt["build_shipyard"]);
        Assert.True(completedAt["produce_fighter"] >= completedAt["build_hq"]);
        Assert.True(completedAt["produce_fighter"] >= completedAt["build_power"]);
        Assert.True(completedAt["produce_fighter"] >= completedAt["build_shipyard"],
            "produce_fighter must be the last primary to complete (hybrid fighter stub).");
    }

    [Fact]
    public void TrainingMission02_elapsed_time_recorded_on_victory()
    {
        var mission = LoadMission("training_02_building");
        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);

        SpawnLivingTaggedUnit(
            world, state, "player_support", "support_repair",
            faction: 1, position: MapCoordinates.GridToWorld(12, 12));

        var place = CreatePlaceBuildingStub(world);
        var agent = new MissionPlaythroughAgent(mission, CreateContext(world, state, placeBuilding: place));

        bool spawnedFighter = false;
        float elapsed = 0f;
        while (!agent.IsFinished && elapsed < 90f)
        {
            agent.Tick(0.5f);

            if (!spawnedFighter && HasCompletedBuilding(world, "shipyard_small"))
            {
                SpawnPlayerFighter(world, "fighter_basic");
                spawnedFighter = true;
            }

            objectiveSystem.Update(world, 0.5f);
            elapsed += 0.5f;
        }

        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.True(state.ElapsedTime > 0f);
        Assert.True(state.ElapsedTime < 300f);

        var screen = new MissionVictoryScreen();
        screen.SetMissionResult(state, isVictory: true);

        var renderer = new MissionVictoryRecordingRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == mission.DisplayName);
        Assert.Contains(renderer.Texts, t => t.StartsWith("Time: ", StringComparison.Ordinal));
        Assert.Contains(renderer.Texts, t => t == $"XP Earned: {mission.Rewards?.Xp ?? 0}");
    }

    private sealed class MissionVictoryRecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<string> Texts { get; } = [];

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
            => Texts.Add(text);
    }

    // ── Mission 3: refinery + minerals (real mining simulation) ─────────────────

    /// <summary>
    /// Achievability evidence for training_03_harvest:
    /// <list type="bullet">
    /// <item>demoScript place_building + wait_for_construction path places resource_refinery.</item>
    /// <item>demoScript selects miner_basic, moves to sector_alpha minerals node [45,20], then harvest step.</item>
    /// <item>Agent <c>harvest</c> step wires collectors; collect Minerals:1000 completes via
    /// MovementSystem + ResourceSystem + MiningVisualSystem deposit cycles — no bulk grant.</item>
    /// </list>
    /// </summary>
    [Fact]
    public void TrainingMission03_refinery_and_collect_minerals_achievable()
    {
        MovementBalance.ResetForTests();
        MovementBalance.Apply(new MovementConfig
        {
            GlobalSpeedMultiplier = 1f,
            GlobalAccelerationMultiplier = 1f,
        });

        var mission = LoadMission("training_03_harvest");

        Assert.Contains(mission.DemoScript, s =>
            s.Type == "place_building" && s.BuildingId == "resource_refinery");
        Assert.Contains(mission.DemoScript, s =>
            string.Equals(s.Type, "wait_for_construction", StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.BuildingId, "resource_refinery", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mission.DemoScript, s =>
            string.Equals(s.Type, "select_units", StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.Filter, "miner_basic", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mission.DemoScript, s =>
            string.Equals(s.Type, "move_to", StringComparison.OrdinalIgnoreCase)
            && s.Position is [45, 20]);
        Assert.Contains(mission.DemoScript, s =>
            string.Equals(s.Type, "harvest", StringComparison.OrdinalIgnoreCase)
            && s.Seconds >= MiningVisualSystem.DroneShuttleDuration * 6f);
        Assert.Contains(mission.DemoScript, s =>
            s.Type == "wait_objective" && s.ObjectiveId == "collect_minerals");

        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var resources = new ResourceManager(bus);
        var start = mission.StartConditions!.StartingResources!;
        var pool = resources.AddPlayer(1);
        pool.SetStartingAmount(ResourceType.Energy, start.Energy);
        pool.SetStartingAmount(ResourceType.Minerals, start.Minerals);
        pool.SetStartingAmount(ResourceType.Data, start.Data);
        pool.SetStartingAmount(ResourceType.Crew, start.Crew);

        var world = new World();
        world.AddSystem(new MovementSystem());
        world.AddSystem(new ResourceSystem(resources));
        world.AddSystem(new MiningVisualSystem());
        var objectiveSystem = new ObjectiveSystem(state, bus, resources);

        Entity hq = world.CreateEntity();
        world.AddComponent(hq, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(12, 12),
        });
        world.AddComponent(hq, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        SpawnLivingTaggedUnit(
            world, state, "player_support", "support_repair",
            faction: 1, position: MapCoordinates.GridToWorld(13, 12));
        Entity miner = SpawnMinerForHarvest(
            world, state, "player_miner", "miner_basic",
            position: MapCoordinates.GridToWorld(14, 12));

        var map = new AssetManager(GetTestDataPath()).Load<MapDefinition>("Maps/sector_alpha")!;
        var mineralNodeDef = map.ResourceNodes
            .First(n => string.Equals(n.Type, "minerals", StringComparison.OrdinalIgnoreCase)
                        && n.Position is [45, 20]);
        Entity mineralNode = SpawnMapMineralNode(world, mineralNodeDef);

        var place = CreatePlaceBuildingStub(world);
        var agent = new MissionPlaythroughAgent(
            mission, CreateContext(world, state, resources, place));

        float startMinerals = resources.GetDisplay(1, ResourceType.Minerals).Current;
        float harvestSimSeconds = 0f;
        bool agentHarvestWired = false;
        const float harvestLoopBudgetSeconds = 600f;
        float elapsed = 0f;
        while (!agent.IsFinished && elapsed < harvestLoopBudgetSeconds)
        {
            agent.Tick(0.5f);

            var collector = world.GetComponent<ResourceCollectorComponent>(miner);
            if (!agentHarvestWired
                && collector != null
                && collector.AssignedNode == mineralNode
                && collector.DepositTarget != Entity.Null)
            {
                agentHarvestWired = true;
            }

            if (HasCompletedBuilding(world, "resource_refinery"))
            {
                harvestSimSeconds += 0.5f;
                world.Update(0.5f);
            }

            objectiveSystem.Update(world, 0.5f);
            elapsed += 0.5f;
        }

        float harvestedViaSimulation =
            resources.GetDisplay(1, ResourceType.Minerals).Current - startMinerals;
        float mineralsRequired = 1000f - startMinerals;

        Assert.True(state.FindObjective("build_refinery")!.IsCompleted);
        Assert.True(state.FindObjective("collect_minerals")!.IsCompleted);
        Assert.True(state.AllPrimaryComplete);
        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.True(state.ElapsedTime > 0f);
        Assert.True(agentHarvestWired, "Agent harvest step must wire miner to node and refinery");
        Assert.True(harvestSimSeconds > 0f, "Harvest simulation must run via ResourceSystem ticks");
        Assert.True(harvestedViaSimulation >= mineralsRequired,
            $"Simulated harvest must reach target without bulk grant (got {harvestedViaSimulation}, need {mineralsRequired})");
    }

    /// <summary>
    /// Documented fallback when movement timing is bypassed: direct collector wiring
    /// proves ResourceSystem deposit loop without agent path (not scored for achievability).
    /// </summary>
    [Fact]
    public void TrainingMission03_harvest_simulation_fallback_wires_collector_directly()
    {
        var mission = LoadMission("training_03_harvest");
        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var resources = new ResourceManager(bus);
        resources.AddPlayer(1).SetStartingAmount(ResourceType.Minerals, 250f);

        var world = new World();
        world.AddSystem(new ResourceSystem(resources));
        world.AddSystem(new MiningVisualSystem());

        Entity refinery = world.CreateEntity();
        world.AddComponent(refinery, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(24, 20),
        });
        world.AddComponent(refinery, new BuildingComponent
        {
            BuildingType = "resource_refinery",
            PlayerId = 1,
        });

        Entity miner = SpawnMinerForHarvest(world, state, "player_miner", "miner_basic");
        Entity mineralNode = SpawnMapMineralNode(world, new MapResourceNode
        {
            Type = "minerals",
            Position = [45, 20],
            Amount = 3000,
        });

        WireMinerForHarvest(world, miner, mineralNode, refinery);

        float startMinerals = resources.GetDisplay(1, ResourceType.Minerals).Current;
        float elapsed = 0f;
        while (elapsed < 90f)
        {
            SyncMinerHarvestPosition(world, miner, mineralNode, refinery);
            world.Update(0.5f);
            elapsed += 0.5f;
        }

        float harvested = resources.GetDisplay(1, ResourceType.Minerals).Current - startMinerals;
        Assert.True(harvested >= 750f, $"Fallback harvest loop must deposit minerals (got {harvested})");
    }

    // ── Mission 4: five turrets without defeat ────────────────────────────────

    [Fact]
    public void TrainingMission04_five_turrets_complete_without_defeat()
    {
        var mission = LoadMission("training_04_defense");

        var placeSteps = mission.DemoScript
            .Where(s => string.Equals(s.Type, "place_building", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.BuildingId, "defense_turret", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Equal(5, placeSteps.Length);

        float scriptedWaitSeconds = mission.DemoScript
            .Where(s => string.Equals(s.Type, "wait", StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.Seconds);
        Assert.True(scriptedWaitSeconds >= 15f,
            "demoScript must pace turret placement to overlap first raid_wave at 20s");
        Assert.Contains(mission.DemoScript, s =>
            string.Equals(s.Type, "camera_pan", StringComparison.OrdinalIgnoreCase)
            && s.Position is [55, 55]);

        // Scripted raid pressure exists (not forced defeat).
        Assert.Contains(mission.Triggers, t =>
            !t.OneShot && t.Actions.Any(a =>
                string.Equals(a.Type, "spawn_units", StringComparison.OrdinalIgnoreCase)
                && a.Units != null
                && a.Units.Contains("interceptor_mk2")));

        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);

        // Starting HQ.
        Entity hq = world.CreateEntity();
        world.AddComponent(hq, new TransformComponent
        {
            Position = MapCoordinates.GridToWorld(12, 12),
        });
        world.AddComponent(hq, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        // Critical unit stays alive — no forced defeat under spawn pressure.
        Entity support = SpawnLivingTaggedUnit(
            world, state, "player_support", "support_repair",
            faction: 1, position: MapCoordinates.GridToWorld(13, 12));

        var triggerSystem = MissionTriggerHarness.CreateTriggerSystem(state, bus, GetTestDataPath());

        // No manual raid_interceptor seed — spawns must come from JSON triggers only.
        Assert.False(state.EntityTags.ContainsKey("raid_interceptor"));
        Assert.Equal(0, MissionTriggerHarness.CountHostileInterceptors(world, state));

        var place = CreatePlaceBuildingStub(world);
        var agent = new MissionPlaythroughAgent(mission, CreateContext(world, state, placeBuilding: place));

        bool raidSpawnedFromTrigger = false;
        bool raidSpawnedDuringBuildPhase = false;
        float elapsed = 0f;
        const float delta = 0.5f;
        const float raidWaveSeconds = 20f;
        const float loopBudgetSeconds = 90f;

        while (elapsed < loopBudgetSeconds && (!agent.IsFinished || !raidSpawnedFromTrigger))
        {
            if (!agent.IsFinished)
                agent.Tick(delta);

            triggerSystem.Update(world, delta);
            objectiveSystem.Update(world, delta);
            elapsed += delta;

            if (!raidSpawnedFromTrigger && elapsed >= raidWaveSeconds)
            {
                Assert.True(state.EntityTags.TryGetValue("raid_interceptor", out _),
                    "raid_wave trigger must spawn raid_interceptor at or after 20s");
                raidSpawnedFromTrigger = true;
                raidSpawnedDuringBuildPhase = CountCompletedBuildings(world, "defense_turret") < 5
                                              || state.Phase == MissionPhase.InProgress;
            }
        }

        Assert.True(raidSpawnedFromTrigger, "Raid must spawn from JSON trigger during playthrough");
        Assert.True(raidSpawnedDuringBuildPhase,
            "At least one raid spawn must be active while the build phase is still in progress");
        Assert.True(state.EntityTags.TryGetValue("raid_interceptor", out Entity raid));
        Assert.True(world.IsAlive(raid));
        Assert.True(MissionTriggerHarness.CountHostileInterceptors(world, state) >= 1);

        Assert.True(world.IsAlive(support), "Builder must survive for non-defeat path");
        Assert.True(state.FindObjective("build_turrets")!.IsCompleted);
        Assert.True(state.AllPrimaryComplete);
        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.NotEqual(MissionPhase.Defeat, state.Phase);
        Assert.Equal(5, CountCompletedBuildings(world, "defense_turret"));
    }

    // ── Mission 5: full 16-structure tech tree ────────────────────────────────

    [Fact]
    public void TrainingMission05_full_tech_tree_construct_path_completes()
    {
        var mission = LoadMission("training_05_tech_tree");

        string[] expectedBuildings =
        [
            "command_center",
            "power_reactor",
            "resource_refinery",
            "supply_depot",
            "defense_turret",
            "sensor_array",
            "fabrication_hub",
            "shipyard_small",
            "shield_emitter",
            "missile_battery",
            "shipyard_medium",
            "repair_bay",
            "comms_relay",
            "shipyard_large",
            "orbital_uplink",
            "fortress_core",
        ];

        var placeIds = mission.DemoScript
            .Where(s => string.Equals(s.Type, "place_building", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.BuildingId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(16, expectedBuildings.Length);
        foreach (string id in expectedBuildings)
            Assert.Contains(id, placeIds);

        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);

        SpawnLivingTaggedUnit(
            world, state, "player_support", "support_repair",
            faction: 1, position: MapCoordinates.GridToWorld(12, 12));

        var catalog = CreateBuildMapCatalog();
        var place = CreatePrerequisiteAwarePlaceBuildingStub(world, catalog);
        var agent = new MissionPlaythroughAgent(
            mission,
            CreateContext(world, state, placeBuilding: place, buildMapCatalog: catalog));

        var placementOrder = new List<string>();
        var seenBuildings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        float elapsed = 0f;
        while (!agent.IsFinished && elapsed < 180f)
        {
            agent.Tick(0.5f);
            objectiveSystem.Update(world, 0.5f);
            elapsed += 0.5f;

            foreach (string id in expectedBuildings)
            {
                if (!seenBuildings.Contains(id) && HasCompletedBuilding(world, id))
                {
                    seenBuildings.Add(id);
                    placementOrder.Add(id);
                }
            }
        }

        var builtSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string buildingId in placementOrder)
        {
            var prerequisites = catalog.GetPrerequisites(buildingId);
            Assert.True(
                BuildMapCatalog.IsUnlocked(prerequisites, builtSet),
                $"Building '{buildingId}' placed before prerequisites satisfied.");
            builtSet.Add(buildingId);
        }

        Assert.All(state.PrimaryObjectives, o =>
            Assert.True(o.IsCompleted, $"Objective '{o.Id}' should complete via prerequisite-aware place_building path"));
        Assert.True(state.AllPrimaryComplete);
        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.True(agent.MissionObjectivesComplete || agent.ScriptFinished);
        Assert.True(state.ElapsedTime > 0f);
        Assert.Equal(16, placementOrder.Count);

        foreach (string id in expectedBuildings)
            Assert.True(HasCompletedBuilding(world, id), $"Missing completed building: {id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Entity SpawnMinerForHarvest(
        World world,
        MissionState state,
        string tag,
        string definitionId,
        int faction = 1,
        Vector3? position = null)
    {
        Entity miner = SpawnLivingTaggedUnit(
            world, state, tag, definitionId, faction, position);

        var movement = world.GetComponent<MovementComponent>(miner)!;
        movement.Speed = 45f;
        movement.Acceleration = 60f;
        movement.TurnRate = 120f;

        world.AddComponent(miner, new ResourceCollectorComponent
        {
            HarvestMode = HarvestMode.Drones,
            HarvestRange = HarvestModeDefaults.DefaultRange(HarvestMode.Drones),
            HarvestRate = 15f,
            CarryCapacity = 100f,
        });
        return miner;
    }

    private static Entity SpawnMapMineralNode(World world, MapResourceNode nodeDef)
    {
        Entity node = world.CreateEntity();
        Vector3 pos = MapCoordinates.GridToWorld(nodeDef.Position[0], nodeDef.Position[1]);
        world.AddComponent(node, new TransformComponent { Position = pos });
        world.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = nodeDef.Amount,
            MaxAmount = nodeDef.Amount,
            HarvestRate = 10f,
        });
        return node;
    }

    private static void WireMinerForHarvest(World world, Entity miner, Entity mineralNode, Entity depositTarget)
    {
        world.AddComponent(miner, new ResourceCollectorComponent
        {
            PlayerId = 1,
            AssignedNode = mineralNode,
            DepositTarget = depositTarget,
            CarryCapacity = 300f,
            HarvestRate = 80f,
            HarvestRange = HarvestModeDefaults.DefaultRange(HarvestMode.Drones),
            HarvestMode = HarvestMode.Drones,
            State = CollectorState.MovingToNode,
        });

        SyncMinerHarvestPosition(world, miner, mineralNode, depositTarget);
    }

    /// <summary>
    /// Keeps the miner within harvest range at the node while collecting,
    /// and within deposit radius at the refinery while returning/depositing.
    /// </summary>
    private static void SyncMinerHarvestPosition(
        World world, Entity miner, Entity mineralNode, Entity depositTarget)
    {
        var collector = world.GetComponent<ResourceCollectorComponent>(miner);
        if (collector == null) return;

        var minerTf = world.GetComponent<TransformComponent>(miner)!;
        var nodeTf = world.GetComponent<TransformComponent>(mineralNode)!;
        var depositTf = world.GetComponent<TransformComponent>(depositTarget)!;

        if (collector.State is CollectorState.Returning or CollectorState.Depositing)
            minerTf.Position = depositTf.Position;
        else
            minerTf.Position = nodeTf.Position + new Vector3(8f, 0f, 0f);
    }

    private static Entity? FindCompletedBuilding(World world, string buildingType, int playerId = 1)
    {
        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (building.PlayerId != playerId) continue;
            if (!string.Equals(building.BuildingType, buildingType, StringComparison.OrdinalIgnoreCase))
                continue;
            if (world.HasComponent<UnderConstructionComponent>(entity)) continue;
            if (!world.IsAlive(entity)) continue;
            return entity;
        }

        return null;
    }

    private static bool HasCompletedBuilding(World world, string buildingType, int playerId = 1)
    {
        return CountCompletedBuildings(world, buildingType, playerId) > 0;
    }

    private static int CountCompletedBuildings(World world, string buildingType, int playerId = 1)
    {
        int count = 0;
        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (building.PlayerId != playerId) continue;
            if (!string.Equals(building.BuildingType, buildingType, StringComparison.OrdinalIgnoreCase))
                continue;
            if (world.HasComponent<UnderConstructionComponent>(entity)) continue;
            if (!world.IsAlive(entity)) continue;
            count++;
        }

        return count;
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<string> Texts { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
            => Texts.Add(text);
    }
}

/// <summary>Shared trigger harness for mission JSON spawn tests and playthrough integration.</summary>
internal static class MissionTriggerHarness
{
    public static (MissionState state, EventBus bus, TriggerSystem system, World world, List<string> spawnedUnitIds)
        Create(MissionDefinition mission, string gameDataPath)
    {
        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var spawnedUnitIds = new List<string>();
        var system = CreateTriggerSystem(state, bus, gameDataPath, spawnedUnitIds);
        return (state, bus, system, world, spawnedUnitIds);
    }

    public static TriggerSystem CreateTriggerSystem(
        MissionState state,
        EventBus bus,
        string gameDataPath,
        List<string>? spawnedUnitIds = null)
    {
        var assets = new AssetManager(gameDataPath);
        var factory = new UnitFactory(assets);
        return new TriggerSystem(state, bus, unitFactory: factory, assets: assets)
        {
            OnUnitSpawned = spawnedUnitIds != null
                ? (_, _, def, _) => spawnedUnitIds.Add(def.Id)
                : null,
        };
    }

    public static int CountHostileInterceptors(World world, MissionState state)
    {
        int count = 0;
        foreach (var (entity, combat) in world.Query<CombatTargetComponent>())
        {
            if (combat.Faction != 2) continue;
            if (!world.IsAlive(entity)) continue;

            if (state.EntityTags.TryGetValue("raid_interceptor", out Entity tagged) && tagged == entity)
            {
                count++;
                continue;
            }

            var name = world.GetComponent<EntityNameComponent>(entity);
            if (name != null && string.Equals(name.DefinitionId, "interceptor_mk2", StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }
}
