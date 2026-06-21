using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Moves combatants toward their <see cref="CombatTargetComponent.CurrentTarget"/>
/// until within weapon range, then holds position so <see cref="CombatSystem"/> can fire.
/// Only runs when a valid attack target is locked — does not interfere with patrol or move orders.
/// </summary>
public sealed class AttackChaseSystem : GameSystem
{
    private const float RangeHoldFactor = 0.85f;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, ct) in world.Query<CombatTargetComponent>())
        {
            if (ct.CurrentTarget == Entity.Null)
                continue;

            // Player move / patrol routes take priority over auto-chase.
            if (world.HasComponent<WaypointQueueComponent>(entity)
                || world.HasComponent<DestinationComponent>(entity))
                continue;

            var movement = world.GetComponent<MovementComponent>(entity);
            var wl = world.GetComponent<WeaponListComponent>(entity);
            if (movement == null || wl == null || wl.Weapons.Count == 0) continue;

            if (!world.IsAlive(ct.CurrentTarget))
            {
                ct.CurrentTarget = Entity.Null;
                continue;
            }

            var health = world.GetComponent<HealthComponent>(ct.CurrentTarget);
            if (health == null || health.IsDead)
            {
                ct.CurrentTarget = Entity.Null;
                continue;
            }

            var selfPos = world.GetComponent<TransformComponent>(entity)?.Position ?? Vector3.Zero;
            var targetPos = world.GetComponent<TransformComponent>(ct.CurrentTarget)?.Position ?? Vector3.Zero;
            float dist = HorizontalDistance(selfPos, targetPos);
            float maxRange = MaxWeaponRange(wl);

            if (dist <= maxRange * RangeHoldFactor)
            {
                movement.PathTarget = null;
                continue;
            }

            world.RemoveComponent<DestinationComponent>(entity);
            world.RemoveComponent<PathComponent>(entity);
            movement.PathTarget = targetPos;
        }
    }

    private static float MaxWeaponRange(WeaponListComponent wl)
    {
        float max = 0f;
        foreach (var weapon in wl.Weapons)
            max = MathF.Max(max, weapon.Range);
        return max > 0f ? max : 1f;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}