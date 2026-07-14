using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Computes stable orbit positions for resource collectors harvesting stationary nodes.
/// Miners path to a ring at harvest range instead of the node center.
/// </summary>
public static class HarvestOrbitHelper
{
    private const float OrbitRadiusMin = 8f;
    private const float OrbitRadiusHarvestFactor = 0.85f;
    private const float OrbitRingTolerance = 1.5f;
    public const float OrbitAngularSpeed = 0.25f;

    /// <summary>World position on the XZ orbit ring at <paramref name="angleRadians"/>.</summary>
    public static Vector3 ComputeOrbitTarget(
        Vector3 nodeCenter, float orbitRadius, float angleRadians, float collectorY)
    {
        return new Vector3(
            nodeCenter.X + orbitRadius * MathF.Cos(angleRadians),
            collectorY,
            nodeCenter.Z + orbitRadius * MathF.Sin(angleRadians));
    }

    /// <summary>
    /// Orbit radius clamped to stay outside node collision while remaining within harvest range.
    /// </summary>
    public static float OrbitRadiusFor(
        ResourceCollectorComponent collector, World world, Entity nodeEntity)
    {
        float harvestRange = collector.HarvestRange > 0f
            ? collector.HarvestRange
            : ResourceSystem.ArrivalRadius;
        float fromHarvest = MathF.Max(OrbitRadiusMin, harvestRange * OrbitRadiusHarvestFactor);

        var nodeTransform = world.GetComponent<TransformComponent>(nodeEntity);
        if (nodeTransform == null)
            return fromHarvest;

        float targetScale = MathF.Max(
            nodeTransform.Scale.X,
            MathF.Max(nodeTransform.Scale.Y, nodeTransform.Scale.Z));
        float maxFromScale = targetScale * 0.5f;

        if (maxFromScale >= OrbitRadiusMin)
            return MathF.Min(fromHarvest, maxFromScale);

        return fromHarvest;
    }

    /// <summary>Stable starting angle for a collector/node pair so multiple miners stagger.</summary>
    public static float AssignOrbitAngle(Entity collector, Entity node)
    {
        uint hash = collector.Index * 73856093u
            ^ (uint)collector.Generation * 19349663u
            ^ node.Index * 83492791u
            ^ (uint)node.Generation * 50331653u;
        float twoPi = MathF.PI * 2f;
        return hash % 10_000 / 10_000f * twoPi;
    }

    /// <summary>
    /// Updates movement toward the harvest orbit ring. Returns <c>true</c> when orbit logic applied.
    /// </summary>
    public static bool UpdateHarvestOrbit(
        World world,
        Entity entity,
        ResourceCollectorComponent collector,
        float deltaTime)
    {
        if (!world.IsAlive(collector.AssignedNode))
            return false;

        if (!world.HasComponent<ResourceNodeComponent>(collector.AssignedNode))
            return false;

        var movement = world.GetComponent<MovementComponent>(entity);
        var collectorTransform = world.GetComponent<TransformComponent>(entity);
        var nodeTransform = world.GetComponent<TransformComponent>(collector.AssignedNode);
        if (movement == null || collectorTransform == null || nodeTransform == null)
            return false;

        float orbitRadius = OrbitRadiusFor(collector, world, collector.AssignedNode);
        Vector3 nodeCenter = nodeTransform.Position;
        float collectorY = collectorTransform.Position.Y;

        float horizontalDist = PathRouteHelper.HorizontalDistance(
            collectorTransform.Position, nodeCenter);

        float fallbackAngle = float.IsNaN(collector.OrbitAngle)
            ? AssignOrbitAngle(entity, collector.AssignedNode)
            : collector.OrbitAngle;
        float shipAngle = AngleTowardCollector(
            nodeCenter, collectorTransform.Position, fallbackAngle);

        bool onRing = MathF.Abs(horizontalDist - orbitRadius) <= OrbitRingTolerance;
        float targetAngle;

        if (!onRing)
        {
            targetAngle = shipAngle;
            collector.OrbitAngle = shipAngle;
        }
        else
        {
            float orbitAngle = shipAngle + OrbitAngularSpeed * deltaTime;
            if (orbitAngle > MathF.PI * 2f)
                orbitAngle -= MathF.PI * 2f;
            collector.OrbitAngle = orbitAngle;
            targetAngle = orbitAngle;
        }

        movement.PathTarget = ComputeOrbitTarget(
            nodeCenter, orbitRadius, targetAngle, collectorY);
        ClearRouteComponents(world, entity);
        return true;
    }

    /// <summary>Sets an initial path target toward the nearest orbit approach point.</summary>
    public static void ApplyApproachPath(
        World world, Entity entity, ResourceCollectorComponent collector, Entity nodeEntity)
    {
        var movement = world.GetComponent<MovementComponent>(entity);
        var collectorTransform = world.GetComponent<TransformComponent>(entity);
        var nodeTransform = world.GetComponent<TransformComponent>(nodeEntity);
        if (movement == null || collectorTransform == null || nodeTransform == null)
            return;

        float orbitRadius = OrbitRadiusFor(collector, world, nodeEntity);
        if (float.IsNaN(collector.OrbitAngle))
            collector.OrbitAngle = AssignOrbitAngle(entity, nodeEntity);

        float approachAngle = AngleTowardCollector(
            nodeTransform.Position, collectorTransform.Position, collector.OrbitAngle);
        movement.PathTarget = ComputeOrbitTarget(
            nodeTransform.Position,
            orbitRadius,
            approachAngle,
            collectorTransform.Position.Y);
        ClearRouteComponents(world, entity);
    }

    private static float AngleTowardCollector(
        Vector3 nodeCenter, Vector3 collectorPos, float fallbackAngle)
    {
        float dx = collectorPos.X - nodeCenter.X;
        float dz = collectorPos.Z - nodeCenter.Z;
        if (dx * dx + dz * dz < 0.01f)
            return fallbackAngle;
        return MathF.Atan2(dz, dx);
    }

    private static void ClearRouteComponents(World world, Entity entity)
    {
        world.RemoveComponent<DestinationComponent>(entity);
        world.RemoveComponent<PathComponent>(entity);
        world.RemoveComponent<WaypointQueueComponent>(entity);
    }
}