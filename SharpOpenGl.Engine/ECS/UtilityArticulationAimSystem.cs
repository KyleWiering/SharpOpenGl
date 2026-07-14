using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Drives utility-part aim targets (mining arms, repair arms) from gameplay state.
/// Runs before <see cref="ArticulationSystem"/> so slew smoothing applies the same frame.
/// </summary>
public sealed class UtilityArticulationAimSystem : GameSystem
{
    private const float StowedAngleEpsilon = 0.5f;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _ = deltaTime;
        var toDestroy = new List<Entity>();

        foreach (var (partEntity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.PartType is not (ArticulatedPartType.MiningArm or ArticulatedPartType.RepairArm))
                continue;

            if (!world.IsAlive(part.Owner))
            {
                toDestroy.Add(partEntity);
                continue;
            }

            switch (part.PartType)
            {
                case ArticulatedPartType.MiningArm:
                    UpdateMiningArmAim(world, part);
                    break;
                case ArticulatedPartType.RepairArm:
                    UpdateRepairArmAim(world, part);
                    break;
            }
        }

        foreach (Entity entity in toDestroy)
            world.DestroyEntity(entity);
    }

    private static void UpdateMiningArmAim(World world, ArticulatedPartComponent part)
    {
        var collector = world.GetComponent<ResourceCollectorComponent>(part.Owner);
        if (collector == null)
        {
            ApplyMiningArmStowedAim(world, part);
            return;
        }

        if (collector.State == CollectorState.Collecting
            && world.IsAlive(collector.AssignedNode))
        {
            var ownerTf = world.GetComponent<TransformComponent>(part.Owner);
            var nodeTf = world.GetComponent<TransformComponent>(collector.AssignedNode);
            if (ownerTf == null || nodeTf == null)
            {
                ApplyMiningArmStowedAim(world, part);
                return;
            }

            Vector3 pivotWorld = ComputePivotWorld(ownerTf, part.LocalPivotOffset);
            Vector3 ownerForward = ComputeOwnerForward(ownerTf);
            (float targetYaw, float targetPitch) = ArticulationMath.ComputeAimAngles(
                pivotWorld,
                nodeTf.Position,
                ownerForward,
                part.YawMin,
                part.YawMax,
                part.PitchMin,
                part.PitchMax);

            part.HasAimTarget = true;
            part.TargetYaw = targetYaw;
            part.TargetPitch = targetPitch;
            return;
        }

        ApplyMiningArmStowedAim(world, part);
    }

    private static void UpdateRepairArmAim(World world, ArticulatedPartComponent part)
    {
        var repair = world.GetComponent<ShipRepairComponent>(part.Owner);
        if (repair == null || repair.ActiveTarget == Entity.Null || !world.IsAlive(repair.ActiveTarget))
        {
            ApplyRepairArmStowedAim(world, part);
            return;
        }

        var ownerTf = world.GetComponent<TransformComponent>(part.Owner);
        var targetTf = world.GetComponent<TransformComponent>(repair.ActiveTarget);
        if (ownerTf == null || targetTf == null)
        {
            ApplyRepairArmStowedAim(world, part);
            return;
        }

        Vector3 pivotWorld = ComputePivotWorld(ownerTf, part.LocalPivotOffset);
        float distSq = HorizontalDistanceSq(ownerTf.Position, targetTf.Position);
        if (distSq > repair.RepairRange * repair.RepairRange)
        {
            ApplyRepairArmStowedAim(world, part);
            return;
        }

        Vector3 ownerForward = ComputeOwnerForward(ownerTf);
        (float targetYaw, float targetPitch) = ArticulationMath.ComputeAimAngles(
            pivotWorld,
            targetTf.Position,
            ownerForward,
            part.YawMin,
            part.YawMax,
            part.PitchMin,
            part.PitchMax);

        part.HasAimTarget = true;
        part.TargetYaw = targetYaw;
        part.TargetPitch = targetPitch;
    }

    private static void ApplyMiningArmStowedAim(World world, ArticulatedPartComponent part)
    {
        string hullKey = ResolveOwnerHullKey(world, part.Owner);
        var (restYaw, restPitch) = UtilityPartMeshes.ResolveMiningArmStowedPose(hullKey);
        ApplyStowedAim(part, restYaw, restPitch);
    }

    private static void ApplyRepairArmStowedAim(World world, ArticulatedPartComponent part)
    {
        string hullKey = ResolveOwnerHullKey(world, part.Owner);
        var (restYaw, restPitch) = UtilityPartMeshes.ResolveRepairArmStowedPose(hullKey);
        ApplyStowedAim(part, restYaw, restPitch);
    }

    private static void ApplyStowedAim(ArticulatedPartComponent part, float restYaw, float restPitch)
    {
        part.TargetYaw = restYaw;
        part.TargetPitch = restPitch;

        bool atRest = MathF.Abs(part.CurrentYaw - restYaw) <= StowedAngleEpsilon
            && MathF.Abs(part.CurrentPitch - restPitch) <= StowedAngleEpsilon;

        part.HasAimTarget = !atRest;
    }

    private static string ResolveOwnerHullKey(World world, Entity owner)
    {
        var name = world.GetComponent<EntityNameComponent>(owner);
        if (name != null && !string.IsNullOrWhiteSpace(name.DefinitionId))
            return name.DefinitionId.ToLowerInvariant();

        return "miner_basic";
    }

    /// <summary>
    /// Pivot world position using scaled local offset and owner yaw (matches
    /// <see cref="ShipEngineEmitterSystem"/> nozzle origin math).
    /// </summary>
    private static Vector3 ComputePivotWorld(TransformComponent ownerTf, Vector3 localPivotOffset)
    {
        float yawRad = MathHelper.DegreesToRadians(ownerTf.EulerAngles.Y);
        Matrix4 yawRot = Matrix4.CreateRotationY(yawRad);

        Vector3 scaledOffset = new Vector3(
            localPivotOffset.X * ownerTf.Scale.X,
            localPivotOffset.Y * ownerTf.Scale.Y,
            localPivotOffset.Z * ownerTf.Scale.Z);
        Vector3 worldOffset = Vector3.TransformNormal(scaledOffset, yawRot);
        return ownerTf.Position + worldOffset;
    }

    /// <summary>Owner forward (−Z) rotated by owner yaw.</summary>
    private static Vector3 ComputeOwnerForward(TransformComponent ownerTf)
    {
        float yawRad = MathHelper.DegreesToRadians(ownerTf.EulerAngles.Y);
        Matrix4 yawRot = Matrix4.CreateRotationY(yawRad);
        Vector3 forward = Vector3.TransformNormal(-Vector3.UnitZ, yawRot);
        return forward.LengthSquared > 1e-6f ? Vector3.Normalize(forward) : -Vector3.UnitZ;
    }

    private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return dx * dx + dz * dz;
    }
}