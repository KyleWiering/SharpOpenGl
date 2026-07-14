using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Spawns articulated special-hull sub-parts (bay doors, sensor dish, launcher pod, wing flaps)
/// at ship creation for the six target hull families.
/// </summary>
public static class SpecialHullArticulationSpawner
{
    private const float PivotMatchEpsilon = 0.02f;
    private const float DefaultSlewRate = 90f;

    private static readonly HashSet<ArticulatedPartType> SpecialPartTypes =
    [
        ArticulatedPartType.BayDoor,
        ArticulatedPartType.SensorDish,
        ArticulatedPartType.LauncherPod,
        ArticulatedPartType.WingFlap,
        ArticulatedPartType.EngineGimbal,
    ];

    /// <summary>
    /// Attaches special-hull articulated children for eligible hull definitions.
    /// Idempotent — skips parts already present for the owner (matched by type + pivot).
    /// </summary>
    /// <returns>Count of newly created child entities.</returns>
    public static int AttachSpecialHullParts(World world, Entity ship, EntityDefinition def, string raceId)
    {
        _ = raceId;
        if (!SpecialHullArticulationDefs.IsTargetHull(def.Id))
            return 0;

        string hullKey = def.Id.ToLowerInvariant();

        if (HasSpecialPartsInDefinition(def))
        {
            if (HasArticulatedPartsForRootOwner(world, ship))
                FinalizeExistingSpecialParts(world, ship, def, hullKey);

            return SpawnFromDefinition(world, ship, def, hullKey);
        }

        if (HasArticulatedPartsForRootOwner(world, ship))
            return 0;

        return SpawnFromFallback(world, ship, hullKey);
    }

    /// <summary>Counts special-hull articulated children owned directly by <paramref name="owner"/>.</summary>
    public static int CountSpecialPartsForOwner(World world, Entity owner, ArticulatedPartType? partType = null)
    {
        int count = 0;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner != owner)
                continue;

            if (partType.HasValue && part.PartType != partType.Value)
                continue;

            if (!IsSpecialHullDriverType(part.PartType))
                continue;

            count++;
        }

        return count;
    }

    private static bool HasSpecialPartsInDefinition(EntityDefinition def)
    {
        if (!ArticulationDefinitionParser.IsValid(def.Articulation))
            return false;

        foreach (ArticulationPartDefinition partDef in def.Articulation!.Parts)
        {
            if (!ArticulationDefinitionParser.TryParsePartType(partDef.PartType, out ArticulatedPartType parsed))
                continue;

            ArticulatedPartType resolved = ResolveSpawnPartType(def.Id.ToLowerInvariant(), partDef, parsed);
            if (SpecialPartTypes.Contains(resolved))
                return true;
        }

        return false;
    }

    private static int FinalizeExistingSpecialParts(
        World world,
        Entity ship,
        EntityDefinition def,
        string hullKey)
    {
        int updated = 0;
        Vector4 accent = SpecialHullArticulationDefs.ResolveAccentTint(hullKey);

        foreach (var (partEntity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) != ship)
                continue;

            ArticulationPartDefinition? partDef = FindDefinitionPart(def, part);
            if (partDef != null)
            {
                if (ArticulationDefinitionParser.TryParsePartType(partDef.PartType, out ArticulatedPartType parsed))
                {
                    ArticulatedPartType resolved = ResolveSpawnPartType(hullKey, partDef, parsed);
                    if (part.PartType != resolved && SpecialPartTypes.Contains(resolved))
                    {
                        part.PartType = resolved;
                        updated++;
                    }
                }
            }
            else if (hullKey == "drone_swarm"
                && part.PartType == ArticulatedPartType.TurretYaw
                && PivotNearlyEqual(part.LocalPivotOffset, new Vector3(0f, 0.1f, -0.35f)))
            {
                part.PartType = ArticulatedPartType.LauncherPod;
                updated++;
            }

            if (!IsSpecialHullDriverType(part.PartType))
                continue;

            RenderComponent? render = world.GetComponent<RenderComponent>(partEntity);
            if (render != null)
                render.Color = accent;
        }

        return updated;
    }

    private static ArticulationPartDefinition? FindDefinitionPart(
        EntityDefinition def,
        ArticulatedPartComponent part)
    {
        if (!ArticulationDefinitionParser.IsValid(def.Articulation))
            return null;

        foreach (ArticulationPartDefinition partDef in def.Articulation!.Parts)
        {
            Vector3? pivot = ArticulationDefinitionParser.TryParseVec3(partDef.LocalPivot);
            if (pivot == null)
                continue;

            if (PivotNearlyEqual(part.LocalPivotOffset, pivot.Value))
                return partDef;
        }

        return null;
    }

    private static bool HasArticulatedPartsForRootOwner(World world, Entity owner)
    {
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) == owner)
                return true;
        }

        return false;
    }

    private static int SpawnFromDefinition(World world, Entity ship, EntityDefinition def, string hullKey)
    {
        int created = 0;
        var spawnedById = new Dictionary<string, Entity>(StringComparer.Ordinal);

        foreach (ArticulationPartDefinition partDef in def.Articulation!.Parts)
        {
            if (!ArticulationDefinitionParser.TryParsePartType(partDef.PartType, out ArticulatedPartType parsed))
                continue;

            ArticulatedPartType partType = ResolveSpawnPartType(hullKey, partDef, parsed);
            if (!SpecialPartTypes.Contains(partType))
                continue;

            Vector3? pivot = ArticulationDefinitionParser.TryParseVec3(partDef.LocalPivot);
            if (pivot == null)
                continue;

            if (HasMatchingPart(world, ship, partType, pivot.Value)
                || HasEquivalentExistingPart(world, ship, hullKey, partDef, partType, pivot.Value))
                continue;

            Entity owner = ship;
            if (!string.IsNullOrWhiteSpace(partDef.OwnerPartId)
                && spawnedById.TryGetValue(partDef.OwnerPartId, out Entity parentPart))
            {
                owner = parentPart;
            }

            Vector3 meshOffset = ArticulationDefinitionParser.TryParseVec3(partDef.MeshOffset) ?? Vector3.Zero;
            string meshKey = !string.IsNullOrWhiteSpace(partDef.MeshKey)
                ? partDef.MeshKey
                : ResolveDefaultMeshKey(hullKey, partType, partDef.Id);

            Entity partEntity = CreatePartEntity(
                world,
                owner,
                hullKey,
                partType,
                pivot.Value,
                meshOffset,
                partDef.YawMin ?? 0f,
                partDef.YawMax ?? 0f,
                partDef.PitchMin ?? 0f,
                partDef.PitchMax ?? 0f,
                partDef.IdleSweep ?? false,
                partDef.IdleSweepSpeed ?? 25f,
                partDef.SlewRate ?? DefaultSlewRate,
                meshKey);

            if (!string.IsNullOrWhiteSpace(partDef.Id))
                spawnedById[partDef.Id] = partEntity;

            created++;
        }

        return created;
    }

    private static int SpawnFromFallback(World world, Entity ship, string hullKey)
    {
        if (!SpecialHullArticulationDefs.TryGetParts(hullKey, out SpecialHullPartDef[] parts))
            return 0;

        int created = 0;
        foreach (SpecialHullPartDef partDef in parts)
        {
            if (HasMatchingPart(world, ship, partDef.PartType, partDef.LocalPivotOffset))
                continue;

            CreatePartEntity(
                world,
                ship,
                hullKey,
                partDef.PartType,
                partDef.LocalPivotOffset,
                partDef.MeshLocalOffset,
                partDef.YawMin,
                partDef.YawMax,
                partDef.PitchMin,
                partDef.PitchMax,
                partDef.IdleSweepEnabled,
                partDef.IdleSweepSpeed,
                partDef.SlewRate,
                partDef.MeshKey);

            created++;
        }

        return created;
    }

    private static Entity CreatePartEntity(
        World world,
        Entity owner,
        string hullKey,
        ArticulatedPartType partType,
        Vector3 localPivot,
        Vector3 meshLocalOffset,
        float yawMin,
        float yawMax,
        float pitchMin,
        float pitchMax,
        bool idleSweepEnabled,
        float idleSweepSpeed,
        float slewRate,
        string meshKey)
    {
        Entity partEntity = world.CreateEntity();
        world.AddComponent(partEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            PartType = partType,
            LocalPivotOffset = localPivot,
            MeshLocalOffset = meshLocalOffset,
            YawMin = yawMin,
            YawMax = yawMax,
            PitchMin = pitchMin,
            PitchMax = pitchMax,
            CurrentYaw = 0f,
            CurrentPitch = 0f,
            TargetYaw = 0f,
            TargetPitch = 0f,
            HasAimTarget = false,
            IdleSweepEnabled = idleSweepEnabled,
            IdleSweepSpeed = idleSweepSpeed,
            SlewRateDegreesPerSecond = slewRate,
        });
        world.AddComponent(partEntity, new RenderComponent
        {
            MeshKey = meshKey,
            Color = SpecialHullArticulationDefs.ResolveAccentTint(hullKey),
            Visible = true,
            PrimitiveType = 4,
        });

        return partEntity;
    }

    private static bool HasEquivalentExistingPart(
        World world,
        Entity owner,
        string hullKey,
        ArticulationPartDefinition partDef,
        ArticulatedPartType resolvedType,
        Vector3 localPivot)
    {
        if (resolvedType != ArticulatedPartType.LauncherPod)
            return false;

        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) != owner)
                continue;

            if (part.PartType == ArticulatedPartType.TurretYaw
                && string.Equals(partDef.Id, "launcher_pod", StringComparison.OrdinalIgnoreCase)
                && PivotNearlyEqual(part.LocalPivotOffset, localPivot))
            {
                return true;
            }
        }

        _ = hullKey;
        return false;
    }

    private static bool HasMatchingPart(
        World world,
        Entity owner,
        ArticulatedPartType partType,
        Vector3 localPivot)
    {
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner != owner || part.PartType != partType)
                continue;

            if (PivotNearlyEqual(part.LocalPivotOffset, localPivot))
                return true;
        }

        return false;
    }

    private static bool PivotNearlyEqual(Vector3 a, Vector3 b) =>
        MathF.Abs(a.X - b.X) <= PivotMatchEpsilon
        && MathF.Abs(a.Y - b.Y) <= PivotMatchEpsilon
        && MathF.Abs(a.Z - b.Z) <= PivotMatchEpsilon;

    private static ArticulatedPartType ResolveSpawnPartType(
        string hullKey,
        ArticulationPartDefinition partDef,
        ArticulatedPartType parsed)
    {
        if (hullKey == "drone_swarm"
            && parsed == ArticulatedPartType.TurretYaw
            && string.Equals(partDef.Id, "launcher_pod", StringComparison.OrdinalIgnoreCase))
        {
            return ArticulatedPartType.LauncherPod;
        }

        return parsed;
    }

    private static string ResolveDefaultMeshKey(string hullKey, ArticulatedPartType partType, string partId) =>
        partType switch
        {
            ArticulatedPartType.BayDoor when hullKey == "bomber_heavy" =>
                ArticulatedShipPartMeshes.BuildBayDoorKey(hullKey, partId.Contains("port", StringComparison.OrdinalIgnoreCase)),
            ArticulatedPartType.BayDoor when hullKey == "carrier_command" =>
                ArticulatedShipPartMeshes.BuildPartKey("deck_segment", hullKey),
            ArticulatedPartType.LauncherPod =>
                ArticulatedShipPartMeshes.BuildPartKey("launcher_pod", hullKey),
            ArticulatedPartType.SensorDish =>
                ArticulatedShipPartMeshes.BuildPartKey("sensor_dish", hullKey),
            ArticulatedPartType.WingFlap or ArticulatedPartType.EngineGimbal =>
                ArticulatedShipPartMeshes.BuildWingFlapKey(hullKey, partId.Contains("left", StringComparison.OrdinalIgnoreCase)),
            _ => string.Empty,
        };

    private static bool IsSpecialHullDriverType(ArticulatedPartType type) =>
        SpecialPartTypes.Contains(type);
}