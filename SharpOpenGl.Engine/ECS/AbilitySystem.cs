using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Multiplayer;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Ticks cooldowns on all <see cref="AbilityListComponent"/> entities each frame
/// and processes ability activations requested via <see cref="ActivateAbility"/>.
/// </summary>
/// <remarks>
/// Ability types implemented:
/// <list type="bullet">
///   <item><b>shield_boost</b> — instantly restores 50 % of the hero's max shields.</item>
///   <item><b>emp_burst</b>    — disables the hero's target for 3 s (clears its current target).</item>
///   <item>Race ultimates (slot 2) — AoE salvo, precision beam, or disable field per race.</item>
/// </list>
/// </remarks>
public sealed class AbilitySystem : GameSystem
{
    private readonly EventBus _bus;

    public AbilitySystem(EventBus bus) => _bus = bus;

    private readonly Queue<(Entity caster, int slot)> _pending = new();

    /// <summary>
    /// Request an ability activation for <paramref name="caster"/> on slot <paramref name="slot"/>.
    /// The activation is processed on the next <see cref="Update"/> call.
    /// </summary>
    public void ActivateAbility(Entity caster, int slot) =>
        _pending.Enqueue((caster, slot));

    /// <summary>Process a multiplayer <see cref="UseAbilityCommand"/>.</summary>
    public void HandleUseAbility(World world, UseAbilityCommand command)
    {
        Entity? caster = FindEntityByIndex(world, command.SourceEntityId);
        if (caster == null) return;

        var al = world.GetComponent<AbilityListComponent>(caster.Value);
        if (al == null) return;

        var ability = al.Abilities.Find(a =>
            string.Equals(a.Id, command.AbilityId, StringComparison.OrdinalIgnoreCase));
        if (ability == null) return;

        ActivateAbility(caster.Value, ability.Slot);
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        TickCooldowns(world, deltaTime);
        TickDisabled(world, deltaTime);
        ProcessPending(world);
    }

    private static void TickCooldowns(World world, float deltaTime)
    {
        foreach (var (_, al) in world.Query<AbilityListComponent>())
        {
            foreach (var ability in al.Abilities)
            {
                if (ability.CurrentCooldown > 0f)
                    ability.CurrentCooldown = Math.Max(0f, ability.CurrentCooldown - deltaTime);
            }
        }
    }

    private static void TickDisabled(World world, float deltaTime)
    {
        foreach (var (entity, disabled) in world.Query<DisabledComponent>())
        {
            disabled.RemainingSeconds = Math.Max(0f, disabled.RemainingSeconds - deltaTime);
            if (disabled.RemainingSeconds <= 0f)
                world.RemoveComponent<DisabledComponent>(entity);
        }
    }

    private void ProcessPending(World world)
    {
        while (_pending.TryDequeue(out var req))
        {
            var (caster, slot) = req;
            if (!world.IsAlive(caster)) continue;

            var al = world.GetComponent<AbilityListComponent>(caster);
            var ability = al?.GetBySlot(slot);
            if (ability == null || !ability.IsReady) continue;

            ability.Activate();
            ApplyAbilityEffect(world, caster, ability);
            _bus.Publish(new AbilityActivatedEvent(caster.Index, slot, ability.Id));
        }
    }

    private void ApplyAbilityEffect(World world, Entity caster, AbilityComponent ability)
    {
        switch (ability.Id)
        {
            case "shield_boost":
                ApplyShieldBoost(world, caster);
                break;
            case "emp_burst":
                ApplyEmpBurst(world, caster);
                break;
            default:
                if (RaceUltimateSchema.TryGetByAbilityId(ability.Id, out RaceUltimateDefinition? ultimate))
                    ApplyRaceUltimate(world, caster, ultimate);
                break;
        }
    }

    private void ApplyRaceUltimate(World world, Entity caster, RaceUltimateDefinition ultimate)
    {
        switch (ultimate.EffectType.ToLowerInvariant())
        {
            case "beam":
                ApplyPrecisionBeam(world, caster, ultimate);
                break;
            case "disable":
                ApplyDisableField(world, caster, ultimate);
                break;
            default:
                ApplyAoEUltimate(world, caster, ultimate);
                break;
        }
    }

    private static void ApplyShieldBoost(World world, Entity caster)
    {
        var health = world.GetComponent<HealthComponent>(caster);
        if (health == null) return;

        float restore = health.MaxShields * 0.5f;
        health.CurrentShields = Math.Min(health.MaxShields, health.CurrentShields + restore);
    }

    private static void ApplyEmpBurst(World world, Entity caster)
    {
        var ct = world.GetComponent<CombatTargetComponent>(caster);
        if (ct == null || !world.IsAlive(ct.CurrentTarget)) return;

        var targetCt = world.GetComponent<CombatTargetComponent>(ct.CurrentTarget);
        if (targetCt != null)
            targetCt.CurrentTarget = Entity.Null;

        ApplyDisable(world, ct.CurrentTarget, 3f);
    }

    private void ApplyPrecisionBeam(World world, Entity caster, RaceUltimateDefinition ultimate)
    {
        var ct = world.GetComponent<CombatTargetComponent>(caster);
        if (ct == null || !world.IsAlive(ct.CurrentTarget)) return;

        var casterPos = world.GetComponent<TransformComponent>(caster)?.Position ?? Vector3.Zero;
        var targetPos = world.GetComponent<TransformComponent>(ct.CurrentTarget)?.Position ?? casterPos;
        Vector4 color = ResolveRaceProjectileColor(world, caster);

        ApplyDirectDamage(world, caster, ct.CurrentTarget, ultimate.Damage);
        SpawnBeamFlash(world, casterPos, targetPos, color, 1.1f);
        _bus.Publish(new ExplosionVfxEvent(targetPos, ExplosionVfxKind.Impact, 1.2f));
    }

    private void ApplyAoEUltimate(World world, Entity caster, RaceUltimateDefinition ultimate)
    {
        var casterPos = world.GetComponent<TransformComponent>(caster)?.Position ?? Vector3.Zero;
        var ct = world.GetComponent<CombatTargetComponent>(caster);
        Vector3 targetPos = ct != null && world.IsAlive(ct.CurrentTarget)
            ? world.GetComponent<TransformComponent>(ct.CurrentTarget)?.Position ?? casterPos
            : casterPos;

        bool novaAtCaster = ultimate.AbilityId.Equals("solari_solar_nova", StringComparison.OrdinalIgnoreCase);
        Vector3 impactPos = novaAtCaster ? casterPos : targetPos;
        Vector4 color = ResolveRaceProjectileColor(world, caster);
        var visual = ParseVisual(ultimate.ProjectileVisual);
        int faction = world.GetComponent<CombatTargetComponent>(caster)?.Faction ?? 1;

        if (novaAtCaster || ultimate.ProjectileSpeed <= 0f)
        {
            SpawnAoEBurstVisual(world, impactPos, ultimate, color, visual);
            ApplyAoEDamage(world, caster, impactPos, ultimate.Damage, ultimate.AoeRadius, faction);
            return;
        }

        int salvo = Math.Max(1, ultimate.SalvoCount);
        for (int i = 0; i < salvo; i++)
        {
            float angle = salvo > 1 ? MathF.PI * 2f * i / salvo : 0f;
            float offset = salvo > 1 ? 8f : 0f;
            Vector3 spawnPos = casterPos + new Vector3(MathF.Cos(angle) * offset, 12f + i * 2f, MathF.Sin(angle) * offset);
            Entity target = ct?.CurrentTarget ?? Entity.Null;

            SpawnUltimateProjectile(world, caster, spawnPos, target, ultimate, color, visual, faction);
        }
    }

    private void ApplyDisableField(World world, Entity caster, RaceUltimateDefinition ultimate)
    {
        var casterPos = world.GetComponent<TransformComponent>(caster)?.Position ?? Vector3.Zero;
        var ct = world.GetComponent<CombatTargetComponent>(caster);
        bool atCaster = ultimate.AbilityId.Equals("cryo_freeze_field", StringComparison.OrdinalIgnoreCase);
        Vector3 center = atCaster
            ? casterPos
            : ct != null && world.IsAlive(ct.CurrentTarget)
                ? world.GetComponent<TransformComponent>(ct.CurrentTarget)?.Position ?? casterPos
                : casterPos;

        int faction = ct?.Faction ?? 1;
        Vector4 color = ResolveRaceProjectileColor(world, caster);
        var visual = ParseVisual(ultimate.ProjectileVisual);

        SpawnAoEBurstVisual(world, center, ultimate, color, visual);

        float radiusSq = ultimate.AoeRadius * ultimate.AoeRadius;
        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (health.IsDead) continue;
            var candidateCt = world.GetComponent<CombatTargetComponent>(candidate);
            if (candidateCt != null && candidateCt.Faction == faction) continue;

            var pos = world.GetComponent<TransformComponent>(candidate)?.Position ?? center;
            if ((pos - center).LengthSquared > radiusSq) continue;

            if (ultimate.Damage > 0f)
            {
                float final = DamageCalculator.Apply(ultimate.Damage, health);
                _bus.Publish(new DamageDealtEvent(caster.Index, candidate.Index, ultimate.Damage, final));
            }

            ApplyDisable(world, candidate, ultimate.DisableDuration);
        }

        _bus.Publish(new ExplosionVfxEvent(center, ExplosionVfxKind.Impact, MathF.Max(1f, ultimate.AoeRadius * 0.1f)));
    }

    private void ApplyAoEDamage(World world, Entity caster, Vector3 center, float damage, float radius, int faction)
    {
        if (radius <= 0f) return;

        float radiusSq = radius * radius;
        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (health.IsDead) continue;
            var candidateCt = world.GetComponent<CombatTargetComponent>(candidate);
            if (candidateCt != null && candidateCt.Faction == faction) continue;

            var pos = world.GetComponent<TransformComponent>(candidate)?.Position ?? center;
            if ((pos - center).LengthSquared > radiusSq) continue;

            float final = DamageCalculator.Apply(damage, health);
            _bus.Publish(new DamageDealtEvent(caster.Index, candidate.Index, damage, final));
        }

        _bus.Publish(new ExplosionVfxEvent(center, ExplosionVfxKind.Impact, MathF.Max(1f, radius * 0.12f)));
    }

    private void ApplyDirectDamage(World world, Entity caster, Entity target, float damage)
    {
        var health = world.GetComponent<HealthComponent>(target);
        if (health == null || health.IsDead) return;

        float final = DamageCalculator.Apply(damage, health);
        _bus.Publish(new DamageDealtEvent(caster.Index, target.Index, damage, final));
    }

    private static void ApplyDisable(World world, Entity target, float duration)
    {
        if (!world.IsAlive(target) || duration <= 0f) return;

        var ct = world.GetComponent<CombatTargetComponent>(target);
        if (ct != null)
            ct.CurrentTarget = Entity.Null;

        if (world.HasComponent<DisabledComponent>(target))
        {
            var existing = world.GetComponent<DisabledComponent>(target)!;
            existing.RemainingSeconds = Math.Max(existing.RemainingSeconds, duration);
            return;
        }

        world.AddComponent(target, new DisabledComponent { RemainingSeconds = duration });
    }

    private void SpawnUltimateProjectile(
        World world, Entity caster, Vector3 spawnPos, Entity target,
        RaceUltimateDefinition ultimate, Vector4 color, WeaponVisualKind visual, int faction)
    {
        Entity proj = world.CreateEntity();
        var dir = target != Entity.Null
            ? (world.GetComponent<TransformComponent>(target)?.Position ?? spawnPos) - spawnPos
            : Vector3.UnitZ;
        if (dir.LengthSquared > 0f) dir = Vector3.Normalize(dir);
        float yaw = MathHelper.RadiansToDegrees(MathF.Atan2(dir.X, dir.Z));

        world.AddComponent(proj, new TransformComponent
        {
            Position = spawnPos,
            Scale = Vector3.One * 1.4f,
            EulerAngles = new Vector3(0f, yaw, 0f),
        });
        world.AddComponent(proj, new ProjectileComponent
        {
            Owner = caster,
            Target = target,
            Type = ProjectileType.AoE,
            Damage = ultimate.Damage,
            Speed = ultimate.ProjectileSpeed,
            Lifetime = ultimate.ProjectileLifetime,
            MaxLifetime = ultimate.ProjectileLifetime,
            BlastRadius = ultimate.AoeRadius,
            Direction = dir,
            OwnerFaction = faction,
        });
        world.AddComponent(proj, new ProjectileVisualComponent { Visual = visual, Scale = 1.4f });
        world.AddComponent(proj, new RenderComponent
        {
            MeshKey = WeaponProfiles.MeshKey(visual),
            Color = color,
            Visible = true,
            PrimitiveType = visual == WeaponVisualKind.Wave ? 1 : 4,
        });
    }

    private static void SpawnAoEBurstVisual(
        World world, Vector3 center, RaceUltimateDefinition ultimate, Vector4 color, WeaponVisualKind visual)
    {
        Entity flash = world.CreateEntity();
        world.AddComponent(flash, new TransformComponent
        {
            Position = center with { Y = MathF.Max(center.Y, 1.5f) },
            Scale = Vector3.One * MathF.Max(1.5f, ultimate.AoeRadius * 0.08f),
        });
        world.AddComponent(flash, new ProjectileComponent
        {
            Type = ProjectileType.Linear,
            Damage = 0f,
            Speed = 0f,
            Lifetime = MathF.Max(0.2f, ultimate.ProjectileLifetime),
            MaxLifetime = MathF.Max(0.2f, ultimate.ProjectileLifetime),
            Direction = Vector3.UnitY,
        });
        world.AddComponent(flash, new ProjectileVisualComponent { Visual = visual, Scale = 1.6f });
        world.AddComponent(flash, new RenderComponent
        {
            MeshKey = WeaponProfiles.MeshKey(visual),
            Color = color,
            Visible = true,
            PrimitiveType = visual == WeaponVisualKind.Wave ? 1 : 4,
        });
    }

    private static void SpawnBeamFlash(World world, Vector3 start, Vector3 end, Vector4 color, float scale)
    {
        var dir = end - start;
        float length = dir.Length;
        if (length < 0.01f) return;
        dir = Vector3.Normalize(dir);
        float yaw = MathHelper.RadiansToDegrees(MathF.Atan2(dir.X, dir.Z));
        Vector3 mid = (start + end) * 0.5f;

        Entity flash = world.CreateEntity();
        world.AddComponent(flash, new TransformComponent
        {
            Position = mid with { Y = MathF.Max(mid.Y, 1.2f) },
            Scale = new Vector3(scale * 0.55f, scale * 0.55f, length * 0.55f),
            EulerAngles = new Vector3(0f, yaw, 0f),
        });
        world.AddComponent(flash, new ProjectileComponent
        {
            Type = ProjectileType.Linear,
            Damage = 0f,
            Speed = 0f,
            Lifetime = 0.15f,
            MaxLifetime = 0.15f,
            Direction = dir,
        });
        world.AddComponent(flash, new ProjectileVisualComponent
        {
            Visual = WeaponVisualKind.Beam,
            Scale = scale,
        });
        world.AddComponent(flash, new RenderComponent
        {
            MeshKey = WeaponProfiles.MeshKey(WeaponVisualKind.Beam),
            Color = color,
            Visible = true,
            PrimitiveType = 4,
        });
    }

    private static Vector4 ResolveRaceProjectileColor(World world, Entity caster)
    {
        string raceId = world.GetComponent<RaceComponent>(caster)?.RaceId ?? string.Empty;
        if (RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race))
        {
            float[] accent = race.Palette.Accent;
            if (accent.Length >= 3)
                return new Vector4(accent[0], accent[1], accent[2], 1f);
        }

        return new Vector4(0.6f, 0.85f, 1f, 1f);
    }

    private static WeaponVisualKind ParseVisual(string visual) => visual.ToLowerInvariant() switch
    {
        "beam" => WeaponVisualKind.Beam,
        "torpedo" => WeaponVisualKind.Torpedo,
        "rocket" => WeaponVisualKind.Rocket,
        "bomb" => WeaponVisualKind.Bomb,
        "energy_pulse" or "pulse" => WeaponVisualKind.EnergyPulse,
        "wave" => WeaponVisualKind.Wave,
        _ => WeaponVisualKind.LaserBolt,
    };

    private static Entity? FindEntityByIndex(World world, uint index)
    {
        foreach (var (entity, _) in world.Query<AbilityListComponent>())
        {
            if (entity.Index == index)
                return entity;
        }

        foreach (var (entity, _) in world.Query<HeroComponent>())
        {
            if (entity.Index == index)
                return entity;
        }

        return null;
    }
}