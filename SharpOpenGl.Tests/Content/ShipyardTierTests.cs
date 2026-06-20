using System.Text.Json;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Content;

public class ShipyardTierTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [Theory]
    [InlineData("shipyard_small", 5)]
    [InlineData("shipyard_medium", 11)]
    [InlineData("shipyard_large", 16)]
    public void Shipyard_producible_count_matches_tier(string id, int expectedCount)
    {
        var def = LoadBase(id);
        Assert.NotNull(def.Producible);
        Assert.Equal(expectedCount, def.Producible!.Count);
    }

    [Fact]
    public void Large_shipyard_includes_dreadnought_and_carrier()
    {
        var def = LoadBase("shipyard_large");
        Assert.Contains("dreadnought", def.Producible!);
        Assert.Contains("carrier_command", def.Producible!);
    }

    [Fact]
    public void Small_shipyard_excludes_capital_ships()
    {
        var def = LoadBase("shipyard_small");
        Assert.DoesNotContain("cruiser_heavy", def.Producible!);
        Assert.DoesNotContain("dreadnought", def.Producible!);
    }

    [Theory]
    [InlineData("interceptor_mk2", "fighter")]
    [InlineData("corvette_fast", "corvette")]
    [InlineData("dreadnought", "dreadnought")]
    [InlineData("drone_swarm", "drone")]
    public void New_ship_definitions_load(string id, string category)
    {
        var def = LoadShip(id);
        Assert.Equal(id, def.Id);
        Assert.Equal(category, def.Category);
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

    private static EntityDefinition LoadBase(string id) =>
        Load(Path.Combine(GetGameDataPath(), "Bases", $"{id}.json"));

    private static EntityDefinition LoadShip(string id) =>
        Load(Path.Combine(GetGameDataPath(), "Ships", $"{id}.json"));

    private static EntityDefinition Load(string path)
    {
        string json = File.ReadAllText(path);
        var def = JsonSerializer.Deserialize<EntityDefinition>(json, Options);
        Assert.NotNull(def);
        return def!;
    }
}