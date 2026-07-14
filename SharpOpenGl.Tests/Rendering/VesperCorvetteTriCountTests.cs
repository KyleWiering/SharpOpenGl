using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class VesperCorvetteTriCountTests
{
    public VesperCorvetteTriCountTests() => RaceVisualSchema.ResetForTests();

    [Fact]
    public void Vesper_corvette_fast_loop3_recovery_tri_budget()
    {
        float[] mesh = RaceShipMeshes.Build("vesper", "corvette_fast");
        int tris = mesh.Length / ProceduralMeshes.Stride / 3;
        Assert.InRange(tris, 190, 200);
    }

    [Fact]
    public void Vesper_medium_loop4_tri_budgets()
    {
        int frigate = TriCount("frigate_strike");
        int gunship = TriCount("gunship_heavy");
        int bomber = TriCount("bomber_heavy");
        Assert.True(frigate <= 220, $"frigate_strike {frigate} tris");
        Assert.True(gunship <= 220, $"gunship_heavy {gunship} tris");
        Assert.True(bomber <= 220, $"bomber_heavy {bomber} tris");
    }

    private static int TriCount(string hull) =>
        RaceShipMeshes.Build("vesper", hull).Length / ProceduralMeshes.Stride / 3;
}