using SharpOpenGl.Engine.Assets;
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
    public void Each_spawn_has_baseArea_and_command_center_plus_shipyard()
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
                Assert.Contains(placements, p =>
                    p.BuildingId.Contains("shipyard", StringComparison.OrdinalIgnoreCase));
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