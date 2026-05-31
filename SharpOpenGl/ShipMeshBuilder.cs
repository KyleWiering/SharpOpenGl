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
