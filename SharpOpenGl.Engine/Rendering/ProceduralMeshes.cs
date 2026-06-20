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