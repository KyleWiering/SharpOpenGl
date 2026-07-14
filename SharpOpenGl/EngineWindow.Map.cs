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
    private readonly CombatFogGate _combatFogGate = new();
    private AutoMoveSystem? _autoMoveSystem;
    private int _groundQuadVao, _groundQuadVbo, _groundQuadVertCount;
    private FogNebulaOverlay? _fogNebulaOverlay;
    private readonly List<Vector3> _objectiveWaypoints = new();

    private void InitializeMapSystems()
    {
        if (_world == null || _movementSystem == null) return;

        float halfMap = MapWorldSize * 0.5f;
        var gridOrigin = new Vector2(-halfMap, -halfMap);

        if (_sandboxChunkedMode)
        {
            _sandboxChunkGrid = new SandboxChunkGrid(GridCellSize, gridOrigin);
            _sandboxChunkGrid.EnsureChunksAround(
                Vector3.Zero, radiusChunks: _sandboxChunkLoadRadius, _proceduralMapSeed, GridCellSize);
            _gridSystem = _sandboxChunkGrid.Grid;
        }
        else
        {
            _sandboxChunkGrid = null;
            _gridSystem = new GridSystem(GridColumns, GridRows, GridCellSize, gridOrigin);
        }

        _fogOfWar = new FogOfWar(_gridSystem, playerCount: 2);

        BindCombatFogGate(_gridSystem, _fogOfWar);

        _autoMoveSystem = new AutoMoveSystem(_eventBus);
        _squadSystem = new SquadSystem();
        _pathFollowingSystem = new PathFollowingSystem(_gridSystem);
        _fogOfWarSystem = new FogOfWarSystem(_gridSystem, _fogOfWar, playerId: 0);

        _world.AddSystem(_squadSystem);
        _world.AddSystem(_autoMoveSystem);
        _world.AddSystem(_pathFollowingSystem);
        _world.AddSystem(new AttackChaseSystem());
        _world.AddSystem(new HarvestOrbitSystem());
        _world.AddSystem(_movementSystem);
        _world.AddSystem(_fogOfWarSystem);

        if (_groundQuadVertCount == 0)
        {
            (_groundQuadVao, _groundQuadVbo, _groundQuadVertCount) =
                MeshBuilder.BuildGroundQuad(1f, 1f, new Vector3(1f, 1f, 1f));
        }

        _fogNebulaOverlay ??= new FogNebulaOverlay();

        RefreshObjectiveWaypoints();
    }

    /// <summary>Wires grid/fog into combat LOS gate; optional per-player fog index override for multiplayer.</summary>
    private void BindCombatFogGate(GridSystem grid, FogOfWar fog, Func<int, int>? gamePlayerToFogIndex = null)
    {
        _combatFogGate.Grid = grid;
        _combatFogGate.Fog = fog;
        _combatFogGate.GamePlayerToFogIndex = gamePlayerToFogIndex;
    }

    private void RevealAreaAt(Vector3 worldPosition, int radius)
    {
        if (_fogOfWar == null || _gridSystem == null) return;
        if (_gridSystem.WorldToGrid(worldPosition, out int gx, out int gy))
            _fogOfWar.Reveal(0, gx, gy, radius);
    }

    private void SpawnPendingSandboxChunkEconomy()
    {
        if (_sandboxChunkGrid == null) return;
        var pending = _sandboxChunkGrid.GetPendingEconomyChunks().ToList();
        if (pending.Count > 0)
            SpawnSandboxChunkEconomy(pending);
    }

    private void SpawnSandboxChunkEconomy(IReadOnlyList<SandboxChunkGrid.LoadedChunkInfo> chunks)
    {
        if (_world == null || _sandboxChunkGrid == null) return;

        float halfMap = MapWorldSize * 0.5f;
        var gridOrigin = new Vector2(-halfMap, -halfMap);
        var meshes = BuildMapFeatureMeshes();

        foreach (var chunk in chunks)
        {
            // Do not reveal fog when spawning procedural nodes — minimap/world stay dark until units explore.
            MapFeatureSpawner.SpawnChunkEconomy(
                _world,
                chunk.Map,
                chunk.ChunkX,
                chunk.ChunkY,
                GridCellSize,
                gridOrigin,
                meshes,
                revealArea: null);
            _sandboxChunkGrid.MarkEconomySpawned(chunk.ChunkX, chunk.ChunkY);
        }
    }

    private void EnsureSandboxChunksAround(Vector3 center, IReadOnlyList<Vector3>? extraPositions = null)
    {
        if (!_sandboxChunkedMode || _sandboxChunkGrid == null || _world == null)
            return;

        int previousWidth = _gridSystem?.Width ?? 0;
        int previousHeight = _gridSystem?.Height ?? 0;
        var allNewChunks = new List<SandboxChunkGrid.LoadedChunkInfo>();

        void LoadAround(Vector3 pos)
        {
            var chunks = _sandboxChunkGrid!.EnsureChunksAround(
                pos, radiusChunks: _sandboxChunkLoadRadius, _proceduralMapSeed, GridCellSize);
            allNewChunks.AddRange(chunks);
        }

        LoadAround(center);
        if (extraPositions != null)
        {
            foreach (Vector3 pos in extraPositions)
                LoadAround(pos);
        }

        if (allNewChunks.Count > 0)
        {
            SpawnSandboxChunkEconomy(allNewChunks);
            RefreshSandboxGridBindings(previousWidth, previousHeight);
        }
    }

    private void UpdateSandboxChunks()
    {
        if (!_sandboxChunkedMode || _sandboxChunkGrid == null || _world == null)
            return;

        int previousWidth = _gridSystem?.Width ?? 0;
        int previousHeight = _gridSystem?.Height ?? 0;

        var newChunks = _sandboxChunkGrid.EnsureChunksAround(
            _rtsCamera.Target, radiusChunks: _sandboxChunkLoadRadius, _proceduralMapSeed, GridCellSize);

        if (newChunks.Count > 0)
        {
            SpawnSandboxChunkEconomy(newChunks);
            RefreshSandboxGridBindings(previousWidth, previousHeight);
        }
    }

    private void RefreshSandboxGridBindings(int previousWidth, int previousHeight)
    {
        if (_sandboxChunkGrid == null || _world == null || _movementSystem == null)
            return;

        GridSystem? oldGrid = _gridSystem;
        FogOfWar? oldFog = _fogOfWar;

        _gridSystem = _sandboxChunkGrid.Grid;
        _fogOfWar = new FogOfWar(_gridSystem, playerCount: 2);

        if (oldGrid != null && oldFog != null)
            MigrateFogState(oldGrid, oldFog, _gridSystem, _fogOfWar);

        BindCombatFogGate(_gridSystem, _fogOfWar);

        if (_pathFollowingSystem != null)
            _world.RemoveSystem(_pathFollowingSystem);
        _pathFollowingSystem = new PathFollowingSystem(_gridSystem);
        _world.AddSystem(_pathFollowingSystem);

        if (_fogOfWarSystem != null)
            _world.RemoveSystem(_fogOfWarSystem);
        _fogOfWarSystem = new FogOfWarSystem(_gridSystem, _fogOfWar, playerId: 0);
        _world.AddSystem(_fogOfWarSystem);

        if (previousWidth != _gridSystem.Width || previousHeight != _gridSystem.Height)
            _gridLineStep = -1;
    }

    private static void MigrateFogState(
        GridSystem oldGrid,
        FogOfWar oldFog,
        GridSystem newGrid,
        FogOfWar newFog)
    {
        const int playerId = 0;
        foreach (GridCell oldCell in oldGrid.AllCells())
        {
            FogState state = oldFog.GetState(playerId, oldCell.X, oldCell.Y);
            if (state == FogState.Unexplored)
                continue;

            Vector3 world = oldGrid.GridToWorld(oldCell.X, oldCell.Y);
            if (!newGrid.WorldToGrid(world, out int nx, out int ny))
                continue;

            newFog.Reveal(playerId, nx, ny, radius: 0);
            GridCell? newCell = newGrid.GetCell(nx, ny);
            if (state == FogState.Explored && newCell != null)
                newCell.SetFog(playerId, FogState.Explored);
        }
    }

    private void ClampCameraTarget()
    {
        if (_sandboxChunkedMode && _sandboxChunkGrid != null)
        {
            var (minX, maxX, minZ, maxZ) = _sandboxChunkGrid.LoadedWorldBounds;
            float margin = GridCellSize * 2f;
            _rtsCamera.Target = new Vector3(
                Math.Clamp(_rtsCamera.Target.X, minX + margin, maxX - margin),
                0f,
                Math.Clamp(_rtsCamera.Target.Z, minZ + margin, maxZ - margin));
            return;
        }

        float half = MapWorldSize * 0.5f - GridCellSize;
        _rtsCamera.Target = new Vector3(
            Math.Clamp(_rtsCamera.Target.X, -half, half),
            0f,
            Math.Clamp(_rtsCamera.Target.Z, -half, half));
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
        if (_sandboxChunkedMode && _sandboxChunkGrid != null)
            return _sandboxChunkGrid.WorldToNormalized(worldPos);

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
        if (_sandboxChunkedMode && _sandboxChunkGrid != null)
        {
            var (minX, maxX, minZ, maxZ) = _sandboxChunkGrid.LoadedWorldBounds;
            _rtsCamera.Target = new Vector3(
                minX + normalizedPosition.X * (maxX - minX),
                0f,
                minZ + normalizedPosition.Y * (maxZ - minZ));
            return;
        }

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

    private int ResolveMinimapPlayerId(Entity entity)
    {
        if (_world == null) return 1;

        if (_world.HasComponent<AIControlledComponent>(entity))
            return _world.GetComponent<AIControlledComponent>(entity)!.PlayerId;

        if (_world.HasComponent<CombatTargetComponent>(entity))
        {
            int faction = _world.GetComponent<CombatTargetComponent>(entity)!.Faction;
            if (faction > 0) return faction;
        }

        if (_world.HasComponent<ResourceCollectorComponent>(entity))
            return Math.Max(1, _world.GetComponent<ResourceCollectorComponent>(entity)!.PlayerId);

        if (_world.HasComponent<BuildingComponent>(entity))
            return Math.Max(1, _world.GetComponent<BuildingComponent>(entity)!.PlayerId);

        return 1;
    }

    private void BindMinimap()
    {
        if (_world == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        float half = MapWorldSize * 0.5f;
        Vector2i minimapGridSize = _sandboxChunkedMode && _sandboxChunkGrid != null
            ? new Vector2i(
                _sandboxChunkGrid.Grid.Width,
                _sandboxChunkGrid.Grid.Height)
            : new Vector2i(GridColumns, GridRows);
        var units = new List<MinimapUnit>();

        foreach (var (entity, _) in _world.Query<TransformComponent>())
        {
            if (!_world.HasComponent<RenderComponent>(entity)) continue;
            if (_world.HasComponent<ResourceNodeComponent>(entity)) continue;
            if (_world.HasComponent<MapFeatureComponent>(entity)) continue;

            var tf = _world.GetComponent<TransformComponent>(entity)!;
            var render = _world.GetComponent<RenderComponent>(entity);
            if (render == null || !render.Visible) continue;

            bool isEnemy = _world.HasComponent<AIControlledComponent>(entity);
            if (isEnemy)
            {
                if (!IsVisibleToPlayer(tf.Position)) continue;
            }
            else if (!IsExploredByPlayer(tf.Position))
            {
                continue;
            }

            var kind = GameplayEntityDisplay.Classify(_world, entity);
            int playerId = ResolveMinimapPlayerId(entity);
            bool isFriendly = kind == EntityDisplayKind.Friendly;
            Vector4 color = PlayerColorPalette.GetTintVector4(Math.Max(1, playerId));
            if (!isFriendly)
                color.W = 1f;

            units.Add(new MinimapUnit(WorldToNormalized(tf.Position), color, isFriendly, playerId));
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
        Vector2 camMin;
        Vector2 camMax;
        if (_sandboxChunkedMode && _sandboxChunkGrid != null)
        {
            var (minX, maxX, minZ, maxZ) = _sandboxChunkGrid.LoadedWorldBounds;
            float mapW = MathF.Max(1f, maxX - minX);
            float mapH = MathF.Max(1f, maxZ - minZ);
            camMin = new Vector2(
                (_rtsCamera.Target.X - viewW * 0.5f - minX) / mapW,
                (_rtsCamera.Target.Z - viewH * 0.5f - minZ) / mapH);
            camMax = new Vector2(
                (_rtsCamera.Target.X + viewW * 0.5f - minX) / mapW,
                (_rtsCamera.Target.Z + viewH * 0.5f - minZ) / mapH);
        }
        else
        {
            camMin = new Vector2(
                (_rtsCamera.Target.X - viewW * 0.5f + half) / MapWorldSize,
                (_rtsCamera.Target.Z - viewH * 0.5f + half) / MapWorldSize);
            camMax = new Vector2(
                (_rtsCamera.Target.X + viewW * 0.5f + half) / MapWorldSize,
                (_rtsCamera.Target.Z + viewH * 0.5f + half) / MapWorldSize);
        }

        camMin = Vector2.Clamp(camMin, Vector2.Zero, Vector2.One);
        camMax = Vector2.Clamp(camMax, Vector2.Zero, Vector2.One);

        hud.Minimap.FogOfWar = _fogOfWar;
        hud.Minimap.PlayerId = 0;
        hud.Minimap.GridSize = minimapGridSize;
        hud.Minimap.FogDisplayCells = new Vector2i(50, 50);
        hud.Minimap.SyncFogPaletteFromWorld();
        hud.Minimap.Units = units;
        hud.Minimap.ObjectiveMarkers = markers;
        hud.Minimap.FeatureMarkers = MinimapFeatureBinder.Collect(
            _world,
            GetFogStateAt,
            WorldToNormalized);
        hud.Minimap.CameraViewport = (camMin, camMax);
    }

    private void UpdateFogNebulaOverlay(float deltaTime) =>
        _fogNebulaOverlay?.Update(deltaTime);

    private void RenderFogOverlay(Matrix4 projection, Matrix4 view)
    {
        if (_fogOfWar == null || _gridSystem == null || _fogNebulaOverlay == null || _groundQuadVertCount == 0)
            return;

        _ = projection;
        _ = view;

        // Draw order: terrain/entities first, then static nebula fog veils, then markers/UI rings.
        float chunkWorld = FogNebulaOverlay.ChunkCells * GridCellSize;
        Vector4? cameraBounds = TryGetFogCameraBoundsXZ();

        _fogNebulaOverlay.Sync(_fogOfWar, _gridSystem, playerId: 0, chunkWorld, cameraBounds);
        if (_fogNebulaOverlay.VeilQuads.Count == 0)
            return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_groundQuadVao);

        foreach (FogVeilQuad veil in _fogNebulaOverlay.VeilQuads)
        {
            var model = Matrix4.CreateScale(veil.SizeX, 1f, veil.SizeZ) *
                        Matrix4.CreateTranslation(veil.Center.X, veil.Center.Y, veil.Center.Z);
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, veil.Color);
            GL.Uniform1(_uniformRaceTextureIndex, -1);
            GL.Uniform1(_uniformComponentTextureIndex, -1);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _groundQuadVertCount);
        }

        GL.BindVertexArray(0);
        GL.Disable(EnableCap.Blend);
    }

    /// <summary>World-space XZ bounds (minX, maxX, minZ, maxZ) for fog chunk culling on large maps.</summary>
    private Vector4? TryGetFogCameraBoundsXZ()
    {
        float viewW = _rtsCamera.Height * 1.4f;
        float viewH = viewW * Size.X / MathF.Max(1f, Size.Y);
        float margin = GridCellSize * FogNebulaOverlay.ChunkCells;

        if (_sandboxChunkedMode && _sandboxChunkGrid != null)
        {
            var (minX, maxX, minZ, maxZ) = _sandboxChunkGrid.LoadedWorldBounds;
            return new Vector4(
                MathF.Max(minX, _rtsCamera.Target.X - viewW * 0.5f - margin),
                MathF.Min(maxX, _rtsCamera.Target.X + viewW * 0.5f + margin),
                MathF.Max(minZ, _rtsCamera.Target.Z - viewH * 0.5f - margin),
                MathF.Min(maxZ, _rtsCamera.Target.Z + viewH * 0.5f + margin));
        }

        return new Vector4(
            _rtsCamera.Target.X - viewW * 0.5f - margin,
            _rtsCamera.Target.X + viewW * 0.5f + margin,
            _rtsCamera.Target.Z - viewH * 0.5f - margin,
            _rtsCamera.Target.Z + viewH * 0.5f + margin);
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
        GL.Uniform4(_uniformColor, RoutePreviewHelper.DefaultLineColorRgba);

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var queue = _world.GetComponent<WaypointQueueComponent>(entity);
            if (queue == null) continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            var segments = RoutePreviewHelper.BuildSegments(transform.Position, queue);
            if (segments == null) continue;

            float[] vertices = MeshBuilder.BuildLineStripVertices(
                segments, RoutePreviewHelper.DefaultLineColor, y: RoutePreviewHelper.DefaultLineY);
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