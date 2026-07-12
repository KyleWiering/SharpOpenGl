using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

/// <summary>CLI options for <c>--mesh-preview</c> capture (set from Program before EngineWindow starts).</summary>
public static class MeshPreviewLaunchOptions
{
    public static bool Enabled { get; set; }
    public static string Race { get; set; } = "vesper";
    public static string Hull { get; set; } = "fighter_basic";
}

public partial class EngineWindow
{
    private readonly bool _meshPreviewMode = MeshPreviewLaunchOptions.Enabled;
    private readonly string _meshPreviewRace = MeshPreviewLaunchOptions.Race;
    private readonly string _meshPreviewHull = MeshPreviewLaunchOptions.Hull;
    private float _meshPreviewYaw;
    private int _meshPreviewVao;
    private int _meshPreviewVertCount;

    private void InitializeMeshPreview()
    {
        EnsureMeshAssets();
        string meshKey = MeshManifest.ShipKey(_meshPreviewRace, _meshPreviewHull);
        string fallback = "meshes/shared/default_ship.obj";

        float[] vertices = RaceShipMeshes.Build(_meshPreviewRace, _meshPreviewHull);
        var uploaded = MeshBuilder.UploadProcedural(vertices);
        _meshRegistry!.Register(meshKey, uploaded);
        _objMeshCache[meshKey] = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);

        var entry = _meshRegistry!.GetOrFallback(meshKey, fallback);
        _meshPreviewVao = entry?.Vao ?? 0;
        _meshPreviewVertCount = entry?.VertexCount ?? 0;
        _meshPreviewYaw = 45f;

        Console.WriteLine($"[MeshPreview] race={_meshPreviewRace} hull={_meshPreviewHull} key={meshKey} verts={_meshPreviewVertCount}");
    }

    private void RenderMeshPreview(Matrix4 projection)
    {
        if (_meshPreviewVao == 0) return;

        EnsureOpenGlRenderer();

        // Fixed 45° three-quarter profile — pulled back vs loop-40, still fills frame.
        var view = Matrix4.LookAt(new Vector3(7.2f, 6.0f, 7.2f), new Vector3(0f, 0.20f, 0f), Vector3.UnitY);
        var hull = RaceVisualSchema.ResolveHullProfile(_meshPreviewHull);
        float scale = Math.Clamp(14.5f / Math.Max(hull.Size, 1f), 4.2f, 5.8f);
        float yaw = MathHelper.DegreesToRadians(_meshPreviewYaw);
        Matrix4 model =
            Matrix4.CreateScale(scale) *
            Matrix4.CreateRotationY(yaw) *
            Matrix4.CreateTranslation(0f, 0.20f, 0f);

        int raceTexture = RaceTextureIndex.Resolve(_meshPreviewRace);
        _openGlRenderer!.BeginFrame(projection, view);
        _openGlRenderer.DrawMesh(
            _meshPreviewVao,
            _meshPreviewVertCount,
            model,
            Vector4.Zero,
            4,
            raceTexture,
            new Vector3(1.04f, 1.04f, 1.06f));
        _openGlRenderer.EndFrame();
    }
}