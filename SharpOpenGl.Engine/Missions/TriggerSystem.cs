using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// ECS system that evaluates mission trigger conditions each frame and
/// executes the associated scripted actions when they fire.
/// </summary>
public sealed class TriggerSystem : GameSystem
{
    private readonly MissionState     _mission;
    private readonly EventBus?        _bus;
    private readonly ScriptedEventRunner _runner;

    // ── Constructor ───────────────────────────────────────────────────────────

    public TriggerSystem(
        MissionState mission,
        ScriptedEventRunner runner,
        EventBus? bus = null)
    {
        _mission = mission;
        _runner  = runner;
        _bus     = bus;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        if (_mission.Status != MissionStatus.InProgress) return;

        foreach (TriggerRecord trigger in _mission.Triggers)
        {
            if (trigger.HasFired && trigger.Definition.Once) continue;

            // Advance per-trigger timer for timer-type conditions
            if (trigger.Definition.Condition.Type == "timer")
                trigger.ElapsedSeconds += deltaTime;

            if (IsConditionMet(trigger))
            {
                FireTrigger(trigger, world);
                trigger.Fire();
                if (!trigger.Definition.Once)
                    trigger.Reset();
            }
        }
    }

    // ── Condition evaluation ──────────────────────────────────────────────────

    private bool IsConditionMet(TriggerRecord trigger)
    {
        TriggerConditionDefinition cond = trigger.Definition.Condition;
        return cond.Type switch
        {
            "timer"               => trigger.ElapsedSeconds >= (cond.Seconds ?? float.MaxValue),
            "kill_count"          => _mission.TotalKills    >= (cond.Count   ?? int.MaxValue),
            "resource_threshold"  => EvalResourceThreshold(cond),
            "area_enter"          => EvalAreaEnter(cond),
            _                     => false,
        };
    }

    private bool EvalResourceThreshold(TriggerConditionDefinition cond)
    {
        if (_runner.GetResourceAmount == null) return false;
        string resource = cond.Resource ?? string.Empty;
        float amount    = _runner.GetResourceAmount(resource);
        return amount >= (cond.Threshold ?? float.MaxValue);
    }

    private bool EvalAreaEnter(TriggerConditionDefinition cond)
    {
        if (_runner.GetHeroPosition == null || cond.Position == null || cond.Position.Length < 2)
            return false;

        float radius = cond.Radius ?? 2f;
        var pos  = _runner.GetHeroPosition();
        float dx = pos.X - cond.Position[0];
        float dy = pos.Y - cond.Position[1];
        return (dx * dx + dy * dy) <= radius * radius;
    }

    // ── Trigger execution ─────────────────────────────────────────────────────

    private void FireTrigger(TriggerRecord trigger, World world)
    {
        _bus?.Publish(new TriggerFiredEvent(_mission.Definition.Id, trigger.Definition.Id));

        foreach (TriggerActionDefinition action in trigger.Definition.Actions)
            _runner.Execute(action, _mission, world);
    }
}
