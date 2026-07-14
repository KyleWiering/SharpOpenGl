using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Build;

public class BuilderShipPlacementTests
{
    /// <summary>Full 16-structure tech tree from <c>GameData/Config/build_map.json</c>.</summary>
    private static readonly string[] FullBuildTreeIds =
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [Fact]
    public void StructureBuilderComponent_loads_from_support_repair_definition()
    {
        var def = LoadShipDefinition("support_repair");
        var world = new World();
        var factory = new ShipFactory(assets: null);
        Entity builder = factory.Create(world, def);

        var builderComp = world.GetComponent<StructureBuilderComponent>(builder);
        Assert.NotNull(builderComp);
        Assert.Equal(80f, builderComp!.PlacementRange);
        Assert.Equal(SkirmishMapLogic.BuilderTier1BuildingIds.Length, builderComp.BuildableIds.Count);
        Assert.Equal(
            SkirmishMapLogic.BuilderTier1BuildingIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(),
            builderComp.BuildableIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray());
        Assert.DoesNotContain("command_center", builderComp.BuildableIds, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("shipyard_small", builderComp.BuildableIds, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Support_repair_whitelist_is_tier1_builder_only()
    {
        var def = LoadShipDefinition("support_repair");
        var builder = def.Components!.StructureBuilder!;
        Assert.NotNull(builder.BuildableIds);
        Assert.Equal(SkirmishMapLogic.BuilderTier1BuildingIds.Length, builder.BuildableIds!.Length);
        foreach (string tier1Id in SkirmishMapLogic.BuilderTier1BuildingIds)
            Assert.Contains(tier1Id, builder.BuildableIds!, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("shipyard_small", builder.BuildableIds!, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Placement_rejected_when_builder_out_of_range()
    {
        var world = new World();
        var builder = CreateBuilder(world, new Vector3(0f, 0f, 0f), placementRange: 80f);
        var targetPos = new Vector3(200f, 0f, 0f);

        bool inRange = IsBuilderInPlacementRange(world, builder, targetPos, out string reason);

        Assert.False(inRange);
        Assert.Contains("out of range", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Placement_succeeds_when_builder_in_range()
    {
        var grid = new GridSystem(20, 20, 10f, new Vector2(-100f, -100f));
        var world = new World();
        var resources = CreateFundedResources();
        var catalog = CreateCatalog();
        var builder = CreateBuilder(world, new Vector3(0f, 0f, 0f), placementRange: 80f);

        world.AddComponent(world.CreateEntity(), new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        var def = catalogDefinitions()["supply_depot"];
        var targetPos = new Vector3(40f, 0f, 40f);

        Assert.True(IsBuilderInPlacementRange(world, builder, targetPos, out _));

        var validation = BuildingPlacementValidator.Validate(
            grid, world, playerId: 1, def, targetPos, catalog, resources, supply: null);
        Assert.True(validation.IsValid);

        float mineralsBefore = resources.GetPlayer(1)!.GetAmount(ResourceType.Minerals);
        Assert.True(resources.TrySpendCost(1, def.Cost?.Energy ?? 0, def.Cost?.Minerals ?? 0,
            def.Cost?.Data ?? 0, def.Cost?.Crew ?? 0));

        Entity building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = targetPos });
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = def.Components!.Building!.BuildingType,
            PlayerId = 1,
            Footprint = def.Components.Building.Footprint,
        });
        BuildingFootprint.Occupy(grid, building, targetPos, def.Components.Building.Footprint);

        Assert.Equal(mineralsBefore - (def.Cost?.Minerals ?? 0),
            resources.GetPlayer(1)!.GetAmount(ResourceType.Minerals));
        Assert.Equal("supply_depot", world.GetComponent<BuildingComponent>(building)!.BuildingType);
    }

    [Fact]
    public void Buildable_ids_filtered_to_structure_builder_whitelist()
    {
        var world = new World();
        world.AddComponent(world.CreateEntity(), new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        var catalog = CreateCatalog();
        var views = catalog.BuildViews(world, playerId: 1, CreateFundedResources(), supply: null);
        var whitelist = SkirmishMapLogic.BuilderTier1BuildingIds;

        var filtered = FilterBuildViews(views, whitelist);
        var visibleIds = filtered
            .SelectMany(category => category.Buildings)
            .Select(entry => entry.Id)
            .OrderBy(id => id)
            .ToArray();

        // Catalog is a subset of the full tree; every visible entry must be whitelisted.
        Assert.All(visibleIds, id =>
            Assert.Contains(id, whitelist, StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            filtered.SelectMany(category => category.Buildings),
            entry => !whitelist.Contains(entry.Id, StringComparer.OrdinalIgnoreCase));
        Assert.Contains("supply_depot", visibleIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("defense_turret", visibleIds, StringComparer.OrdinalIgnoreCase);
    }

    private static Entity CreateBuilder(World world, Vector3 position, float placementRange)
    {
        Entity builder = world.CreateEntity();
        world.AddComponent(builder, new TransformComponent { Position = position });
        world.AddComponent(builder, new StructureBuilderComponent
        {
            PlacementRange = placementRange,
            BuildableIds = SkirmishMapLogic.BuilderTier1BuildingIds.ToList(),
        });
        return builder;
    }

    private static bool IsBuilderInPlacementRange(
        World world, Entity builderEntity, Vector3 worldPos, out string reason)
    {
        reason = string.Empty;
        var builderComp = world.GetComponent<StructureBuilderComponent>(builderEntity);
        var builderTransform = world.GetComponent<TransformComponent>(builderEntity);
        if (builderComp == null || builderTransform == null)
            return true;

        float range = builderComp.PlacementRange > 0f
            ? builderComp.PlacementRange
            : StructureBuilderComponent.DefaultPlacementRange;
        float distance = Vector3.Distance(builderTransform.Position, worldPos);
        if (distance <= range)
            return true;

        reason = $"Builder out of range ({range:0}m)";
        return false;
    }

    private static List<BuildMapCategoryView> FilterBuildViews(
        List<BuildMapCategoryView> views,
        IReadOnlyList<string> buildableIds)
    {
        var allowed = new HashSet<string>(buildableIds, StringComparer.OrdinalIgnoreCase);
        var filtered = new List<BuildMapCategoryView>();

        foreach (var category in views)
        {
            var buildings = category.Buildings
                .Where(entry => allowed.Contains(entry.Id))
                .ToList();
            if (buildings.Count == 0)
                continue;

            filtered.Add(new BuildMapCategoryView
            {
                Id = category.Id,
                DisplayName = category.DisplayName,
                TierIndex = category.TierIndex,
                UnlockedCount = buildings.Count(static entry => entry.IsUnlocked),
                TotalCount = buildings.Count,
                Buildings = buildings,
            });
        }

        return filtered;
    }

    private static BuildMapCatalog CreateCatalog()
    {
        var config = new BuildMapConfig
        {
            Categories =
            [
                new BuildMapCategoryConfig
                {
                    Id = "defense",
                    DisplayName = "Defense",
                    Buildings =
                    [
                        new BuildMapEntryConfig { Id = "defense_turret", Prerequisites = ["command_center"] },
                        new BuildMapEntryConfig { Id = "sensor_array", Prerequisites = ["command_center"] },
                    ],
                },
                new BuildMapCategoryConfig
                {
                    Id = "economy",
                    DisplayName = "Economy",
                    Buildings =
                    [
                        new BuildMapEntryConfig { Id = "power_reactor", Prerequisites = ["command_center"] },
                        new BuildMapEntryConfig { Id = "resource_refinery", Prerequisites = ["command_center"] },
                    ],
                },
                new BuildMapCategoryConfig
                {
                    Id = "support",
                    DisplayName = "Support",
                    Buildings =
                    [
                        new BuildMapEntryConfig { Id = "supply_depot", Prerequisites = ["command_center"] },
                    ],
                },
            ],
        };

        return new BuildMapCatalog(config, catalogDefinitions());
    }

    private static Dictionary<string, EntityDefinition> catalogDefinitions() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["supply_depot"] = new()
            {
                Id = "supply_depot",
                DisplayName = "Supply Depot",
                Cost = new CostDefinition { Energy = 90, Minerals = 120, Crew = 1 },
                Components = new ComponentsDefinition
                {
                    Building = new BuildingDefinition
                    {
                        BuildingType = "supply_depot",
                        Footprint = [2, 2],
                    },
                },
            },
            ["sensor_array"] = new()
            {
                Id = "sensor_array",
                DisplayName = "Sensor Array",
                Cost = new CostDefinition(),
                Components = new ComponentsDefinition
                {
                    Building = new BuildingDefinition { BuildingType = "sensor_array", Footprint = [1, 1] },
                },
            },
            ["defense_turret"] = new()
            {
                Id = "defense_turret",
                DisplayName = "Defense Turret",
                Cost = new CostDefinition(),
                Components = new ComponentsDefinition
                {
                    Building = new BuildingDefinition { BuildingType = "defense_turret", Footprint = [1, 1] },
                },
            },
            ["power_reactor"] = new()
            {
                Id = "power_reactor",
                DisplayName = "Power Reactor",
                Cost = new CostDefinition(),
                Components = new ComponentsDefinition
                {
                    Building = new BuildingDefinition { BuildingType = "power_reactor", Footprint = [2, 2] },
                },
            },
            ["resource_refinery"] = new()
            {
                Id = "resource_refinery",
                DisplayName = "Resource Refinery",
                Cost = new CostDefinition(),
                Components = new ComponentsDefinition
                {
                    Building = new BuildingDefinition { BuildingType = "resource_refinery", Footprint = [2, 2] },
                },
            },
        };

    private static ResourceManager CreateFundedResources()
    {
        var resources = new ResourceManager();
        var player = resources.AddPlayer(1);
        player.SetStartingAmount(ResourceType.Energy, 5000);
        player.SetStartingAmount(ResourceType.Minerals, 5000);
        player.SetStartingAmount(ResourceType.Data, 5000);
        player.SetStartingAmount(ResourceType.Crew, 5000);
        return resources;
    }

    private static EntityDefinition LoadShipDefinition(string id)
    {
        string path = Path.Combine(GetGameDataPath(), "Ships", $"{id}.json");
        string json = File.ReadAllText(path);
        var def = JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions);
        Assert.NotNull(def);
        Assert.NotNull(def!.Components?.StructureBuilder);
        return def;
    }

    private static string GetGameDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln.");
        return Path.Combine(dir, "GameData");
    }
}