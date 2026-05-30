using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// A wireframe cube rendered as a placeholder when no mesh or asset is available.
/// Create one instance per "missing asset" slot and call Render each frame.
/// </summary>
public sealed class DefaultObject : IDisposable
{
    private int _vao;
    private int _vbo;
    private int _vertexCount;
    private bool _initialized;

    /// <summary>Color of the wireframe (defaults to cyan).</summary>
    public Vector3 Color { get; set; } = new Vector3(0f, 1f, 1f);

    /// <summary>Initialize GPU resources. Must be called with a valid GL context.</summary>
    public void Initialize()
    {
        (_vao, _vbo, _vertexCount) = MeshBuilder.BuildWireframeCube(Color);
        _initialized = true;
    }

    /// <summary>
    /// Draw the default object. The shader must expose the same uniforms as the
    /// engine's standard shader (model matrix + overrideColor).
    /// </summary>
    /// <param name="modelUniform">Uniform location for the model matrix.</param>
    /// <param name="colorUniform">Uniform location for the override color.</param>
    /// <param name="transform">World-space transform for this object.</param>
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
