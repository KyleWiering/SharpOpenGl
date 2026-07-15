using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class KorathAccentRatioTests
{
    public KorathAccentRatioTests() => RaceVisualSchema.ResetForTests();

    [Theory]
    [InlineData("scout_light")]
    [InlineData("fighter_basic")]
    [InlineData("interceptor_mk2")]
    [InlineData("drone_swarm")]
    public void Korath_compact_hulls_have_scorer_visible_accent_ratio(string hullId)
    {
        float ratio = MeasureAccentRatio(RaceShipMeshes.Build("korath", hullId));
        Assert.True(ratio > 0f, $"korath/{hullId} accent ratio {ratio:P2} — expected >0% (verts lum >0.9)");
    }

    [Fact]
    public void Korath_scout_light_accent_ratio_exceeds_two_percent_of_verts()
    {
        float ratio = MeasureAccentRatio(RaceShipMeshes.Build("korath", "scout_light"));
        Assert.True(ratio >= 0.02f, $"scout_light accent ratio {ratio:P2} — expected ≥2% scorer accent verts");
    }

    private static float MeasureAccentRatio(float[] mesh)
    {
        int accentVerts = 0;
        int totalVerts = ProceduralMeshes.VertexCount(mesh);
        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = (mesh[i + 3] + mesh[i + 4] + mesh[i + 5]) / 3f;
            if (lum > 0.9f)
                accentVerts++;
        }

        return accentVerts / Math.Max(totalVerts, 1f);
    }
}