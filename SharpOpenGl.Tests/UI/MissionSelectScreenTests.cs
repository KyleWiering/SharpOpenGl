using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MissionSelectScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 StarMapOrigin = new(40f, 110f);
    private static readonly Vector2 StarMapSize = new(1320f, 860f);
    private static readonly Vector2 StartButtonCenter = new(1550f, 970f);
    private const float PreviewPanelLeft = 1380f;
    private const float PreviewPadding = 20f;
    private const float PreviewIconColumnLeft = PreviewPanelLeft + PreviewPadding;

    private static Vector2 PlanetCenter(Vector2 normalizedPosition) =>
        StarMapOrigin + new Vector2(
            normalizedPosition.X * StarMapSize.X,
            normalizedPosition.Y * StarMapSize.Y);

    [Fact]
    public void SetMissions_auto_selects_first_unlocked_mission_and_enables_start()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            Entry("a", "Alpha", new Vector2(0.2f, 0.5f)),
            Entry("b", "Beta", new Vector2(0.7f, 0.5f)),
        ]);

        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;

        bool consumed = screen.HandlePointerTapped(StartButtonCenter, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal("a", startedId);
    }

    [Fact]
    public void SetMissions_empty_list_keeps_start_disabled()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions([]);

        bool started = false;
        screen.MissionStartRequested += _ => started = true;

        screen.HandlePointerTapped(StartButtonCenter, 0, ReferenceViewport);

        Assert.False(started);
    }

    [Fact]
    public void Selecting_second_planet_starts_that_mission()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            Entry("a", "Alpha", new Vector2(0.2f, 0.5f)),
            Entry("b", "Beta", new Vector2(0.7f, 0.5f)),
        ]);

        Vector2 betaCenter = PlanetCenter(new Vector2(0.7f, 0.5f));
        screen.HandlePointerTapped(betaCenter, 0, ReferenceViewport);

        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;
        screen.HandlePointerTapped(StartButtonCenter, 0, ReferenceViewport);

        Assert.Equal("b", startedId);
    }

    [Fact]
    public void Locked_planet_cannot_be_selected()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            Entry("a", "Alpha", new Vector2(0.2f, 0.5f)),
            Entry("b", "Beta", new Vector2(0.7f, 0.5f), prerequisiteId: "a"),
        ]);

        Vector2 betaCenter = PlanetCenter(new Vector2(0.7f, 0.5f));
        screen.HandlePointerTapped(betaCenter, 0, ReferenceViewport);

        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;
        screen.HandlePointerTapped(StartButtonCenter, 0, ReferenceViewport);

        Assert.Equal("a", startedId);
    }

    [Fact]
    public void Double_click_unlocked_planet_starts_mission()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions([Entry("a", "Alpha", new Vector2(0.2f, 0.5f))]);

        Vector2 alphaCenter = PlanetCenter(new Vector2(0.2f, 0.5f));
        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;

        screen.HandlePointerTapped(alphaCenter, 0, ReferenceViewport);
        screen.Update(0.1f);
        screen.HandlePointerTapped(alphaCenter, 0, ReferenceViewport);

        Assert.Equal("a", startedId);
    }

    [Fact]
    public void MissionSelect_start_and_back_use_icon_buttons()
    {
        var screen = new MissionSelectScreen();

        var start = Assert.IsType<IconButton>(screen.FindButton("StartMission"));
        var back = Assert.IsType<IconButton>(screen.FindButton("Back"));

        Assert.Equal(MenuIconKind.NavStartMission, start.Icon);
        Assert.Equal(MenuIconKind.NavBack, back.Icon);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, start.Layout);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, back.Layout);
    }

    [Fact]
    public void MissionSelect_preview_empty_state_draws_briefing_glyph()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions([]);

        var renderer = new RecordingUIRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t.Contains("Select an unlocked system", StringComparison.Ordinal));
        Assert.True(CountPreviewIconColumnRects(renderer) >= 3,
            "Empty preview should draw briefing glyph rects in the icon column.");
    }

    [Fact]
    public void MissionSelect_preview_objectives_header_has_glyph()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            new MissionEntry
            {
                Id = "tutorial_01",
                Title = "First Contact",
                Description = "Short summary.",
                BriefingText = "Commander, hostiles detected.",
                ObjectivesPreview = ["Destroy the scout"],
                PlanetName = "Helios Prime",
                StarMapPosition = new Vector2(0.2f, 0.5f),
            },
        ]);

        var renderer = new RecordingUIRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "OBJECTIVES");
        var objectivesHeader = renderer.TextDraws.First(d => d.Text == "OBJECTIVES");
        int nearbyIconRects = renderer.Rects.Count(rect =>
            rect.Position.X >= PreviewIconColumnLeft - 1f
            && rect.Position.X < PreviewIconColumnLeft + 28f
            && MathF.Abs(rect.Position.Y - objectivesHeader.Position.Y) < 40f);

        Assert.True(nearbyIconRects >= 3,
            "Objectives header should include icon-slot draws beside the label.");
    }

    [Fact]
    public void SetMissions_populates_preview_with_briefing_and_objectives()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            new MissionEntry
            {
                Id = "tutorial_01",
                Title = "First Contact",
                Description = "Short summary.",
                MapId = "sector_alpha",
                BriefingText = "Commander, hostiles detected in Sector Alpha.",
                ObjectivesPreview = ["Destroy the scout", "Protect your base"],
                PlanetName = "Helios Prime",
                StarMapPosition = new Vector2(0.2f, 0.5f),
            },
        ]);

        var renderer = new RecordingRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "First Contact");
        Assert.Contains(renderer.Texts, t => t == "System: Helios Prime");
        Assert.Contains(renderer.Texts, t => t == "Map: sector alpha");
        Assert.Contains(renderer.Texts, t => t.Contains("hostiles detected"));
        Assert.Contains(renderer.Texts, t => t == "• Destroy the scout");
        Assert.Contains(renderer.Texts, t => t == "• Protect your base");
    }

    [Fact]
    public void Completed_mission_shows_victory_marker_on_star_map()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            Entry("a", "Alpha", new Vector2(0.2f, 0.5f), completed: true),
        ],
            new HashSet<string> { "a" });

        var renderer = new RecordingRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "✓");
        Assert.Contains(renderer.Texts, t => t == "Mission completed");
    }

    [Fact]
    public void Locked_planet_draws_lock_badge_on_star_map()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            Entry("a", "Alpha", new Vector2(0.2f, 0.5f)),
            Entry("b", "Beta", new Vector2(0.7f, 0.5f), prerequisiteId: "a"),
        ]);

        var renderer = new RecordingUIRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "LOCK");
        Assert.Contains(renderer.Texts, t => t == "Beta");
        Assert.DoesNotContain(renderer.Texts, t => t.Contains("[LOCKED]", StringComparison.Ordinal));
        Vector4 lockedBodyColor = new Vector4(0.3f, 0.65f, 1f, 1f) * MenuTheme.StarMapLockedBodyTint;
        Assert.True(CountRectsNearPlanet(renderer, new Vector2(0.7f, 0.5f), lockedBodyColor) > 20,
            "Locked planet should render desaturated body pixels.");
    }

    [Fact]
    public void Selected_planet_draws_selection_ring_on_star_map()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            Entry("a", "Alpha", new Vector2(0.2f, 0.5f)),
            Entry("b", "Beta", new Vector2(0.7f, 0.5f)),
        ]);

        var renderer = new RecordingUIRenderer();
        screen.Draw(renderer);

        Vector2 alphaCenter = PlanetCenter(new Vector2(0.2f, 0.5f));
        int selectionRingRects = renderer.Rects.Count(rect =>
            ColorsCloseRgb(rect.Color, MenuTheme.StarMapSelectionRingColor, 0.12f)
            && rect.Color.W > 0.15f
            && MathF.Abs(rect.Position.X - alphaCenter.X) < 40f
            && MathF.Abs(rect.Position.Y - alphaCenter.Y) < 40f);

        Assert.True(selectionRingRects >= 8,
            "Selected mission should draw bright selection ring dots around the planet.");
        var alphaLabel = renderer.TextDraws.First(d => d.Text == "Alpha");
        Assert.True(ColorsClose(alphaLabel.Color, MenuTheme.StarMapSelectionRingColor, 0.05f),
            "Selected planet label should use the selection accent color.");
    }

    [Fact]
    public void Planet_labels_draw_scrim_backdrop_on_star_map()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions([Entry("a", "Alpha", new Vector2(0.2f, 0.5f))]);

        var renderer = new RecordingUIRenderer();
        screen.Draw(renderer);

        Vector2 alphaCenter = PlanetCenter(new Vector2(0.2f, 0.5f));
        bool hasScrim = renderer.Rects.Any(rect =>
            ColorsClose(rect.Color, MenuTheme.StarMapLabelScrimColor, 0.05f)
            && rect.Position.Y > alphaCenter.Y + 20f
            && rect.Size.X >= 40f);

        Assert.True(hasScrim, "Planet labels should sit on a semi-opaque scrim for starfield contrast.");
    }

    private static int CountPreviewIconColumnRects(RecordingUIRenderer renderer) =>
        renderer.Rects.Count(rect =>
            rect.Position.X >= PreviewIconColumnLeft - 1f
            && rect.Position.X < PreviewIconColumnLeft + 30f);

    private static int CountRectsNearPlanet(RecordingUIRenderer renderer, Vector2 normalizedPosition, Vector4 color)
    {
        Vector2 center = PlanetCenter(normalizedPosition);
        return renderer.Rects.Count(rect =>
            ColorsClose(rect.Color, color, 0.08f)
            && MathF.Abs(rect.Position.X - center.X) < 30f
            && MathF.Abs(rect.Position.Y - center.Y) < 30f);
    }

    private static bool ColorsClose(Vector4 actual, Vector4 expected, float tolerance) =>
        ColorsCloseRgb(actual, expected, tolerance)
        && MathF.Abs(actual.W - expected.W) <= tolerance;

    private static bool ColorsCloseRgb(Vector4 actual, Vector4 expected, float tolerance) =>
        MathF.Abs(actual.X - expected.X) <= tolerance
        && MathF.Abs(actual.Y - expected.Y) <= tolerance
        && MathF.Abs(actual.Z - expected.Z) <= tolerance;

    private static MissionEntry Entry(
        string id,
        string title,
        Vector2 position,
        string? prerequisiteId = null,
        bool completed = false) =>
        new()
        {
            Id = id,
            Title = title,
            Description = $"{title} description.",
            PlanetName = title,
            StarMapPosition = position,
            PrerequisiteMissionId = prerequisiteId,
            IsCompleted = completed,
        };

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<string> Texts { get; } = new();
        public List<(string Text, float FontSize, Vector2 Position, Vector4 Color)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
        {
            Texts.Add(text);
            TextDraws.Add((text, fontSize, position, color));
        }
    }

    private sealed class RecordingUIRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<string> Texts { get; } = new();
        public List<(string Text, float FontSize, Vector2 Position, Vector4 Color)> TextDraws { get; } = new();
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Rects { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
        {
            Texts.Add(text);
            TextDraws.Add((text, fontSize, position, color));
        }
    }
}