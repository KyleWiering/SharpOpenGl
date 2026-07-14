using SharpOpenGl.Engine.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace SharpOpenGl.Tests.Rendering;

public class SolariCapitalTriCountTests
{
    private readonly ITestOutputHelper _output;

    public SolariCapitalTriCountTests(ITestOutputHelper output)
    {
        _output = output;
        RaceVisualSchema.ResetForTests();
    }

    [Fact]
    public void Solari_capital_loop3_tri_budgets()
    {
        Assert.True(TriCount("carrier_command") <= 320, $"carrier_command {TriCount("carrier_command")} tris");
        Assert.True(TriCount("dreadnought") <= 380, $"dreadnought {TriCount("dreadnought")} tris");
        Assert.True(TriCount("cruiser_heavy") <= 255, $"cruiser_heavy {TriCount("cruiser_heavy")} tris");
        foreach (var hull in new[] { "carrier_command", "dreadnought", "cruiser_heavy" })
            _output.WriteLine($"{hull}: {TriCount(hull)} tris");
    }

    private static int TriCount(string hull) =>
        RaceShipMeshes.Build("solari", hull).Length / ProceduralMeshes.Stride / 3;
}