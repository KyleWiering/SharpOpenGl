using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Rendering;
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
        var pathFollowing = new PathFollowingSystem(_gridSystem);
        var fogSystem = new FogOfWarSystem(_gridSystem, _fogOfWar, playerId: 0);

        _world.AddSystem(_autoMoveSystem);
        _world.AddSystem(pathFollowing);
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

    private bool IsVisibleToPlayer(Vector3 worldPos)
    {
        if (_fogOfWar == null || _gridSystem == null) return true;
        if (!_gridSystem.WorldToGrid(worldPos, out int gx, out int gy)) return false;
        return _fogOfWar.GetState(0, gx, gy) == FogState.Visible;
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

            bool isFriendly = !isEnemy;
            Vector4 color = isFriendly
                ? new Vector4(0.35f, 0.9f, 0.45f, 1f)
                : new Vector4(0.95f, 0.3f, 0.3f, 1f);

            if (_world.HasComponent<ResourceNodeComponent>(entity))
                color = new Vector4(0.95f, 0.85f, 0.2f, 1f);
            else if (_world.HasComponent<BuildingComponent>(entity))
                color = new Vector4(0.45f, 0.65f, 0.95f, 1f);

            units.Add(new MinimapUnit(WorldToNormalized(tf.Position), color, isFriendly));
        }

        var markers = new List<MinimapMarker>();
        foreach (Vector3 waypoint in _objectiveWaypoints)
        {
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

    private void RenderObjectiveMarkers(Matrix4 projection, Matrix4 view)
    {
        if (_objectiveWaypoints.Count == 0) return;

        GL.BindVertexArray(_moveTargetVao);
        foreach (Vector3 waypoint in _objectiveWaypoints)
        {
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