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
    private PlacementFailureReason _placementPreviewReason = PlacementFailureReason.None;
    private bool _placementPreviewInRange = true;
    private Vector3? _placementSnappedPos;
    private float _placementHintFlashTimer;
    private float _placementValidPulsePhase;
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
                TierIndex = category.TierIndex,
                UnlockedCount = buildings.Count(static entry => entry.IsUnlocked),
                TotalCount = buildings.Count,
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
        _placementPreviewReason = PlacementFailureReason.None;
        _placementPreviewInRange = true;
        _placementSnappedPos = null;
        _placementHintFlashTimer = 0f;
        _placementValidPulsePhase = 0f;
        _attackMoveMode = false;
        _attackMode = false;
        _patrolMode = false;
        _moveCommandMode = false;
        _harvestCommandMode = false;

        if (_definitions.TryGetValue(buildingId, out var def))
        {
            string displayName = string.IsNullOrWhiteSpace(def.DisplayName)
                ? buildingId
                : def.DisplayName;
            SetPlacementHint($"{displayName} — Right-click to cancel", isValid: true);

            // Rotation input for components.building.rotates is deferred (no rotate key binding yet).
            _ = def.Components?.Building?.Rotates;
        }
    }

    private void CancelPlacementMode(bool keepSuccessToast = false)
    {
        _placementBuildingId = null;
        _placementBuilderEntity = null;
        _placementPreviewValid = false;
        _placementPreviewReason = PlacementFailureReason.None;
        _placementPreviewInRange = true;
        _placementSnappedPos = null;
        _placementValidPulsePhase = 0f;
        _builderPickEntity = null;
        if (!keepSuccessToast)
        {
            _placementHintFlashTimer = 0f;
            ClearPlacementHint();
        }
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

        reason = PlacementFailureReasonExtensions.ToBuilderRangeMessage(range);
        return false;
    }

    private bool TryGetSnappedPlacementCursor(out Vector3 snappedPos)
    {
        snappedPos = default;
        if (_gridSystem == null)
            return false;

        Vector3? worldPos = ScreenToWorldGround(new Vector2(MousePosition.X, MousePosition.Y));
        if (worldPos == null)
            return false;

        snappedPos = BuildingFootprint.SnapToCellCenter(_gridSystem, worldPos.Value);
        return true;
    }

    private void SetPlacementHint(string hint, bool isValid, bool flash = false)
    {
        if (_uiManager.Current is not GameplayHUD hud)
            return;

        hud.PlacementHint = hint;
        hud.PlacementHintIsValid = isValid;
        hud.PlacementHintFlash = flash ? 1f : 0f;
        if (flash)
            _placementHintFlashTimer = 0.55f;
    }

    private void ClearPlacementHint()
    {
        if (_uiManager.Current is not GameplayHUD hud)
            return;

        hud.PlacementHint = string.Empty;
        hud.PlacementHintIsValid = true;
        hud.PlacementHintFlash = 0f;
    }

    private void FlashPlacementFailure(string message)
    {
        SetPlacementHint(message, isValid: false, flash: true);
    }

    private void FlashPlacementSuccess(string message)
    {
        SetPlacementHint(message, isValid: true, flash: true);
    }

    private static Vector4 ResolvePlacementCategoryMarker(
        PlacementFailureReason reason, bool inRange)
    {
        if (!inRange)
            return new Vector4(0.95f, 0.35f, 0.95f, 0.95f);

        return reason switch
        {
            PlacementFailureReason.Locked or PlacementFailureReason.CannotAfford
                or PlacementFailureReason.SupplyCap or PlacementFailureReason.UnknownDefinition =>
                new Vector4(1f, 0.78f, 0.2f, 0.95f),
            PlacementFailureReason.ImpassableTerrain or PlacementFailureReason.OutOfBounds
                or PlacementFailureReason.ResourceBlocked =>
                new Vector4(1f, 0.52f, 0.12f, 0.95f),
            PlacementFailureReason.CellOccupied =>
                new Vector4(0.72f, 0.1f, 0.1f, 0.95f),
            _ => new Vector4(0.85f, 0.85f, 0.85f, 0.9f),
        };
    }

    private void TickPlacementHintFlash(float deltaTime)
    {
        if (_placementBuildingId != null)
            _placementValidPulsePhase += deltaTime * 4.5f;

        if (_placementHintFlashTimer > 0f)
        {
            _placementHintFlashTimer = Math.Max(0f, _placementHintFlashTimer - deltaTime);
            if (_uiManager.Current is GameplayHUD hud && !string.IsNullOrWhiteSpace(hud.PlacementHint))
                hud.PlacementHintFlash = _placementHintFlashTimer / 0.55f;

            if (_placementHintFlashTimer <= 0f && _placementBuildingId == null)
                ClearPlacementHint();
            return;
        }

        if (_placementBuildingId != null && _placementPreviewValid &&
            _uiManager.Current is GameplayHUD previewHud)
        {
            previewHud.PlacementHintFlash = 0.5f + 0.5f * MathF.Sin(_placementValidPulsePhase);
        }
    }

    private void UpdatePlacementPreview()
    {
        _placementPreviewValid = false;
        _placementPreviewReason = PlacementFailureReason.None;
        _placementPreviewInRange = true;
        _placementSnappedPos = null;

        if (_placementBuildingId == null || _world == null || _gridSystem == null ||
            _resourceManager == null || _buildMapCatalog == null)
            return;

        if (!_definitions.TryGetValue(_placementBuildingId, out var def))
            return;

        if (!TryGetSnappedPlacementCursor(out Vector3 snappedPos))
            return;

        _placementSnappedPos = snappedPos;

        var result = BuildingPlacementValidator.Validate(
            _gridSystem, _world, playerId: 1, def, snappedPos,
            _buildMapCatalog, _resourceManager, _supplySystem);

        bool inRange = true;
        string rangeReason = string.Empty;
        if (_placementBuilderEntity is Entity builderEntity)
            inRange = IsBuilderInPlacementRange(builderEntity, snappedPos, out rangeReason);

        _placementPreviewReason = result.Reason;
        _placementPreviewInRange = inRange;
        _placementPreviewValid = result.IsValid && inRange;

        string displayName = string.IsNullOrWhiteSpace(def.DisplayName)
            ? _placementBuildingId
            : def.DisplayName;
        string status = PlacementFailureReasonExtensions.BuildStatusMessage(result, inRange, rangeReason);
        bool flash = _placementHintFlashTimer > 0f;
        SetPlacementHint($"{displayName} — {status}", _placementPreviewValid, flash);

        if (_uiManager.Current is GameplayHUD hud)
        {
            if (flash)
                hud.PlacementHintFlash = _placementHintFlashTimer / 0.55f;
            else if (_placementPreviewValid)
                hud.PlacementHintFlash = 0.5f + 0.5f * MathF.Sin(_placementValidPulsePhase);
            else
                hud.PlacementHintFlash = 0f;
        }
    }

    private void RenderPlacementPreview()
    {
        if (_placementBuildingId == null || _gridSystem == null || _groundQuadVertCount == 0) return;
        if (!_definitions.TryGetValue(_placementBuildingId, out var def)) return;
        if (_placementSnappedPos is not Vector3 worldPos) return;

        var (cols, rows) = BuildingFootprint.GetSize(def.Components?.Building?.Footprint);
        float pulse = _placementPreviewValid
            ? 0.78f + 0.22f * MathF.Sin(_placementValidPulsePhase)
            : 1f;
        Vector4 fillColor = _placementPreviewValid
            ? new Vector4(0.2f, 0.9f, 0.35f, 0.45f * pulse)
            : new Vector4(0.95f, 0.2f, 0.2f, 0.5f);
        Vector4 edgeColor = _placementPreviewValid
            ? new Vector4(0.1f, 0.95f, 0.4f, 0.75f * pulse)
            : ResolvePlacementCategoryMarker(_placementPreviewReason, _placementPreviewInRange);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_groundQuadVao);

        foreach (var (x, y) in BuildingFootprint.EnumerateCells(_gridSystem, worldPos, cols, rows))
        {
            Vector3 center = _gridSystem.GridToWorld(x, y);
            var model = Matrix4.CreateScale(GridCellSize, 1f, GridCellSize) *
                        Matrix4.CreateTranslation(center.X, 0.18f, center.Z);
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, fillColor);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _groundQuadVertCount);

            float edgeThickness = 0.22f;
            float edgeHeight = 0.24f;
            float half = GridCellSize * 0.5f;
            DrawPlacementEdge(center, half, edgeThickness, edgeHeight, 0f, -half, edgeColor);
            DrawPlacementEdge(center, half, edgeThickness, edgeHeight, 0f, half, edgeColor);
            DrawPlacementEdge(center, half, edgeThickness, edgeHeight, -half, 0f, edgeColor);
            DrawPlacementEdge(center, half, edgeThickness, edgeHeight, half, 0f, edgeColor);
        }

        if (!_placementPreviewValid)
            RenderPlacementCornerMarkers(worldPos, cols, rows, edgeColor);

        GL.Disable(EnableCap.Blend);
    }

    private void DrawPlacementEdge(
        Vector3 center, float half, float thickness, float height, float offsetX, float offsetZ, Vector4 color)
    {
        bool horizontal = MathF.Abs(offsetZ) > MathF.Abs(offsetX);
        float scaleX = horizontal ? GridCellSize : thickness;
        float scaleZ = horizontal ? thickness : GridCellSize;
        var model = Matrix4.CreateScale(scaleX, 1f, scaleZ) *
                    Matrix4.CreateTranslation(center.X + offsetX, height, center.Z + offsetZ);
        GL.UniformMatrix4(_uniformModel, false, ref model);
        GL.Uniform4(_uniformColor, color);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _groundQuadVertCount);
    }

    private void RenderPlacementCornerMarkers(Vector3 worldPos, int cols, int rows, Vector4 color)
    {
        if (!BuildingFootprint.TryGetOrigin(_gridSystem!, worldPos, cols, rows, out int originX, out int originY))
            return;

        int maxX = originX + cols - 1;
        int maxY = originY + rows - 1;
        var corners = new[]
        {
            _gridSystem!.GridToWorld(originX, originY),
            _gridSystem.GridToWorld(maxX, originY),
            _gridSystem.GridToWorld(originX, maxY),
            _gridSystem.GridToWorld(maxX, maxY),
        };

        float markerSize = GridCellSize * 0.22f;
        foreach (Vector3 corner in corners)
        {
            var model = Matrix4.CreateScale(markerSize, 1f, markerSize) *
                        Matrix4.CreateTranslation(corner.X, 0.3f, corner.Z);
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, color);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _groundQuadVertCount);
        }
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