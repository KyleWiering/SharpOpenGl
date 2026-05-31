using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>Unit tests for <see cref="TriggerSystem"/>.</summary>
public class TriggerSystemTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (MissionState state, EventBus bus, TriggerSystem system, World world)
        Setup(TriggerDefinition[] triggers)
    {
        var def = new MissionDefinition
        {
            Id       = "test",
            Map      = "test_map",
            Triggers = triggers,
            Objectives = new ObjectivesDefinition { Primary = [], Secondary = [] },
            Victory  = new EndConditionDefinition { Type = "all_primary_complete" },
            Defeat   = new EndConditionDefinition { Type = "hero_destroyed" },
        };

        var state  = new MissionState(def) { Phase = MissionPhase.InProgress };
        var bus    = new EventBus();
        var system = new TriggerSystem(state, bus);
        var world  = new World();

        return (state, bus, system, world);
    }

    private static TriggerDefinition MakeTimerTrigger(
        string id, float seconds, bool oneShot = true) => new()
    {
        Id = id,
        Condition = new TriggerConditionDefinition { Type = "timer", Seconds = seconds },
        Actions   = [],
        OneShot   = oneShot,
    };

    // ── Timer trigger ─────────────────────────────────────────────────────────

    [Fact]
    public void Timer_trigger_does_not_fire_before_elapsed()
    {
        var (state, bus, system, world) = Setup([MakeTimerTrigger("t1", 10f)]);

        system.Update(world, 5f);

        Assert.False(state.Triggers[0].HasFired);
    }

    [Fact]
    public void Timer_trigger_fires_after_elapsed()
    {
        var (state, bus, system, world) = Setup([MakeTimerTrigger("t1", 10f)]);

        system.Update(world, 11f);

        Assert.True(state.Triggers[0].HasFired);
    }

    [Fact]
    public void Timer_trigger_accumulates_across_multiple_frames()
    {
        var (state, bus, system, world) = Setup([MakeTimerTrigger("t1", 10f)]);

        system.Update(world, 5f);
        system.Update(world, 3f);
        system.Update(world, 3f); // total 11s

        Assert.True(state.Triggers[0].HasFired);
    }

    [Fact]
    public void OneShot_trigger_does_not_fire_twice()
    {
        var (state, bus, system, world) = Setup([MakeTimerTrigger("t1", 1f, oneShot: true)]);

        int fireCount = 0;
        bus.Subscribe<TriggerFiredEvent>(_ => fireCount++);

        system.Update(world, 2f); // fires
        system.Update(world, 2f); // should NOT fire again

        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void Non_oneShot_trigger_can_fire_multiple_times()
    {
        var (state, bus, system, world) = Setup([MakeTimerTrigger("t1", 1f, oneShot: false)]);

        int fireCount = 0;
        bus.Subscribe<TriggerFiredEvent>(_ => fireCount++);

        system.Update(world, 2f); // fires (elapsed > 1)
        system.Update(world, 2f); // fires again (elapsed still > 1)

        Assert.True(fireCount > 1);
    }

    // ── TriggerFired event ────────────────────────────────────────────────────

    [Fact]
    public void TriggerFired_event_published_with_correct_ids()
    {
        var (state, bus, system, world) = Setup([MakeTimerTrigger("my_trigger", 1f)]);

        TriggerFiredEvent? evt = null;
        bus.Subscribe<TriggerFiredEvent>(e => evt = e);

        system.Update(world, 2f);

        Assert.NotNull(evt);
        Assert.Equal("test", evt!.MissionId);
        Assert.Equal("my_trigger", evt!.TriggerId);
    }

    // ── Dialog action ─────────────────────────────────────────────────────────

    [Fact]
    public void Dialog_action_publishes_DialogEvent()
    {
        var def = MakeTimerTrigger("t1", 1f);
        def.Actions = [
            new TriggerActionDefinition
            {
                Type    = "dialog",
                Speaker = "Commander",
                Text    = "Incoming hostiles!",
            }
        ];

        var (_, bus, system, world) = Setup([def]);

        DialogEvent? evt = null;
        bus.Subscribe<DialogEvent>(e => evt = e);

        system.Update(world, 2f);

        Assert.NotNull(evt);
        Assert.Equal("Commander", evt!.Speaker);
        Assert.Equal("Incoming hostiles!", evt!.Text);
    }

    // ── Kill count ────────────────────────────────────────────────────────────

    [Fact]
    public void KillCount_trigger_fires_when_count_reached()
    {
        var trigger = new TriggerDefinition
        {
            Id        = "kill_5",
            Condition = new TriggerConditionDefinition { Type = "kill_count", Count = 3 },
            Actions   = [],
            OneShot   = true,
        };

        var (state, bus, system, world) = Setup([trigger]);

        system.RecordKill();
        system.RecordKill();
        system.Update(world, 0.016f); // count = 2 < 3 → no fire

        Assert.False(state.Triggers[0].HasFired);

        system.RecordKill();
        system.Update(world, 0.016f); // count = 3 ≥ 3 → fire

        Assert.True(state.Triggers[0].HasFired);
    }

    // ── Phase guard ───────────────────────────────────────────────────────────

    [Fact]
    public void System_does_not_update_when_not_InProgress()
    {
        var (state, bus, system, world) = Setup([MakeTimerTrigger("t1", 1f)]);

        state.Phase = MissionPhase.Victory;

        system.Update(world, 5f);

        Assert.Equal(0f, state.Triggers[0].ElapsedSeconds);
        Assert.False(state.Triggers[0].HasFired);
    }
}
