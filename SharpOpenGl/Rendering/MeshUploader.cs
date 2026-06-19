using OpenTK.Graphics.OpenGL4;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Rendering;

/// <summary>
/// Uploads parsed mesh data to the GPU (desktop OpenGL only).
/// </summary>
public static class MeshUploader
{
    public static (int vao, int vbo, int vertexCount) Upload(ObjMeshData data)
    {
        int vertexCount = data.VertexCount;
        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer,
            data.Vertices.Length * sizeof(float),
            data.Vertices,
            BufferUsageHint.StaticDraw);

        const int stride = ObjMeshData.Stride * sizeof(float);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false,
            stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
        return (vao, vbo, vertexCount);
    }
}