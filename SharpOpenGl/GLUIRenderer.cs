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
        float charWidth = UIFontMetrics.GetCharWidth(fontSize);
        float glyphHeight = UIFontMetrics.GetGlyphHeight(fontSize);
        float padTop = UIFontMetrics.GetGlyphPadTop(fontSize);
        float spacing = UIFontMetrics.GetCharSpacing(fontSize);
        float lineThickness = UIFontMetrics.GetLineThickness(fontSize);
        var shadow = new Vector4(0f, 0f, 0f, color.W * 0.45f);

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
            float y = position.Y + padTop;
            charIndex++;

            DrawGlyph(c, x + 1f, y + 1f, charWidth, glyphHeight, lineThickness, shadow);
            DrawGlyph(c, x, y, charWidth, glyphHeight, lineThickness, color);
        }
    }

    private void DrawGlyph(char c, float x, float y, float w, float h, float t, Vector4 color)
    {
        float halfH = h * 0.5f;
        float diagStep = MathF.Max(t, h * 0.22f);

        foreach (var seg in UIFontGlyphSegments.GetSegments(c))
        {
            switch (seg)
            {
                case UIFontGlyphSegments.Segment.Top:
                    DrawRect(new Vector2(x, y), new Vector2(w, t), color);
                    break;
                case UIFontGlyphSegments.Segment.Bottom:
                    DrawRect(new Vector2(x, y + h - t), new Vector2(w, t), color);
                    break;
                case UIFontGlyphSegments.Segment.Middle:
                    DrawRect(new Vector2(x, y + halfH - t * 0.5f), new Vector2(w, t), color);
                    break;
                case UIFontGlyphSegments.Segment.MiddleRight:
                    DrawRect(new Vector2(x + w * 0.5f, y + halfH - t * 0.5f), new Vector2(w * 0.5f + t, t), color);
                    break;
                case UIFontGlyphSegments.Segment.MiddleLeft:
                    DrawRect(new Vector2(x, y + halfH - t * 0.5f), new Vector2(w * 0.5f + t, t), color);
                    break;
                case UIFontGlyphSegments.Segment.TopCenterCap:
                    DrawRect(new Vector2(x + w * 0.28f, y), new Vector2(w * 0.44f, t), color);
                    break;
                case UIFontGlyphSegments.Segment.BottomCenterCap:
                    DrawRect(new Vector2(x + w * 0.28f, y + h - t), new Vector2(w * 0.44f, t), color);
                    break;
                case UIFontGlyphSegments.Segment.TopLeft:
                    DrawRect(new Vector2(x, y), new Vector2(t, halfH), color);
                    break;
                case UIFontGlyphSegments.Segment.TopRight:
                    DrawRect(new Vector2(x + w - t, y), new Vector2(t, halfH), color);
                    break;
                case UIFontGlyphSegments.Segment.BottomLeft:
                    DrawRect(new Vector2(x, y + halfH), new Vector2(t, halfH), color);
                    break;
                case UIFontGlyphSegments.Segment.BottomRight:
                    DrawRect(new Vector2(x + w - t, y + halfH), new Vector2(t, halfH), color);
                    break;
                case UIFontGlyphSegments.Segment.LeftFull:
                    DrawRect(new Vector2(x, y), new Vector2(t, h), color);
                    break;
                case UIFontGlyphSegments.Segment.RightFull:
                    DrawRect(new Vector2(x + w - t, y), new Vector2(t, h), color);
                    break;
                case UIFontGlyphSegments.Segment.CenterVert:
                    DrawRect(new Vector2(x + w * 0.5f - t * 0.5f, y), new Vector2(t, h), color);
                    break;
                case UIFontGlyphSegments.Segment.BottomCenterStem:
                    DrawRect(new Vector2(x + w * 0.5f - t * 0.5f, y + halfH), new Vector2(t, halfH), color);
                    break;
                case UIFontGlyphSegments.Segment.TopHalfRight:
                    DrawRect(new Vector2(x + w * 0.5f, y), new Vector2(w * 0.5f, t), color);
                    break;
                case UIFontGlyphSegments.Segment.BottomHalfLeft:
                    DrawRect(new Vector2(x, y + h - t), new Vector2(w * 0.5f, t), color);
                    break;
                case UIFontGlyphSegments.Segment.Dot:
                    DrawRect(new Vector2(x + w * 0.4f, y + h - t * 2f), new Vector2(t * 1.5f, t * 1.5f), color);
                    break;
                case UIFontGlyphSegments.Segment.DotLow:
                    DrawRect(new Vector2(x + w * 0.35f, y + h - t * 1.2f), new Vector2(t * 1.5f, t * 1.5f), color);
                    break;
                case UIFontGlyphSegments.Segment.TopTick:
                    DrawRect(new Vector2(x + w * 0.55f, y), new Vector2(t, t * 1.8f), color);
                    break;
                case UIFontGlyphSegments.Segment.TopPeakLeft:
                {
                    float peakStep = MathF.Max(t * 1.5f, h * 0.18f);
                    DrawRect(new Vector2(x + t, y), new Vector2(t, peakStep), color);
                    DrawRect(new Vector2(x + w * 0.22f, y), new Vector2(t, peakStep * 1.55f), color);
                    DrawRect(new Vector2(x + w * 0.38f, y), new Vector2(t, peakStep * 2.1f), color);
                    break;
                }
                case UIFontGlyphSegments.Segment.TopPeakRight:
                {
                    float peakStep = MathF.Max(t * 1.5f, h * 0.18f);
                    DrawRect(new Vector2(x + w - t * 2f, y), new Vector2(t, peakStep), color);
                    DrawRect(new Vector2(x + w * 0.62f, y), new Vector2(t, peakStep * 1.55f), color);
                    DrawRect(new Vector2(x + w * 0.46f, y), new Vector2(t, peakStep * 2.1f), color);
                    break;
                }
                case UIFontGlyphSegments.Segment.BottomValleyLeft:
                {
                    float valleyStep = MathF.Max(t * 1.5f, h * 0.18f);
                    DrawRect(new Vector2(x + t, y + h - valleyStep), new Vector2(t, valleyStep), color);
                    DrawRect(new Vector2(x + w * 0.22f, y + h - valleyStep * 1.55f), new Vector2(t, valleyStep * 1.55f), color);
                    DrawRect(new Vector2(x + w * 0.38f, y + h - valleyStep * 2.1f), new Vector2(t, valleyStep * 2.1f), color);
                    break;
                }
                case UIFontGlyphSegments.Segment.BottomValleyRight:
                {
                    float valleyStep = MathF.Max(t * 1.5f, h * 0.18f);
                    DrawRect(new Vector2(x + w - t * 2f, y + h - valleyStep), new Vector2(t, valleyStep), color);
                    DrawRect(new Vector2(x + w * 0.62f, y + h - valleyStep * 1.55f), new Vector2(t, valleyStep * 1.55f), color);
                    DrawRect(new Vector2(x + w * 0.46f, y + h - valleyStep * 2.1f), new Vector2(t, valleyStep * 2.1f), color);
                    break;
                }
                case UIFontGlyphSegments.Segment.DiagTopRightToBottomLeft:
                    DrawRect(new Vector2(x + w * 0.66f, y + h * 0.08f), new Vector2(t, diagStep), color);
                    DrawRect(new Vector2(x + w * 0.33f, y + h * 0.33f), new Vector2(t, diagStep), color);
                    DrawRect(new Vector2(x, y + h * 0.58f), new Vector2(t, diagStep), color);
                    break;
                case UIFontGlyphSegments.Segment.DiagTopLeftToBottomRight:
                    DrawRect(new Vector2(x, y + h * 0.08f), new Vector2(t, diagStep), color);
                    DrawRect(new Vector2(x + w * 0.33f, y + h * 0.33f), new Vector2(t, diagStep), color);
                    DrawRect(new Vector2(x + w * 0.66f, y + h * 0.58f), new Vector2(t, diagStep), color);
                    break;
                case UIFontGlyphSegments.Segment.DiagMidLeftTopRight:
                    DrawRect(new Vector2(x + w * 0.42f, y + halfH - t), new Vector2(t, halfH * 0.52f), color);
                    DrawRect(new Vector2(x + w * 0.68f, y + t), new Vector2(t, halfH * 0.48f), color);
                    DrawRect(new Vector2(x + w - t, y), new Vector2(t, t * 1.6f), color);
                    break;
                case UIFontGlyphSegments.Segment.DiagMidLeftBottomRight:
                    DrawRect(new Vector2(x + w * 0.42f, y + halfH), new Vector2(t, halfH * 0.52f), color);
                    DrawRect(new Vector2(x + w * 0.68f, y + h - halfH * 0.48f - t), new Vector2(t, halfH * 0.48f), color);
                    DrawRect(new Vector2(x + w - t, y + h - t * 1.6f), new Vector2(t, t * 1.6f), color);
                    break;
                case UIFontGlyphSegments.Segment.DiagMidRightBottomRight:
                    DrawRect(new Vector2(x + w * 0.55f, y + halfH - t * 0.5f), new Vector2(t, halfH * 0.45f), color);
                    DrawRect(new Vector2(x + w * 0.75f, y + h * 0.62f), new Vector2(t, h * 0.22f), color);
                    DrawRect(new Vector2(x + w - t, y + h - t), new Vector2(t, t), color);
                    break;
                case UIFontGlyphSegments.Segment.TopHalfLeft:
                {
                    float shelfStep = MathF.Max(t * 1.2f, h * 0.12f);
                    DrawRect(new Vector2(x, y), new Vector2(w * 0.34f, t), color);
                    DrawRect(new Vector2(x + w * 0.28f, y), new Vector2(t, shelfStep), color);
                    break;
                }
                case UIFontGlyphSegments.Segment.BottomHalfRight:
                    DrawRect(new Vector2(x + w * 0.5f, y + h - t), new Vector2(w * 0.5f, t), color);
                    break;
            }
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