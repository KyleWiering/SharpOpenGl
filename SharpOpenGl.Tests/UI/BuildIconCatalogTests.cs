using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class BuildIconCatalogTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly string[] TieredStructureIds =
    [
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
    ];

    [Fact]
    public void All_build_map_json_ids_resolve_via_TryGetIcon()
    {
        var config = LoadBuildMapConfig();
        var ids = config.Categories
            .SelectMany(category => category.Buildings)
            .Select(building => building.Id)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(TieredStructureIds.OrderBy(id => id).ToArray(), ids);

        foreach (string id in ids)
            Assert.True(BuildIconCatalog.TryGetIcon(id, out _), $"Missing icon for '{id}'.");
    }

    [Fact]
    public void All_tiered_structure_ids_resolve_with_expected_shapes()
    {
        var expectedShapes = new Dictionary<string, BuildIconShape>(StringComparer.OrdinalIgnoreCase)
        {
            ["command_center"] = BuildIconShape.CommandCenter,
            ["shipyard_small"] = BuildIconShape.Shipyard,
            ["shipyard_medium"] = BuildIconShape.Shipyard,
            ["shipyard_large"] = BuildIconShape.Shipyard,
            ["power_reactor"] = BuildIconShape.Reactor,
            ["resource_refinery"] = BuildIconShape.Depot,
            ["supply_depot"] = BuildIconShape.Depot,
            ["fabrication_hub"] = BuildIconShape.Depot,
            ["sensor_array"] = BuildIconShape.Sensor,
            ["defense_turret"] = BuildIconShape.Turret,
            ["shield_emitter"] = BuildIconShape.Reactor,
            ["missile_battery"] = BuildIconShape.Turret,
            ["repair_bay"] = BuildIconShape.Shipyard,
            ["comms_relay"] = BuildIconShape.Sensor,
            ["orbital_uplink"] = BuildIconShape.Capstone,
            ["fortress_core"] = BuildIconShape.Capstone,
        };

        foreach (string id in TieredStructureIds)
        {
            Assert.True(BuildIconCatalog.TryGetIcon(id, out BuildIconDescriptor icon), $"Missing icon for '{id}'.");
            Assert.Equal(id, icon.BuildingType);
            Assert.Equal(expectedShapes[id], icon.Shape);
            Assert.True(icon.PrimaryTint.W > 0f);
            Assert.True(icon.AccentTint.W > 0f);
        }
    }

    [Fact]
    public void Unknown_id_returns_command_center_fallback_without_throw()
    {
        BuildIconDescriptor icon = default;
        var ex = Record.Exception(() => BuildIconCatalog.TryGetIcon("unknown_structure_xyz", out icon));

        Assert.Null(ex);
        Assert.Equal(BuildIconShape.CommandCenter, icon.Shape);
        Assert.Equal("unknown_structure_xyz", icon.BuildingType);
    }

    [Theory]
    [InlineData(BuildIconShape.CommandCenter)]
    [InlineData(BuildIconShape.Shipyard)]
    [InlineData(BuildIconShape.Reactor)]
    [InlineData(BuildIconShape.Turret)]
    [InlineData(BuildIconShape.Sensor)]
    [InlineData(BuildIconShape.Depot)]
    [InlineData(BuildIconShape.Capstone)]
    public void Draw_emits_rects_for_each_shape(BuildIconShape shape)
    {
        string buildingId = shape switch
        {
            BuildIconShape.CommandCenter => "command_center",
            BuildIconShape.Shipyard => "shipyard_medium",
            BuildIconShape.Reactor => "power_reactor",
            BuildIconShape.Turret => "defense_turret",
            BuildIconShape.Sensor => "sensor_array",
            BuildIconShape.Depot => "supply_depot",
            BuildIconShape.Capstone => "orbital_uplink",
            _ => "command_center",
        };

        BuildIconDescriptor icon = BuildIconCatalog.Get(buildingId);
        var renderer = new RecordingRenderer();

        BuildIconDrawing.Draw(renderer, icon, Vector2.Zero, BuildIconDrawing.MinimumSize);

        Assert.True(renderer.RectDrawCount >= 1, $"Shape '{shape}' produced no rect draws.");
        Assert.True(renderer.MaxRectSize.X >= BuildIconDrawing.MinimumSize);
        Assert.True(renderer.MaxRectSize.Y >= BuildIconDrawing.MinimumSize);
    }

    [Fact]
    public void BuildViews_populates_icon_on_each_entry()
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
        var entries = views.SelectMany(c => c.Buildings).ToList();

        Assert.Equal(TieredStructureIds.Length, entries.Count);
        foreach (var entry in entries)
        {
            Assert.Equal(entry.Id, entry.Icon.BuildingType);
            Assert.NotEqual(default(BuildIconDescriptor), entry.Icon);
        }
    }

    private static BuildMapConfig LoadBuildMapConfig()
    {
        string path = Path.Combine(GetGameDataPath(), "Config", "build_map.json");
        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BuildMapConfig>(json, JsonOptions);
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
            var def = JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions);
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

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public int RectDrawCount { get; private set; }
        public Vector2 MaxRectSize { get; private set; }

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
        {
            RectDrawCount++;
            MaxRectSize = new Vector2(
                MathF.Max(MaxRectSize.X, size.X),
                MathF.Max(MaxRectSize.Y, size.Y));
        }

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}