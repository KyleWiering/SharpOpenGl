using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>
/// Unit tests for MissionLoader, MissionState, ObjectiveSystem,
/// TriggerSystem, MissionRewards, and ScriptedEventRunner.
/// </summary>
public class MissionTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static MissionDefinition MakeDef(string id = "test") =>
        new()
        {
            Id          = id,
            DisplayName = "Test Mission",
            Victory     = new MissionEndCondition { Type = "all_primary_complete" },
            Defeat      = new MissionEndCondition { Type = "hero_destroyed" },
        };

    private static MissionObjectiveDefinition MakeDestroyObj(string id, string target) =>
        new() { Id = id, Type = "destroy_target", Target = target };

    private static MissionObjectiveDefinition MakeSurviveObj(string id, float seconds) =>
        new() { Id = id, Type = "survive_time", Seconds = seconds };

    private static MissionObjectiveDefinition MakeCollectObj(string id, string resource, float amount) =>
        new() { Id = id, Type = "collect", Resource = resource, Amount = amount };

    // ─────────────────────────────────────────────────────────────────────────
    // MissionLoader
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MissionLoader_FromDefinition_creates_objective_records()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeDestroyObj("obj1", "target_a"));
        def.Objectives.Secondary.Add(MakeSurviveObj("obj2", 60f));

        MissionState state = MissionLoader.FromDefinition(def);

        Assert.Equal(2, state.Objectives.Count);
        Assert.True(state.Objectives[0].IsPrimary);
        Assert.False(state.Objectives[1].IsPrimary);
    }

    [Fact]
    public void MissionLoader_FromDefinition_creates_trigger_records()
    {
        var def = MakeDef();
        def.Triggers.Add(new MissionTriggerDefinition
        {
            Id        = "t1",
            Condition = new TriggerConditionDefinition { Type = "timer", Seconds = 10 },
        });

        MissionState state = MissionLoader.FromDefinition(def);
        Assert.Single(state.Triggers);
        Assert.Equal("t1", state.Triggers[0].Definition.Id);
    }

    [Fact]
    public void MissionLoader_initial_status_is_Briefing()
    {
        MissionState state = MissionLoader.FromDefinition(MakeDef());
        Assert.Equal(MissionStatus.Briefing, state.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MissionState lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void StartMission_transitions_to_InProgress()
    {
        MissionState state = MissionLoader.FromDefinition(MakeDef());
        state.StartMission();
        Assert.Equal(MissionStatus.InProgress, state.Status);
    }

    [Fact]
    public void Tick_only_advances_timer_when_InProgress()
    {
        MissionState state = MissionLoader.FromDefinition(MakeDef());
        state.Tick(5f);
        Assert.Equal(0f, state.ElapsedSeconds); // still Briefing

        state.StartMission();
        state.Tick(5f);
        Assert.Equal(5f, state.ElapsedSeconds);
    }

    [Fact]
    public void SetVictory_sets_correct_status()
    {
        MissionState state = MissionLoader.FromDefinition(MakeDef());
        state.StartMission();
        state.SetVictory();
        Assert.Equal(MissionStatus.Victory, state.Status);
    }

    [Fact]
    public void SetDefeat_stores_reason()
    {
        MissionState state = MissionLoader.FromDefinition(MakeDef());
        state.StartMission();
        state.SetDefeat("hero_destroyed");
        Assert.Equal(MissionStatus.Defeat, state.Status);
        Assert.Equal("hero_destroyed", state.DefeatReason);
    }

    [Fact]
    public void AllPrimaryComplete_false_when_objectives_pending()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeDestroyObj("obj1", "target"));
        MissionState state = MissionLoader.FromDefinition(def);
        Assert.False(state.AllPrimaryComplete);
    }

    [Fact]
    public void Reset_restores_initial_state()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeSurviveObj("s1", 10f));
        MissionState state = MissionLoader.FromDefinition(def);
        state.StartMission();
        state.Tick(5f);
        state.Objectives[0].Complete();
        state.SetVictory();

        state.Reset();

        Assert.Equal(MissionStatus.Briefing, state.Status);
        Assert.Equal(0f, state.ElapsedSeconds);
        Assert.False(state.Objectives[0].IsCompleted);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ObjectiveSystem — destroy_target
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ObjectiveSystem_destroy_target_completes_when_entity_gone()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeDestroyObj("kill_scout", "scout"));
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var world  = new World();
        var system = new ObjectiveSystem(state) { IsEntityAlive = _ => false };
        system.Tick(world, 0.016f);

        Assert.True(state.Objectives[0].IsCompleted);
    }

    [Fact]
    public void ObjectiveSystem_destroy_target_not_completed_while_alive()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeDestroyObj("kill_scout", "scout"));
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var world  = new World();
        var system = new ObjectiveSystem(state) { IsEntityAlive = _ => true };
        system.Tick(world, 0.016f);

        Assert.False(state.Objectives[0].IsCompleted);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ObjectiveSystem — survive_time
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ObjectiveSystem_survive_time_completes_after_duration()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeSurviveObj("survive", 3f));
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var world  = new World();
        var system = new ObjectiveSystem(state);

        // 2 seconds — not yet done
        system.Tick(world, 2f);
        Assert.False(state.Objectives[0].IsCompleted);

        // 1.5 more seconds — past threshold
        system.Tick(world, 1.5f);
        Assert.True(state.Objectives[0].IsCompleted);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ObjectiveSystem — collect
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ObjectiveSystem_collect_completes_when_enough_resource()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeCollectObj("gather", "energy", 500f));
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var world  = new World();
        var system = new ObjectiveSystem(state)
        {
            GetResourceAmount = _ => 600f, // already enough
        };

        system.Tick(world, 0.016f);
        Assert.True(state.Objectives[0].IsCompleted);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ObjectiveSystem — victory / defeat events
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ObjectiveSystem_fires_MissionCompletedEvent_on_victory()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeDestroyObj("kill", "target"));
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var bus = new EventBus();
        MissionCompletedEvent? evt = null;
        bus.Subscribe<MissionCompletedEvent>(e => evt = e);

        var world  = new World();
        var system = new ObjectiveSystem(state, bus) { IsEntityAlive = _ => false };
        system.Tick(world, 0.016f);

        Assert.NotNull(evt);
        Assert.Equal("test", evt!.MissionId);
        Assert.Equal(MissionStatus.Victory, state.Status);
    }

    [Fact]
    public void ObjectiveSystem_fires_MissionFailedEvent_when_hero_destroyed()
    {
        var def = MakeDef();
        def.Defeat = new MissionEndCondition { Type = "hero_destroyed" };
        // No primary objectives — so victory never triggers first
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var bus = new EventBus();
        MissionFailedEvent? evt = null;
        bus.Subscribe<MissionFailedEvent>(e => evt = e);

        var world  = new World();
        var system = new ObjectiveSystem(state, bus) { IsEntityAlive = _ => false };
        system.Tick(world, 0.016f);

        Assert.NotNull(evt);
        Assert.Equal(MissionStatus.Defeat, state.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TriggerSystem — timer
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TriggerSystem_timer_fires_after_elapsed_seconds()
    {
        var def = MakeDef();
        def.Triggers.Add(new MissionTriggerDefinition
        {
            Id = "t1",
            Condition = new TriggerConditionDefinition { Type = "timer", Seconds = 5f },
            Actions   = [new TriggerActionDefinition { Type = "dialog", Speaker = "AI", Text = "Hello" }],
            Once      = true,
        });
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var bus = new EventBus();
        TriggerFiredEvent? fired = null;
        bus.Subscribe<TriggerFiredEvent>(e => fired = e);

        var runner = new ScriptedEventRunner(bus);
        var world  = new World();
        var system = new TriggerSystem(state, runner, bus);

        system.Tick(world, 3f);  // 3 s — not yet
        Assert.Null(fired);

        system.Tick(world, 3f);  // 6 s total — fired
        Assert.NotNull(fired);
        Assert.Equal("t1", fired!.TriggerId);
    }

    [Fact]
    public void TriggerSystem_once_trigger_fires_only_once()
    {
        var def = MakeDef();
        def.Triggers.Add(new MissionTriggerDefinition
        {
            Id = "t1",
            Condition = new TriggerConditionDefinition { Type = "timer", Seconds = 1f },
            Actions   = [new TriggerActionDefinition { Type = "dialog", Text = "once" }],
            Once      = true,
        });
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        int fireCount = 0;
        var bus = new EventBus();
        bus.Subscribe<TriggerFiredEvent>(_ => fireCount++);

        var runner = new ScriptedEventRunner(bus);
        var world  = new World();
        var system = new TriggerSystem(state, runner, bus);

        system.Tick(world, 2f); // fires once
        system.Tick(world, 2f); // should not fire again
        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void TriggerSystem_kill_count_trigger_fires_on_threshold()
    {
        var def = MakeDef();
        def.Triggers.Add(new MissionTriggerDefinition
        {
            Id = "k1",
            Condition = new TriggerConditionDefinition { Type = "kill_count", Count = 3 },
            Actions   = [],
            Once      = true,
        });
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        int fireCount = 0;
        var bus = new EventBus();
        bus.Subscribe<TriggerFiredEvent>(_ => fireCount++);

        var runner = new ScriptedEventRunner(bus);
        var world  = new World();
        var system = new TriggerSystem(state, runner, bus);

        state.RecordKill();
        state.RecordKill();
        system.Tick(world, 0.016f); // 2 kills — not yet

        state.RecordKill();
        system.Tick(world, 0.016f); // 3 kills — fires

        Assert.Equal(1, fireCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TriggerSystem — scripted actions
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TriggerSystem_dialog_action_publishes_DialogEvent()
    {
        var def = MakeDef();
        def.Triggers.Add(new MissionTriggerDefinition
        {
            Id = "d1",
            Condition = new TriggerConditionDefinition { Type = "timer", Seconds = 0f },
            Actions   = [new TriggerActionDefinition { Type = "dialog", Speaker = "Hero", Text = "Move out!" }],
            Once      = true,
        });
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var bus = new EventBus();
        DialogEvent? dlg = null;
        bus.Subscribe<DialogEvent>(e => dlg = e);

        var runner = new ScriptedEventRunner(bus);
        var system = new TriggerSystem(state, runner, bus);
        system.Tick(new World(), 0.1f);

        Assert.NotNull(dlg);
        Assert.Equal("Hero",    dlg!.Speaker);
        Assert.Equal("Move out!", dlg.Text);
    }

    [Fact]
    public void TriggerSystem_complete_objective_action_marks_objective_done()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeDestroyObj("obj1", "target"));
        def.Triggers.Add(new MissionTriggerDefinition
        {
            Id = "force",
            Condition = new TriggerConditionDefinition { Type = "timer", Seconds = 0f },
            Actions =
            [
                new TriggerActionDefinition { Type = "complete_objective", ObjectiveId = "obj1" }
            ],
            Once = true,
        });
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var runner = new ScriptedEventRunner();
        var system = new TriggerSystem(state, runner);
        system.Tick(new World(), 0.1f);

        Assert.True(state.Objectives[0].IsCompleted);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MissionRewards
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MissionRewards_adds_resources_to_player()
    {
        var rm = new ResourceManager();
        rm.AddPlayer(1);

        var def = MakeDef();
        def.Rewards.Resources.Energy   = 200f;
        def.Rewards.Resources.Minerals = 50f;
        def.Rewards.Xp = 100;

        var state   = MissionLoader.FromDefinition(def);
        var rewards = new MissionRewards(rm);
        var applied = rewards.Apply(1, state);

        Assert.Equal(200f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy));
        Assert.Equal(50f,  rm.GetPlayer(1)!.GetAmount(ResourceType.Minerals));
        Assert.Equal(100,  applied.Xp);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ScriptedEventRunner — spawn hook
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScriptedEventRunner_spawn_units_calls_hook()
    {
        var spawned = new List<(string, float, float)>();
        var runner  = new ScriptedEventRunner
        {
            SpawnUnit = (id, x, y) => spawned.Add((id, x, y)),
        };

        var action = new TriggerActionDefinition
        {
            Type     = "spawn_units",
            Units    = ["fighter_basic", "fighter_basic"],
            Position = [40, 50],
        };

        var state = MissionLoader.FromDefinition(MakeDef());
        runner.Execute(action, state, new World());

        Assert.Equal(2, spawned.Count);
        Assert.All(spawned, s => Assert.Equal(40f, s.Item2));
        Assert.All(spawned, s => Assert.Equal(50f, s.Item3));
    }

    [Fact]
    public void ScriptedEventRunner_fail_mission_sets_defeat()
    {
        var runner = new ScriptedEventRunner();
        var action = new TriggerActionDefinition { Type = "fail_mission", Reason = "timeout" };
        var state  = MissionLoader.FromDefinition(MakeDef());
        state.StartMission();

        runner.Execute(action, state, new World());

        Assert.Equal(MissionStatus.Defeat, state.Status);
        Assert.Equal("timeout", state.DefeatReason);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ObjectiveChangedEvent
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ObjectiveSystem_publishes_ObjectiveChangedEvent_on_completion()
    {
        var def = MakeDef();
        def.Objectives.Primary.Add(MakeDestroyObj("kill", "target"));
        var state = MissionLoader.FromDefinition(def);
        state.StartMission();

        var bus = new EventBus();
        ObjectiveChangedEvent? evt = null;
        bus.Subscribe<ObjectiveChangedEvent>(e => evt = e);

        // Prevent victory event from muddying the waters — use secondary-only def
        // Actually the primary completes and then victory triggers; that's fine.
        var system = new ObjectiveSystem(state, bus) { IsEntityAlive = _ => false };
        system.Tick(new World(), 0.016f);

        Assert.NotNull(evt);
        Assert.Equal("test", evt!.MissionId);
        Assert.Equal("kill", evt.ObjectiveId);
        Assert.True(evt.Completed);
    }
}
