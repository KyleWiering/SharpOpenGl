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

    public void End()
    {
        GL.Enable(EnableCap.DepthTest);
    }

    public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
    {
        GL.Uniform4(_uniformColor, color);

        int locTransform = GL.GetUniformLocation(_program, "transform");
        var transform = Matrix4.CreateScale(size.X, size.Y, 1f) *
                        Matrix4.CreateTranslation(position.X, position.Y, 0f);
        GL.UniformMatrix4(locTransform, false, ref transform);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color)
    {
        float thickness = MathF.Max(1.5f, size.Y * 0.01f);
        DrawRect(position, new Vector2(size.X, thickness), color);
        DrawRect(new Vector2(position.X, position.Y + size.Y - thickness),
            new Vector2(size.X, thickness), color);
        DrawRect(position, new Vector2(thickness, size.Y), color);
        DrawRect(new Vector2(position.X + size.X - thickness, position.Y),
            new Vector2(thickness, size.Y), color);
    }

    public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
    {
        if (string.IsNullOrEmpty(text)) return;

        string[] lines = text.Split('\n');
        float lineHeight = fontSize * UITextDrawing.LineHeightFactor;
        for (int line = 0; line < lines.Length; line++)
            DrawTextLine(lines[line], position + new Vector2(0f, line * lineHeight), fontSize, color);
    }

    private void DrawTextLine(string text, Vector2 position, float fontSize, Vector4 color)
    {
        float charWidth = fontSize * 0.62f;
        float charHeight = fontSize;
        float spacing = charWidth * 0.15f;
        float lineThickness = MathF.Max(3.5f, fontSize * 0.26f);
        var outline = new Vector4(0f, 0f, 0f, color.W * 0.85f);

        int charIndex = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                charIndex++;
                continue;
            }

            float x = position.X + charIndex * (charWidth + spacing);
            float y = position.Y;
            charIndex++;

            DrawGlyph(c, x - 1f, y, charWidth, charHeight, lineThickness, outline);
            DrawGlyph(c, x + 1f, y, charWidth, charHeight, lineThickness, outline);
            DrawGlyph(c, x, y - 1f, charWidth, charHeight, lineThickness, outline);
            DrawGlyph(c, x, y + 1f, charWidth, charHeight, lineThickness, outline);
            DrawGlyph(c, x, y, charWidth, charHeight, lineThickness, color);
        }
    }

    private void DrawGlyph(char c, float x, float y, float w, float h, float t, Vector4 color)
    {
        float halfH = h * 0.5f;

        var segments = GetCharSegments(char.ToUpper(c));
        foreach (var seg in segments)
        {
            switch (seg)
            {
                case Seg.Top:
                    DrawRect(new Vector2(x, y), new Vector2(w, t), color);
                    break;
                case Seg.Bottom:
                    DrawRect(new Vector2(x, y + h - t), new Vector2(w, t), color);
                    break;
                case Seg.Middle:
                    DrawRect(new Vector2(x, y + halfH - t * 0.5f), new Vector2(w, t), color);
                    break;
                case Seg.TopLeft:
                    DrawRect(new Vector2(x, y), new Vector2(t, halfH), color);
                    break;
                case Seg.TopRight:
                    DrawRect(new Vector2(x + w - t, y), new Vector2(t, halfH), color);
                    break;
                case Seg.BottomLeft:
                    DrawRect(new Vector2(x, y + halfH), new Vector2(t, halfH), color);
                    break;
                case Seg.BottomRight:
                    DrawRect(new Vector2(x + w - t, y + halfH), new Vector2(t, halfH), color);
                    break;
                case Seg.LeftFull:
                    DrawRect(new Vector2(x, y), new Vector2(t, h), color);
                    break;
                case Seg.RightFull:
                    DrawRect(new Vector2(x + w - t, y), new Vector2(t, h), color);
                    break;
                case Seg.CenterVert:
                    DrawRect(new Vector2(x + w * 0.5f - t * 0.5f, y), new Vector2(t, h), color);
                    break;
                case Seg.TopHalfRight:
                    DrawRect(new Vector2(x + w * 0.5f, y), new Vector2(w * 0.5f, t), color);
                    break;
                case Seg.BottomHalfLeft:
                    DrawRect(new Vector2(x, y + h - t), new Vector2(w * 0.5f, t), color);
                    break;
                case Seg.Dot:
                    DrawRect(new Vector2(x + w * 0.4f, y + h - t * 2f), new Vector2(t * 1.5f, t * 1.5f), color);
                    break;
                case Seg.DiagTopRightToBottomLeft:
                    DrawRect(new Vector2(x + w * 0.66f, y + h * 0.1f), new Vector2(t, h * 0.25f), color);
                    DrawRect(new Vector2(x + w * 0.33f, y + h * 0.35f), new Vector2(t, h * 0.25f), color);
                    DrawRect(new Vector2(x, y + h * 0.6f), new Vector2(t, h * 0.25f), color);
                    break;
                case Seg.DiagTopLeftToBottomRight:
                    DrawRect(new Vector2(x, y + h * 0.1f), new Vector2(t, h * 0.25f), color);
                    DrawRect(new Vector2(x + w * 0.33f, y + h * 0.35f), new Vector2(t, h * 0.25f), color);
                    DrawRect(new Vector2(x + w * 0.66f, y + h * 0.6f), new Vector2(t, h * 0.25f), color);
                    break;
                case Seg.TopHalfLeft:
                    DrawRect(new Vector2(x, y), new Vector2(w * 0.5f, t), color);
                    break;
                case Seg.BottomHalfRight:
                    DrawRect(new Vector2(x + w * 0.5f, y + h - t), new Vector2(w * 0.5f, t), color);
                    break;
            }
        }
    }

    private enum Seg
    {
        Top, Bottom, Middle,
        TopLeft, TopRight, BottomLeft, BottomRight,
        LeftFull, RightFull, CenterVert,
        TopHalfRight, BottomHalfLeft, Dot,
        DiagTopRightToBottomLeft, DiagTopLeftToBottomRight,
        TopHalfLeft, BottomHalfRight
    }

    private static Seg[] GetCharSegments(char c) => c switch
    {
        'A' => [Seg.Top, Seg.TopLeft, Seg.TopRight, Seg.Middle, Seg.BottomLeft, Seg.BottomRight],
        'B' => [Seg.Top, Seg.Bottom, Seg.Middle, Seg.LeftFull, Seg.TopRight, Seg.BottomRight],
        'C' => [Seg.Top, Seg.Bottom, Seg.TopLeft, Seg.BottomLeft],
        'D' => [Seg.Top, Seg.Bottom, Seg.TopRight, Seg.BottomRight, Seg.LeftFull],
        'E' => [Seg.Top, Seg.Bottom, Seg.Middle, Seg.TopLeft, Seg.BottomLeft],
        'F' => [Seg.Top, Seg.Middle, Seg.TopLeft, Seg.BottomLeft],
        'G' => [Seg.Top, Seg.Bottom, Seg.TopLeft, Seg.BottomLeft, Seg.BottomRight, Seg.Middle],
        'H' => [Seg.TopLeft, Seg.TopRight, Seg.Middle, Seg.BottomLeft, Seg.BottomRight],
        'I' => [Seg.Top, Seg.Bottom, Seg.CenterVert],
        'J' => [Seg.Top, Seg.TopRight, Seg.BottomRight, Seg.Bottom, Seg.BottomLeft],
        'K' => [Seg.LeftFull, Seg.Middle, Seg.TopRight, Seg.BottomRight],
        'L' => [Seg.TopLeft, Seg.BottomLeft, Seg.Bottom],
        'M' => [Seg.Top, Seg.LeftFull, Seg.RightFull, Seg.CenterVert],
        'N' => [Seg.Top, Seg.Bottom, Seg.LeftFull, Seg.RightFull],
        'O' => [Seg.Top, Seg.Bottom, Seg.TopLeft, Seg.TopRight, Seg.BottomLeft, Seg.BottomRight],
        'P' => [Seg.Top, Seg.Middle, Seg.TopLeft, Seg.TopRight, Seg.BottomLeft],
        'Q' => [Seg.Top, Seg.Bottom, Seg.TopLeft, Seg.TopRight, Seg.BottomLeft, Seg.BottomRight],
        'R' => [Seg.Top, Seg.Middle, Seg.TopLeft, Seg.TopRight, Seg.BottomLeft, Seg.BottomRight],
        'S' => [Seg.Top, Seg.Bottom, Seg.Middle, Seg.TopLeft, Seg.BottomRight],
        'T' => [Seg.Top, Seg.CenterVert],
        'U' => [Seg.Bottom, Seg.TopLeft, Seg.TopRight, Seg.BottomLeft, Seg.BottomRight],
        'V' => [Seg.TopLeft, Seg.TopRight, Seg.BottomLeft, Seg.BottomRight, Seg.Bottom],
        'W' => [Seg.Bottom, Seg.LeftFull, Seg.RightFull, Seg.CenterVert],
        'X' => [Seg.TopLeft, Seg.TopRight, Seg.Middle, Seg.BottomLeft, Seg.BottomRight],
        'Y' => [Seg.TopLeft, Seg.TopRight, Seg.Middle, Seg.CenterVert],
        'Z' => [Seg.Top, Seg.Bottom, Seg.Middle, Seg.TopRight, Seg.BottomLeft],
        '0' => [Seg.Top, Seg.Bottom, Seg.TopLeft, Seg.TopRight, Seg.BottomLeft, Seg.BottomRight],
        '1' => [Seg.TopRight, Seg.BottomRight],
        '2' => [Seg.Top, Seg.TopRight, Seg.Middle, Seg.BottomLeft, Seg.Bottom],
        '3' => [Seg.Top, Seg.Middle, Seg.Bottom, Seg.TopRight, Seg.BottomRight],
        '4' => [Seg.TopLeft, Seg.Middle, Seg.TopRight, Seg.BottomRight],
        '5' => [Seg.Top, Seg.TopLeft, Seg.Middle, Seg.BottomRight, Seg.Bottom],
        '6' => [Seg.Top, Seg.TopLeft, Seg.Middle, Seg.BottomLeft, Seg.BottomRight, Seg.Bottom],
        '7' => [Seg.Top, Seg.TopRight, Seg.BottomRight],
        '8' => [Seg.Top, Seg.Bottom, Seg.Middle, Seg.TopLeft, Seg.TopRight, Seg.BottomLeft, Seg.BottomRight],
        '9' => [Seg.Top, Seg.Bottom, Seg.Middle, Seg.TopLeft, Seg.TopRight, Seg.BottomRight],
        '.' => [Seg.Dot],
        ':' => [Seg.Top, Seg.Bottom],
        '-' => [Seg.Middle],
        '+' => [Seg.Middle, Seg.CenterVert],
        '/' => [Seg.TopRight, Seg.BottomLeft],
        '(' => [Seg.Top, Seg.TopLeft, Seg.BottomLeft, Seg.Bottom],
        ')' => [Seg.Top, Seg.TopRight, Seg.BottomRight, Seg.Bottom],
        '=' => [Seg.Middle, Seg.Bottom],
        '_' => [Seg.Bottom],
        '>' => [Seg.TopLeft, Seg.Middle, Seg.BottomLeft],
        '<' => [Seg.TopRight, Seg.Middle, Seg.BottomRight],
        '#' => [Seg.Middle, Seg.CenterVert, Seg.Top, Seg.Bottom],
        '•' => [Seg.Dot],
        '·' => [Seg.Dot],
        _ => [Seg.Middle],
    };

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