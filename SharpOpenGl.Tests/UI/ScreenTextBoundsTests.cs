using System.Reflection;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ScreenTextBoundsTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

    private const float MainMenuTitleWidth = 900f;
    private const float MainMenuButtonInner = 400f - 20f;
    private const float MainMenuSubtitleWrap = 900f - 8f;

    private const float MissionPreviewWrap = 500f - 40f;
    private const float MissionHeadingWidth = 900f;
    private const float MissionStartButtonInner = 340f - 20f;
    private const float MissionBackButtonInner = 220f - 20f;

    private const float MpMapLabelWrap = 640f - 20f;
    private const float MpRaceLabelWrap = 300f - 20f;
    private const float MpValidationWrap = 900f - 20f;
    private const float MpStartButtonInner = 340f - 20f;
    private const float MpKindButtonInner = 150f - 20f;

    private const float BriefingWrap = 1920f - 160f - 32f;
    private const float PauseButtonInner = 280f - 20f;
    private const float SettingsButtonInner = 400f - 20f;

    [Fact]
    public void MainMenuScreen_text_fits_label_and_button_bounds_at_1920x1080()
    {
        var screen = new MainMenuScreen(hasSave: true);
        var inner = new RecordingRenderer(ReferenceViewport);

        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.FontSize >= 50f
                ? MainMenuTitleWidth
                : draw.FontSize > 20f
                    ? MainMenuSubtitleWrap
                    : MainMenuButtonInner;
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    [Fact]
    public void MainMenuScreen_long_subtitle_wraps_within_panel_width()
    {
        var screen = new MainMenuScreen();
        var subtitle = FindWidget<Label>(screen, "Subtitle");
        subtitle!.Text =
            "Command the void across multiple sectors with extended fleet operations and strategic deployment";
        subtitle.WrapWidth = UITextDrawing.ContentWrapWidth(900f, 4f);

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        Assert.All(inner.TextDraws.Where(d => d.FontSize < 30f), draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= subtitle.WrapWidth + 1f, draw.Text);
        });
    }

    [Fact]
    public void MissionSelectScreen_long_mission_preview_text_fits_panel_bounds()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            new MissionEntry
            {
                Id = "long_mission",
                Title = "Operation Vanguard: Deep Space Reconnaissance and Extended Territorial Control",
                Description = "A very long summary that should wrap inside the preview panel without overflowing the mission select layout.",
                MapId = "sector_alpha_extended_operations_zone",
                BriefingText =
                    "Commander, hostile forces have established fortified positions across the outer rim. " +
                    "Your orders are to neutralize their command infrastructure and secure all relay stations " +
                    "before the enemy fleet completes its warp assembly sequence.",
                ObjectivesPreview =
                [
                    "Destroy all enemy super-heavy dreadnought production facilities in the outer rim",
                    "Protect the civilian evacuation corridor for at least fifteen minutes under continuous fire",
                ],
                PlanetName = "Helios Prime Extended Colony Zone Alpha Seven",
                StarMapPosition = new Vector2(0.35f, 0.45f),
            },
        ]);

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.FontSize switch
            {
                >= 30f => MissionHeadingWidth,
                >= 18f when draw.Text is "Start Mission" => MissionStartButtonInner,
                >= 18f when draw.Text is "Back" => MissionBackButtonInner,
                >= 16f => MissionPreviewWrap,
                _ => MissionPreviewWrap,
            };
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    [Fact]
    public void MultiplayerSetupScreen_labels_and_buttons_fit_bounds_with_stress_data()
    {
        var screen = new MultiplayerSetupScreen();

        for (int slot = 2; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            screen.CycleSlotKind(slot);
            screen.CycleSlotKind(slot);
        }

        while (screen.GetSelectedMapId() != "duel_frontier")
            screen.CycleMap(1);

        for (int raceStep = 0; raceStep < 12; raceStep++)
            screen.CycleSlotRace(0, 1);

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = ResolveMultiplayerMaxWidth(draw);
            Assert.True(width <= maxWidth + 1f, $"[{draw.Text}] width {width} > {maxWidth}");
        }
    }

    [Fact]
    public void MultiplayerSetupScreen_validation_message_wraps_when_map_too_small()
    {
        var screen = new MultiplayerSetupScreen();
        for (int slot = 2; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            screen.CycleSlotKind(slot);
            screen.CycleSlotKind(slot);
        }

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        var validationLines = inner.TextDraws
            .Where(d => d.FontSize <= 16f && d.Text.Contains("supports up to", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(validationLines);
        Assert.All(validationLines, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= MpValidationWrap + 1f, draw.Text);
        });
    }

    [Fact]
    public void BriefingScreen_long_objectives_fit_content_width()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            DisplayName = "Operation Vanguard: Extended Deep Space Reconnaissance Campaign",
            Description = "Long fallback briefing body for missions without dedicated narrative text blocks.",
            Briefing = new BriefingDefinition
            {
                Text =
                    "Commander, reconnaissance probes report multiple hostile staging areas along the frontier. " +
                    "Establish forward bases, interdict enemy supply lines, and hold the relay corridor until reinforcements arrive.",
                ObjectivesPreview =
                [
                    "Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector",
                    "Protect the civilian evacuation corridor for fifteen minutes under continuous weapons fire",
                ],
            },
        });

        var inner = new RecordingRenderer(ReferenceViewport);
        screen.Draw(inner);

        AssertScreenTextWithinViewport(inner.TextDraws);
        Assert.All(inner.TextDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.Text is "Start Mission" or "Back"
                ? 280f - 20f
                : BriefingWrap;
            Assert.True(width <= maxWidth + 1f, draw.Text);
        });
    }

    [Fact]
    public void SettingsScreen_and_pause_screen_short_labels_fit_buttons()
    {
        string settingsDir = Path.Combine(Path.GetTempPath(), $"sg_screen_bounds_{Guid.NewGuid():N}");
        Directory.CreateDirectory(settingsDir);
        try
        {
            var settings = new SettingsManager(Path.Combine(settingsDir, "settings.json"));
            var settingsScreen = new SettingsScreen(settings);
            var pauseScreen = new PauseScreen();

            var inner = new RecordingRenderer(ReferenceViewport);
            settingsScreen.Draw(inner);
            pauseScreen.Draw(inner);

            Assert.All(inner.TextDraws, draw =>
            {
                float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
                float maxWidth = draw.Text is "Resume" or "Save Game" or "Settings" or "Quit to Menu"
                    ? PauseButtonInner
                    : SettingsButtonInner;
                Assert.True(width <= maxWidth + 1f, draw.Text);
            });
        }
        finally
        {
            if (Directory.Exists(settingsDir))
                Directory.Delete(settingsDir, recursive: true);
        }
    }

    private static float ResolveMultiplayerMaxWidth((string Text, float FontSize, Vector2 Position) draw)
    {
        if (draw.Text is "<" or ">" or "—")
            return 44f;

        if (draw.Text is "Start Match" or "Back")
            return MpStartButtonInner;

        if (draw.Text is "Human" or "AI" or "Empty")
            return MpKindButtonInner;

        if (draw.Text.StartsWith("Map:", StringComparison.Ordinal))
            return MpMapLabelWrap;

        if (draw.Text.StartsWith("Player ", StringComparison.Ordinal))
            return 120f;

        if (draw.Text.StartsWith("Up to ", StringComparison.Ordinal))
            return 1100f - 20f;

        if (draw.FontSize >= 30f)
            return 900f;

        if (draw.FontSize >= 18f)
            return MpRaceLabelWrap;

        if (draw.FontSize <= 16f)
            return MpValidationWrap;

        return MpRaceLabelWrap;
    }

    private static void AssertScreenTextWithinViewport(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws)
    {
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= -1f, draw.Text);
            Assert.True(draw.Position.X + width <= ReferenceViewport.X + 1f, draw.Text);
            Assert.True(draw.Position.Y >= -1f, draw.Text);
            Assert.True(draw.Position.Y + draw.FontSize <= ReferenceViewport.Y + 1f, draw.Text);
        }
    }

    private static TWidget? FindWidget<TWidget>(UIScreen screen, string name)
        where TWidget : Widget
    {
        foreach (Widget root in GetRoots(screen))
        {
            TWidget? match = FindWidgetInTree<TWidget>(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static TWidget? FindWidgetInTree<TWidget>(Widget widget, string name)
        where TWidget : Widget
    {
        if (widget is TWidget match && widget.Name == name)
            return match;

        foreach (Widget child in widget.Children)
        {
            TWidget? childMatch = FindWidgetInTree<TWidget>(child, name);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    private static IEnumerable<Widget> GetRoots(UIScreen screen)
    {
        FieldInfo? field = typeof(UIScreen).GetField("_roots", BindingFlags.Instance | BindingFlags.NonPublic);
        return (IEnumerable<Widget>)field!.GetValue(screen)!;
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}