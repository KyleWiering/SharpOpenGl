using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

/// <summary>
/// Builds procedural ship meshes for the RTS game.
/// Creates simple triangle/diamond shapes colored by role.
/// </summary>
public static class ShipMeshBuilder
{
    /// <summary>
    /// Build a simple arrow/ship shape pointing along +Z, centered at origin.
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildShipMesh(
        Vector3 color, float size = 2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;

        // A diamond/arrow shape (top-down view, pointing +Z)
        float[] vertices =
        {
            // Triangle 1 - nose
             0f,    0f,  s,     r, g, b,   // front tip
            -s*0.4f, 0f, -s*0.5f, r*0.7f, g*0.7f, b*0.7f, // back left
             s*0.4f, 0f, -s*0.5f, r*0.7f, g*0.7f, b*0.7f, // back right

            // Triangle 2 - left wing
            -s*0.4f, 0f, -s*0.5f, r*0.5f, g*0.5f, b*0.5f,
            -s*0.8f, 0f, -s*0.3f, r*0.4f, g*0.4f, b*0.4f,
            -s*0.3f, 0f,  0f,     r*0.6f, g*0.6f, b*0.6f,

            // Triangle 3 - right wing
             s*0.4f, 0f, -s*0.5f, r*0.5f, g*0.5f, b*0.5f,
             s*0.8f, 0f, -s*0.3f, r*0.4f, g*0.4f, b*0.4f,
             s*0.3f, 0f,  0f,     r*0.6f, g*0.6f, b*0.6f,

            // Triangle 4 - slight height for 3D feel
             0f,    0.3f*s, s*0.5f, r*1.2f, g*1.2f, b*1.2f,
            -s*0.2f, 0f,    0f,     r*0.8f, g*0.8f, b*0.8f,
             s*0.2f, 0f,    0f,     r*0.8f, g*0.8f, b*0.8f,
        };

        // Clamp colors to [0,1]
        for (int i = 0; i < vertices.Length; i += 6)
        {
            vertices[i + 3] = MathHelper.Clamp(vertices[i + 3], 0f, 1f);
            vertices[i + 4] = MathHelper.Clamp(vertices[i + 4], 0f, 1f);
            vertices[i + 5] = MathHelper.Clamp(vertices[i + 5], 0f, 1f);
        }

        return UploadMesh(vertices);
    }

    /// <summary>
    /// Build a selection ring (circle of lines) in the XZ plane.
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildSelectionRing(
        Vector3 color, float radius = 2.5f, int segments = 24)
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

        return UploadMesh(verts.ToArray(), PrimitiveType.Lines);
    }

    /// <summary>
    /// Build a move-target indicator (X shape).
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildMoveTarget(
        Vector3 color, float size = 1.5f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;
        float[] vertices =
        {
            -s, 0.1f, -s, r, g, b,
             s, 0.1f,  s, r, g, b,
             s, 0.1f, -s, r, g, b,
            -s, 0.1f,  s, r, g, b,
        };
        return UploadMesh(vertices, PrimitiveType.Lines);
    }

    /// <summary>
    /// Build a heavy bomber ship — wide, flat body with twin engine pods.
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildBomberMesh(
        Vector3 color, float size = 2.5f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;

        float[] vertices =
        {
            // Main body - wide flat diamond
             0f,    0f,   s*0.7f,   r, g, b,
            -s*0.6f, 0f, -s*0.4f,   r*0.7f, g*0.7f, b*0.7f,
             s*0.6f, 0f, -s*0.4f,   r*0.7f, g*0.7f, b*0.7f,

            // Left engine pod
            -s*0.5f, 0.1f*s,  s*0.2f,  r*0.5f, g*0.5f, b*0.8f,
            -s*0.8f, 0f,     -s*0.5f,  r*0.3f, g*0.3f, b*0.6f,
            -s*0.3f, 0f,     -s*0.5f,  r*0.4f, g*0.4f, b*0.7f,

            // Right engine pod
             s*0.5f, 0.1f*s,  s*0.2f,  r*0.5f, g*0.5f, b*0.8f,
             s*0.3f, 0f,     -s*0.5f,  r*0.4f, g*0.4f, b*0.7f,
             s*0.8f, 0f,     -s*0.5f,  r*0.3f, g*0.3f, b*0.6f,

            // Top hull plate
             0f,     0.2f*s,  s*0.4f,  r*1.1f, g*1.1f, b*1.1f,
            -s*0.4f, 0f,      0f,      r*0.8f, g*0.8f, b*0.8f,
             s*0.4f, 0f,      0f,      r*0.8f, g*0.8f, b*0.8f,

            // Tail fin
             0f,     0.3f*s, -s*0.3f,  r*0.6f, g*0.6f, b*0.9f,
            -s*0.1f, 0f,     -s*0.6f,  r*0.4f, g*0.4f, b*0.6f,
             s*0.1f, 0f,     -s*0.6f,  r*0.4f, g*0.4f, b*0.6f,
        };

        for (int i = 0; i < vertices.Length; i += 6)
        {
            vertices[i + 3] = MathHelper.Clamp(vertices[i + 3], 0f, 1f);
            vertices[i + 4] = MathHelper.Clamp(vertices[i + 4], 0f, 1f);
            vertices[i + 5] = MathHelper.Clamp(vertices[i + 5], 0f, 1f);
        }

        return UploadMesh(vertices);
    }

    /// <summary>
    /// Build a destroyer mesh — long, sleek body with forward-swept wings.
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildDestroyerMesh(
        Vector3 color, float size = 3.5f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;

        float[] vertices =
        {
            // Long nose cone
             0f,    0f,    s,        r*1.2f, g*1.2f, b*1.2f,
            -s*0.2f, 0f,   s*0.3f,   r*0.8f, g*0.8f, b*0.8f,
             s*0.2f, 0f,   s*0.3f,   r*0.8f, g*0.8f, b*0.8f,

            // Main hull left
            -s*0.2f, 0f,   s*0.3f,   r*0.7f, g*0.7f, b*0.7f,
            -s*0.3f, 0f,  -s*0.6f,   r*0.5f, g*0.5f, b*0.5f,
             0f,     0.15f*s, 0f,     r*0.9f, g*0.9f, b*0.9f,

            // Main hull right
             s*0.2f, 0f,   s*0.3f,   r*0.7f, g*0.7f, b*0.7f,
             0f,     0.15f*s, 0f,     r*0.9f, g*0.9f, b*0.9f,
             s*0.3f, 0f,  -s*0.6f,   r*0.5f, g*0.5f, b*0.5f,

            // Forward-swept left wing
            -s*0.3f, 0f,  -s*0.2f,   r*0.6f, g*0.6f, b*0.6f,
            -s*0.7f, 0f,   s*0.1f,   r*0.4f, g*0.4f, b*0.4f,
            -s*0.4f, 0f,  -s*0.5f,   r*0.5f, g*0.5f, b*0.5f,

            // Forward-swept right wing
             s*0.3f, 0f,  -s*0.2f,   r*0.6f, g*0.6f, b*0.6f,
             s*0.4f, 0f,  -s*0.5f,   r*0.5f, g*0.5f, b*0.5f,
             s*0.7f, 0f,   s*0.1f,   r*0.4f, g*0.4f, b*0.4f,

            // Engine block
            -s*0.15f, 0.05f*s, -s*0.6f, r*0.3f, g*0.3f, b*0.5f,
             s*0.15f, 0.05f*s, -s*0.6f, r*0.3f, g*0.3f, b*0.5f,
             0f,      0.1f*s,  -s*0.8f, r*0.2f, g*0.2f, b*0.4f,
        };

        for (int i = 0; i < vertices.Length; i += 6)
        {
            vertices[i + 3] = MathHelper.Clamp(vertices[i + 3], 0f, 1f);
            vertices[i + 4] = MathHelper.Clamp(vertices[i + 4], 0f, 1f);
            vertices[i + 5] = MathHelper.Clamp(vertices[i + 5], 0f, 1f);
        }

        return UploadMesh(vertices);
    }

    /// <summary>
    /// Build a carrier mesh — large, boxy with hangar bay opening.
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildCarrierMesh(
        Vector3 color, float size = 4f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float s = size;

        float[] vertices =
        {
            // Front bow - angular
             0f,    0.1f*s,  s*0.8f,  r, g, b,
            -s*0.4f, 0f,     s*0.3f,  r*0.7f, g*0.7f, b*0.7f,
             s*0.4f, 0f,     s*0.3f,  r*0.7f, g*0.7f, b*0.7f,

            // Port hull
            -s*0.4f, 0f,     s*0.3f,  r*0.6f, g*0.6f, b*0.6f,
            -s*0.5f, 0f,    -s*0.7f,  r*0.4f, g*0.4f, b*0.4f,
            -s*0.4f, 0.2f*s,-s*0.3f,  r*0.5f, g*0.5f, b*0.5f,

            // Starboard hull
             s*0.4f, 0f,     s*0.3f,  r*0.6f, g*0.6f, b*0.6f,
             s*0.4f, 0.2f*s,-s*0.3f,  r*0.5f, g*0.5f, b*0.5f,
             s*0.5f, 0f,    -s*0.7f,  r*0.4f, g*0.4f, b*0.4f,

            // Top deck
            -s*0.35f, 0.2f*s, s*0.2f,  r*0.8f, g*0.8f, b*0.8f,
             s*0.35f, 0.2f*s, s*0.2f,  r*0.8f, g*0.8f, b*0.8f,
             0f,      0.25f*s,-s*0.4f,  r*0.9f, g*0.9f, b*0.9f,

            // Command tower
             s*0.2f,  0.25f*s, 0f,      r*0.9f, g*0.9f, b*0.9f,
             s*0.3f,  0.25f*s,-s*0.2f,  r*0.7f, g*0.7f, b*0.7f,
             s*0.25f, 0.4f*s, -s*0.1f,  r*1.0f, g*1.0f, b*1.0f,

            // Hangar opening (dark inset)
            -s*0.2f, 0.02f*s, -s*0.4f, r*0.2f, g*0.2f, b*0.2f,
             s*0.2f, 0.02f*s, -s*0.4f, r*0.2f, g*0.2f, b*0.2f,
             0f,     0.02f*s, -s*0.7f, r*0.15f, g*0.15f, b*0.15f,

            // Stern
            -s*0.4f, 0.1f*s, -s*0.7f, r*0.35f, g*0.35f, b*0.35f,
             s*0.4f, 0.1f*s, -s*0.7f, r*0.35f, g*0.35f, b*0.35f,
             0f,     0.15f*s,-s*0.85f, r*0.3f, g*0.3f, b*0.3f,
        };

        for (int i = 0; i < vertices.Length; i += 6)
        {
            vertices[i + 3] = MathHelper.Clamp(vertices[i + 3], 0f, 1f);
            vertices[i + 4] = MathHelper.Clamp(vertices[i + 4], 0f, 1f);
            vertices[i + 5] = MathHelper.Clamp(vertices[i + 5], 0f, 1f);
        }

        return UploadMesh(vertices);
    }

    /// <summary>
    /// Build an engine trail mesh (elongated triangle that pulses behind ships).
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildEngineTrail(
        Vector3 color, float length = 2f)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float l = length;

        float[] vertices =
        {
            // Bright core
             0f,     0f,    0f,     r, g, b,
            -l*0.15f, 0f,  -l,     r*0.2f, g*0.2f, b*0.1f,
             l*0.15f, 0f,  -l,     r*0.2f, g*0.2f, b*0.1f,

            // Outer glow left
             0f,     0f,    0f,     r*0.6f, g*0.6f, b*0.6f,
            -l*0.3f, 0f,   -l*0.7f, r*0.1f, g*0.1f, b*0.05f,
            -l*0.1f, 0f,   -l*0.8f, r*0.1f, g*0.1f, b*0.05f,

            // Outer glow right
             0f,     0f,    0f,     r*0.6f, g*0.6f, b*0.6f,
             l*0.1f, 0f,   -l*0.8f, r*0.1f, g*0.1f, b*0.05f,
             l*0.3f, 0f,   -l*0.7f, r*0.1f, g*0.1f, b*0.05f,
        };

        for (int i = 0; i < vertices.Length; i += 6)
        {
            vertices[i + 3] = MathHelper.Clamp(vertices[i + 3], 0f, 1f);
            vertices[i + 4] = MathHelper.Clamp(vertices[i + 4], 0f, 1f);
            vertices[i + 5] = MathHelper.Clamp(vertices[i + 5], 0f, 1f);
        }

        return UploadMesh(vertices);
    }

    private static (int vao, int vbo, int vertexCount) UploadMesh(
        float[] data, PrimitiveType _ = PrimitiveType.Triangles)
    {
        int stride = 6;
        int vertexCount = data.Length / stride;

        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float),
            data, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
            stride * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false,
            stride * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
        return (vao, vbo, vertexCount);
    }
}
