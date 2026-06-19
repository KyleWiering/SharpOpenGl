using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class UIScalerIntegrationTests
{
    private static readonly Vector2 PhysicalViewport = new(1024f, 768f);

    [Fact]
    public void UIManager_scales_main_menu_New_Game_click_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        var menu = new MainMenuScreen();
        bool clicked = false;
        menu.NewGameRequested += () => clicked = true;
        mgr.Push(menu);

        // "New Game" is the first button: TopCenter, offset (-160, 322), size 320×60
        // at reference 1920×1080 → centre ≈ (800, 352).
        var scaler = new UIScaler(PhysicalViewport);
        Vector2 logicalCenter = new(800f, 350f);
        Vector2 physicalTap = scaler.ScalePosition(logicalCenter);

        bool consumed = mgr.HandlePointerTapped(physicalTap, 0, PhysicalViewport);

        Assert.True(consumed);
        Assert.True(clicked);
    }

    [Fact]
    public void UIManager_scales_mission_select_Start_Mission_click_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            new MissionEntry { Id = "tutorial_01", Title = "Tutorial", Description = "Learn basics." },
        ]);

        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;
        mgr.Push(screen);

        // Start Mission: BottomLeft, offset (480, -80), size 280×56
        // at reference 1920×1080 → centre ≈ (620, 972).
        var scaler = new UIScaler(PhysicalViewport);
        Vector2 logicalCenter = new(620f, 972f);
        Vector2 physicalTap = scaler.ScalePosition(logicalCenter);

        bool consumed = mgr.HandlePointerTapped(physicalTap, 0, PhysicalViewport);

        Assert.True(consumed);
        Assert.Equal("tutorial_01", startedId);
    }

    [Fact]
    public void ScaledUIRenderer_scales_draw_calls_to_physical_pixels()
    {
        var inner = new RecordingRenderer(new Vector2(1024f, 768f));
        var scaler = new UIScaler(PhysicalViewport);
        var scaled = new ScaledUIRenderer(inner, scaler);

        scaled.DrawRect(new Vector2(100f, 200f), new Vector2(50f, 30f), Vector4.One);

        Assert.Equal(new Vector2(53.33f, 142.22f), inner.LastRectPosition, precision: 1);
        Assert.Equal(new Vector2(26.67f, 21.33f), inner.LastRectSize, precision: 1);
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public Vector2 LastRectPosition { get; private set; }
        public Vector2 LastRectSize { get; private set; }

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
        {
            LastRectPosition = position;
            LastRectSize = size;
        }

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}