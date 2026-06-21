using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private ExplosionVfxController? _explosionVfx;
    private int _particleVao;
    private int _particleVbo;
    private int _uniformPointSize;

    private void InitializeExplosionVfx()
    {
        _explosionVfx = new ExplosionVfxController();
        _explosionVfx.Bind(_eventBus);

        _uniformPointSize = GL.GetUniformLocation(_shaderProgram, "pointSize");

        _particleVao = GL.GenVertexArray();
        _particleVbo = GL.GenBuffer();
        GL.BindVertexArray(_particleVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVbo);

        const int maxFloats = 4096 * 6;
        GL.BufferData(BufferTarget.ArrayBuffer, maxFloats * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw);

        int stride = 6 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 12);
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    private void UpdateExplosionVfx(float deltaTime) => _explosionVfx?.Update(deltaTime);

    private void RenderExplosionVfx()
    {
        if (_explosionVfx == null) return;

        var (pointCount, vertices) = _explosionVfx.BuildVertexData();
        if (pointCount == 0) return;

        GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);

        var identity = Matrix4.Identity;
        GL.UniformMatrix4(_uniformModel, false, ref identity);
        GL.Uniform4(_uniformColor, Vector4.Zero);
        GL.Uniform1(_uniformPointSize, 5f);

        GL.BindVertexArray(_particleVao);
        GL.DrawArrays(PrimitiveType.Points, 0, pointCount);
        GL.BindVertexArray(0);
    }
}