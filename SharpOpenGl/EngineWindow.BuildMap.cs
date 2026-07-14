using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private BuildMapCatalog? _buildMapCatalog;
    private bool _placementPreviewValid;
    private int _defenseTurretVao, _defenseTurretVbo, _defenseTurretVertCount;
    private int _sensorArrayVao, _sensorArrayVbo, _sensorArrayVertCount;
    private int _resourceRefineryVao, _resourceRefineryVbo, _resourceRefineryVertCount;
    private int _repairBayVao, _repairBayVbo, _repairBayVertCount;
    private int _powerReactorVao, _powerReactorVbo, _powerReactorVertCount;
    private int _supplyDepotVao, _supplyDepotVbo, _supplyDepotVertCount;

    private void LoadBuildMapCatalog()
    {
        const string path = "GameData/Config/build_map.json";
        var config = JsonLoader.Load<BuildMapConfig>(path);
        if (config == null || config.Categories.Count == 0)
        {
            Console.WriteLine("[BuildMap] No build_map.json found, using empty catalog.");
            _buildMapCatalog = new BuildMapCatalog(new BuildMapConfig(), _definitions);
            return;
        }

        _buildMapCatalog = new BuildMapCatalog(config, _definitions);
        Console.WriteLine($"[BuildMap] Loaded {config.Categories.Count} categories.");
    }

    private void LoadStructureMeshes()
    {
        (_defenseTurretVao, _defenseTurretVbo, _defenseTurretVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildDefenseTurret(new Vector3(0.9f, 0.35f, 0.3f)));
        (_sensorArrayVao, _sensorArrayVbo, _sensorArrayVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildSensorArray(new Vector3(0.45f, 0.75f, 1f)));
        (_resourceRefineryVao, _resourceRefineryVbo, _resourceRefineryVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildResourceRefinery(new Vector3(0.75f, 0.55f, 0.35f)));
        (_repairBayVao, _repairBayVbo, _repairBayVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildRepairBay(new Vector3(0.55f, 0.7f, 0.95f)));
        (_powerReactorVao, _powerReactorVbo, _powerReactorVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildPowerReactor(new Vector3(0.35f, 0.95f, 1f)));
        (_supplyDepotVao, _supplyDepotVbo, _supplyDepotVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildSupplyDepot(new Vector3(0.85f, 0.7f, 0.25f)));
    }

    private void WireBuildMapHud(GameplayHUD hud)
    {
        hud.BuildMapPanel.BuildingSelected += OnBuildMapBuildingSelected;
        hud.BuildMapPanel.CloseRequested += () => hud.BuildMapPanel.Visible = false;
        hud.BuildMapRequested += ToggleBuildMapPanel;
    }

    private void ToggleBuildMapPanel()
    {
        if (_uiManager.Current is not GameplayHUD hud) return;

        if (_placementBuildingId != null)
            CancelPlacementMode();

        _builderPickEntity = null;

        hud.BuildMapPanel.Visible = !hud.BuildMapPanel.Visible;
        if (hud.BuildMapPanel.Visible)
            BindBuildMapPanel();
    }

    private void BindBuildMapPanel()
    {
        if (_world == null || _resourceManager == null || _buildMapCatalog == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        hud.BuildMapPanel.Categories = _buildMapCatalog.BuildViews(
            _world, playerId: 1, _resourceManager, _supplySystem);
    }

    private void BindBuilderBuildPanel(Entity builder)
    {
        if (_world == null || _resourceManager == null || _buildMapCatalog == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        var builderComp = _world.GetComponent<StructureBuilderComponent>(builder);
        if (builderComp == null) return;

        var views = _buildMapCatalog.BuildViews(_world, playerId: 1, _resourceManager, _supplySystem);
        hud.BuildMapPanel.Categories = FilterBuildViews(views, builderComp.BuildableIds);
    }

    private static List<BuildMapCategoryView> FilterBuildViews(
        List<BuildMapCategoryView> views,
        IReadOnlyList<string> buildableIds)
    {
        var allowed = new HashSet<string>(buildableIds, StringComparer.OrdinalIgnoreCase);
        var filtered = new List<BuildMapCategoryView>();

        foreach (var category in views)
        {
            var buildings = category.Buildings
                .Where(entry => allowed.Contains(entry.Id))
                .ToList();
            if (buildings.Count == 0)
                continue;

            filtered.Add(new BuildMapCategoryView
            {
                Id = category.Id,
                DisplayName = category.DisplayName,
                Buildings = buildings,
            });
        }

        return filtered;
    }

    private void OnBuildMapBuildingSelected(string defId)
    {
        if (_uiManager.Current is GameplayHUD hud)
            hud.BuildMapPanel.Visible = false;

        Entity? builder = _builderPickEntity;
        _builderPickEntity = null;
        BeginPlacementMode(defId, builder);
    }

    private void BeginPlacementMode(string buildingId, Entity? builderEntity = null)
    {
        _placementBuildingId = buildingId;
        _placementBuilderEntity = builderEntity;
        _placementPreviewValid = false;
        _attackMoveMode = false;
        _attackMode = false;
        _patrolMode = false;
        _moveCommandMode = false;
        _harvestCommandMode = false;

        if (_definitions.TryGetValue(buildingId, out var def))
            Console.WriteLine($"[Place] Click to place: {def.DisplayName} (right-click to cancel)");
    }

    private void CancelPlacementMode()
    {
        _placementBuildingId = null;
        _placementBuilderEntity = null;
        _placementPreviewValid = false;
        _builderPickEntity = null;
    }

    private void EnterBuilderStructurePickMode()
    {
        if (_world == null || _buildMapCatalog == null) return;

        var selected = GetSelectedPlayerEntities();
        Entity? builder = null;
        foreach (var entity in selected)
        {
            if (_world.HasComponent<StructureBuilderComponent>(entity))
            {
                builder = entity;
                break;
            }
        }

        if (!builder.HasValue) return;

        _builderPickEntity = builder;
        _attackMoveMode = false;
        _attackMode = false;
        _patrolMode = false;
        _moveCommandMode = false;
        _harvestCommandMode = false;

        if (_uiManager.Current is GameplayHUD hud)
        {
            BindBuilderBuildPanel(builder.Value);
            hud.BuildMapPanel.Visible = true;
        }
    }

    private bool IsBuilderInPlacementRange(Entity builderEntity, Vector3 worldPos, out string reason)
    {
        reason = string.Empty;
        if (_world == null)
            return true;

        var builderComp = _world.GetComponent<StructureBuilderComponent>(builderEntity);
        var builderTransform = _world.GetComponent<TransformComponent>(builderEntity);
        if (builderComp == null || builderTransform == null)
            return true;

        float range = builderComp.PlacementRange > 0f
            ? builderComp.PlacementRange
            : StructureBuilderComponent.DefaultPlacementRange;
        float distance = Vector3.Distance(builderTransform.Position, worldPos);
        if (distance <= range)
            return true;

        reason = $"Builder out of range ({range:0}m)";
        return false;
    }

    private void UpdatePlacementPreview()
    {
        _placementPreviewValid = false;
        if (_placementBuildingId == null || _world == null || _gridSystem == null ||
            _resourceManager == null || _buildMapCatalog == null)
            return;

        if (!_definitions.TryGetValue(_placementBuildingId, out var def))
            return;

        Vector3? worldPos = ScreenToWorldGround(new Vector2(MousePosition.X, MousePosition.Y));
        if (worldPos == null) return;

        var result = BuildingPlacementValidator.Validate(
            _gridSystem, _world, playerId: 1, def, worldPos.Value,
            _buildMapCatalog, _resourceManager, _supplySystem);

        bool inRange = true;
        if (_placementBuilderEntity is Entity builderEntity)
            inRange = IsBuilderInPlacementRange(builderEntity, worldPos.Value, out _);

        _placementPreviewValid = result.IsValid && inRange;
    }

    private void RenderPlacementPreview()
    {
        if (_placementBuildingId == null || _gridSystem == null || _fogQuadVertCount == 0) return;
        if (!_definitions.TryGetValue(_placementBuildingId, out var def)) return;

        Vector3? worldPos = ScreenToWorldGround(new Vector2(MousePosition.X, MousePosition.Y));
        if (worldPos == null) return;

        var (cols, rows) = BuildingFootprint.GetSize(def.Components?.Building?.Footprint);
        Vector4 color = _placementPreviewValid
            ? new Vector4(0.2f, 0.9f, 0.35f, 0.45f)
            : new Vector4(0.95f, 0.2f, 0.2f, 0.5f);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_fogQuadVao);

        foreach (var (x, y) in BuildingFootprint.EnumerateCells(_gridSystem, worldPos.Value, cols, rows))
        {
            Vector3 center = _gridSystem.GridToWorld(x, y);
            var model = Matrix4.CreateScale(GridCellSize, 1f, GridCellSize) *
                        Matrix4.CreateTranslation(center.X, 0.18f, center.Z);
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, color);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _fogQuadVertCount);
        }

        GL.Disable(EnableCap.Blend);
    }

    private (int meshId, int vertCount, Vector3 scale) ResolveBuildingMesh(string buildingType, int playerId = 1)
    {
        string raceId = ResolveFactionRaceId(playerId, isEnemy: playerId != _humanPlayerId);
        return ResolveRaceBuildingMesh(buildingType, raceId);
    }

    private void RegisterExistingBuildingOccupancy(Entity entity, Vector3 position, BuildingComponent building)
    {
        if (_gridSystem == null) return;
        BuildingFootprint.Occupy(_gridSystem, entity, position, building.Footprint);
    }
}