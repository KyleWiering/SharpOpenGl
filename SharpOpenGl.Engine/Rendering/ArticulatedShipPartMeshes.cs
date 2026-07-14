using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Low-poly procedural meshes for articulated ship sub-parts (turrets, bay doors, deck segments,
/// launcher pods, sensor dishes, wing flaps).
/// Mesh keys: <c>articulated/turret_yaw/{hullKey}</c>, <c>articulated/bay_door/{hullKey}/port</c>, etc.
/// </summary>
public static class ArticulatedShipPartMeshes
{
    public const string KeyPrefix = "articulated/";

    private const int CylinderSegments = 6;

    private static readonly Vector3 NeutralTint = new(0.55f, 0.55f, 0.58f);

    private readonly record struct HullSpec(float PartScale, float BaseRadius, float BarrelLength);

    private static readonly Dictionary<string, HullSpec> HullSpecs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["corvette_fast"] = new(1f, 0.35f, 1.2f),
        ["gunship_heavy"] = new(1f, 0.45f, 1.6f),
        ["destroyer_assault"] = new(1f, 0.55f, 2.0f),
        ["bomber_heavy"] = new(0.92f, 0.38f, 0f),
        ["carrier_command"] = new(1.35f, 0.58f, 0f),
        ["drone_swarm"] = new(0.48f, 0.24f, 0f),
        ["scout_light"] = new(0.58f, 0.3f, 0f),
        ["fighter_basic"] = new(0.52f, 0.26f, 0f),
        ["interceptor_mk2"] = new(0.54f, 0.27f, 0f),
    };

    private static readonly string[] TurretHullKeys = ["corvette_fast", "gunship_heavy", "destroyer_assault"];

    private static readonly string[] SpecialHullFamilies =
    [
        "bomber_heavy",
        "carrier_command",
        "drone_swarm",
        "scout_light",
        "fighter_basic",
        "interceptor_mk2",
    ];

    /// <summary>All articulated ship part mesh keys (turrets + special-hull families).</summary>
    public static IEnumerable<string> AllPartKeys()
    {
        foreach (string hullKey in TurretHullKeys)
        {
            yield return BuildPartKey("turret_yaw", hullKey);
            yield return BuildPartKey("turret_pitch", hullKey);
        }

        yield return BuildBayDoorKey("bomber_heavy", port: true);
        yield return BuildBayDoorKey("bomber_heavy", port: false);
        yield return BuildPartKey("deck_segment", "carrier_command");
        yield return BuildPartKey("launcher_pod", "drone_swarm");
        yield return BuildPartKey("sensor_dish", "scout_light");
        yield return BuildWingFlapKey("fighter_basic", left: true);
        yield return BuildWingFlapKey("fighter_basic", left: false);
        yield return BuildWingFlapKey("interceptor_mk2", left: true);
        yield return BuildWingFlapKey("interceptor_mk2", left: false);
    }

    public static string BuildPartKey(string partType, string hullKey) =>
        $"{KeyPrefix}{partType}/{NormalizeHullKey(hullKey)}";

    public static string BuildBayDoorKey(string hullKey, bool port) =>
        $"{KeyPrefix}bay_door/{NormalizeHullKey(hullKey)}/{(port ? "port" : "starboard")}";

    public static string BuildWingFlapKey(string hullKey, bool left) =>
        $"{KeyPrefix}wing_flap/{NormalizeHullKey(hullKey)}/{(left ? "left" : "right")}";

    /// <summary>Resolve mesh by stable part key.</summary>
    public static bool TryBuild(string partKey, Vector3 hullTint, out float[] vertices)
    {
        vertices = [];
        if (string.IsNullOrWhiteSpace(partKey)
            || !partKey.StartsWith(KeyPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string[] segments = partKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3 || segments.Length > 4)
            return false;

        string partType = segments[1];
        string hullKey = NormalizeHullKey(segments[2]);
        string? side = segments.Length == 4 ? segments[3] : null;

        if (!HullSpecs.ContainsKey(hullKey))
            return false;

        Vector3 tint = hullTint.LengthSquared > 1e-6f ? hullTint : NeutralTint;
        vertices = partType.ToLowerInvariant() switch
        {
            "turret_yaw" when segments.Length == 3 => BuildTurretYawBase(hullKey, tint),
            "turret_pitch" when segments.Length == 3 => BuildTurretPitchBarrel(hullKey, tint),
            "bay_door" when segments.Length == 4 => BuildBombBayDoor(hullKey, tint, IsPortSide(side)),
            "deck_segment" when segments.Length == 3 => BuildCarrierDeckSegment(hullKey, tint),
            "launcher_pod" when segments.Length == 3 => BuildLauncherPod(hullKey, tint),
            "sensor_dish" when segments.Length == 3 => BuildScoutSensorDish(hullKey, tint),
            "wing_flap" when segments.Length == 4 => BuildWingFlap(hullKey, tint, IsLeftSide(side)),
            _ => [],
        };

        return vertices.Length > 0;
    }

    /// <summary>Short cylinder + collar (~turret pedestal).</summary>
    public static float[] BuildTurretYawBase(string hullKey, Vector3 color)
    {
        hullKey = NormalizeHullKey(hullKey);
        if (!HullSpecs.TryGetValue(hullKey, out HullSpec spec))
            return [];

        float radius = spec.BaseRadius;
        float height = radius * 0.55f;
        float collarRadius = radius * 1.18f;
        float collarHeight = radius * 0.14f;

        var verts = new List<float>();
        AppendCylinder(verts, color, radius, height, CylinderSegments, yBase: 0f);
        AppendCylinder(verts, ScaleColor(color, 1.08f), collarRadius, collarHeight, CylinderSegments, yBase: height);
        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>Tapered prism barrel along +Z.</summary>
    public static float[] BuildTurretPitchBarrel(string hullKey, Vector3 color)
    {
        hullKey = NormalizeHullKey(hullKey);
        if (!HullSpecs.TryGetValue(hullKey, out HullSpec spec))
            return [];

        float length = spec.BarrelLength;
        float baseHalf = spec.BaseRadius * 0.32f;
        float tipHalf = spec.BaseRadius * 0.14f;
        float baseLift = spec.BaseRadius * 0.06f;

        var verts = new List<float>();
        AppendTaperedPrismBarrel(verts, color, baseHalf, tipHalf, length, baseLift);
        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>Hinged bomb-bay door panel; pivot at hinge edge (pitch axis).</summary>
    public static float[] BuildBombBayDoor(string hullKey, Vector3 color, bool mirrored)
    {
        hullKey = NormalizeHullKey(hullKey);
        if (!HullSpecs.TryGetValue(hullKey, out HullSpec spec))
            return [];

        float s = spec.PartScale;
        float sign = mirrored ? -1f : 1f;
        float width = s * 0.42f;
        float height = s * 0.34f;
        float depth = s * 0.06f;
        float lip = s * 0.05f;

        var verts = new List<float>();
        Vector3 hingeL = new(-width * 0.5f * sign, 0f, 0f);
        Vector3 hingeR = new(width * 0.5f * sign, 0f, 0f);
        Vector3 topL = new(-width * 0.46f * sign, height, depth);
        Vector3 topR = new(width * 0.46f * sign, height, depth);
        Vector3 lipL = new(-width * 0.4f * sign, height + lip * 0.35f, depth + lip);
        Vector3 lipR = new(width * 0.4f * sign, height + lip * 0.35f, depth + lip);

        AddQuad(verts, hingeL, hingeR, topR, topL, color.X, color.Y, color.Z);
        AddQuad(verts, topL, topR, lipR, lipL, ScaleColor(color, 1.04f));

        Vector3 innerL = hingeL with { Y = height * 0.12f, Z = depth * 0.35f };
        Vector3 innerR = hingeR with { Y = height * 0.12f, Z = depth * 0.35f };
        AddQuad(verts, innerL, innerR, topR, topL, ScaleColor(color, 0.9f));

        AddTri(verts, hingeL, hingeR, hingeR with { Z = -depth * 0.4f }, color.X * 0.88f, color.Y * 0.88f, color.Z * 0.9f);
        AddTri(verts, hingeL, hingeR with { Z = -depth * 0.4f }, hingeL with { Z = -depth * 0.4f },
            color.X * 0.88f, color.Y * 0.88f, color.Z * 0.9f);

        AddTri(verts, topL, lipL, lipR, ScaleColor(color, 1.08f));
        AddTri(verts, topL, lipR, topR, ScaleColor(color, 1.08f));

        float frameW = width * 0.08f;
        Vector3 f0 = hingeL with { X = hingeL.X - frameW * 0.3f * sign };
        Vector3 f1 = hingeL with { X = hingeL.X + frameW * sign, Y = height * 0.85f };
        Vector3 f2 = hingeL with { X = hingeL.X + frameW * sign, Y = height * 0.85f, Z = depth * 0.5f };
        AddTri(verts, f0, f1, f2, ScaleColor(color, 0.82f));

        Vector3 g0 = hingeR with { X = hingeR.X - frameW * sign, Y = height * 0.85f };
        Vector3 g1 = hingeR with { X = hingeR.X + frameW * 0.3f * sign };
        Vector3 g2 = hingeR with { X = hingeR.X - frameW * sign, Y = height * 0.85f, Z = depth * 0.5f };
        AddTri(verts, g0, g1, g2, ScaleColor(color, 0.82f));

        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>Carrier flight-deck elevator / bay door slab.</summary>
    public static float[] BuildCarrierDeckSegment(string hullKey, Vector3 color)
    {
        hullKey = NormalizeHullKey(hullKey);
        if (!HullSpecs.TryGetValue(hullKey, out HullSpec spec))
            return [];

        float s = spec.PartScale;
        float halfW = s * 0.55f;
        float halfD = s * 0.28f;
        float thickness = s * 0.05f;
        float railH = s * 0.07f;

        var verts = new List<float>();
        Vector3 bl = new(-halfW, 0f, -halfD);
        Vector3 br = new(halfW, 0f, -halfD);
        Vector3 fl = new(-halfW * 0.92f, 0f, halfD);
        Vector3 fr = new(halfW * 0.92f, 0f, halfD);

        AddQuad(verts, bl, br, fr, fl, color.X, color.Y, color.Z);
        AddQuad(verts, bl with { Y = thickness }, fl with { Y = thickness },
            fr with { Y = thickness }, br with { Y = thickness }, ScaleColor(color, 0.92f));

        AddQuad(verts, bl, br, br with { Y = thickness }, bl with { Y = thickness }, ScaleColor(color, 0.86f));
        AddQuad(verts, fl, fr, fr with { Y = thickness }, fl with { Y = thickness }, ScaleColor(color, 0.86f));
        AddQuad(verts, bl, fl, fl with { Y = thickness }, bl with { Y = thickness }, ScaleColor(color, 0.84f));
        AddQuad(verts, br, fr, fr with { Y = thickness }, br with { Y = thickness }, ScaleColor(color, 0.84f));

        Vector3 r0 = new(-halfW, thickness, -halfD * 0.7f);
        Vector3 r1 = new(-halfW * 0.96f, thickness + railH, -halfD * 0.55f);
        Vector3 r2 = new(-halfW * 0.88f, thickness + railH, halfD * 0.55f);
        Vector3 r3 = new(-halfW * 0.84f, thickness, halfD * 0.7f);
        AddQuad(verts, r0, r1, r2, r3, ScaleColor(color, 1.06f));

        Vector3 s0 = new(halfW, thickness, -halfD * 0.7f);
        Vector3 s1 = new(halfW * 0.96f, thickness + railH, -halfD * 0.55f);
        Vector3 s2 = new(halfW * 0.88f, thickness + railH, halfD * 0.55f);
        Vector3 s3 = new(halfW * 0.84f, thickness, halfD * 0.7f);
        AddQuad(verts, s0, s1, s2, s3, ScaleColor(color, 1.06f));

        AddTri(verts,
            new Vector3(0f, thickness * 1.1f, -halfD * 0.2f),
            new Vector3(-halfW * 0.15f, thickness * 1.1f, halfD * 0.15f),
            new Vector3(halfW * 0.15f, thickness * 1.1f, halfD * 0.15f),
            ScaleColor(color, 0.78f));

        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>Small spine-mounted launcher pod with tube hint.</summary>
    public static float[] BuildLauncherPod(string hullKey, Vector3 color)
    {
        hullKey = NormalizeHullKey(hullKey);
        if (!HullSpecs.TryGetValue(hullKey, out HullSpec spec))
            return [];

        float s = spec.PartScale;
        float podRadius = s * 0.14f;
        float podHeight = s * 0.22f;
        float tubeRadius = s * 0.08f;
        float tubeLength = s * 0.28f;

        var verts = new List<float>();
        AppendCylinder(verts, color, podRadius, podHeight, 4, yBase: 0f);
        AppendCylinder(verts, ScaleColor(color, 1.1f), tubeRadius, tubeLength, 4, yBase: podHeight * 0.55f, axisZ: true);

        Vector3 collar0 = new(-podRadius * 1.1f, podHeight * 0.45f, -podRadius * 0.2f);
        Vector3 collar1 = new(podRadius * 1.1f, podHeight * 0.45f, -podRadius * 0.2f);
        Vector3 collar2 = new(podRadius * 0.9f, podHeight * 0.65f, podRadius * 0.35f);
        Vector3 collar3 = new(-podRadius * 0.9f, podHeight * 0.65f, podRadius * 0.35f);
        AddQuad(verts, collar0, collar1, collar2, collar3, ScaleColor(color, 0.95f));

        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>Scout sensor dish — station dish geometry at ~0.35 hull scale.</summary>
    public static float[] BuildScoutSensorDish(string hullKey, Vector3 color)
    {
        hullKey = NormalizeHullKey(hullKey);
        if (!HullSpecs.TryGetValue(hullKey, out HullSpec spec))
            return [];

        float s = spec.PartScale * 0.35f;
        Vector3 accent = ScaleColor(color, 1.15f);
        Vector3 primary = ScaleColor(color, 0.95f);
        Vector3 secondary = ScaleColor(color, 0.85f);

        float pivotY = s * 0.58f;
        float dishH = s * 0.52f - pivotY;
        float dishTop = s * 0.72f - pivotY;
        float dishDepth = s * 0.12f;
        float dishReach = s * 0.18f;

        var verts = new List<float>();
        for (int facet = 0; facet < 6; facet++)
        {
            float ang0 = MathF.PI * 2f * facet / 6f;
            float ang1 = MathF.PI * 2f * (facet + 1) / 6f;
            float x0 = MathF.Cos(ang0) * dishReach;
            float z0 = MathF.Sin(ang0) * dishDepth;
            float x1 = MathF.Cos(ang1) * dishReach;
            float z1 = MathF.Sin(ang1) * dishDepth;
            Vector3 facetColor = facet % 2 == 0 ? accent : accent * 1.08f;
            AddTri(verts,
                new Vector3(0f, dishTop, 0f),
                new Vector3(x0, dishH, z0),
                new Vector3(x1, dishH, z1),
                facetColor.X, facetColor.Y, facetColor.Z);
        }

        AddTri(verts,
            new Vector3(-s * 0.04f, dishTop, -s * 0.02f),
            new Vector3(s * 0.04f, dishTop, -s * 0.02f),
            new Vector3(0f, dishTop + s * 0.04f, s * 0.02f),
            accent.X * 1.12f, accent.Y * 1.12f, accent.Z * 1.12f);
        AddTri(verts,
            new Vector3(-s * 0.03f, dishH, s * 0.06f),
            new Vector3(s * 0.03f, dishH, s * 0.06f),
            new Vector3(0f, dishTop, s * 0.04f),
            primary.X, primary.Y, primary.Z);
        AddTri(verts,
            new Vector3(-s * 0.05f, dishH * 0.5f, -s * 0.04f),
            new Vector3(s * 0.05f, dishH * 0.5f, -s * 0.04f),
            new Vector3(0f, dishH, 0f),
            secondary.X, secondary.Y, secondary.Z);

        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>Subtle wing flap trapezoid; pivot at root edge.</summary>
    public static float[] BuildWingFlap(string hullKey, Vector3 color, bool left)
    {
        hullKey = NormalizeHullKey(hullKey);
        if (!HullSpecs.TryGetValue(hullKey, out HullSpec spec))
            return [];

        float s = spec.PartScale;
        float sign = left ? -1f : 1f;
        float rootX = 0f;
        float tipX = s * 0.34f * sign;
        float rootZ = -s * 0.08f;
        float tipZ = s * 0.18f;
        float thickness = s * 0.025f;

        var verts = new List<float>();
        Vector3 r0 = new(rootX, 0f, rootZ);
        Vector3 r1 = new(rootX, 0f, rootZ + s * 0.06f);
        Vector3 t0 = new(tipX, s * 0.04f, tipZ);
        Vector3 t1 = new(tipX * 0.92f, s * 0.06f, tipZ + s * 0.04f);

        AddQuad(verts, r0, r1, t1, t0, color.X, color.Y, color.Z);
        AddQuad(verts, r0 with { Y = thickness }, t0 with { Y = thickness },
            t1 with { Y = thickness }, r1 with { Y = thickness }, ScaleColor(color, 0.9f));
        AddTri(verts, r0, r1, r1 with { Y = thickness }, ScaleColor(color, 0.86f));
        AddTri(verts, r0, r1 with { Y = thickness }, r0 with { Y = thickness }, ScaleColor(color, 0.86f));
        AddTri(verts, t0, t1, t1 with { Y = thickness }, ScaleColor(color, 0.94f));
        AddTri(verts, t0, t1 with { Y = thickness }, t0 with { Y = thickness }, ScaleColor(color, 0.94f));

        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    public static int TriangleCount(float[] vertices) =>
        vertices.Length / ProceduralMeshes.Stride / 3;

    public static IReadOnlyList<string> SpecialHullFamilyKeys => SpecialHullFamilies;

    private static string NormalizeHullKey(string hullKey) => hullKey.ToLowerInvariant() switch
    {
        "corvette" => "corvette_fast",
        "gunship" => "gunship_heavy",
        "destroyer" => "destroyer_assault",
        "bomber" => "bomber_heavy",
        "carrier" => "carrier_command",
        "drone" => "drone_swarm",
        "scout" => "scout_light",
        "fighter" => "fighter_basic",
        "interceptor" => "interceptor_mk2",
        _ => hullKey,
    };

    private static bool IsPortSide(string? side) =>
        !side?.Equals("starboard", StringComparison.OrdinalIgnoreCase) ?? true;

    private static bool IsLeftSide(string? side) =>
        !side?.Equals("right", StringComparison.OrdinalIgnoreCase) ?? true;

    private static Vector3 ScaleColor(Vector3 color, float scale) =>
        new(color.X * scale, color.Y * scale, color.Z * scale);

    private static void AppendCylinder(
        List<float> verts, Vector3 color, float radius, float height, int segments, float yBase, bool axisZ = false)
    {
        float r = color.X, g = color.Y, b = color.Z;

        for (int i = 0; i < segments; i++)
        {
            float angle0 = MathF.PI * 2f * i / segments;
            float angle1 = MathF.PI * 2f * (i + 1) / segments;

            float x0 = MathF.Cos(angle0) * radius;
            float z0 = MathF.Sin(angle0) * radius;
            float x1 = MathF.Cos(angle1) * radius;
            float z1 = MathF.Sin(angle1) * radius;

            if (axisZ)
            {
                var base0 = new Vector3(x0, yBase, z0);
                var base1 = new Vector3(x1, yBase, z1);
                var tip0 = new Vector3(x0, yBase, z0 + height);
                var tip1 = new Vector3(x1, yBase, z1 + height);
                AddTri(verts, base0, base1, tip1, r, g, b);
                AddTri(verts, base0, tip1, tip0, r, g, b);
            }
            else
            {
                var bottom0 = new Vector3(x0, yBase, z0);
                var bottom1 = new Vector3(x1, yBase, z1);
                var top0 = new Vector3(x0, yBase + height, z0);
                var top1 = new Vector3(x1, yBase + height, z1);

                AddTri(verts, bottom0, bottom1, top1, r, g, b);
                AddTri(verts, bottom0, top1, top0, r, g, b);

                AddTri(verts, Vector3.Zero with { Y = yBase }, bottom1, bottom0, r * 0.88f, g * 0.88f, b * 0.9f);
                AddTri(verts, Vector3.Zero with { Y = yBase + height }, top0, top1, r * 1.06f, g * 1.06f, b * 1.04f);
            }
        }
    }

    private static void AppendTaperedPrismBarrel(
        List<float> verts, Vector3 color, float baseHalf, float tipHalf, float length, float baseLift)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float midZ = length * 0.42f;

        Vector3 b0 = new(-baseHalf, baseLift, 0f);
        Vector3 b1 = new(baseHalf, baseLift, 0f);
        Vector3 b2 = new(baseHalf, baseLift * 1.6f, 0f);
        Vector3 b3 = new(-baseHalf, baseLift * 1.6f, 0f);

        Vector3 m0 = new(-baseHalf * 0.78f, baseLift * 1.2f, midZ);
        Vector3 m1 = new(baseHalf * 0.78f, baseLift * 1.2f, midZ);
        Vector3 m2 = new(baseHalf * 0.62f, baseLift * 1.45f, midZ);
        Vector3 m3 = new(-baseHalf * 0.62f, baseLift * 1.45f, midZ);

        Vector3 t0 = new(-tipHalf, baseLift * 0.9f, length);
        Vector3 t1 = new(tipHalf, baseLift * 0.9f, length);
        Vector3 t2 = new(tipHalf * 0.55f, baseLift * 1.1f, length);
        Vector3 t3 = new(-tipHalf * 0.55f, baseLift * 1.1f, length);

        AddQuad(verts, b0, b1, m1, m0, r, g, b);
        AddQuad(verts, b1, b2, m2, m1, r * 0.95f, g * 0.95f, b * 0.96f);
        AddQuad(verts, b2, b3, m3, m2, r * 0.92f, g * 0.92f, b * 0.94f);
        AddQuad(verts, b3, b0, m0, m3, r * 0.9f, g * 0.9f, b * 0.92f);

        AddQuad(verts, m0, m1, t1, t0, r * 1.02f, g * 1.02f, b * 1.03f);
        AddQuad(verts, m1, m2, t2, t1, r * 1.04f, g * 1.04f, b * 1.05f);
        AddQuad(verts, m2, m3, t3, t2, r * 1.06f, g * 1.06f, b * 1.07f);
        AddQuad(verts, m3, m0, t0, t3, r * 1.0f, g * 1.0f, b * 1.01f);

        AddTri(verts, b0, b2, b1, r * 0.85f, g * 0.85f, b * 0.87f);
        AddTri(verts, b0, b3, b2, r * 0.85f, g * 0.85f, b * 0.87f);
        AddTri(verts, t0, t1, t2, r * 1.1f, g * 1.1f, b * 1.12f);
        AddTri(verts, t0, t2, t3, r * 1.1f, g * 1.1f, b * 1.12f);
    }

    private static void AddQuad(
        List<float> verts,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        float r, float g, float bl)
    {
        AddTri(verts, a, b, c, r, g, bl);
        AddTri(verts, a, c, d, r, g, bl);
    }

    private static void AddQuad(List<float> verts, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 color) =>
        AddQuad(verts, a, b, c, d, color.X, color.Y, color.Z);

    private static void AddTri(
        List<float> verts, Vector3 a, Vector3 b, Vector3 c, float r, float g, float bl)
    {
        verts.AddRange([a.X, a.Y, a.Z, r, g, bl]);
        verts.AddRange([b.X, b.Y, b.Z, r, g, bl]);
        verts.AddRange([c.X, c.Y, c.Z, r, g, bl]);
    }

    private static void AddTri(List<float> verts, Vector3 a, Vector3 b, Vector3 c, Vector3 color) =>
        AddTri(verts, a, b, c, color.X, color.Y, color.Z);
}