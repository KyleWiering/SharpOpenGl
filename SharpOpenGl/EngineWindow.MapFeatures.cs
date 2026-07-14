using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private int _planetVao, _planetVbo, _planetVertCount;
    private int _asteroidFieldVao, _asteroidFieldVbo, _asteroidFieldVertCount;
    private int _nebulaVao, _nebulaVbo, _nebulaVertCount;
    private int _sceneryVao, _sceneryVbo, _sceneryVertCount;
    private int _ionStormVao, _ionStormVbo, _ionStormVertCount;
    private int _wormholeRemnantVao, _wormholeRemnantVbo, _wormholeRemnantVertCount;

    private MapFeatureSpawner.MeshHandles BuildMapFeatureMeshes() => new()
    {
        PlanetMeshId = _planetVao,
        PlanetVertCount = _planetVertCount,
        AsteroidFieldMeshId = _asteroidFieldVao,
        AsteroidFieldVertCount = _asteroidFieldVertCount,
        NebulaMeshId = _nebulaVao,
        NebulaVertCount = _nebulaVertCount,
        SceneryMeshId = _sceneryVao,
        SceneryVertCount = _sceneryVertCount,
        IonStormMeshId = _ionStormVao,
        IonStormVertCount = _ionStormVertCount,
        WormholeRemnantMeshId = _wormholeRemnantVao,
        WormholeRemnantVertCount = _wormholeRemnantVertCount,
        ResourceNodeMeshId = _resourceNodeVao,
        ResourceNodeVertCount = _resourceNodeVertCount,
        PrimitiveTriangles = (int)PrimitiveType.Triangles,
    };

    private void LoadMapFeatureMeshes()
    {
        (_planetVao, _planetVbo, _planetVertCount) =
            MeshBuilder.BuildPlanetSphere(new Vector3(0.75f, 0.8f, 0.9f), 4f);
        (_asteroidFieldVao, _asteroidFieldVbo, _asteroidFieldVertCount) =
            MeshBuilder.BuildAsteroidFieldCluster(new Vector3(0.52f, 0.48f, 0.44f), 3f);
        (_nebulaVao, _nebulaVbo, _nebulaVertCount) =
            MeshBuilder.BuildNebulaCloud(3f);
        (_sceneryVao, _sceneryVbo, _sceneryVertCount) =
            MeshBuilder.BuildSceneryCluster(new Vector3(0.5f, 0.52f, 0.55f), 3f);
        (_ionStormVao, _ionStormVbo, _ionStormVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildIonStorm(3f));
        (_wormholeRemnantVao, _wormholeRemnantVbo, _wormholeRemnantVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildWormholeRemnant(3f));
    }

    private void SpawnMapContent(string mapKey)
    {
        if (_world == null || _assetManager == null) return;

        var map = _assetManager.Load<MapDefinition>($"Maps/{mapKey}");
        if (map == null)
        {
            Console.WriteLine($"[Map] Could not load Maps/{mapKey}, using procedural nodes only.");
            SpawnResourceNodes(ResolveProceduralMapSeed(mapKey));
            return;
        }

        if (!_sandboxChunkedMode && _gridSystem != null)
        {
            int terrainCells = MapTerrainApplicator.ApplyToGrid(_gridSystem, map);
            Console.WriteLine($"[Map] Applied terrain to {_gridSystem.Width}×{_gridSystem.Height} grid ({terrainCells} cells).");
        }

        MapFeatureSpawner.SpawnAll(_world, map, BuildMapFeatureMeshes(), RevealAreaAt);

        if (map.ResourceNodes.Length == 0 && map.MapFeatures.Length == 0)
            SpawnResourceNodes(ResolveProceduralMapSeed(mapKey));

        Console.WriteLine($"[Map] Spawned {map.ResourceNodes.Length} nodes and {map.MapFeatures.Length} features from {mapKey}.");
    }

    /// <summary>
    /// Deterministic seed for procedural economy scatter.
    /// Set during gameplay init; falls back to a stable hash of <paramref name="mapKey"/>.
    /// </summary>
    private int ResolveProceduralMapSeed(string? mapKey = null)
    {
        if (_proceduralMapSeed != 0)
            return _proceduralMapSeed;

        if (!string.IsNullOrEmpty(mapKey))
            return HashProceduralSeed(mapKey);

        return 42;
    }

    private static int HashProceduralSeed(string value) => ProceduralSeedHelper.HashString(value);
}