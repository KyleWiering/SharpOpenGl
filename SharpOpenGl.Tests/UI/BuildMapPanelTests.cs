using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class BuildMapPanelTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

    [Fact]
    public void Selecting_ready_building_fires_BuildingSelected()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        string? selected = null;
        panel.BuildingSelected += id => selected = id;

        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var tap = pos + new Vector2(20f, 80f);
        bool consumed = panel.HandlePointerTapped(tap, 0, Vector2.Zero, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal("defense_turret", selected);
    }

    [Fact]
    public void Locked_building_click_does_not_fire_selection()
    {
        var panel = CreatePanelWithEntry(unlocked: false, afford: true);
        string? selected = null;
        panel.BuildingSelected += id => selected = id;

        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var tap = pos + new Vector2(20f, 80f);
        panel.HandlePointerTapped(tap, 0, Vector2.Zero, ReferenceViewport);

        Assert.Null(selected);
    }

    [Fact]
    public void GameplayHUD_routes_build_map_clicks_before_world()
    {
        var hud = new GameplayHUD();
        hud.BuildMapPanel.Visible = true;
        hud.BuildMapPanel.Categories = CreatePanelWithEntry(true, true).Categories;

        string? selected = null;
        hud.BuildMapPanel.BuildingSelected += id => selected = id;

        var (pos, _) = hud.BuildMapPanel.Resolve(Vector2.Zero, ReferenceViewport);
        var tap = pos + new Vector2(20f, 80f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal("defense_turret", selected);
    }

    [Fact]
    public void Build_map_button_fires_BuildMapRequested()
    {
        var hud = new GameplayHUD();
        bool opened = false;
        hud.BuildMapRequested += () => opened = true;

        var tap = new Vector2(1756f, 28f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.True(opened);
    }

    private static BuildMapPanel CreatePanelWithEntry(bool unlocked, bool afford)
    {
        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(12f, -220f),
            Size = new Vector2(420f, 640f),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "defense",
                    DisplayName = "Defense",
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "defense_turret",
                            Name = "Defense Turret",
                            FootprintCols = 1,
                            FootprintRows = 1,
                            EnergyCost = 60,
                            MineralsCost = 90,
                            IsUnlocked = unlocked,
                            CanAfford = afford,
                        },
                    ],
                },
            ],
        };
        return panel;
    }
}