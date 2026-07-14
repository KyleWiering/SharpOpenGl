using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

public class MissionLoaderTests
{
    private static string GetTestDataPath()
    {
        // Navigate from test bin output to GameData
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

    // ── Load single mission ──────────────────────────────────────────────────

    [Fact]
    public void Load_returns_mission_for_valid_file()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01");

        Assert.NotNull(mission);
        Assert.Equal("tutorial_01", mission.Id);
        Assert.Equal("First Contact", mission.DisplayName);
    }

    [Fact]
    public void Load_ship_gallery_includes_full_roster()
    {
        var loader = CreateLoader();
        var mission = loader.Load("ship_gallery");

        Assert.NotNull(mission);
        Assert.Equal("ship_gallery", mission.Id);
        Assert.Single(mission.StartConditions!.StartingUnits);
        Assert.Equal("hero_default", mission.StartConditions.StartingUnits[0]);
        Assert.Empty(mission.Triggers);
        Assert.Null(mission.PrerequisiteMissionId);
    }

    [Fact]
    public void Load_returns_null_for_missing_file()
    {
        var loader = CreateLoader();
        var mission = loader.Load("nonexistent_mission");

        Assert.Null(mission);
    }

    [Fact]
    public void Load_caches_result()
    {
        var loader = CreateLoader();
        var first = loader.Load("tutorial_01");
        var second = loader.Load("tutorial_01");

        Assert.Same(first, second);
    }

    // ── Mission fields ───────────────────────────────────────────────────────

    [Fact]
    public void Load_parses_map_reference()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.Equal("sector_alpha", mission.Map);
    }

    [Fact]
    public void Load_parses_briefing()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Briefing);
        Assert.Contains("enemy scout", mission.Briefing.Text);
        Assert.NotEmpty(mission.Briefing.ObjectivesPreview);
    }

    [Fact]
    public void Load_parses_start_conditions()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Equal(new[] { 5, 5 }, mission.StartConditions.PlayerSpawn);
        Assert.Contains("hero_default", mission.StartConditions.StartingUnits);
        Assert.NotNull(mission.StartConditions.StartingResources);
        Assert.Equal(500f, mission.StartConditions.StartingResources.Energy);
        // New optional fields: default base spawn + empty starting buildings for existing missions.
        Assert.True(mission.StartConditions.SpawnDefaultBase);
        Assert.Empty(mission.StartConditions.StartingBuildings);
    }

    [Fact]
    public void StartConditions_deserializes_startingBuildings_and_spawnDefaultBase()
    {
        const string json = """
            {
              "id": "training_stub",
              "displayName": "Training Stub",
              "map": "sector_alpha",
              "startConditions": {
                "playerSpawn": [12, 12],
                "startingUnits": ["support_repair", "miner_basic"],
                "startingBuildings": ["command_center"],
                "spawnDefaultBase": false,
                "startingResources": { "energy": 1000, "minerals": 1000, "data": 200, "crew": 50 }
              },
              "objectives": { "primary": [], "secondary": [] },
              "triggers": []
            }
            """;

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        var mission = System.Text.Json.JsonSerializer.Deserialize<MissionDefinition>(json, options);
        Assert.NotNull(mission);
        Assert.NotNull(mission!.StartConditions);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.Equal(new[] { "command_center" }, mission.StartConditions.StartingBuildings);
        Assert.Equal(new[] { "support_repair", "miner_basic" }, mission.StartConditions.StartingUnits);
        Assert.Equal(new[] { 12, 12 }, mission.StartConditions.PlayerSpawn);

        Assert.True(StartConditionsSpawnLogic.HasExplicitStartingBuildings(mission.StartConditions));
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(mission.StartConditions));
    }

    [Fact]
    public void StartConditionsSpawnLogic_preserves_legacy_default_base()
    {
        Assert.True(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(null));
        Assert.True(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(new StartConditionsDefinition()));
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(
            new StartConditionsDefinition { SpawnDefaultBase = false }));
        Assert.False(StartConditionsSpawnLogic.ShouldSpawnDefaultBase(
            new StartConditionsDefinition
            {
                SpawnDefaultBase = true,
                StartingBuildings = ["command_center"],
            }));
    }

    [Fact]
    public void Load_parses_objectives()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Objectives);
        Assert.Single(mission.Objectives.Primary);
        Assert.Equal("destroy_scout", mission.Objectives.Primary[0].Id);
        Assert.Equal("destroy_target", mission.Objectives.Primary[0].Type);
        Assert.Single(mission.Objectives.Secondary);
    }

    [Fact]
    public void Load_parses_triggers()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.Equal(2, mission.Triggers.Length);
        var scoutTrigger = mission.Triggers[0];
        Assert.Equal("spawn_enemy_scout", scoutTrigger.Id);
        Assert.Equal(5f, scoutTrigger.Condition!.Seconds);
        Assert.Equal("enemy_scout_1", scoutTrigger.Actions[0].Tag);

        var waveTrigger = mission.Triggers[1];
        Assert.Equal("spawn_wave_1", waveTrigger.Id);
        Assert.NotNull(waveTrigger.Condition);
        Assert.Equal("timer", waveTrigger.Condition.Type);
        Assert.Equal(30f, waveTrigger.Condition.Seconds);
        Assert.Equal(2, waveTrigger.Actions.Length);
        Assert.True(waveTrigger.OneShot);
    }

    [Fact]
    public void Load_parses_victory_and_defeat()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);
        Assert.NotNull(mission.Defeat);
        Assert.Equal("hero_destroyed", mission.Defeat.Type);
    }

    [Fact]
    public void Load_parses_rewards()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Rewards);
        Assert.Equal(100, mission.Rewards.Xp);
        Assert.Contains("fighter_advanced", mission.Rewards.Unlocks);
        Assert.NotNull(mission.Rewards.Resources);
        Assert.Equal(200f, mission.Rewards.Resources.Energy);
    }

    [Fact]
    public void Load_parses_star_map_fields()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.Equal("Helios Prime", mission.PlanetName);
        Assert.Equal(new[] { 0.15f, 0.55f }, mission.StarMapPosition);
        Assert.Equal("#4DA6FF", mission.PlanetColor);
        Assert.Null(mission.PrerequisiteMissionId);
    }

    [Fact]
    public void Load_parses_prerequisite_mission_id()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_02")!;

        Assert.Equal("Asteria Belt", mission.PlanetName);
        Assert.Equal("tutorial_01", mission.PrerequisiteMissionId);
    }

    [Fact]
    public void Load_mission_abandoned_salvage_succeeds()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_abandoned_salvage");

        Assert.NotNull(mission);
        Assert.Equal("mission_abandoned_salvage", mission.Id);
        Assert.Equal("Salvage Run", mission.DisplayName);
    }

    [Fact]
    public void Load_mission_abandoned_salvage_has_star_map_and_defeat_unit_destroyed()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_abandoned_salvage")!;

        Assert.Equal("Driftfield Salvage", mission.PlanetName);
        Assert.Equal(new[] { 0.52f, 0.38f }, mission.StarMapPosition);
        Assert.Equal("#7AB8D4", mission.PlanetColor);
        Assert.Equal("mission_02", mission.PrerequisiteMissionId);

        Assert.NotNull(mission.Defeat);
        Assert.Equal("unit_destroyed", mission.Defeat.Type);
        Assert.Equal("player_support", mission.Defeat.Target);
    }

    [Fact]
    public void Load_mission_abandoned_salvage_has_star_map_briefing_and_objectives()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_abandoned_salvage")!;

        Assert.Equal("Driftfield Salvage", mission.PlanetName);
        Assert.Equal(new[] { 0.52f, 0.38f }, mission.StarMapPosition);
        Assert.Equal("#7AB8D4", mission.PlanetColor);
        Assert.Equal("mission_02", mission.PrerequisiteMissionId);

        Assert.NotNull(mission.Briefing);
        Assert.Contains("abandoned interceptors", mission.Briefing.Text);
        Assert.Equal(4, mission.Briefing.ObjectivesPreview.Length);

        Assert.NotNull(mission.Objectives);
        Assert.Equal(4, mission.Objectives.Primary.Length);
        Assert.Equal(2, mission.Objectives.Primary.Count(o => o.Type == "repair_target"));
        Assert.Equal("derelict_1", mission.Objectives.Primary[1].Target);
        Assert.Equal("derelict_2", mission.Objectives.Primary[3].Target);
    }

    [Fact]
    public void Load_mission_abandoned_salvage_has_demo_safe_spawns_and_defeat()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_abandoned_salvage")!;

        Assert.Equal(new[] { 8, 12 }, mission.StartConditions!.PlayerSpawn);
        Assert.Contains("support_repair", mission.StartConditions.StartingUnits);

        var spawnTrigger = mission.Triggers.First(t => t.Id == "spawn_derelicts");
        var derelictSpawns = spawnTrigger.Actions
            .Where(a => a.Type == "spawn_units")
            .ToArray();
        Assert.Equal(2, derelictSpawns.Length);
        Assert.Equal(new[] { 28, 18 }, derelictSpawns[0].Position);
        Assert.Equal("derelict_1", derelictSpawns[0].Tag);
        Assert.Equal(new[] { 44, 30 }, derelictSpawns[1].Position);
        Assert.Equal("derelict_2", derelictSpawns[1].Tag);

        Assert.NotNull(mission.Defeat);
        Assert.Equal("unit_destroyed", mission.Defeat.Type);

        var repairSteps = mission.DemoScript
            .Where(s => s.Type == "repair_target")
            .ToArray();
        Assert.Equal(2, repairSteps.Length);
        Assert.Equal("derelict_1", repairSteps[0].TargetTag);
        Assert.Equal("derelict_2", repairSteps[1].TargetTag);
    }

    [Fact]
    public void LoadAll_includes_mission_abandoned_salvage()
    {
        var loader = CreateLoader();
        string missionsPath = Path.Combine(GetTestDataPath(), "Missions");
        var missions = loader.LoadAll(missionsPath);

        Assert.Contains(missions, m => m.Id == "mission_abandoned_salvage");
    }

    // ── mission_build_tree ───────────────────────────────────────────────────

    [Fact]
    public void Load_mission_build_tree_succeeds()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_build_tree");

        Assert.NotNull(mission);
        Assert.Equal("mission_build_tree", mission.Id);
        Assert.Equal("Foundation Protocol", mission.DisplayName);
        Assert.Equal("sector_alpha", mission.Map);
    }

    [Fact]
    public void Load_mission_build_tree_has_star_map_and_null_prerequisite()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_build_tree")!;

        Assert.Equal("Architect's Proving Ground", mission.PlanetName);
        Assert.Equal(new[] { 0.68f, 0.72f }, mission.StarMapPosition);
        Assert.Null(mission.PrerequisiteMissionId);
    }

    [Fact]
    public void Load_mission_build_tree_starts_with_support_repair()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_build_tree")!;

        Assert.Contains("support_repair", mission.StartConditions!.StartingUnits);
        Assert.NotNull(mission.StartConditions.StartingResources);
        Assert.True(mission.StartConditions.StartingResources.Energy > 0);
        Assert.True(mission.StartConditions.StartingResources.Minerals > 0);
        Assert.True(mission.StartConditions.StartingResources.Data > 0);
        Assert.True(mission.StartConditions.StartingResources.Crew > 0);
    }

    [Fact]
    public void LoadAll_includes_mission_build_tree()
    {
        var loader = CreateLoader();
        string missionsPath = Path.Combine(GetTestDataPath(), "Missions");
        var missions = loader.LoadAll(missionsPath);

        Assert.Contains(missions, m => m.Id == "mission_build_tree");
    }

    [Fact]
    public void Load_mission_build_tree_defeat_unit_destroyed()
    {
        var loader = CreateLoader();
        var mission = loader.Load("mission_build_tree")!;

        Assert.NotNull(mission.Defeat);
        Assert.Equal("unit_destroyed", mission.Defeat.Type);
        Assert.Equal("player_support", mission.Defeat.Target);
    }

    // ── LoadAll ──────────────────────────────────────────────────────────────

    [Fact]
    public void LoadAll_finds_missions_excluding_templates()
    {
        var loader = CreateLoader();
        string missionsPath = Path.Combine(GetTestDataPath(), "Missions");
        var missions = loader.LoadAll(missionsPath);

        Assert.NotEmpty(missions);
        Assert.All(missions, m => Assert.False(m.Id.StartsWith('_')));
    }

    [Fact]
    public void LoadAll_returns_empty_for_missing_directory()
    {
        var loader = CreateLoader();
        var missions = loader.LoadAll("/nonexistent/path");

        Assert.Empty(missions);
    }

    // ── Exists / Invalidate ──────────────────────────────────────────────────

    [Fact]
    public void Exists_returns_true_for_valid_mission()
    {
        var loader = CreateLoader();
        Assert.True(loader.Exists("tutorial_01"));
    }

    [Fact]
    public void Exists_returns_false_for_missing_mission()
    {
        var loader = CreateLoader();
        Assert.False(loader.Exists("nonexistent"));
    }

    [Fact]
    public void Invalidate_clears_cache()
    {
        var loader = CreateLoader();
        var first = loader.Load("tutorial_01");
        loader.Invalidate("tutorial_01");
        var second = loader.Load("tutorial_01");

        // After invalidation, a new instance is loaded
        Assert.NotSame(first, second);
        Assert.Equal(first!.Id, second!.Id);
    }
}