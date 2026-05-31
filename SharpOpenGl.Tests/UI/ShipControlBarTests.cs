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
        bar.UpdateForShip(hasWeapons: true, hasMovement: true, stance: Stance.Defensive);

        // All children should be visible
        foreach (var child in bar.Children)
            Assert.True(child.Visible);
    }

    [Fact]
    public void ControlBar_hides_attack_for_unarmed_ship()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(hasWeapons: false, hasMovement: true, stance: null);

        // Find the attack-move button (index 3) and stance button (index 4)
        Assert.False(bar.Children[3].Visible); // A-Move
        Assert.False(bar.Children[4].Visible); // Stance
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
        bar.UpdateForShip(hasWeapons: true, hasMovement: true, stance: Stance.Aggressive);

        // The stance button label should reflect aggressive stance
        var stanceButton = (Button)bar.Children[4];
        Assert.Equal("[A]", stanceButton.Label);
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

        Assert.True(bar.HandleKeyShortcut('a'));
        Assert.Equal("attack_move", bar.ActiveCommand);

        Assert.False(bar.HandleKeyShortcut('z')); // unknown key
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
