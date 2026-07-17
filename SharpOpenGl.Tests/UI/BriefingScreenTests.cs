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
    private static readonly Vector2 CompactViewport = new(1024f, 768f);

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

        var objectivesScroll = FindWidget<ScrollPanel>(screen, "ObjectivesPreview");
        Assert.NotNull(objectivesScroll);
        float scrollbarGutter = objectivesScroll!.ShowScrollbar ? 10f : 0f;
        float wrapWidth = UITextDrawing.ContentWrapWidth(
            objective!.Size.X - scrollbarGutter,
            objective.Padding);
        var wrapped = UITextDrawing.WrapTextLimited(
            "• Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector while maintaining supply lines",
            wrapWidth,
            18f,
            maxLines: 4);

        Assert.True(wrapped.Count <= 4);
        Assert.DoesNotContain('…', wrapped[^1]);
    }

    [Fact]
    public void Briefing_panel_width_derives_from_viewport_margins()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            Id = "layout",
            DisplayName = "Operation Vanguard: Extended Deep Space Reconnaissance Campaign",
            Description = "Fallback body.",
            Briefing = new BriefingDefinition
            {
                Text = "Commander, review objectives before launch.",
                ObjectivesPreview = ["Secure the relay corridor"],
            },
        });

        var panel = FindWidget<Panel>(screen, "BriefingText");
        var bodyScroll = FindWidget<ScrollPanel>(screen, "BriefingBodyScroll");
        var objectives = FindWidget<ScrollPanel>(screen, "ObjectivesPreview");
        var missionTitle = FindWidget<Label>(screen, "MissionTitle");
        Assert.NotNull(panel);
        Assert.NotNull(bodyScroll);
        Assert.NotNull(objectives);
        Assert.NotNull(missionTitle);

        float expectedPanelW = ReferenceViewport.X - 160f;
        Assert.Equal(expectedPanelW, panel!.Size.X, precision: 1);
        Assert.Equal(expectedPanelW, objectives!.Size.X, precision: 1);
        Assert.Equal(expectedPanelW - 32f, bodyScroll!.Size.X, precision: 1);
        Assert.Equal(expectedPanelW - 32f, missionTitle!.Size.X, precision: 1);

        var (panelPos, panelSize) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        Assert.True(panelPos.X >= 79f);
        Assert.True(panelPos.X + panelSize.X <= ReferenceViewport.X - 79f);
    }

    [Fact]
    public void Briefing_title_chrome_has_viewport_wrap_width()
    {
        var screen = new BriefingScreen();
        var title = FindWidget<Label>(screen, "BriefingTitle");
        var subtitle = FindWidget<Label>(screen, "BriefingSubtitle");
        Assert.NotNull(title);
        Assert.NotNull(subtitle);

        float panelW = ReferenceViewport.X - 160f;
        Assert.True(title!.WrapWidth > 0f);
        Assert.True(subtitle!.WrapWidth > 0f);
        Assert.True(title.Size.X <= 900f);
        Assert.True(title.Size.X <= panelW + 1f);
        Assert.True(subtitle.Size.X <= 700f);
        Assert.True(subtitle.Size.X <= panelW + 1f);
    }

    [Fact]
    public void Briefing_compact_viewport_objectives_relayout_without_overlap()
    {
        const string longObjective =
            "Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector while maintaining supply lines";

        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            DisplayName = "Operation Vanguard: Extended Deep Space Reconnaissance Campaign",
            Description = "Fallback briefing body.",
            Briefing = new BriefingDefinition
            {
                Text = "Commander, review all primary objectives before launch.",
                ObjectivesPreview =
                [
                    longObjective,
                    longObjective,
                    longObjective,
                    longObjective,
                ],
            },
        });

        var inner = new RecordingTextRenderer(CompactViewport);
        var renderer = new ScaledUIRenderer(inner, new UIScaler(CompactViewport));
        screen.Draw(renderer);

        AssertNoHorizontalBleed(inner.TextDraws, CompactViewport);

        var objectivesScroll = FindWidget<ScrollPanel>(screen, "ObjectivesPreview");
        Assert.NotNull(objectivesScroll);
        Assert.True(objectivesScroll!.ContentHeight > objectivesScroll.Size.Y);
        Assert.True(objectivesScroll.MaxScrollOffset(objectivesScroll.Size) > 0f);

        float previousBottom = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            var objective = FindWidget<Label>(screen, $"Objective_{i}");
            Assert.NotNull(objective);
            Assert.True(objective!.Position.Y >= previousBottom - 0.5f,
                $"Objective_{i} should stack below prior rows after compact relayout.");
            previousBottom = objective.Position.Y + objective.Size.Y;
        }
    }

    [Fact]
    public void Briefing_compact_viewport_all_text_avoids_horizontal_bleed()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            Id = "compact_bleed",
            DisplayName = "Operation Vanguard: Extended Deep Space Reconnaissance Campaign",
            Description = "Fallback briefing body.",
            Briefing = new BriefingDefinition
            {
                Text = string.Join(
                    " ",
                    Enumerable.Repeat(
                        "Commander, reconnaissance probes report multiple hostile staging areas along the contested frontier corridor.",
                        40)),
                ObjectivesPreview =
                [
                    "Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector",
                    "Protect the civilian evacuation corridor for fifteen minutes under continuous weapons fire",
                ],
            },
        });

        var inner = new RecordingTextRenderer(CompactViewport);
        var renderer = new ScaledUIRenderer(inner, new UIScaler(CompactViewport));
        screen.Draw(renderer);

        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= -1f, draw.Text);
            Assert.True(draw.Position.X + width <= CompactViewport.X + 1f, draw.Text);
        }
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

    private static void AssertNoHorizontalBleed(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws,
        Vector2 viewport)
    {
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= -1f, draw.Text);
            Assert.True(draw.Position.X + width <= viewport.X + 1f, draw.Text);
        }
    }

    private sealed class RecordingTextRenderer : IUIRenderer
    {
        public RecordingTextRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}