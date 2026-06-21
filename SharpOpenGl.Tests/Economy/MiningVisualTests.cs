using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using Xunit;

namespace SharpOpenGl.Tests.Economy;

public class MiningVisualTests
{
    private static (World, ResourceSystem, MiningVisualSystem) MakeSetup()
    {
        var rm = new ResourceManager();
        rm.AddPlayer(1);
        var resourceSystem = new ResourceSystem(rm);
        var visualSystem = new MiningVisualSystem();
        var world = new World();
        world.AddSystem(resourceSystem);
        world.AddSystem(visualSystem);
        return (world, resourceSystem, visualSystem);
    }

    private static Entity MakeNode(World world, Vector3 pos, float amount = 500f)
    {
        Entity node = world.CreateEntity();
        world.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = amount,
            MaxAmount = amount,
            HarvestRate = 10f,
        });
        world.AddComponent(node, new TransformComponent { Position = pos });
        return node;
    }

    private static Entity MakeCollector(
        World world,
        HarvestMode mode,
        Entity node,
        Vector3 pos,
        float harvestRate = 15f,
        float capacity = 100f,
        CollectorState state = CollectorState.Collecting)
    {
        Entity collector = world.CreateEntity();
        world.AddComponent(collector, new ResourceCollectorComponent
        {
            PlayerId = 1,
            AssignedNode = node,
            HarvestMode = mode,
            HarvestRange = HarvestModeDefaults.DefaultRange(mode),
            HarvestRate = harvestRate,
            CarryCapacity = capacity,
            State = state,
        });
        world.AddComponent(collector, new TransformComponent { Position = pos });
        return collector;
    }

    [Fact]
    public void Drone_mode_spawns_drone_entities_while_collecting()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, new Vector3(0f, 0f, 0f));
        Entity collector = MakeCollector(world, HarvestMode.Drones, node, new Vector3(10f, 0f, 0f));

        world.Update(0f);

        int drones = world.Query<MiningDroneComponent>().Count();
        Assert.InRange(drones, 1, 3);
    }

    [Fact]
    public void Drone_mode_cargo_increments_only_after_shuttle_return()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, new Vector3(0f, 0f, 0f), amount: 200f);
        Entity collector = MakeCollector(world, HarvestMode.Drones, node, new Vector3(5f, 0f, 0f),
            harvestRate: 16f, capacity: 100f);

        world.Update(1f);
        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        Assert.Equal(0f, cComp.CarryAmount);

        // One full outbound + inbound shuttle cycle.
        world.Update(MiningVisualSystem.DroneShuttleDuration * 2f);
        Assert.True(cComp.CarryAmount > 0f);
    }

    [Fact]
    public void Drone_mode_despawns_drones_when_returning()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, Vector3.Zero);
        Entity collector = MakeCollector(world, HarvestMode.Drones, node, new Vector3(5f, 0f, 0f));

        world.Update(0f);
        Assert.NotEmpty(world.Query<MiningDroneComponent>());

        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        cComp.State = CollectorState.Returning;
        world.Update(0f);

        Assert.Empty(world.Query<MiningDroneComponent>());
    }

    [Fact]
    public void Eva_mode_spawns_astronaut_visuals_on_asteroid_nodes()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, new Vector3(20f, 0f, 10f));
        Entity collector = MakeCollector(world, HarvestMode.Eva, node, new Vector3(20f, 0f, 30f));

        world.Update(0f);

        var crew = world.Query<MiningVisualComponent>()
            .Where(v => v.Item2.Kind == MiningVisualKind.EvaCrew)
            .ToList();
        Assert.InRange(crew.Count, 1, 2);
    }

    [Fact]
    public void Eva_mode_spawns_crew_on_harvestable_planet()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, new Vector3(40f, 0f, 40f));
        world.AddComponent(node, new EntityNameComponent
        {
            DisplayName = "Harvest World",
            DefinitionId = "harvestable_planet",
        });
        Entity collector = MakeCollector(world, HarvestMode.Eva, node, new Vector3(40f, 0f, 70f));

        world.Update(0f);

        Assert.NotEmpty(world.Query<MiningVisualComponent>()
            .Where(v => v.Item2.Kind == MiningVisualKind.EvaCrew));
    }

    [Fact]
    public void Tractor_beam_mode_tags_beam_visual_on_collector()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, new Vector3(0f, 0f, 50f));
        Entity collector = MakeCollector(world, HarvestMode.TractorBeam, node, new Vector3(0f, 0f, 0f));

        world.Update(0f);

        Assert.True(world.HasComponent<TractorBeamVisualComponent>(collector));
        var beam = world.GetComponent<TractorBeamVisualComponent>(collector)!;
        Assert.Equal(node, beam.NodeEntity);
    }

    [Fact]
    public void Tractor_beam_extracts_in_pulses_not_continuously()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, Vector3.Zero, amount: 500f);
        Entity collector = MakeCollector(world, HarvestMode.TractorBeam, node, Vector3.Zero,
            harvestRate: 20f, capacity: 200f);

        world.Update(0.2f);
        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        Assert.Equal(0f, cComp.CarryAmount);

        world.Update(MiningVisualSystem.TractorPulseInterval);
        Assert.True(cComp.CarryAmount > 0f);
    }

    [Fact]
    public void Collecting_respects_harvest_range_per_mode()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, Vector3.Zero);
        Entity collector = MakeCollector(world, HarvestMode.TractorBeam, node,
            new Vector3(100f, 0f, 0f), state: CollectorState.MovingToNode);
        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        cComp.HarvestRange = 55f;

        world.Update(0f);
        Assert.Equal(CollectorState.MovingToNode, cComp.State);

        cComp.HarvestRange = 120f;
        world.Update(0f);
        Assert.Equal(CollectorState.Collecting, cComp.State);
    }
}