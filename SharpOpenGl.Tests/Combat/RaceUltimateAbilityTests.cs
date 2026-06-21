using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Combat;

/// <summary>Tests for race ultimate combat effects in <see cref="AbilitySystem"/>.</summary>
public class RaceUltimateAbilityTests
{
    private static (World world, EventBus bus, AbilitySystem system) MakeWorld()
    {
        RaceUltimateSchema.ResetForTests();
        var bus = new EventBus();
        var system = new AbilitySystem(bus);
        var world = new World();
        world.AddSystem(system);
        world.AddSystem(new ProjectileSystem(bus));
        return (world, bus, system);
    }

    private static Entity MakeHero(World world, int faction, Vector3 pos, string ultimateId, float cooldown = 0f)
    {
        Entity hero = world.CreateEntity();
        world.AddComponent(hero, new TransformComponent { Position = pos });
        world.AddComponent(hero, new HealthComponent { MaxHP = 500f, CurrentHP = 500f, MaxShields = 100f, CurrentShields = 50f });
        world.AddComponent(hero, new HeroComponent());
        world.AddComponent(hero, new RaceComponent { RaceId = ultimateId.Split('_')[0] });
        world.AddComponent(hero, new CombatTargetComponent { Faction = faction });

        var al = new AbilityListComponent();
        al.Abilities.Add(new AbilityComponent
        {
            Slot = RaceUltimatePolicy.UltimateSlot,
            Id = ultimateId,
            MaxCooldown = cooldown > 0f ? cooldown : 90f,
            CurrentCooldown = 0f,
        });
        world.AddComponent(hero, al);
        return hero;
    }

    private static Entity MakeEnemy(World world, int faction, Vector3 pos, float hp = 200f)
    {
        Entity enemy = world.CreateEntity();
        world.AddComponent(enemy, new TransformComponent { Position = pos });
        world.AddComponent(enemy, new HealthComponent { MaxHP = hp, CurrentHP = hp, Armor = 0f });
        world.AddComponent(enemy, new CombatTargetComponent { Faction = faction, CurrentTarget = Entity.Null });
        return enemy;
    }

    [Fact]
    public void Solari_solar_nova_deals_aoe_damage_at_caster()
    {
        var (world, bus, system) = MakeWorld();
        Entity hero = MakeHero(world, faction: 1, Vector3.Zero, "solari_solar_nova");
        Entity near = MakeEnemy(world, faction: 2, new Vector3(10f, 0, 0), hp: 300f);
        Entity far = MakeEnemy(world, faction: 2, new Vector3(200f, 0, 0), hp: 300f);

        var damageEvents = new List<DamageDealtEvent>();
        bus.Subscribe<DamageDealtEvent>(e => damageEvents.Add(e));

        system.ActivateAbility(hero, RaceUltimatePolicy.UltimateSlot);
        world.Update(0f);

        Assert.Contains(damageEvents, e => e.TargetId == near.Index);
        Assert.DoesNotContain(damageEvents, e => e.TargetId == far.Index);
        Assert.True(world.GetComponent<HealthComponent>(near)!.CurrentHP < 300f);
    }

    [Fact]
    public void Vesper_precision_beam_deals_single_target_damage()
    {
        var (world, bus, system) = MakeWorld();
        Entity hero = MakeHero(world, faction: 1, Vector3.Zero, "vesper_precision_beam");
        Entity target = MakeEnemy(world, faction: 2, new Vector3(15f, 0, 0), hp: 500f);
        world.GetComponent<CombatTargetComponent>(hero)!.CurrentTarget = target;

        var damageEvents = new List<DamageDealtEvent>();
        bus.Subscribe<DamageDealtEvent>(e => damageEvents.Add(e));

        system.ActivateAbility(hero, RaceUltimatePolicy.UltimateSlot);
        world.Update(0f);

        Assert.Single(damageEvents);
        Assert.Equal(target.Index, damageEvents[0].TargetId);
        Assert.True(world.GetComponent<HealthComponent>(target)!.CurrentHP < 500f);
    }

    [Fact]
    public void Cryo_freeze_field_disables_enemies_in_radius()
    {
        var (world, _, system) = MakeWorld();
        Entity hero = MakeHero(world, faction: 1, Vector3.Zero, "cryo_freeze_field");
        Entity enemy = MakeEnemy(world, faction: 2, new Vector3(12f, 0, 0));
        world.AddComponent(enemy, new MovementComponent { Speed = 100f, PathTarget = new Vector3(50f, 0, 0) });

        system.ActivateAbility(hero, RaceUltimatePolicy.UltimateSlot);
        world.Update(0f);

        var disabled = world.GetComponent<DisabledComponent>(enemy);
        Assert.NotNull(disabled);
        Assert.True(disabled!.IsActive);

        var movement = new MovementSystem();
        movement.Update(world, 0.1f);
        Assert.Null(world.GetComponent<MovementComponent>(enemy)!.PathTarget);
    }

    [Fact]
    public void Terran_orbital_salvo_spawns_race_colored_aoe_projectiles()
    {
        var (world, _, system) = MakeWorld();
        Entity hero = MakeHero(world, faction: 1, Vector3.Zero, "terran_orbital_salvo");
        Entity target = MakeEnemy(world, faction: 2, new Vector3(30f, 0, 0));
        world.GetComponent<CombatTargetComponent>(hero)!.CurrentTarget = target;

        system.ActivateAbility(hero, RaceUltimatePolicy.UltimateSlot);
        world.Update(0f);

        bool foundProjectile = false;
        foreach (var (entity, proj) in world.Query<ProjectileComponent>())
        {
            if (proj.Type != ProjectileType.AoE) continue;
            var render = world.GetComponent<RenderComponent>(entity);
            Assert.NotNull(render);
            Assert.True(render!.Color.X > 0.5f);
            foundProjectile = true;
        }

        Assert.True(foundProjectile);
    }

    [Fact]
    public void Ultimate_activation_publishes_event_and_respects_cooldown()
    {
        var (world, bus, system) = MakeWorld();
        Entity hero = MakeHero(world, faction: 1, Vector3.Zero, "nexar_swarm_strike", cooldown: 60f);
        Entity target = MakeEnemy(world, faction: 2, new Vector3(20f, 0, 0));
        world.GetComponent<CombatTargetComponent>(hero)!.CurrentTarget = target;

        AbilityActivatedEvent? activated = null;
        bus.Subscribe<AbilityActivatedEvent>(e => activated = e);

        system.ActivateAbility(hero, RaceUltimatePolicy.UltimateSlot);
        world.Update(0f);

        Assert.NotNull(activated);
        Assert.Equal("nexar_swarm_strike", activated!.AbilityId);

        system.ActivateAbility(hero, RaceUltimatePolicy.UltimateSlot);
        var secondEvents = new List<AbilityActivatedEvent>();
        bus.Subscribe<AbilityActivatedEvent>(e => secondEvents.Add(e));
        world.Update(0f);
        Assert.Empty(secondEvents);
    }

    [Fact]
    public void UseAbilityCommand_activates_ultimate_by_id()
    {
        var (world, bus, system) = MakeWorld();
        Entity hero = MakeHero(world, faction: 1, Vector3.Zero, "voidborn_gravity_rift");
        Entity enemy = MakeEnemy(world, faction: 2, new Vector3(8f, 0, 0));
        world.GetComponent<CombatTargetComponent>(hero)!.CurrentTarget = enemy;

        AbilityActivatedEvent? activated = null;
        bus.Subscribe<AbilityActivatedEvent>(e => activated = e);

        system.HandleUseAbility(world, new UseAbilityCommand
        {
            PlayerId = 1,
            Tick = 1,
            SourceEntityId = hero.Index,
            AbilityId = "voidborn_gravity_rift",
        });
        world.Update(0f);

        Assert.NotNull(activated);
        Assert.NotNull(world.GetComponent<DisabledComponent>(enemy));
    }
}