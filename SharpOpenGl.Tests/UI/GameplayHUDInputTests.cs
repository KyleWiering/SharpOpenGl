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
    public void Pause_button_consumes_click()
    {
        var hud = new GameplayHUD();
        bool paused = false;
        hud.PauseRequested += () => paused = true;
        // Pause button: TopRight (-72, 8), size 56×40 → centre ≈ (1820, 28).
        var tap = new Vector2(1820f, 28f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);
        Assert.True(consumed);
        Assert.True(paused);
    }
}