using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using Xunit;

namespace SharpOpenGl.Tests.Economy;

public class ResourceSystemTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (World, ResourceManager, ResourceSystem) MakeSetup(
        int playerId = 1, float energyIncome = 0f)
    {
        var rm = new ResourceManager();
        var pr = rm.AddPlayer(playerId);
        pr.AddIncome(ResourceType.Energy, energyIncome);
        var system = new ResourceSystem(rm);
        var world  = new World();
        world.AddSystem(system);
        return (world, rm, system);
    }

    private static Entity MakeNode(World world, ResourceType type, float amount,
        float harvestRate = 10f, float respawnTime = 0f, Vector3? pos = null)
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new ResourceNodeComponent
        {
            ResourceType  = type,
            Amount        = amount,
            MaxAmount     = amount,
            HarvestRate   = harvestRate,
            RespawnTime   = respawnTime,
            RespawnCountdown = respawnTime
        });
        if (pos.HasValue)
            world.AddComponent(e, new TransformComponent { Position = pos.Value });
        return e;
    }

    private static Entity MakeCollector(World world, int playerId, Entity node, Entity depot,
        float capacity = 50f, CollectorState state = CollectorState.MovingToNode, Vector3? pos = null,
        float harvestRate = 20f, HarvestMode harvestMode = HarvestMode.Eva)
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new ResourceCollectorComponent
        {
            PlayerId      = playerId,
            AssignedNode  = node,
            DepositTarget = depot,
            CarryCapacity = capacity,
            HarvestRate   = harvestRate,
            HarvestRange  = HarvestModeDefaults.DefaultRange(harvestMode),
            HarvestMode   = harvestMode,
            State         = state
        });
        if (pos.HasValue)
            world.AddComponent(e, new TransformComponent { Position = pos.Value });
        return e;
    }

    private static Entity MakeDepot(World world, Vector3? pos = null)
    {
        Entity e = world.CreateEntity();
        if (pos.HasValue)
            world.AddComponent(e, new TransformComponent { Position = pos.Value });
        return e;
    }

    // ── Income tick ──────────────────────────────────────────────────────────

    [Fact]
    public void System_applies_base_income_each_tick()
    {
        var (world, rm, _) = MakeSetup(playerId: 1, energyIncome: 10f);
        world.Update(1f);
        Assert.Equal(10f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy));
    }

    // ── Node respawn ─────────────────────────────────────────────────────────

    [Fact]
    public void Node_respawns_after_countdown()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, ResourceType.Minerals, 0f,
            harvestRate: 0f, respawnTime: 5f);
        var comp = world.GetComponent<ResourceNodeComponent>(node)!;
        comp.Amount = 0f;
        comp.RespawnCountdown = 3f;
        comp.MaxAmount = 100f;

        world.Update(3f); // exactly reaches zero
        Assert.Equal(100f, comp.Amount);
    }

    [Fact]
    public void Node_does_not_respawn_before_countdown()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, ResourceType.Minerals, 0f,
            harvestRate: 0f, respawnTime: 10f);
        var comp = world.GetComponent<ResourceNodeComponent>(node)!;
        comp.Amount = 0f;
        comp.RespawnCountdown = 10f;

        world.Update(5f);
        Assert.Equal(0f, comp.Amount);
    }

    [Fact]
    public void Node_with_zero_respawn_time_never_refills()
    {
        var (world, _, _) = MakeSetup();
        Entity node = MakeNode(world, ResourceType.Minerals, 0f, respawnTime: 0f);
        var comp = world.GetComponent<ResourceNodeComponent>(node)!;
        comp.Amount = 0f;

        world.Update(100f);
        Assert.Equal(0f, comp.Amount);
    }

    // ── Collector state machine (no-transform path) ──────────────────────────

    [Fact]
    public void Collector_without_transform_moves_to_collecting_immediately()
    {
        var (world, _, _) = MakeSetup();
        Entity node    = MakeNode(world, ResourceType.Minerals, 100f);
        Entity depot   = MakeDepot(world);
        Entity collector = MakeCollector(world, 1, node, depot, capacity: 50f,
            state: CollectorState.MovingToNode);

        world.Update(0f); // single tick: MovingToNode → Collecting
        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        Assert.Equal(CollectorState.Collecting, cComp.State);
    }

    [Fact]
    public void Collector_harvests_minerals_from_node()
    {
        var (world, rm, _) = MakeSetup();
        var pr = rm.GetPlayer(1)!;

        Entity node    = MakeNode(world, ResourceType.Minerals, 100f, harvestRate: 20f);
        Entity depot   = MakeDepot(world);
        Entity collector = MakeCollector(world, 1, node, depot, capacity: 50f,
            state: CollectorState.Collecting);

        world.Update(1f); // extract up to 20 units
        var nodeComp = world.GetComponent<ResourceNodeComponent>(node)!;
        var cComp    = world.GetComponent<ResourceCollectorComponent>(collector)!;

        Assert.Equal(80f, nodeComp.Amount);
        Assert.Equal(20f, cComp.CarryAmount);
    }

    [Fact]
    public void Collector_transitions_to_returning_when_full()
    {
        var (world, _, _) = MakeSetup();
        Entity node    = MakeNode(world, ResourceType.Minerals, 1000f, harvestRate: 100f);
        Entity depot   = MakeDepot(world);
        Entity collector = MakeCollector(world, 1, node, depot, capacity: 50f,
            state: CollectorState.Collecting, harvestRate: 100f);

        world.Update(1f); // 100 units/sec would fill 50-unit hold
        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        Assert.Equal(CollectorState.Returning, cComp.State);
        Assert.Equal(50f, cComp.CarryAmount);
    }

    [Fact]
    public void Collector_deposits_and_resources_reach_player_pool()
    {
        var (world, rm, _) = MakeSetup();
        Entity node    = MakeNode(world, ResourceType.Minerals, 200f, harvestRate: 100f);
        Entity depot   = MakeDepot(world);
        Entity collector = MakeCollector(world, 1, node, depot, capacity: 50f,
            state: CollectorState.Depositing);

        // Pre-load cargo.
        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        cComp.CarryAmount = 40f;
        cComp.CarryType   = ResourceType.Minerals;

        world.Update(0f); // Depositing → deposit + back to MovingToNode
        Assert.Equal(40f, rm.GetPlayer(1)!.GetAmount(ResourceType.Minerals));
        Assert.Equal(0f,  cComp.CarryAmount);
    }

    [Fact]
    public void Collector_goes_idle_when_node_is_depleted_and_no_respawn()
    {
        var (world, rm, _) = MakeSetup();
        Entity node    = MakeNode(world, ResourceType.Minerals, 5f, harvestRate: 100f);
        Entity depot   = MakeDepot(world);
        Entity collector = MakeCollector(world, 1, node, depot, capacity: 50f,
            state: CollectorState.Collecting);

        world.Update(1f); // Node depletes; collector fills with 5 units → Returning.
        var cComp = world.GetComponent<ResourceCollectorComponent>(collector)!;
        Assert.Equal(CollectorState.Returning, cComp.State);

        world.Update(0f); // Returning → Depositing (no transform = co-located).
        Assert.Equal(CollectorState.Depositing, cComp.State);

        world.Update(0f); // Depositing → deposits, node still depleted → Idle.
        Assert.Equal(CollectorState.Idle, cComp.State);
        Assert.Equal(Entity.Null, cComp.AssignedNode);
    }

    [Fact]
    public void Node_depletion_starts_respawn_countdown()
    {
        var (world, _, _) = MakeSetup();
        Entity node    = MakeNode(world, ResourceType.Minerals, 5f,
            harvestRate: 100f, respawnTime: 30f);
        Entity depot   = MakeDepot(world);
        MakeCollector(world, 1, node, depot, capacity: 50f, state: CollectorState.Collecting);

        world.Update(1f); // Node is depleted.
        var nodeComp = world.GetComponent<ResourceNodeComponent>(node)!;
        Assert.True(nodeComp.IsDepleted);
        Assert.Equal(30f, nodeComp.RespawnCountdown);
    }

    // ── No infinite resource exploit ─────────────────────────────────────────

    [Fact]
    public void Cannot_harvest_more_than_node_contains()
    {
        var (world, rm, _) = MakeSetup();
        Entity node    = MakeNode(world, ResourceType.Minerals, 10f, harvestRate: 1000f);
        Entity depot   = MakeDepot(world);
        Entity collector = MakeCollector(world, 1, node, depot, capacity: 500f,
            state: CollectorState.Collecting);

        world.Update(1f);
        var nodeComp = world.GetComponent<ResourceNodeComponent>(node)!;
        var cComp    = world.GetComponent<ResourceCollectorComponent>(collector)!;

        // Node should be empty; collector should have exactly 10 units, not 1000.
        Assert.Equal(0f,  nodeComp.Amount);
        Assert.Equal(10f, cComp.CarryAmount);
    }

    [Fact]
    public void Resources_cannot_exceed_max_storage_via_income()
    {
        var (world, rm, _) = MakeSetup(energyIncome: 1000f);
        var pr = rm.GetPlayer(1)!;

        world.Update(100f); // massive income tick
        Assert.Equal(pr.GetMax(ResourceType.Energy), pr.GetAmount(ResourceType.Energy));
    }
}
