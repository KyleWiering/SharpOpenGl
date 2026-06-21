using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MissionSelectScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 StarMapOrigin = new(40f, 110f);
    private static readonly Vector2 StarMapSize = new(1320f, 860f);
    private static readonly Vector2 StartButtonCenter = new(1550f, 970f);

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
        Assert.Contains(renderer.Texts, t => t == "✓ Mission completed");
    }

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

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
            => Texts.Add(text);
    }
}