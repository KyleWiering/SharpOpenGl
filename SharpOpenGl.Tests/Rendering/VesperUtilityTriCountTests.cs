using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class VesperUtilityTriCountTests
{
    public VesperUtilityTriCountTests() => RaceVisualSchema.ResetForTests();

    [Theory]
    [InlineData("freighter_bulk", 180, 210)]
    [InlineData("transport_cargo", 200, 230)]
    [InlineData("miner_basic", 170, 250)]
    [InlineData("miner_eva", 170, 250)]
    [InlineData("miner_tractor", 170, 250)]
    [InlineData("support_repair", 200, 260)]
    public void Vesper_utility_hulls_stay_within_loop2_tri_budget(string hullId, int minTris, int maxTris)
    {
        float[] mesh = RaceShipMeshes.Build("vesper", hullId);
        int tris = mesh.Length / ProceduralMeshes.Stride / 3;
        Assert.True(tris >= minTris && tris <= maxTris, $"{hullId}: {tris} tris (expected {minTris}-{maxTris})");
    }
}