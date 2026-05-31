using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl;

/// <summary>
/// OpenGL immediate-mode implementation of <see cref="IUIRenderer"/>.
/// Draws 2D quads and text using a simple orthographic shader.
/// </summary>
public sealed class GLUIRenderer : IUIRenderer, IDisposable
{
    private int _program;
    private int _uniformProjection;
    private int _uniformColor;
    private int _vao;
    private int _vbo;

    public Vector2 ViewportSize { get; private set; }

    public void Initialize(int viewportWidth, int viewportHeight)
    {
        ViewportSize = new Vector2(viewportWidth, viewportHeight);

        _program = CreateUIShaderProgram();
        _uniformProjection = GL.GetUniformLocation(_program, "projection");
        _uniformColor = GL.GetUniformLocation(_program, "color");

        // A single quad (two triangles) we reuse for all rects
        float[] quadVerts =
        {
            0f, 0f,
            1f, 0f,
            1f, 1f,
            0f, 0f,
            1f, 1f,
            0f, 1f,
        };

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVerts.Length * sizeof(float),
            quadVerts, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    public void UpdateViewport(int width, int height)
    {
        ViewportSize = new Vector2(width, height);
    }

    /// <summary>Begin a UI rendering pass (sets up orthographic state).</summary>
    public void Begin()
    {
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.UseProgram(_program);

        var projection = Matrix4.CreateOrthographicOffCenter(
            0f, ViewportSize.X, ViewportSize.Y, 0f, -1f, 1f);
        GL.UniformMatrix4(_uniformProjection, false, ref projection);
    }

    /// <summary>End the UI rendering pass (restores 3D state).</summary>
    public void End()
    {
        GL.Enable(EnableCap.DepthTest);
    }

    public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
    {
        GL.Uniform4(_uniformColor, color);

        // Transform: scale to size, then translate to position
        int locTransform = GL.GetUniformLocation(_program, "transform");
        var transform = Matrix4.CreateScale(size.X, size.Y, 1f) *
                        Matrix4.CreateTranslation(position.X, position.Y, 0f);
        GL.UniformMatrix4(locTransform, false, ref transform);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color)
    {
        float thickness = 1f;
        // Top
        DrawRect(position, new Vector2(size.X, thickness), color);
        // Bottom
        DrawRect(new Vector2(position.X, position.Y + size.Y - thickness),
            new Vector2(size.X, thickness), color);
        // Left
        DrawRect(position, new Vector2(thickness, size.Y), color);
        // Right
        DrawRect(new Vector2(position.X + size.X - thickness, position.Y),
            new Vector2(thickness, size.Y), color);
    }

    public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
    {
        // Simple block-character text rendering using quads
        float charWidth = fontSize * 0.6f;
        float charHeight = fontSize;
        float spacing = charWidth * 0.1f;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ')
                continue;

            float x = position.X + i * (charWidth + spacing);
            float y = position.Y;

            // Draw a small filled rect for each character
            DrawRect(new Vector2(x + charWidth * 0.1f, y + charHeight * 0.1f),
                new Vector2(charWidth * 0.8f, charHeight * 0.8f), color);
        }
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }

    private static int CreateUIShaderProgram()
    {
        const string vertSrc = @"
#version 330 core
layout(location = 0) in vec2 aPos;

uniform mat4 projection;
uniform mat4 transform;

void main()
{
    gl_Position = projection * transform * vec4(aPos, 0.0, 1.0);
}
";
        const string fragSrc = @"
#version 330 core
uniform vec4 color;
out vec4 FragColor;

void main()
{
    FragColor = color;
}
";
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vertSrc);
        GL.CompileShader(vs);

        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, fragSrc);
        GL.CompileShader(fs);

        int program = GL.CreateProgram();
        GL.AttachShader(program, vs);
        GL.AttachShader(program, fs);
        GL.LinkProgram(program);

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        return program;
    }
}
