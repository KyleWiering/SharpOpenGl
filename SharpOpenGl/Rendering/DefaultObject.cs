using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl.Rendering;

/// <summary>
/// A wireframe cube rendered as a placeholder when no mesh or asset is available.
/// </summary>
public sealed class DefaultObject : IDisposable
{
    private int _vao;
    private int _vbo;
    private int _vertexCount;
    private bool _initialized;

    public Vector3 Color { get; set; } = new Vector3(0f, 1f, 1f);

    public void Initialize()
    {
        (_vao, _vbo, _vertexCount) = MeshBuilder.BuildWireframeCube(Color);
        _initialized = true;
    }

    public void Render(int modelUniform, int colorUniform, Matrix4 transform)
    {
        if (!_initialized) return;

        GL.UniformMatrix4(modelUniform, false, ref transform);
        GL.Uniform4(colorUniform, new Vector4(Color.X, Color.Y, Color.Z, 1f));

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _vertexCount);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_initialized)
        {
            MeshBuilder.DeleteMesh(_vao, _vbo);
            _initialized = false;
        }
    }
}