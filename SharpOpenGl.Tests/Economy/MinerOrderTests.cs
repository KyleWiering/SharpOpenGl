using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Tests.Config;
using Xunit;

namespace SharpOpenGl.Tests.Economy;

[Collection("MovementBalance")]
public class MinerOrderTests
{
    [Fact]
    public void AssignMinerToNode_sets_moving_to_node_state()
    {
        var world = new World();
        Entity node = CreateNode(world, new Vector3(50f, 0f, 0f));
        Entity depot = CreateDepot(world, new Vector3(0f, 0f, 0f));
        Entity miner = CreateMiner(world, new Vector3(0f, 0f, 0f));

        MinerAssignment.AssignToNode(world, miner, node, depot);

        var collector = world.GetComponent<ResourceCollectorComponent>(miner)!;
        Assert.Equal(node, collector.AssignedNode);
        Assert.Equal(CollectorState.MovingToNode, collector.State);
        Assert.Equal(1, collector.PlayerId);
        Assert.Equal(depot, collector.DepositTarget);
    }

    [Fact]
    public void Right_click_node_assigns_selected_miner()
    {
        var world = new World();
        Entity node = CreateNode(world, new Vector3(8f, 0f, 0f));
        Entity depot = CreateDepot(world, new Vector3(0f, 0f, 0f));
        Entity miner = CreateMiner(world, new Vector3(0f, 0f, 0f), selected: true);

        Entity? targetNode = MinerAssignment.FindResourceNodeAt(world, new Vector3(10f, 0f, 0f));
        Assert.Equal(node, targetNode);

        bool assigned = MinerAssignment.TryAssignSelectedMiners(world, targetNode!.Value, depot);
        Assert.True(assigned);

        var collector = world.GetComponent<ResourceCollectorComponent>(miner)!;
        Assert.Equal(node, collector.AssignedNode);
        Assert.Equal(CollectorState.MovingToNode, collector.State);
    }

    [Fact]
    public void Collector_maintains_orbit_distance_while_collecting()
    {
        MovementBalance.ResetForTests();
        MovementBalance.Apply(new MovementConfig
        {
            GlobalSpeedMultiplier = 1f,
            GlobalAccelerationMultiplier = 1f,
        });

        var rm = new ResourceManager();
        rm.AddPlayer(1);
        var world = new World();
        world.AddSystem(new HarvestOrbitSystem());
        world.AddSystem(new MovementSystem());
        world.AddSystem(new ResourceSystem(rm));

        Entity node = CreateNode(world, Vector3.Zero, amount: 500f, harvestRate: 20f);
        Entity depot = CreateDepot(world, new Vector3(200f, 0f, 0f));
        // Start co-located with node (worst case): must radially reach harvest ring within 2s.
        Entity miner = CreateMovingMiner(world, Vector3.Zero, HarvestMode.Eva);

        MinerAssignment.AssignToNode(world, miner, node, depot);

        for (int i = 0; i < 120; i++)
            world.Update(1f / 60f);

        var collector = world.GetComponent<ResourceCollectorComponent>(miner)!;
        var minerTf = world.GetComponent<TransformComponent>(miner)!;
        float orbitRadius = HarvestOrbitHelper.OrbitRadiusFor(collector, world, node);
        float distance = PathRouteHelper.HorizontalDistance(minerTf.Position, Vector3.Zero);

        Assert.Equal(CollectorState.Collecting, collector.State);
        Assert.InRange(distance, orbitRadius * 0.9f, collector.HarvestRange);
    }

    [Fact]
    public void Multiple_miners_get_distinct_orbit_angles()
    {
        var world = new World();
        Entity node = CreateNode(world, Vector3.Zero);
        Entity depot = CreateDepot(world, new Vector3(100f, 0f, 0f));
        Entity minerA = CreateMiner(world, new Vector3(30f, 0f, 0f));
        Entity minerB = CreateMiner(world, new Vector3(-20f, 0f, 10f));

        MinerAssignment.AssignToNode(world, minerA, node, depot);
        MinerAssignment.AssignToNode(world, minerB, node, depot);

        var angleA = world.GetComponent<ResourceCollectorComponent>(minerA)!.OrbitAngle;
        var angleB = world.GetComponent<ResourceCollectorComponent>(minerB)!.OrbitAngle;
        float delta = MathF.Abs(angleA - angleB);
        if (delta > MathF.PI)
            delta = MathF.PI * 2f - delta;

        Assert.True(delta > 0.5f);
    }

    [Fact]
    public void Deposit_increases_player_minerals()
    {
        var rm = new ResourceManager();
        rm.AddPlayer(1);
        var system = new ResourceSystem(rm);
        var world = new World();
        world.AddSystem(system);

        Entity node = CreateNode(world, new Vector3(20f, 0f, 0f), amount: 200f, harvestRate: 100f);
        Entity depot = CreateDepot(world, new Vector3(0f, 0f, 0f));
        Entity miner = world.CreateEntity();
        world.AddComponent(miner, new ResourceCollectorComponent
        {
            PlayerId = 1,
            AssignedNode = node,
            DepositTarget = depot,
            CarryCapacity = 50f,
            HarvestRate = 100f,
            State = CollectorState.Depositing,
            CarryAmount = 35f,
            CarryType = ResourceType.Minerals,
        });

        world.Update(0f);

        Assert.Equal(35f, rm.GetPlayer(1)!.GetAmount(ResourceType.Minerals));
        Assert.Equal(0f, world.GetComponent<ResourceCollectorComponent>(miner)!.CarryAmount);
    }

    private static Entity CreateNode(
        World world, Vector3 position, float amount = 100f, float harvestRate = 20f)
    {
        Entity node = world.CreateEntity();
        world.AddComponent(node, new TransformComponent { Position = position });
        world.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = amount,
            MaxAmount = amount,
            HarvestRate = harvestRate,
        });
        return node;
    }

    private static Entity CreateDepot(World world, Vector3 position)
    {
        Entity depot = world.CreateEntity();
        world.AddComponent(depot, new TransformComponent { Position = position });
        return depot;
    }

    private static Entity CreateMiner(World world, Vector3 position, bool selected = false)
    {
        Entity miner = world.CreateEntity();
        world.AddComponent(miner, new TransformComponent { Position = position });
        world.AddComponent(miner, new ResourceCollectorComponent
        {
            CarryCapacity = 50f,
            HarvestRate = 20f,
            HarvestRange = HarvestModeDefaults.DefaultRange(HarvestMode.Eva),
        });
        world.AddComponent(miner, new SelectionComponent
        {
            IsSelected = selected,
            SelectionRadius = 10f,
        });
        return miner;
    }

    private static Entity CreateMovingMiner(
        World world, Vector3 position, HarvestMode mode)
    {
        Entity miner = CreateMiner(world, position);
        world.AddComponent(miner, new MovementComponent
        {
            Speed = 40f,
            Acceleration = 80f,
            TurnRate = 180f,
        });
        var collector = world.GetComponent<ResourceCollectorComponent>(miner)!;
        collector.HarvestMode = mode;
        collector.HarvestRange = HarvestModeDefaults.DefaultRange(mode);
        return miner;
    }
}

/// <summary>Test helper mirroring player miner assignment command paths.</summary>
internal static class MinerAssignment
{
    public static void AssignToNode(World world, Entity miner, Entity node, Entity depositTarget)
    {
        var collector = world.GetComponent<ResourceCollectorComponent>(miner);
        if (collector == null) return;

        collector.AssignedNode = node;
        collector.State = CollectorState.MovingToNode;
        collector.PlayerId = 1;
        collector.DepositTarget = depositTarget;
        collector.OrbitAngle = HarvestOrbitHelper.AssignOrbitAngle(miner, node);

        RouteCommands.ClearRoute(world, miner);
    }

    public static Entity? FindResourceNodeAt(World world, Vector3 worldPos)
    {
        const float nodeClickRadius = 12f;
        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, node) in world.Query<ResourceNodeComponent>())
        {
            if (node.IsDepleted) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = (transform.Position - worldPos).Length;
            if (dist < nodeClickRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    public static bool TryAssignSelectedMiners(World world, Entity nodeEntity, Entity depositTarget)
    {
        bool assigned = false;
        foreach (var (entity, sel) in world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var collector = world.GetComponent<ResourceCollectorComponent>(entity);
            if (collector == null) continue;

            AssignToNode(world, entity, nodeEntity, depositTarget);
            assigned = true;
        }

        return assigned;
    }
}