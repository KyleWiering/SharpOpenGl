using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class AttackChaseSystemTests
{
    [Fact]
    public void AttackChase_does_not_override_active_move_route()
    {
        var world = new World();
        world.AddSystem(new AttackChaseSystem());

        Entity ship = world.CreateEntity();
        Entity enemy = world.CreateEntity();

        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(ship, new MovementComponent { Speed = 10f, Acceleration = 50f, TurnRate = 360f });
        world.AddComponent(ship, new WeaponListComponent());
        world.GetComponent<WeaponListComponent>(ship)!.Weapons.Add(
            new WeaponComponent { Range = 100f });
        world.AddComponent(ship, new CombatTargetComponent
        {
            Faction = 1,
            CurrentTarget = enemy,
        });
        world.AddComponent(ship, new WaypointQueueComponent
        {
            Waypoints = [new Vector3(50f, 0f, 0f)],
        });
        world.AddComponent(ship, new DestinationComponent
        {
            Target = new Vector3(50f, 0f, 0f),
            GridX = 50,
            GridY = 0,
        });

        world.AddComponent(enemy, new TransformComponent { Position = new Vector3(5f, 0f, 0f) });
        world.AddComponent(enemy, new HealthComponent { CurrentHP = 100f, MaxHP = 100f });
        world.AddComponent(enemy, new CombatTargetComponent { Faction = 2 });

        world.Update(0.016f);

        Assert.True(world.HasComponent<DestinationComponent>(ship));
        Assert.False(world.HasComponent<PathComponent>(ship));
        var movement = world.GetComponent<MovementComponent>(ship)!;
        Assert.Null(movement.PathTarget);

        world.Dispose();
    }
}