using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private ShipyardPreviewSystem? _shipyardPreviewSystem;

    private void RegisterShipyardPreviewSystem()
    {
        _shipyardPreviewSystem = new ShipyardPreviewSystem(id =>
            _definitions.TryGetValue(id, out var def) ? def : null)
        {
            MeshUploader = UploadShipyardPreviewMesh,
            ResolveFactionRaceId = playerId =>
                ResolveFactionRaceId(playerId, isEnemy: playerId != _humanPlayerId),
        };
        _world!.AddSystem(_shipyardPreviewSystem);
    }

    private (int MeshId, int VertexCount) UploadShipyardPreviewMesh(float[] vertices, int primitiveType)
    {
        bool lines = primitiveType == 1;
        var uploaded = MeshBuilder.UploadProcedural(vertices, lines);
        return (uploaded.vao, uploaded.vertexCount);
    }

    private bool IsShipyardPreviewEntity(Entity entity) =>
        _world != null && _world.HasComponent<ShipyardPreviewTagComponent>(entity);

    private void SetupShipyardPreviewScreenshotArtifact()
    {
        if (_world == null) return;

        Vector3 yardPos = Vector3.Zero;
        SpawnShipyardWithBuildPreview("shipyard_small", yardPos, "fighter_basic", buildFraction: 0.5f);

        Vector3 exitPad = yardPos + new Vector3(35f, 0f, 0f);
        _rtsCamera.Target = exitPad;
        _rtsCamera.Height = 52f;
        RevealAreaAt(yardPos, 24);

        Console.WriteLine("[Screenshot] Shipyard preview mid-build framing at ~50% fighter_basic.");
    }

    private void SpawnShipyardWithBuildPreview(
        string buildingId,
        Vector3 worldPos,
        string unitId,
        float buildFraction)
    {
        if (_world == null) return;

        if (!_definitions.TryGetValue(buildingId, out var def))
        {
            def = _assetManager?.Load<EntityDefinition>($"Bases/{buildingId}");
            if (def != null)
                _definitions[buildingId] = def;
        }

        if (def == null)
        {
            Console.WriteLine($"[ShipyardPreview] Unknown building '{buildingId}'.");
            return;
        }

        if (!_definitions.TryGetValue(unitId, out var unitDef))
        {
            unitDef = _assetManager?.Load<EntityDefinition>($"Ships/{unitId}");
            if (unitDef != null)
                _definitions[unitId] = unitDef;
        }

        if (unitDef == null || unitDef.BuildTime <= 0f)
        {
            Console.WriteLine($"[ShipyardPreview] Unknown or invalid unit '{unitId}'.");
            return;
        }

        string buildingType = def.Components?.Building?.BuildingType ?? buildingId;
        var (meshId, vertCount, scale) = ResolveBuildingMesh(buildingType, 1);

        var building = _world.CreateEntity();
        _world.AddComponent(building, new TransformComponent { Position = worldPos, Scale = scale });
        var render = new RenderComponent
        {
            MeshId = meshId,
            VertexCount = vertCount,
            Visible = true,
            PrimitiveType = (int)PrimitiveType.Triangles,
        };
        ApplyRaceTexturing(render, ResolveFactionRaceId(1, isEnemy: false), 1);
        _world.AddComponent(building, render);
        _world.AddComponent(building, new SelectionComponent { IsSelected = false, SelectionRadius = 15f });

        var healthDef = def.Components?.Health;
        _world.AddComponent(building, new HealthComponent
        {
            MaxHP = healthDef?.MaxHP ?? 1500f,
            CurrentHP = healthDef?.MaxHP ?? 1500f,
            Armor = healthDef?.Armor ?? 75f,
        });

        var buildingDef = def.Components?.Building;
        float clampedFraction = Math.Clamp(buildFraction, 0.01f, 0.99f);
        var buildingComp = new BuildingComponent
        {
            BuildingType = buildingType,
            ProductionRate = buildingDef?.ProductionRate ?? 1f,
            Footprint = buildingDef?.Footprint ?? [2, 2],
            Rotates = buildingDef?.Rotates ?? false,
            PlayerId = 1,
            RallyPoint = worldPos + new Vector3(35f, 0f, 0f),
            Producible = def.Producible?.ToList() ?? [],
            BuildProgress = clampedFraction * unitDef.BuildTime,
        };
        buildingComp.BuildQueue.Enqueue(unitId);
        _world.AddComponent(building, buildingComp);
        _world.AddComponent(building, new EntityNameComponent
        {
            DisplayName = string.IsNullOrWhiteSpace(def.DisplayName)
                ? buildingType.Replace("_", " ")
                : def.DisplayName,
            DefinitionId = def.Id,
        });

        if (_gridSystem != null)
            BuildingFootprint.Occupy(_gridSystem, building, worldPos, buildingComp.Footprint);
    }
}