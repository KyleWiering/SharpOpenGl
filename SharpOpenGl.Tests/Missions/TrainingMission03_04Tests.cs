using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>
/// Schema and composition tests for training missions 3 (harvest) and 4 (defense).
/// Class name includes <c>TrainingMission</c> for filter discoverability.
/// </summary>
public class TrainingMission03_04Tests
{
    private static string GetTestDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException(
                "Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }

    private static MissionLoader CreateLoader()
    {
        var assets = new AssetManager(GetTestDataPath());
        return new MissionLoader(assets);
    }

    // ── training_03_harvest ──────────────────────────────────────────────────

    [Fact]
    public void TrainingMission03_loads_and_has_expected_identity()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest");

        Assert.NotNull(mission);
        Assert.Equal("training_03_harvest", mission.Id);
        Assert.Null(mission.PrerequisiteMissionId);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("sector_alpha", mission.Map);
    }

    [Fact]
    public void TrainingMission03_start_composition()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Contains("support_repair", mission.StartConditions.StartingUnits);
        Assert.Contains("miner_basic", mission.StartConditions.StartingUnits);
        Assert.Contains("command_center", mission.StartConditions.StartingBuildings);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.True(StartConditionsSpawnLogic.HasExplicitStartingBuildings(mission.StartConditions));
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(mission.StartConditions));
    }

    [Fact]
    public void TrainingMission03_objectives_construct_refinery_and_collect_minerals()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.NotNull(mission.Objectives);
        Assert.NotEmpty(mission.Objectives.Primary);

        var construct = mission.Objectives.Primary
            .FirstOrDefault(o => string.Equals(o.Type, "construct", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(construct);
        Assert.Equal("resource_refinery:1", construct.Target);

        var collect = mission.Objectives.Primary
            .FirstOrDefault(o => string.Equals(o.Type, "collect", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(collect);
        Assert.Equal("Minerals:1000", collect.Target);
    }

    [Fact]
    public void TrainingMission03_briefing_mentions_harvest_workflow()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.NotNull(mission.Briefing);
        string briefing = mission.Briefing!.Text ?? string.Empty;
        string preview = string.Join(' ', mission.Briefing.ObjectivesPreview ?? []);

        foreach (string keyword in new[] { "refinery", "minerals", "miner", "1000" })
        {
            Assert.True(
                briefing.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || preview.Contains(keyword, StringComparison.OrdinalIgnoreCase),
                $"Briefing or objectivesPreview must mention '{keyword}' for harvest workflow clarity");
        }

        Assert.NotNull(mission.Briefing.ObjectivesPreview);
        Assert.Contains(mission.Briefing.ObjectivesPreview, p =>
            p.Contains("refinery", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mission.Briefing.ObjectivesPreview, p =>
            p.Contains("minerals", StringComparison.OrdinalIgnoreCase)
            && p.Contains("harvest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TrainingMission03_map_has_viable_mineral_node()
    {
        TrainingMission03_sector_alpha_has_viable_mineral_nodes();
    }

    [Fact]
    public void TrainingMission03_sector_alpha_has_viable_mineral_nodes()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        // training_03_harvest must run on sector_alpha (mineral node map).
        Assert.Equal("sector_alpha", mission.Map);

        var assets = new AssetManager(GetTestDataPath());
        var map = assets.Load<MapDefinition>("Maps/sector_alpha");
        Assert.NotNull(map);

        var mineralNodes = map!.ResourceNodes
            .Where(n => string.Equals(n.Type, "minerals", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(mineralNodes);

        // Assumption: player spawn [12,12] can reach map resource nodes; no impassable gate on sector_alpha.
        // Viability: at least one minerals node with amount >= 1000 (primary node at [45,20] holds 3000).
        Assert.Contains(mineralNodes, n => n.Amount >= 1000);

        var primaryNode = mineralNodes.FirstOrDefault(n =>
            n.Position.Length >= 2 && n.Position[0] == 45 && n.Position[1] == 20);
        Assert.NotNull(primaryNode);
        Assert.True(primaryNode!.Amount >= 1000,
            "sector_alpha minerals node at [45,20] must satisfy training_03 collect Minerals:1000");

        // Harvestable planet Kethos Minor supplements sector mineral access.
        var harvestablePlanet = map.MapFeatures.FirstOrDefault(f =>
            string.Equals(f.Kind, "harvestable_planet", StringComparison.OrdinalIgnoreCase)
            && string.Equals(f.ResourceType, "minerals", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(harvestablePlanet);
        Assert.True(harvestablePlanet!.Amount >= 1000);
    }

    [Fact]
    public void TrainingMission03_victory_all_primary_and_no_mandatory_hostiles()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);

        // No required combat: empty triggers, or no hostile spawn_units actions.
        bool hasHostileSpawn = mission.Triggers.Any(t =>
            t.Actions.Any(a =>
                string.Equals(a.Type, "spawn_units", StringComparison.OrdinalIgnoreCase)
                && (a.Faction is null or 2)));
        Assert.False(hasHostileSpawn);
    }

    // ── training_04_defense ──────────────────────────────────────────────────

    [Fact]
    public void TrainingMission04_loads_and_has_expected_identity()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense");

        Assert.NotNull(mission);
        Assert.Equal("training_04_defense", mission.Id);
        Assert.Null(mission.PrerequisiteMissionId);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrainingMission04_start_composition_and_resources()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Contains("command_center", mission.StartConditions.StartingBuildings);
        Assert.Contains("support_repair", mission.StartConditions.StartingUnits);
        Assert.False(mission.StartConditions.SpawnDefaultBase);

        // defense_turret cost: 60E / 90M / 1 crew each — five turrets plus rebuild buffer.
        const float turretEnergy = 60f;
        const float turretMinerals = 90f;
        const float turretCrew = 1f;
        const float bufferEnergy = 100f;
        const float bufferMinerals = 100f;
        const float bufferCrew = 5f;

        if (mission.StartConditions.UnlimitedResources)
            return;

        var resources = mission.StartConditions.StartingResources;
        Assert.NotNull(resources);
        Assert.True(resources!.Energy >= turretEnergy * 5f + bufferEnergy);
        Assert.True(resources.Minerals >= turretMinerals * 5f + bufferMinerals);
        Assert.True(resources.Crew >= turretCrew * 5f + bufferCrew);
    }

    [Fact]
    public void TrainingMission04_briefing_states_spawn_stop_on_victory()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.NotNull(mission.Briefing);
        string briefing = mission.Briefing!.Text ?? string.Empty;
        string preview = string.Join(' ', mission.Briefing.ObjectivesPreview ?? []);

        bool mentionsSpawnStop =
            briefing.Contains("spawns cease", StringComparison.OrdinalIgnoreCase)
            || briefing.Contains("until all five", StringComparison.OrdinalIgnoreCase)
            || preview.Contains("until all five", StringComparison.OrdinalIgnoreCase)
            || preview.Contains("Raids continue", StringComparison.OrdinalIgnoreCase);

        Assert.True(mentionsSpawnStop,
            "Briefing or objectivesPreview must explain raids continue until five turrets are online");
        Assert.Contains("support_repair", mission.StartConditions!.StartingUnits);
        Assert.Equal("unit_destroyed", mission.Defeat?.Type);
        Assert.Equal("player_support", mission.Defeat?.Target);
    }

    [Fact]
    public void TrainingMission04_objective_construct_five_turrets_and_victory()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.NotNull(mission.Objectives);
        var construct = mission.Objectives.Primary
            .FirstOrDefault(o => string.Equals(o.Type, "construct", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(construct);
        Assert.Equal("defense_turret:5", construct.Target);

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);
        Assert.DoesNotContain(
            mission.Objectives.Primary,
            o => string.Equals(o.Type, "survive_time", StringComparison.OrdinalIgnoreCase)
                 && mission.Objectives.Primary.Length == 1);
    }

    [Fact]
    public void TrainingMission04_repeating_hostile_interceptor_spawn()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        var repeatingRaids = mission.Triggers
            .Where(t => !t.OneShot)
            .Where(t => t.Condition != null
                        && string.Equals(t.Condition.Type, "timer", StringComparison.OrdinalIgnoreCase)
                        && t.Condition.Seconds is >= 5f and <= 60f)
            .Where(t => t.Actions.Any(a =>
                string.Equals(a.Type, "spawn_units", StringComparison.OrdinalIgnoreCase)
                && a.Units != null
                && a.Units.Contains("interceptor_mk2")))
            .ToArray();

        Assert.NotEmpty(repeatingRaids);

        var spawnActions = repeatingRaids
            .SelectMany(t => t.Actions)
            .Where(a => string.Equals(a.Type, "spawn_units", StringComparison.OrdinalIgnoreCase)
                        && a.Units != null
                        && a.Units.Contains("interceptor_mk2"))
            .ToArray();

        Assert.NotEmpty(spawnActions);
        foreach (var action in spawnActions)
        {
            // Null faction resolves to engine default 2 (hostile); explicit 2 is also hostile.
            int resolvedFaction = action.Faction ?? 2;
            Assert.Equal(2, resolvedFaction);
        }
    }

    [Fact]
    public void TrainingMission04_raid_triggers_spawn_interceptors_from_mission_json()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        var raidTriggers = mission.Triggers
            .Where(t => string.Equals(t.Id, "raid_wave", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t.Id, "raid_wave_alt", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Equal(2, raidTriggers.Length);

        string gameDataPath = GetTestDataPath();
        var (state, bus, system, world, spawnedUnitIds) = MissionTriggerHarness.Create(mission, gameDataPath);

        // No manual raid_interceptor seed — spawns must come from JSON triggers only.
        Assert.False(state.EntityTags.ContainsKey("raid_interceptor"));

        int spawnCountBefore = MissionTriggerHarness.CountHostileInterceptors(world, state);
        Assert.Equal(0, spawnCountBefore);

        // Advance past first raid_wave threshold (20s) without manual entity creation.
        system.Update(world, 21f);

        Assert.Contains(state.Triggers, t =>
            string.Equals(t.Definition.Id, "raid_wave", StringComparison.OrdinalIgnoreCase) && t.HasFired);

        Assert.True(state.EntityTags.TryGetValue("raid_interceptor", out Entity raid));
        Assert.True(world.IsAlive(raid));

        var combat = world.GetComponent<CombatTargetComponent>(raid);
        Assert.NotNull(combat);
        Assert.Equal(2, combat!.Faction);

        Assert.Contains("interceptor_mk2", spawnedUnitIds);
        Assert.True(MissionTriggerHarness.CountHostileInterceptors(world, state) >= 1);
    }

    [Fact]
    public void TrainingMission04_spawns_stop_after_five_turrets()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;
        string gameDataPath = GetTestDataPath();

        var state = new MissionState(mission) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var world = new World();
        var objectiveSystem = new ObjectiveSystem(state, bus);
        var spawnedUnitIds = new List<string>();

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

        var triggerSystem = MissionTriggerHarness.CreateTriggerSystem(
            state, bus, gameDataPath, spawnedUnitIds);

        var place = (string buildingId, Vector3 worldPos) =>
        {
            Entity e = world.CreateEntity();
            world.AddComponent(e, new TransformComponent { Position = worldPos });
            world.AddComponent(e, new BuildingComponent
            {
                BuildingType = buildingId,
                PlayerId = 1,
            });
            world.AddComponent(e, new HealthComponent { MaxHP = 1000f, CurrentHP = 1000f });
            return true;
        };

        // Advance past first raid_wave (20s) before completing turrets.
        triggerSystem.Update(world, 21f);
        Assert.Contains("interceptor_mk2", spawnedUnitIds);
        Assert.True(MissionTriggerHarness.CountHostileInterceptors(world, state) >= 1);

        int spawnsBeforeVictory = spawnedUnitIds.Count;

        int[][] positions = [[18, 14], [22, 14], [18, 18], [22, 18], [26, 16]];
        foreach (int[] pos in positions)
            place("defense_turret", MapCoordinates.GridToWorld(pos[0], pos[1]));

        objectiveSystem.Update(world, 0.5f);
        Assert.Equal(MissionPhase.Victory, state.Phase);
        Assert.True(state.FindObjective("build_turrets")!.IsCompleted);

        int spawnsAtVictory = spawnedUnitIds.Count;

        // Continue well past raid_wave_alt (45s) — no further spawns after victory.
        triggerSystem.Update(world, 50f);
        Assert.True(
            spawnsAtVictory == spawnedUnitIds.Count,
            "TriggerSystem must halt repeating spawns once MissionPhase.Victory is reached");
        Assert.True(spawnsBeforeVictory >= 1,
            "At least one hostile interceptor must spawn from raid triggers before victory");
    }
}
