using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Procedural sub-part meshes for articulated station weapons (turret barrel, missile pod).
/// Geometry is authored in part-local space with the pivot at the part origin.
/// </summary>
public static class ArticulatedStationPartMeshes
{
    /// <summary>Mesh cache key prefix for a race-tinted turret barrel.</summary>
    public const string TurretBarrelKeyPrefix = "station_part:turret_barrel";

    /// <summary>Mesh cache key prefix for a race-tinted missile launcher pod.</summary>
    public const string MissilePodKeyPrefix = "station_part:missile_pod";

    /// <summary>Mesh cache key prefix for a race-tinted sensor dish.</summary>
    public const string SensorDishKeyPrefix = "station_part:sensor_dish";

    /// <summary>Mesh cache key prefix for a race-tinted shipyard crane arm.</summary>
    public const string ShipyardCraneKeyPrefix = "station_part:shipyard_crane";

    /// <summary>Mesh cache key prefix for a race-tinted shipyard bay door.</summary>
    public const string ShipyardBayDoorKeyPrefix = "station_part:shipyard_bay_door";

    public const string CommMastKeyPrefix = "station_part:comm_mast";
    public const string ReactorRingKeyPrefix = "station_part:reactor_ring";
    public const string DepotCraneKeyPrefix = "station_part:depot_crane";
    public const string RefineryArmKeyPrefix = "station_part:refinery_arm";
    public const string RepairBayDoorKeyPrefix = "station_part:repair_bay_door";
    public const string RepairCraneKeyPrefix = "station_part:repair_crane";
    public const string AssemblerArmKeyPrefix = "station_part:assembler_arm";
    public const string CommsDishKeyPrefix = "station_part:comms_dish";
    public const string UplinkDishKeyPrefix = "station_part:uplink_dish";
    public const string ShieldDishKeyPrefix = "station_part:shield_dish";
    public const string FortressBarrelKeyPrefix = "station_part:fortress_barrel";
    public const string FortressPodKeyPrefix = "station_part:fortress_pod";

    /// <summary>All station part key prefixes registered for procedural mesh upload.</summary>
    public static readonly string[] AllPartKeyPrefixes =
    [
        TurretBarrelKeyPrefix,
        MissilePodKeyPrefix,
        SensorDishKeyPrefix,
        ShipyardCraneKeyPrefix,
        ShipyardBayDoorKeyPrefix,
        CommMastKeyPrefix,
        ReactorRingKeyPrefix,
        DepotCraneKeyPrefix,
        RefineryArmKeyPrefix,
        RepairBayDoorKeyPrefix,
        RepairCraneKeyPrefix,
        AssemblerArmKeyPrefix,
        CommsDishKeyPrefix,
        UplinkDishKeyPrefix,
        ShieldDishKeyPrefix,
        FortressBarrelKeyPrefix,
        FortressPodKeyPrefix,
    ];

    /// <summary>
    /// Builds a tapered laser barrel along +Z from the pitch pivot (barrel root).
    /// Matches default <see cref="RaceStationMeshes"/> defense-turret barrel tri placement.
    /// </summary>
    public static float[] BuildTurretBarrel(string raceId, float scale)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 accent = ToVector3(race.Palette.Accent);
        float s = scale;
        var writer = new RaceMeshWriter();

        float barrelBaseZ = -s * 0.08f;
        float barrelMidZ = s * 0.16f;
        float barrelTipZ = s * 0.30f;

        writer.TriColored(new Vector3(0, 0, 0),
            new Vector3(-s * 0.14f, -s * 0.12f, barrelBaseZ),
            new Vector3(s * 0.14f, -s * 0.12f, barrelBaseZ), accent);
        writer.TriColored(new Vector3(0, 0, 0),
            new Vector3(s * 0.14f, -s * 0.12f, barrelBaseZ),
            new Vector3(0, s * 0.10f, barrelMidZ), accent * 1.15f);
        writer.TriColored(new Vector3(-s * 0.10f, s * 0.04f, barrelMidZ),
            new Vector3(s * 0.10f, s * 0.04f, barrelMidZ),
            new Vector3(0, s * 0.16f, barrelTipZ), accent * 1.2f);

        if (race.Modifiers.Protrusion > 0.25f)
        {
            writer.TriColored(new Vector3(0, 0, barrelTipZ),
                new Vector3(-s * 0.08f, -s * 0.12f, barrelMidZ),
                new Vector3(s * 0.08f, -s * 0.12f, barrelMidZ), accent);
            for (int side = -1; side <= 1; side += 2)
            {
                writer.TriColored(
                    new Vector3(side * s * 0.24f, -s * 0.06f, s * 0.04f),
                    new Vector3(side * s * 0.34f, s * 0.08f, -s * 0.04f),
                    new Vector3(side * s * 0.20f, s * 0.02f, -s * 0.12f),
                    accent * 0.95f);
            }
        }

        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        return writer.ToArray();
    }

    /// <summary>
    /// Builds a wider missile launcher pod with 2–4 tubes, distinct from the laser barrel.
    /// Pivot is at the pod mount center; tubes extend along +Z.
    /// </summary>
    public static float[] BuildMissileLauncherPod(string raceId, float scale)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        Vector3 accent = ToVector3(race.Palette.Accent);
        float s = scale;
        var writer = new RaceMeshWriter();

        float podHalfW = s * 0.22f;
        float podHalfH = s * 0.10f;
        float podDepth = s * 0.14f;

        writer.TriColored(
            new Vector3(-podHalfW, -podHalfH, -podDepth),
            new Vector3(podHalfW, -podHalfH, -podDepth),
            new Vector3(podHalfW, podHalfH, -podDepth), primary);
        writer.TriColored(
            new Vector3(-podHalfW, -podHalfH, -podDepth),
            new Vector3(podHalfW, podHalfH, -podDepth),
            new Vector3(-podHalfW, podHalfH, -podDepth), secondary * 0.9f);

        writer.TriColored(
            new Vector3(-podHalfW, -podHalfH, podDepth),
            new Vector3(podHalfW, podHalfH, podDepth),
            new Vector3(podHalfW, -podHalfH, podDepth), secondary);
        writer.TriColored(
            new Vector3(-podHalfW, -podHalfH, podDepth),
            new Vector3(-podHalfW, podHalfH, podDepth),
            new Vector3(podHalfW, podHalfH, podDepth), primary * 0.92f);

        int tubeCount = race.Modifiers.Protrusion > 0.35f ? 4 : 2;
        float tubeSpacing = s * 0.12f;
        float tubeLen = s * 0.28f;
        float tubeR = s * 0.04f;

        for (int i = 0; i < tubeCount; i++)
        {
            float offset = (i - (tubeCount - 1) * 0.5f) * tubeSpacing;
            var baseCenter = new Vector3(offset, 0f, podDepth * 0.5f);
            var tip = baseCenter + new Vector3(0f, 0f, tubeLen);
            var side = new Vector3(tubeR, 0f, 0f);

            writer.TriColored(baseCenter - side, baseCenter + side, tip, accent);
            writer.TriColored(baseCenter - side, tip, baseCenter + new Vector3(0f, tubeR, 0f), accent * 1.1f);
            writer.TriColored(baseCenter + side, tip, baseCenter + new Vector3(0f, tubeR, 0f), accent * 0.95f);
        }

        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        return writer.ToArray();
    }

    /// <summary>
    /// Optional static turret hub without an integrated barrel (articulation split v1).
    /// Delegates to <see cref="RaceStationMeshes"/> with barrel omission on the default path.
    /// </summary>
    public static float[] BuildTurretHubStaticHull(string raceId, float scale) =>
        RaceStationMeshes.Build("defense_turret", raceId, splitArticulation: true, styleScaleOverride: scale / 7f);

    /// <summary>
    /// Builds a parabolic sensor dish crown in part-local space (pivot at dish hinge).
    /// Matches accent-heavy crown geometry from <see cref="RaceStationMeshes"/> sensor arrays.
    /// </summary>
    public static float[] BuildSensorDish(string raceId, float scale)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        Vector3 accent = ToVector3(race.Palette.Accent);
        float s = scale;
        var writer = new RaceMeshWriter();

        float pivotY = s * 0.58f;
        float dishH = s * 0.52f - pivotY;
        float dishTop = s * 0.72f - pivotY;
        float dishDepth = s * 0.12f;
        float dishReach = s * 0.18f;

        for (int facet = 0; facet < 6; facet++)
        {
            float ang0 = MathF.PI * 2f * facet / 6f;
            float ang1 = MathF.PI * 2f * (facet + 1) / 6f;
            float x0 = MathF.Cos(ang0) * dishReach;
            float z0 = MathF.Sin(ang0) * dishDepth;
            float x1 = MathF.Cos(ang1) * dishReach;
            float z1 = MathF.Sin(ang1) * dishDepth;
            writer.TriColored(
                new Vector3(0f, dishTop, 0f),
                new Vector3(x0, dishH, z0),
                new Vector3(x1, dishH, z1),
                facet % 2 == 0 ? accent : accent * 1.08f);
        }

        writer.TriColored(
            new Vector3(-s * 0.04f, dishTop, -s * 0.02f),
            new Vector3(s * 0.04f, dishTop, -s * 0.02f),
            new Vector3(0f, dishTop + s * 0.04f, s * 0.02f),
            accent * 1.12f);
        writer.TriColored(
            new Vector3(-s * 0.03f, dishH, s * 0.06f),
            new Vector3(s * 0.03f, dishH, s * 0.06f),
            new Vector3(0f, dishTop, s * 0.04f),
            primary * 0.95f);
        writer.TriColored(
            new Vector3(-s * 0.05f, dishH * 0.5f, -s * 0.04f),
            new Vector3(s * 0.05f, dishH * 0.5f, -s * 0.04f),
            new Vector3(0f, dishH, 0f),
            secondary * 0.9f);

        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        return writer.ToArray();
    }

    /// <summary>
    /// Builds the central gantry crane arm in part-local space (pivot at gantry root).
    /// Geometry mirrors the spine + cross-beam from <see cref="RaceStationMeshes"/> shipyards.
    /// </summary>
    public static float[] BuildShipyardCraneArm(string raceId, float scale)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 accent = ToVector3(race.Palette.Accent);
        float s = scale;
        var writer = new RaceMeshWriter();

        float pivotY = s * 0.78f;
        float pivotZ = s * 0.06f;

        writer.TriColored(
            new Vector3(-s * 0.14f, s * 0.16f - pivotY, -pivotZ),
            new Vector3(s * 0.14f, s * 0.16f - pivotY, -pivotZ),
            new Vector3(0f, 0f, 0f),
            accent);
        writer.TriColored(
            new Vector3(-s * 0.10f, s * 0.52f - pivotY, -s * 0.04f - pivotZ),
            new Vector3(s * 0.10f, s * 0.52f - pivotY, -s * 0.04f - pivotZ),
            new Vector3(0f, 0f, 0f),
            primary * 0.95f);
        writer.TriColored(
            new Vector3(-s * 0.08f, 0f, -pivotZ),
            new Vector3(s * 0.08f, 0f, -pivotZ),
            new Vector3(0f, s * 0.10f, 0f),
            accent * 1.1f);
        writer.TriColored(
            new Vector3(-s * 0.06f, s * 0.10f, 0f),
            new Vector3(s * 0.06f, s * 0.10f, 0f),
            new Vector3(0f, s * 0.14f, s * 0.02f),
            accent * 1.05f);

        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        return writer.ToArray();
    }

    /// <summary>
    /// Builds a single bay arch door panel in part-local space (pivot at bay arch hinge).
    /// </summary>
    public static float[] BuildShipyardBayDoor(string raceId, float scale, int bayIndex = 0)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        float s = scale;
        var writer = new RaceMeshWriter();

        float pivotY = s * 0.28f;
        float pivotZ = -s * 0.18f;
        float z = -s * 0.32f + bayIndex * s * 0.26f - pivotZ;
        float bayPeak = s * 0.38f - pivotY;
        float innerPeak = s * 0.28f - pivotY;
        float baseY = s * 0.17f - pivotY;

        writer.TriColored(
            new Vector3(-s * 0.38f, baseY, z),
            new Vector3(s * 0.38f, baseY, z),
            new Vector3(0f, bayPeak, z + s * 0.14f),
            primary * 0.95f);
        writer.TriColored(
            new Vector3(-s * 0.25f, baseY, z),
            new Vector3(s * 0.25f, baseY, z),
            new Vector3(0f, innerPeak, z + s * 0.08f),
            secondary * 0.88f);
        writer.TriColored(
            new Vector3(-s * 0.12f, baseY, z + s * 0.06f),
            new Vector3(s * 0.12f, baseY, z + s * 0.06f),
            new Vector3(0f, innerPeak * 0.6f, z + s * 0.10f),
            primary * 0.92f);

        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        return writer.ToArray();
    }

    public static float[] BuildCommMast(string raceId, float scale) =>
        BuildSensorDish(raceId, scale);

    public static float[] BuildReactorCoreRing(string raceId, float scale)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 accent = ToVector3(race.Palette.Accent);
        float s = scale;
        var writer = new RaceMeshWriter();

        for (int i = 0; i < 10; i++)
        {
            float a0 = MathF.PI * 2f * i / 10f;
            float a1 = MathF.PI * 2f * (i + 1) / 10f;
            float r = s * 0.38f;
            var p0 = new Vector3(MathF.Cos(a0) * r, 0f, MathF.Sin(a0) * r);
            var p1 = new Vector3(MathF.Cos(a1) * r, 0f, MathF.Sin(a1) * r);
            writer.TriColored(p0, p1, new Vector3(0f, s * 0.08f, 0f), accent * (i % 2 == 0 ? 1f : 1.1f));
            writer.TriColored(p0, p1, new Vector3(0f, -s * 0.04f, 0f), accent * 0.85f);
        }

        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        return writer.ToArray();
    }

    public static float[] BuildDepotCrane(string raceId, float scale)
    {
        float[] crane = BuildShipyardCraneArm(raceId, scale);
        return crane.Length > 0 ? crane : BuildCraneSilhouette(raceId, scale, reach: 0.10f);
    }

    public static float[] BuildRefineryArm(string raceId, float scale) =>
        BuildCraneSilhouette(raceId, scale, reach: 0.14f, angled: true);

    public static float[] BuildRepairBayDoor(string raceId, float scale) =>
        BuildShipyardBayDoor(raceId, scale);

    public static float[] BuildRepairCrane(string raceId, float scale) =>
        BuildShipyardCraneArm(raceId, scale);

    public static float[] BuildAssemblerArm(string raceId, float scale) =>
        BuildCraneSilhouette(raceId, scale, reach: 0.12f, joints: 2);

    public static float[] BuildCommsRelayDish(string raceId, float scale) =>
        BuildSensorDish(raceId, scale);

    public static float[] BuildUplinkDish(string raceId, float scale) =>
        BuildSensorDish(raceId, scale);

    public static float[] BuildShieldEmitterDish(string raceId, float scale) =>
        BuildSensorDish(raceId, scale);

    public static float[] BuildFortressTurretBarrel(string raceId, float scale) =>
        BuildTurretBarrel(raceId, scale);

    public static float[] BuildFortressMissilePod(string raceId, float scale) =>
        BuildMissileLauncherPod(raceId, scale);

    /// <summary>Builds procedural station part geometry for a key prefix.</summary>
    public static bool TryBuild(string partKey, string raceId, float scale, out float[] vertices)
    {
        vertices = partKey switch
        {
            TurretBarrelKeyPrefix => BuildTurretBarrel(raceId, scale),
            MissilePodKeyPrefix => BuildMissileLauncherPod(raceId, scale),
            SensorDishKeyPrefix => BuildSensorDish(raceId, scale),
            ShipyardCraneKeyPrefix => BuildShipyardCraneArm(raceId, scale),
            ShipyardBayDoorKeyPrefix => BuildShipyardBayDoor(raceId, scale),
            CommMastKeyPrefix => BuildCommMast(raceId, scale),
            ReactorRingKeyPrefix => BuildReactorCoreRing(raceId, scale),
            DepotCraneKeyPrefix => BuildDepotCrane(raceId, scale),
            RefineryArmKeyPrefix => BuildRefineryArm(raceId, scale),
            RepairBayDoorKeyPrefix => BuildRepairBayDoor(raceId, scale),
            RepairCraneKeyPrefix => BuildRepairCrane(raceId, scale),
            AssemblerArmKeyPrefix => BuildAssemblerArm(raceId, scale),
            CommsDishKeyPrefix => BuildCommsRelayDish(raceId, scale),
            UplinkDishKeyPrefix => BuildUplinkDish(raceId, scale),
            ShieldDishKeyPrefix => BuildShieldEmitterDish(raceId, scale),
            FortressBarrelKeyPrefix => BuildFortressTurretBarrel(raceId, scale),
            FortressPodKeyPrefix => BuildFortressMissilePod(raceId, scale),
            _ => [],
        };

        return vertices.Length > 0;
    }

    private static float[] BuildCraneSilhouette(
        string raceId, float scale, float reach = 0.12f, bool angled = false, int joints = 1)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 accent = ToVector3(race.Palette.Accent);
        float s = scale;
        var writer = new RaceMeshWriter();

        float xOff = angled ? s * 0.08f : 0f;
        writer.TriColored(new Vector3(-s * 0.08f, 0f, 0f), new Vector3(s * 0.08f, 0f, 0f),
            new Vector3(xOff, s * 0.12f, s * reach), accent);
        for (int j = 0; j < joints; j++)
        {
            float y = s * (0.12f + j * 0.10f);
            writer.TriColored(new Vector3(xOff - s * 0.06f, y, s * reach),
                new Vector3(xOff + s * 0.06f, y, s * reach),
                new Vector3(xOff, y + s * 0.10f, s * (reach + 0.04f)), primary * 0.95f);
        }

        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        return writer.ToArray();
    }

    /// <summary>Resolves the mesh cache key for a station part.</summary>
    public static string ResolveMeshKey(string prefix, string raceId) =>
        $"{prefix}:{raceId}";

    private static Vector3 ToVector3(float[] rgb) =>
        rgb.Length >= 3
            ? new Vector3(rgb[0], rgb[1], rgb[2])
            : Vector3.One;
}