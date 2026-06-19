using Microsoft.JSInterop;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Browser.Rendering;

/// <summary>Canvas 2D implementation of <see cref="IUIRenderer"/> via JS interop.</summary>
public sealed class CanvasUIRenderer : IUIRenderer
{
    private readonly IJSRuntime _js;

    public CanvasUIRenderer(IJSRuntime js) => _js = js;

    public Vector2 ViewportSize { get; private set; }

    public async Task InitializeAsync(string canvasId, int width, int height)
    {
        await _js.InvokeVoidAsync("sharpUi.init", canvasId);
        Resize(width, height);
    }

    public void Resize(int width, int height)
    {
        ViewportSize = new Vector2(width, height);
        _ = _js.InvokeVoidAsync("sharpUi.resize", width, height);
    }

    public void Begin() => _ = _js.InvokeVoidAsync("sharpUi.clear");

    public void End() { }

    public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
        _ = _js.InvokeVoidAsync("sharpUi.fillRect",
            position.X, position.Y, size.X, size.Y, color.X, color.Y, color.Z, color.W);

    public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) =>
        _ = _js.InvokeVoidAsync("sharpUi.strokeRect",
            position.X, position.Y, size.X, size.Y, color.X, color.Y, color.Z, color.W);

    public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
        _ = _js.InvokeVoidAsync("sharpUi.drawText",
            text, position.X, position.Y, fontSize, color.X, color.Y, color.Z, color.W);
}