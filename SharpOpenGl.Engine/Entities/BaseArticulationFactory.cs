using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Entities;

/// <summary>
/// Static articulation spawn tables — <b>fallback only</b> when <c>EntityDefinition.Articulation</c> is absent.
/// JSON blocks in <c>GameData/Bases/*.json</c> are the source of truth via <see cref="ArticulationSpawner"/>.
/// </summary>
internal static class BaseArticulationFactory
{
    /// <summary>Base mesh scale factor matching <see cref="RaceStationMeshes"/> station geometry.</summary>
    internal const float PivotScale = 7f;

    internal static void TrySpawnArticulation(
        World world, Entity owner, string buildingType, EntityDefinition? def = null)
    {
        if (def?.Articulation != null)
        {
            ArticulationSpawner.SpawnFromDefinition(world, owner, def.Articulation);
            return;
        }

        switch (buildingType.ToLowerInvariant())
        {
            case "defense_turret":
                SpawnDefenseTurret(world, owner);
                break;
            case "missile_battery":
                SpawnMissileBattery(world, owner);
                break;
            case "sensor_array":
                SpawnSensorArray(world, owner);
                break;
            case "shipyard_small":
                SpawnShipyard(world, owner, bays: 2);
                break;
            case "shipyard_medium":
            case "shipyard":
                SpawnShipyard(world, owner, bays: 3);
                break;
            case "shipyard_large":
                SpawnShipyard(world, owner, bays: 5);
                break;
        }
    }

    private static void SpawnDefenseTurret(World world, Entity owner)
    {
        float s = PivotScale;

        Entity yawEntity = world.CreateEntity();
        world.AddComponent(yawEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            PartType = ArticulatedPartType.TurretYaw,
            LocalPivotOffset = new Vector3(0f, s * 0.42f, 0f),
            YawMin = -180f,
            YawMax = 180f,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 25f,
            SlewRateDegreesPerSecond = 90f,
        });

        Entity pitchEntity = world.CreateEntity();
        world.AddComponent(pitchEntity, new ArticulatedPartComponent
        {
            Owner = yawEntity,
            PartType = ArticulatedPartType.TurretPitch,
            LocalPivotOffset = new Vector3(0f, s * 0.10f, s * 0.42f),
            PitchMin = -10f,
            PitchMax = 45f,
            IdleSweepEnabled = false,
            SlewRateDegreesPerSecond = 90f,
        });
        world.AddComponent(pitchEntity, new RenderComponent
        {
            MeshKey = ArticulatedStationPartMeshes.TurretBarrelKeyPrefix,
            MeshId = -1,
            Visible = true,
            PrimitiveType = 4,
        });
    }

    private static void SpawnMissileBattery(World world, Entity owner)
    {
        float s = PivotScale;

        Entity podEntity = world.CreateEntity();
        world.AddComponent(podEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            PartType = ArticulatedPartType.TurretYaw,
            LocalPivotOffset = new Vector3(0f, s * 0.48f, s * 0.12f),
            YawMin = -120f,
            YawMax = 120f,
            IdleSweepEnabled = false,
            SlewRateDegreesPerSecond = 45f,
        });
        world.AddComponent(podEntity, new RenderComponent
        {
            MeshKey = ArticulatedStationPartMeshes.MissilePodKeyPrefix,
            MeshId = -1,
            Visible = true,
            PrimitiveType = 4,
        });
    }

    /// <summary>
    /// Sensor dish child: compose order is
    /// <c>parentModel (incl. <see cref="Build.StationRotationSystem"/> Y) * pivot * dishYaw * meshOffset</c>.
    /// Dish <see cref="ArticulatedPartComponent.CurrentYaw"/> is owner-local; parent spin is not written to owner yaw.
    /// </summary>
    private static void SpawnSensorArray(World world, Entity owner)
    {
        float s = PivotScale;

        Entity dishEntity = world.CreateEntity();
        world.AddComponent(dishEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            PartType = ArticulatedPartType.SensorDish,
            LocalPivotOffset = new Vector3(0f, s * 0.58f, s * 0.08f),
            MeshLocalOffset = new Vector3(0f, -s * 0.58f, -s * 0.08f),
            YawMin = -60f,
            YawMax = 60f,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 25f,
            HasAimTarget = false,
            SlewRateDegreesPerSecond = 90f,
        });
        world.AddComponent(dishEntity, new RenderComponent
        {
            MeshKey = ArticulatedStationPartMeshes.SensorDishKeyPrefix,
            MeshId = -1,
            Visible = true,
            PrimitiveType = 4,
        });
    }

    private static void SpawnShipyard(World world, Entity owner, int bays)
    {
        float s = PivotScale;

        Entity craneEntity = world.CreateEntity();
        world.AddComponent(craneEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            PartType = ArticulatedPartType.Crane,
            LocalPivotOffset = new Vector3(0f, s * 0.78f, s * 0.06f),
            YawMin = -45f,
            YawMax = 45f,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 20f,
            SlewRateDegreesPerSecond = 60f,
        });
        world.AddComponent(craneEntity, new RenderComponent
        {
            MeshKey = ArticulatedStationPartMeshes.ShipyardCraneKeyPrefix,
            MeshId = -1,
            Visible = true,
            PrimitiveType = 4,
        });

        float doorZ = bays >= 5 ? -s * 0.20f : -s * 0.18f;
        Entity doorEntity = world.CreateEntity();
        world.AddComponent(doorEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            PartType = ArticulatedPartType.BayDoor,
            LocalPivotOffset = new Vector3(0f, s * 0.28f, doorZ),
            PitchMin = 0f,
            PitchMax = 90f,
            IdleSweepEnabled = false,
            SlewRateDegreesPerSecond = 60f,
        });
        world.AddComponent(doorEntity, new RenderComponent
        {
            MeshKey = ArticulatedStationPartMeshes.ShipyardBayDoorKeyPrefix,
            MeshId = -1,
            Visible = true,
            PrimitiveType = 4,
        });
    }
}