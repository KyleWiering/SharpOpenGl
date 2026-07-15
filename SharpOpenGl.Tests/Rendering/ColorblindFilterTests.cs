using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ColorblindFilterTests
{
    [Fact]
    public void None_mode_preserves_team_tint()
    {
        Vector3 original = new(1f, 0.32f, 0.28f);
        Vector3 filtered = ColorblindFilter.Filter(original, ColorblindMode.None);
        Assert.Equal(original, filtered);
    }

    [Fact]
    public void RedGreen_mode_separates_red_and_green_channels()
    {
        Vector3 red = new(1f, 0.1f, 0.1f);
        Vector3 green = new(0.1f, 1f, 0.1f);
        Vector3 redFiltered = ColorblindFilter.Filter(red, ColorblindMode.RedGreen);
        Vector3 greenFiltered = ColorblindFilter.Filter(green, ColorblindMode.RedGreen);

        Assert.NotEqual(redFiltered, greenFiltered);
    }

    [Fact]
    public void Monochrome_mode_produces_equal_rgb()
    {
        Vector3 color = new(0.35f, 0.72f, 1f);
        Vector3 filtered = ColorblindFilter.Filter(color, ColorblindMode.Monochrome);
        Assert.Equal(filtered.X, filtered.Y, precision: 3);
        Assert.Equal(filtered.Y, filtered.Z, precision: 3);
    }

    [Fact]
    public void PlayerColorPalette_applies_active_filter()
    {
        ColorblindFilter.Mode = ColorblindMode.Monochrome;
        Vector3 tint = PlayerColorPalette.GetTint(2);
        Assert.Equal(tint.X, tint.Y, precision: 3);
        ColorblindFilter.Mode = ColorblindMode.None;
    }
}