using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>Unit tests for <see cref="TriggerSystem"/>.</summary>
public class TriggerSystemTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (MissionState state, EventBus bus, TriggerSystem system, World world)
        Setup(TriggerDefinition[] triggers, UnitFactory? unitFactory = null)
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
        var system = new TriggerSystem(state, bus, unitFactory: unitFactory);
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

        system.Update(world, 2f); // fires (elapsed >= 1), then interval-resets to 0
        system.Update(world, 2f); // fires again after another full interval

        Assert.True(fireCount > 1);
    }

    [Fact]
    public void Repeating_timer_interval_resets_does_not_spam_every_frame()
    {
        // oneShot: false, timer seconds = 5: fire once at 5s; not at +2s; again at +3s more.
        var (state, bus, system, world) = Setup([MakeTimerTrigger("wave", 5f, oneShot: false)]);

        int fireCount = 0;
        bus.Subscribe<TriggerFiredEvent>(_ => fireCount++);

        system.Update(world, 5f);
        Assert.Equal(1, fireCount);
        Assert.Equal(0f, state.Triggers[0].ElapsedSeconds);

        system.Update(world, 2f);
        Assert.Equal(1, fireCount); // mid-interval

        system.Update(world, 3f);
        Assert.Equal(2, fireCount);
        Assert.Equal(0f, state.Triggers[0].ElapsedSeconds);
    }

    [Fact]
    public void Repeating_timer_spawn_count_only_increments_on_interval_boundaries()
    {
        // 5s interval over ~10.1s → 2 fires, not dozens.
        var trigger = MakeTimerTrigger("spawn_wave", 5f, oneShot: false);
        trigger.Actions =
        [
            new TriggerActionDefinition
            {
                Type  = "spawn_units",
                Units = ["interceptor_mk2"],
                Tag   = "enemy_wave",
            }
        ];

        var factory = new UnitFactory();
        var (state, bus, system, world) = Setup([trigger], unitFactory: factory);

        int fireCount = 0;
        bus.Subscribe<TriggerFiredEvent>(_ => fireCount++);

        // First interval: fire once.
        system.Update(world, 5f);
        Assert.Equal(1, fireCount);

        // Mid-interval spam frames must not re-fire (would without ElapsedSeconds reset).
        for (int i = 0; i < 200; i++)
            system.Update(world, 0.016f);
        Assert.Equal(1, fireCount);

        // Second full interval → second fire/spawn only.
        system.Update(world, 5f);
        Assert.Equal(2, fireCount);

        int spawnCount = 0;
        foreach (var _ in world.Query<CombatTargetComponent>())
            spawnCount++;
        Assert.Equal(2, spawnCount);
    }

    // ── Spawn faction ─────────────────────────────────────────────────────────

    [Fact]
    public void Spawn_units_defaults_to_faction_2_hostile()
    {
        var trigger = MakeTimerTrigger("spawn", 1f);
        trigger.Actions =
        [
            new TriggerActionDefinition
            {
                Type  = "spawn_units",
                Units = ["interceptor_mk2"],
                Tag   = "enemy_interceptor",
            }
        ];

        var factory = new UnitFactory();
        var (_, _, system, world) = Setup([trigger], unitFactory: factory);

        system.Update(world, 2f);

        CombatTargetComponent? combat = null;
        foreach (var (_, ct) in world.Query<CombatTargetComponent>())
            combat = ct;

        Assert.NotNull(combat);
        Assert.Equal(2, combat!.Faction);
    }

    [Fact]
    public void Spawn_units_respects_explicit_faction()
    {
        var trigger = MakeTimerTrigger("spawn_ally", 1f);
        trigger.Actions =
        [
            new TriggerActionDefinition
            {
                Type    = "spawn_units",
                Units   = ["interceptor_mk2"],
                Faction = 1,
            }
        ];

        var factory = new UnitFactory();
        var (_, _, system, world) = Setup([trigger], unitFactory: factory);

        system.Update(world, 2f);

        CombatTargetComponent? combat = null;
        foreach (var (_, ct) in world.Query<CombatTargetComponent>())
            combat = ct;

        Assert.NotNull(combat);
        Assert.Equal(1, combat!.Faction);
    }

    [Fact]
    public void PlayerColorPalette_faction_2_is_red_hostile_tint()
    {
        var tint = PlayerColorPalette.GetTint(2);
        Assert.True(tint.X > tint.Y && tint.X > tint.Z, "P2 should be red-ish (high R, lower G/B)");
        Assert.Equal(1.0f, tint.X, precision: 2);
        Assert.Equal(0.32f, tint.Y, precision: 2);
        Assert.Equal(0.28f, tint.Z, precision: 2);
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
