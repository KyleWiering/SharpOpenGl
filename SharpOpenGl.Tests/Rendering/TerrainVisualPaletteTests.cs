using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class TerrainVisualPaletteTests
{
    [Fact]
    public void ResolveTint_impassable_has_highest_alpha()
    {
        Vector4? impassable = TerrainVisualPalette.ResolveTint(TerrainType.Impassable);
        Vector4? nebula = TerrainVisualPalette.ResolveTint(TerrainType.Nebula);

        Assert.NotNull(impassable);
        Assert.NotNull(nebula);
        Assert.True(impassable!.Value.W > nebula!.Value.W);
    }

    [Fact]
    public void ResolveTint_space_returns_null()
    {
        Assert.Null(TerrainVisualPalette.ResolveTint(TerrainType.Space));
    }

    [Fact]
    public void AffectsPathing_marks_non_default_terrain()
    {
        Assert.False(TerrainVisualPalette.AffectsPathing(TerrainType.Space));
        Assert.True(TerrainVisualPalette.AffectsPathing(TerrainType.AsteroidField));
        Assert.True(TerrainVisualPalette.AffectsPathing(TerrainType.IonStorm));
    }

    [Theory]
    [InlineData(TerrainType.Impassable, "Blocked")]
    [InlineData(TerrainType.AsteroidField, "Slow (×3)")]
    [InlineData(TerrainType.IonStorm, "Slow (×2.5)")]
    public void DescribePathing_labels_match_movement_cost(TerrainType terrain, string label)
    {
        Assert.Equal(label, TerrainVisualPalette.DescribePathing(terrain));
    }
}