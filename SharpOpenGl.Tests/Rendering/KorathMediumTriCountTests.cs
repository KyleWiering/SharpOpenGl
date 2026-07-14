using SharpOpenGl.Engine.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace SharpOpenGl.Tests.Rendering;

public class KorathMediumTriCountTests
{
    private readonly ITestOutputHelper _output;

    public KorathMediumTriCountTests(ITestOutputHelper output)
    {
        _output = output;
        RaceVisualSchema.ResetForTests();
    }

    [Fact]
    public void Korath_medium_loop3_tri_budgets()
    {
        Assert.True(TriCount("bomber_heavy") <= 300, $"bomber_heavy {TriCount("bomber_heavy")} tris");
        Assert.True(TriCount("gunship_heavy") <= 280, $"gunship_heavy {TriCount("gunship_heavy")} tris");
        Assert.True(TriCount("frigate_strike") <= 320, $"frigate_strike {TriCount("frigate_strike")} tris");
        Assert.True(TriCount("destroyer_assault") <= 450, $"destroyer_assault {TriCount("destroyer_assault")} tris");
        foreach (var hull in new[] { "bomber_heavy", "gunship_heavy", "frigate_strike", "destroyer_assault", "corvette_fast" })
            _output.WriteLine($"{hull}: {TriCount(hull)} tris");
    }

    private static int TriCount(string hull) =>
        RaceShipMeshes.Build("korath", hull).Length / ProceduralMeshes.Stride / 3;
}