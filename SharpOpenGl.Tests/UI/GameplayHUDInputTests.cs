using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class GameplayHUDInputTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

    [Fact]
    public void Clicks_on_empty_hud_area_do_not_consume_input()
    {
        var hud = new GameplayHUD();
        var tap = new Vector2(960f, 540f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);
        Assert.False(consumed);
    }

    [Fact]
    public void Clicks_on_minimap_consume_input()
    {
        var hud = new GameplayHUD();
        Vector2? clicked = null;
        hud.MinimapClicked += norm => clicked = norm;
        var tap = new Vector2(120f, 700f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);
        Assert.True(consumed);
        Assert.NotNull(clicked);
    }

    [Fact]
    public void Minimap_edge_clicks_route_through_hud()
    {
        var hud = new GameplayHUD();
        Vector2? clicked = null;
        hud.MinimapClicked += norm => clicked = norm;

        var (pos, size) = hud.Minimap.Resolve(Vector2.Zero, ReferenceViewport);
        var topLeftTap = pos;
        var bottomRightTap = pos + size;

        Assert.True(hud.HandlePointerTapped(topLeftTap, 0, ReferenceViewport));
        Assert.NotNull(clicked);
        Assert.Equal(0f, clicked!.Value.X, 2);
        Assert.Equal(0f, clicked.Value.Y, 2);

        clicked = null;
        Assert.True(hud.HandlePointerTapped(bottomRightTap, 0, ReferenceViewport));
        Assert.NotNull(clicked);
        Assert.Equal(1f, clicked!.Value.X, 2);
        Assert.Equal(1f, clicked.Value.Y, 2);
    }

    [Fact]
    public void Pause_button_consumes_click()
    {
        var hud = new GameplayHUD();
        bool paused = false;
        hud.PauseRequested += () => paused = true;
        // Pause button: TopRight (-8, 8), size 112×44 → centre ≈ (1856, 30).
        var tap = new Vector2(1856f, 30f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);
        Assert.True(consumed);
        Assert.True(paused);
    }

    [Fact]
    public void Ship_control_bar_build_button_routes_through_hud()
    {
        var hud = new GameplayHUD();
        hud.ShipControlBar.Visible = true;
        hud.ShipControlBar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: true,
            stance: null,
            formation: null,
            showFormation: false);

        var (barPos, barSize) = hud.ShipControlBar.Resolve(Vector2.Zero, ReferenceViewport);
        var buildButton = hud.ShipControlBar.Children[7];
        var (btnPos, btnSize) = buildButton.Resolve(barPos, barSize);
        var tap = btnPos + btnSize * 0.5f;

        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal("build", hud.ShipControlBar.ActiveCommand);
    }

    [Fact]
    public void Build_map_button_does_not_dismiss_build_flow_hint()
    {
        var hud = new GameplayHUD { Visible = true };
        Assert.True(hud.ShowBuildFlowHint);

        var tap = new Vector2(1744f, 30f);
        hud.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.True(hud.ShowBuildFlowHint);
    }

    [Fact]
    public void DismissBuildFlowHint_clears_coach_mark_and_builder_hint()
    {
        var hud = new GameplayHUD { Visible = true, ShowBuilderShortcutHint = true };
        hud.DismissBuildFlowHint();

        Assert.False(hud.ShowBuildFlowHint);
        Assert.False(hud.ShowBuilderShortcutHint);
    }

    [Fact]
    public void Builder_selected_shows_b_key_shortcut_hint()
    {
        var inner = new GameplayHudRecordingRenderer();
        var hud = new GameplayHUD { Visible = true, ShowBuildFlowHint = true };
        hud.BindShipControlBar(
            hasWeapons: false,
            hasMovement: false,
            hasResourceCollector: false,
            hasStructureBuilder: true,
            stance: null,
            anySelected: true,
            formation: null,
            showFormation: false);

        hud.Draw(inner);

        Assert.True(hud.ShowBuilderShortcutHint);
        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Press B", StringComparison.OrdinalIgnoreCase)
            && draw.Text.Contains("build menu", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(inner.TextDraws, draw =>
            draw.Text.Contains("select builder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GameplayHUD_draws_build_flow_hint_when_enabled()
    {
        var inner = new GameplayHudRecordingRenderer();
        var hud = new GameplayHUD { Visible = true, ShowBuildFlowHint = true };

        hud.Draw(inner);

        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Tip:", StringComparison.OrdinalIgnoreCase)
            && draw.Text.Contains("Build (B)", StringComparison.Ordinal));
    }

    [Fact]
    public void GameplayHUD_draws_first_mission_onboarding_hint_when_enabled()
    {
        var inner = new GameplayHudRecordingRenderer();
        var hud = new GameplayHUD { Visible = true, ShowFirstMissionOnboardingHint = true };

        hud.Draw(inner);

        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Training mission", StringComparison.OrdinalIgnoreCase)
            && draw.Text.Contains("objectives", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DismissFirstMissionOnboardingHint_hides_banner()
    {
        var hud = new GameplayHUD { Visible = true, ShowFirstMissionOnboardingHint = true };
        hud.DismissFirstMissionOnboardingHint();
        Assert.False(hud.ShowFirstMissionOnboardingHint);
    }

    [Fact]
    public void Build_map_close_button_routes_through_hud()
    {
        var hud = new GameplayHUD();
        hud.BuildMapPanel.Visible = true;
        hud.BuildMapPanel.Categories =
        [
            new SharpOpenGl.Engine.Build.BuildMapCategoryView
            {
                Id = "defense",
                DisplayName = "Defense",
                Buildings = [],
            },
        ];

        var (pos, size) = hud.BuildMapPanel.Resolve(Vector2.Zero, ReferenceViewport);
        bool closed = false;
        hud.BuildMapPanel.CloseRequested += () => closed = true;

        float closeCenterX = pos.X + size.X - 10f - 24f;
        float aboveVisualY = pos.Y + 4f;
        bool consumed = hud.HandlePointerTapped(
            new Vector2(closeCenterX, aboveVisualY), 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.True(closed);
    }

    private sealed class GameplayHudRecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}