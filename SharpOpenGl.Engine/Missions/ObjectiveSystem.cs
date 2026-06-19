using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// ECS <see cref="GameSystem"/> that evaluates mission objective conditions each frame and
/// updates <see cref="ObjectiveProgress"/> accordingly.
/// </summary>
public sealed class ObjectiveSystem : GameSystem
{
    private readonly MissionState _state;
    private readonly EventBus _bus;
    private readonly ResourceManager? _resources;

    /// <summary>Player ID whose resources are checked for <c>collect</c> objectives.</summary>
    public int PlayerId { get; set; } = 1;

    /// <param name="state">The running mission state to update.</param>
    /// <param name="bus">Event bus used to publish objective and mission events.</param>
    /// <param name="resources">Optional resource manager for <c>collect</c> objectives.</param>
    public ObjectiveSystem(MissionState state, EventBus bus, ResourceManager? resources = null)
    {
        _state     = state;
        _bus       = bus;
        _resources = resources;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        if (_state.Phase != MissionPhase.InProgress) return;

        _state.ElapsedTime += deltaTime;

        foreach (var obj in _state.AllObjectives)
        {
            if (obj.IsCompleted) continue;

            bool wasCompleted = EvaluateObjective(world, obj, deltaTime);
            if (wasCompleted)
            {
                obj.IsCompleted = true;
                _bus.Publish(new ObjectiveChangedEvent(_state.Definition.Id, obj.Id, true));
            }
        }

        CheckEndConditions(world);
    }

    private bool EvaluateObjective(World world, ObjectiveProgress obj, float deltaTime)
    {
        return obj.Definition.Type switch
        {
            "destroy_target"  => EvalDestroyTarget(world, obj),
            "survive_time"    => EvalSurviveTime(obj, deltaTime),
            "reach_area"      => EvalReachArea(world, obj),
            "collect"         => EvalCollect(obj),
            "condition"       => EvalCondition(world, obj),
            _                 => false,
        };
    }

    private bool EvalDestroyTarget(World world, ObjectiveProgress obj)
    {
        if (string.IsNullOrEmpty(obj.Definition.Target)) return false;

        if (_state.EntityGroups.TryGetValue(obj.Definition.Target, out HashSet<Entity>? group)
            && group.Count > 0)
        {
            group.RemoveWhere(e => !world.IsAlive(e));
            return group.Count == 0;
        }

        if (!_state.EntityTags.TryGetValue(obj.Definition.Target, out Entity target))
            return false;

        return !world.IsAlive(target);
    }

    private static bool EvalSurviveTime(ObjectiveProgress obj, float deltaTime)
    {
        if (!float.TryParse(obj.Definition.Target, out float required)) return false;

        obj.ElapsedTime += deltaTime;
        return obj.ElapsedTime >= required;
    }

    private static bool EvalReachArea(World world, ObjectiveProgress obj)
    {
        if (!MapCoordinates.TryParseReachArea(obj.Definition.Condition, out Vector3 center, out float radius))
            return false;

        foreach (var (entity, tf) in world.Query<TransformComponent>())
        {
            if (world.HasComponent<AIControlledComponent>(entity)) continue;
            if (world.HasComponent<BuildingComponent>(entity)) continue;

            float dx = tf.Position.X - center.X;
            float dz = tf.Position.Z - center.Z;
            if (MathF.Sqrt(dx * dx + dz * dz) <= radius)
                return true;
        }
        return false;
    }

    private bool EvalCollect(ObjectiveProgress obj)
    {
        if (_resources == null || string.IsNullOrEmpty(obj.Definition.Target)) return false;

        var parts = obj.Definition.Target.Split(':');
        if (parts.Length != 2) return false;
        if (!float.TryParse(parts[1], out float required)) return false;

        if (!Enum.TryParse<ResourceType>(parts[0], ignoreCase: true, out ResourceType rt)) return false;

        var display = _resources.GetDisplay(PlayerId, rt);
        return display.Current >= required;
    }

    private bool EvalCondition(World world, ObjectiveProgress obj)
    {
        if (obj.Definition.Condition?.Trim() == "hero.health == hero.maxHealth")
        {
            foreach (var (_, hero) in world.Query<HeroComponent>())
            {
                _ = hero;
                var health = world.Query<HealthComponent>()
                    .FirstOrDefault(p => world.HasComponent<HeroComponent>(p.Entity));
                if (health.Component == null) return false;
                return health.Component.CurrentHP >= health.Component.MaxHP;
            }
            return false;
        }
        return false;
    }

    private void CheckEndConditions(World world)
    {
        string victoryType = _state.Definition.Victory?.Type ?? string.Empty;
        string defeatType  = _state.Definition.Defeat?.Type ?? string.Empty;

        if (victoryType == "all_primary_complete" && _state.AllPrimaryComplete)
        {
            _state.Phase = MissionPhase.Victory;
            _bus.Publish(new MissionVictoryEvent(_state.Definition.Id));
            return;
        }

        if (defeatType == "hero_destroyed")
        {
            bool heroAlive = world.Query<HeroComponent>().Any();
            if (!heroAlive)
            {
                _state.Phase        = MissionPhase.Defeat;
                _state.DefeatReason = "Hero ship destroyed.";
                _bus.Publish(new MissionDefeatEvent(_state.Definition.Id, _state.DefeatReason));
            }
        }
    }
}