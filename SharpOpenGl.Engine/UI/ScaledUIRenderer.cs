using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Wraps an <see cref="IUIRenderer"/> and scales logical (reference-resolution)
/// draw calls to physical screen pixels.
/// </summary>
public sealed class ScaledUIRenderer : IUIRenderer
{
    /// <summary>Minimum physical font size so menu text stays legible at 1024×768.</summary>
    public const float MinPhysicalFontSize = 16f;

    private readonly IUIRenderer _inner;
    private readonly UIScaler _scaler;

    public ScaledUIRenderer(IUIRenderer inner, UIScaler scaler)
    {
        _inner = inner;
        _scaler = scaler;
    }

    /// <inheritdoc/>
    public Vector2 ViewportSize => _inner.ViewportSize;

    /// <inheritdoc/>
    public float ScaleToPhysical(float logicalPixels) =>
        logicalPixels * _scaler.UniformScale;

    /// <inheritdoc/>
    public float ResolveFontSize(float logicalFontSize) =>
        MathF.Max(logicalFontSize * _scaler.UniformScale, MinPhysicalFontSize);

    /// <inheritdoc/>
    public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
    {
        _inner.DrawRect(_scaler.ScalePosition(position), _scaler.ScaleSize(size), color);
    }

    /// <inheritdoc/>
    public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color)
    {
        _inner.DrawRectOutline(_scaler.ScalePosition(position), _scaler.ScaleSize(size), color);
    }

    /// <inheritdoc/>
    public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
    {
        float physicalSize = ResolveFontSize(fontSize);
        _inner.DrawText(text, _scaler.ScalePosition(position), physicalSize, color);
    }
}