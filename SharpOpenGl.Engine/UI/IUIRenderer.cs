using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Minimal 2-D drawing interface used by the UI layer.
/// Implementations can target OpenGL immediate-mode quads, a sprite batcher,
/// or a no-op stub for unit tests.
/// </summary>
public interface IUIRenderer
{
    /// <summary>Current viewport dimensions in pixels.</summary>
    Vector2 ViewportSize { get; }

    /// <summary>Draw a filled rectangle.</summary>
    /// <param name="position">Top-left corner in screen-space pixels.</param>
    /// <param name="size">Width and height in pixels.</param>
    /// <param name="color">RGBA colour (each channel 0–1).</param>
    void DrawRect(Vector2 position, Vector2 size, Vector4 color);

    /// <summary>Draw a rectangle outline (1-pixel border).</summary>
    void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color);

    /// <summary>
    /// Render a string of text.
    /// </summary>
    /// <param name="text">Text to render.</param>
    /// <param name="position">Top-left of the text block in screen-space pixels.</param>
    /// <param name="fontSize">Approximate character height in pixels.</param>
    /// <param name="color">RGBA colour.</param>
    void DrawText(string text, Vector2 position, float fontSize, Vector4 color);
}
