using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Drives real-time auto-attack combat each frame:
/// <list type="bullet">
///   <item>Validates and drops stale targets.</item>
///   <item>Acquires a new target when a unit is idle.</item>
///   <item>Fires weapons at the current target when in range and cooldown is ready.</item>
///   <item>Spawns <see cref="ProjectileComponent"/> entities (or applies instant damage).</item>
///   <item>Handles unit death: publishes <see cref="UnitDiedEvent"/>, destroys the entity.</item>
/// </list>
/// Requires both <see cref="CombatTargetComponent"/> and <see cref="WeaponListComponent"/>
/// on the attacker, and <see cref="HealthComponent"/> on the target.
/// </summary>
public sealed class CombatSystem : GameSystem
{
    private readonly EventBus _bus;

    /// <summary>XP awarded to the killer when a unit dies (future: drive from balance.json).</summary>
    public int BaseXpPerKill { get; set; } = 25;

    public CombatSystem(EventBus bus) => _bus = bus;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        TickWeaponCooldowns(world, deltaTime);
        TickCombat(world, deltaTime);
        ProcessDeaths(world);
    }

    // ── Weapon cooldowns ──────────────────────────────────────────────────────

    private static void TickWeaponCooldowns(World world, float deltaTime)
    {
        foreach (var (_, wl) in world.Query<WeaponListComponent>())
        {
            foreach (var w in wl.Weapons)
            {
                if (w.Cooldown > 0f)
                    w.Cooldown = Math.Max(0f, w.Cooldown - deltaTime);
            }
        }
    }

    // ── Main combat loop ──────────────────────────────────────────────────────

    private void TickCombat(World world, float deltaTime)
    {
        foreach (var (attacker, ct) in world.Query<CombatTargetComponent>())
        {
            var wl = world.GetComponent<WeaponListComponent>(attacker);
            if (wl == null || wl.Weapons.Count == 0) continue;

            // Drop stale target.
            if (!world.IsAlive(ct.CurrentTarget) ||
                world.GetComponent<HealthComponent>(ct.CurrentTarget) == null ||
                world.GetComponent<HealthComponent>(ct.CurrentTarget)!.IsDead)
            {
                ct.CurrentTarget = Entity.Null;
            }

            // Acquire target if needed.
            if (ct.CurrentTarget == Entity.Null)
            {
                ct.CurrentTarget = AcquireTarget(world, attacker, ct, wl.Weapons[0].Range);
            }

            if (ct.CurrentTarget == Entity.Null) continue;

            // Try to fire each weapon.
            var attackerPos = world.GetComponent<TransformComponent>(attacker)?.Position ?? Vector3.Zero;
            var targetPos   = world.GetComponent<TransformComponent>(ct.CurrentTarget)?.Position ?? Vector3.Zero;
            float dist      = (targetPos - attackerPos).Length;

            foreach (var weapon in wl.Weapons)
            {
                if (!weapon.IsReady) continue;
                if (dist > weapon.Range) continue;

                Fire(world, attacker, ct, weapon, attackerPos, targetPos);
                weapon.Cooldown = weapon.FireRate > 0f ? 1f / weapon.FireRate : float.MaxValue;
            }
        }
    }

    // ── Target acquisition ────────────────────────────────────────────────────

    private static Entity AcquireTarget(
        World world, Entity attacker, CombatTargetComponent ct, float range)
    {
        var attackerPos = world.GetComponent<TransformComponent>(attacker)?.Position ?? Vector3.Zero;

        Entity best     = Entity.Null;
        float  bestVal  = float.MaxValue;
        bool   maximize = ct.TargetingMode == TargetPriority.HighestPriority;

        if (maximize) bestVal = float.MinValue;

        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (candidate == attacker) continue;
            if (health.IsDead) continue;

            // Skip friendlies.
            var candidateCt = world.GetComponent<CombatTargetComponent>(candidate);
            if (candidateCt != null && candidateCt.Faction == ct.Faction) continue;

            var candidatePos = world.GetComponent<TransformComponent>(candidate)?.Position ?? Vector3.Zero;
            float dist       = (candidatePos - attackerPos).Length;
            if (dist > range) continue;

            float score = ct.TargetingMode switch
            {
                TargetPriority.Closest         => dist,
                TargetPriority.LowestHP        => health.CurrentHP,
                TargetPriority.HighestPriority => candidateCt?.Priority ?? 0,
                _                              => dist,
            };

            bool isBetter = maximize ? score > bestVal : score < bestVal;
            if (isBetter)
            {
                bestVal = score;
                best    = candidate;
            }
        }

        return best;
    }

    // ── Firing ────────────────────────────────────────────────────────────────

    private void Fire(
        World world, Entity attacker, CombatTargetComponent ct,
        WeaponComponent weapon, Vector3 attackerPos, Vector3 targetPos)
    {
        ProjectileType pType = weapon.ProjectileType switch
        {
            "homing"  => ProjectileType.Homing,
            "missile" => ProjectileType.Homing,
            "aoe"     => ProjectileType.AoE,
            "instant" => ProjectileType.Instant,
            _         => ProjectileType.Linear,
        };

        if (pType == ProjectileType.Instant)
        {
            ApplyInstantDamage(world, attacker, ct.CurrentTarget, weapon.Damage);
            return;
        }

        // Spawn a projectile entity.
        Entity proj = world.CreateEntity();

        var dir = (targetPos - attackerPos);
        if (dir.LengthSquared > 0f) dir = Vector3.Normalize(dir);

        world.AddComponent(proj, new TransformComponent { Position = attackerPos });
        world.AddComponent(proj, new ProjectileComponent
        {
            Owner         = attacker,
            Target        = ct.CurrentTarget,
            Type          = pType,
            Damage        = weapon.Damage,
            Speed         = 400f,
            Lifetime      = 5f,
            Direction     = dir,
            OwnerFaction  = ct.Faction,
        });
    }

    private void ApplyInstantDamage(World world, Entity attacker, Entity target, float baseDamage)
    {
        var health = world.GetComponent<HealthComponent>(target);
        if (health == null) return;

        float final = DamageCalculator.Apply(baseDamage, health);
        _bus.Publish(new DamageDealtEvent(attacker.Index, target.Index, baseDamage, final));
    }

    // ── Death processing ──────────────────────────────────────────────────────

    private void ProcessDeaths(World world)
    {
        // Collect dead entities first to avoid mutating during iteration.
        var dead = new List<(Entity entity, HealthComponent health)>();
        foreach (var (e, hp) in world.Query<HealthComponent>())
        {
            if (hp.IsDead) dead.Add((e, hp));
        }

        foreach (var (entity, _) in dead)
        {
            // Award XP to hero if one exists.
            Entity killer = FindKiller(world, entity);
            int xp        = BaseXpPerKill;

            if (world.IsAlive(killer))
            {
                var heroComp = world.GetComponent<HeroComponent>(killer);
                if (heroComp != null)
                    heroComp.XP += xp;
            }

            _bus.Publish(new UnitDiedEvent(entity.Index, killer.Index, xp));
            world.DestroyEntity(entity);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the most recent attacker of <paramref name="victim"/> by scanning projectiles,
    /// or <see cref="Entity.Null"/> if none can be identified.
    /// </summary>
    private static Entity FindKiller(World world, Entity victim)
    {
        // Walk all combat targets to find an entity currently targeting the victim.
        foreach (var (candidate, ct) in world.Query<CombatTargetComponent>())
        {
            if (ct.CurrentTarget == victim) return candidate;
        }
        return Entity.Null;
    }
}
