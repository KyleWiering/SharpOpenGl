using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class RaceSilhouetteTests
{
    public RaceSilhouetteTests() => RaceVisualSchema.ResetForTests();

    [Fact]
    public void Each_race_fighter_has_distinct_silhouette_vertex_count()
    {
        var counts = RaceVisualSchema.AllRaces
            .Select(r => (r.Id, ProceduralMeshes.VertexCount(RaceShipMeshes.Build(r.Id, "fighter_basic"))))
            .ToList();

        var distinct = counts.Select(c => c.Item2).Distinct().Count();
        Assert.True(distinct >= 7, $"Expected >=7 distinct fighter silhouettes, got {distinct}: {string.Join(", ", counts.Select(c => $"{c.Id}={c.Item2}"))}");
        Assert.True(counts.First(c => c.Id == "korath").Item2 > counts.First(c => c.Id == "vesper").Item2);
        Assert.True(counts.First(c => c.Id == "nexar").Item2 > counts.First(c => c.Id == "vesper").Item2);
    }

    [Theory]
    [InlineData("korath", "truss")]
    [InlineData("vesper", "vasudan")]
    [InlineData("aetherian", "organic")]
    [InlineData("nexar", "asymmetric")]
    [InlineData("terran", "retro")]
    public void Race_style_matches_schema(string raceId, string expectedStyle)
    {
        Assert.True(RaceVisualSchema.TryGetRace(raceId, out var race));
        Assert.Equal(expectedStyle, race!.Style);
        float[] mesh = RaceShipMeshes.Build(raceId, "destroyer_assault");
        Assert.True(ProceduralMeshes.VertexCount(mesh) >= 20);
    }
}