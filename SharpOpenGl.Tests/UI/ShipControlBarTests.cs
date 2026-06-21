using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ShipControlBarTests
{
    [Fact]
    public void ControlBar_shows_all_buttons_for_armed_ship()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        foreach (var child in bar.Children)
            Assert.True(child.Visible);
    }

    [Fact]
    public void ControlBar_hides_attack_for_unarmed_ship()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: false,
            hasMovement: true,
            stance: null,
            formation: null,
            showFormation: false);

        Assert.False(bar.Children[3].Visible); // Attack
        Assert.False(bar.Children[4].Visible); // A-Move
        Assert.False(bar.Children[5].Visible); // Stance
        Assert.False(bar.Children[6].Visible); // Formation
    }

    [Fact]
    public void ControlBar_uses_standard_grid_dimensions()
    {
        var bar = new ShipControlBar();
        Assert.Equal(406f, bar.Size.X);
        Assert.Equal(288f, bar.Size.Y);
        Assert.Equal(120f, ShipControlBar.ButtonWidth);
        Assert.Equal(80f, ShipControlBar.ButtonHeight);
    }

    [Fact]
    public void ControlBar_command_activated_event_fires()
    {
        var bar = new ShipControlBar();
        string? received = null;
        bar.CommandActivated += cmd => received = cmd;

        bar.HandleKeyShortcut('m');
        Assert.Equal("move", received);
    }

    [Fact]
    public void ControlBar_stance_display_updates()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            stance: Stance.Aggressive,
            formation: FormationType.Wedge,
            showFormation: true);

        var stanceButton = (Button)bar.Children[5];
        Assert.Equal("[A]", stanceButton.Label);

        var formationButton = (Button)bar.Children[6];
        Assert.Equal("Wedge", formationButton.Label);
    }

    [Fact]
    public void ControlBar_keyboard_shortcuts()
    {
        var bar = new ShipControlBar();

        Assert.True(bar.HandleKeyShortcut('m'));
        Assert.Equal("move", bar.ActiveCommand);

        Assert.True(bar.HandleKeyShortcut('s'));
        Assert.Equal("stop", bar.ActiveCommand);

        Assert.True(bar.HandleKeyShortcut('p'));
        Assert.Equal("patrol", bar.ActiveCommand);

        Assert.True(bar.HandleKeyShortcut('t'));
        Assert.Equal("attack", bar.ActiveCommand);

        Assert.True(bar.HandleKeyShortcut('a'));
        Assert.Equal("attack_move", bar.ActiveCommand);

        Assert.False(bar.HandleKeyShortcut('z'));
    }

    [Fact]
    public void ControlBar_formation_cycle_shortcut_fires_event()
    {
        var bar = new ShipControlBar();
        int cycles = 0;
        bar.FormationCycled += () => cycles++;

        Assert.True(bar.HandleKeyShortcut('g'));
        Assert.Equal(1, cycles);
    }

    [Fact]
    public void ControlBar_clear_command_resets_state()
    {
        var bar = new ShipControlBar();
        bar.HandleKeyShortcut('m');
        Assert.Equal("move", bar.ActiveCommand);

        bar.ClearActiveCommand();
        Assert.Null(bar.ActiveCommand);
    }
}