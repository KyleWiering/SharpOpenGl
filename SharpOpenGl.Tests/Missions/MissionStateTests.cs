using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>Unit tests for <see cref="MissionState"/>.</summary>
public class MissionStateTests
{
    private static MissionDefinition MakeDefinition(
        string id = "test_mission",
        ObjectiveDefinition[]? primary = null,
        ObjectiveDefinition[]? secondary = null,
        TriggerDefinition[]? triggers = null) => new()
    {
        Id          = id,
        DisplayName = "Test Mission",
        Map         = "test_map",
        Objectives  = new ObjectivesDefinition
        {
            Primary   = primary   ?? [],
            Secondary = secondary ?? [],
        },
        Triggers = triggers ?? [],
        Victory  = new EndConditionDefinition { Type = "all_primary_complete" },
        Defeat   = new EndConditionDefinition { Type = "hero_destroyed" },
    };

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void State_starts_in_None_phase()
    {
        var state = new MissionState(MakeDefinition());
        Assert.Equal(MissionPhase.None, state.Phase);
    }

    [Fact]
    public void State_creates_objective_progress_from_definition()
    {
        var def = MakeDefinition(
            primary: [new ObjectiveDefinition { Id = "p1", Type = "destroy_target" }],
            secondary: [new ObjectiveDefinition { Id = "s1", Type = "survive_time" }]);

        var state = new MissionState(def);

        Assert.Single(state.PrimaryObjectives);
        Assert.Single(state.SecondaryObjectives);
        Assert.Equal("p1", state.PrimaryObjectives[0].Id);
        Assert.Equal("s1", state.SecondaryObjectives[0].Id);
        Assert.True(state.PrimaryObjectives[0].IsPrimary);
        Assert.False(state.SecondaryObjectives[0].IsPrimary);
    }

    [Fact]
    public void State_creates_trigger_progress_from_definition()
    {
        var def = MakeDefinition(triggers:
        [
            new TriggerDefinition
            {
                Id        = "t1",
                Condition = new TriggerConditionDefinition { Type = "timer", Seconds = 10f },
                Actions   = [],
                OneShot   = true,
            }
        ]);

        var state = new MissionState(def);

        Assert.Single(state.Triggers);
        Assert.Equal("t1", state.Triggers[0].Definition.Id);
        Assert.False(state.Triggers[0].HasFired);
    }

    // ── AllPrimaryComplete ────────────────────────────────────────────────────

    [Fact]
    public void AllPrimaryComplete_false_when_no_objectives_completed()
    {
        var def = MakeDefinition(
            primary: [new ObjectiveDefinition { Id = "p1", Type = "destroy_target" }]);

        var state = new MissionState(def);
        Assert.False(state.AllPrimaryComplete);
    }

    [Fact]
    public void AllPrimaryComplete_true_when_all_primary_completed()
    {
        var def = MakeDefinition(
            primary:
            [
                new ObjectiveDefinition { Id = "p1", Type = "destroy_target" },
                new ObjectiveDefinition { Id = "p2", Type = "destroy_target" },
            ]);

        var state = new MissionState(def);
        state.PrimaryObjectives[0].IsCompleted = true;
        state.PrimaryObjectives[1].IsCompleted = true;

        Assert.True(state.AllPrimaryComplete);
    }

    [Fact]
    public void AllPrimaryComplete_false_with_empty_primary_list()
    {
        var state = new MissionState(MakeDefinition());
        Assert.False(state.AllPrimaryComplete);
    }

    // ── FindObjective / FindTrigger ───────────────────────────────────────────

    [Fact]
    public void FindObjective_returns_matching_objective()
    {
        var def = MakeDefinition(
            primary: [new ObjectiveDefinition { Id = "target_obj" }]);

        var state = new MissionState(def);
        var found = state.FindObjective("target_obj");

        Assert.NotNull(found);
        Assert.Equal("target_obj", found!.Id);
    }

    [Fact]
    public void FindObjective_returns_null_for_missing_id()
    {
        var state = new MissionState(MakeDefinition());
        Assert.Null(state.FindObjective("nonexistent"));
    }

    [Fact]
    public void FindTrigger_returns_matching_trigger()
    {
        var def = MakeDefinition(triggers:
        [
            new TriggerDefinition { Id = "my_trigger", Actions = [] }
        ]);

        var state = new MissionState(def);
        var found = state.FindTrigger("my_trigger");

        Assert.NotNull(found);
        Assert.Equal("my_trigger", found!.Definition.Id);
    }

    // ── ElapsedTime ───────────────────────────────────────────────────────────

    [Fact]
    public void ElapsedTime_starts_at_zero()
    {
        var state = new MissionState(MakeDefinition());
        Assert.Equal(0f, state.ElapsedTime);
    }

    [Fact]
    public void ElapsedTime_can_be_set()
    {
        var state = new MissionState(MakeDefinition()) { ElapsedTime = 42f };
        Assert.Equal(42f, state.ElapsedTime);
    }

    // ── EntityTags ────────────────────────────────────────────────────────────

    [Fact]
    public void EntityTags_starts_empty()
    {
        var state = new MissionState(MakeDefinition());
        Assert.Empty(state.EntityTags);
    }

    [Fact]
    public void EntityTags_can_be_populated()
    {
        var state = new MissionState(MakeDefinition());
        var world = new World();
        state.EntityTags["enemy_scout_1"] = world.CreateEntity();
        Assert.Single(state.EntityTags);
    }
}
