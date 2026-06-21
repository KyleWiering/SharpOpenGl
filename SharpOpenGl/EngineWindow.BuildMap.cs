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
        {
            _placementBuildingId = null;
            _placementPreviewValid = false;
        }

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

    private void OnBuildMapBuildingSelected(string defId)
    {
        if (_uiManager.Current is GameplayHUD hud)
            hud.BuildMapPanel.Visible = false;

        _placementBuildingId = defId;
        _placementPreviewValid = false;
        _attackMoveMode = false;
        _attackMode = false;
        _patrolMode = false;

        if (_definitions.TryGetValue(defId, out var def))
            Console.WriteLine($"[Place] Click to place: {def.DisplayName} (right-click to cancel)");
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
        _placementPreviewValid = result.IsValid;
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

    private (int meshId, int vertCount, Vector3 scale) ResolveBuildingMesh(string buildingType) => buildingType switch
    {
        "shipyard_small" => (_shipyardSmallVao, _shipyardSmallVertCount, new Vector3(1.6f, 1.6f, 1.6f)),
        "shipyard_medium" or "shipyard" => (_shipyardMediumVao, _shipyardMediumVertCount, new Vector3(2.2f, 2.2f, 2.2f)),
        "shipyard_large" => (_shipyardLargeVao, _shipyardLargeVertCount, new Vector3(2.8f, 2.8f, 2.8f)),
        "defense_turret" => (_defenseTurretVao, _defenseTurretVertCount, new Vector3(1.4f, 1.4f, 1.4f)),
        "sensor_array" => (_sensorArrayVao, _sensorArrayVertCount, new Vector3(1.8f, 1.8f, 1.8f)),
        "resource_refinery" => (_resourceRefineryVao, _resourceRefineryVertCount, new Vector3(2f, 2f, 2f)),
        "repair_bay" => (_repairBayVao, _repairBayVertCount, new Vector3(2.4f, 2.4f, 2.4f)),
        "power_reactor" => (_powerReactorVao, _powerReactorVertCount, new Vector3(1.8f, 1.8f, 1.8f)),
        "supply_depot" => (_supplyDepotVao, _supplyDepotVertCount, new Vector3(1.6f, 1.6f, 1.6f)),
        _ => (_commandCenterVao, _commandCenterVertCount, new Vector3(2f, 2f, 2f)),
    };

    private void RegisterExistingBuildingOccupancy(Entity entity, Vector3 position, BuildingComponent building)
    {
        if (_gridSystem == null) return;
        BuildingFootprint.Occupy(_gridSystem, entity, position, building.Footprint);
    }
}