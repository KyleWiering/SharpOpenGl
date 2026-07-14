using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// ECS system that restores hull HP on nearby damaged friendly/neutral ships
/// for entities with <see cref="ShipRepairComponent"/>.
/// </summary>
public sealed class RepairSystem : GameSystem
{
    /// <summary>HP fraction at which <see cref="DisabledComponent"/> is cleared after repair.</summary>
    public const float DisabledReactivationThreshold = 0.25f;

    private readonly EventBus _bus;

    /// <summary>Player faction id used to identify repair-capable player units.</summary>
    public int PlayerFaction { get; set; } = 1;

    public RepairSystem(EventBus bus) => _bus = bus;

    public override void Update(World world, float deltaTime)
    {
        foreach (var (repairer, repair) in world.Query<ShipRepairComponent>())
        {
            if (!IsPlayerRepairer(world, repairer))
            {
                repair.ActiveTarget = Entity.Null;
                continue;
            }

            var repairerTf = world.GetComponent<TransformComponent>(repairer);
            if (repairerTf == null)
            {
                repair.ActiveTarget = Entity.Null;
                continue;
            }

            Entity? target = ResolveTarget(world, repairer, repair, repairerTf.Position);
            repair.ActiveTarget = target ?? Entity.Null;
            if (target == null) continue;

            var targetEntity = target.Value;
            var health = world.GetComponent<HealthComponent>(targetEntity);
            if (health == null || health.IsDead || health.CurrentHP >= health.MaxHP) continue;

            float amount = repair.RepairRate * deltaTime;
            health.CurrentHP = MathF.Min(health.MaxHP, health.CurrentHP + amount);

            // Disabled derelicts stay inert until repair brings them above the reactivation threshold.
            if (world.HasComponent<DisabledComponent>(targetEntity) &&
                health.HPFraction >= DisabledReactivationThreshold)
            {
                world.RemoveComponent<DisabledComponent>(targetEntity);
            }

            _bus.Publish(new RepairTickEvent(repairer.Index, targetEntity.Index));
        }
    }

    private bool IsPlayerRepairer(World world, Entity repairer)
    {
        var combat = world.GetComponent<CombatTargetComponent>(repairer);
        int faction = combat?.Faction ?? PlayerFaction;
        return faction == PlayerFaction;
    }

    private Entity? ResolveTarget(
        World world, Entity repairer, ShipRepairComponent repair, Vector3 repairerPos)
    {
        var order = world.GetComponent<RepairOrderComponent>(repairer);
        if (order != null && order.Target != Entity.Null &&
            IsEligibleTarget(world, repairer, repair, repairerPos, order.Target))
        {
            return order.Target;
        }

        Entity? nearest = null;
        float nearestDistSq = float.MaxValue;

        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (!IsEligibleTarget(world, repairer, repair, repairerPos, candidate)) continue;

            var tf = world.GetComponent<TransformComponent>(candidate);
            if (tf == null) continue;

            float distSq = HorizontalDistanceSq(repairerPos, tf.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private bool IsEligibleTarget(
        World world, Entity repairer, ShipRepairComponent repair, Vector3 repairerPos, Entity candidate)
    {
        if (candidate == repairer || !world.IsAlive(candidate)) return false;

        var health = world.GetComponent<HealthComponent>(candidate);
        if (health == null || health.IsDead || health.CurrentHP >= health.MaxHP) return false;

        if (!MatchesRepairableCategory(world, candidate, repair.RepairableCategories)) return false;

        var repairerFaction = world.GetComponent<CombatTargetComponent>(repairer)?.Faction ?? PlayerFaction;
        var candidateFaction = world.GetComponent<CombatTargetComponent>(candidate)?.Faction;
        if (candidateFaction != repairerFaction && candidateFaction != 0) return false;

        var tf = world.GetComponent<TransformComponent>(candidate);
        if (tf == null) return false;

        float dist = MathF.Sqrt(HorizontalDistanceSq(repairerPos, tf.Position));
        return dist <= repair.RepairRange;
    }

    internal static bool MatchesRepairableCategory(
        World world, Entity target, IReadOnlyList<string> categories)
    {
        if (categories.Count == 0) return true;

        var name = world.GetComponent<EntityNameComponent>(target);
        string definitionId = name?.DefinitionId ?? string.Empty;
        if (string.IsNullOrEmpty(definitionId)) return false;

        foreach (string category in categories)
        {
            if (definitionId.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                definitionId.StartsWith(category + "_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return dx * dx + dz * dz;
    }
}