using System.Reflection;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class BriefingScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

    [Fact]
    public void Briefing_start_and_back_use_icon_buttons()
    {
        var screen = new BriefingScreen();

        var start = Assert.IsType<IconButton>(screen.FindButton("StartMission"));
        var back = Assert.IsType<IconButton>(screen.FindButton("Back"));

        Assert.Equal(MenuIconKind.NavStartMission, start.Icon);
        Assert.Equal(MenuIconKind.NavBack, back.Icon);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, start.Layout);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, back.Layout);
        Assert.Equal(IconButton.TitleNavIconSize, start.IconSize);
        Assert.Equal(IconButton.TitleNavIconSize, back.IconSize);
    }

    [Fact]
    public void Briefing_back_requested_fires_handler()
    {
        var screen = new BriefingScreen();
        bool backRequested = false;
        screen.BackRequested += () => backRequested = true;

        IUIButton? back = screen.FindButton("Back");
        Assert.NotNull(back);
        back!.Activate();

        Assert.True(backRequested);
    }

    [Fact]
    public void Briefing_start_requested_fires_handler_without_interceptor()
    {
        var screen = new BriefingScreen();
        bool startRequested = false;
        screen.StartRequested += () => startRequested = true;

        IUIButton? start = screen.FindButton("StartMission");
        Assert.NotNull(start);
        start!.Activate();

        Assert.True(startRequested);
    }

    [Fact]
    public void Briefing_set_mission_preserves_context_ids()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            Id = "tutorial_01",
            DisplayName = "First Contact",
            Description = "Hostiles detected.",
            Briefing = new BriefingDefinition
            {
                Text = "Commander, scouts report enemy activity.",
                ObjectivesPreview = ["Destroy the scout"],
            },
        });

        Assert.Equal("tutorial_01", screen.MissionId);
        Assert.Equal("First Contact", screen.MissionDisplayName);
    }

    [Fact]
    public void Briefing_back_button_uses_secondary_nav_hierarchy()
    {
        var screen = new BriefingScreen();
        var start = Assert.IsType<IconButton>(screen.FindButton("StartMission"));
        var back = Assert.IsType<IconButton>(screen.FindButton("Back"));

        Assert.True(start.ShowHoverGlow, "Start Mission should use primary hover glow.");
        Assert.False(back.ShowHoverGlow, "Back should be secondary without hover glow.");
        Assert.Equal(MenuTheme.MutedTextColor, back.TextColor);
        Assert.Equal(MenuTheme.ButtonText, start.TextColor);
    }

    [Fact]
    public void Briefing_long_objectives_wrap_up_to_four_lines_without_ellipsis()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            Id = "stress_wrap",
            DisplayName = "Stress Wrap",
            Description = "Fallback.",
            Briefing = new BriefingDefinition
            {
                Text = "Briefing body.",
                ObjectivesPreview =
                [
                    "Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector while maintaining supply lines",
                ],
            },
        });

        var objective = FindWidget<Label>(screen, "Objective_0");
        Assert.NotNull(objective);
        Assert.Equal(4, objective!.MaxLines);

        float wrapWidth = UITextDrawing.ContentWrapWidth(1920f - 160f, 16f);
        var wrapped = UITextDrawing.WrapTextLimited(
            "• Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector while maintaining supply lines",
            wrapWidth,
            18f,
            maxLines: 4);

        Assert.True(wrapped.Count <= 4);
        Assert.DoesNotContain('…', wrapped[^1]);
    }

    [Fact]
    public void Briefing_nav_buttons_draw_icon_column_glyphs()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            Id = "tutorial_01",
            DisplayName = "First Contact",
            Description = "Hostiles detected.",
            Briefing = new BriefingDefinition
            {
                Text = "Commander, scouts report enemy activity.",
                ObjectivesPreview = ["Destroy the scout"],
            },
        });

        var renderer = new RecordingUIRenderer();
        screen.Draw(renderer);

        AssertIconColumnDraws(renderer, screen.FindButton("StartMission") as IconButton);
        AssertIconColumnDraws(renderer, screen.FindButton("Back") as IconButton);
    }

    private static void AssertIconColumnDraws(RecordingUIRenderer renderer, IconButton? button)
    {
        Assert.NotNull(button);
        var (buttonPos, buttonSize) = button!.Resolve(Vector2.Zero, ReferenceViewport);
        float iconColumnRight = buttonPos.X + IconButton.TitleNavIconColumnWidth;

        int iconColumnRects = renderer.Rects.Count(rect =>
            rect.Position.X >= buttonPos.X - 1f
            && rect.Position.X + rect.Size.X <= iconColumnRight + 2f
            && rect.Position.Y >= buttonPos.Y - 1f
            && rect.Position.Y + rect.Size.Y <= buttonPos.Y + buttonSize.Y + 1f);

        Assert.True(iconColumnRects >= 1, $"[{button.Label}] should draw nav glyph rects in icon column.");
    }

    private static T? FindWidget<T>(UIScreen screen, string name) where T : Widget
    {
        FieldInfo? field = typeof(UIScreen).GetField("_roots", BindingFlags.Instance | BindingFlags.NonPublic);
        var roots = (IReadOnlyList<Widget>)(field?.GetValue(screen) ?? Array.Empty<Widget>());
        foreach (Widget root in roots)
        {
            T? match = FindWidgetInTree<T>(root, name);
            if (match != null)
                return match;
        }
        return null;
    }

    private static T? FindWidgetInTree<T>(Widget widget, string name) where T : Widget
    {
        if (widget.Name == name && widget is T match)
            return match;

        foreach (Widget child in widget.Children)
        {
            T? childMatch = FindWidgetInTree<T>(child, name);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    private sealed class RecordingUIRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Rects { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}