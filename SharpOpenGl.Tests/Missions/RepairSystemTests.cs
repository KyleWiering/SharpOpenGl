using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

/// <summary>Unit tests for <see cref="RepairSystem"/> and repair commands.</summary>
public class RepairSystemTests
{
    private static (World world, EventBus bus, RepairSystem system) Setup()
    {
        var world = new World();
        var bus = new EventBus();
        var system = new RepairSystem(bus) { PlayerFaction = 1 };
        return (world, bus, system);
    }

    private static Entity CreateDamagedFighter(World world, Vector3 position, float hpFraction = 0.1f)
    {
        Entity entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent { Position = position });
        world.AddComponent(entity, new HealthComponent { MaxHP = 100f, CurrentHP = 100f * hpFraction });
        world.AddComponent(entity, new CombatTargetComponent { Faction = 0 });
        world.AddComponent(entity, new EntityNameComponent { DefinitionId = "fighter_basic" });
        return entity;
    }

    private static Entity CreateRepairer(World world, Vector3 position, float range = 60f, float rate = 12f)
    {
        Entity entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent { Position = position });
        world.AddComponent(entity, new CombatTargetComponent { Faction = 1 });
        world.AddComponent(entity, new SelectionComponent());
        world.AddComponent(entity, new ShipRepairComponent
        {
            RepairRange = range,
            RepairRate = rate,
            RepairableCategories = ["fighter", "gunship", "corvette"],
        });
        return entity;
    }

    [Fact]
    public void Repair_restores_hp_on_nearby_damaged_friendly_ship()
    {
        var (world, _, system) = Setup();
        Entity repairer = CreateRepairer(world, Vector3.Zero);
        Entity target = CreateDamagedFighter(world, new Vector3(10f, 0f, 0f), hpFraction: 0.2f);

        system.Update(world, 1f);

        var health = world.GetComponent<HealthComponent>(target);
        Assert.NotNull(health);
        Assert.Equal(32f, health!.CurrentHP, precision: 3);
    }

    [Fact]
    public void Repair_skips_targets_out_of_range()
    {
        var (world, _, system) = Setup();
        CreateRepairer(world, Vector3.Zero, range: 5f);
        Entity target = CreateDamagedFighter(world, new Vector3(50f, 0f, 0f), hpFraction: 0.2f);

        system.Update(world, 1f);

        var health = world.GetComponent<HealthComponent>(target);
        Assert.Equal(20f, health!.CurrentHP, precision: 3);
    }

    [Fact]
    public void Repair_prefers_explicit_repair_order_target()
    {
        var (world, _, system) = Setup();
        Entity repairer = CreateRepairer(world, Vector3.Zero);
        Entity nearTarget = CreateDamagedFighter(world, new Vector3(5f, 0f, 0f), hpFraction: 0.2f);
        Entity farTarget = CreateDamagedFighter(world, new Vector3(40f, 0f, 0f), hpFraction: 0.2f);

        world.AddComponent(repairer, new RepairOrderComponent { Target = farTarget });

        system.Update(world, 1f);

        Assert.Equal(20f, world.GetComponent<HealthComponent>(nearTarget)!.CurrentHP, precision: 3);
        Assert.Equal(32f, world.GetComponent<HealthComponent>(farTarget)!.CurrentHP, precision: 3);
    }

    [Fact]
    public void Repair_clears_disabled_when_hp_reaches_reactivation_threshold()
    {
        var (world, _, system) = Setup();
        CreateRepairer(world, Vector3.Zero, rate: 30f);
        Entity target = CreateDamagedFighter(world, new Vector3(5f, 0f, 0f), hpFraction: 0.1f);
        world.AddComponent(target, new DisabledComponent { RemainingSeconds = 1_000f });

        system.Update(world, 1f);

        Assert.False(world.HasComponent<DisabledComponent>(target));
        Assert.True(world.GetComponent<HealthComponent>(target)!.HPFraction >= RepairSystem.DisabledReactivationThreshold);
    }

    [Fact]
    public void RepairTickEvent_fires_when_repair_applied()
    {
        var (world, bus, system) = Setup();
        Entity repairer = CreateRepairer(world, Vector3.Zero);
        Entity target = CreateDamagedFighter(world, new Vector3(5f, 0f, 0f), hpFraction: 0.2f);

        RepairTickEvent? evt = null;
        bus.Subscribe<RepairTickEvent>(e => evt = e);

        system.Update(world, 0.5f);

        Assert.NotNull(evt);
        Assert.Equal(repairer.Index, evt!.RepairerId);
        Assert.Equal(target.Index, evt.TargetId);
    }

    [Fact]
    public void RepairCommand_sets_repair_order_on_selected_repairers()
    {
        var world = new World();
        Entity repairer = CreateRepairer(world, Vector3.Zero);
        world.GetComponent<SelectionComponent>(repairer)!.IsSelected = true;
        Entity target = CreateDamagedFighter(world, new Vector3(5f, 0f, 0f));

        var executor = new GameCommandExecutor();
        var context = new GameCommandContext { World = world, PlayerId = 1 };
        bool ok = executor.Execute(context, new RepairCommand
        {
            PlayerId = 1,
            Tick = 1,
            RepairerIds = [repairer.Index],
            TargetEntityId = target.Index,
        });

        Assert.True(ok);
        var order = world.GetComponent<RepairOrderComponent>(repairer);
        Assert.NotNull(order);
        Assert.Equal(target, order!.Target);
    }
}