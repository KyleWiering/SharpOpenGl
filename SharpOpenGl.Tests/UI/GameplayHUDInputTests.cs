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
    public void Clicks_on_minimap_do_not_consume_input()
    {
        var hud = new GameplayHUD();
        var tap = new Vector2(120f, 1000f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);
        Assert.False(consumed);
    }

    [Fact]
    public void Pause_button_consumes_click()
    {
        var hud = new GameplayHUD();
        bool paused = false;
        hud.PauseRequested += () => paused = true;
        var tap = new Vector2(1860f, 28f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);
        Assert.True(consumed);
        Assert.True(paused);
    }
}