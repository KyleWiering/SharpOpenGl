using System.Text.Json;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Entities;

public class EntityDefinitionRoleTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [Theory]
    [InlineData("fighter_basic", ShipRole.Military)]
    [InlineData("miner_basic", ShipRole.Engineering)]
    [InlineData("hero_default", ShipRole.Political)]
    [InlineData("carrier_command", ShipRole.Political)]
    public void Representative_ships_deserialize_explicit_shipRole(string id, ShipRole expected)
    {
        var def = LoadShip(id);
        Assert.Equal(expected, def.ShipRole);
        Assert.Equal(expected, ShipRoleResolver.Resolve(def));
    }

    [Theory]
    [InlineData("""{"id":"test_miner","category":"miner"}""", ShipRole.Engineering)]
    [InlineData("""{"id":"test_freighter","category":"freighter"}""", ShipRole.Engineering)]
    [InlineData("""{"id":"test_transport","category":"transport"}""", ShipRole.Engineering)]
    [InlineData("""{"id":"test_support","category":"support"}""", ShipRole.Engineering)]
    [InlineData("""{"id":"test_hero","category":"hero"}""", ShipRole.Political)]
    [InlineData("""{"id":"test_fighter","category":"fighter"}""", ShipRole.Military)]
    public void Missing_shipRole_infers_from_category(string json, ShipRole expected)
    {
        var def = JsonSerializer.Deserialize<EntityDefinition>(json, Options);
        Assert.NotNull(def);
        Assert.Null(def!.ShipRole);
        Assert.Equal(expected, ShipRoleResolver.Resolve(def));
    }

    [Fact]
    public void ShipRole_deserializes_case_insensitively()
    {
        const string json = """{"id":"test","category":"fighter","shipRole":"MILITARY"}""";
        var def = JsonSerializer.Deserialize<EntityDefinition>(json, Options);
        Assert.NotNull(def);
        Assert.Equal(ShipRole.Military, def!.ShipRole);
    }

    [Fact]
    public void All_ship_catalog_files_define_shipRole_except_template()
    {
        string shipsDir = Path.Combine(GetGameDataPath(), "Ships");
        var missing = new List<string>();

        foreach (string file in Directory.GetFiles(shipsDir, "*.json"))
        {
            string name = Path.GetFileName(file);
            if (name.StartsWith('_'))
                continue;

            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (!doc.RootElement.TryGetProperty("shipRole", out var roleProp) ||
                roleProp.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(roleProp.GetString()))
            {
                missing.Add(name);
            }
        }

        Assert.Empty(missing);
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
        return def!;
    }
}