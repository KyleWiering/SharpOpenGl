using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Moves projectile entities each frame and resolves hits.
/// <list type="bullet">
///   <item><b>Linear</b>: travel in <see cref="ProjectileComponent.Direction"/> at constant speed.</item>
///   <item><b>Homing</b>: steer toward target entity's current position each frame.</item>
///   <item><b>AoE</b>: travels like Homing; on arrival deals damage to all enemies in blast radius.</item>
///   <item>Projectiles are destroyed on hit, on target death, or when lifetime expires.</item>
/// </list>
/// </summary>
public sealed class ProjectileSystem : GameSystem
{
    private readonly EventBus _bus;

    /// <summary>Distance at which a projectile is considered to have "hit" its target.</summary>
    public const float HitRadius = 2f;

    public ProjectileSystem(EventBus bus) => _bus = bus;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        var toDestroy = new List<Entity>();

        foreach (var (projEntity, proj) in world.Query<ProjectileComponent>())
        {
            proj.Lifetime -= deltaTime;
            if (proj.Lifetime <= 0f)
            {
                toDestroy.Add(projEntity);
                continue;
            }

            var transform = world.GetComponent<TransformComponent>(projEntity);
            if (transform == null)
            {
                toDestroy.Add(projEntity);
                continue;
            }

            bool hit = proj.Type switch
            {
                ProjectileType.Linear  => StepLinear(world, proj, transform, deltaTime, projEntity),
                ProjectileType.Homing  => StepHoming(world, proj, transform, deltaTime, projEntity),
                ProjectileType.AoE     => StepAoE(world, proj, transform, deltaTime, projEntity),
                _                      => false,
            };

            if (hit) toDestroy.Add(projEntity);
        }

        foreach (var e in toDestroy)
            if (world.IsAlive(e)) world.DestroyEntity(e);
    }

    // ── Linear ────────────────────────────────────────────────────────────────

    private bool StepLinear(World world, ProjectileComponent proj, TransformComponent transform, float dt,
        Entity projEntity)
    {
        // Check hit at current position before moving (catches slow-moving projectiles).
        if (CheckHit(world, proj, transform.Position, single: true, projEntity)) return true;

        transform.Position += proj.Direction * proj.Speed * dt;

        // Check hit at new position.
        return CheckHit(world, proj, transform.Position, single: true, projEntity);
    }

    // ── Homing ────────────────────────────────────────────────────────────────

    private bool StepHoming(World world, ProjectileComponent proj, TransformComponent transform, float dt,
        Entity projEntity)
    {
        if (!world.IsAlive(proj.Target))
        {
            // Target is gone; continue on last known direction.
            transform.Position += proj.Direction * proj.Speed * dt;
            return false;
        }

        var targetPos = world.GetComponent<TransformComponent>(proj.Target)?.Position
                        ?? transform.Position;
        var toTarget = targetPos - transform.Position;

        if (toTarget.LengthSquared < HitRadius * HitRadius)
        {
            // Reached target.
            ApplyDamage(world, proj, proj.Target, projEntity);
            return true;
        }

        proj.Direction     = Vector3.Normalize(toTarget);
        transform.Position += proj.Direction * proj.Speed * dt;
        return false;
    }

    // ── AoE ───────────────────────────────────────────────────────────────────

    private bool StepAoE(World world, ProjectileComponent proj, TransformComponent transform,
        float dt, Entity projEntity)
    {
        // Travel toward target like homing.
        if (!world.IsAlive(proj.Target))
        {
            // Detonate at current position.
            ExplodeAoE(world, proj, transform.Position, projEntity);
            return true;
        }

        var targetPos = world.GetComponent<TransformComponent>(proj.Target)?.Position
                        ?? transform.Position;
        var toTarget = targetPos - transform.Position;

        if (toTarget.LengthSquared < HitRadius * HitRadius)
        {
            ExplodeAoE(world, proj, transform.Position, projEntity);
            return true;
        }

        proj.Direction     = Vector3.Normalize(toTarget);
        transform.Position += proj.Direction * proj.Speed * dt;
        return false;
    }

    private void ExplodeAoE(World world, ProjectileComponent proj, Vector3 center, Entity projEntity)
    {
        float radiusSq = proj.BlastRadius * proj.BlastRadius;

        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (health.IsDead) continue;

            var candidateCt = world.GetComponent<CombatTargetComponent>(candidate);
            if (candidateCt != null && candidateCt.Faction == proj.OwnerFaction) continue;

            var candidatePos = world.GetComponent<TransformComponent>(candidate)?.Position ?? center;
            if ((candidatePos - center).LengthSquared > radiusSq) continue;

            float final = DamageCalculator.Apply(proj.Damage, health);
            _bus.Publish(new ProjectileHitEvent(projEntity.Index, candidate.Index, proj.Damage));
            _bus.Publish(new DamageDealtEvent(proj.Owner.Index, candidate.Index, proj.Damage, final));
        }

        _bus.Publish(new ExplosionVfxEvent(center, ExplosionVfxKind.Impact, MathF.Max(1f, proj.BlastRadius * 0.15f)));
    }

    // ── Shared hit detection ──────────────────────────────────────────────────

    /// <summary>
    /// Broad-phase check: find the first (or all) hostile entities within <see cref="HitRadius"/>.
    /// </summary>
    private bool CheckHit(World world, ProjectileComponent proj, Vector3 pos, bool single, Entity projEntity)
    {
        bool anyHit = false;

        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (health.IsDead) continue;

            var candidateCt = world.GetComponent<CombatTargetComponent>(candidate);
            if (candidateCt != null && candidateCt.Faction == proj.OwnerFaction) continue;

            var candidatePos = world.GetComponent<TransformComponent>(candidate)?.Position ?? pos;
            if ((candidatePos - pos).LengthSquared > HitRadius * HitRadius) continue;

            ApplyDamage(world, proj, candidate, projEntity);
            anyHit = true;

            if (single) return true;
        }

        return anyHit;
    }

    private void ApplyDamage(World world, ProjectileComponent proj, Entity target, Entity projEntity)
    {
        var health = world.GetComponent<HealthComponent>(target);
        if (health == null) return;

        float final = DamageCalculator.Apply(proj.Damage, health);
        _bus.Publish(new ProjectileHitEvent(projEntity.Index, target.Index, proj.Damage));
        _bus.Publish(new DamageDealtEvent(proj.Owner.Index, target.Index, proj.Damage, final));

        var hitPos = world.GetComponent<TransformComponent>(target)?.Position
                     ?? world.GetComponent<TransformComponent>(projEntity)?.Position
                     ?? Vector3.Zero;
        _bus.Publish(new ExplosionVfxEvent(hitPos, ExplosionVfxKind.Impact));
    }
}
