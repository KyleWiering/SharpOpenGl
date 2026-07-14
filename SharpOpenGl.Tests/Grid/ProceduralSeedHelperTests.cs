using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class ProceduralSeedHelperTests
{
    [Fact]
    public void ParseSeed_numeric_returns_value()
    {
        Assert.Equal(42, ProceduralSeedHelper.ParseSeed("42"));
        Assert.Equal(-7, ProceduralSeedHelper.ParseSeed("-7"));
        Assert.Equal(1_000_000, ProceduralSeedHelper.ParseSeed("1000000"));
    }

    [Fact]
    public void ParseSeed_same_string_same_hash()
    {
        const string seedText = "frontier-alpha";

        int first = ProceduralSeedHelper.ParseSeed(seedText);
        int second = ProceduralSeedHelper.ParseSeed(seedText);

        Assert.Equal(first, second);
        Assert.Equal(ProceduralSeedHelper.HashString(seedText), first);
    }

    [Fact]
    public void ParseSeed_empty_uses_documented_default()
    {
        Assert.Equal(ProceduralSeedHelper.EmptyInputDefaultSeed, ProceduralSeedHelper.ParseSeed(null));
        Assert.Equal(ProceduralSeedHelper.EmptyInputDefaultSeed, ProceduralSeedHelper.ParseSeed(""));
        Assert.Equal(ProceduralSeedHelper.EmptyInputDefaultSeed, ProceduralSeedHelper.ParseSeed("   "));
    }
}