using SharpOpenGl.Engine.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace SharpOpenGl.Tests.Rendering;

public class KorathCapitalTriCountTests
{
    private readonly ITestOutputHelper _output;

    public KorathCapitalTriCountTests(ITestOutputHelper output)
    {
        _output = output;
        RaceVisualSchema.ResetForTests();
    }

    [Fact]
    public void Korath_capital_loop5_tri_budgets()
    {
        Assert.True(TriCount("carrier_command") <= 400, $"carrier_command {TriCount("carrier_command")} tris");
        Assert.True(TriCount("dreadnought") <= 500, $"dreadnought {TriCount("dreadnought")} tris");
        Assert.True(TriCount("cruiser_heavy") <= 661, $"cruiser_heavy {TriCount("cruiser_heavy")} tris");
        foreach (var hull in new[] { "carrier_command", "dreadnought", "cruiser_heavy" })
            _output.WriteLine($"{hull}: {TriCount(hull)} tris");
    }

    private static int TriCount(string hull) =>
        RaceShipMeshes.Build("korath", hull).Length / ProceduralMeshes.Stride / 3;
}