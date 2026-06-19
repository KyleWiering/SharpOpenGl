using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl.Rendering;

/// <summary>
/// Builds common procedural meshes (VAO + VBO) for engine and game use.
/// Each builder method returns (vao, vbo, vertexCount).
/// Caller is responsible for disposing the returned handles via <see cref="DeleteMesh"/>.
/// </summary>
public static class MeshBuilder
{
    /// <summary>
    /// Build a unit wireframe cube centered at the origin.
    /// Vertex layout: vec3 position + vec3 color (stride = 6 floats).
    /// </summary>
    public static (int vao, int vbo, int vertexCount) BuildWireframeCube(Vector3 color)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float[] v = BuildWireframeCubeVertices(r, g, b);
        return Upload(v, 6, PrimitiveType.Lines);
    }

    /// <summary>Build a quad in the XY plane (two triangles), with given color.</summary>
    public static (int vao, int vbo, int vertexCount) BuildQuad(
        float width, float height, Vector3 color)
    {
        float hw = width * 0.5f, hh = height * 0.5f;
        float r = color.X, g = color.Y, b = color.Z;
        float[] v =
        {
            -hw, -hh, 0, r, g, b,
             hw, -hh, 0, r, g, b,
             hw,  hh, 0, r, g, b,

            -hw, -hh, 0, r, g, b,
             hw,  hh, 0, r, g, b,
            -hw,  hh, 0, r, g, b,
        };
        return Upload(v, 6, PrimitiveType.Triangles);
    }

    /// <summary>Build a quad in the XZ plane (two triangles), with given color.</summary>
    public static (int vao, int vbo, int vertexCount) BuildGroundQuad(
        float width, float depth, Vector3 color)
    {
        float hw = width * 0.5f, hd = depth * 0.5f;
        float r = color.X, g = color.Y, b = color.Z;
        float[] v =
        {
            -hw, 0, -hd, r, g, b,
             hw, 0, -hd, r, g, b,
             hw, 0,  hd, r, g, b,

            -hw, 0, -hd, r, g, b,
             hw, 0,  hd, r, g, b,
            -hw, 0,  hd, r, g, b,
        };
        return Upload(v, 6, PrimitiveType.Triangles);
    }

    /// <summary>Build a flat grid of lines in the XZ plane.</summary>
    public static (int vao, int vbo, int vertexCount) BuildGrid(
        int columns, int rows, float cellSize, Vector3 color)
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

        return Upload(verts.ToArray(), 6, PrimitiveType.Lines);
    }

    /// <summary>Build a point-cloud mesh from pre-built vertex data (pos+col, stride 6).</summary>
    public static (int vao, int vbo, int vertexCount) BuildPointCloud(float[] posColorData)
        => Upload(posColorData, 6, PrimitiveType.Points);

    /// <summary>Delete a previously built mesh's GPU resources.</summary>
    public static void DeleteMesh(int vao, int vbo)
    {
        GL.DeleteBuffer(vbo);
        GL.DeleteVertexArray(vao);
    }

    private static (int vao, int vbo, int vertexCount) Upload(
        float[] data, int stride, PrimitiveType _)
    {
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

    private static float[] BuildWireframeCubeVertices(float r, float g, float b)
    {
        float[] corners = {
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
        };

        int[] edges = {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        float[] result = new float[edges.Length * 6];
        for (int i = 0; i < edges.Length; i++)
        {
            int c = edges[i] * 3;
            result[i * 6 + 0] = corners[c];
            result[i * 6 + 1] = corners[c + 1];
            result[i * 6 + 2] = corners[c + 2];
            result[i * 6 + 3] = r;
            result[i * 6 + 4] = g;
            result[i * 6 + 5] = b;
        }
        return result;
    }
}