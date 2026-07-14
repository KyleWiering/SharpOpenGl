using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using Xunit;

namespace SharpOpenGl.Tests.Gameplay;

/// <summary>
/// Regression tests for peaceful sandbox station combat (no misfire on friendly non-combat units).
/// </summary>
public class SandboxStationCombatTests
{
    private static (World world, EventBus bus) MakePeacefulSandboxCombatWorld()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new StanceSystem());
        world.AddSystem(new CombatSystem(bus));
        world.AddSystem(new ProjectileSystem(bus));
        return (world, bus);
    }

    private static Entity MakeArmedStation(World world, Vector3 pos, Stance stance)
    {
        Entity station = world.CreateEntity();
        world.AddComponent(station, new TransformComponent { Position = pos });
        world.AddComponent(station, new HealthComponent { MaxHP = 2000f, CurrentHP = 2000f, Armor = 100f });
        world.AddComponent(station, new BuildingComponent { PlayerId = 1 });
        world.AddComponent(station, new CombatTargetComponent
        {
            Faction = 1,
            TargetingMode = TargetPriority.Closest,
            Priority = 50,
        });
        world.AddComponent(station, new StanceComponent { CurrentStance = stance });

        var wl = new WeaponListComponent();
        wl.Weapons.Add(new WeaponComponent
        {
            Slot = 0,
            Type = "energy_pulse",
            Damage = 45f,
            Range = 500f,
            FireRate = 2f,
            ProjectileType = "linear",
        });
        world.AddComponent(station, wl);
        return station;
    }

    private static Entity MakeFriendlyNonCombatUnit(World world, Vector3 pos)
    {
        Entity unit = world.CreateEntity();
        world.AddComponent(unit, new TransformComponent { Position = pos });
        world.AddComponent(unit, new HealthComponent { MaxHP = 100f, CurrentHP = 100f, Armor = 0f });
        return unit;
    }

    private static int CountProjectiles(World world)
    {
        int count = 0;
        foreach (var _ in world.Query<ProjectileComponent>())
            count++;
        return count;
    }

    [Fact]
    public void Peaceful_sandbox_stations_do_not_fire_at_friendly_non_combat_units()
    {
        var (world, _) = MakePeacefulSandboxCombatWorld();

        MakeArmedStation(world, new Vector3(-30f, 0f, -30f), Stance.Neutral);
        MakeArmedStation(world, new Vector3(50f, 0f, -50f), Stance.Neutral);
        MakeFriendlyNonCombatUnit(world, new Vector3(-20f, 1f, -25f));

        for (int tick = 0; tick < 30; tick++)
            world.Update(0.1f);

        Assert.Equal(0, CountProjectiles(world));

        world.Dispose();
    }

    [Fact]
    public void Neutral_stance_blocks_combat_system_target_acquisition()
    {
        var (world, _) = MakePeacefulSandboxCombatWorld();

        Entity station = MakeArmedStation(world, Vector3.Zero, Stance.Neutral);
        MakeFriendlyNonCombatUnit(world, new Vector3(10f, 0f, 0f));

        world.Update(0.1f);

        var ct = world.GetComponent<CombatTargetComponent>(station)!;
        Assert.Equal(Entity.Null, ct.CurrentTarget);
        Assert.Equal(0, CountProjectiles(world));

        world.Dispose();
    }

    [Fact]
    public void AcquireTarget_skips_health_entities_without_combat_target_unless_ai()
    {
        var bus = new EventBus();
        var world = new World();
        world.AddSystem(new CombatSystem(bus));
        world.AddSystem(new ProjectileSystem(bus));

        Entity station = MakeArmedStation(world, Vector3.Zero, Stance.Defensive);
        MakeFriendlyNonCombatUnit(world, new Vector3(10f, 0f, 0f));

        world.Update(0.1f);

        var ct = world.GetComponent<CombatTargetComponent>(station)!;
        Assert.Equal(Entity.Null, ct.CurrentTarget);
        Assert.Equal(0, CountProjectiles(world));

        world.Dispose();
    }
}