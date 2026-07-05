using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private MeshRegistry? _meshRegistry;
    private MeshAssetService? _meshAssetService;
    private ShipDesignerRenderer? _shipDesignerRenderer;
    private OpenGlRenderer? _openGlRenderer;
    private readonly Dictionary<string, (int vao, int vbo, int vertCount)> _objMeshCache = new(StringComparer.OrdinalIgnoreCase);

    private void EnsureMeshAssets()
    {
        string root = ResolveGameDataPath();
        _meshAssetService ??= new MeshAssetService(root);
        _meshRegistry ??= new MeshRegistry();
        RegisterManifestMeshes();
    }

    private void RegisterManifestMeshes()
    {
        if (_meshAssetService == null || _meshRegistry == null) return;

        foreach (var entry in _meshAssetService.Manifest.Entries)
        {
            if (_meshRegistry.Contains(entry.Key))
                continue;

            string path = _meshAssetService.Manifest.ResolveMeshPath(_meshAssetService.GameDataRoot, entry.Key);
            var data = ObjMeshLoader.Parse(path);
            if (data == null) continue;

            var uploaded = MeshBuilder.UploadObj(data);
            _meshRegistry.Register(entry.Key, uploaded);
            _objMeshCache[entry.Key] = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
        }

        if (!_meshRegistry.Contains("meshes/shared/default_ship.obj"))
        {
            var fallback = MeshBuilder.UploadProcedural(
                ProceduralMeshes.BuildShipMesh(new Vector3(0.5f, 0.5f, 0.8f)));
            _meshRegistry.Register("meshes/shared/default_ship.obj", fallback);
        }
    }

    private (int vao, int vertCount) TryGetObjMesh(string meshKey)
    {
        EnsureMeshAssets();
        if (_objMeshCache.TryGetValue(meshKey, out var cached))
            return (cached.vao, cached.vertCount);

        if (_meshAssetService?.TryLoadObj(meshKey) is { } data)
        {
            var uploaded = MeshBuilder.UploadObj(data);
            _objMeshCache[meshKey] = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
            _meshRegistry?.Register(meshKey, uploaded);
            return (uploaded.vao, uploaded.vertexCount);
        }

        return (0, 0);
    }

    private void EnsureOpenGlRenderer()
    {
        _openGlRenderer ??= new OpenGlRenderer(
            _shaderProgram,
            _uniformProjection,
            _uniformView,
            _uniformModel,
            _uniformColor,
            _uniformRaceTextureIndex,
            _uniformTeamTint);
    }

    private bool EnsureDesignerMeshLoaded(ShipDesignerScreen designer)
    {
        EnsureMeshAssets();
        string meshKey = designer.MeshKey;
        const string fallback = "meshes/shared/default_ship.obj";

        if (!_meshRegistry!.Contains(meshKey))
        {
            if (TryGetObjMesh(meshKey).vao == 0)
            {
                float[] vertices = designer.Category == DesignerAssetCategory.Station
                    ? RaceBuildingMeshes.Build(designer.ShipId, designer.RaceId)
                    : RaceShipMeshes.Build(designer.RaceId, designer.ShipId);

                var uploaded = MeshBuilder.UploadProcedural(vertices);
                _meshRegistry.Register(meshKey, uploaded);
                _objMeshCache[meshKey] = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
            }
        }

        bool ready = _meshRegistry.GetOrFallback(meshKey, fallback)?.Vao > 0;
        designer.NotifyPreviewMeshReady(ready);
        return ready;
    }

    private float ResolveDesignerPreviewScale(ShipDesignerScreen designer)
    {
        var hull = RaceVisualSchema.ResolveHullProfile(designer.ShipId);
        return Math.Clamp(9f / Math.Max(hull.Size, 1f), 1.2f, 2.8f);
    }

    internal void RenderShipDesignerPreview(ShipDesignerScreen designer, Matrix4 projection, float deltaTime)
    {
        if (!EnsureDesignerMeshLoaded(designer))
            return;

        EnsureOpenGlRenderer();
        _shipDesignerRenderer ??= new ShipDesignerRenderer(_meshRegistry!);
        _shipDesignerRenderer.AutoRotate(designer, deltaTime);

        var view = Matrix4.LookAt(new Vector3(14f, 9f, 14f), Vector3.Zero, Vector3.UnitY);
        int raceTexture = RaceTextureIndex.Resolve(designer.RaceId);
        float scale = ResolveDesignerPreviewScale(designer);

        _shipDesignerRenderer.Render(
            designer,
            designer.MeshKey,
            "meshes/shared/default_ship.obj",
            _openGlRenderer!,
            projection,
            view,
            scale,
            raceTexture,
            Vector3.One);
    }

    private void BindShipDesigner(ShipDesignerScreen designer)
    {
        void LoadPreview()
        {
            EnsureDesignerMeshLoaded(designer);
        }

        designer.SelectionChanged += LoadPreview;
        designer.PreviewRequested += LoadPreview;
        LoadPreview();
    }

}