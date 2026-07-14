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
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        for (int i = 0; i < bar.Children.Count; i++)
        {
            var button = bar.Children[i];
            bool expectVisible = button != bar.Children[7] && button != bar.Children[8];
            Assert.Equal(expectVisible, button.Visible);
        }
    }

    [Fact]
    public void ControlBar_hides_build_when_not_structure_builder()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        var buildButton = Assert.IsType<IconButton>(bar.Children[7]);
        Assert.Equal("Build", buildButton.Label);
        Assert.Equal(MenuIconKind.Build, buildButton.Icon);
        Assert.False(buildButton.Visible);
    }

    [Fact]
    public void ControlBar_hides_attack_for_unarmed_ship()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: false,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
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
        Assert.Equal(412f, bar.Size.X);
        Assert.Equal(292f, bar.Size.Y);
        Assert.Equal(120f, ShipControlBar.ButtonWidth);
        Assert.Equal(80f, ShipControlBar.ButtonHeight);
        Assert.Equal(12f, ShipControlBar.ButtonGap);
    }

    [Fact]
    public void Build_button_hit_rect_meets_minimum_gutter()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        var (barPos, barSize) = bar.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var buildButton = Assert.IsType<IconButton>(bar.Children[7]);
        var harvestButton = Assert.IsType<IconButton>(bar.Children[8]);
        var (buildPos, buildSize) = buildButton.Resolve(barPos, barSize);

        var (_, hitSize) = ShipControlBar.GetExpandedHitRect(buildPos, buildSize, requireMinimumExtent: true);
        Assert.True(hitSize.X >= ShipControlBar.MinimumCommandHitExtent);
        Assert.True(hitSize.Y >= ShipControlBar.MinimumCommandHitExtent);

        float gutterX = buildPos.X + buildSize.X + ShipControlBar.ButtonGap * 0.5f;
        float rowCenterY = buildPos.Y + buildSize.Y * 0.5f;
        var gutterPoint = new Vector2(gutterX, rowCenterY);

        buildButton.UpdatePointerState(gutterPoint, false, barPos, barSize);
        harvestButton.UpdatePointerState(gutterPoint, false, barPos, barSize);
        Assert.False(buildButton.IsHovered);
        Assert.False(harvestButton.IsHovered);
    }

    [Fact]
    public void ControlBar_command_buttons_use_icon_button_type()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        MenuIconKind[] expectedIcons =
        [
            MenuIconKind.Move,
            MenuIconKind.Stop,
            MenuIconKind.Patrol,
            MenuIconKind.Attack,
            MenuIconKind.AttackMove,
            MenuIconKind.StanceDefensive,
            MenuIconKind.FormationLine,
            MenuIconKind.Build,
            MenuIconKind.Harvest,
        ];

        Assert.Equal(expectedIcons.Length, bar.Children.Count);
        for (int i = 0; i < bar.Children.Count; i++)
        {
            var iconButton = Assert.IsType<IconButton>(bar.Children[i]);
            Assert.Equal(expectedIcons[i], iconButton.Icon);
            Assert.DoesNotContain("[", iconButton.Label);
        }
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
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Aggressive,
            formation: FormationType.Wedge,
            showFormation: true);

        var stanceButton = Assert.IsType<IconButton>(bar.Children[5]);
        Assert.Equal(MenuIconKind.StanceAggressive, stanceButton.Icon);
        Assert.Equal("Aggressive", stanceButton.Label);
        Assert.True(stanceButton.IsActive);
        Assert.DoesNotContain("[", stanceButton.Label);

        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Defensive,
            formation: FormationType.Wedge,
            showFormation: true);

        Assert.Equal(MenuIconKind.StanceDefensive, stanceButton.Icon);
        Assert.Equal("Defensive", stanceButton.Label);
        Assert.True(stanceButton.IsActive);

        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Neutral,
            formation: FormationType.Wedge,
            showFormation: true);

        Assert.Equal(MenuIconKind.StancePassive, stanceButton.Icon);
        Assert.Equal("Hold", stanceButton.Label);
        Assert.True(stanceButton.IsActive);
    }

    [Fact]
    public void ControlBar_formation_display_updates()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Defensive,
            formation: FormationType.Wedge,
            showFormation: true);

        var formationButton = Assert.IsType<IconButton>(bar.Children[6]);
        Assert.Equal("Wedge", formationButton.Label);
        Assert.Equal(MenuIconKind.FormationWedge, formationButton.Icon);
        Assert.Equal("Formation: Wedge (G)", formationButton.TooltipHint);
        Assert.True(formationButton.IsActive);

        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Defensive,
            formation: FormationType.Box,
            showFormation: true);

        Assert.Equal("Box", formationButton.Label);
        Assert.Equal(MenuIconKind.FormationBox, formationButton.Icon);
        Assert.Equal("Formation: Box (G)", formationButton.TooltipHint);
        Assert.True(formationButton.IsActive);
    }

    [Fact]
    public void ControlBar_attack_move_and_stance_labels_are_readable()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        var attackMove = Assert.IsType<IconButton>(bar.Children[4]);
        Assert.Equal("Attack Move", attackMove.Label);
        Assert.Equal("Attack Move (A)", attackMove.TooltipHint);

        var stance = Assert.IsType<IconButton>(bar.Children[5]);
        Assert.Equal("Defensive", stance.Label);
        Assert.Equal("Stance: Defensive (V)", stance.TooltipHint);
        Assert.True(stance.IsActive);
    }

    [Fact]
    public void ControlBar_stance_colors_differ_by_mode()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Aggressive,
            formation: null,
            showFormation: false);

        var aggressive = Assert.IsType<IconButton>(bar.Children[5]);
        Vector4 aggressiveColor = aggressive.ActiveNormalColor;

        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: Stance.Defensive,
            formation: null,
            showFormation: false);

        var defensive = Assert.IsType<IconButton>(bar.Children[5]);
        Assert.NotEqual(aggressiveColor, defensive.ActiveNormalColor);
    }

    [Fact]
    public void ControlBar_command_buttons_have_icon_and_readable_label()
    {
        var bar = new ShipControlBar();
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Defensive,
            formation: FormationType.Line,
            showFormation: true);

        foreach (var child in bar.Children)
        {
            var button = Assert.IsType<IconButton>(child);
            if (!button.Visible) continue;

            Assert.False(string.IsNullOrWhiteSpace(button.Label));
            Assert.InRange(button.Label.Length, 1, 12);
            Assert.DoesNotContain("[", button.Label);
            Assert.False(string.IsNullOrWhiteSpace(button.TooltipHint));
            Assert.True(Enum.IsDefined(button.Icon));
        }
    }

    [Fact]
    public void ControlBar_abbreviated_buttons_provide_tooltip_on_hover()
    {
        var bar = new ShipControlBar { Visible = true };
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Defensive,
            formation: FormationType.Wedge,
            showFormation: true);

        var (barPos, barSize) = bar.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var attackButton = Assert.IsType<IconButton>(bar.Children[3]);
        var (btnPos, btnSize) = attackButton.Resolve(barPos, barSize);
        var center = btnPos + btnSize * 0.5f;

        attackButton.UpdatePointerState(center, false, barPos, barSize);

        TooltipContent? tooltip = attackButton.GetTooltipContent();
        Assert.NotNull(tooltip);
        Assert.Equal("Attack (T)", tooltip!.Title);
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