using System.Text.Json;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Economy;

public class ResourceCollectorSchemaTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [Theory]
    [InlineData("miner_basic", "drones", 28f, 15f, 100f)]
    [InlineData("miner_tractor", "tractor_beam", 55f, 12f, 115f)]
    [InlineData("miner_eva", "eva", 35f, 14f, 90f)]
    public void Miner_definitions_define_harvest_mode_and_stats(
        string id, string mode, float range, float rate, float capacity)
    {
        var def = LoadShip(id);
        var rc = def.Components!.ResourceCollector!;
        Assert.Equal(mode, rc.HarvestMode);
        Assert.Equal(range, rc.HarvestRange);
        Assert.Equal(rate, rc.HarvestRate);
        Assert.Equal(capacity, rc.CarryCapacity);
        Assert.True(rc.HarvestRateMultiplier > 0f);
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

    private static EntityDefinition LoadShip(string id)
    {
        string path = Path.Combine(GetGameDataPath(), "Ships", $"{id}.json");
        string json = File.ReadAllText(path);
        var def = JsonSerializer.Deserialize<EntityDefinition>(json, Options);
        Assert.NotNull(def);
        Assert.NotNull(def!.Components?.ResourceCollector);
        return def;
    }
}