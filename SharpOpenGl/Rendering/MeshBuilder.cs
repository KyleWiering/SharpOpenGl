using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Rendering;

/// <summary>
/// Builds common procedural meshes (VAO + VBO) for engine and game use.
/// Each builder method returns (vao, vbo, vertexCount).
/// Caller is responsible for disposing the returned handles via <see cref="DeleteMesh"/>.
/// </summary>
public static class MeshBuilder
{
    /// <summary>Build a filled diamond beacon for resource nodes.</summary>
    public static (int vao, int vbo, int vertexCount) BuildResourceNodeMarker(Vector3 color, float size = 2f)
    {
        float[] v = ProceduralMeshes.BuildResourceNodeMarker(color, size);
        return Upload(v, 6, PrimitiveType.Triangles);
    }


    /// <summary>Build a low-poly planet sphere.</summary>
    public static (int vao, int vbo, int vertexCount) BuildPlanetSphere(Vector3 color, float radius = 4f)
    {
        float[] v = ProceduralMeshes.BuildPlanetSphere(color, radius);
        return Upload(v, 6, PrimitiveType.Triangles);
    }

    /// <summary>Build a rocky scenery cluster.</summary>
    public static (int vao, int vbo, int vertexCount) BuildSceneryCluster(Vector3 color, float size = 3f)
    {
        float[] v = ProceduralMeshes.BuildSceneryCluster(color, size);
        return Upload(v, 6, PrimitiveType.Triangles);
    }

    /// <summary>Build a chunky asteroid field rock swarm.</summary>
    public static (int vao, int vbo, int vertexCount) BuildAsteroidFieldCluster(Vector3 color, float size = 3f)
    {
        float[] v = ProceduralMeshes.BuildAsteroidFieldCluster(color, size);
        return Upload(v, 6, PrimitiveType.Triangles);
    }

    /// <summary>Build a soft volumetric-style nebula cloud.</summary>
    public static (int vao, int vbo, int vertexCount) BuildNebulaCloud(float size = 3f)
    {
        float[] v = ProceduralMeshes.BuildNebulaCloud(size);
        return Upload(v, 6, PrimitiveType.Triangles);
    }

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
        int columns, int rows, float cellSize, Vector3 color, int lineStep = 1)
    {
        float[] data = ProceduralMeshes.BuildGrid(columns, rows, cellSize, color, lineStep);
        return Upload(data, 6, PrimitiveType.Lines);
    }

    /// <summary>Build a point-cloud mesh from pre-built vertex data (pos+col, stride 6).</summary>
    public static (int vao, int vbo, int vertexCount) BuildPointCloud(float[] posColorData)
        => Upload(posColorData, 6, PrimitiveType.Points);

    /// <summary>Build vertex data for a world-space line strip on the XZ plane.</summary>
    public static float[] BuildLineStripVertices(
        IReadOnlyList<Vector3> points, Vector3 color, float y = 0.25f) =>
        ProceduralMeshes.BuildLineStripVertices(points, color, y);

    /// <summary>Upload or resize dynamic vertex data for a line strip.</summary>
    public static (int vao, int vbo, int vertexCount) CreateDynamicLineStrip()
    {
        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        const int stride = 6;
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
            stride * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false,
            stride * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
        return (vao, vbo, 0);
    }

    /// <summary>Update a dynamic line-strip buffer created by <see cref="CreateDynamicLineStrip"/>.</summary>
    public static int UpdateDynamicLineStrip(int vbo, float[] vertices)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
            vertices, BufferUsageHint.DynamicDraw);
        return vertices.Length / 6;
    }


    /// <summary>Upload procedural vertex data (pos+col, stride 6).</summary>
    public static (int vao, int vbo, int vertexCount) UploadProcedural(float[] vertices, bool lines = false)
        => Upload(vertices, 6, lines ? PrimitiveType.Lines : PrimitiveType.Triangles);

    /// <summary>Upload parsed OBJ data (pos+normal, stride 6).</summary>
    public static (int vao, int vbo, int vertexCount) UploadObj(ObjMeshData data)
        => Upload(data.Vertices, ObjMeshData.Stride, PrimitiveType.Triangles);

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