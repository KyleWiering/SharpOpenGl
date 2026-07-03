using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private MeshRegistry? _meshRegistry;
    private MeshAssetService? _meshAssetService;
    private ShipDesignerRenderer? _shipDesignerRenderer;
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
                ProceduralMeshes.BuildShipMesh(new OpenTK.Mathematics.Vector3(0.5f, 0.5f, 0.8f)));
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

    internal void RenderShipDesignerPreview(ShipDesignerScreen designer)
    {
        EnsureMeshAssets();
        _shipDesignerRenderer ??= new ShipDesignerRenderer(_meshRegistry!);

        string meshKey = designer.MeshKey;
        string fallback = "meshes/shared/default_ship.obj";

        if (!_meshRegistry!.Contains(meshKey))
            TryGetObjMesh(meshKey);

        // Preview uses the platform renderer when available; designer overlay is UI-only for now.
        _shipDesignerRenderer.AutoRotate(designer, 1f / 60f);
    }
}