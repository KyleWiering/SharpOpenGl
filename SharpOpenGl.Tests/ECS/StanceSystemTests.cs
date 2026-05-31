using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class StanceSystemTests
{
    private static (World world, Entity friendly, Entity enemy) SetupCombat(
        Stance stance, float distance = 100f)
    {
        var world = new World();

        Entity friendly = world.CreateEntity();
        world.AddComponent(friendly, new TransformComponent
        {
            Position = Vector3.Zero
        });
        world.AddComponent(friendly, new HealthComponent
        {
            CurrentHP = 100, MaxHP = 100
        });
        world.AddComponent(friendly, new CombatTargetComponent
        {
            Faction = 1,
            TargetingMode = TargetPriority.Closest
        });
        world.AddComponent(friendly, new WeaponListComponent());
        world.GetComponent<WeaponListComponent>(friendly)!.Weapons.Add(
            new WeaponComponent { Damage = 10, Range = 200, FireRate = 1 }
        );
        world.AddComponent(friendly, new StanceComponent
        {
            CurrentStance = stance
        });

        Entity enemy = world.CreateEntity();
        world.AddComponent(enemy, new TransformComponent
        {
            Position = new Vector3(distance, 0, 0)
        });
        world.AddComponent(enemy, new HealthComponent
        {
            CurrentHP = 100, MaxHP = 100
        });
        world.AddComponent(enemy, new CombatTargetComponent
        {
            Faction = 2,
            TargetingMode = TargetPriority.Closest
        });

        return (world, friendly, enemy);
    }

    [Fact]
    public void Neutral_stance_does_not_acquire_target()
    {
        var (world, friendly, _) = SetupCombat(Stance.Neutral, distance: 50f);
        var system = new StanceSystem();

        system.Update(world, 0.016f);

        var ct = world.GetComponent<CombatTargetComponent>(friendly)!;
        Assert.Equal(Entity.Null, ct.CurrentTarget);

        world.Dispose();
    }

    [Fact]
    public void Defensive_stance_retaliates_when_attacked()
    {
        var (world, friendly, enemy) = SetupCombat(Stance.Defensive, distance: 100f);

        // Enemy targets friendly
        var enemyCt = world.GetComponent<CombatTargetComponent>(enemy)!;
        enemyCt.CurrentTarget = friendly;

        var system = new StanceSystem();
        system.Update(world, 0.016f);

        var friendlyCt = world.GetComponent<CombatTargetComponent>(friendly)!;
        Assert.Equal(enemy, friendlyCt.CurrentTarget);

        world.Dispose();
    }

    [Fact]
    public void Defensive_stance_engages_nearby_enemies()
    {
        var (world, friendly, enemy) = SetupCombat(Stance.Defensive, distance: 100f);
        var system = new StanceSystem();

        system.Update(world, 0.016f);

        var ct = world.GetComponent<CombatTargetComponent>(friendly)!;
        Assert.Equal(enemy, ct.CurrentTarget);

        world.Dispose();
    }

    [Fact]
    public void Aggressive_stance_pursues_enemies_in_range()
    {
        var (world, friendly, enemy) = SetupCombat(Stance.Aggressive, distance: 150f);
        var system = new StanceSystem();

        system.Update(world, 0.016f);

        var ct = world.GetComponent<CombatTargetComponent>(friendly)!;
        Assert.Equal(enemy, ct.CurrentTarget);

        world.Dispose();
    }

    [Fact]
    public void Aggressive_stance_ignores_out_of_range_enemies()
    {
        var (world, friendly, _) = SetupCombat(Stance.Aggressive, distance: 500f);
        var system = new StanceSystem();

        system.Update(world, 0.016f);

        var ct = world.GetComponent<CombatTargetComponent>(friendly)!;
        Assert.Equal(Entity.Null, ct.CurrentTarget);

        world.Dispose();
    }

    [Fact]
    public void StanceComponent_defaults_to_defensive()
    {
        var comp = new StanceComponent();
        Assert.Equal(Stance.Defensive, comp.CurrentStance);
    }
}
