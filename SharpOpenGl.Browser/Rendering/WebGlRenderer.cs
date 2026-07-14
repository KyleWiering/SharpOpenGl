using Microsoft.JSInterop;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Browser.Rendering;

/// <summary>
/// WebGL2 implementation of <see cref="IRenderer"/> — same shader/uniforms as desktop OpenGL.
/// </summary>
public sealed class WebGlRenderer : IRenderer
{
    private readonly IJSRuntime _js;
    private Matrix4 _projection = Matrix4.Identity;
    private Matrix4 _view = Matrix4.Identity;
    private bool _inFrame;

    public WebGlRenderer(IJSRuntime js) => _js = js;

    public async Task InitializeAsync(string canvasId, int width, int height)
    {
        bool ok = await _js.InvokeAsync<bool>("sharpGl.init", canvasId);
        if (!ok) throw new InvalidOperationException("WebGL2 initialization failed.");
        Resize(width, height);
    }

    public async Task<int> UploadMeshAsync(float[] vertices) =>
        await _js.InvokeAsync<int>("sharpGl.uploadMesh", vertices);

    public void Clear(float r, float g, float b) =>
        _ = _js.InvokeVoidAsync("sharpGl.clear", r, g, b);

    public void BeginFrame(Matrix4 projection, Matrix4 view)
    {
        _projection = projection;
        _view = view;
        _inFrame = true;
        _ = _js.InvokeVoidAsync("sharpGl.beginFrame",
            MatrixToArray(projection), MatrixToArray(view));
    }

    public void DrawMesh(int vao, int vertexCount, Matrix4 model, Vector4 color, int primitiveType,
        int raceTextureIndex = -1, Vector3 teamTint = default, int componentTextureIndex = -1)
    {
        if (!_inFrame || vao <= 0 || vertexCount <= 0) return;
        _ = _js.InvokeVoidAsync("sharpGl.drawMesh",
            vao, vertexCount, MatrixToArray(model),
            new[] { color.X, color.Y, color.Z, color.W }, primitiveType,
            raceTextureIndex,
            new[] { teamTint.X, teamTint.Y, teamTint.Z },
            componentTextureIndex);
    }

    public void DrawPoints(int meshId, float[] vertices, int pointCount, Matrix4 model, float pointSize = 5f)
    {
        if (!_inFrame || meshId <= 0 || pointCount <= 0 || vertices.Length == 0) return;
        _ = _js.InvokeVoidAsync("sharpGl.drawPoints",
            meshId, vertices, pointCount, MatrixToArray(model), pointSize);
    }

    public void DrawLineStrip(int meshId, float[] vertices, int vertexCount, Matrix4 model, Vector4 color)
    {
        if (!_inFrame || meshId <= 0 || vertexCount <= 0 || vertices.Length == 0) return;
        _ = _js.InvokeVoidAsync("sharpGl.drawLineStrip",
            meshId, vertices, vertexCount, MatrixToArray(model),
            new[] { color.X, color.Y, color.Z, color.W });
    }

    public void EndFrame() => _inFrame = false;

    public void Resize(int width, int height) =>
        _ = _js.InvokeVoidAsync("sharpGl.resize", width, height);

    private static float[] MatrixToArray(Matrix4 m) =>
        [m.M11, m.M12, m.M13, m.M14,
         m.M21, m.M22, m.M23, m.M24,
         m.M31, m.M32, m.M33, m.M34,
         m.M41, m.M42, m.M43, m.M44];
}