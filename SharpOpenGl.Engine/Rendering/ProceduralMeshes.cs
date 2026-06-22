using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Platform-agnostic procedural mesh vertex data (pos XYZ + color RGB, stride 6).
/// Used by desktop OpenGL upload and browser WebGL upload.
/// </summary>
public static class ProceduralMeshes
{
    public const int Stride = 6;

    /// <summary>Build a race-specific ship hull (8 races × 17 hull classes).</summary>
    public static float[] BuildRaceShip(string raceId, string hullOrDefinitionId, Vector3 color, float sizeScale = 1f)
        => RaceShipMeshes.Build(raceId, hullOrDefinitionId, color, sizeScale);

    public static float[] BuildShipDesign(string designId, Vector3 color, float sizeScale = 1f)
        => RaceShipMeshes.BuildDesign(designId, color, sizeScale);

    public static float[] BuildShipDesign(int designIndex, Vector3 color, float sizeScale = 1f)
        => RaceShipMeshes.BuildDesign(designIndex, color, sizeScale);

    public static float[] BuildShipMesh(Vector3 color, float size = 2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        float[] vertices =
        {
             0f,    0f,  s,     r, g, b,
            -s*0.4f, 0f, -s*0.5f, r*0.7f, g*0.7f, b*0.7f,
             s*0.4f, 0f, -s*0.5f, r*0.7f, g*0.7f, b*0.7f,

            -s*0.4f, 0f, -s*0.5f, r*0.5f, g*0.5f, b*0.5f,
            -s*0.8f, 0f, -s*0.3f, r*0.4f, g*0.4f, b*0.4f,
            -s*0.3f, 0f,  0f,     r*0.6f, g*0.6f, b*0.6f,

             s*0.4f, 0f, -s*0.5f, r*0.5f, g*0.5f, b*0.5f,
             s*0.8f, 0f, -s*0.3f, r*0.4f, g*0.4f, b*0.4f,
             s*0.3f, 0f,  0f,     r*0.6f, g*0.6f, b*0.6f,

             0f,    0.3f*s, s*0.5f, r*1.2f, g*1.2f, b*1.2f,
            -s*0.2f, 0f,    0f,     r*0.8f, g*0.8f, b*0.8f,
             s*0.2f, 0f,    0f,     r*0.8f, g*0.8f, b*0.8f,
        };
        return ClampColors(vertices);
    }

    public static float[] BuildGrid(int columns, int rows, float cellSize, Vector3 color, int lineStep = 1)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float totalW = columns * cellSize;
        float totalH = rows * cellSize;
        var verts = new List<float>();

        lineStep = Math.Max(1, lineStep);

        for (int x = 0; x <= columns; x += lineStep)
        {
            float px = x * cellSize;
            verts.AddRange(new[] { px, 0f, 0f, r, g, b });
            verts.AddRange(new[] { px, 0f, totalH, r, g, b });
        }

        if (columns % lineStep != 0)
        {
            verts.AddRange(new[] { totalW, 0f, 0f, r, g, b });
            verts.AddRange(new[] { totalW, 0f, totalH, r, g, b });
        }

        for (int z = 0; z <= rows; z += lineStep)
        {
            float pz = z * cellSize;
            verts.AddRange(new[] { 0f, 0f, pz, r, g, b });
            verts.AddRange(new[] { totalW, 0f, pz, r, g, b });
        }

        if (rows % lineStep != 0)
        {
            verts.AddRange(new[] { 0f, 0f, totalH, r, g, b });
            verts.AddRange(new[] { totalW, 0f, totalH, r, g, b });
        }

        return verts.ToArray();
    }

    /// <summary>Soft radial disc for faction aura (vertex luminance = falloff mask).</summary>
    public static float[] BuildTeamAuraDisc(float radius = 1f, int segments = 32)
    {
        var verts = new List<float>();
        float y = 0.08f;

        for (int i = 0; i < segments; i++)
        {
            float angle0 = MathF.PI * 2f * i / segments;
            float angle1 = MathF.PI * 2f * (i + 1) / segments;

            float x0 = MathF.Cos(angle0) * radius;
            float z0 = MathF.Sin(angle0) * radius;
            float x1 = MathF.Cos(angle1) * radius;
            float z1 = MathF.Sin(angle1) * radius;

            float fade0 = 0.12f + 0.88f * (1f - (i + 0.5f) / segments);
            float fade1 = 0.12f + 0.88f * (1f - (i + 1.5f) / segments);
            verts.AddRange(new[] { 0f, y, 0f, 1f, 1f, 1f });
            verts.AddRange(new[] { x0, y, z0, fade0, fade0, fade0 });
            verts.AddRange(new[] { x1, y, z1, fade1, fade1, fade1 });
        }

        return verts.ToArray();
    }

    public static float[] BuildSelectionRing(Vector3 color, float radius = 2.5f, int segments = 24)
    {
        float r = color.X, g = color.Y, b = color.Z;
        var verts = new List<float>();
        for (int i = 0; i < segments; i++)
        {
            float angle0 = MathF.PI * 2f * i / segments;
            float angle1 = MathF.PI * 2f * (i + 1) / segments;
            verts.AddRange(new[]
            {
                MathF.Cos(angle0) * radius, 0.1f, MathF.Sin(angle0) * radius, r, g, b,
                MathF.Cos(angle1) * radius, 0.1f, MathF.Sin(angle1) * radius, r, g, b,
            });
        }
        return verts.ToArray();
    }

    public static float[] BuildEngineTrail(Vector3 color, float length = 2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float l = length;
        float[] vertices =
        {
             0f,     0f,    0f,     r, g, b,
            -l*0.15f, 0f,  -l,     r*0.2f, g*0.2f, b*0.1f,
             l*0.15f, 0f,  -l,     r*0.2f, g*0.2f, b*0.1f,

             0f,     0f,    0f,     r*0.6f, g*0.6f, b*0.6f,
            -l*0.3f, 0f,   -l*0.7f, r*0.1f, g*0.1f, b*0.05f,
            -l*0.1f, 0f,   -l*0.8f, r*0.1f, g*0.1f, b*0.05f,

             0f,     0f,    0f,     r*0.6f, g*0.6f, b*0.6f,
             l*0.1f, 0f,   -l*0.8f, r*0.1f, g*0.1f, b*0.05f,
             l*0.3f, 0f,   -l*0.7f, r*0.1f, g*0.1f, b*0.05f,
        };
        return ClampColors(vertices);
    }

    public static float[] BuildMoveTarget(Vector3 color, float size = 1.5f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return
        [
            -s, 0.1f, -s, r, g, b,
             s, 0.1f,  s, r, g, b,
             s, 0.1f, -s, r, g, b,
            -s, 0.1f,  s, r, g, b,
        ];
    }

    /// <summary>Filled diamond beacon for harvestable resource nodes.</summary>
    public static float[] BuildResourceNodeMarker(Vector3 color, float size = 2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        float[] vertices =
        {
            0f, s * 1.2f, 0f, r, g, b,
            -s, 0f, 0f, r, g, b,
            0f, 0f, s, r, g, b,

            0f, s * 1.2f, 0f, r, g, b,
            0f, 0f, s, r, g, b,
            s, 0f, 0f, r, g, b,

            0f, s * 1.2f, 0f, r, g, b,
            s, 0f, 0f, r, g, b,
            0f, 0f, -s, r, g, b,

            0f, s * 1.2f, 0f, r, g, b,
            0f, 0f, -s, r, g, b,
            -s, 0f, 0f, r, g, b,

            0f, -s * 0.25f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            0f, 0f, s, r * 0.75f, g * 0.75f, b * 0.75f,
            -s, 0f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,

            0f, -s * 0.25f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            s, 0f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            0f, 0f, s, r * 0.75f, g * 0.75f, b * 0.75f,

            0f, -s * 0.25f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            0f, 0f, -s, r * 0.75f, g * 0.75f, b * 0.75f,
            s, 0f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,

            0f, -s * 0.25f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            -s, 0f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            0f, 0f, -s, r * 0.75f, g * 0.75f, b * 0.75f,
        };
        return ClampColors(vertices);
    }

    /// <summary>Low-poly sphere for neutral or harvestable planets.</summary>
    public static float[] BuildPlanetSphere(Vector3 color, float radius = 4f, int segments = 10)
    {
        float r = color.X, g = color.Y, b = color.Z;
        var verts = new List<float>();

        for (int lat = 0; lat < segments; lat++)
        {
            float t0 = MathF.PI * lat / segments;
            float t1 = MathF.PI * (lat + 1) / segments;
            float y0 = MathF.Cos(t0) * radius;
            float y1 = MathF.Cos(t1) * radius;
            float r0 = MathF.Sin(t0) * radius;
            float r1 = MathF.Sin(t1) * radius;

            for (int lon = 0; lon < segments; lon++)
            {
                float p0 = MathF.PI * 2f * lon / segments;
                float p1 = MathF.PI * 2f * (lon + 1) / segments;

                Vector3 a = new(MathF.Cos(p0) * r0, y0, MathF.Sin(p0) * r0);
                Vector3 bb = new(MathF.Cos(p1) * r0, y0, MathF.Sin(p1) * r0);
                Vector3 c = new(MathF.Cos(p0) * r1, y1, MathF.Sin(p0) * r1);
                Vector3 d = new(MathF.Cos(p1) * r1, y1, MathF.Sin(p1) * r1);

                AddTri(verts, a, bb, c, r, g, b);
                AddTri(verts, bb, d, c, r, g, b);
            }
        }

        return ClampColors(verts.ToArray());
    }

    /// <summary>Cluster of rocks for generic debris scenery.</summary>
    public static float[] BuildSceneryCluster(Vector3 color, float size = 3f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        float[] vertices =
        {
            0f, s * 0.4f, 0f, r, g, b,
            -s * 0.6f, 0f, s * 0.35f, r * 0.85f, g * 0.85f, b * 0.85f,
            s * 0.5f, 0f, s * 0.2f, r * 0.9f, g * 0.9f, b * 0.9f,

            -s * 0.55f, 0f, -s * 0.25f, r * 0.8f, g * 0.8f, b * 0.8f,
            s * 0.45f, 0f, -s * 0.4f, r * 0.75f, g * 0.75f, b * 0.75f,
            0f, s * 0.25f, s * 0.1f, r * 0.95f, g * 0.95f, b * 0.95f,

            -s * 0.3f, 0f, s * 0.55f, r * 0.7f, g * 0.7f, b * 0.7f,
            s * 0.65f, 0f, 0.1f, r * 0.82f, g * 0.82f, b * 0.82f,
            0f, s * 0.15f, -s * 0.5f, r * 0.78f, g * 0.78f, b * 0.78f,
        };
        return ClampColors(vertices);
    }

    /// <summary>Chunky tumbling rock swarm for asteroid field scenery.</summary>
    public static float[] BuildAsteroidFieldCluster(Vector3 baseColor, float size = 3f)
    {
        float s = size;
        float r = baseColor.X, g = baseColor.Y, b = baseColor.Z;
        var verts = new List<float>();

        AddRockChunk(verts, new Vector3(0f, s * 0.42f, 0f), s * 0.58f, r, g, b, 0.12f);
        AddRockChunk(verts, new Vector3(-s * 0.62f, s * 0.18f, s * 0.34f), s * 0.44f, r * 0.92f, g * 0.88f, b * 0.82f, 0.28f);
        AddRockChunk(verts, new Vector3(s * 0.56f, s * 0.08f, s * 0.22f), s * 0.4f, r * 0.88f, g * 0.84f, b * 0.78f, 0.41f);
        AddRockChunk(verts, new Vector3(-s * 0.34f, s * 0.05f, -s * 0.52f), s * 0.48f, r * 0.8f, g * 0.76f, b * 0.7f, 0.55f);
        AddRockChunk(verts, new Vector3(s * 0.38f, s * 0.22f, -s * 0.46f), s * 0.42f, r * 0.86f, g * 0.8f, b * 0.74f, 0.67f);
        AddRockChunk(verts, new Vector3(-s * 0.18f, s * 0.32f, s * 0.52f), s * 0.34f, r * 0.95f, g * 0.9f, b * 0.84f, 0.79f);
        AddRockChunk(verts, new Vector3(s * 0.72f, s * 0.14f, -s * 0.08f), s * 0.3f, r * 0.78f, g * 0.74f, b * 0.68f, 0.91f);
        AddRockChunk(verts, new Vector3(-s * 0.74f, s * 0.1f, -s * 0.16f), s * 0.28f, r * 0.74f, g * 0.7f, b * 0.64f, 1.03f);
        AddRockChunk(verts, new Vector3(0.12f * s, s * 0.12f, s * 0.68f), s * 0.26f, r * 0.9f, g * 0.86f, b * 0.8f, 1.17f);
        AddRockChunk(verts, new Vector3(-0.08f * s, s * 0.26f, -0.12f * s), s * 0.22f, r * 0.98f, g * 0.94f, b * 0.88f, 1.29f);

        return ClampColors(verts.ToArray());
    }

    /// <summary>Soft volumetric-style gas cloud with purple, magenta, and cyan tones.</summary>
    public static float[] BuildNebulaCloud(float size = 3f)
    {
        float s = size;
        var verts = new List<float>();

        AddCloudPuff(verts, new Vector3(0f, s * 0.18f, 0f), s * 0.72f,
            new Vector3(0.42f, 0.1f, 0.62f), new Vector3(0.72f, 0.22f, 0.88f), segments: 10);
        AddCloudPuff(verts, new Vector3(s * 0.22f, s * 0.28f, -s * 0.14f), s * 0.58f,
            new Vector3(0.82f, 0.18f, 0.58f), new Vector3(0.98f, 0.38f, 0.72f), segments: 9);
        AddCloudPuff(verts, new Vector3(-s * 0.28f, s * 0.24f, s * 0.18f), s * 0.52f,
            new Vector3(0.24f, 0.58f, 0.92f), new Vector3(0.38f, 0.82f, 1f), segments: 8);
        AddCloudPuff(verts, new Vector3(s * 0.08f, s * 0.36f, s * 0.3f), s * 0.4f,
            new Vector3(0.55f, 0.12f, 0.75f), new Vector3(0.78f, 0.3f, 0.95f), segments: 7);
        AddCloudPuff(verts, new Vector3(-s * 0.12f, s * 0.32f, -s * 0.34f), s * 0.46f,
            new Vector3(0.68f, 0.22f, 0.82f), new Vector3(0.9f, 0.45f, 0.95f), segments: 8);
        AddCloudWisp(verts, new Vector3(-s * 0.42f, s * 0.2f, -s * 0.08f), s * 0.34f,
            new Vector3(0.18f, 0.72f, 0.95f), new Vector3(0.32f, 0.9f, 1f));
        AddCloudWisp(verts, new Vector3(s * 0.36f, s * 0.16f, s * 0.08f), s * 0.3f,
            new Vector3(0.88f, 0.28f, 0.68f), new Vector3(1f, 0.42f, 0.82f));

        return ClampColors(verts.ToArray());
    }

    public static float[] BuildLaserBolt(Vector3 color, float length = 1.2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float w = length * 0.08f;
        return ClampColors(
        [
            0f, 0f, 0f, r, g, b,
            0f, w, 0f, r, g, b,
            0f, 0f, length, r * 1.2f, g * 1.2f, b * 1.2f,
            0f, 0f, 0f, r, g, b,
            0f, 0f, length, r * 1.2f, g * 1.2f, b * 1.2f,
            0f, -w, 0f, r, g, b,
        ]);
    }

    public static float[] BuildBeamStreak(Vector3 color, float length = 2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float w = length * 0.05f;
        return ClampColors(
        [
            0f, w, -length * 0.5f, r, g, b,
            0f, -w, -length * 0.5f, r, g, b,
            0f, w, length * 0.5f, r * 1.3f, g * 1.3f, b * 1.3f,
            0f, -w, -length * 0.5f, r, g, b,
            0f, -w, length * 0.5f, r * 1.3f, g * 1.3f, b * 1.3f,
            0f, w, length * 0.5f, r * 1.3f, g * 1.3f, b * 1.3f,
        ]);
    }

    public static float[] BuildTorpedo(Vector3 color, float size = 1.4f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            0f, 0f, s, r, g, b,
            -s * 0.25f, 0f, -s * 0.6f, r * 0.8f, g * 0.8f, b * 0.8f,
            s * 0.25f, 0f, -s * 0.6f, r * 0.8f, g * 0.8f, b * 0.8f,
            -s * 0.25f, 0f, -s * 0.6f, r * 0.8f, g * 0.8f, b * 0.8f,
            s * 0.25f, 0f, -s * 0.6f, r * 0.8f, g * 0.8f, b * 0.8f,
            0f, 0f, -s, r * 0.7f, g * 0.7f, b * 0.7f,
        ]);
    }

    public static float[] BuildRocket(Vector3 color, float size = 1f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            0f, 0f, s * 0.8f, r, g, b,
            -s * 0.2f, 0f, -s * 0.5f, r * 0.85f, g * 0.85f, b * 0.85f,
            s * 0.2f, 0f, -s * 0.5f, r * 0.85f, g * 0.85f, b * 0.85f,
            -s * 0.35f, 0f, -s * 0.2f, r * 0.7f, g * 0.7f, b * 0.7f,
            s * 0.35f, 0f, -s * 0.2f, r * 0.7f, g * 0.7f, b * 0.7f,
            0f, 0f, -s * 0.7f, r * 0.6f, g * 0.6f, b * 0.6f,
        ]);
    }

    public static float[] BuildBomb(Vector3 color, float radius = 1.2f)
        => BuildPlanetSphere(color, radius, segments: 6);

    public static float[] BuildEnergyPulse(Vector3 color, float size = 1f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            0f, s * 0.35f, 0f, r, g, b,
            -s * 0.45f, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            s * 0.45f, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            -s * 0.45f, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            0f, 0f, s * 0.45f, r * 0.85f, g * 0.85f, b * 0.85f,
            s * 0.45f, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
        ]);
    }

    public static float[] BuildWaveRing(Vector3 color, float radius = 1.5f, int segments = 16)
        => BuildSelectionRing(color, radius, segments);

    /// <summary>Small octahedron mining drone.</summary>
    public static float[] BuildMiningDrone(Vector3 color, float size = 0.8f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            0f, s, 0f, r, g, b,
            -s, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            0f, 0f, s, r * 0.85f, g * 0.85f, b * 0.85f,
            0f, s, 0f, r, g, b,
            0f, 0f, s, r * 0.85f, g * 0.85f, b * 0.85f,
            s, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            0f, -s, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            -s, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            0f, 0f, -s, r * 0.8f, g * 0.8f, b * 0.8f,
            0f, -s, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            0f, 0f, -s, r * 0.8f, g * 0.8f, b * 0.8f,
            s, 0f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
        ]);
    }

    /// <summary>Compact gun turret placeholder.</summary>
    public static float[] BuildDefenseTurret(Vector3 color, float size = 3f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            0f, s * 0.35f, 0f, r, g, b,
            -s * 0.35f, 0f, -s * 0.35f, r * 0.85f, g * 0.85f, b * 0.85f,
            s * 0.35f, 0f, -s * 0.35f, r * 0.85f, g * 0.85f, b * 0.85f,
            -s * 0.35f, 0f, -s * 0.35f, r * 0.85f, g * 0.85f, b * 0.85f,
            s * 0.35f, 0f, s * 0.35f, r * 0.8f, g * 0.8f, b * 0.8f,
            s * 0.35f, 0f, -s * 0.35f, r * 0.85f, g * 0.85f, b * 0.85f,
            0f, s * 0.15f, s * 0.55f, r * 1.1f, g * 1.1f, b * 1.1f,
            -s * 0.12f, s * 0.1f, s * 0.35f, r * 0.95f, g * 0.95f, b * 0.95f,
            s * 0.12f, s * 0.1f, s * 0.35f, r * 0.95f, g * 0.95f, b * 0.95f,
        ]);
    }

    /// <summary>Dish-on-pole sensor array placeholder.</summary>
    public static float[] BuildSensorArray(Vector3 color, float size = 4f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            0f, s * 0.55f, 0f, r, g, b,
            -s * 0.45f, s * 0.1f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            s * 0.45f, s * 0.1f, 0f, r * 0.9f, g * 0.9f, b * 0.9f,
            0f, s * 0.05f, 0f, r * 0.75f, g * 0.75f, b * 0.75f,
            -s * 0.08f, 0f, 0f, r * 0.7f, g * 0.7f, b * 0.7f,
            s * 0.08f, 0f, 0f, r * 0.7f, g * 0.7f, b * 0.7f,
            0f, s * 0.05f, s * 0.12f, r * 0.8f, g * 0.8f, b * 0.8f,
            -s * 0.08f, 0f, 0f, r * 0.7f, g * 0.7f, b * 0.7f,
            0f, s * 0.05f, s * 0.12f, r * 0.8f, g * 0.8f, b * 0.8f,
            s * 0.08f, 0f, 0f, r * 0.7f, g * 0.7f, b * 0.7f,
        ]);
    }

    /// <summary>Industrial refinery placeholder.</summary>
    public static float[] BuildResourceRefinery(Vector3 color, float size = 5f)
        => BuildSceneryCluster(color, size);

    /// <summary>Hangar-style repair bay placeholder.</summary>
    public static float[] BuildRepairBay(Vector3 color, float size = 6f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            -s * 0.5f, 0f, -s * 0.35f, r, g, b,
            s * 0.5f, 0f, -s * 0.35f, r, g, b,
            s * 0.5f, s * 0.35f, -s * 0.35f, r * 0.9f, g * 0.9f, b * 0.9f,
            -s * 0.5f, 0f, -s * 0.35f, r, g, b,
            s * 0.5f, s * 0.35f, -s * 0.35f, r * 0.9f, g * 0.9f, b * 0.9f,
            -s * 0.5f, s * 0.35f, -s * 0.35f, r * 0.85f, g * 0.85f, b * 0.85f,
            -s * 0.5f, 0f, s * 0.35f, r * 0.95f, g * 0.95f, b * 0.95f,
            s * 0.5f, 0f, s * 0.35f, r * 0.95f, g * 0.95f, b * 0.95f,
            s * 0.5f, s * 0.35f, s * 0.35f, r * 0.88f, g * 0.88f, b * 0.88f,
            -s * 0.5f, 0f, s * 0.35f, r * 0.95f, g * 0.95f, b * 0.95f,
            s * 0.5f, s * 0.35f, s * 0.35f, r * 0.88f, g * 0.88f, b * 0.88f,
            -s * 0.5f, s * 0.35f, s * 0.35f, r * 0.85f, g * 0.85f, b * 0.85f,
        ]);
    }

    /// <summary>Glowing reactor dome placeholder.</summary>
    public static float[] BuildPowerReactor(Vector3 color, float size = 4.5f)
        => BuildPlanetSphere(color, size * 0.35f, segments: 8);

    /// <summary>Crate cluster supply depot placeholder.</summary>
    public static float[] BuildSupplyDepot(Vector3 color, float size = 4f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            -s * 0.35f, 0f, -s * 0.25f, r, g, b,
            s * 0.2f, 0f, -s * 0.25f, r * 0.9f, g * 0.9f, b * 0.9f,
            -s * 0.1f, s * 0.3f, 0f, r * 0.95f, g * 0.95f, b * 0.95f,
            s * 0.15f, 0f, s * 0.2f, r * 0.85f, g * 0.85f, b * 0.85f,
            s * 0.45f, 0f, -s * 0.1f, r * 0.88f, g * 0.88f, b * 0.88f,
            s * 0.35f, s * 0.25f, s * 0.15f, r * 0.92f, g * 0.92f, b * 0.92f,
        ]);
    }

    /// <summary>Low-poly EVA astronaut with tool arm.</summary>
    public static float[] BuildEvaAstronaut(Vector3 color, float size = 1.2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        return ClampColors(
        [
            0f, s * 0.9f, 0f, r, g, b,
            -s * 0.25f, s * 0.35f, 0f, r * 0.95f, g * 0.95f, b * 0.95f,
            s * 0.25f, s * 0.35f, 0f, r * 0.95f, g * 0.95f, b * 0.95f,
            -s * 0.25f, s * 0.35f, 0f, r * 0.95f, g * 0.95f, b * 0.95f,
            0f, s * 0.05f, s * 0.2f, r * 0.85f, g * 0.85f, b * 0.85f,
            s * 0.25f, s * 0.35f, 0f, r * 0.95f, g * 0.95f, b * 0.95f,
            -s * 0.18f, 0f, 0f, r * 0.8f, g * 0.8f, b * 0.8f,
            s * 0.18f, 0f, 0f, r * 0.8f, g * 0.8f, b * 0.8f,
            0f, s * 0.05f, -s * 0.15f, r * 0.82f, g * 0.82f, b * 0.82f,
            -s * 0.18f, 0f, 0f, r * 0.8f, g * 0.8f, b * 0.8f,
            0f, s * 0.05f, -s * 0.15f, r * 0.82f, g * 0.82f, b * 0.82f,
            s * 0.18f, 0f, 0f, r * 0.8f, g * 0.8f, b * 0.8f,
            s * 0.35f, s * 0.45f, s * 0.1f, r * 0.7f, g * 0.75f, b * 0.9f,
            s * 0.55f, s * 0.55f, s * 0.15f, r * 0.65f, g * 0.7f, b * 0.85f,
            s * 0.35f, s * 0.25f, s * 0.1f, r * 0.7f, g * 0.75f, b * 0.9f,
        ]);
    }

    private static void AddRockChunk(
        List<float> verts, Vector3 center, float radius, float r, float g, float bl, float phase)
    {
        Vector3[] points =
        [
            center + new Vector3(0f, radius * 0.9f, 0f),
            center + new Vector3(radius * 0.75f, radius * 0.1f, radius * 0.35f),
            center + new Vector3(-radius * 0.55f, radius * 0.2f, radius * 0.55f),
            center + new Vector3(radius * 0.2f, radius * 0.05f, -radius * 0.7f),
            center + new Vector3(-radius * 0.65f, radius * 0.15f, -radius * 0.35f),
            center + new Vector3(radius * 0.45f, radius * 0.25f, -radius * 0.15f),
        ];

        for (int i = 0; i < points.Length; i++)
        {
            float wobble = MathF.Sin(phase + i * 1.7f) * radius * 0.08f;
            points[i] += new Vector3(wobble, wobble * 0.5f, -wobble * 0.6f);
        }

        int[][] faces =
        [
            [0, 1, 2],
            [0, 2, 4],
            [0, 4, 5],
            [0, 5, 1],
            [3, 2, 1],
            [3, 4, 2],
            [3, 5, 4],
            [3, 1, 5],
            [1, 5, 2],
            [2, 5, 4],
        ];

        foreach (int[] face in faces)
        {
            float shade = 0.78f + 0.22f * MathF.Abs(MathF.Sin(phase + face[0]));
            AddTri(verts, points[face[0]], points[face[1]], points[face[2]], r * shade, g * shade, bl * shade);
        }
    }

    private static void AddCloudPuff(
        List<float> verts, Vector3 center, float radius, Vector3 coreColor, Vector3 edgeColor, int segments)
    {
        for (int lat = 0; lat < segments; lat++)
        {
            float t0 = MathF.PI * 0.5f * lat / segments;
            float t1 = MathF.PI * 0.5f * (lat + 1) / segments;
            float y0 = center.Y + MathF.Sin(t0) * radius;
            float y1 = center.Y + MathF.Sin(t1) * radius;
            float r0 = MathF.Cos(t0) * radius;
            float r1 = MathF.Cos(t1) * radius;
            float blend0 = lat / (float)segments;
            float blend1 = (lat + 1) / (float)segments;
            Vector3 color0 = Vector3.Lerp(coreColor, edgeColor, blend0);
            Vector3 color1 = Vector3.Lerp(coreColor, edgeColor, blend1);

            for (int lon = 0; lon < segments; lon++)
            {
                float p0 = MathF.PI * 2f * lon / segments;
                float p1 = MathF.PI * 2f * (lon + 1) / segments;

                Vector3 a = new(
                    center.X + MathF.Cos(p0) * r0,
                    y0,
                    center.Z + MathF.Sin(p0) * r0);
                Vector3 b = new(
                    center.X + MathF.Cos(p1) * r0,
                    y0,
                    center.Z + MathF.Sin(p1) * r0);
                Vector3 c = new(
                    center.X + MathF.Cos(p0) * r1,
                    y1,
                    center.Z + MathF.Sin(p0) * r1);
                Vector3 d = new(
                    center.X + MathF.Cos(p1) * r1,
                    y1,
                    center.Z + MathF.Sin(p1) * r1);

                AddTri(verts, center, a, b, coreColor.X, coreColor.Y, coreColor.Z);
                AddTri(verts, a, c, b, color0.X, color0.Y, color0.Z);
                AddTri(verts, b, c, d, color1.X, color1.Y, color1.Z);
            }
        }
    }

    private static void AddCloudWisp(
        List<float> verts, Vector3 center, float radius, Vector3 coreColor, Vector3 edgeColor)
    {
        int segments = 6;
        for (int i = 0; i < segments; i++)
        {
            float angle0 = MathF.PI * 2f * i / segments;
            float angle1 = MathF.PI * 2f * (i + 1) / segments;
            float lift = radius * 0.35f;

            Vector3 inner = center + new Vector3(0f, lift * 0.4f, 0f);
            Vector3 a = center + new Vector3(MathF.Cos(angle0) * radius, lift, MathF.Sin(angle0) * radius);
            Vector3 b = center + new Vector3(MathF.Cos(angle1) * radius, lift, MathF.Sin(angle1) * radius);
            Vector3 c = center + new Vector3(MathF.Cos((angle0 + angle1) * 0.5f) * radius * 1.2f, lift * 1.35f,
                MathF.Sin((angle0 + angle1) * 0.5f) * radius * 1.2f);

            AddTri(verts, inner, a, b, coreColor.X, coreColor.Y, coreColor.Z);
            AddTri(verts, a, c, b, edgeColor.X, edgeColor.Y, edgeColor.Z);
        }
    }

    private static void AddTri(List<float> verts, Vector3 a, Vector3 b, Vector3 c, float r, float g, float bl)
    {
        verts.AddRange(new[] { a.X, a.Y, a.Z, r, g, bl });
        verts.AddRange(new[] { b.X, b.Y, b.Z, r, g, bl });
        verts.AddRange(new[] { c.X, c.Y, c.Z, r, g, bl });
    }

    public static int VertexCount(float[] vertices) => vertices.Length / Stride;

    internal static float[] ClampColors(float[] vertices)
    {
        for (int i = 0; i < vertices.Length; i += Stride)
        {
            vertices[i + 3] = MathHelper.Clamp(vertices[i + 3], 0f, 1f);
            vertices[i + 4] = MathHelper.Clamp(vertices[i + 4], 0f, 1f);
            vertices[i + 5] = MathHelper.Clamp(vertices[i + 5], 0f, 1f);
        }
        return vertices;
    }
}