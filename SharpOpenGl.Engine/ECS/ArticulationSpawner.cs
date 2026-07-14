using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Generic loader that spawns articulated child entities from JSON <see cref="ArticulationDefinition"/> blocks.
/// </summary>
public static class ArticulationSpawner
{
    /// <summary>
    /// Spawns articulated child entities for <paramref name="owner"/> from JSON definition.
    /// Idempotent — returns 0 if parts already exist for this owner chain.
    /// </summary>
    /// <returns>Count of new part entities created.</returns>
    public static int SpawnFromDefinition(
        World world,
        Entity owner,
        ArticulationDefinition? definition,
        string? defaultMeshHullKey = null)
    {
        if (HasArticulatedPartsForOwner(world, owner))
            return 0;

        if (!ArticulationDefinitionParser.IsValid(definition))
            return 0;

        var partEntities = new Dictionary<string, Entity>(StringComparer.Ordinal);
        int created = 0;

        foreach (ArticulationPartDefinition partDef in definition!.Parts)
        {
            if (!ArticulationDefinitionParser.TryParsePartType(partDef.PartType, out ArticulatedPartType partType))
                continue;

            Vector3? pivot = ArticulationDefinitionParser.TryParseVec3(partDef.LocalPivot);
            if (pivot == null)
                continue;

            Vector3 meshOffset = ArticulationDefinitionParser.TryParseVec3(partDef.MeshOffset) ?? Vector3.Zero;

            Entity partEntity = world.CreateEntity();
            var component = new ArticulatedPartComponent
            {
                Owner = owner,
                PartType = partType,
                LocalPivotOffset = pivot.Value,
                MeshLocalOffset = meshOffset,
            };

            ApplyAngleLimits(component, partType, partDef);
            ApplySpawnDefaults(component, partType, partDef);

            world.AddComponent(partEntity, component);

            string? meshKey = ResolveMeshKey(partDef, partType, defaultMeshHullKey);
            if (!string.IsNullOrWhiteSpace(meshKey))
            {
                world.AddComponent(partEntity, new RenderComponent
                {
                    MeshKey = meshKey,
                    MeshId = -1,
                    Visible = true,
                    PrimitiveType = 4,
                });
            }

            partEntities[partDef.Id] = partEntity;
            created++;
        }

        foreach (ArticulationPartDefinition partDef in definition.Parts)
        {
            if (string.IsNullOrWhiteSpace(partDef.OwnerPartId))
                continue;

            if (!partEntities.TryGetValue(partDef.Id, out Entity childEntity))
                continue;

            if (!partEntities.TryGetValue(partDef.OwnerPartId, out Entity parentEntity))
            {
                world.DestroyEntity(childEntity);
                partEntities.Remove(partDef.Id);
                created--;
                continue;
            }

            ArticulatedPartComponent child = world.GetComponent<ArticulatedPartComponent>(childEntity)!;
            child.Owner = parentEntity;
        }

        return created;
    }

    private static bool HasArticulatedPartsForOwner(World world, Entity owner)
    {
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) == owner)
                return true;
        }

        return false;
    }

    private static string? ResolveMeshKey(
        ArticulationPartDefinition partDef,
        ArticulatedPartType partType,
        string? defaultMeshHullKey)
    {
        if (!string.IsNullOrWhiteSpace(partDef.MeshKey))
            return partDef.MeshKey;

        if (string.IsNullOrWhiteSpace(defaultMeshHullKey))
            return null;

        return partType switch
        {
            ArticulatedPartType.TurretYaw =>
                ArticulatedShipPartMeshes.BuildPartKey("turret_yaw", defaultMeshHullKey),
            ArticulatedPartType.TurretPitch =>
                ArticulatedShipPartMeshes.BuildPartKey("turret_pitch", defaultMeshHullKey),
            _ => null,
        };
    }

    private static void ApplyAngleLimits(
        ArticulatedPartComponent component,
        ArticulatedPartType partType,
        ArticulationPartDefinition partDef)
    {
        switch (partType)
        {
            case ArticulatedPartType.TurretYaw:
                component.YawMin = partDef.YawMin ?? -120f;
                component.YawMax = partDef.YawMax ?? 120f;
                component.PitchMin = partDef.PitchMin ?? -10f;
                component.PitchMax = partDef.PitchMax ?? 45f;
                break;
            case ArticulatedPartType.TurretPitch:
                component.YawMin = partDef.YawMin ?? 0f;
                component.YawMax = partDef.YawMax ?? 0f;
                component.PitchMin = partDef.PitchMin ?? -10f;
                component.PitchMax = partDef.PitchMax ?? 45f;
                break;
            default:
                component.YawMin = partDef.YawMin ?? -180f;
                component.YawMax = partDef.YawMax ?? 180f;
                component.PitchMin = partDef.PitchMin ?? -90f;
                component.PitchMax = partDef.PitchMax ?? 90f;
                break;
        }
    }

    private static void ApplySpawnDefaults(
        ArticulatedPartComponent component,
        ArticulatedPartType partType,
        ArticulationPartDefinition partDef)
    {
        component.SlewRateDegreesPerSecond = partDef.SlewRate ?? ResolveDefaultSlewRate(partType);

        if (partDef.IdleSweep.HasValue)
        {
            component.IdleSweepEnabled = partDef.IdleSweep.Value;
            component.IdleSweepSpeed = partDef.IdleSweepSpeed ?? ResolveDefaultIdleSweepSpeed(partType);
            return;
        }

        switch (partType)
        {
            case ArticulatedPartType.TurretYaw:
                component.IdleSweepEnabled = true;
                component.IdleSweepSpeed = 8f;
                break;
            case ArticulatedPartType.TurretPitch:
                component.IdleSweepEnabled = false;
                component.IdleSweepSpeed = 0f;
                break;
            case ArticulatedPartType.SensorDish:
            case ArticulatedPartType.Crane:
                component.IdleSweepEnabled = true;
                component.IdleSweepSpeed = 25f;
                break;
            default:
                component.IdleSweepEnabled = false;
                component.IdleSweepSpeed = 0f;
                break;
        }
    }

    private static float ResolveDefaultSlewRate(ArticulatedPartType partType) =>
        partType switch
        {
            ArticulatedPartType.TurretYaw or ArticulatedPartType.TurretPitch => 90f,
            _ => 90f,
        };

    private static float ResolveDefaultIdleSweepSpeed(ArticulatedPartType partType) =>
        partType switch
        {
            ArticulatedPartType.TurretYaw => 8f,
            ArticulatedPartType.SensorDish or ArticulatedPartType.Crane => 25f,
            _ => 0f,
        };
}