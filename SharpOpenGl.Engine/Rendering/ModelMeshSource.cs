using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Builds procedural meshes for quality scoring and mesh-preview by asset kind.</summary>
public static class ModelMeshSource
{
    public const string KindShip = "ship";
    public const string KindStation = "station";
    public const string KindObject = "object";

    public static readonly string[] AllObjectIds =
    [
        "drone_worker",
        "shield_generator",
        "selection_ring",
        "engine_trail",
        "move_target",
        "team_aura_disc",
        "resource_node",
        "mining_drone",
        "eva_astronaut",
        "neutral_planet",
        "harvestable_planet",
        "asteroid_field",
        "nebula",
        "debris",
        "ion_storm",
        "wormhole_remnant",
        "laser",
        "beam",
        "torpedo",
        "missile",
        "bomb",
        "cannon",
        "wave",
    ];

    public static bool IsKnownKind(string? assetKind) =>
        assetKind is KindShip or KindStation or KindObject;

    public static float[] Build(string assetKind, string modelId, string? raceId = null)
    {
        string kind = NormalizeKind(assetKind);
        string id = modelId.Trim();
        raceId = string.IsNullOrWhiteSpace(raceId) ? RaceShipMeshes.DefaultRace : raceId.Trim();

        return kind switch
        {
            KindStation => RaceStationMeshes.Build(id, raceId),
            KindObject => BuildObject(id),
            _ => RaceShipMeshes.Build(raceId, id),
        };
    }

    public static string ResolveObjRelativePath(string assetKind, string modelId, string? raceId)
    {
        string kind = NormalizeKind(assetKind);
        string id = modelId.Trim();
        return kind switch
        {
            KindStation => $"Stations/{raceId}/{id}.obj",
            KindObject => ResolveObjectRelativePath(id),
            _ => $"Ships/{raceId}/{id}.obj",
        };
    }

    public static string ResolveMeshKey(string assetKind, string modelId, string? raceId)
    {
        string kind = NormalizeKind(assetKind);
        return kind switch
        {
            KindStation => MeshManifest.StationKey(raceId ?? RaceShipMeshes.DefaultRace, modelId),
            KindObject => ResolveObjectMeshKey(modelId),
            _ => MeshManifest.ShipKey(raceId ?? RaceShipMeshes.DefaultRace, modelId),
        };
    }

    public static string NormalizeKind(string? assetKind) =>
        assetKind?.Trim().ToLowerInvariant() switch
        {
            KindStation or "base" or "building" => KindStation,
            KindObject or "unit" or "effect" or "projectile" or "environment" => KindObject,
            _ => KindShip,
        };

    private static float[] BuildObject(string objectId)
    {
        return objectId.ToLowerInvariant() switch
        {
            "drone_worker" => ProceduralMeshes.BuildMiningDrone(new Vector3(0.7f, 0.75f, 0.8f), 1f),
            "shield_generator" => ProceduralMeshes.BuildShieldGenerator(new Vector3(0.55f, 0.82f, 0.95f)),
            "selection_ring" => ProceduralMeshes.BuildSelectionRing(new Vector3(0f, 1f, 0f), 3f),
            "engine_trail" => ProceduralMeshes.BuildEngineTrail(new Vector3(1f, 0.6f, 0.1f), 2.5f),
            "move_target" => ProceduralMeshes.BuildMoveTarget(new Vector3(0f, 1f, 0.5f), 2f),
            "team_aura_disc" => ProceduralMeshes.BuildTeamAuraDisc(),
            "resource_node" => ProceduralMeshes.BuildResourceNodeMarker(new Vector3(0.9f, 0.85f, 0.2f), 2f),
            "mining_drone" => ProceduralMeshes.BuildMiningDrone(new Vector3(0.9f, 0.8f, 0.25f)),
            "eva_astronaut" => ProceduralMeshes.BuildEvaAstronaut(new Vector3(0.92f, 0.94f, 0.98f)),
            "neutral_planet" => ProceduralMeshes.BuildPlanetSphere(new Vector3(0.75f, 0.8f, 0.9f), 4f),
            "harvestable_planet" => ProceduralMeshes.BuildPlanetSphere(new Vector3(0.55f, 0.85f, 0.45f), 4f),
            "asteroid_field" => ProceduralMeshes.BuildAsteroidFieldCluster(new Vector3(0.6f, 0.55f, 0.5f), 3f),
            "nebula" => ProceduralMeshes.BuildNebulaCloud(3f),
            "debris" => ProceduralMeshes.BuildSceneryCluster(new Vector3(0.5f, 0.5f, 0.55f), 3f),
            "ion_storm" => ProceduralMeshes.BuildIonStorm(3f),
            "wormhole_remnant" => ProceduralMeshes.BuildWormholeRemnant(3f),
            "laser" => ProceduralMeshes.BuildLaserBolt(new Vector3(1f, 0.5f, 0.35f)),
            "beam" => ProceduralMeshes.BuildBeamStreak(new Vector3(0.55f, 0.95f, 1f)),
            "torpedo" => ProceduralMeshes.BuildTorpedo(new Vector3(0.9f, 0.9f, 0.95f)),
            "missile" => ProceduralMeshes.BuildRocket(new Vector3(1f, 0.75f, 0.2f)),
            "bomb" => ProceduralMeshes.BuildBomb(new Vector3(1f, 0.55f, 0.15f)),
            "cannon" => ProceduralMeshes.BuildEnergyPulse(new Vector3(0.7f, 0.45f, 1f)),
            "wave" => ProceduralMeshes.BuildWaveRing(new Vector3(0.4f, 1f, 0.85f)),
            _ => ProceduralMeshes.BuildLaserBolt(new Vector3(0.8f, 0.8f, 0.85f)),
        };
    }

    private static string ResolveObjectRelativePath(string objectId) => objectId.ToLowerInvariant() switch
    {
        "drone_worker" => "Units/drone_worker.obj",
        "shield_generator" => "Units/shield_generator.obj",
        "neutral_planet" or "harvestable_planet" => $"Environment/planets/{objectId}.obj",
        "asteroid_field" or "nebula" or "debris" or "ion_storm" or "wormhole_remnant"
            => $"Environment/scenery/{objectId}.obj",
        "laser" or "beam" or "torpedo" or "missile" or "bomb" or "cannon" or "wave" => $"Projectiles/{objectId}.obj",
        _ => $"Effects/{objectId}.obj",
    };

    private static string ResolveObjectMeshKey(string objectId) => objectId.ToLowerInvariant() switch
    {
        "drone_worker" => "meshes/units/drone_worker.obj",
        "shield_generator" => "meshes/units/shield_generator.obj",
        "neutral_planet" or "harvestable_planet" => MeshManifest.EnvironmentKey(objectId),
        "laser" or "beam" or "torpedo" or "missile" or "bomb" or "cannon" or "wave" => MeshManifest.ProjectileKey(objectId),
        _ => MeshManifest.EffectKey(objectId),
    };
}