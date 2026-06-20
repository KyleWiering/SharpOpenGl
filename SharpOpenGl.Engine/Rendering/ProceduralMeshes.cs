using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Platform-agnostic procedural mesh vertex data (pos XYZ + color RGB, stride 6).
/// Used by desktop OpenGL upload and browser WebGL upload.
/// </summary>
public static class ProceduralMeshes
{
    public const int Stride = 6;

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

    public static float[] BuildGrid(int columns, int rows, float cellSize, Vector3 color)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float totalW = columns * cellSize;
        float totalH = rows * cellSize;
        var verts = new List<float>();

        for (int x = 0; x <= columns; x++)
        {
            float px = x * cellSize;
            verts.AddRange(new[] { px, 0f, 0f, r, g, b });
            verts.AddRange(new[] { px, 0f, totalH, r, g, b });
        }

        for (int z = 0; z <= rows; z++)
        {
            float pz = z * cellSize;
            verts.AddRange(new[] { 0f, 0f, pz, r, g, b });
            verts.AddRange(new[] { totalW, 0f, pz, r, g, b });
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

    /// <summary>Cluster of rocks for asteroid / debris scenery.</summary>
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

    private static void AddTri(List<float> verts, Vector3 a, Vector3 b, Vector3 c, float r, float g, float bl)
    {
        verts.AddRange(new[] { a.X, a.Y, a.Z, r, g, bl });
        verts.AddRange(new[] { b.X, b.Y, b.Z, r, g, bl });
        verts.AddRange(new[] { c.X, c.Y, c.Z, r, g, bl });
    }

    public static int VertexCount(float[] vertices) => vertices.Length / Stride;

    private static float[] ClampColors(float[] vertices)
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