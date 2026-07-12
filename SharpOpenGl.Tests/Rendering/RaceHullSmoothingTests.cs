using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

/// <summary>
/// Races 3–8 (korath → cryo) use smoothed lofted hulls — verify cohesive high-vertex meshes.
/// </summary>
public class RaceHullSmoothingTests
{
    private static readonly string[] Races38 =
        ["korath", "aetherian", "nexar", "solari", "voidborn", "cryo"];

    public RaceHullSmoothingTests() => RaceVisualSchema.ResetForTests();

    [Theory]
    [MemberData(nameof(AllRace38Ships))]
    public void Race_3_to_8_ships_have_smooth_hull_vertex_budget(string raceId, string shipId)
    {
        float[] mesh = RaceShipMeshes.Build(raceId, shipId);
        int verts = ProceduralMeshes.VertexCount(mesh);
        int minimum = shipId is "drone_swarm" or "scout_light" ? 45 : 60;
        Assert.True(verts >= minimum,
            $"{raceId}/{shipId} has {verts} verts; expected >= {minimum} for cohesive hull");
    }

    [Fact]
    public void Race_3_to_8_fighters_exceed_patchwork_baseline()
    {
        foreach (string race in Races38)
        {
            int verts = ProceduralMeshes.VertexCount(RaceShipMeshes.Build(race, "fighter_basic"));
            Assert.True(verts >= 72, $"{race} fighter has {verts} verts; expected >= 72");
        }
    }

    public static IEnumerable<object[]> AllRace38Ships()
    {
        foreach (string race in Races38)
        foreach (string ship in FleetGalleryLayout.AllShipIds)
            yield return [race, ship];
    }
}