using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using Xunit;

namespace SharpOpenGl.Tests.Combat;

/// <summary>
/// Unit tests for <see cref="DamageCalculator"/>, <see cref="CombatSystem"/>,
/// <see cref="ProjectileSystem"/>, and <see cref="AbilitySystem"/>.
/// </summary>
public class CombatSystemTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // DamageCalculator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DamageCalculator_no_armor_no_shields_deals_full_damage()
    {
        var health = new HealthComponent { MaxHP = 100f, CurrentHP = 100f, Armor = 0f };
        float final = DamageCalculator.Apply(20f, health);
        Assert.Equal(20f, final, 4);
        Assert.Equal(80f, health.CurrentHP, 4);
    }

    [Fact]
    public void DamageCalculator_armor_reduces_damage_by_formula()
    {
        // finalDamage = 100 × (100 / (100 + 100)) = 50
        var health = new HealthComponent { MaxHP = 200f, CurrentHP = 200f, Armor = 100f };
        float final = DamageCalculator.Apply(100f, health);
        Assert.Equal(50f, final, 4);
        Assert.Equal(150f, health.CurrentHP, 4);
    }

    [Fact]
    public void DamageCalculator_shields_absorb_damage_before_hp()
    {
        var health = new HealthComponent
        {
            MaxHP = 100f, CurrentHP = 100f,
            MaxShields = 50f, CurrentShields = 50f,
            Armor = 0f
        };
        // 30 damage absorbed entirely by shields
        float final = DamageCalculator.Apply(30f, health);
        Assert.Equal(0f, final);           // nothing hit HP
        Assert.Equal(20f, health.CurrentShields);
        Assert.Equal(100f, health.CurrentHP);
    }

    [Fact]
    public void DamageCalculator_damage_bleeds_through_depleted_shields()
    {
        var health = new HealthComponent
        {
            MaxHP = 100f, CurrentHP = 100f,
            MaxShields = 10f, CurrentShields = 10f,
            Armor = 0f
        };
        // 30 damage: 10 absorbed by shields, 20 hits HP
        DamageCalculator.Apply(30f, health);
        Assert.Equal(0f,  health.CurrentShields);
        Assert.Equal(80f, health.CurrentHP, 4);
    }

    [Fact]
    public void DamageCalculator_minimum_damage_is_always_one()
    {
        // Extreme armor — should still deal 1 damage
        var health = new HealthComponent { MaxHP = 100f, CurrentHP = 100f, Armor = 9999f };
        float final = DamageCalculator.Apply(1f, health);
        Assert.Equal(1f, final);
        Assert.Equal(99f, health.CurrentHP);
    }

    [Fact]
    public void DamageCalculator_does_nothing_to_dead_entity()
    {
        var health = new HealthComponent { MaxHP = 100f, CurrentHP = 0f };
        float final = DamageCalculator.Apply(50f, health);
        Assert.Equal(0f, final);
        Assert.Equal(0f, health.CurrentHP);
    }

    [Fact]
    public void DamageCalculator_hp_cannot_go_below_zero()
    {
        var health = new HealthComponent { MaxHP = 100f, CurrentHP = 5f, Armor = 0f };
        DamageCalculator.Apply(100f, health);
        Assert.Equal(0f, health.CurrentHP);
    }

    [Fact]
    public void DamageCalculator_preview_matches_apply_when_no_shields()
    {
        float preview = DamageCalculator.Preview(50f, armor: 50f, currentShields: 0f);
        var   health  = new HealthComponent { MaxHP = 200f, CurrentHP = 200f, Armor = 50f };
        float actual  = DamageCalculator.Apply(50f, health);
        Assert.Equal(preview, actual, 4);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CombatSystem — target acquisition
    // ─────────────────────────────────────────────────────────────────────────

    private static (World world, EventBus bus, CombatSystem system) MakeCombatWorld()
    {
        var bus    = new EventBus();
        var system = new CombatSystem(bus);
        var world  = new World();
        world.AddSystem(system);
        return (world, bus, system);
    }

    private static Entity MakeFighter(World world, int faction, Vector3 pos, float range = 200f,
        float hp = 100f, TargetPriority targeting = TargetPriority.Closest, int priority = 10)
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent { Position = pos });
        world.AddComponent(e, new HealthComponent { MaxHP = hp, CurrentHP = hp, Armor = 0f });
        world.AddComponent(e, new CombatTargetComponent
        {
            Faction       = faction,
            TargetingMode = targeting,
            Priority      = priority,
        });
        var wl = new WeaponListComponent();
        wl.Weapons.Add(new WeaponComponent
        {
            Slot = 0, Type = "laser", Damage = 10f, Range = range,
            FireRate = 2f, ProjectileType = "instant"
        });
        world.AddComponent(e, wl);
        return e;
    }

    [Fact]
    public void CombatSystem_attacker_acquires_nearest_enemy()
    {
        var (world, _, _) = MakeCombatWorld();
        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f);
        Entity near     = MakeFighter(world, faction: 2, pos: new Vector3(10f, 0, 0));
        Entity far      = MakeFighter(world, faction: 2, pos: new Vector3(400f, 0, 0));

        world.Update(0.001f); // small dt so nobody dies yet

        var ct = world.GetComponent<CombatTargetComponent>(attacker)!;
        Assert.Equal(near, ct.CurrentTarget);
    }

    [Fact]
    public void CombatSystem_attacker_ignores_friendly_units()
    {
        var (world, _, _) = MakeCombatWorld();
        Entity attacker  = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f);
        Entity friendly  = MakeFighter(world, faction: 1, pos: new Vector3(5f, 0, 0));
        Entity enemy     = MakeFighter(world, faction: 2, pos: new Vector3(100f, 0, 0));

        world.Update(0.001f);

        var ct = world.GetComponent<CombatTargetComponent>(attacker)!;
        Assert.Equal(enemy, ct.CurrentTarget);
    }

    [Fact]
    public void CombatSystem_no_target_when_no_enemies_in_range()
    {
        var (world, _, _) = MakeCombatWorld();
        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 50f);
        MakeFighter(world, faction: 2, pos: new Vector3(200f, 0, 0));

        world.Update(0.001f);

        var ct = world.GetComponent<CombatTargetComponent>(attacker)!;
        Assert.Equal(Entity.Null, ct.CurrentTarget);
    }

    [Fact]
    public void CombatSystem_lowest_hp_targeting_picks_weakest_enemy()
    {
        var (world, _, _) = MakeCombatWorld();
        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f,
            targeting: TargetPriority.LowestHP);
        Entity strong = MakeFighter(world, faction: 2, pos: new Vector3(10f, 0, 0), hp: 100f);
        Entity weak   = MakeFighter(world, faction: 2, pos: new Vector3(20f, 0, 0), hp: 10f);

        world.Update(0.001f);

        var ct = world.GetComponent<CombatTargetComponent>(attacker)!;
        Assert.Equal(weak, ct.CurrentTarget);
    }

    [Fact]
    public void CombatSystem_highest_priority_targeting_picks_high_value_target()
    {
        var (world, _, _) = MakeCombatWorld();
        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f,
            targeting: TargetPriority.HighestPriority);
        Entity low  = MakeFighter(world, faction: 2, pos: new Vector3(10f, 0, 0), priority: 5);
        Entity high = MakeFighter(world, faction: 2, pos: new Vector3(50f, 0, 0), priority: 100);

        world.Update(0.001f);

        var ct = world.GetComponent<CombatTargetComponent>(attacker)!;
        Assert.Equal(high, ct.CurrentTarget);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CombatSystem — instant damage and unit death
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CombatSystem_instant_weapon_deals_damage_per_frame()
    {
        var (world, bus, _) = MakeCombatWorld();

        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f);
        Entity target   = MakeFighter(world, faction: 2, pos: new Vector3(10f, 0, 0));

        var received = new List<DamageDealtEvent>();
        bus.Subscribe<DamageDealtEvent>(e => received.Add(e));

        // Tick with deltaTime large enough to trigger fire (1s = 2 shots at FireRate 2)
        world.Update(1f);

        // At least one hit should have landed on the target (both entities shoot each other).
        Assert.Contains(received, ev => ev.TargetId == target.Index);
    }

    [Fact]
    public void CombatSystem_dead_entity_is_removed_from_world()
    {
        var (world, bus, _) = MakeCombatWorld();

        // Give target 1 HP so a single shot kills it.
        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f);
        Entity target   = MakeFighter(world, faction: 2, pos: new Vector3(5f, 0, 0), hp: 1f);

        world.Update(1f);

        Assert.False(world.IsAlive(target));
    }

    [Fact]
    public void CombatSystem_unit_died_event_is_published()
    {
        var (world, bus, _) = MakeCombatWorld();
        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f);
        Entity target   = MakeFighter(world, faction: 2, pos: new Vector3(5f, 0, 0), hp: 1f);

        UnitDiedEvent? diedEvent = null;
        bus.Subscribe<UnitDiedEvent>(e => diedEvent = e);

        world.Update(1f);

        Assert.NotNull(diedEvent);
        Assert.Equal(target.Index, diedEvent!.VictimId);
    }

    [Fact]
    public void CombatSystem_hero_gains_xp_on_kill()
    {
        var (world, _, system) = MakeCombatWorld();
        system.BaseXpPerKill = 50;

        Entity hero = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f);
        world.AddComponent(hero, new HeroComponent { Level = 1, XP = 0 });
        Entity target = MakeFighter(world, faction: 2, pos: new Vector3(5f, 0, 0), hp: 1f);

        world.Update(1f);

        var heroComp = world.GetComponent<HeroComponent>(hero)!;
        Assert.Equal(50, heroComp.XP);
    }

    [Fact]
    public void CombatSystem_target_is_dropped_when_it_dies()
    {
        var (world, _, _) = MakeCombatWorld();
        Entity attacker = MakeFighter(world, faction: 1, pos: Vector3.Zero, range: 500f);
        MakeFighter(world, faction: 2, pos: new Vector3(5f, 0, 0), hp: 1f);

        world.Update(1f); // target dies and is destroyed
        world.Update(0f); // stale target reference is cleared

        var ct = world.GetComponent<CombatTargetComponent>(attacker);
        // attacker may still be alive; its current target should be null now
        if (ct != null)
            Assert.Equal(Entity.Null, ct.CurrentTarget);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ProjectileSystem
    // ─────────────────────────────────────────────────────────────────────────

    private static (World world, EventBus bus) MakeProjectileWorld()
    {
        var bus    = new EventBus();
        var system = new ProjectileSystem(bus);
        var world  = new World();
        world.AddSystem(system);
        return (world, bus);
    }

    [Fact]
    public void ProjectileSystem_linear_projectile_moves_in_direction()
    {
        var (world, _) = MakeProjectileWorld();

        Entity proj = world.CreateEntity();
        world.AddComponent(proj, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(proj, new ProjectileComponent
        {
            Type      = ProjectileType.Linear,
            Speed     = 100f,
            Lifetime  = 10f,
            Direction = Vector3.UnitX,
            Damage    = 0f,
        });

        world.Update(1f);

        var t = world.GetComponent<TransformComponent>(proj);
        Assert.NotNull(t);
        Assert.Equal(100f, t!.Position.X, 2);
    }

    [Fact]
    public void ProjectileSystem_projectile_expires_after_lifetime()
    {
        var (world, _) = MakeProjectileWorld();

        Entity proj = world.CreateEntity();
        world.AddComponent(proj, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(proj, new ProjectileComponent
        {
            Type     = ProjectileType.Linear,
            Speed    = 10f,
            Lifetime = 0.5f,
            Direction = Vector3.UnitX,
            Damage   = 0f,
        });

        world.Update(1f); // exceeds lifetime

        Assert.False(world.IsAlive(proj));
    }

    [Fact]
    public void ProjectileSystem_homing_projectile_steers_toward_target()
    {
        var (world, _) = MakeProjectileWorld();

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(0f, 0f, 100f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        Entity proj = world.CreateEntity();
        world.AddComponent(proj, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(proj, new ProjectileComponent
        {
            Type         = ProjectileType.Homing,
            Target       = target,
            Speed        = 200f,
            Lifetime     = 10f,
            Direction    = Vector3.UnitZ,
            Damage       = 10f,
            OwnerFaction = 1,
        });

        world.Update(0.5f);

        // Projectile should have moved toward target (z > 0).
        if (world.IsAlive(proj))
        {
            var t = world.GetComponent<TransformComponent>(proj)!;
            Assert.True(t.Position.Z > 0f, "Homing projectile should move toward target.");
        }
    }

    [Fact]
    public void ProjectileSystem_linear_projectile_hits_enemy_in_range()
    {
        var (world, bus) = MakeProjectileWorld();

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(10f, 0, 0) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        Entity proj = world.CreateEntity();
        world.AddComponent(proj, new TransformComponent { Position = new Vector3(9f, 0, 0) });
        world.AddComponent(proj, new ProjectileComponent
        {
            Type         = ProjectileType.Linear,
            Speed        = 100f,
            Lifetime     = 10f,
            Direction    = Vector3.UnitX,
            Damage       = 20f,
            OwnerFaction = 1,
        });

        var hits = new List<ProjectileHitEvent>();
        bus.Subscribe<ProjectileHitEvent>(e => hits.Add(e));

        world.Update(0.1f); // moves 10 world units, passing through target

        Assert.NotEmpty(hits);
    }

    [Fact]
    public void ProjectileSystem_friendly_fire_not_applied()
    {
        var (world, bus) = MakeProjectileWorld();

        Entity friendly = world.CreateEntity();
        world.AddComponent(friendly, new TransformComponent { Position = new Vector3(1f, 0, 0) });
        world.AddComponent(friendly, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(friendly, new CombatTargetComponent { Faction = 1 }); // same faction

        Entity proj = world.CreateEntity();
        world.AddComponent(proj, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(proj, new ProjectileComponent
        {
            Type         = ProjectileType.Linear,
            Speed        = 100f,
            Lifetime     = 10f,
            Direction    = Vector3.UnitX,
            Damage       = 50f,
            OwnerFaction = 1, // same faction
        });

        var hits = new List<ProjectileHitEvent>();
        bus.Subscribe<ProjectileHitEvent>(e => hits.Add(e));

        world.Update(0.5f);

        Assert.Empty(hits); // friendly fire blocked
        var hp = world.GetComponent<HealthComponent>(friendly)!;
        Assert.Equal(100f, hp.CurrentHP); // no damage
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AbilitySystem
    // ─────────────────────────────────────────────────────────────────────────

    private static (World world, EventBus bus, AbilitySystem abilitySystem) MakeAbilityWorld()
    {
        var bus    = new EventBus();
        var system = new AbilitySystem(bus);
        var world  = new World();
        world.AddSystem(system);
        return (world, bus, system);
    }

    private static Entity MakeHeroWithAbility(World world, string abilityId, float cooldown,
        int slot = 0)
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new HealthComponent
        {
            MaxHP = 500f, CurrentHP = 500f,
            MaxShields = 200f, CurrentShields = 50f,
        });
        var al = new AbilityListComponent();
        al.Abilities.Add(new AbilityComponent
        {
            Id = abilityId, Slot = slot,
            MaxCooldown = cooldown, CurrentCooldown = 0f,
        });
        world.AddComponent(e, al);
        return e;
    }

    [Fact]
    public void AbilitySystem_cooldown_ticks_down_each_frame()
    {
        var (world, _, abilitySystem) = MakeAbilityWorld();
        Entity hero = MakeHeroWithAbility(world, "shield_boost", cooldown: 30f);

        // Activate so cooldown starts.
        abilitySystem.ActivateAbility(hero, slot: 0);
        world.Update(0f);

        // Cooldown should be 30 s.
        var al = world.GetComponent<AbilityListComponent>(hero)!;
        Assert.Equal(30f, al.GetBySlot(0)!.CurrentCooldown, 2);

        world.Update(10f); // tick 10 s
        Assert.Equal(20f, al.GetBySlot(0)!.CurrentCooldown, 2);
    }

    [Fact]
    public void AbilitySystem_cannot_activate_on_cooldown()
    {
        var (world, bus, abilitySystem) = MakeAbilityWorld();
        Entity hero = MakeHeroWithAbility(world, "shield_boost", cooldown: 30f);

        abilitySystem.ActivateAbility(hero, slot: 0);
        world.Update(0f); // activates — now on cooldown

        // Try again while on cooldown.
        abilitySystem.ActivateAbility(hero, slot: 0);

        var events = new List<AbilityActivatedEvent>();
        bus.Subscribe<AbilityActivatedEvent>(e => events.Add(e));

        world.Update(0f); // should NOT activate again

        Assert.Empty(events);
    }

    [Fact]
    public void AbilitySystem_shield_boost_restores_half_max_shields()
    {
        var (world, _, abilitySystem) = MakeAbilityWorld();
        Entity hero = MakeHeroWithAbility(world, "shield_boost", cooldown: 30f);

        abilitySystem.ActivateAbility(hero, slot: 0);
        world.Update(0f);

        var health = world.GetComponent<HealthComponent>(hero)!;
        // restore = 200 * 0.5 = 100; was 50 → capped at MaxShields 200
        Assert.Equal(150f, health.CurrentShields, 2);
    }

    [Fact]
    public void AbilitySystem_shield_boost_does_not_exceed_max_shields()
    {
        var (world, _, abilitySystem) = MakeAbilityWorld();
        Entity hero = MakeHeroWithAbility(world, "shield_boost", cooldown: 30f);

        // Start with near-full shields.
        world.GetComponent<HealthComponent>(hero)!.CurrentShields = 190f;

        abilitySystem.ActivateAbility(hero, slot: 0);
        world.Update(0f);

        var health = world.GetComponent<HealthComponent>(hero)!;
        Assert.Equal(200f, health.CurrentShields, 2); // capped
    }

    [Fact]
    public void AbilitySystem_ability_activated_event_is_published()
    {
        var (world, bus, abilitySystem) = MakeAbilityWorld();
        Entity hero = MakeHeroWithAbility(world, "emp_burst", cooldown: 60f);

        AbilityActivatedEvent? evt = null;
        bus.Subscribe<AbilityActivatedEvent>(e => evt = e);

        abilitySystem.ActivateAbility(hero, slot: 0);
        world.Update(0f);

        Assert.NotNull(evt);
        Assert.Equal(hero.Index, evt!.CasterId);
        Assert.Equal("emp_burst", evt.AbilityId);
    }

    [Fact]
    public void AbilitySystem_emp_burst_clears_enemy_target()
    {
        var (world, _, abilitySystem) = MakeAbilityWorld();

        // Hero
        Entity hero = world.CreateEntity();
        var heroHealth = new HealthComponent { MaxHP = 500f, CurrentHP = 500f, MaxShields = 100f, CurrentShields = 50f };
        world.AddComponent(hero, heroHealth);
        var al = new AbilityListComponent();
        al.Abilities.Add(new AbilityComponent { Id = "emp_burst", Slot = 0, MaxCooldown = 60f });
        world.AddComponent(hero, al);

        // Enemy has hero as current target.
        Entity enemy = world.CreateEntity();
        var enemyCt = new CombatTargetComponent { Faction = 2, CurrentTarget = hero };
        world.AddComponent(enemy, enemyCt);
        world.AddComponent(enemy, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });

        // Hero has enemy as current target (so emp knows who to hit).
        var heroCt = new CombatTargetComponent { Faction = 1, CurrentTarget = enemy };
        world.AddComponent(hero, heroCt);

        abilitySystem.ActivateAbility(hero, slot: 0);
        world.Update(0f);

        Assert.Equal(Entity.Null, enemyCt.CurrentTarget);
    }
}
