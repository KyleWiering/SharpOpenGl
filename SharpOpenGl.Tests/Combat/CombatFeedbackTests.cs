using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Combat;

public class CombatFeedbackTests
{
    [Fact]
    public void Damage_dealt_triggers_hit_flash_on_rendered_target()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatFeedbackSystem(bus));
        world.AddSystem(new ProjectileSystem(bus));
        world.AddSystem(new CombatSystem(bus));

        Entity attacker = world.CreateEntity();
        world.AddComponent(attacker, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(attacker, new CombatTargetComponent { Faction = 1 });
        var wl = new WeaponListComponent();
        wl.Weapons.Add(new WeaponComponent
        {
            Slot = 0, Type = "laser", Damage = 10f, Range = 500f,
            FireRate = 2f, ProjectileType = "instant",
        });
        world.AddComponent(attacker, wl);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(5f, 0f, 0f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });
        world.AddComponent(target, new RenderComponent
        {
            Color = new Vector4(0.4f, 0.4f, 0.45f, 1f),
            Visible = true,
        });

        world.GetComponent<CombatTargetComponent>(attacker)!.CurrentTarget = target;

        world.Update(0.001f);

        Assert.True(world.HasComponent<HitFlashComponent>(target));
        var render = world.GetComponent<RenderComponent>(target)!;
        Assert.True(render.Color.X > 0.5f || render.Color.Y > 0.35f);
    }

    [Fact]
    public void Hit_flash_restores_base_color_after_duration()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatFeedbackSystem(bus));

        Entity target = world.CreateEntity();
        var baseColor = new Vector4(0.3f, 0.35f, 0.4f, 1f);
        world.AddComponent(target, new RenderComponent { Color = baseColor, Visible = true });
        CombatFeedbackSystem.TriggerHitFlash(world, target);

        world.Update(0.25f);

        Assert.False(world.HasComponent<HitFlashComponent>(target));
        Assert.Equal(baseColor, world.GetComponent<RenderComponent>(target)!.Color);
    }

    [Fact]
    public void Projectile_spawn_includes_weapon_colored_trail_emitter()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatSystem(bus));

        Entity attacker = world.CreateEntity();
        world.AddComponent(attacker, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(attacker, new CombatTargetComponent { Faction = 1 });
        var wl = new WeaponListComponent();
        wl.Weapons.Add(new WeaponComponent
        {
            Slot = 0, Type = "cannon", Damage = 10f, Range = 500f,
            FireRate = 2f, ProjectileType = "linear",
        });
        world.AddComponent(attacker, wl);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(10f, 0f, 0f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });
        world.GetComponent<CombatTargetComponent>(attacker)!.CurrentTarget = target;

        world.Update(0.6f);

        bool foundTrail = false;
        foreach (var (entity, _) in world.Query<ProjectileComponent>())
        {
            var emitter = world.GetComponent<ParticleEmitterComponent>(entity);
            Assert.NotNull(emitter);
            Assert.True(emitter!.Emitter.StartColor.Z > 0.8f);
            foundTrail = true;
        }

        Assert.True(foundTrail);
    }

    [Fact]
    public void Shield_depletion_publishes_shield_break_ring_event()
    {
        var bus = new EventBus();
        CombatRingVfxEvent? ring = null;
        bus.Subscribe<CombatRingVfxEvent>(e => ring = e);

        var world = new World();
        Entity attacker = world.CreateEntity();
        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(4f, 0f, 2f) });
        world.AddComponent(target, new HealthComponent
        {
            MaxHP = 100f,
            CurrentHP = 100f,
            MaxShields = 20f,
            CurrentShields = 10f,
        });

        CombatDamagePublisher.ApplyAndPublish(world, bus, attacker, target, 15f);

        Assert.NotNull(ring);
        Assert.Equal(CombatRingVfxKind.ShieldBreak, ring!.Kind);
        Assert.Equal(0f, world.GetComponent<HealthComponent>(target)!.CurrentShields);
    }

    [Fact]
    public void Race_ultimate_activation_triggers_cast_flash_on_caster()
    {
        RaceUltimateSchema.ResetForTests();
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatFeedbackSystem(bus));

        Entity caster = world.CreateEntity();
        world.AddComponent(caster, new RenderComponent
        {
            Color = new Vector4(0.4f, 0.4f, 0.45f, 1f),
            Visible = true,
        });
        world.AddComponent(caster, new RaceComponent { RaceId = "terran" });

        world.Update(0.001f);
        bus.Publish(new AbilityActivatedEvent(caster.Index, 2, "terran_orbital_salvo"));

        Assert.True(world.HasComponent<CastFlashComponent>(caster));
    }

    [Fact]
    public void Combat_system_publishes_death_expand_ring_on_ship_destroy()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatFeedbackSystem(bus));
        world.AddSystem(new ProjectileSystem(bus));
        world.AddSystem(new CombatSystem(bus));

        Entity attacker = world.CreateEntity();
        world.AddComponent(attacker, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(attacker, new CombatTargetComponent { Faction = 1 });
        var wl = new WeaponListComponent();
        wl.Weapons.Add(new WeaponComponent
        {
            Slot = 0, Type = "laser", Damage = 50f, Range = 500f,
            FireRate = 2f, ProjectileType = "instant",
        });
        world.AddComponent(attacker, wl);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(5f, 0f, 0f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 1f, CurrentHP = 1f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        CombatRingVfxEvent? ring = null;
        bus.Subscribe<CombatRingVfxEvent>(e => ring = e);
        world.GetComponent<CombatTargetComponent>(attacker)!.CurrentTarget = target;

        for (int i = 0; i < 30 && ring == null; i++)
            world.Update(0.05f);

        Assert.NotNull(ring);
        Assert.Equal(CombatRingVfxKind.DeathExpand, ring!.Kind);
    }

    [Fact]
    public void Non_ultimate_ability_does_not_trigger_cast_flash()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatFeedbackSystem(bus));

        Entity caster = world.CreateEntity();
        world.AddComponent(caster, new RenderComponent
        {
            Color = new Vector4(0.4f, 0.4f, 0.45f, 1f),
            Visible = true,
        });

        bus.Publish(new AbilityActivatedEvent(caster.Index, 0, "shield_boost"));

        Assert.False(world.HasComponent<CastFlashComponent>(caster));
    }

    [Fact]
    public void Heavy_damage_triggers_hp_bar_pulse_component()
    {
        var world = new World();
        Entity target = world.CreateEntity();
        world.AddComponent(target, new HealthComponent { MaxHP = 200f, CurrentHP = 200f });

        CombatFeedbackSystem.TriggerHpBarPulse(world, target);

        Assert.True(world.HasComponent<HpBarPulseComponent>(target));
        Assert.True(world.GetComponent<HpBarPulseComponent>(target)!.Intensity > 0f);
    }

    [Fact]
    public void Damage_dealt_event_triggers_hp_bar_pulse_on_heavy_hit()
    {
        var bus = new EventBus();
        var world = new World();
        var feedback = new CombatFeedbackSystem(bus);
        world.AddSystem(feedback);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new HealthComponent { MaxHP = 200f, CurrentHP = 200f });
        world.Update(0.001f);

        CombatFeedbackSystem.TriggerHitFlash(world, target);
        Assert.False(world.HasComponent<HpBarPulseComponent>(target));

        CombatFeedbackSystem.TriggerHpBarPulse(world, target);
        Assert.True(world.HasComponent<HpBarPulseComponent>(target));
    }

    [Fact]
    public void Light_damage_does_not_trigger_hp_bar_pulse()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatFeedbackSystem(bus));

        Entity target = world.CreateEntity();
        world.AddComponent(target, new HealthComponent { MaxHP = 500f, CurrentHP = 500f });

        world.Update(0.001f);
        bus.Publish(new DamageDealtEvent(1, target.Index, 10f, 8f));

        Assert.False(world.HasComponent<HpBarPulseComponent>(target));
    }

    [Theory]
    [InlineData(200f, 40f, true)]
    [InlineData(500f, 8f, false)]
    [InlineData(100f, 20f, true)]
    public void IsHeavyHit_uses_fraction_and_minimum_threshold(float maxHp, float damage, bool expected)
    {
        var health = new HealthComponent { MaxHP = maxHp, CurrentHP = maxHp };
        Assert.Equal(expected, CombatFeedbackSystem.IsHeavyHit(damage, health));
    }

    [Fact]
    public void Ultimate_cast_tint_comes_from_race_ultimates_json()
    {
        RaceUltimateSchema.ResetForTests();

        Vector3 terran = CombatFeedbackSystem.ResolveUltimateCastTint("terran", "terran_orbital_salvo");
        Vector3 cryo = CombatFeedbackSystem.ResolveUltimateCastTint("cryo", "cryo_freeze_field");

        Assert.Equal(0.95f, terran.X, 2);
        Assert.Equal(0.42f, terran.Y, 2);
        Assert.NotEqual(terran, cryo);
        Assert.True(cryo.Z > cryo.X);
    }

    [Theory]
    [InlineData(WeaponVisualKind.LaserBolt, 0.25f, 0.95f)]
    [InlineData(WeaponVisualKind.Rocket, 1f, 0.52f)]
    [InlineData(WeaponVisualKind.EnergyPulse, 0.82f, 0.32f)]
    public void Trail_colors_are_distinct_per_weapon_kind(
        WeaponVisualKind kind, float expectedR, float expectedG)
    {
        var color = WeaponProfiles.TrailColor(kind);
        Assert.Equal(expectedR, color.X, 2);
        Assert.Equal(expectedG, color.Y, 2);
    }

    [Theory]
    [InlineData(WeaponVisualKind.Torpedo, 0.42f, 0.88f)]
    [InlineData(WeaponVisualKind.Rocket, 1f, 0.34f)]
    public void Homing_trail_colors_differ_from_linear_missiles(
        WeaponVisualKind kind, float expectedR, float expectedG)
    {
        var linear = WeaponProfiles.TrailColor(kind, ProjectileType.Linear);
        var homing = WeaponProfiles.TrailColor(kind, ProjectileType.Homing);

        Assert.Equal(expectedR, homing.X, 2);
        Assert.Equal(expectedG, homing.Y, 2);
        Assert.NotEqual(linear, homing);
    }

    [Fact]
    public void Homing_trail_steer_tint_warms_as_lifetime_elapses()
    {
        var baseTint = WeaponProfiles.TrailColor(WeaponVisualKind.Torpedo, ProjectileType.Homing);
        var early = WeaponProfiles.HomingTrailSteerTint(baseTint, lifetimeRatio: 0.9f, WeaponVisualKind.Torpedo);
        var late = WeaponProfiles.HomingTrailSteerTint(baseTint, lifetimeRatio: 0.1f, WeaponVisualKind.Torpedo);

        Assert.True(late.X > early.X);
        Assert.True(late.Z < early.Z);
    }
}