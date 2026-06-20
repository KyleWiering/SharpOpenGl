using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private int _planetVao, _planetVbo, _planetVertCount;
    private int _sceneryVao, _sceneryVbo, _sceneryVertCount;

    private MapFeatureSpawner.MeshHandles BuildMapFeatureMeshes() => new()
    {
        PlanetMeshId = _planetVao,
        PlanetVertCount = _planetVertCount,
        SceneryMeshId = _sceneryVao,
        SceneryVertCount = _sceneryVertCount,
        ResourceNodeMeshId = _resourceNodeVao,
        ResourceNodeVertCount = _resourceNodeVertCount,
        PrimitiveTriangles = (int)PrimitiveType.Triangles,
    };

    private void LoadMapFeatureMeshes()
    {
        (_planetVao, _planetVbo, _planetVertCount) =
            MeshBuilder.BuildPlanetSphere(new Vector3(0.75f, 0.8f, 0.9f), 4f);
        (_sceneryVao, _sceneryVbo, _sceneryVertCount) =
            MeshBuilder.BuildSceneryCluster(new Vector3(0.5f, 0.52f, 0.55f), 3f);
    }

    private void SpawnMapContent(string mapKey)
    {
        if (_world == null || _assetManager == null) return;

        var map = _assetManager.Load<MapDefinition>($"Maps/{mapKey}");
        if (map == null)
        {
            Console.WriteLine($"[Map] Could not load Maps/{mapKey}, using procedural nodes only.");
            SpawnResourceNodes(new Random(123));
            return;
        }

        MapFeatureSpawner.SpawnAll(_world, map, BuildMapFeatureMeshes(), RevealAreaAt);

        if (map.ResourceNodes.Length == 0 && map.MapFeatures.Length == 0)
            SpawnResourceNodes(new Random(123));

        Console.WriteLine($"[Map] Spawned {map.ResourceNodes.Length} nodes and {map.MapFeatures.Length} features from {mapKey}.");
    }
}