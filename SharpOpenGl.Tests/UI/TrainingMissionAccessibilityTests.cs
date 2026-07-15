using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.UI;

/// <summary>
/// Verifies all five training missions are accessible on the mission-select star map
/// with no prerequisite completions (prerequisiteMissionId null in JSON).
/// </summary>
public class TrainingMissionAccessibilityTests
{
    private static readonly string[] TrainingMissionIds =
    [
        "training_01_interceptor",
        "training_02_building",
        "training_03_harvest",
        "training_04_defense",
        "training_05_tech_tree",
    ];

    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 StartButtonCenter = new(1550f, 970f);
    private static readonly Vector2 PreviewPanelScrollPoint = new(1630f, 460f);

    private static string GetTestDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }

    private static MissionDefinition LoadMission(string missionId)
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var mission = loader.Load(missionId);
        Assert.NotNull(mission);
        return mission!;
    }

    [Theory]
    [MemberData(nameof(TrainingMissionIdsData))]
    public void Training_mission_is_unlocked_on_star_map_with_no_completions(string missionId)
    {
        var definition = LoadMission(missionId);
        Assert.Null(definition.PrerequisiteMissionId);

        var entry = MissionEntryMapper.FromDefinition(definition);
        Assert.Null(entry.PrerequisiteMissionId);

        var screen = new MissionSelectScreen();
        screen.SetMissions([entry]);

        var node = screen.GetStarMapNodes().Single(n => n.Id == missionId);
        Assert.True(node.IsUnlocked);
        Assert.False(screen.IsMissionLocked(missionId));

        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;
        bool consumed = screen.HandlePointerTapped(StartButtonCenter, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal(missionId, startedId);
    }

    [Fact]
    public void All_five_training_missions_unlocked_on_star_map_without_prerequisite_locks()
    {
        var loader = new MissionLoader(new AssetManager(GetTestDataPath()));
        var entries = TrainingMissionIds
            .Select(id => MissionEntryMapper.FromDefinition(loader.Load(id)!))
            .ToList();

        Assert.Equal(5, entries.Count);
        Assert.All(entries, e => Assert.Null(e.PrerequisiteMissionId));

        var screen = new MissionSelectScreen();
        screen.SetMissions(entries);

        var nodes = screen.GetStarMapNodes().ToList();
        Assert.Equal(5, nodes.Count);

        foreach (string missionId in TrainingMissionIds)
        {
            var node = nodes.Single(n => n.Id == missionId);
            Assert.True(node.IsUnlocked, $"{missionId} must be unlocked with no completions");
            Assert.False(screen.IsMissionLocked(missionId));
        }

        var positions = entries.Select(e => e.StarMapPosition).ToList();
        Assert.Equal(5, positions.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(TrainingMissionIdsData))]
    public void Training_mission_shows_completion_marker_when_in_completedMissionIds(string missionId)
    {
        var definition = LoadMission(missionId);
        var completedMissionIds = new HashSet<string> { missionId };
        var entry = MissionEntryMapper.FromDefinition(definition, completedMissionIds);

        var screen = new MissionSelectScreen();
        screen.SetMissions([entry], completedMissionIds);

        // Long training briefings overflow the preview ScrollPanel; scroll to the completion footer.
        for (int i = 0; i < 20; i++)
            screen.HandleScroll(PreviewPanelScrollPoint, 1f, ReferenceViewport);

        var renderer = new RecordingRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "✓");
        Assert.Contains(renderer.Texts, t => t == "Mission completed");

        var node = screen.GetStarMapNodes().Single(n => n.Id == missionId);
        Assert.True(node.IsCompleted);
    }

    public static IEnumerable<object[]> TrainingMissionIdsData() =>
        TrainingMissionIds.Select(id => new object[] { id });

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