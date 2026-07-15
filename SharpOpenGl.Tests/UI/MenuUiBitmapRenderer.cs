using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using StbImageWriteSharp;

namespace SharpOpenGl.Tests.UI;

/// <summary>
/// Software IUIRenderer that rasterizes menu screens to a 1024×768 PNG for continuity artifacts.
/// </summary>
internal sealed class MenuUiBitmapRenderer : IUIRenderer
{
    private readonly byte[] _pixels;
    private readonly int _width;
    private readonly int _height;

    public MenuUiBitmapRenderer(Vector2 viewport)
    {
        _width = (int)viewport.X;
        _height = (int)viewport.Y;
        ViewportSize = viewport;
        _pixels = new byte[_width * _height * 4];
        FillBackground();
    }

    public Vector2 ViewportSize { get; }

    public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
    {
        int x0 = (int)MathF.Floor(position.X);
        int y0 = (int)MathF.Floor(position.Y);
        int x1 = (int)MathF.Ceiling(position.X + size.X);
        int y1 = (int)MathF.Ceiling(position.Y + size.Y);

        for (int y = Math.Max(0, y0); y < Math.Min(_height, y1); y++)
        {
            for (int x = Math.Max(0, x0); x < Math.Min(_width, x1); x++)
                BlendPixel(x, y, color);
        }
    }

    public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color)
    {
        DrawRect(position, new Vector2(size.X, 1f), color);
        DrawRect(position + new Vector2(0f, size.Y - 1f), new Vector2(size.X, 1f), color);
        DrawRect(position, new Vector2(1f, size.Y), color);
        DrawRect(position + new Vector2(size.X - 1f, 0f), new Vector2(1f, size.Y), color);
    }

    public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        float x = position.X;
        float charWidth = MathF.Max(4f, fontSize * 0.55f);
        int barHeight = Math.Max(1, (int)MathF.Ceiling(fontSize));

        foreach (char ch in text)
        {
            if (ch == ' ')
            {
                x += charWidth * 0.45f;
                continue;
            }

            DrawRect(new Vector2(x, position.Y), new Vector2(charWidth, barHeight), color);
            x += charWidth;
        }
    }

    public void SavePng(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        var writer = new ImageWriter();
        writer.WritePng(_pixels, _width, _height, ColorComponents.RedGreenBlueAlpha, stream);
    }

    private void FillBackground()
    {
        for (int y = 0; y < _height; y++)
        {
            float t = _height <= 1 ? 0f : y / (float)(_height - 1);
            var color = Vector4.Lerp(MenuTheme.StarfieldTop, MenuTheme.StarfieldBottom, t);
            for (int x = 0; x < _width; x++)
                WriteOpaquePixel(x, y, color);
        }
    }

    private void BlendPixel(int x, int y, Vector4 color)
    {
        int index = (y * _width + x) * 4;
        float alpha = Math.Clamp(color.W, 0f, 1f);
        if (alpha <= 0f)
            return;

        float inv = 1f - alpha;
        _pixels[index] = (byte)Math.Clamp(_pixels[index] * inv + color.X * 255f * alpha, 0f, 255f);
        _pixels[index + 1] = (byte)Math.Clamp(_pixels[index + 1] * inv + color.Y * 255f * alpha, 0f, 255f);
        _pixels[index + 2] = (byte)Math.Clamp(_pixels[index + 2] * inv + color.Z * 255f * alpha, 0f, 255f);
        _pixels[index + 3] = 255;
    }

    private void WriteOpaquePixel(int x, int y, Vector4 color)
    {
        int index = (y * _width + x) * 4;
        _pixels[index] = (byte)Math.Clamp(color.X * 255f, 0f, 255f);
        _pixels[index + 1] = (byte)Math.Clamp(color.Y * 255f, 0f, 255f);
        _pixels[index + 2] = (byte)Math.Clamp(color.Z * 255f, 0f, 255f);
        _pixels[index + 3] = 255;
    }
}