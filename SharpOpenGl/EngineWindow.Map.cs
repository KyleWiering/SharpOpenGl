using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private GridSystem? _gridSystem;
    private FogOfWar? _fogOfWar;
    private AutoMoveSystem? _autoMoveSystem;
    private int _fogQuadVao, _fogQuadVbo, _fogQuadVertCount;
    private readonly List<Vector3> _objectiveWaypoints = new();

    private void InitializeMapSystems()
    {
        if (_world == null || _movementSystem == null) return;

        float halfMap = MapWorldSize * 0.5f;
        _gridSystem = new GridSystem(GridColumns, GridRows, GridCellSize, new Vector2(-halfMap, -halfMap));
        _fogOfWar = new FogOfWar(_gridSystem, playerCount: 2);

        _autoMoveSystem = new AutoMoveSystem(_eventBus);
        _squadSystem = new SquadSystem();
        var pathFollowing = new PathFollowingSystem(_gridSystem);
        var fogSystem = new FogOfWarSystem(_gridSystem, _fogOfWar, playerId: 0);

        _world.AddSystem(_squadSystem);
        _world.AddSystem(_autoMoveSystem);
        _world.AddSystem(pathFollowing);
        _world.AddSystem(new AttackChaseSystem());
        _world.AddSystem(_movementSystem);
        _world.AddSystem(fogSystem);

        if (_fogQuadVertCount == 0)
        {
            (_fogQuadVao, _fogQuadVbo, _fogQuadVertCount) =
                MeshBuilder.BuildGroundQuad(1f, 1f, new Vector3(1f, 1f, 1f));
        }

        RefreshObjectiveWaypoints();
    }

    private void RevealAreaAt(Vector3 worldPosition, int radius)
    {
        if (_fogOfWar == null || _gridSystem == null) return;
        if (_gridSystem.WorldToGrid(worldPosition, out int gx, out int gy))
            _fogOfWar.Reveal(0, gx, gy, radius);
    }

    private void RefreshObjectiveWaypoints()
    {
        _objectiveWaypoints.Clear();
        if (_missionController?.CurrentMission == null) return;

        foreach (var obj in _missionController.CurrentMission.PrimaryObjectives)
        {
            if (obj.IsCompleted) continue;
            if (obj.Definition.Type != "reach_area") continue;
            if (!MapCoordinates.TryParseReachArea(obj.Definition.Condition, out Vector3 center, out _))
                continue;
            _objectiveWaypoints.Add(center);
        }
    }

    private Vector2 WorldToNormalized(Vector3 worldPos)
    {
        float half = MapWorldSize * 0.5f;
        return new Vector2(
            (worldPos.X + half) / MapWorldSize,
            (worldPos.Z + half) / MapWorldSize);
    }

    private FogState GetFogStateAt(Vector3 worldPos)
    {
        if (_fogOfWar == null || _gridSystem == null) return FogState.Visible;
        if (!_gridSystem.WorldToGrid(worldPos, out int gx, out int gy)) return FogState.Unexplored;
        return _fogOfWar.GetState(0, gx, gy);
    }

    private bool IsVisibleToPlayer(Vector3 worldPos) =>
        GetFogStateAt(worldPos) == FogState.Visible;

    private bool IsExploredByPlayer(Vector3 worldPos) =>
        GetFogStateAt(worldPos) != FogState.Unexplored;

    private bool ShouldRenderEntity(Entity entity, Vector3 worldPos)
    {
        if (_world == null) return true;

        if (_world.HasComponent<AIControlledComponent>(entity) ||
            (_world.HasComponent<ProjectileComponent>(entity) &&
             !_world.HasComponent<SelectionComponent>(entity)))
            return IsVisibleToPlayer(worldPos);

        if (_world.HasComponent<ResourceNodeComponent>(entity) ||
            _world.HasComponent<MapFeatureComponent>(entity))
            return IsExploredByPlayer(worldPos);

        return true;
    }

    private void PanCameraToMinimap(Vector2 normalizedPosition)
    {
        float half = MapWorldSize * 0.5f;
        _rtsCamera.Target = new Vector3(
            normalizedPosition.X * MapWorldSize - half,
            0f,
            normalizedPosition.Y * MapWorldSize - half);
    }

    private bool TryHandleMinimapClick(Vector2 physicalPoint, Vector2 viewportSize)
    {
        if (_uiManager.Current is not GameplayHUD hud) return false;

        _uiManager.Resize(viewportSize);
        Vector2 logical = _uiManager.Scaler.UnscalePosition(physicalPoint);
        if (!hud.Minimap.TryGetClick(logical, Vector2.Zero, UIScaler.ReferenceSize, out Vector2 norm))
            return false;

        PanCameraToMinimap(norm);
        return true;
    }

    private void BindMinimap()
    {
        if (_world == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        float half = MapWorldSize * 0.5f;
        var units = new List<MinimapUnit>();

        foreach (var (entity, _) in _world.Query<TransformComponent>())
        {
            if (!_world.HasComponent<RenderComponent>(entity)) continue;
            var tf = _world.GetComponent<TransformComponent>(entity)!;
            var render = _world.GetComponent<RenderComponent>(entity);
            if (render == null || !render.Visible) continue;

            bool isEnemy = _world.HasComponent<AIControlledComponent>(entity);
            if (isEnemy && !IsVisibleToPlayer(tf.Position)) continue;

            var kind = GameplayEntityDisplay.Classify(_world, entity);
            Vector4 color = kind == EntityDisplayKind.Friendly
                ? GameplayEntityDisplay.FriendlyColor
                : GameplayEntityDisplay.LabelColor(kind);
            bool isFriendly = kind == EntityDisplayKind.Friendly;

            units.Add(new MinimapUnit(WorldToNormalized(tf.Position), color, isFriendly));
        }

        var markers = new List<MinimapMarker>();
        foreach (Vector3 waypoint in _objectiveWaypoints)
        {
            if (!IsExploredByPlayer(waypoint)) continue;
            markers.Add(new MinimapMarker(
                WorldToNormalized(waypoint),
                new Vector4(1f, 0.75f, 0.15f, 1f)));
        }

        float viewW = _rtsCamera.Height * 1.4f;
        float viewH = viewW * Size.X / MathF.Max(1f, Size.Y);
        var camMin = new Vector2(
            (_rtsCamera.Target.X - viewW * 0.5f + half) / MapWorldSize,
            (_rtsCamera.Target.Z - viewH * 0.5f + half) / MapWorldSize);
        var camMax = new Vector2(
            (_rtsCamera.Target.X + viewW * 0.5f + half) / MapWorldSize,
            (_rtsCamera.Target.Z + viewH * 0.5f + half) / MapWorldSize);
        camMin = Vector2.Clamp(camMin, Vector2.Zero, Vector2.One);
        camMax = Vector2.Clamp(camMax, Vector2.Zero, Vector2.One);

        hud.Minimap.FogOfWar = _fogOfWar;
        hud.Minimap.PlayerId = 0;
        hud.Minimap.GridSize = new Vector2i(GridColumns, GridRows);
        hud.Minimap.FogDisplayCells = new Vector2i(50, 50);
        hud.Minimap.Units = units;
        hud.Minimap.ObjectiveMarkers = markers;
        hud.Minimap.CameraViewport = (camMin, camMax);
    }

    private void RenderFogOverlay(Matrix4 projection, Matrix4 view)
    {
        if (_fogOfWar == null || _gridSystem == null || _fogQuadVertCount == 0) return;

        const int chunkCells = 10;
        float chunkWorld = chunkCells * GridCellSize;
        int chunksX = GridColumns / chunkCells;
        int chunksY = GridRows / chunkCells;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_fogQuadVao);

        for (int cx = 0; cx < chunksX; cx++)
        for (int cy = 0; cy < chunksY; cy++)
        {
            bool anyVisible = false;
            bool anyExplored = false;
            for (int dx = 0; dx < chunkCells; dx++)
            for (int dy = 0; dy < chunkCells; dy++)
            {
                var state = _fogOfWar.GetState(0, cx * chunkCells + dx, cy * chunkCells + dy);
                if (state == FogState.Visible) anyVisible = true;
                if (state != FogState.Unexplored) anyExplored = true;
            }

            if (anyVisible) continue;

            Vector4 color = anyExplored
                ? new Vector4(0.05f, 0.06f, 0.12f, 0.72f)
                : new Vector4(0.01f, 0.01f, 0.03f, 0.9f);

            int midX = cx * chunkCells + chunkCells / 2;
            int midY = cy * chunkCells + chunkCells / 2;
            Vector3 center = _gridSystem.GridToWorld(midX, midY);

            var model = Matrix4.CreateScale(chunkWorld, 1f, chunkWorld) *
                        Matrix4.CreateTranslation(center.X, 0.12f, center.Z);
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, color);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _fogQuadVertCount);
        }

        GL.Disable(EnableCap.Blend);
    }

    private void RenderRoutePreviews()
    {
        if (_world == null) return;

        if (_routePreviewVbo == 0)
            (_routePreviewVao, _routePreviewVbo, _) = MeshBuilder.CreateDynamicLineStrip();

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_routePreviewVao);
        var identity = Matrix4.Identity;
        GL.UniformMatrix4(_uniformModel, false, ref identity);
        GL.Uniform4(_uniformColor, new Vector4(0.2f, 0.85f, 0.55f, 0.45f));

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var queue = _world.GetComponent<WaypointQueueComponent>(entity);
            if (queue == null || queue.Waypoints.Count == 0) continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            var segments = new List<Vector3> { transform.Position };
            for (int i = queue.CurrentIndex; i < queue.Waypoints.Count; i++)
                segments.Add(queue.Waypoints[i]);

            if (queue.Patrol && queue.Waypoints.Count > 0)
                segments.Add(queue.Waypoints[0]);

            if (segments.Count < 2) continue;

            float[] vertices = MeshBuilder.BuildLineStripVertices(
                segments, new Vector3(0.2f, 0.85f, 0.55f), y: 0.3f);
            int vertCount = MeshBuilder.UpdateDynamicLineStrip(_routePreviewVbo, vertices);
            GL.DrawArrays(PrimitiveType.LineStrip, 0, vertCount);
        }

        GL.Disable(EnableCap.Blend);
    }

    private void RenderObjectiveMarkers(Matrix4 projection, Matrix4 view)
    {
        if (_objectiveWaypoints.Count == 0) return;

        GL.BindVertexArray(_moveTargetVao);
        foreach (Vector3 waypoint in _objectiveWaypoints)
        {
            if (!IsExploredByPlayer(waypoint)) continue;

            float pulse = 0.7f + 0.3f * MathF.Sin(System.Environment.TickCount * 0.004f);
            var model = Matrix4.CreateScale(pulse * 1.2f) *
                        Matrix4.CreateTranslation(waypoint with { Y = 0.5f });
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, new Vector4(1f, 0.82f, 0.15f, 1f));
            GL.DrawArrays(PrimitiveType.Lines, 0, _moveTargetVertCount);

            var ring = Matrix4.CreateScale(18f * pulse) *
                       Matrix4.CreateTranslation(waypoint with { Y = 0.15f });
            GL.UniformMatrix4(_uniformModel, false, ref ring);
            GL.Uniform4(_uniformColor, new Vector4(1f, 0.9f, 0.25f, 0.85f));
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
            GL.BindVertexArray(_moveTargetVao);
        }
    }
}