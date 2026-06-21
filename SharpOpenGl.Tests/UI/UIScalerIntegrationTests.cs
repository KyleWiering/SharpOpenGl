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

        // "New Game" is the first button at reference 1920×1080 → centre ≈ (960, 330).
        var scaler = new UIScaler(PhysicalViewport);
        Vector2 logicalCenter = new(960f, 330f);
        Vector2 physicalTap = scaler.ScalePosition(logicalCenter);

        bool consumed = mgr.HandlePointerTapped(physicalTap, 0, PhysicalViewport);

        Assert.True(consumed);
        Assert.True(clicked);
    }

    [Fact]
    public void UIManager_updates_main_menu_hover_state_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        var menu = new MainMenuScreen();
        mgr.Push(menu);

        var scaler = new UIScaler(PhysicalViewport);
        Vector2 logicalCenter = new(960f, 330f);
        Vector2 physicalPoint = scaler.ScalePosition(logicalCenter);

        mgr.HandlePointerMove(physicalPoint, false, PhysicalViewport);

        Button? newGame = menu.FindButton("NewGame");
        Assert.NotNull(newGame);
        Assert.True(newGame.IsHovered);
        Assert.Equal(newGame, mgr.FindHoveredButton());
    }

    [Fact]
    public void Main_menu_Continue_disabled_without_save_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        var menu = new MainMenuScreen(hasSave: false);
        bool continued = false;
        menu.ContinueRequested += () => continued = true;
        mgr.Push(menu);

        Button? continueBtn = menu.FindButton("Continue");
        Assert.NotNull(continueBtn);
        Assert.False(continueBtn.IsEnabled);
        Assert.False(menu.HasSave);

        var scaler = new UIScaler(PhysicalViewport);
        // Continue is the third button: centre ≈ (960, 486).
        Vector2 physicalTap = scaler.ScalePosition(new Vector2(960f, 486f));
        bool consumed = mgr.HandlePointerTapped(physicalTap, 0, PhysicalViewport);

        Assert.False(consumed);
        Assert.False(continued);
    }

    [Fact]
    public void Main_menu_Continue_enabled_with_save_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        var menu = new MainMenuScreen(hasSave: true);
        bool continued = false;
        menu.ContinueRequested += () => continued = true;
        mgr.Push(menu);

        Button? continueBtn = menu.FindButton("Continue");
        Assert.NotNull(continueBtn);
        Assert.True(continueBtn.IsEnabled);

        var scaler = new UIScaler(PhysicalViewport);
        Vector2 physicalTap = scaler.ScalePosition(new Vector2(960f, 486f));
        bool consumed = mgr.HandlePointerTapped(physicalTap, 0, PhysicalViewport);

        Assert.True(consumed);
        Assert.True(continued);
    }

    [Fact]
    public void Main_menu_keyboard_down_and_enter_activate_button()
    {
        var mgr = new UIManager(new EventBus());
        var menu = new MainMenuScreen();
        bool multiplayer = false;
        menu.MultiplayerRequested += () => multiplayer = true;
        mgr.Push(menu);

        Assert.True(mgr.HandleKey(UIKey.Down));
        Button? multiplayerBtn = menu.FindButton("Multiplayer");
        Assert.NotNull(multiplayerBtn);
        Assert.True(multiplayerBtn.IsKeyboardFocused);

        Assert.True(mgr.HandleKey(UIKey.Enter));
        Assert.True(multiplayer);
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

        // Start Mission: BottomLeft, offset (1380, -80), size 340×60
        // at reference 1920×1080 → centre ≈ (1550, 970).
        var scaler = new UIScaler(PhysicalViewport);
        Vector2 logicalCenter = new(1550f, 970f);
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

        Assert.Equal(53.33f, inner.LastRectPosition.X, 1);
        Assert.Equal(142.22f, inner.LastRectPosition.Y, 1);
        Assert.Equal(26.67f, inner.LastRectSize.X, 1);
        Assert.Equal(21.33f, inner.LastRectSize.Y, 1);
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