using System.Text.Json;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>
/// Loader / schema tests for training_05_tech_tree — full 16-structure tech tree.
/// </summary>
public class TrainingMission05TechTreeTests
{
    private const string MissionId = "training_05_tech_tree";

    /// <summary>Canonical 16 building ids — must match GameData/Config/build_map.json.</summary>
    private static readonly HashSet<string> CanonicalBuildMapIds = new(StringComparer.Ordinal)
    {
        "command_center",
        "shipyard_small",
        "shipyard_medium",
        "shipyard_large",
        "power_reactor",
        "resource_refinery",
        "supply_depot",
        "fabrication_hub",
        "sensor_array",
        "defense_turret",
        "shield_emitter",
        "missile_battery",
        "repair_bay",
        "comms_relay",
        "orbital_uplink",
        "fortress_core",
    };

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

    private static MissionDefinition LoadMission()
    {
        var mission = CreateLoader().Load(MissionId);
        Assert.NotNull(mission);
        return mission!;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

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

    private static Dictionary<string, string> LoadEntityDisplayNames()
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string basesPath = Path.Combine(GetTestDataPath(), "Bases");
        foreach (string file in Directory.GetFiles(basesPath, "*.json"))
        {
            if (Path.GetFileName(file).StartsWith('_')) continue;
            var def = JsonSerializer.Deserialize<EntityDefinition>(File.ReadAllText(file), JsonOptions);
            if (def?.Id != null)
                names[def.Id] = def.DisplayName;
        }

        return names;
    }

    /// <summary>
    /// Extract building definition id from construct target (<c>id:count</c> or bare id).
    /// Unit constructs (<c>unit:id:count</c>) return null.
    /// </summary>
    private static string? ExtractBuildingIdFromConstructTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return null;

        string[] parts = target.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return null;

        // unit:definitionId:count
        if (parts[0].Equals("unit", StringComparison.OrdinalIgnoreCase))
            return null;

        return parts[0];
    }

    // ── Load succeeds ────────────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_Load_succeeds()
    {
        var mission = LoadMission();

        Assert.Equal(MissionId, mission.Id);
        Assert.Contains("Training", mission.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    // ── Accessible ───────────────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_is_accessible_without_prerequisite()
    {
        var mission = LoadMission();
        Assert.Null(mission.PrerequisiteMissionId);
    }

    // ── Start builder ────────────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_starts_with_support_repair_and_high_or_unlimited_resources()
    {
        var mission = LoadMission();
        Assert.NotNull(mission.StartConditions);
        Assert.Contains("support_repair", mission.StartConditions.StartingUnits);

        bool unlimited = mission.StartConditions.UnlimitedResources;
        var resources = mission.StartConditions.StartingResources;
        bool highResources = resources != null
            && resources.Energy >= 1000f
            && resources.Minerals >= 1000f;

        Assert.True(unlimited || highResources,
            "Expected UnlimitedResources or high starting resources for the full tech tree.");
    }

    // ── No free base ─────────────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_does_not_spawn_default_base()
    {
        var mission = LoadMission();
        Assert.NotNull(mission.StartConditions);
        Assert.False(mission.StartConditions.SpawnDefaultBase);
        Assert.Empty(mission.StartConditions.StartingBuildings);
    }

    // ── Full tree objectives ─────────────────────────────────────────────────

    [Fact]
    public void All_sixteen_build_map_structures_have_construct_objectives()
    {
        var mission = LoadMission();
        Assert.NotNull(mission.Objectives);
        Assert.Equal(16, mission.Objectives.Primary.Length);

        var constructedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var objective in mission.Objectives.Primary)
        {
            Assert.Equal("construct", objective.Type, ignoreCase: true);
            string? buildingId = ExtractBuildingIdFromConstructTarget(objective.Target);
            Assert.False(string.IsNullOrEmpty(buildingId),
                $"Primary objective '{objective.Id}' target '{objective.Target}' did not yield a building id.");
            constructedIds.Add(buildingId!);
        }

        Assert.True(CanonicalBuildMapIds.SetEquals(constructedIds),
            "Every build_map structure must have exactly one construct primary. " +
            $"Missing: {string.Join(", ", CanonicalBuildMapIds.Except(constructedIds))}; " +
            $"Extra: {string.Join(", ", constructedIds.Except(CanonicalBuildMapIds))}");
    }

    [Fact]
    public void Construct_order_respects_build_map_prerequisites()
    {
        var mission = LoadMission();
        var catalog = CreateBuildMapCatalog();
        var built = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var placeOrder = mission.DemoScript
            .Where(s => string.Equals(s.Type, "place_building", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.BuildingId!)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        Assert.Equal(CanonicalBuildMapIds.Count, placeOrder.Count);

        foreach (string buildingId in placeOrder)
        {
            var prerequisites = catalog.GetPrerequisites(buildingId);
            Assert.True(
                BuildMapCatalog.IsUnlocked(prerequisites, built),
                $"Building '{buildingId}' is not unlocked with built set [{string.Join(", ", built)}]. " +
                $"Missing prereqs: {string.Join(", ", prerequisites.Where(p => !built.Contains(p)))}");

            built.Add(buildingId);
        }

        Assert.True(CanonicalBuildMapIds.SetEquals(built));
    }

    [Fact]
    public void TrainingMission05_objective_descriptions_match_entity_display_names()
    {
        var mission = LoadMission();
        var displayNames = LoadEntityDisplayNames();

        foreach (var objective in mission.Objectives!.Primary)
        {
            string? buildingId = ExtractBuildingIdFromConstructTarget(objective.Target);
            Assert.False(string.IsNullOrEmpty(buildingId));
            Assert.True(displayNames.TryGetValue(buildingId!, out string? displayName),
                $"Missing entity definition for '{buildingId}'.");

            Assert.Equal($"Build {displayName}", objective.Description);
        }
    }

    [Fact]
    public void TrainingMission05_has_no_triggers()
    {
        var mission = LoadMission();
        Assert.NotNull(mission.Triggers);
        Assert.Empty(mission.Triggers);
    }

    [Fact]
    public void TrainingMission05_primary_objectives_cover_full_16_structure_tech_tree()
    {
        var mission = LoadMission();
        Assert.NotNull(mission.Objectives);
        Assert.NotEmpty(mission.Objectives.Primary);

        Assert.All(mission.Objectives.Primary, o =>
            Assert.Equal("construct", o.Type));

        var constructedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var objective in mission.Objectives.Primary)
        {
            string? buildingId = ExtractBuildingIdFromConstructTarget(objective.Target);
            Assert.False(string.IsNullOrEmpty(buildingId),
                $"Primary objective '{objective.Id}' target '{objective.Target}' did not yield a building id.");
            constructedIds.Add(buildingId!);
        }

        Assert.Equal(CanonicalBuildMapIds.Count, constructedIds.Count);
        Assert.True(CanonicalBuildMapIds.SetEquals(constructedIds),
            "Construct targets must be set-equal to the full build_map set. " +
            $"Missing: {string.Join(", ", CanonicalBuildMapIds.Except(constructedIds))}; " +
            $"Extra: {string.Join(", ", constructedIds.Except(CanonicalBuildMapIds))}");
    }

    // ── Victory ──────────────────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_victory_is_all_primary_complete()
    {
        var mission = LoadMission();
        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);
    }

    // ── LoadAll ──────────────────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_LoadAll_includes_mission()
    {
        var loader = CreateLoader();
        string missionsPath = Path.Combine(GetTestDataPath(), "Missions");
        var missions = loader.LoadAll(missionsPath);

        Assert.Contains(missions, m => m.Id == MissionId);
    }

    // ── Old mission intact ───────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_mission_build_tree_still_loads()
    {
        var mission = CreateLoader().Load("mission_build_tree");

        Assert.NotNull(mission);
        Assert.Equal("mission_build_tree", mission.Id);
    }

    // ── demoScript coverage ──────────────────────────────────────────────────

    [Fact]
    public void TrainingMission05_demoScript_places_all_16_buildings()
    {
        var mission = LoadMission();
        Assert.NotEmpty(mission.DemoScript);

        var placeSteps = mission.DemoScript
            .Where(s => string.Equals(s.Type, "place_building", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(placeSteps);

        var placedIds = new HashSet<string>(
            placeSteps
                .Select(s => s.BuildingId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!),
            StringComparer.Ordinal);

        Assert.True(CanonicalBuildMapIds.SetEquals(placedIds),
            "demoScript place_building steps must cover the full 16-building set. " +
            $"Missing: {string.Join(", ", CanonicalBuildMapIds.Except(placedIds))}; " +
            $"Extra: {string.Join(", ", placedIds.Except(CanonicalBuildMapIds))}");
    }

    [Fact]
    public void TrainingMission05_demoScript_places_command_center_before_other_buildings()
    {
        var mission = LoadMission();
        var placeOrder = mission.DemoScript
            .Where(s => string.Equals(s.Type, "place_building", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.BuildingId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();

        Assert.NotEmpty(placeOrder);
        int ccIndex = placeOrder.FindIndex(id =>
            id.Equals("command_center", StringComparison.OrdinalIgnoreCase));
        Assert.True(ccIndex >= 0, "demoScript must place command_center.");
        Assert.Equal(0, ccIndex);
    }

    /// <summary>
    /// Maps construct-primary building id to objective id (e.g. <c>command_center</c> → <c>build_command_center</c>).
    /// Objective ids follow <c>build_{buildingId}</c> for all 16 tech-tree primaries.
    /// </summary>
    private static Dictionary<string, string> BuildConstructPrimaryObjectiveMap(MissionDefinition mission)
    {
        Assert.NotNull(mission.Objectives);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var objective in mission.Objectives.Primary)
        {
            if (!string.Equals(objective.Type, "construct", StringComparison.OrdinalIgnoreCase))
                continue;

            string? buildingId = ExtractBuildingIdFromConstructTarget(objective.Target);
            Assert.False(string.IsNullOrEmpty(buildingId),
                $"Construct primary '{objective.Id}' target '{objective.Target}' did not yield a building id.");

            map[buildingId!] = objective.Id;
        }

        return map;
    }

    [Fact]
    public void TrainingMission05_demoScript_has_wait_objective_per_construct_primary()
    {
        var mission = LoadMission();
        Assert.NotEmpty(mission.DemoScript);

        var buildingToObjective = BuildConstructPrimaryObjectiveMap(mission);
        Assert.Equal(CanonicalBuildMapIds.Count, buildingToObjective.Count);

        var steps = mission.DemoScript.ToList();
        var coveredObjectives = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < steps.Count; i++)
        {
            if (!string.Equals(steps[i].Type, "place_building", StringComparison.OrdinalIgnoreCase))
                continue;

            string? buildingId = steps[i].BuildingId;
            Assert.False(string.IsNullOrWhiteSpace(buildingId),
                $"place_building at demoScript index {i} is missing buildingId.");

            Assert.True(buildingToObjective.TryGetValue(buildingId!, out string? expectedObjectiveId),
                $"place_building '{buildingId}' has no matching construct primary objective.");

            Assert.True(i + 1 < steps.Count,
                $"place_building '{buildingId}' at index {i} must be followed by wait_objective.");

            var next = steps[i + 1];
            Assert.True(string.Equals(next.Type, "wait_objective", StringComparison.OrdinalIgnoreCase),
                $"Expected wait_objective immediately after place_building '{buildingId}' at index {i}, " +
                $"found '{next.Type}'.");
            Assert.Equal(expectedObjectiveId, next.ObjectiveId);
            coveredObjectives.Add(expectedObjectiveId);
        }

        var expectedObjectiveIds = buildingToObjective.Values.ToHashSet(StringComparer.Ordinal);
        Assert.True(expectedObjectiveIds.SetEquals(coveredObjectives),
            "Every construct primary must have a place_building → wait_objective pair in demoScript. " +
            $"Missing objectives: {string.Join(", ", expectedObjectiveIds.Except(coveredObjectives))}");
    }
}
