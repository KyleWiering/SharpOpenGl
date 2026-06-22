using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class RaceSubstrateCatalogTests
{
    public RaceSubstrateCatalogTests() => RaceVisualSchema.ResetForTests();

    [Fact]
    public void Catalog_enumerates_232_race_model_substrates()
    {
        int count = RaceSubstrateCatalog.AllEntries().Count();
        Assert.Equal(232, count);
        Assert.Equal(232, RaceSubstrateCatalog.TotalEntryCount);
    }

    [Fact]
    public void Every_race_has_explicit_substrate_in_schema()
    {
        foreach (var race in RaceVisualSchema.AllRaces)
        {
            Assert.NotNull(race.Substrate);
            Assert.False(string.IsNullOrWhiteSpace(race.Substrate!.Pattern));
            Assert.True(race.Substrate.UvScale > 0);
            Assert.True(race.Substrate.MicroFrequency > 0);
        }
    }

    [Theory]
    [MemberData(nameof(ShipSubstrateEntries))]
    public void Ship_substrate_entry_builds_valid_mesh(RaceSubstrateCatalog.SubstrateEntry entry)
    {
        float[] mesh = RaceShipMeshes.Build(entry.RaceId, entry.ModelId);
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
        Assert.True(ProceduralMeshes.VertexCount(mesh) >= 12);
    }

    [Theory]
    [MemberData(nameof(StationSubstrateEntries))]
    public void Station_substrate_entry_builds_valid_mesh(RaceSubstrateCatalog.SubstrateEntry entry)
    {
        float[] mesh = RaceStationMeshes.Build(entry.ModelId, entry.RaceId);
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
        Assert.True(ProceduralMeshes.VertexCount(mesh) >= 30);
    }

    [Fact]
    public void Substrate_profiles_differ_across_races()
    {
        var profiles = RaceVisualSchema.AllRaces
            .Select(r => RaceSubstrateProfile.ForRace(r))
            .ToList();

        Assert.Equal(8, profiles.Select(p => p.Pattern).Distinct().Count());
        Assert.Contains(profiles, p => p.Grit >= 0.12f);
        Assert.Contains(profiles, p => p.Grit <= 0.07f);
    }

    [Fact]
    public void Same_hull_differs_by_race_vertex_count_or_color()
    {
        const string hull = "destroyer_assault";
        var meshes = RaceVisualSchema.AllRaces
            .Select(r => RaceShipMeshes.Build(r.Id, hull))
            .ToList();

        var vertexCounts = meshes.Select(ProceduralMeshes.VertexCount).Distinct().ToList();
        var firstColors = meshes.Select(m => (m[3], m[4], m[5])).Distinct().ToList();

        Assert.True(vertexCounts.Count > 1 || firstColors.Count > 1);
    }

    public static IEnumerable<object[]> ShipSubstrateEntries() =>
        RaceSubstrateCatalog.AllEntries()
            .Where(e => e.Kind == RaceSubstrateCatalog.SubstrateKind.Ship)
            .Select(e => new object[] { e });

    public static IEnumerable<object[]> StationSubstrateEntries() =>
        RaceSubstrateCatalog.AllEntries()
            .Where(e => e.Kind == RaceSubstrateCatalog.SubstrateKind.Station)
            .Select(e => new object[] { e });
}