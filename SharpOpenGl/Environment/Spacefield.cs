using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl.Environment;

/// <summary>
/// Renders a starfield of random colored points.
/// Ported from original Spacefield.cs using modern OpenGL (VAO/VBO).
/// </summary>
public class Spacefield : IEnvironment
{
    private int _vao;
    private int _vbo;
    private int _vertexCount;
    private float _rotation;

    public void Initialize()
    {
        var random = new Random(42); // Fixed seed for reproducible screenshots
        int starCount = 1000;
        _vertexCount = starCount;

        // Each vertex: 3 floats position + 3 floats color
        float[] vertices = new float[starCount * 6];

        for (int i = 0; i < starCount; i++)
        {
            int offset = i * 6;
            vertices[offset + 0] = (random.Next(1000) - 500); // x
            vertices[offset + 1] = (random.Next(1000) - 500); // y
            vertices[offset + 2] = (random.Next(1000) - 500); // z

            float r = (random.Next(5) / 5.0f) + 0.5f;
            float g = (random.Next(5) / 5.0f) + 0.5f;
            float b = (random.Next(5) / 5.0f) + 0.5f;
            vertices[offset + 3] = r;
            vertices[offset + 4] = g;
            vertices[offset + 5] = b;
        }

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
        var model = Matrix4.CreateRotationY(_rotation) * Matrix4.CreateRotationX(_rotation * 0.35f);
        GL.UniformMatrix4(modelUniform, false, ref model);

        // No override color (use per-vertex colors)
        GL.Uniform4(colorUniform, new Vector4(0, 0, 0, 0));

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Points, 0, _vertexCount);
        GL.BindVertexArray(0);
    }

    public void Update(double elapsedTime)
    {
        _rotation += (float)elapsedTime * 0.04f;
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }
}
