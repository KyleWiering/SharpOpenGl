using Microsoft.JSInterop;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Browser.Rendering;

/// <summary>Renders ECS entities with a WebGL2 batch via JS interop.</summary>
public sealed class WebGlSceneRenderer
{
    private readonly IJSRuntime _js;
    private readonly RTSCamera _camera = new();

    public WebGlSceneRenderer(IJSRuntime js) => _js = js;

    public RTSCamera Camera => _camera;

    public async Task InitializeAsync(string canvasId, int width, int height)
    {
        await _js.InvokeVoidAsync("sharpGl.init", canvasId);
        Resize(width, height);
    }

    public void Resize(int width, int height) =>
        _ = _js.InvokeVoidAsync("sharpGl.resize", width, height);

    public void Render(World world, int viewportWidth, int viewportHeight)
    {
        float aspect = viewportWidth / (float)Math.Max(1, viewportHeight);
        Matrix4 view = _camera.GetViewMatrix();
        Matrix4 proj = _camera.GetProjectionMatrix(aspectRatio: aspect);

        var batch = new List<float>(4096);
        AppendGrid(batch, 200, 200, 10f, new Vector4(0.15f, 0.15f, 0.25f, 1f));

        foreach (var (entity, render) in world.Query<RenderComponent>())
        {
            if (!render.Visible) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;
            AppendShipMarker(batch, transform.Position, render.Color, transform.Scale.X);
        }

        float[] mvp = Matrix4ToArray(proj * view);
        _ = _js.InvokeVoidAsync("sharpGl.clear", 0f, 0f, 0.02f);
        if (batch.Count > 0)
            _ = _js.InvokeVoidAsync("sharpGl.drawBatch", mvp, batch.ToArray(), 0);
    }

    private static void AppendGrid(List<float> batch, int cols, int rows, float cell, Vector4 color)
    {
        float w = cols * cell;
        float h = rows * cell;
        for (int x = 0; x <= cols; x += 4)
        {
            float px = x * cell - w * 0.5f;
            AddLine(batch, new Vector3(px, 0, -h * 0.5f), new Vector3(px, 0, h * 0.5f), color);
        }
        for (int z = 0; z <= rows; z += 4)
        {
            float pz = z * cell - h * 0.5f;
            AddLine(batch, new Vector3(-w * 0.5f, 0, pz), new Vector3(w * 0.5f, 0, pz), color);
        }
    }

    private static void AppendShipMarker(List<float> batch, Vector3 pos, Vector4 color, float scale)
    {
        float s = MathF.Max(2f, scale * 3f);
        AddTriangle(batch,
            pos + new Vector3(0, 0, s),
            pos + new Vector3(-s * 0.5f, 0, -s * 0.4f),
            pos + new Vector3(s * 0.5f, 0, -s * 0.4f),
            color);
    }

    private static void AddLine(List<float> batch, Vector3 a, Vector3 b, Vector4 c)
    {
        AddVertex(batch, a, c);
        AddVertex(batch, b, c);
    }

    private static void AddTriangle(List<float> batch, Vector3 a, Vector3 b, Vector3 c, Vector4 col)
    {
        AddVertex(batch, a, col);
        AddVertex(batch, b, col);
        AddVertex(batch, c, col);
    }

    private static void AddVertex(List<float> batch, Vector3 p, Vector4 c)
    {
        batch.Add(p.X); batch.Add(p.Y); batch.Add(p.Z);
        batch.Add(c.X); batch.Add(c.Y); batch.Add(c.Z); batch.Add(c.W);
    }

    private static float[] Matrix4ToArray(Matrix4 m) =>
        [m.M11, m.M12, m.M13, m.M14,
         m.M21, m.M22, m.M23, m.M24,
         m.M31, m.M32, m.M33, m.M34,
         m.M41, m.M42, m.M43, m.M44];
}