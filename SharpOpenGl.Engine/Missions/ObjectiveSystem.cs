using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// ECS system that evaluates all active mission objectives each frame and
/// marks them complete when their conditions are satisfied.
/// Also checks victory and defeat conditions.
/// </summary>
public sealed class ObjectiveSystem : GameSystem
{
    private readonly MissionState _mission;
    private readonly EventBus?    _bus;

    // ── Hooks (set by game layer to provide live data) ────────────────────────

    /// <summary>
    /// Return whether the entity with the given tag/id is still alive.
    /// Default: always returns <c>false</c> (treat target as destroyed).
    /// </summary>
    public Func<string, bool> IsEntityAlive { get; set; } = _ => false;

    /// <summary>
    /// Return the current amount of a named resource for player 1.
    /// Default: always returns 0.
    /// </summary>
    public Func<string, float> GetResourceAmount { get; set; } = _ => 0f;

    /// <summary>
    /// Return the world position of the player's hero (for reach_area checks).
    /// Default: origin.
    /// </summary>
    public Func<Vector2> GetHeroPosition { get; set; } = () => Vector2.Zero;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ObjectiveSystem(MissionState mission, EventBus? bus = null)
    {
        _mission = mission;
        _bus     = bus;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        if (_mission.Status != MissionStatus.InProgress) return;

        foreach (ObjectiveRecord obj in _mission.Objectives)
        {
            if (obj.IsCompleted) continue;
            EvaluateObjective(obj, deltaTime);
        }

        CheckEndConditions();
    }

    // ── Per-objective evaluation ──────────────────────────────────────────────

    private void EvaluateObjective(ObjectiveRecord obj, float deltaTime)
    {
        bool met = obj.Definition.Type switch
        {
            "destroy_target" => EvalDestroyTarget(obj),
            "escort"         => EvalEscort(obj),
            "survive_time"   => EvalSurviveTime(obj, deltaTime),
            "collect"        => EvalCollect(obj),
            "reach_area"     => EvalReachArea(obj),
            _                => false,
        };

        if (!met) return;

        obj.Complete();
        _bus?.Publish(new ObjectiveChangedEvent(
            _mission.Definition.Id, obj.Definition.Id, completed: true));
    }

    private bool EvalDestroyTarget(ObjectiveRecord obj) =>
        !string.IsNullOrEmpty(obj.Definition.Target) &&
        !IsEntityAlive(obj.Definition.Target);

    private bool EvalEscort(ObjectiveRecord obj) =>
        !string.IsNullOrEmpty(obj.Definition.Target) &&
        IsEntityAlive(obj.Definition.Target) &&
        _mission.AllPrimaryComplete;  // simplified: escort alive at victory

    private bool EvalSurviveTime(ObjectiveRecord obj, float deltaTime)
    {
        float required = obj.Definition.Seconds ?? 0f;
        obj.Progress += deltaTime;
        return obj.Progress >= required;
    }

    private bool EvalCollect(ObjectiveRecord obj)
    {
        if (string.IsNullOrEmpty(obj.Definition.Resource)) return false;
        float current = GetResourceAmount(obj.Definition.Resource);
        return current >= (obj.Definition.Amount ?? 0f);
    }

    private bool EvalReachArea(ObjectiveRecord obj)
    {
        int[]? pos = obj.Definition.Position;
        if (pos == null || pos.Length < 2) return false;

        float radius = obj.Definition.Radius ?? 2f;
        Vector2 heroPos = GetHeroPosition();
        float dx = heroPos.X - pos[0];
        float dy = heroPos.Y - pos[1];
        return (dx * dx + dy * dy) <= radius * radius;
    }

    // ── End condition check ───────────────────────────────────────────────────

    private void CheckEndConditions()
    {
        // Victory
        string vtype = _mission.Definition.Victory.Type;
        bool victory = vtype switch
        {
            "all_primary_complete" => _mission.AllPrimaryComplete,
            "timer" => _mission.ElapsedSeconds >=
                       (_mission.Definition.Victory.Seconds ?? float.MaxValue),
            _ => false,
        };

        if (victory)
        {
            _mission.SetVictory();
            _bus?.Publish(new MissionCompletedEvent(_mission.Definition.Id));
            return;
        }

        // Defeat
        string dtype = _mission.Definition.Defeat.Type;
        bool defeat = dtype switch
        {
            "hero_destroyed" => !IsEntityAlive("hero"),
            "timer" => _mission.ElapsedSeconds >=
                       (_mission.Definition.Defeat.Seconds ?? float.MaxValue),
            _ => false,
        };

        if (defeat)
        {
            _mission.SetDefeat(dtype);
            _bus?.Publish(new MissionFailedEvent(_mission.Definition.Id, dtype));
        }
    }
}
