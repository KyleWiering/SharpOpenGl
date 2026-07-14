using System.Text.Json;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class SkirmishMapLogicTests
{
    [Theory]
    [InlineData("duel_frontier", 2)]
    [InlineData("four_corners", 4)]
    [InlineData("octagon_rim", 8)]
    public void Skirmish_maps_validate_and_match_player_count(string mapId, int expectedPlayers)
    {
        var map = LoadSkirmishMap(mapId);

        Assert.True(map.Skirmish);
        Assert.Equal(expectedPlayers, map.PlayerCount);
        Assert.Equal(expectedPlayers, map.SpawnPoints.Length);
        Assert.Empty(SkirmishMapLogic.Validate(map));
        Assert.True(SkirmishMapLogic.SupportsPlayerCount(map, expectedPlayers));
    }

    [Fact]
    public void Each_spawn_has_baseArea_and_command_center()
    {
        string mapsPath = Path.Combine(GetGameDataPath(), "Maps");
        var catalog = new SkirmishMapCatalog(new AssetManager(GetGameDataPath()));

        foreach (var map in catalog.LoadAll(mapsPath))
        {
            Assert.Empty(SkirmishMapLogic.Validate(map));

            foreach (var spawn in map.SpawnPoints)
            {
                Assert.True(SkirmishMapLogic.TryParseBaseArea(spawn.BaseArea, out _, out _, out _, out _));

                var placements = SkirmishMapLogic.ResolveStarterPlacements(spawn);
                Assert.NotEmpty(placements);
                Assert.Contains(placements, p =>
                    p.BuildingId.Equals("command_center", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void ResolveActiveSpawns_uses_first_n_spawn_points()
    {
        var map = LoadSkirmishMap("octagon_rim");

        var twoPlayerSpawns = SkirmishMapLogic.ResolveActiveSpawns(map, 2);
        Assert.Equal(2, twoPlayerSpawns.Count);
        Assert.Equal(1, twoPlayerSpawns[0].Player);
        Assert.Equal(2, twoPlayerSpawns[1].Player);
    }

    [Fact]
    public void Starter_placements_stay_inside_baseArea()
    {
        var map = LoadSkirmishMap("duel_frontier");
        var spawn = map.SpawnPoints[0];

        Assert.True(SkirmishMapLogic.TryParseBaseArea(
            spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY));

        foreach (var (_, gridX, gridY) in SkirmishMapLogic.ResolveStarterPlacements(spawn))
        {
            Assert.InRange(gridX, minX, maxX);
            Assert.InRange(gridY, minY, maxY);
        }
    }

    [Fact]
    public void Skirmish_human_spawn_includes_support_repair_definition()
    {
        string path = Path.Combine(GetGameDataPath(), "Ships", "support_repair.json");
        Assert.True(File.Exists(path), "support_repair ship definition required for skirmish builder spawn.");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        var def = JsonSerializer.Deserialize<EntityDefinition>(File.ReadAllText(path), options);
        Assert.NotNull(def);
        Assert.Equal("support_repair", def!.Id);

        var builder = def.Components?.StructureBuilder;
        Assert.NotNull(builder);
        Assert.True(builder!.PlacementRange > 0);

        var buildableIds = builder.BuildableIds ?? [];
        Assert.Equal(SkirmishMapLogic.BuilderTier1BuildingIds.Length, buildableIds.Length);
        foreach (string tier1Id in SkirmishMapLogic.BuilderTier1BuildingIds)
            Assert.Contains(tier1Id, buildableIds);
        Assert.DoesNotContain("shipyard_small", buildableIds);
        Assert.DoesNotContain("command_center", buildableIds);

        Assert.Equal(4500f, SkirmishMapLogic.SkirmishStartingEnergy);
        Assert.Equal(5500f, SkirmishMapLogic.SkirmishStartingMinerals);
        Assert.Equal(700f, SkirmishMapLogic.SkirmishStartingData);
        Assert.Equal(55f, SkirmishMapLogic.SkirmishStartingCrew);
    }

    [Fact]
    public void All_skirmish_maps_have_mineable_content()
    {
        string mapsPath = Path.Combine(GetGameDataPath(), "Maps");
        var catalog = new SkirmishMapCatalog(new AssetManager(GetGameDataPath()));

        foreach (var map in catalog.LoadAll(mapsPath))
        {
            Assert.Empty(SkirmishMapLogic.ValidateEconomy(map));

            bool hasHarvestablePlanet = map.MapFeatures.Any(f =>
                f.Kind.Equals("harvestable_planet", StringComparison.OrdinalIgnoreCase));

            Assert.True(
                map.ResourceNodes.Length >= 4 || hasHarvestablePlanet,
                $"Map '{map.Id}' must have >=4 resourceNodes or a harvestable_planet.");
        }
    }

    [Fact]
    public void Sector_alpha_campaign_map_has_mineable_content()
    {
        var assets = new AssetManager(GetGameDataPath());
        var map = assets.Load<MapDefinition>("Maps/sector_alpha");
        Assert.NotNull(map);
        Assert.Empty(SkirmishMapLogic.ValidateEconomy(map!));
    }

    [Fact]
    public void Procedural_map_generator_satisfies_economy_validation_across_seeds()
    {
        var config = new MapGeneratorConfig
        {
            Width = 64,
            Height = 64,
            ResourceNodeCount = 4,
            HarvestablePlanetCount = 1,
            NeutralPlanetCount = 1,
            ScatterSceneryFromTerrain = false,
        };

        for (int seed = 1; seed <= 10; seed++)
        {
            MapDefinition def = new MapGenerator(seed).Generate(config);
            Assert.Empty(SkirmishMapLogic.ValidateEconomy(def));
        }
    }

    [Theory]
    [InlineData("duel_frontier")]
    [InlineData("four_corners")]
    [InlineData("octagon_rim")]
    public void Authored_skirmish_maps_pass_fairness_validation(string mapId)
    {
        var map = LoadSkirmishMap(mapId);
        Assert.Empty(SkirmishMapLogic.ValidateFairness(map));
        Assert.Empty(SkirmishMapLogic.Validate(map));
    }

    [Theory]
    [InlineData("duel_frontier", 37, 37)]
    [InlineData("four_corners", 43, 43)]
    [InlineData("octagon_rim", 33, 33)]
    public void Skirmish_spawns_use_uniform_base_area_dimensions(string mapId, int width, int height)
    {
        var map = LoadSkirmishMap(mapId);

        foreach (var spawn in map.SpawnPoints)
        {
            Assert.True(SkirmishMapLogic.TryParseBaseArea(
                spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY));

            Assert.Equal(width, maxX - minX + 1);
            Assert.Equal(height, maxY - minY + 1);
            Assert.InRange(spawn.Position[0], minX, maxX);
            Assert.InRange(spawn.Position[1], minY, maxY);
        }
    }

    [Theory]
    [InlineData("duel_frontier", 4)]
    [InlineData("four_corners", 4)]
    [InlineData("octagon_rim", 4)]
    public void Each_spawn_has_matching_home_resource_count(string mapId, int expectedHomeNodes)
    {
        var map = LoadSkirmishMap(mapId);

        var counts = map.SpawnPoints
            .Select(sp => SkirmishMapLogic.CountHomeResources(sp, map.ResourceNodes))
            .Distinct()
            .ToArray();

        Assert.Single(counts);
        Assert.Equal(expectedHomeNodes, counts[0]);
    }

    [Fact]
    public void Duel_frontier_flank_terrain_is_horizontally_symmetric()
    {
        var map = LoadSkirmishMap("duel_frontier");
        Assert.Empty(SkirmishMapLogic.ValidateFairness(map));
        Assert.True(SkirmishMapLogic.HasUniformStarterOffsets(map));
    }

    [Fact]
    public void SkirmishMapCatalog_resolve_entries_includes_authored_maps()
    {
        var entries = SkirmishMapCatalog.ResolveEntries(GetGameDataPath());

        Assert.Contains(entries, e => e.Id == "duel_frontier" && e.PlayerCount == 2);
        Assert.Contains(entries, e => e.Id == "four_corners" && e.PlayerCount == 4);
        Assert.Contains(entries, e => e.Id == "octagon_rim" && e.PlayerCount == 8);
    }

    private static MapDefinition LoadSkirmishMap(string mapId)
    {
        var catalog = new SkirmishMapCatalog(new AssetManager(GetGameDataPath()));
        var map = catalog.Load(mapId);
        Assert.NotNull(map);
        return map!;
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