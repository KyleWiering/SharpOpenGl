using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class PauseScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

    [Fact]
    public void Pause_screen_icon_buttons_use_expected_nav_glyphs()
    {
        var screen = new PauseScreen();

        AssertIconButton(screen, "Resume", MenuIconKind.NavResume, "Resume");
        AssertIconButton(screen, "SaveGame", MenuIconKind.NavSave, "Save Game");
        AssertIconButton(screen, "LoadGame", MenuIconKind.NavLoadGame, "Load Game");
        AssertIconButton(screen, "Settings", MenuIconKind.NavSettings, "Settings");
        AssertIconButton(screen, "QuitToMenu", MenuIconKind.NavQuit, "Quit to Menu");
    }

    [Fact]
    public void Pause_resume_save_settings_and_quit_handlers_fire()
    {
        var screen = new PauseScreen();
        bool resume = false;
        bool save = false;
        bool load = false;
        bool settings = false;
        bool quit = false;

        screen.ResumeRequested += () => resume = true;
        screen.SaveGameRequested += () => save = true;
        screen.LoadGameRequested += () => load = true;
        screen.SettingsRequested += () => settings = true;
        screen.QuitToMenuRequested += () => quit = true;

        screen.FindButton("Resume")!.Activate();
        screen.FindButton("SaveGame")!.Activate();
        screen.FindButton("LoadGame")!.Activate();
        screen.FindButton("Settings")!.Activate();
        screen.FindButton("QuitToMenu")!.Activate();

        Assert.True(resume);
        Assert.True(save);
        Assert.True(load);
        Assert.True(settings);
        Assert.True(quit);
    }

    [Fact]
    public void Pause_esc_resume_cycle_pops_overlay_from_ui_stack()
    {
        var mgr = new UIManager(new EventBus());
        var baseScreen = new FakeGameplayScreen();
        var pause = new PauseScreen();
        pause.ResumeRequested += () => mgr.Pop();

        mgr.Push(baseScreen);
        mgr.Push(pause);

        Assert.Equal(2, mgr.ScreenCount);
        Assert.Same(pause, mgr.Current);

        pause.FindButton("Resume")!.Activate();

        Assert.Equal(1, mgr.ScreenCount);
        Assert.Same(baseScreen, mgr.Current);
    }

    [Fact]
    public void Pause_overlay_keeps_gameplay_screen_visible_in_stack()
    {
        var mgr = new UIManager(new EventBus());
        var baseScreen = new FakeGameplayScreen();
        mgr.Push(baseScreen);
        mgr.Push(new PauseScreen());

        mgr.Update(0.016f);
        Assert.Equal(2, mgr.ScreenCount);
    }

    [Fact]
    public void Pause_overlay_card_uses_menu_theme_panel_colours()
    {
        var screen = new PauseScreen();
        var card = Assert.IsType<Panel>(FindWidget(screen, "PauseCard"));

        Assert.Equal(MenuTheme.PanelBackground, card.BackgroundColor);
        Assert.Equal(MenuTheme.PanelBorder, card.BorderColor);
        Assert.True(card.DrawBorder);
    }

    [Fact]
    public void Pause_overlay_backdrop_uses_menu_theme_scrim_colour()
    {
        var screen = new PauseScreen();
        var backdrop = Assert.IsType<Panel>(FindWidget(screen, "Backdrop"));

        Assert.Equal(MenuTheme.OverlayBackdrop, backdrop.BackgroundColor);
        Assert.False(backdrop.DrawBorder);
    }

    [Fact]
    public void Pause_load_game_button_disabled_when_no_saves_exist()
    {
        var screen = new PauseScreen(hasSave: false);
        var load = Assert.IsType<IconButton>(screen.FindButton("LoadGame"));

        Assert.False(load.IsEnabled);
    }

    [Fact]
    public void Pause_screen_draw_emits_nav_icon_glyph_rects()
    {
        var screen = new PauseScreen();
        var renderer = new RecordingUIRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "Resume");
        Assert.Contains(renderer.Texts, t => t == "Save Game");
        Assert.Contains(renderer.Texts, t => t == "Load Game");
        Assert.Contains(renderer.Texts, t => t == "Paused");
        Assert.True(renderer.Rects.Count >= 20,
            "Pause menu should emit button chrome and nav icon glyph rects.");
    }

    private static Widget? FindWidget(UIScreen screen, string name)
    {
        foreach (Widget root in GetRoots(screen))
        {
            Widget? match = FindWidgetInTree(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Widget? FindWidgetInTree(Widget widget, string name)
    {
        if (widget.Name == name)
            return widget;

        foreach (Widget child in widget.Children)
        {
            Widget? match = FindWidgetInTree(child, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static IEnumerable<Widget> GetRoots(UIScreen screen)
    {
        var field = typeof(UIScreen).GetProperty(
            "Roots",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (IReadOnlyList<Widget>)field!.GetValue(screen)!;
    }

    private static void AssertIconButton(PauseScreen screen, string name, MenuIconKind icon, string label)
    {
        var button = Assert.IsType<IconButton>(screen.FindButton(name));
        Assert.Equal(icon, button.Icon);
        Assert.Equal(label, button.Label);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, button.Layout);
        Assert.Equal(IconButton.TitleNavIconSize, button.IconSize);
    }

    private sealed class FakeGameplayScreen : UIScreen
    {
        public override string ScreenName => "Gameplay";
        public override bool IsOverlay => false;
    }

    private sealed class RecordingUIRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<string> Texts { get; } = new();
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Rects { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            Texts.Add(text);
    }
}