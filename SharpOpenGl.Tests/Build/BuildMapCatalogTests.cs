using System.Text.Json;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Build;

public class BuildMapCatalogTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [Fact]
    public void Build_map_json_loads_five_categories()
    {
        var config = LoadBuildMapConfig();
        Assert.Equal(5, config.Categories.Count);
        Assert.Contains(config.Categories, c => c.DisplayName == "Defense");
        Assert.Contains(config.Categories, c => c.DisplayName == "Economy");
        Assert.Contains(config.Categories, c => c.DisplayName == "Production");
        Assert.Contains(config.Categories, c => c.DisplayName == "Support");
        Assert.Contains(config.Categories, c => c.DisplayName == "Capstone");
    }

    [Fact]
    public void Build_map_json_has_sixteen_structures()
    {
        var config = LoadBuildMapConfig();
        int structureCount = config.Categories.Sum(c => c.Buildings.Count);
        Assert.Equal(16, structureCount);
    }

    [Fact]
    public void Every_build_map_id_has_base_definition()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();

        foreach (var category in config.Categories)
        {
            foreach (var entry in category.Buildings)
            {
                Assert.True(
                    defs.ContainsKey(entry.Id),
                    $"Build map entry '{entry.Id}' has no matching base definition.");
            }
        }
    }

    [Fact]
    public void Shipyard_requires_command_center_and_power_reactor_prerequisites()
    {
        var config = LoadBuildMapConfig();
        var production = config.Categories.First(c => c.Id == "production");
        var shipyard = production.Buildings.First(b => b.Id == "shipyard_small");
        Assert.Contains("command_center", shipyard.Prerequisites);
        Assert.Contains("power_reactor", shipyard.Prerequisites);
    }

    [Theory]
    [MemberData(nameof(PrerequisiteChainCases))]
    public void IsUnlocked_respects_prerequisite_chain(string[] built, string target, bool expected)
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        var catalog = new BuildMapCatalog(config, defs);
        var prerequisites = catalog.GetPrerequisites(target);
        bool unlocked = BuildMapCatalog.IsUnlocked(prerequisites, new HashSet<string>(built));
        Assert.Equal(expected, unlocked);
    }

    public static TheoryData<string[], string, bool> PrerequisiteChainCases => new()
    {
        { new[] { "command_center", "power_reactor" }, "shipyard_small", true },
        { new[] { "command_center" }, "shipyard_small", false },
        { Array.Empty<string>(), "shipyard_small", false },
        { new[] { "command_center", "shipyard_small" }, "repair_bay", true },
        { new[] { "command_center" }, "repair_bay", false },
        { new[] { "command_center" }, "fabrication_hub", false },
        { new[] { "command_center", "resource_refinery", "power_reactor" }, "fabrication_hub", true },
        { new[] { "command_center", "shipyard_medium", "sensor_array", "comms_relay" }, "orbital_uplink", true },
        { new[] { "command_center" }, "fortress_core", false },
    };

    [Fact]
    public void BuildViews_populates_build_time_lock_and_afford_metadata()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        var catalog = new BuildMapCatalog(config, defs);
        var world = new World();
        var resources = new ResourceManager();
        var player = resources.AddPlayer(1);
        player.SetStartingAmount(ResourceType.Energy, 5000);
        player.SetStartingAmount(ResourceType.Minerals, 5);
        player.SetStartingAmount(ResourceType.Data, 5000);
        player.SetStartingAmount(ResourceType.Crew, 5000);

        var cc = world.CreateEntity();
        world.AddComponent(cc, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        var views = catalog.BuildViews(world, 1, resources, supply: null);
        var shipyard = views.SelectMany(c => c.Buildings).First(b => b.Id == "shipyard_small");
        var reactor = views.SelectMany(c => c.Buildings).First(b => b.Id == "power_reactor");

        Assert.Equal(defs["shipyard_small"].BuildTime, shipyard.BuildTime);
        Assert.False(shipyard.IsUnlocked);
        Assert.Equal("Requires: Power Reactor", shipyard.LockReason);
        Assert.Contains("Command Center", shipyard.Prerequisites);
        Assert.Contains("Power Reactor", shipyard.Prerequisites);
        Assert.DoesNotContain(shipyard.Prerequisites, name => name.Contains('_'));

        Assert.True(reactor.IsUnlocked);
        Assert.False(reactor.CanAfford);
        Assert.Equal("Insufficient: minerals", reactor.AffordReason);
        Assert.Null(reactor.LockReason);
        Assert.Equal(30f, reactor.BuildTime);
    }

    [Fact]
    public void BuildViews_marks_shipyard_locked_without_prerequisite_building()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        var catalog = new BuildMapCatalog(config, defs);
        var world = new World();
        var resources = new ResourceManager();
        var player = resources.AddPlayer(1);
        player.SetStartingAmount(ResourceType.Energy, 5000);
        player.SetStartingAmount(ResourceType.Minerals, 5000);
        player.SetStartingAmount(ResourceType.Data, 5000);
        player.SetStartingAmount(ResourceType.Crew, 5000);

        var views = catalog.BuildViews(world, 1, resources, supply: null);
        var shipyard = views.SelectMany(c => c.Buildings).First(b => b.Id == "shipyard_small");

        Assert.False(shipyard.IsUnlocked);
        Assert.False(shipyard.IsSelectable);
    }

    [Fact]
    public void Skirmish_unlock_rules_match_campaign_catalog()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        var catalog = new BuildMapCatalog(config, defs);

        var allIds = config.Categories
            .SelectMany(c => c.Buildings)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(16, allIds.Count);
        Assert.All(allIds, id => Assert.True(defs.ContainsKey(id), $"Build map id '{id}' missing base definition."));

        var builtSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "command_center" };
        string[] placementOrder =
        [
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

        foreach (string buildingId in placementOrder)
        {
            var prerequisites = catalog.GetPrerequisites(buildingId);
            bool unlocked = BuildMapCatalog.IsUnlocked(prerequisites, builtSet);
            Assert.True(unlocked,
                $"{buildingId} should unlock with built set [{string.Join(", ", builtSet)}] (prereqs: [{string.Join(", ", prerequisites)}]).");
            builtSet.Add(buildingId);
        }

        Assert.Equal(16, builtSet.Count);
    }

    [Fact]
    public void Skirmish_starter_does_not_grant_unlock_cheat()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        var catalog = new BuildMapCatalog(config, defs);

        var builtSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "command_center" };
        var shipyardPrereqs = catalog.GetPrerequisites("shipyard_small");

        Assert.False(BuildMapCatalog.IsUnlocked(shipyardPrereqs, builtSet),
            "shipyard_small must stay locked with CC-only starter (requires power_reactor).");
    }

    [Fact]
    public void BuildViews_unlocks_shipyard_when_command_center_and_power_reactor_exist()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        var catalog = new BuildMapCatalog(config, defs);
        var world = new World();
        var cc = world.CreateEntity();
        world.AddComponent(cc, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });
        var reactor = world.CreateEntity();
        world.AddComponent(reactor, new BuildingComponent
        {
            BuildingType = "power_reactor",
            PlayerId = 1,
        });

        var resources = new ResourceManager();
        var player = resources.AddPlayer(1);
        player.SetStartingAmount(ResourceType.Energy, 5000);
        player.SetStartingAmount(ResourceType.Minerals, 5000);
        player.SetStartingAmount(ResourceType.Data, 5000);
        player.SetStartingAmount(ResourceType.Crew, 5000);

        var views = catalog.BuildViews(world, 1, resources, supply: null);
        var shipyard = views.SelectMany(c => c.Buildings).First(b => b.Id == "shipyard_small");

        Assert.True(shipyard.IsUnlocked);
        Assert.True(shipyard.CanAfford);
        Assert.True(shipyard.IsSelectable);
    }

    private static BuildMapConfig LoadBuildMapConfig()
    {
        string path = Path.Combine(GetGameDataPath(), "Config", "build_map.json");
        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BuildMapConfig>(json, Options);
        Assert.NotNull(config);
        return config!;
    }

    private static Dictionary<string, EntityDefinition> LoadAllBaseDefinitions()
    {
        var defs = new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);
        string basesPath = Path.Combine(GetGameDataPath(), "Bases");
        foreach (string file in Directory.GetFiles(basesPath, "*.json"))
        {
            if (Path.GetFileName(file).StartsWith('_')) continue;
            string json = File.ReadAllText(file);
            var def = JsonSerializer.Deserialize<EntityDefinition>(json, Options);
            if (def?.Id != null)
                defs[def.Id] = def;
        }
        return defs;
    }

    private static string GetGameDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln from base directory.");
        return Path.Combine(dir, "GameData");
    }
}