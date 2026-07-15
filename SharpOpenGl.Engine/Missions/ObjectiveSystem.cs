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
            "construct"       => EvalConstruct(world, obj),
            "condition"       => EvalCondition(world, obj),
            "repair_target"   => EvalRepairTarget(world, obj),
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

        // Canonical target: "{ResourceType}:{amount}" e.g. "Minerals:1000", "Energy:500".
        var parts = obj.Definition.Target.Split(':');
        if (parts.Length != 2) return false;
        if (!float.TryParse(parts[1], out float required)) return false;

        if (!Enum.TryParse<ResourceType>(parts[0], ignoreCase: true, out ResourceType rt)) return false;

        var display = _resources.GetDisplay(PlayerId, rt);
        return display.Current >= required;
    }

    /// <summary>
    /// Evaluates a <c>construct</c> objective.
    /// <para>
    /// Building count: <c>target = "{definitionId}:{count}"</c>
    /// (e.g. <c>defense_turret:5</c>). Counts completed player-owned buildings
    /// (<see cref="BuildingComponent.PlayerId"/> == <see cref="PlayerId"/>,
    /// matching <see cref="BuildingComponent.BuildingType"/>, not under construction).
    /// </para>
    /// <para>
    /// Unit production: <c>target = "unit:{definitionId}:{count}"</c>
    /// (e.g. <c>unit:fighter_basic:1</c>). Counts living non-building entities with
    /// matching <see cref="EntityNameComponent.DefinitionId"/> owned by the player
    /// (<see cref="CombatTargetComponent.Faction"/> == <see cref="PlayerId"/>).
    /// </para>
    /// </summary>
    private bool EvalConstruct(World world, ObjectiveProgress obj)
    {
        if (string.IsNullOrEmpty(obj.Definition.Target)) return false;

        var parts = obj.Definition.Target.Split(':');
        if (parts.Length < 2) return false;

        // Unit form: "unit:{definitionId}:{count}"
        if (parts.Length == 3
            && parts[0].Equals("unit", StringComparison.OrdinalIgnoreCase))
        {
            string unitDefId = parts[1];
            if (string.IsNullOrEmpty(unitDefId)) return false;
            if (!int.TryParse(parts[2], out int unitCount) || unitCount < 1) return false;

            int living = 0;
            foreach (var (entity, name) in world.Query<EntityNameComponent>())
            {
                if (world.HasComponent<BuildingComponent>(entity)) continue;
                if (!world.IsAlive(entity)) continue;
                if (!string.Equals(name.DefinitionId, unitDefId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var ct = world.GetComponent<CombatTargetComponent>(entity);
                if (ct == null || ct.Faction != PlayerId) continue;

                var health = world.GetComponent<HealthComponent>(entity);
                if (health != null && health.IsDead) continue;

                living++;
                if (living >= unitCount) return true;
            }

            return living >= unitCount;
        }

        // Building form: "{definitionId}:{count}"
        if (parts.Length != 2) return false;

        string buildingDefId = parts[0];
        if (string.IsNullOrEmpty(buildingDefId)) return false;
        if (!int.TryParse(parts[1], out int buildingCount) || buildingCount < 1) return false;

        int completed = 0;
        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (building.PlayerId != PlayerId) continue;
            if (!string.Equals(building.BuildingType, buildingDefId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (world.HasComponent<UnderConstructionComponent>(entity)) continue;
            if (!world.IsAlive(entity)) continue;

            completed++;
            if (completed >= buildingCount) return true;
        }

        return completed >= buildingCount;
    }

    private bool EvalRepairTarget(World world, ObjectiveProgress obj)
    {
        if (string.IsNullOrEmpty(obj.Definition.Target)) return false;
        if (!_state.EntityTags.TryGetValue(obj.Definition.Target, out Entity target)) return false;
        if (!world.IsAlive(target)) return false;

        var health = world.GetComponent<HealthComponent>(target);
        if (health == null || health.MaxHP <= 0f) return false;

        float threshold = ParseHealthPercentThreshold(obj.Definition.Condition);
        return health.CurrentHP / health.MaxHP >= threshold;
    }

    private static float ParseHealthPercentThreshold(string? condition)
    {
        const float defaultThreshold = 0.50f;
        if (string.IsNullOrWhiteSpace(condition)) return defaultThreshold;

        string trimmed = condition.Trim();
        if (trimmed.StartsWith("healthPercent", StringComparison.OrdinalIgnoreCase))
        {
            int geIndex = trimmed.IndexOf(">=", StringComparison.Ordinal);
            if (geIndex >= 0)
            {
                string valuePart = trimmed[(geIndex + 2)..].Trim();
                if (float.TryParse(valuePart, out float parsed))
                    return parsed;
            }
        }

        return defaultThreshold;
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

        if (defeatType == "unit_destroyed")
        {
            string? tag = _state.Definition.Defeat?.Target;
            if (string.IsNullOrEmpty(tag)) return;

            bool destroyed = !_state.EntityTags.TryGetValue(tag, out Entity tagged) ||
                             !world.IsAlive(tagged) ||
                             world.GetComponent<HealthComponent>(tagged)?.IsDead == true;

            if (destroyed)
            {
                _state.Phase        = MissionPhase.Defeat;
                _state.DefeatReason = $"Critical unit '{tag}' destroyed.";
                _bus.Publish(new MissionDefeatEvent(_state.Definition.Id, _state.DefeatReason));
            }
        }
    }
}