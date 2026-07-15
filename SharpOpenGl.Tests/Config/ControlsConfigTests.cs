using SharpOpenGl.Engine.Config;
using Xunit;

namespace SharpOpenGl.Tests.Config;

public class ControlsConfigTests
{
    [Fact]
    public void controls_json_loads_ship_control_bar_shortcuts()
    {
        string root = ResolveGameDataRoot();
        var config = ControlsConfig.Load(root);

        Assert.NotNull(config.ShipControlBar);
        var bar = config.ShipControlBar!;
        Assert.Equal("M", bar.Move);
        Assert.Equal("S", bar.Stop);
        Assert.Equal("X", bar.StopAlt);
        Assert.Equal("P", bar.Patrol);
        Assert.Equal("T", bar.Attack);
        Assert.Equal("A", bar.AttackMove);
        Assert.Equal("F", bar.AttackMoveAlt);
        Assert.Equal("H", bar.HoldPosition);
        Assert.Equal("V", bar.DefensiveStance);
        Assert.Equal("G", bar.FormationCycle);
        Assert.Equal("B", bar.BuildStructures);
    }

    [Fact]
    public void controls_json_includes_append_waypoint_mouse_binding()
    {
        string root = ResolveGameDataRoot();
        var config = ControlsConfig.Load(root);

        Assert.NotNull(config.Mouse);
        Assert.Equal("Shift+RightButton", config.Mouse!["AppendWaypoint"]);
    }

    [Fact]
    public void Key_binding_overrides_merge_into_keyboard_map()
    {
        string root = ResolveGameDataRoot();
        var overrides = new Dictionary<string, string> { ["Pause"] = "P" };
        var config = ControlsConfig.Load(root, overrides);

        Assert.NotNull(config.Keyboard);
        Assert.Equal("P", config.Keyboard!["Pause"]);
    }

    [Fact]
    public void ship_control_bar_shortcuts_match_handle_key_mapping()
    {
        string root = ResolveGameDataRoot();
        var bar = ControlsConfig.Load(root).ShipControlBar!;
        var hud = new SharpOpenGl.Engine.UI.Widgets.ShipControlBar();
        hud.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: null,
            formation: null,
            showFormation: true);

        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.Move[0])));
        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.Stop[0])));
        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.Patrol[0])));
        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.Attack[0])));
        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.AttackMove[0])));
        // F is routed via EngineWindow.TryHandleUnitShortcut (maps Keys.F → 'a' before HandleKeyShortcut).
        Assert.Equal("F", bar.AttackMoveAlt);
        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.Harvest[0])));
        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.BuildStructures[0])));
        Assert.True(hud.HandleKeyShortcut(char.ToLowerInvariant(bar.FormationCycle[0])));
    }

    private static string ResolveGameDataRoot()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln.");
        return Path.Combine(dir, "GameData");
    }
}