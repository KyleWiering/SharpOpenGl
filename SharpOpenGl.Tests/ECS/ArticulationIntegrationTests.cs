using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ArticulationIntegrationTests
{
    private static EntityDefinition CorvetteDef() => new()
    {
        Id = "corvette_fast",
        Category = "corvette",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 180, Shields = 40, Armor = 20 },
            Movement = new MovementDefinition { Speed = 75, Acceleration = 90, TurnRate = 150 },
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 28, Range = 260, FireRate = 2.2f },
            ],
        },
    };

    private static EntityDefinition DefenseTurretDef() => new()
    {
        Id = "defense_turret",
        Category = "base",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 600, Armor = 40 },
            Building = new BuildingDefinition
            {
                BuildingType = "defense_turret",
                Footprint = [1, 1],
                Rotates = true,
            },
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 35, Range = 320, FireRate = 3.0f },
            ],
        },
    };

    private static EntityDefinition SensorArrayDef() => new()
    {
        Id = "sensor_array",
        Category = "base",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 450, Armor = 25 },
            Building = new BuildingDefinition
            {
                BuildingType = "sensor_array",
                Footprint = [2, 2],
                Rotates = true,
            },
        },
    };

    [Fact]
    public void Mixed_spawn_tick_system_observes_angle_delta()
    {
        CombatBalance.ResetForTests();
        var world = new World();
        var articulationSystem = new ArticulationSystem();

        Entity ship = new ShipFactory().Create(world, CorvetteDef());
        world.AddComponent(ship, new TransformComponent { Position = new Vector3(-20f, 0f, 0f) });
        world.AddComponent(ship, new CombatTargetComponent
        {
            Faction = 1,
            TargetingMode = TargetPriority.Closest,
        });

        Entity turret = new BaseFactory().Create(world, DefenseTurretDef());
        world.AddComponent(turret, new TransformComponent { Position = new Vector3(0f, 0f, 0f) });

        Entity sensor = new BaseFactory().Create(world, SensorArrayDef());
        world.AddComponent(sensor, new TransformComponent { Position = new Vector3(20f, 0f, 0f) });

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(30f, 5f, -10f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;
        world.GetComponent<CombatTargetComponent>(turret)!.CurrentTarget = target;

        var initialAngles = SnapshotArticulatedAngles(world);

        for (int i = 0; i < 32; i++)
            articulationSystem.Update(world, 0.016f);

        var finalAngles = SnapshotArticulatedAngles(world);

        Assert.True(initialAngles.Count > 0);
        Assert.True(AnyAngleChanged(initialAngles, finalAngles));
    }

    private static Dictionary<Entity, (float yaw, float pitch)> SnapshotArticulatedAngles(World world)
    {
        var snapshot = new Dictionary<Entity, (float yaw, float pitch)>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
            snapshot[entity] = (part.CurrentYaw, part.CurrentPitch);
        return snapshot;
    }

    private static bool AnyAngleChanged(
        Dictionary<Entity, (float yaw, float pitch)> before,
        Dictionary<Entity, (float yaw, float pitch)> after)
    {
        foreach (var (entity, initial) in before)
        {
            if (!after.TryGetValue(entity, out var final))
                continue;

            if (MathF.Abs(final.yaw - initial.yaw) > 1e-3f
                || MathF.Abs(final.pitch - initial.pitch) > 1e-3f)
                return true;
        }

        return false;
    }
}