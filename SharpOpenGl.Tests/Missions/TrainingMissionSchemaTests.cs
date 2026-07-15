using System.Text.Json;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>
/// Common schema invariants for all five training missions (01–05).
/// Class name matches filter FullyQualifiedName~TrainingMission.
/// Mission-specific exhaustive checks live in TrainingMission03_04Tests / TrainingMission05TechTreeTests.
/// </summary>
public class TrainingMissionSchemaTests
{
    private static readonly string[] KnownStationTypeIds =
    [
        "command_center",
        "shipyard_small",
        "shipyard_medium",
        "shipyard_large",
        "power_reactor",
        "defense_turret",
        "mining_station",
        "research_lab",
    ];

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

    private static void AssertPrimaryObjectiveFields(ObjectiveDefinition[] primaries)
    {
        Assert.NotEmpty(primaries);
        foreach (var obj in primaries)
        {
            Assert.False(string.IsNullOrWhiteSpace(obj.Id));
            Assert.False(string.IsNullOrWhiteSpace(obj.Type));
            if (obj.Type is "destroy_target" or "construct")
                Assert.False(string.IsNullOrWhiteSpace(obj.Target));
        }
    }

    /// <summary>
    /// Each briefing preview line that names a construct milestone must have a matching primary.
    /// <paramref name="chain"/> pairs a preview substring with the expected construct target.
    /// </summary>
    private static void AssertObjectivesPreviewMatchesConstructChain(
        MissionDefinition mission,
        (string previewContains, string primaryTarget)[] chain)
    {
        Assert.NotNull(mission.Briefing);
        Assert.NotNull(mission.Objectives);

        var constructPrimaries = mission.Objectives.Primary
            .Where(o => string.Equals(o.Type, "construct", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var (previewContains, primaryTarget) in chain)
        {
            Assert.Contains(mission.Briefing.ObjectivesPreview, line =>
                line.Contains(previewContains, StringComparison.OrdinalIgnoreCase));

            Assert.Contains(constructPrimaries, o =>
                string.Equals(o.Target, primaryTarget, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ── training_01_interceptor ──────────────────────────────────────────────

    [Fact]
    public void Training01_loads_with_id_and_display_name()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor");

        Assert.NotNull(mission);
        Assert.Equal("training_01_interceptor", mission.Id);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Training01_has_null_prerequisite_and_star_map()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        Assert.Null(mission.PrerequisiteMissionId);
        Assert.False(string.IsNullOrWhiteSpace(mission.Map));
        Assert.Equal("sector_alpha", mission.Map);
        Assert.False(string.IsNullOrWhiteSpace(mission.PlanetName));
        Assert.Equal(2, mission.StarMapPosition.Length);
    }

    [Fact]
    public void Training01_starts_with_single_interceptor_no_free_base()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Equal(new[] { "interceptor_mk2" }, mission.StartConditions.StartingUnits);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.Empty(mission.StartConditions.StartingBuildings ?? []);
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(mission.StartConditions));
    }

    [Fact]
    public void Training01_primary_is_destroy_target_enemy_interceptor()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        Assert.NotNull(mission.Objectives);
        Assert.NotEmpty(mission.Objectives.Primary);
        AssertPrimaryObjectiveFields(mission.Objectives.Primary);

        var destroy = mission.Objectives.Primary
            .Where(o => string.Equals(o.Type, "destroy_target", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(destroy);
        Assert.Contains(destroy, o =>
            string.Equals(o.Target, "enemy_interceptor_1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Training01_victory_and_defeat_conditions()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);

        Assert.NotNull(mission.Defeat);
        Assert.Equal("unit_destroyed", mission.Defeat.Type);
        Assert.False(string.IsNullOrWhiteSpace(mission.Defeat.Target));
    }

    [Fact]
    public void TrainingMission01_briefing_states_player_interceptor_defeat_condition()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        Assert.NotNull(mission.Defeat);
        Assert.Equal("unit_destroyed", mission.Defeat.Type);
        Assert.Equal("player_interceptor", mission.Defeat.Target);

        Assert.NotNull(mission.Briefing);
        Assert.False(string.IsNullOrWhiteSpace(mission.Briefing.Text));
        Assert.NotEmpty(mission.Briefing.ObjectivesPreview);

        string[] defeatKeywords = ["destroyed", "lose", "defeat", "fail"];
        string[] playerShipKeywords = ["your", "interceptor", "ship"];

        bool briefingMentionsDefeat =
            defeatKeywords.Any(k => mission.Briefing.Text.Contains(k, StringComparison.OrdinalIgnoreCase))
            && playerShipKeywords.Any(k => mission.Briefing.Text.Contains(k, StringComparison.OrdinalIgnoreCase));

        bool previewMentionsDefeat = mission.Briefing.ObjectivesPreview.Any(line =>
            defeatKeywords.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase))
            && playerShipKeywords.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase)));

        Assert.True(
            briefingMentionsDefeat || previewMentionsDefeat,
            "Briefing text or objectivesPreview must state that losing the player interceptor causes defeat.");
    }

    [Fact]
    public void Training01_spawns_hostile_interceptor_without_stations()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        var spawnActions = mission.Triggers
            .SelectMany(t => t.Actions ?? [])
            .Where(a => string.Equals(a.Type, "spawn_units", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Contains(spawnActions, a =>
            a.Units != null
            && a.Units.Contains("interceptor_mk2")
            && string.Equals(a.Tag, "enemy_interceptor_1", StringComparison.OrdinalIgnoreCase)
            && (a.Faction is null or 2));

        // No station/base type ids in any spawn action units.
        foreach (var action in spawnActions)
        {
            var units = action.Units ?? [];
            Assert.DoesNotContain(units, u =>
                KnownStationTypeIds.Contains(u, StringComparer.OrdinalIgnoreCase));
        }

        Assert.Empty(mission.StartConditions!.StartingBuildings ?? []);
    }

    [Fact]
    public void TrainingMission01_enemy_spawn_is_faction_2_with_tag()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        var spawnTrigger = mission.Triggers
            .FirstOrDefault(t => string.Equals(t.Id, "spawn_red_interceptor", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(spawnTrigger);
        Assert.True(spawnTrigger!.OneShot);

        Assert.NotNull(spawnTrigger.Condition);
        Assert.Equal("timer", spawnTrigger.Condition!.Type, ignoreCase: true);
        Assert.True(spawnTrigger.Condition.Seconds >= 2f,
            "spawn_red_interceptor must delay at least 2s before hostile entry");

        var spawnAction = spawnTrigger.Actions
            .FirstOrDefault(a => string.Equals(a.Type, "spawn_units", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(spawnAction);
        Assert.NotNull(spawnAction!.Units);
        Assert.Contains("interceptor_mk2", spawnAction.Units);
        Assert.Equal("enemy_interceptor_1", spawnAction.Tag);
        Assert.Equal(2, spawnAction.Faction ?? 2);
        Assert.Equal(new[] { 45, 45 }, spawnAction.Position);
    }

    [Fact]
    public void TrainingMission01_no_starting_buildings_or_default_base()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Empty(mission.StartConditions!.StartingBuildings ?? []);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(mission.StartConditions));

        Assert.NotNull(mission.StartConditions.StartingResources);
        Assert.Equal(0f, mission.StartConditions.StartingResources!.Energy);
        Assert.Equal(0f, mission.StartConditions.StartingResources.Minerals);
        Assert.Equal(0f, mission.StartConditions.StartingResources.Data);
        Assert.Equal(0f, mission.StartConditions.StartingResources.Crew);
    }

    [Fact]
    public void TrainingMission01_demoScript_waits_for_spawn_before_attack()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_01_interceptor")!;

        Assert.NotNull(mission.DemoScript);
        int attackMoveIndex = Array.FindIndex(mission.DemoScript, s =>
            string.Equals(s.Type, "attack_move", StringComparison.OrdinalIgnoreCase));
        Assert.True(attackMoveIndex >= 0, "demoScript must include attack_move toward hostile spawn");

        float waitBeforeAttack = 0f;
        for (int i = 0; i < attackMoveIndex; i++)
        {
            if (string.Equals(mission.DemoScript[i].Type, "wait", StringComparison.OrdinalIgnoreCase))
                waitBeforeAttack += mission.DemoScript[i].Seconds;
        }

        Assert.True(waitBeforeAttack >= 2.5f,
            "demoScript must wait at least 2.5s before attack_move so spawn_red_interceptor fires first");
    }

    // ── training_02_building ─────────────────────────────────────────────────

    [Fact]
    public void Training02_loads_with_id_and_display_name()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building");

        Assert.NotNull(mission);
        Assert.Equal("training_02_building", mission.Id);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Training02_has_null_prerequisite_and_builder_start()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;

        Assert.Null(mission.PrerequisiteMissionId);
        Assert.NotNull(mission.StartConditions);
        Assert.Equal(new[] { "support_repair" }, mission.StartConditions.StartingUnits);
        Assert.True(mission.StartConditions.UnlimitedResources);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.Empty(mission.StartConditions.StartingBuildings ?? []);
    }

    [Fact]
    public void Training02_has_no_enemy_spawn_units()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;

        var spawnActions = mission.Triggers
            .SelectMany(t => t.Actions ?? [])
            .Where(a => string.Equals(a.Type, "spawn_units", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(spawnActions);
    }

    [Fact]
    public void Training02_has_four_construct_primaries()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;

        Assert.NotNull(mission.Objectives);
        AssertPrimaryObjectiveFields(mission.Objectives.Primary);

        var constructTargets = mission.Objectives.Primary
            .Where(o => string.Equals(o.Type, "construct", StringComparison.OrdinalIgnoreCase))
            .Select(o => o.Target ?? string.Empty)
            .ToArray();

        string[] expected =
        [
            "command_center:1",
            "power_reactor:1",
            "shipyard_small:1",
            "unit:fighter_basic:1",
        ];

        foreach (string target in expected)
        {
            Assert.Contains(constructTargets, t =>
                string.Equals(t, target, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void TrainingMission02_has_power_reactor_construct_primary()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;

        var buildPower = mission.Objectives!.Primary
            .FirstOrDefault(o => string.Equals(o.Id, "build_power", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(buildPower);
        Assert.Equal("construct", buildPower.Type, ignoreCase: true);
        Assert.Equal("power_reactor:1", buildPower.Target);
        Assert.Equal("Place a Power Reactor", buildPower.Description);
    }

    [Fact]
    public void TrainingMission02_objectivesPreview_matches_construct_chain()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;

        AssertObjectivesPreviewMatchesConstructChain(mission,
        [
            ("Base HQ", "command_center:1"),
            ("Power Reactor", "power_reactor:1"),
            ("Shipyard", "shipyard_small:1"),
            ("fighter", "unit:fighter_basic:1"),
        ]);
    }

    [Fact]
    public void TrainingMission02_build_chain_matches_build_map_prerequisites()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;
        var catalog = CreateBuildMapCatalog();

        string[] buildingChain = ["command_center", "power_reactor", "shipyard_small"];
        var built = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string buildingId in buildingChain)
        {
            var prerequisites = catalog.GetPrerequisites(buildingId);
            Assert.True(
                BuildMapCatalog.IsUnlocked(prerequisites, built),
                $"Mission construct order places '{buildingId}' before prerequisites " +
                $"[{string.Join(", ", prerequisites)}] are satisfied by prior steps.");
            built.Add(buildingId);
        }

        var powerPrereqs = catalog.GetPrerequisites("power_reactor");
        Assert.Contains("command_center", powerPrereqs);

        var shipyardPrereqs = catalog.GetPrerequisites("shipyard_small");
        Assert.Contains("command_center", shipyardPrereqs);
        Assert.Contains("power_reactor", shipyardPrereqs);

        var primaryIds = mission.Objectives!.Primary.Select(o => o.Id).ToArray();
        int hqIdx = Array.IndexOf(primaryIds, "build_hq");
        int powerIdx = Array.IndexOf(primaryIds, "build_power");
        int shipyardIdx = Array.IndexOf(primaryIds, "build_shipyard");
        int fighterIdx = Array.IndexOf(primaryIds, "produce_fighter");

        Assert.True(hqIdx >= 0 && powerIdx >= 0 && shipyardIdx >= 0 && fighterIdx >= 0);
        Assert.True(hqIdx < powerIdx && powerIdx < shipyardIdx && shipyardIdx < fighterIdx,
            "Primary objectives must teach HQ → power → shipyard → fighter in order.");
    }

    [Fact]
    public void Training02_victory_and_briefing()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);

        Assert.NotNull(mission.Briefing);
        Assert.False(string.IsNullOrWhiteSpace(mission.Briefing.Text));
        Assert.NotEmpty(mission.Briefing.ObjectivesPreview);
    }

    [Fact]
    public void Training02_construct_targets_remain_incomplete_with_empty_world()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_02_building")!;

        Assert.NotNull(mission.Objectives);
        var primaries = mission.Objectives.Primary
            .Where(o => string.Equals(o.Type, "construct", StringComparison.OrdinalIgnoreCase))
            .Select(o => new ObjectiveDefinition
            {
                Id = o.Id,
                Type = o.Type,
                Target = o.Target,
                Description = o.Description,
            })
            .ToArray();

        Assert.Equal(4, primaries.Length);

        // Prove construct target strings are recognized: empty world keeps them incomplete.
        foreach (var obj in primaries)
        {
            Assert.False(string.IsNullOrWhiteSpace(obj.Target));
            string[] parts = obj.Target!.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.True(parts.Length >= 2, $"Construct target '{obj.Target}' should parse as type:count or unit:type:count");
            Assert.True(int.TryParse(parts[^1], out int count) && count >= 1,
                $"Construct target '{obj.Target}' should end with a positive count");
        }

        var def = new MissionDefinition
        {
            Id = "training_02_building_clone",
            Map = mission.Map,
            Objectives = new ObjectivesDefinition
            {
                Primary = primaries,
                Secondary = [],
            },
            Triggers = [],
            Victory = new EndConditionDefinition { Type = "all_primary_complete" },
            Defeat = new EndConditionDefinition { Type = "unit_destroyed", Target = "player_support" },
        };

        var state = new MissionState(def) { Phase = MissionPhase.InProgress };
        var bus = new EventBus();
        var system = new ObjectiveSystem(state, bus);
        var world = new World();

        system.Update(world, 0.016f);

        Assert.All(state.PrimaryObjectives, o => Assert.False(o.IsCompleted));
    }

    // ── training_03_harvest ──────────────────────────────────────────────────

    [Fact]
    public void Training03_loads_with_id_and_display_name()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest");

        Assert.NotNull(mission);
        Assert.Equal("training_03_harvest", mission.Id);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Training03_has_null_prerequisite_and_star_map()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.Null(mission.PrerequisiteMissionId);
        Assert.False(string.IsNullOrWhiteSpace(mission.Map));
        Assert.Equal("sector_alpha", mission.Map);
        Assert.False(string.IsNullOrWhiteSpace(mission.PlanetName));
        Assert.Equal(2, mission.StarMapPosition.Length);
    }

    [Fact]
    public void Training03_starts_with_hq_builder_and_miner()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Contains("support_repair", mission.StartConditions.StartingUnits);
        Assert.Contains("miner_basic", mission.StartConditions.StartingUnits);
        Assert.Contains("command_center", mission.StartConditions.StartingBuildings);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(mission.StartConditions));
    }

    [Fact]
    public void Training03_primary_construct_refinery_and_collect_minerals()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.NotNull(mission.Objectives);
        AssertPrimaryObjectiveFields(mission.Objectives.Primary);

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
    public void Training03_victory_and_defeat_conditions()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);

        Assert.NotNull(mission.Defeat);
        Assert.Equal("base_destroyed", mission.Defeat.Type);
    }

    [Fact]
    public void Training03_ux_clarity_baseline()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_03_harvest")!;

        Assert.False(string.IsNullOrWhiteSpace(mission.DisplayName));
        Assert.False(string.IsNullOrWhiteSpace(mission.Description));
        Assert.NotNull(mission.Briefing);
        Assert.False(string.IsNullOrWhiteSpace(mission.Briefing.Text));
        Assert.NotEmpty(mission.Briefing.ObjectivesPreview);
    }

    // ── training_04_defense ──────────────────────────────────────────────────

    [Fact]
    public void Training04_loads_with_id_and_display_name()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense");

        Assert.NotNull(mission);
        Assert.Equal("training_04_defense", mission.Id);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Training04_has_null_prerequisite_and_star_map()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.Null(mission.PrerequisiteMissionId);
        Assert.False(string.IsNullOrWhiteSpace(mission.Map));
        Assert.Equal("sector_alpha", mission.Map);
        Assert.False(string.IsNullOrWhiteSpace(mission.PlanetName));
        Assert.Equal(2, mission.StarMapPosition.Length);
    }

    [Fact]
    public void Training04_starts_with_hq_and_builder()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Contains("support_repair", mission.StartConditions.StartingUnits);
        Assert.Contains("command_center", mission.StartConditions.StartingBuildings);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(mission.StartConditions));
    }

    [Fact]
    public void Training04_primary_construct_five_turrets()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.NotNull(mission.Objectives);
        AssertPrimaryObjectiveFields(mission.Objectives.Primary);

        var construct = mission.Objectives.Primary
            .FirstOrDefault(o => string.Equals(o.Type, "construct", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(construct);
        Assert.Equal("defense_turret:5", construct.Target);
    }

    [Fact]
    public void Training04_victory_and_defeat_conditions()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);

        Assert.NotNull(mission.Defeat);
        Assert.Equal("unit_destroyed", mission.Defeat.Type);
        Assert.False(string.IsNullOrWhiteSpace(mission.Defeat.Target));
    }

    [Fact]
    public void Training04_ux_clarity_baseline()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_04_defense")!;

        Assert.False(string.IsNullOrWhiteSpace(mission.DisplayName));
        Assert.False(string.IsNullOrWhiteSpace(mission.Description));
        Assert.NotNull(mission.Briefing);
        Assert.False(string.IsNullOrWhiteSpace(mission.Briefing.Text));
        Assert.NotEmpty(mission.Briefing.ObjectivesPreview);
    }

    // ── training_05_tech_tree ────────────────────────────────────────────────

    [Fact]
    public void Training05_loads_with_id_and_display_name()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_05_tech_tree");

        Assert.NotNull(mission);
        Assert.Equal("training_05_tech_tree", mission.Id);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Training05_has_null_prerequisite_and_star_map()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_05_tech_tree")!;

        Assert.Null(mission.PrerequisiteMissionId);
        Assert.False(string.IsNullOrWhiteSpace(mission.Map));
        Assert.Equal("sector_alpha", mission.Map);
        Assert.False(string.IsNullOrWhiteSpace(mission.PlanetName));
        Assert.Equal(2, mission.StarMapPosition.Length);
    }

    [Fact]
    public void Training05_starts_with_builder_no_free_base()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_05_tech_tree")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Equal(new[] { "support_repair" }, mission.StartConditions.StartingUnits);
        Assert.True(mission.StartConditions.UnlimitedResources);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.Empty(mission.StartConditions.StartingBuildings ?? []);
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(mission.StartConditions));
    }

    [Fact]
    public void Training05_primary_construct_objectives_include_command_center()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_05_tech_tree")!;

        Assert.NotNull(mission.Objectives);
        AssertPrimaryObjectiveFields(mission.Objectives.Primary);

        Assert.All(mission.Objectives.Primary, o =>
            Assert.Equal("construct", o.Type, ignoreCase: true));

        var constructTargets = mission.Objectives.Primary
            .Select(o => o.Target ?? string.Empty)
            .ToArray();

        Assert.Contains(constructTargets, t =>
            string.Equals(t, "command_center:1", StringComparison.OrdinalIgnoreCase));
        Assert.True(mission.Objectives.Primary.Length >= 16,
            "Full tech tree mission should declare at least 16 construct primaries.");
    }

    [Fact]
    public void Training05_victory_and_defeat_conditions()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_05_tech_tree")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);

        Assert.NotNull(mission.Defeat);
        Assert.Equal("unit_destroyed", mission.Defeat.Type);
        Assert.False(string.IsNullOrWhiteSpace(mission.Defeat.Target));
    }

    [Fact]
    public void Training05_ux_clarity_baseline()
    {
        var loader = CreateLoader();
        var mission = loader.Load("training_05_tech_tree")!;

        Assert.False(string.IsNullOrWhiteSpace(mission.DisplayName));
        Assert.False(string.IsNullOrWhiteSpace(mission.Description));
        Assert.NotNull(mission.Briefing);
        Assert.False(string.IsNullOrWhiteSpace(mission.Briefing.Text));
        Assert.NotEmpty(mission.Briefing.ObjectivesPreview);
    }

    private static readonly JsonSerializerOptions BuildMapJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static BuildMapCatalog CreateBuildMapCatalog()
    {
        string path = Path.Combine(GetTestDataPath(), "Config", "build_map.json");
        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BuildMapConfig>(json, BuildMapJsonOptions);
        Assert.NotNull(config);

        var defs = new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);
        string basesPath = Path.Combine(GetTestDataPath(), "Bases");
        foreach (string file in Directory.GetFiles(basesPath, "*.json"))
        {
            if (Path.GetFileName(file).StartsWith("_", StringComparison.Ordinal)) continue;
            string baseJson = File.ReadAllText(file);
            var def = JsonSerializer.Deserialize<EntityDefinition>(baseJson, BuildMapJsonOptions);
            if (def?.Id != null)
                defs[def.Id] = def;
        }

        return new BuildMapCatalog(config!, defs);
    }

    // ── LoadAll inclusion ────────────────────────────────────────────────────

    [Fact]
    public void LoadAll_includes_all_five_training_missions()
    {
        var loader = CreateLoader();
        string missionsPath = Path.Combine(GetTestDataPath(), "Missions");
        var missions = loader.LoadAll(missionsPath);

        string[] expectedIds =
        [
            "training_01_interceptor",
            "training_02_building",
            "training_03_harvest",
            "training_04_defense",
            "training_05_tech_tree",
        ];

        foreach (string id in expectedIds)
            Assert.Contains(missions, m => m.Id == id);
    }
}
