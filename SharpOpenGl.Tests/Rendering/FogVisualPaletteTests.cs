using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class FogVisualPaletteTests
{
    [Fact]
    public void Palette_matches_FogNebulaOverlay_config_alphas()
    {
        Assert.Equal(FogNebulaOverlay.Config.UnexploredVeilAlpha, FogVisualPalette.UnexploredAlpha);
        Assert.Equal(FogNebulaOverlay.Config.ExploredVeilAlpha, FogVisualPalette.ExploredAlpha);
    }

    [Fact]
    public void Palette_core_colors_used_by_nebula_veil_layers()
    {
        var unexploredLayers = FogNebulaStyle.UnexploredLayers(0, 0);
        var exploredLayers = FogNebulaStyle.ExploredLayers(0, 0);

        Assert.Contains(unexploredLayers, l => l.Rgb == FogVisualPalette.UnexploredCore);
        Assert.Contains(unexploredLayers, l => l.Rgb == FogVisualPalette.UnexploredVeil);
        Assert.Contains(exploredLayers, l => l.Rgb == FogVisualPalette.ExploredCore);
        Assert.Contains(exploredLayers, l => l.Rgb == FogVisualPalette.ExploredVeil);
    }
}