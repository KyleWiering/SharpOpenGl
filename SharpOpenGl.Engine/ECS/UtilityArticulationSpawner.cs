using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Spawns articulated utility sub-parts (mining arms, etc.) on eligible hulls at ship creation.
/// </summary>
public static class UtilityArticulationSpawner
{
    private const float MiningArmYawMin = -90f;
    private const float MiningArmYawMax = 90f;
    private const float MiningArmPitchMin = -15f;
    private const float MiningArmPitchMax = 60f;
    private const float MiningArmSlewRate = 90f;

    private const float RepairArmYawMin = -120f;
    private const float RepairArmYawMax = 120f;
    private const float RepairArmPitchMin = -10f;
    private const float RepairArmPitchMax = 45f;
    private const float RepairArmSlewRate = 90f;

    /// <summary>
    /// Attaches one <see cref="ArticulatedPartType.MiningArm"/> child to miner hulls.
    /// Idempotent — skips when a mining arm already exists for <paramref name="ship"/>.
    /// </summary>
    /// <returns>1 when a new arm was created, 0 otherwise.</returns>
    public static int AttachMinerArms(World world, Entity ship, EntityDefinition def, string raceId)
    {
        _ = raceId;
        if (!TryNormalizeMinerHullId(def.Id, out string hullKey) || HasMiningArm(world, ship))
            return 0;

        var (restYaw, restPitch) = UtilityPartMeshes.ResolveMiningArmStowedPose(hullKey);

        Entity arm = world.CreateEntity();
        world.AddComponent(arm, new ArticulatedPartComponent
        {
            Owner = ship,
            PartType = ArticulatedPartType.MiningArm,
            LocalPivotOffset = UtilityPartMeshes.ResolveMiningArmPivot(hullKey),
            MeshLocalOffset = UtilityPartMeshes.ResolveMiningArmMeshOffset(hullKey),
            YawMin = MiningArmYawMin,
            YawMax = MiningArmYawMax,
            PitchMin = MiningArmPitchMin,
            PitchMax = MiningArmPitchMax,
            CurrentYaw = restYaw,
            CurrentPitch = restPitch,
            TargetYaw = restYaw,
            TargetPitch = restPitch,
            HasAimTarget = false,
            IdleSweepEnabled = false,
            SlewRateDegreesPerSecond = MiningArmSlewRate,
        });
        world.AddComponent(arm, new RenderComponent
        {
            MeshKey = UtilityPartMeshes.MiningArmMeshKey(hullKey),
            Color = new Vector4(1f, 0.82f, 0.32f, 1f),
            Visible = true,
            PrimitiveType = 4,
        });

        return 1;
    }

    /// <summary>
    /// Attaches one <see cref="ArticulatedPartType.RepairArm"/> child to support repair hulls.
    /// Idempotent — skips when a repair arm already exists for <paramref name="ship"/>.
    /// </summary>
    /// <returns>1 when a new arm was created, 0 otherwise.</returns>
    public static int AttachRepairArm(World world, Entity ship, EntityDefinition def, string raceId)
    {
        _ = raceId;
        if (!TryNormalizeRepairHullId(def.Id, out string hullKey) || HasRepairArm(world, ship))
            return 0;

        var (restYaw, restPitch) = UtilityPartMeshes.ResolveRepairArmStowedPose(hullKey);

        Entity arm = world.CreateEntity();
        world.AddComponent(arm, new ArticulatedPartComponent
        {
            Owner = ship,
            PartType = ArticulatedPartType.RepairArm,
            LocalPivotOffset = UtilityPartMeshes.ResolveRepairArmPivot(hullKey),
            MeshLocalOffset = UtilityPartMeshes.ResolveRepairArmMeshOffset(hullKey),
            YawMin = RepairArmYawMin,
            YawMax = RepairArmYawMax,
            PitchMin = RepairArmPitchMin,
            PitchMax = RepairArmPitchMax,
            CurrentYaw = restYaw,
            CurrentPitch = restPitch,
            TargetYaw = restYaw,
            TargetPitch = restPitch,
            HasAimTarget = false,
            IdleSweepEnabled = false,
            SlewRateDegreesPerSecond = RepairArmSlewRate,
        });
        world.AddComponent(arm, new RenderComponent
        {
            MeshKey = UtilityPartMeshes.RepairArmMeshKey(hullKey),
            Color = new Vector4(0.42f, 0.95f, 0.72f, 1f),
            Visible = true,
            PrimitiveType = 4,
        });

        return 1;
    }

    /// <summary>Counts mining-arm child entities owned by <paramref name="owner"/>.</summary>
    public static int CountMiningArmsForOwner(World world, Entity owner)
    {
        int count = 0;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner == owner && part.PartType == ArticulatedPartType.MiningArm)
                count++;
        }

        return count;
    }

    /// <summary>Counts repair-arm child entities owned by <paramref name="owner"/>.</summary>
    public static int CountRepairArmsForOwner(World world, Entity owner)
    {
        int count = 0;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner == owner && part.PartType == ArticulatedPartType.RepairArm)
                count++;
        }

        return count;
    }

    private static bool TryNormalizeMinerHullId(string definitionId, out string hullKey)
    {
        hullKey = definitionId.ToLowerInvariant() switch
        {
            "miner_basic" or "miner_tractor" or "miner_eva" => definitionId.ToLowerInvariant(),
            _ => string.Empty,
        };

        return hullKey.Length > 0;
    }

    private static bool HasMiningArm(World world, Entity owner) =>
        CountMiningArmsForOwner(world, owner) > 0;

    private static bool TryNormalizeRepairHullId(string definitionId, out string hullKey)
    {
        hullKey = definitionId.ToLowerInvariant() switch
        {
            "support_repair" => definitionId.ToLowerInvariant(),
            _ => string.Empty,
        };

        return hullKey.Length > 0;
    }

    private static bool HasRepairArm(World world, Entity owner) =>
        CountRepairArmsForOwner(world, owner) > 0;
}