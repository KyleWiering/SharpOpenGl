using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MissionVictoryScreenTests
{
    [Fact]
    public void Victory_screen_shows_mission_name_elapsed_time_and_xp()
    {
        var definition = MakeDefinition(
            displayName: "Training Alpha",
            xp: 250,
            primary:
            [
                new ObjectiveDefinition
                {
                    Id = "obj_1",
                    Type = "destroy_target",
                    Description = "Destroy the scout",
                },
            ]);

        var state = new MissionState(definition)
        {
            ElapsedTime = 83.4f,
            Phase = MissionPhase.Victory,
        };
        state.PrimaryObjectives[0].IsCompleted = true;

        var screen = new MissionVictoryScreen();
        screen.SetMissionResult(state, isVictory: true);

        var renderer = new RecordingRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "Training Alpha");
        Assert.Contains(renderer.Texts, t => t == "Time: 1:23");
        Assert.Contains(renderer.Texts, t => t == "XP Earned: 250");
    }

    [Fact]
    public void Victory_screen_lists_primary_objectives_with_completion_markers()
    {
        var definition = MakeDefinition(primary:
        [
            new ObjectiveDefinition
            {
                Id = "done_obj",
                Type = "destroy_target",
                Description = "Eliminate hostiles",
            },
            new ObjectiveDefinition
            {
                Id = "pending_obj",
                Type = "survive_time",
                Description = "Protect the base",
            },
        ]);

        var state = new MissionState(definition);
        state.PrimaryObjectives[0].IsCompleted = true;

        var screen = new MissionVictoryScreen();
        screen.SetMissionResult(state, isVictory: true);

        var renderer = new RecordingRenderer();
        screen.Draw(renderer);

        Assert.Contains(renderer.Texts, t => t == "✓ Eliminate hostiles");
        Assert.Contains(renderer.Texts, t => t == "— Protect the base");
    }

    [Fact]
    public void Return_to_menu_button_fires_event()
    {
        var screen = new MissionVictoryScreen();
        screen.SetMissionResult(new MissionState(MakeDefinition()), isVictory: true);

        bool requested = false;
        screen.ReturnToMenuRequested += () => requested = true;

        Button? button = screen.FindButton("ReturnToMenu");
        Assert.NotNull(button);
        button!.Activate();

        Assert.True(requested);
    }

    [Fact]
    public void Replay_mission_button_fires_event()
    {
        var screen = new MissionVictoryScreen();
        screen.SetMissionResult(new MissionState(MakeDefinition()), isVictory: true);

        bool requested = false;
        screen.ReplayMissionRequested += () => requested = true;

        Button? button = screen.FindButton("ReplayMission");
        Assert.NotNull(button);
        button!.Activate();

        Assert.True(requested);
    }

    [Fact]
    public void FormatElapsedTime_uses_seconds_below_one_minute()
    {
        Assert.Equal("42.5 s", MissionVictoryScreen.FormatElapsedTime(42.5f));
    }

    private static MissionDefinition MakeDefinition(
        string displayName = "Test Mission",
        int xp = 100,
        ObjectiveDefinition[]? primary = null) => new()
    {
        Id = "test_mission",
        DisplayName = displayName,
        Map = "sector_alpha",
        Objectives = new ObjectivesDefinition
        {
            Primary = primary ??
            [
                new ObjectiveDefinition
                {
                    Id = "obj_default",
                    Type = "destroy_target",
                    Description = "Complete the objective",
                },
            ],
        },
        Rewards = new RewardsDefinition { Xp = xp },
        Victory = new EndConditionDefinition { Type = "all_primary_complete" },
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