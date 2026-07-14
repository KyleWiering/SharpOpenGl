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

    private static Vector2 MainMenuButtonCenter(int buttonIndex)
    {
        const float btnH = 58f;
        const float gap = 10f;
        float totalH = 8 * btnH + 7 * gap;
        float startY = MathF.Max(248f, (1080f - totalH) * 0.5f);
        float centerY = startY + buttonIndex * (btnH + gap) + btnH * 0.5f;
        return new Vector2(960f, centerY);
    }

    [Fact]
    public void UIManager_scales_main_menu_New_Game_click_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        var menu = new MainMenuScreen();
        bool clicked = false;
        menu.NewGameRequested += () => clicked = true;
        mgr.Push(menu);

        // "New Game" is the first button at reference 1920×1080 → centre ≈ (960, 306).
        var scaler = new UIScaler(PhysicalViewport);
        Vector2 logicalCenter = new(960f, 306f);
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
        Vector2 logicalCenter = new(960f, 306f);
        Vector2 physicalPoint = scaler.ScalePosition(logicalCenter);

        mgr.HandlePointerMove(physicalPoint, false, PhysicalViewport);

        IconButton? newGame = menu.FindNavButton("NewGame");
        Assert.NotNull(newGame);
        Assert.True(newGame.IsHovered);
        Assert.Equal(MenuIconKind.NavNewGame, newGame.Icon);
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

        IconButton? continueBtn = menu.FindNavButton("Continue");
        Assert.NotNull(continueBtn);
        Assert.False(continueBtn.IsEnabled);
        Assert.Equal(MenuIconKind.NavContinue, continueBtn.Icon);
        Assert.False(menu.HasSave);

        var scaler = new UIScaler(PhysicalViewport);
        Vector2 physicalTap = scaler.ScalePosition(MainMenuButtonCenter(buttonIndex: 3));
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

        IconButton? continueBtn = menu.FindNavButton("Continue");
        Assert.NotNull(continueBtn);
        Assert.True(continueBtn.IsEnabled);
        Assert.Equal(MenuIconKind.NavContinue, continueBtn.Icon);

        var scaler = new UIScaler(PhysicalViewport);
        Vector2 physicalTap = scaler.ScalePosition(MainMenuButtonCenter(buttonIndex: 3));
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
        Assert.True(mgr.HandleKey(UIKey.Down));
        IconButton? multiplayerBtn = menu.FindNavButton("Multiplayer");
        Assert.NotNull(multiplayerBtn);
        Assert.True(multiplayerBtn.IsKeyboardFocused);
        Assert.Equal(MenuIconKind.NavMultiplayer, multiplayerBtn.Icon);

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