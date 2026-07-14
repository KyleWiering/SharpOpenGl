using SharpOpenGl.Engine.Economy;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// ECS system that drives the resource economy each frame:
/// <list type="bullet">
///   <item>Applies base income rates via <see cref="ResourceManager.Tick"/>.</item>
///   <item>Advances collector state machines (Idle → MovingToNode → Collecting → Returning → Depositing).</item>
///   <item>Processes node depletion and respawn countdown timers.</item>
/// </list>
/// Proximity detection uses <see cref="TransformComponent"/> when present;
/// collectors without a transform are treated as already co-located with their target.
/// </summary>
public sealed class ResourceSystem : GameSystem
{
    /// <summary>World-unit radius within which a collector docks (legacy fallback).</summary>
    public const float ArrivalRadius = 2f;

    private readonly ResourceManager _resources;

    public ResourceSystem(ResourceManager resources) => _resources = resources;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _resources.Tick(deltaTime);
        TickNodeRespawns(world, deltaTime);
        TickCollectors(world, deltaTime);
    }

    // ── Node respawn ──────────────────────────────────────────────────────────

    private static void TickNodeRespawns(World world, float deltaTime)
    {
        foreach (var (_, node) in world.Query<ResourceNodeComponent>())
        {
            if (!node.IsDepleted || node.RespawnTime <= 0f) continue;
            node.RespawnCountdown -= deltaTime;
            if (node.RespawnCountdown <= 0f)
            {
                node.Amount = node.MaxAmount;
                node.RespawnCountdown = 0f;
            }
        }
    }

    // ── Collector state machine ───────────────────────────────────────────────

    private void TickCollectors(World world, float deltaTime)
    {
        foreach (var (entity, collector) in world.Query<ResourceCollectorComponent>())
        {
            switch (collector.State)
            {
                case CollectorState.MovingToNode:
                    HandleMovingToNode(world, entity, collector);
                    break;

                case CollectorState.Collecting:
                    HandleCollecting(world, entity, collector, deltaTime);
                    break;

                case CollectorState.Returning:
                    HandleReturning(world, entity, collector);
                    break;

                case CollectorState.Depositing:
                    HandleDepositing(world, collector);
                    break;
            }
        }
    }

    // ── State handlers ────────────────────────────────────────────────────────

    private static void HandleMovingToNode(
        World world, Entity entity, ResourceCollectorComponent collector)
    {
        if (!world.IsAlive(collector.AssignedNode))
        {
            collector.State = CollectorState.Idle;
            return;
        }

        var node = world.GetComponent<ResourceNodeComponent>(collector.AssignedNode);
        if (node == null || node.IsDepleted)
        {
            collector.State = CollectorState.Idle;
            return;
        }

        if (!IsWithinHarvestRange(world, entity, collector.AssignedNode, collector))
        {
            EnsureCollectorPathTarget(world, entity, collector.AssignedNode);
            return;
        }

        collector.State = CollectorState.Collecting;
        collector.CarryType = node.ResourceType;
        collector.TractorPulseTimer = 0f;
        node.AssignedCollectors++;
    }

    private static void HandleCollecting(
        World world, Entity entity, ResourceCollectorComponent collector, float deltaTime)
    {
        if (!world.IsAlive(collector.AssignedNode))
        {
            collector.State = CollectorState.Returning;
            return;
        }

        var node = world.GetComponent<ResourceNodeComponent>(collector.AssignedNode);
        if (node == null || node.IsDepleted)
        {
            if (node != null) node.AssignedCollectors = Math.Max(0, node.AssignedCollectors - 1);
            collector.State = CollectorState.Returning;
            return;
        }

        if (!IsWithinHarvestRange(world, entity, collector.AssignedNode, collector))
        {
            node.AssignedCollectors = Math.Max(0, node.AssignedCollectors - 1);
            collector.State = CollectorState.MovingToNode;
            return;
        }

        // Drone mode: cargo increments when drones return (handled by MiningVisualSystem).
        if (collector.HarvestMode == HarvestMode.Drones)
        {
            if (collector.IsFull || node.IsDepleted)
            {
                if (!node.IsDepleted)
                    node.AssignedCollectors = Math.Max(0, node.AssignedCollectors - 1);
                collector.State = CollectorState.Returning;
            }
            return;
        }

        float extractDt = deltaTime;
        if (collector.HarvestMode == HarvestMode.TractorBeam)
        {
            collector.TractorPulseTimer += deltaTime;
            if (collector.TractorPulseTimer < MiningVisualSystem.TractorPulseInterval)
                return;
            extractDt = collector.TractorPulseTimer;
            collector.TractorPulseTimer = 0f;
        }

        float extract = collector.HarvestRate * extractDt;
        float space = collector.CarryCapacity - collector.CarryAmount;
        float actual = Math.Min(extract, Math.Min(node.Amount, space));

        node.Amount -= actual;
        collector.CarryAmount += actual;
        collector.CarryType ??= node.ResourceType;

        if (node.IsDepleted)
        {
            node.AssignedCollectors = 0;
            if (node.RespawnTime > 0f)
                node.RespawnCountdown = node.RespawnTime;
        }

        if (collector.IsFull || node.IsDepleted)
        {
            if (!node.IsDepleted)
                node.AssignedCollectors = Math.Max(0, node.AssignedCollectors - 1);
            collector.State = CollectorState.Returning;
        }
    }

    private static void HandleReturning(
        World world, Entity entity, ResourceCollectorComponent collector)
    {
        if (collector.CarryAmount <= 0f)
        {
            TryReturnToNode(world, collector);
            return;
        }

        if (!world.IsAlive(collector.DepositTarget))
        {
            collector.State = CollectorState.Idle;
            return;
        }

        if (!IsWithinRadius(world, entity, collector.DepositTarget))
        {
            EnsureCollectorPathTarget(world, entity, collector.DepositTarget);
            return;
        }

        collector.State = CollectorState.Depositing;
    }

    private void HandleDepositing(World world, ResourceCollectorComponent collector)
    {
        if (collector.CarryAmount > 0f && collector.CarryType.HasValue)
        {
            _resources.Add(collector.PlayerId, collector.CarryType.Value, collector.CarryAmount);
            collector.CarryAmount = 0f;
        }

        TryReturnToNode(world, collector);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void TryReturnToNode(World world, ResourceCollectorComponent collector)
    {
        if (world.IsAlive(collector.AssignedNode))
        {
            var node = world.GetComponent<ResourceNodeComponent>(collector.AssignedNode);
            if (node != null && !node.IsDepleted)
            {
                collector.State = CollectorState.MovingToNode;
                collector.TractorPulseTimer = 0f;
                return;
            }
        }

        collector.State = CollectorState.Idle;
        collector.AssignedNode = Entity.Null;
    }

    private static void EnsureCollectorPathTarget(World world, Entity entity, Entity target)
    {
        if (!world.IsAlive(target)) return;

        var movement = world.GetComponent<MovementComponent>(entity);
        var targetTransform = world.GetComponent<TransformComponent>(target);
        if (movement == null || targetTransform == null) return;

        movement.PathTarget = targetTransform.Position;
    }

    private static bool IsWithinHarvestRange(
        World world, Entity collector, Entity node, ResourceCollectorComponent comp)
    {
        var tc = world.GetComponent<TransformComponent>(collector);
        var tn = world.GetComponent<TransformComponent>(node);
        if (tc == null || tn == null) return true;

        float range = comp.HarvestRange > 0f ? comp.HarvestRange : ArrivalRadius;
        return (tc.Position - tn.Position).Length <= range;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="a"/> and <paramref name="b"/> are within
    /// <see cref="ArrivalRadius"/> world units, or if either lacks a <see cref="TransformComponent"/>.
    /// </summary>
    private static bool IsWithinRadius(World world, Entity a, Entity b)
    {
        var ta = world.GetComponent<TransformComponent>(a);
        var tb = world.GetComponent<TransformComponent>(b);

        if (ta == null || tb == null) return true;

        return (ta.Position - tb.Position).Length <= ArrivalRadius;
    }
}