using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.UI;
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

        var harvestButton = Assert.IsType<IconButton>(bar.Children[8]);
        Assert.Equal("Harvest", harvestButton.Label);
        Assert.Equal(MenuIconKind.Harvest, harvestButton.Icon);
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

        var buildButton = Assert.IsType<IconButton>(bar.Children[7]);
        Assert.Equal("Build", buildButton.Label);
        Assert.Equal(MenuIconKind.Build, buildButton.Icon);
        Assert.Equal("Build structures (B)", buildButton.TooltipHint);
        Assert.True(buildButton.Label.Length > 1);
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
    public void SetActiveCommand_build_highlights_build_button()
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

        var buildButton = Assert.IsType<IconButton>(bar.Children[7]);
        var moveButton = Assert.IsType<IconButton>(bar.Children[0]);

        bar.SetActiveCommand("build");

        Assert.Equal("build", bar.ActiveCommand);
        Assert.False(moveButton.IsActive);
        Assert.True(buildButton.IsActive);
    }

    [Fact]
    public void ClearActiveCommand_clears_build_highlight()
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

        var buildButton = Assert.IsType<IconButton>(bar.Children[7]);
        bar.SetActiveCommand("build");
        bar.ClearActiveCommand();

        Assert.Null(bar.ActiveCommand);
        Assert.False(buildButton.IsActive);
    }

    [Fact]
    public void Build_active_state_unchanged_after_hit_padding()
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

        var (barPos, barSize) = bar.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var buildButton = Assert.IsType<IconButton>(bar.Children[7]);
        var (btnPos, btnSize) = buildButton.Resolve(barPos, barSize);

        var tap = new Vector2(btnPos.X - 2f, btnPos.Y + btnSize.Y * 0.5f);
        Assert.True(buildButton.HandlePointerTapped(tap, 0, barPos, barSize));
        Assert.Equal("build", bar.ActiveCommand);
        Assert.True(buildButton.IsActive);
    }

    [Fact]
    public void Build_button_tooltip_available_on_hover()
    {
        var bar = new ShipControlBar { Visible = true };
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: true,
            stance: null,
            formation: null,
            showFormation: false);

        var (barPos, barSize) = bar.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var buildButton = Assert.IsType<IconButton>(bar.Children[7]);
        var (btnPos, btnSize) = buildButton.Resolve(barPos, barSize);
        var center = btnPos + btnSize * 0.5f;

        buildButton.UpdatePointerState(center, false, barPos, barSize);

        TooltipContent? tooltip = buildButton.GetTooltipContent();
        Assert.NotNull(tooltip);
        Assert.Equal("Build structures (B)", tooltip!.Title);
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