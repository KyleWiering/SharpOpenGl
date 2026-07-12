using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class KorathHullTests
{
    public KorathHullTests() => RaceVisualSchema.ResetForTests();

    [Theory]
    [InlineData("fighter_basic", 900)]
    [InlineData("dreadnought", 1200)]
    [InlineData("scout_light", 280)]
    public void Korath_ships_have_dense_NASA_hull_geometry(string shipId, int minVerts)
    {
        int verts = ProceduralMeshes.VertexCount(RaceShipMeshes.Build("korath", shipId));
        Assert.True(verts >= minVerts, $"korath/{shipId} has {verts} verts, expected >= {minVerts}");
    }

    [Fact]
    public void Korath_palette_is_warm_NASA_neutral_not_blue_purple()
    {
        Assert.True(RaceVisualSchema.TryGetRace("korath", out var race));
        float[] mesh = RaceShipMeshes.Build("korath", "fighter_basic");

        float r = 0f, g = 0f, b = 0f;
        int count = ProceduralMeshes.VertexCount(mesh);
        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            r += mesh[i + 3];
            g += mesh[i + 4];
            b += mesh[i + 5];
        }
        r /= count; g /= count; b /= count;

        Assert.True(r > 0.55f && g > 0.5f, $"Hull too dark: avg=({r:F2},{g:F2},{b:F2})");
        Assert.True(r >= g && g >= b * 0.92f, $"Blue/purple cast: avg=({r:F2},{g:F2},{b:F2})");
        Assert.True(race!.Palette.Accent[0] > race.Palette.Accent[2], "Accent should be gold-dominant, not blue");
    }

    [Fact]
    public void Korath_style_is_truss()
    {
        Assert.True(RaceVisualSchema.TryGetRace("korath", out var race));
        Assert.Equal("truss", race!.Style);
    }
}