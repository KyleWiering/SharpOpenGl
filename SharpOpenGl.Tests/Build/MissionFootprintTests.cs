using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Build;

/// <summary>
/// Regression tests for campaign mission and skirmish map building footprints
/// after multi-cell occupancy enforcement.
/// </summary>
public class MissionFootprintTests
{
    private const int GridExtent = MapCoordinates.DefaultGridExtent;
    private const float CellSize = MapCoordinates.DefaultCellSize;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Hardcoded mission-world bases from <c>SpawnPlayerBase</c>.</summary>
    private static readonly (string BuildingId, Vector3 WorldPos)[] MissionDefaultBases =
    [
        ("command_center", new Vector3(-30f, 0f, -30f)),
        ("shipyard_medium", new Vector3(50f, 0f, -50f)),
    ];

    [Fact]
    public void Example_scenario_place_building_validates_for_shipyard_small_on_mission_grid()
    {
        var grid = CreateMissionGrid();
        var world = new World();
        var defs = LoadAllBaseDefinitions();
        var catalog = CreateCatalog(defs);
        var resources = CreateFundedResources();

        OccupyMissionDefaultBases(grid, world, defs);

        var step = LoadMission("example_scenario")
            .DemoScript
            .First(s => s.Type == "place_building");

        Assert.Equal("shipyard_small", step.BuildingId);
        Assert.Equal(99f, step.Position![0]);
        Assert.Equal(95f, step.Position[1]);

        Vector3 worldPos = MapCoordinates.GridToWorld(step.Position![0], step.Position[1]);
        var def = defs["shipyard_small"];
        EnsurePrerequisitesBuilt(world, catalog, step.BuildingId!, playerId: 1);

        var result = BuildingPlacementValidator.Validate(
            grid, world, playerId: 1, def, worldPos, catalog, resources, supply: null);

        Assert.True(result.IsValid, $"Expected valid placement, got {result.Reason}");
    }

    [Fact]
    public void Placing_building_B_after_A_fails_when_multi_cell_footprints_overlap()
    {
        var grid = CreateMissionGrid();
        var world = new World();
        var defs = LoadAllBaseDefinitions();
        var catalog = CreateCatalog(defs);
        var resources = CreateFundedResources();

        var commandCenterDef = defs["command_center"];
        var shipyardDef = defs["shipyard_small"];

        Entity commandCenter = world.CreateEntity();
        world.AddComponent(commandCenter, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
            Footprint = commandCenterDef.Components!.Building!.Footprint,
        });

        Vector3 ccPos = MapCoordinates.GridToWorld(20, 20);
        BuildingFootprint.Occupy(grid, commandCenter, ccPos,
            commandCenterDef.Components!.Building!.Footprint!);

        Entity powerReactor = world.CreateEntity();
        world.AddComponent(powerReactor, new BuildingComponent
        {
            BuildingType = "power_reactor",
            PlayerId = 1,
        });

        Vector3 overlappingPos = MapCoordinates.GridToWorld(21, 20);
        var result = BuildingPlacementValidator.Validate(
            grid, world, 1, shipyardDef, overlappingPos, catalog, resources, supply: null);

        Assert.False(result.IsValid);
        Assert.Equal(PlacementFailureReason.CellOccupied, result.Reason);
    }

    /// <summary>15 place_building steps from mission_build_tree demoScript (prerequisite-safe order).</summary>
    private static readonly string[] FullBuildTreePlacementOrder =
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

    /// <summary>
    /// Reference greedy layout for four_corners player-1 (center-anchored grid positions
    /// per <see cref="BuildingFootprint"/> / <see cref="BuildingPlacementValidator"/>).
    /// </summary>
    private static readonly (string BuildingId, int GridX, int GridY)[] FourCornersPlayer1ReferenceLayout =
    [
        ("command_center", 18, 22),
        ("power_reactor", 15, 15),
        ("resource_refinery", 17, 15),
        ("supply_depot", 20, 15),
        ("defense_turret", 21, 14),
        ("sensor_array", 23, 15),
        ("fabrication_hub", 25, 15),
        ("shipyard_small", 28, 15),
        ("shield_emitter", 30, 15),
        ("missile_battery", 32, 15),
        ("shipyard_medium", 34, 15),
        ("repair_bay", 37, 15),
        ("comms_relay", 40, 15),
        ("shipyard_large", 43, 16),
        ("orbital_uplink", 46, 15),
        ("fortress_core", 50, 16),
    ];

    [Theory]
    [InlineData("duel_frontier")]
    [InlineData("four_corners")]
    [InlineData("octagon_rim")]
    public void Skirmish_baseArea_fits_full_build_tree_placement(string mapId)
    {
        var map = LoadSkirmishMap(mapId);
        var defs = LoadAllBaseDefinitions();
        var catalog = CreateCatalog(defs);
        var resources = CreateFundedResources();

        var spawn = map.SpawnPoints.First(sp => sp.Player == 1);
        var placements = SimulateGreedyFullTreePlacement(
            mapId, spawn, defs, catalog, resources, out string? failureReason);

        Assert.Null(failureReason);
        Assert.Equal(16, placements.Count);
    }

    [Fact]
    public void Four_corners_player1_baseArea_reference_layout_matches_greedy_simulation()
    {
        var map = LoadSkirmishMap("four_corners");
        var defs = LoadAllBaseDefinitions();
        var catalog = CreateCatalog(defs);
        var resources = CreateFundedResources();

        var spawn = map.SpawnPoints.First(sp => sp.Player == 1);
        var placements = SimulateGreedyFullTreePlacement(
            "four_corners", spawn, defs, catalog, resources, out string? failureReason);

        Assert.Null(failureReason);
        Assert.Equal(FourCornersPlayer1ReferenceLayout.Length, placements.Count);

        for (int i = 0; i < FourCornersPlayer1ReferenceLayout.Length; i++)
        {
            var expected = FourCornersPlayer1ReferenceLayout[i];
            var actual = placements[i];
            Assert.Equal(expected.BuildingId, actual.BuildingId);
            Assert.Equal(expected.GridX, actual.GridX);
            Assert.Equal(expected.GridY, actual.GridY);
        }
    }

    [Theory]
    [InlineData("duel_frontier")]
    [InlineData("four_corners")]
    [InlineData("octagon_rim")]
    public void Skirmish_starter_buildings_do_not_overlap_footprints(string mapId)
    {
        var map = LoadSkirmishMap(mapId);
        var defs = LoadAllBaseDefinitions();
        var grid = CreateMissionGrid();

        foreach (var spawn in map.SpawnPoints)
        {
            var occupied = new HashSet<(int X, int Y)>();
            foreach (var (buildingId, gridX, gridY) in SkirmishMapLogic.ResolveStarterPlacements(spawn))
            {
                Assert.True(defs.ContainsKey(buildingId), $"Unknown starter building '{buildingId}' on {mapId}");

                int[] footprint = defs[buildingId].Components?.Building?.Footprint ?? [1, 1];
                var (cols, rows) = BuildingFootprint.GetSize(footprint);
                Vector3 worldPos = MapCoordinates.GridToWorld(gridX, gridY);

                foreach (var cell in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
                {
                    Assert.True(grid.InBounds(cell.X, cell.Y),
                        $"{mapId} player {spawn.Player}: {buildingId} at [{gridX},{gridY}] out of bounds ({cell.X},{cell.Y})");
                    Assert.True(occupied.Add(cell),
                        $"{mapId} player {spawn.Player}: {buildingId} at [{gridX},{gridY}] overlaps another starter building at ({cell.X},{cell.Y})");
                }
            }
        }
    }

    [Fact]
    public void All_campaign_missions_have_non_overlapping_demo_place_building_steps()
    {
        var defs = LoadAllBaseDefinitions();
        var catalog = CreateCatalog(defs);
        var resources = CreateFundedResources();

        string missionsPath = Path.Combine(GetGameDataPath(), "Missions");
        var loader = new MissionLoader(new AssetManager(GetGameDataPath()));

        foreach (var mission in loader.LoadAll(missionsPath))
        {
            if (mission.Id.StartsWith("_", StringComparison.Ordinal)) continue;

            var grid = CreateMissionGrid();
            var world = new World();
            if (mission.Id.Equals("mission_build_tree", StringComparison.OrdinalIgnoreCase))
                OccupyCommandCenterOnly(grid, world, defs);
            else
                OccupyMissionDefaultBases(grid, world, defs);

            var occupied = CollectOccupiedCells(grid);
            var placeSteps = mission.DemoScript
                .Where(s => s.Type.Equals("place_building", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var step in placeSteps)
            {
                Assert.False(string.IsNullOrWhiteSpace(step.BuildingId));
                Assert.NotNull(step.Position);
                Assert.True(step.Position.Length >= 2);
                Assert.True(defs.ContainsKey(step.BuildingId!),
                    $"Mission {mission.Id}: unknown building '{step.BuildingId}'");

                int[] footprint = defs[step.BuildingId!].Components?.Building?.Footprint ?? [1, 1];
                var (cols, rows) = BuildingFootprint.GetSize(footprint);
                Vector3 worldPos = MapCoordinates.GridToWorld(step.Position[0], step.Position[1]);

                foreach (var cell in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
                {
                    Assert.False(occupied.Contains(cell),
                        $"Mission {mission.Id}: {step.BuildingId} at [{step.Position[0]},{step.Position[1]}] overlaps existing footprint at ({cell.X},{cell.Y})");
                }

                if (placeSteps.Length == 1)
                    EnsurePrerequisitesBuilt(world, catalog, step.BuildingId!, playerId: 1);

                var result = BuildingPlacementValidator.Validate(
                    grid, world, 1, defs[step.BuildingId!], worldPos, catalog, resources, supply: null);

                Assert.True(result.IsValid,
                    $"Mission {mission.Id}: {step.BuildingId} at [{step.Position[0]},{step.Position[1]}] failed validation: {result.Reason}");

                Entity placed = world.CreateEntity();
                world.AddComponent(placed, new BuildingComponent
                {
                    BuildingType = step.BuildingId!,
                    PlayerId = 1,
                    Footprint = footprint,
                });
                BuildingFootprint.Occupy(grid, placed, worldPos, footprint);
                foreach (var cell in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
                    occupied.Add(cell);
            }
        }
    }

    [Fact]
    public void Mission_build_tree_place_building_steps_validate_on_mission_grid()
    {
        // mission_build_tree spawns command center only (no default shipyard_medium).
        var grid = CreateMissionGrid();
        var world = new World();
        var defs = LoadAllBaseDefinitions();
        var catalog = CreateCatalog(defs);
        var resources = CreateFundedResources();

        OccupyCommandCenterOnly(grid, world, defs);

        var mission = LoadMission("mission_build_tree");
        var occupied = CollectOccupiedCells(grid);

        foreach (var step in mission.DemoScript.Where(s => s.Type == "place_building"))
        {
            Assert.False(string.IsNullOrWhiteSpace(step.BuildingId));
            Assert.NotNull(step.Position);
            Assert.True(step.Position.Length >= 2);
            Assert.True(defs.ContainsKey(step.BuildingId!),
                $"Unknown building '{step.BuildingId}'");

            int[] footprint = defs[step.BuildingId!].Components?.Building?.Footprint ?? [1, 1];
            var (cols, rows) = BuildingFootprint.GetSize(footprint);
            Vector3 worldPos = MapCoordinates.GridToWorld(step.Position[0], step.Position[1]);

            foreach (var cell in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
            {
                Assert.False(occupied.Contains(cell),
                    $"{step.BuildingId} at [{step.Position[0]},{step.Position[1]}] overlaps existing footprint at ({cell.X},{cell.Y})");
            }

            var result = BuildingPlacementValidator.Validate(
                grid, world, 1, defs[step.BuildingId!], worldPos, catalog, resources, supply: null);

            Assert.True(result.IsValid,
                $"{step.BuildingId} at [{step.Position[0]},{step.Position[1]}] failed validation: {result.Reason}");

            Entity placed = world.CreateEntity();
            world.AddComponent(placed, new BuildingComponent
            {
                BuildingType = step.BuildingId!,
                PlayerId = 1,
                Footprint = footprint,
            });
            BuildingFootprint.Occupy(grid, placed, worldPos, footprint);
            foreach (var cell in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
                occupied.Add(cell);
        }
    }

    [Fact]
    public void Mission_default_bases_do_not_overlap_each_other()
    {
        var grid = CreateMissionGrid();
        var defs = LoadAllBaseDefinitions();
        var occupied = new HashSet<(int X, int Y)>();

        foreach (var (buildingId, worldPos) in MissionDefaultBases)
        {
            int[] footprint = defs[buildingId].Components?.Building?.Footprint ?? [1, 1];
            var (cols, rows) = BuildingFootprint.GetSize(footprint);

            foreach (var cell in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
            {
                Assert.True(occupied.Add(cell),
                    $"Mission default base overlap: {buildingId} occupies ({cell.X},{cell.Y})");
            }
        }
    }

    private static GridSystem CreateMissionGrid()
    {
        float half = GridExtent * CellSize * 0.5f;
        return new GridSystem(GridExtent, GridExtent, CellSize, new Vector2(-half, -half));
    }

    private static void OccupyMissionDefaultBases(
        GridSystem grid, World world, Dictionary<string, EntityDefinition> defs)
    {
        foreach (var (buildingId, worldPos) in MissionDefaultBases)
        {
            int[] footprint = defs[buildingId].Components?.Building?.Footprint ?? [2, 2];
            Entity entity = world.CreateEntity();
            world.AddComponent(entity, new BuildingComponent
            {
                BuildingType = buildingId,
                PlayerId = 1,
                Footprint = footprint,
            });
            BuildingFootprint.Occupy(grid, entity, worldPos, footprint);
        }
    }

    /// <summary>
    /// Occupies only the pre-spawned command center for missions that skip default shipyard spawn.
    /// </summary>
    private static void OccupyCommandCenterOnly(
        GridSystem grid, World world, Dictionary<string, EntityDefinition> defs)
    {
        var (buildingId, worldPos) = MissionDefaultBases[0];
        int[] footprint = defs[buildingId].Components?.Building?.Footprint ?? [2, 2];
        Entity entity = world.CreateEntity();
        world.AddComponent(entity, new BuildingComponent
        {
            BuildingType = buildingId,
            PlayerId = 1,
            Footprint = footprint,
        });
        BuildingFootprint.Occupy(grid, entity, worldPos, footprint);
    }

    /// <summary>
    /// Adds prerequisite building components not represented in a mission's demo script
    /// (e.g. example_scenario places shipyard_small without an explicit power_reactor step).
    /// </summary>
    private static void EnsurePrerequisitesBuilt(
        World world, BuildMapCatalog catalog, string buildingId, int playerId)
    {
        var built = BuildingFootprint.GetBuiltTypes(world, playerId);
        foreach (string prereq in catalog.GetPrerequisites(buildingId))
        {
            if (built.Contains(prereq))
                continue;

            EnsurePrerequisitesBuilt(world, catalog, prereq, playerId);
            built = BuildingFootprint.GetBuiltTypes(world, playerId);
            if (built.Contains(prereq))
                continue;

            Entity entity = world.CreateEntity();
            world.AddComponent(entity, new BuildingComponent
            {
                BuildingType = prereq,
                PlayerId = playerId,
            });
        }
    }

    /// <summary>
    /// Greedy full-tree placement: CC at starter offset, then 15 mission_build_tree structures
    /// scanned row-major inside baseArea via <see cref="BuildingPlacementValidator"/>.
    /// </summary>
    private static List<(string BuildingId, int GridX, int GridY)> SimulateGreedyFullTreePlacement(
        string mapId,
        MapSpawnPoint spawn,
        Dictionary<string, EntityDefinition> defs,
        BuildMapCatalog catalog,
        ResourceManager resources,
        out string? failureReason)
    {
        failureReason = null;
        var placements = new List<(string BuildingId, int GridX, int GridY)>();

        if (!SkirmishMapLogic.TryParseBaseArea(spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY))
        {
            failureReason = $"{mapId}: spawn player {spawn.Player} has invalid baseArea.";
            return placements;
        }

        var grid = CreateMissionGrid();
        var world = new World();

        foreach (var (buildingId, gridX, gridY) in SkirmishMapLogic.ResolveStarterPlacements(spawn))
        {
            if (!defs.TryGetValue(buildingId, out var starterDef))
            {
                failureReason = $"{mapId}: unknown starter building '{buildingId}'.";
                return placements;
            }

            int[] footprint = starterDef.Components?.Building?.Footprint ?? [2, 2];
            Vector3 worldPos = MapCoordinates.GridToWorld(gridX, gridY);
            Entity entity = world.CreateEntity();
            world.AddComponent(entity, new BuildingComponent
            {
                BuildingType = buildingId,
                PlayerId = 1,
                Footprint = footprint,
            });
            BuildingFootprint.Occupy(grid, entity, worldPos, footprint);
            placements.Add((buildingId, gridX, gridY));
        }

        foreach (string buildingId in FullBuildTreePlacementOrder)
        {
            if (!defs.TryGetValue(buildingId, out var def))
            {
                failureReason = $"{mapId}: unknown building '{buildingId}'.";
                return placements;
            }

            int[] footprint = def.Components?.Building?.Footprint ?? [1, 1];
            var (cols, rows) = BuildingFootprint.GetSize(footprint);
            bool placed = false;

            for (int gridY = minY; gridY <= maxY && !placed; gridY++)
            for (int gridX = minX; gridX <= maxX && !placed; gridX++)
            {
                Vector3 worldPos = MapCoordinates.GridToWorld(gridX, gridY);
                if (!FootprintFitsInBaseArea(grid, worldPos, cols, rows, minX, minY, maxX, maxY))
                    continue;

                var result = BuildingPlacementValidator.Validate(
                    grid, world, playerId: 1, def, worldPos, catalog, resources, supply: null);

                if (!result.IsValid)
                    continue;

                Entity entity = world.CreateEntity();
                world.AddComponent(entity, new BuildingComponent
                {
                    BuildingType = buildingId,
                    PlayerId = 1,
                    Footprint = footprint,
                });
                BuildingFootprint.Occupy(grid, entity, worldPos, footprint);
                placements.Add((buildingId, gridX, gridY));
                placed = true;
            }

            if (!placed)
            {
                failureReason = $"{mapId}: could not place '{buildingId}' inside baseArea [{minX},{minY},{maxX},{maxY}].";
                return placements;
            }
        }

        return placements;
    }

    private static bool FootprintFitsInBaseArea(
        GridSystem grid,
        Vector3 worldPos,
        int cols,
        int rows,
        int minX,
        int minY,
        int maxX,
        int maxY)
    {
        foreach (var (x, y) in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
        {
            if (x < minX || x > maxX || y < minY || y > maxY)
                return false;
        }

        return true;
    }

    private static HashSet<(int X, int Y)> CollectOccupiedCells(GridSystem grid)
    {
        var occupied = new HashSet<(int X, int Y)>();
        foreach (GridCell cell in grid.AllCells())
        {
            if (cell.Occupant != Entity.Null)
                occupied.Add((cell.X, cell.Y));
        }

        return occupied;
    }

    private static MissionDefinition LoadMission(string missionId)
    {
        var loader = new MissionLoader(new AssetManager(GetGameDataPath()));
        var mission = loader.Load(missionId);
        Assert.NotNull(mission);
        return mission!;
    }

    private static MapDefinition LoadSkirmishMap(string mapId)
    {
        var catalog = new SkirmishMapCatalog(new AssetManager(GetGameDataPath()));
        var map = catalog.Load(mapId);
        Assert.NotNull(map);
        return map!;
    }

    private static BuildMapCatalog CreateCatalog(Dictionary<string, EntityDefinition> defs)
    {
        string path = Path.Combine(GetGameDataPath(), "Config", "build_map.json");
        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BuildMapConfig>(json, JsonOptions);
        Assert.NotNull(config);
        return new BuildMapCatalog(config!, defs);
    }

    private static Dictionary<string, EntityDefinition> LoadAllBaseDefinitions()
    {
        var defs = new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);
        string basesPath = Path.Combine(GetGameDataPath(), "Bases");

        foreach (string file in Directory.GetFiles(basesPath, "*.json"))
        {
            if (Path.GetFileName(file).StartsWith("_", StringComparison.Ordinal)) continue;
            string json = File.ReadAllText(file);
            var def = JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions);
            if (def?.Id != null)
                defs[def.Id] = def;
        }

        return defs;
    }

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