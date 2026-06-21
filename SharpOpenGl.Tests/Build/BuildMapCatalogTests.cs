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
    public void Build_map_json_loads_four_categories()
    {
        var config = LoadBuildMapConfig();
        Assert.Equal(4, config.Categories.Count);
        Assert.Contains(config.Categories, c => c.DisplayName == "Defense");
        Assert.Contains(config.Categories, c => c.DisplayName == "Economy");
        Assert.Contains(config.Categories, c => c.DisplayName == "Production");
        Assert.Contains(config.Categories, c => c.DisplayName == "Support");
    }

    [Fact]
    public void Shipyard_requires_command_center_prerequisite()
    {
        var config = LoadBuildMapConfig();
        var production = config.Categories.First(c => c.Id == "production");
        var shipyard = production.Buildings.First(b => b.Id == "shipyard_small");
        Assert.Contains("command_center", shipyard.Prerequisites);
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
        { new[] { "command_center" }, "shipyard_small", true },
        { Array.Empty<string>(), "shipyard_small", false },
        { new[] { "command_center", "shipyard_small" }, "repair_bay", true },
        { new[] { "command_center" }, "repair_bay", false },
    };

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
    public void BuildViews_unlocks_shipyard_when_command_center_exists()
    {
        var config = LoadBuildMapConfig();
        var defs = LoadAllBaseDefinitions();
        var catalog = new BuildMapCatalog(config, defs);
        var world = new World();
        var baseEntity = world.CreateEntity();
        world.AddComponent(baseEntity, new BuildingComponent
        {
            BuildingType = "command_center",
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