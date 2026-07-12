using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class RaceShipMeshesTests
{
    public RaceShipMeshesTests()
    {
        RaceVisualSchema.ResetForTests();
    }

    [Theory]
    [InlineData("terran")]
    [InlineData("vesper")]
    [InlineData("korath")]
    [InlineData("aetherian")]
    [InlineData("nexar")]
    [InlineData("solari")]
    [InlineData("voidborn")]
    [InlineData("cryo")]
    public void Build_produces_valid_vertex_data_for_each_race(string raceId)
    {
        float[] mesh = RaceShipMeshes.Build(raceId, "fighter_basic");
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
        Assert.True(ProceduralMeshes.VertexCount(mesh) >= 15);
    }

    [Theory]
    [InlineData("scout_light")]
    [InlineData("fighter_basic")]
    [InlineData("interceptor_mk2")]
    [InlineData("drone_swarm")]
    [InlineData("corvette_fast")]
    [InlineData("frigate_strike")]
    [InlineData("gunship_heavy")]
    [InlineData("bomber_heavy")]
    [InlineData("destroyer_assault")]
    [InlineData("cruiser_heavy")]
    [InlineData("carrier_command")]
    [InlineData("dreadnought")]
    [InlineData("miner_basic")]
    [InlineData("transport_cargo")]
    [InlineData("freighter_bulk")]
    [InlineData("support_repair")]
    [InlineData("hero_default")]
    public void Build_terran_variant_for_every_ship_definition(string definitionId)
    {
        float[] mesh = RaceShipMeshes.Build(RaceShipMeshes.DefaultRace, definitionId);
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
    }

    [Fact]
    public void RaceFromSeed_skips_terran_for_enemy_variety()
    {
        string race = RaceVisualSchema.RaceFromSeed("scout_light");
        Assert.NotEqual(RaceShipMeshes.DefaultRace, race);
    }

    [Fact]
    public void Schema_exposes_eight_races()
    {
        Assert.Equal(8, RaceVisualSchema.AllRaces.Count);
        Assert.Contains(RaceVisualSchema.AllRaces, r => r.Id == "voidborn");
        Assert.Contains(RaceVisualSchema.AllRaces, r => r.Id == "cryo");
    }

    [Fact]
    public void Build_applies_custom_tint()
    {
        var tint = new Vector3(1f, 0.2f, 0.1f);
        float[] mesh = RaceShipMeshes.Build("terran", "gunship_heavy", tint);
        Assert.InRange(mesh[3], 0.4f, 1f);
        Assert.InRange(mesh[4], 0f, 0.5f);
    }

    [Fact]
    public void BuildDesign_special_archetypes_produce_valid_meshes()
    {
        var standard = ShipDesignCatalog.All.First(d => !d.IsSpecial && d.RaceId == "terran");
        var special = ShipDesignCatalog.All.First(d => d.IsSpecial && d.RaceId == "terran");
        int standardVerts = ProceduralMeshes.VertexCount(RaceShipMeshes.BuildDesign(standard));
        int specialVerts = ProceduralMeshes.VertexCount(RaceShipMeshes.BuildDesign(special));
        Assert.True(standardVerts >= 3);
        Assert.True(specialVerts >= 3);
        Assert.True(special.IsSpecial);
        Assert.NotEqual(standard.DesignId, special.DesignId);
    }
}