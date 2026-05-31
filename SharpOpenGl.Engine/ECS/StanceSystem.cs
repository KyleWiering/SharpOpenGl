using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Applies stance-based targeting rules to entities with <see cref="StanceComponent"/>.
/// <list type="bullet">
///   <item><b>Neutral</b>: Does not auto-engage enemies.</item>
///   <item><b>Defensive</b>: Engages enemies within defensive radius or that attack first.</item>
///   <item><b>Aggressive</b>: Pursues any enemy within weapon range.</item>
/// </list>
/// </summary>
public sealed class StanceSystem : GameSystem
{
    private const float DefensiveRadius = 150f;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, stance) in world.Query<StanceComponent>())
        {
            var ct = world.GetComponent<CombatTargetComponent>(entity);
            if (ct == null) continue;

            switch (stance.CurrentStance)
            {
                case Stance.Neutral:
                    // Clear any auto-acquired target (keep manually assigned ones)
                    if (ct.CurrentTarget != Entity.Null &&
                        !world.IsAlive(ct.CurrentTarget))
                    {
                        ct.CurrentTarget = Entity.Null;
                    }
                    break;

                case Stance.Defensive:
                    if (ct.CurrentTarget == Entity.Null)
                    {
                        ct.CurrentTarget = FindDefensiveTarget(world, entity, ct);
                    }
                    break;

                case Stance.Aggressive:
                    if (ct.CurrentTarget == Entity.Null)
                    {
                        var wl = world.GetComponent<WeaponListComponent>(entity);
                        float range = wl?.Weapons.Count > 0
                            ? wl.Weapons[0].Range
                            : DefensiveRadius;
                        ct.CurrentTarget = FindAggressiveTarget(world, entity, ct, range);
                    }
                    break;
            }
        }
    }

    private static Entity FindDefensiveTarget(World world, Entity self,
                                               CombatTargetComponent selfCt)
    {
        var selfPos = world.GetComponent<TransformComponent>(self)?.Position ?? Vector3.Zero;

        // Find enemies targeting us or within defensive radius
        foreach (var (candidate, candidateCt) in world.Query<CombatTargetComponent>())
        {
            if (candidate == self) continue;
            if (candidateCt.Faction == selfCt.Faction) continue;

            var health = world.GetComponent<HealthComponent>(candidate);
            if (health == null || health.IsDead) continue;

            // Enemy is targeting us — retaliate
            if (candidateCt.CurrentTarget == self) return candidate;

            // Enemy within defensive radius
            var candidatePos = world.GetComponent<TransformComponent>(candidate)?.Position
                              ?? Vector3.Zero;
            float dist = (candidatePos - selfPos).Length;
            if (dist <= DefensiveRadius) return candidate;
        }

        return Entity.Null;
    }

    private static Entity FindAggressiveTarget(World world, Entity self,
                                                CombatTargetComponent selfCt,
                                                float range)
    {
        var selfPos = world.GetComponent<TransformComponent>(self)?.Position ?? Vector3.Zero;
        Entity closest = Entity.Null;
        float closestDist = float.MaxValue;

        foreach (var (candidate, candidateCt) in world.Query<CombatTargetComponent>())
        {
            if (candidate == self) continue;
            if (candidateCt.Faction == selfCt.Faction) continue;

            var health = world.GetComponent<HealthComponent>(candidate);
            if (health == null || health.IsDead) continue;

            var candidatePos = world.GetComponent<TransformComponent>(candidate)?.Position
                              ?? Vector3.Zero;
            float dist = (candidatePos - selfPos).Length;
            if (dist <= range && dist < closestDist)
            {
                closestDist = dist;
                closest = candidate;
            }
        }

        return closest;
    }
}
