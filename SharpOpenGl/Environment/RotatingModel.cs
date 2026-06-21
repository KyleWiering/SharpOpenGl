using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl.Environment;

/// <summary>
/// A rotating geometric model (triangle/lines).
/// Ported from original Model.cs using modern OpenGL (VAO/VBO).
/// </summary>
public class RotatingModel : IEnvironment
{
    private int _vao;
    private int _vbo;
    private int _vertexCount;
    private float _rotation;

    public void Initialize()
    {
        _rotation = 0.0f;

        // A colorful triangle + some lines to show geometry
        float[] vertices =
        {
            // Triangle vertices (position x,y,z + color r,g,b)
            -1.0f, -1.0f, 0.0f,   1.0f, 0.0f, 0.0f,  // Red
             1.0f, -1.0f, 0.0f,   0.0f, 1.0f, 0.0f,  // Green
             0.0f,  1.0f, 0.0f,   0.0f, 0.0f, 1.0f,  // Blue

            // Second triangle for a diamond shape
             0.0f,  1.0f, 0.0f,   0.0f, 0.0f, 1.0f,  // Blue
             1.0f, -1.0f, 0.0f,   0.0f, 1.0f, 0.0f,  // Green
             2.0f,  1.0f, 0.0f,   1.0f, 1.0f, 0.0f,  // Yellow

            // Third triangle
            -2.0f,  1.0f, 0.0f,   1.0f, 0.0f, 1.0f,  // Magenta
            -1.0f, -1.0f, 0.0f,   1.0f, 0.0f, 0.0f,  // Red
             0.0f,  1.0f, 0.0f,   0.0f, 0.0f, 1.0f,  // Blue
        };

        _vertexCount = vertices.Length / 6;

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // Position attribute (location = 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Color attribute (location = 1)
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);

    }

    public void Render(int shaderProgram, int modelUniform, int colorUniform)
    {
        var model = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(_rotation));
        GL.UniformMatrix4(modelUniform, false, ref model);

        // No override color (use per-vertex colors)
        GL.Uniform4(colorUniform, new Vector4(0, 0, 0, 0));

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
        GL.BindVertexArray(0);
    }

    public void Update(double elapsedTime)
    {
        _rotation += (float)(elapsedTime * 45.0); // 45 degrees per second
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }
}
