using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ShipControlBarBuilderTests
{
    [Fact]
    public void ShipControlBar_shows_harvest_when_collector_selected()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: false,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: false,
            stance: null,
            formation: null,
            showFormation: false);

        var harvestButton = (Button)bar.Children[8];
        Assert.Equal("Hvst", harvestButton.Label);
        Assert.Equal("Harvest (H)", harvestButton.TooltipHint);
        Assert.True(harvestButton.Visible);
    }

    [Fact]
    public void ShipControlBar_shows_build_when_structure_builder_selected()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: true,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        var buildButton = (Button)bar.Children[7];
        Assert.Equal("Build", buildButton.Label);
        Assert.True(buildButton.Visible);
    }

    [Fact]
    public void ShipControlBar_harvest_shortcut_sets_active_command()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: false,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: false,
            stance: null,
            formation: null,
            showFormation: false);

        string? received = null;
        bar.CommandActivated += cmd => received = cmd;

        Assert.True(bar.HandleKeyShortcut('h'));
        Assert.Equal("harvest", received);
        Assert.Equal("harvest", bar.ActiveCommand);
    }

    [Fact]
    public void Build_shortcut_sets_active_command()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: true,
            stance: null,
            formation: null,
            showFormation: false);

        string? received = null;
        bar.CommandActivated += cmd => received = cmd;

        Assert.True(bar.HandleKeyShortcut('b'));
        Assert.Equal("build", received);
        Assert.Equal("build", bar.ActiveCommand);

        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: null,
            formation: null,
            showFormation: false);

        Assert.False(bar.HandleKeyShortcut('b'));
    }
}