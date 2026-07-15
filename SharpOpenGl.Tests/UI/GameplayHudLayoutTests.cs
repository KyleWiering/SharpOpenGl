using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class GameplayHudLayoutTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 CompactPhysicalViewport = new(1024f, 768f);

    [Fact]
    public void ApplyDensityLayout_clamps_build_map_panel_above_minimap()
    {
        var hud = CreateCrowdedHud();
        hud.ApplyDensityLayout();

        var buildMap = GameplayHudLayout.GetBounds(hud.BuildMapPanel, ReferenceViewport);
        var minimap = GameplayHudLayout.GetBounds(hud.Minimap, ReferenceViewport);

        Assert.True(buildMap.Bottom <= minimap.Top - GameplayHudLayout.PanelGap + 0.5f);
        Assert.True(buildMap.Top >= GameplayHudLayout.ResourceBarHeight + GameplayHudLayout.PanelGap - 0.5f);
    }

    [Fact]
    public void ApplyDensityLayout_keeps_unit_info_clear_of_build_map_at_1024x768()
    {
        var hud = CreateCrowdedHud();
        hud.BuildMapPanel.Visible = true;
        hud.ApplyDensityLayout();

        var buildMap = GameplayHudLayout.GetBounds(hud.BuildMapPanel, ReferenceViewport);
        var unitInfo = GameplayHudLayout.GetBounds(hud.UnitInfoPanel, ReferenceViewport);

        Assert.False(buildMap.Overlaps(unitInfo));
        Assert.True(unitInfo.Left >= buildMap.Right + GameplayHudLayout.PanelGap - 0.5f);
        Assert.Equal(GameplayHudLayout.UnitInfoCompactWidth, unitInfo.Width, 0.5f);
        Assert.Equal(GameplayHudLayout.UnitInfoCompactHeight, unitInfo.Height, 0.5f);
    }

    [Fact]
    public void ApplyDensityLayout_restores_standard_unit_info_when_build_map_closed()
    {
        var hud = CreateCrowdedHud();
        hud.BuildMapPanel.Visible = false;
        hud.ApplyDensityLayout();

        var unitInfo = GameplayHudLayout.GetBounds(hud.UnitInfoPanel, ReferenceViewport);

        Assert.Equal(GameplayHudLayout.UnitInfoStandardWidth, unitInfo.Width, 0.5f);
        Assert.Equal(GameplayHudLayout.UnitInfoStandardHeight, unitInfo.Height, 0.5f);
    }

    [Fact]
    public void GameplayHUD_visible_panels_do_not_overlap_when_build_map_open()
    {
        var hud = CreateCrowdedHud();
        hud.BuildMapPanel.Visible = true;
        hud.BindShipControlBar(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Defensive,
            anySelected: true,
            formation: FormationType.Wedge,
            showFormation: true);
        hud.ApplyDensityLayout();

        var panels = new List<GameplayHudLayout.PanelRect>
        {
            GameplayHudLayout.GetBounds(hud.ResourceBar, ReferenceViewport),
            GameplayHudLayout.GetBounds(hud.Minimap, ReferenceViewport),
            GameplayHudLayout.GetBounds(hud.UnitInfoPanel, ReferenceViewport),
            GameplayHudLayout.GetBounds(hud.ShipControlBar, ReferenceViewport),
            GameplayHudLayout.GetBounds(hud.BuildMapPanel, ReferenceViewport),
        };

        Assert.False(GameplayHudLayout.AnyOverlap(panels));
    }

    [Fact]
    public void ResourceBar_density_layout_fits_40px_height()
    {
        var hud = new GameplayHUD();
        hud.ApplyDensityLayout();

        Assert.Equal(GameplayHudLayout.ResourceBarHeight, hud.ResourceBar.Size.Y);
        Assert.True(hud.ResourceBar.Size.Y <= 40f);
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    public void GameplayHUD_draw_applies_density_layout_before_widgets(float viewportWidth, float viewportHeight)
    {
        var physical = new Vector2(viewportWidth, viewportHeight);
        var inner = new RecordingRenderer(physical);
        var scaler = new UIScaler(physical);
        var renderer = new ScaledUIRenderer(inner, scaler);

        var hud = CreateCrowdedHud();
        hud.BuildMapPanel.Visible = true;
        hud.Draw(renderer);

        var buildMap = GameplayHudLayout.GetBounds(hud.BuildMapPanel, ReferenceViewport);
        var unitInfo = GameplayHudLayout.GetBounds(hud.UnitInfoPanel, ReferenceViewport);

        Assert.False(buildMap.Overlaps(unitInfo));
    }

    [Fact]
    public void BuildMapPanel_compact_header_uses_scroll_viewport_when_height_clamped()
    {
        var panel = new BuildMapPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Categories = CreateCrowdedHud().BuildMapPanel.Categories,
        };

        Assert.True(BuildMapPanel.IsCompactHeader(panel.Size));
        float viewport = GameplayHudLayout.BuildMapPanelHeight
            - (BuildMapPanel.GetHeaderBlockHeight(compactHeader: true) + 10f);
        Assert.True(viewport > 360f);
    }

    private static GameplayHUD CreateCrowdedHud()
    {
        var hud = new GameplayHUD
        {
            Visible = true,
            ShowBuildFlowHint = false,
            ShowFirstMissionOnboardingHint = false,
        };

        hud.ResourceBar.Resources =
        [
            new SharpOpenGl.Engine.Economy.ResourceDisplay(
                SharpOpenGl.Engine.Economy.ResourceType.Energy, 450f, 1000f, 12f),
        ];

        hud.BuildMapPanel.Visible = true;
        hud.BuildMapPanel.Categories =
        [
            new SharpOpenGl.Engine.Build.BuildMapCategoryView
            {
                DisplayName = "Defense",
                Buildings =
                [
                    new SharpOpenGl.Engine.Build.BuildMapEntryView
                    {
                        Id = "turret",
                        Name = "Automated Defense Turret Array Complex",
                        IsUnlocked = true,
                        CanAfford = false,
                    },
                    new SharpOpenGl.Engine.Build.BuildMapEntryView
                    {
                        Id = "shield",
                        Name = "Planetary Shield Generator Relay Station",
                        IsUnlocked = false,
                        CanAfford = true,
                    },
                ],
            },
        ];

        hud.UnitInfoPanel.SelectedUnits =
        [
            new UnitInfo
            {
                Name = "Super Heavy Assault Cruiser Mk III Extended Production Variant",
                CurrentHP = 900,
                MaxHP = 1000,
                HPFraction = 0.9f,
            },
        ];

        return hud;
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}