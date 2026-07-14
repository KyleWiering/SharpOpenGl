using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Browser.Rendering;

/// <summary>
/// Mirrors desktop <c>EngineWindow.RenderGameplay</c> using the shared <see cref="IRenderer"/> pipeline.
/// </summary>
public sealed class BrowserGameplayRenderer
{
    private readonly WebGlRenderer _renderer;
    private readonly BrowserMeshLibrary _meshes;
    private readonly BrowserTerrainReadabilityOverlay _terrainOverlay = new();
    private float _auraPulse;
    private readonly RtsCameraController _camera = new()
    {
        Target = Vector3.Zero,
        Height = 200f,
        TiltAngle = 35f,
        PanSpeed = 300f,
        ZoomSpeed = 40f,
        MinHeight = 40f,
        MaxHeight = 1500f,
    };

    public BrowserGameplayRenderer(WebGlRenderer renderer, BrowserMeshLibrary meshes)
    {
        _renderer = renderer;
        _meshes = meshes;
    }

    public RtsCameraController Camera => _camera;

    public void Render(
        World world,
        int viewportWidth,
        int viewportHeight,
        ExplosionVfxController? explosionVfx = null,
        float deltaTime = 0.016f,
        GridSystem? grid = null,
        FogOfWar? fog = null)
    {
        _auraPulse += deltaTime;
        float aspect = viewportWidth / (float)Math.Max(1, viewportHeight);
        Matrix4 projection = _camera.GetProjectionMatrix(aspect);
        Matrix4 view = _camera.GetViewMatrix();

        _renderer.Clear(0f, 0f, 0.05f);
        _renderer.BeginFrame(projection, view);

        float halfGrid = BrowserMeshLibrary.MapWorldSize * 0.5f;
        Matrix4 gridModel = Matrix4.CreateTranslation(-halfGrid, -0.5f, -halfGrid);
        var (gridMesh, gridCount) = _meshes.GetGridForHeight(
            _camera.Height, _camera.MinHeight, _camera.MaxHeight);
        _renderer.DrawMesh(gridMesh, gridCount, gridModel, Vector4.Zero, GlPrimitive.Lines);

        SyncAndRenderTerrainOverlay(grid, fog, viewportWidth, viewportHeight);

        RenderTeamAuras(world);

        foreach (var (entity, render) in world.Query<RenderComponent>())
        {
            if (!render.Visible) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            if (render.MeshId < 0 && !string.IsNullOrEmpty(render.MeshKey))
            {
                if (_meshes.TryResolveProjectileMesh(render.MeshKey, out int meshId, out int vertCount)
                    || _meshes.TryGetArticulatedPart(render.MeshKey, out meshId, out vertCount))
                {
                    render.MeshId = meshId;
                    render.VertexCount = vertCount;
                }
            }

            if (render.MeshId < 0) continue;

            bool isProjectile = world.HasComponent<ProjectileComponent>(entity);
            bool isArticulatedPart = ArticulationDrawHelper.IsArticulatedPartChild(world, entity);

            if (!isProjectile && !isArticulatedPart)
            {
                var movement = world.GetComponent<MovementComponent>(entity);
                if (movement != null && movement.Velocity.LengthSquared > 1f)
                {
                    float speed = movement.Velocity.Length;
                    float trailScale = MathHelper.Clamp(speed / movement.Speed, 0.3f, 1.0f);
                    Matrix4 trailModel = Matrix4.CreateScale(trailScale) *
                                         Matrix4.CreateTranslation(0f, 0f, -0.5f) *
                                         transform.GetModelMatrix();
                    _renderer.DrawMesh(_meshes.EngineTrail, _meshes.EngineTrailCount, trailModel,
                        Vector4.Zero, GlPrimitive.Triangles, componentTextureIndex: ComponentTextureIndex.Engine);
                }
            }

            var projectile = world.GetComponent<ProjectileComponent>(entity);
            var visual = world.GetComponent<ProjectileVisualComponent>(entity);
            Matrix4 model;
            if (!ArticulationDrawHelper.TryGetArticulatedModelMatrix(world, entity, transform, out model))
                model = isProjectile
                    ? BuildProjectileModel(transform, projectile, visual)
                    : transform.GetModelMatrix();

            int raceTex = render.RaceTextureIndex;
            int compTex = -1;
            Vector4 color;
            if (render.RaceTextureIndex >= 0)
            {
                color = Vector4.Zero;
            }
            else if (isProjectile)
            {
                raceTex = -1;
                compTex = ComponentTextureIndex.Weapon;
                color = ResolveProjectileTint(world, entity, projectile, visual);
            }
            else if (render.ComponentTextureIndex >= 0)
            {
                raceTex = -1;
                compTex = render.ComponentTextureIndex;
                color = Vector4.Zero;
            }
            else
            {
                color = world.HasComponent<ResourceNodeComponent>(entity) || world.HasComponent<AIControlledComponent>(entity)
                    ? render.Color
                    : Vector4.Zero;
            }

            _renderer.DrawMesh(render.MeshId, render.VertexCount, model, color, render.PrimitiveType,
                raceTex, render.TeamTint, compTex);
        }

        foreach (var (entity, sel) in world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            Matrix4 ringModel = Matrix4.CreateTranslation(transform.Position);
            _renderer.DrawMesh(_meshes.SelectionRing, _meshes.SelectionRingCount, ringModel,
                new Vector4(0, 1, 0, 1), GlPrimitive.Lines);
        }

        RenderRoutePreviews(world);

        if (explosionVfx != null)
        {
            var (pointCount, vertices) = explosionVfx.BuildVertexData();
            if (pointCount > 0)
            {
                _renderer.DrawPoints(_meshes.ParticleBuffer, vertices, pointCount, Matrix4.Identity, 5f);
            }
        }

        _renderer.EndFrame();
    }

    private void RenderRoutePreviews(World world)
    {
        foreach (var (entity, sel) in world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var queue = world.GetComponent<WaypointQueueComponent>(entity);
            if (queue == null) continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            var segments = RoutePreviewHelper.BuildSegments(transform.Position, queue);
            if (segments == null) continue;

            float[] vertices = ProceduralMeshes.BuildLineStripVertices(
                segments, RoutePreviewHelper.DefaultLineColor, y: RoutePreviewHelper.DefaultLineY);
            int vertCount = ProceduralMeshes.VertexCount(vertices);
            if (vertCount < 2) continue;

            _renderer.DrawLineStrip(
                _meshes.RoutePreviewBuffer,
                vertices,
                vertCount,
                Matrix4.Identity,
                RoutePreviewHelper.DefaultLineColorRgba);
        }
    }

    private void RenderTeamAuras(World world)
    {
        if (_meshes.TeamAuraDiscCount <= 0) return;

        float pulse = 0.78f + 0.22f * MathF.Sin(_auraPulse * 2.4f);
        const float baseRingRadius = 3f;

        foreach (var (entity, render) in world.Query<RenderComponent>())
        {
            if (!render.Visible || render.RaceTextureIndex < 0) continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            int playerId = TeamVisualResolver.ResolvePlayerId(world, entity);
            float selRadius = world.GetComponent<SelectionComponent>(entity)?.SelectionRadius ?? 7f;
            float auraRadius = selRadius * 0.68f;

            Matrix4 discModel = Matrix4.CreateScale(auraRadius) *
                                Matrix4.CreateTranslation(transform.Position with { Y = 0.12f });
            _renderer.DrawMesh(_meshes.TeamAuraDisc, _meshes.TeamAuraDiscCount, discModel,
                PlayerColorPalette.GetAuraColor(playerId, pulse), GlPrimitive.Triangles);

            float outerScale = auraRadius / baseRingRadius * 1.35f;
            Matrix4 outerModel = Matrix4.CreateScale(outerScale) *
                                  Matrix4.CreateTranslation(transform.Position with { Y = 0.16f });
            _renderer.DrawMesh(_meshes.SelectionRing, _meshes.SelectionRingCount, outerModel,
                PlayerColorPalette.GetAuraOuterColor(playerId, pulse), GlPrimitive.Lines);

            float ringScale = auraRadius / baseRingRadius * 1.08f;
            Matrix4 ringModel = Matrix4.CreateScale(ringScale) *
                                Matrix4.CreateTranslation(transform.Position with { Y = 0.2f });
            _renderer.DrawMesh(_meshes.SelectionRing, _meshes.SelectionRingCount, ringModel,
                PlayerColorPalette.GetAuraRingColor(playerId, pulse), GlPrimitive.Lines);
        }
    }

    private void SyncAndRenderTerrainOverlay(GridSystem? grid, FogOfWar? fog, int viewportWidth, int viewportHeight)
    {
        if (grid == null) return;

        float aspect = viewportWidth / (float)Math.Max(1, viewportHeight);
        Vector4 bounds = _camera.GetVisibleBoundsXZ(aspect, margin: 20f);
        _terrainOverlay.Sync(grid, fog, playerId: 1, bounds);
        if (!_terrainOverlay.IsDrawEnabled || _meshes.GroundQuadCount <= 0) return;

        foreach (TerrainReadabilityOverlay.TerrainTintCell cell in _terrainOverlay.Cells)
        {
            Matrix4 model = Matrix4.CreateScale(cell.CellSize, 1f, cell.CellSize) *
                              Matrix4.CreateTranslation(cell.Center.X, 0.14f, cell.Center.Z);
            _renderer.DrawMesh(_meshes.GroundQuad, _meshes.GroundQuadCount, model, cell.Color, GlPrimitive.Triangles);
        }
    }

    private static Vector4 ResolveProjectileTint(
        World world, Entity entity, ProjectileComponent? proj, ProjectileVisualComponent? visual)
    {
        if (proj == null || visual == null)
            return Vector4.Zero;

        if (proj.Type is not (ProjectileType.Homing or ProjectileType.AoE) || proj.MaxLifetime <= 0f)
            return Vector4.Zero;

        float lifetimeRatio = Math.Clamp(proj.Lifetime / proj.MaxLifetime, 0f, 1f);
        Vector4 baseTint = WeaponProfiles.TrailColor(visual.Visual, proj.Type);
        return WeaponProfiles.HomingTrailSteerTint(baseTint, lifetimeRatio, visual.Visual);
    }

    private static Matrix4 BuildProjectileModel(TransformComponent transform, ProjectileComponent? proj,
        ProjectileVisualComponent? visual)
    {
        Vector3 scale = transform.Scale;
        if (proj != null && visual != null && visual.Visual == WeaponVisualKind.Wave && proj.MaxLifetime > 0f)
        {
            float progress = 1f - MathF.Max(0f, proj.Lifetime / proj.MaxLifetime);
            float ring = visual.Scale + progress * proj.BlastRadius * 0.12f;
            scale = new Vector3(ring, 1f, ring);
        }

        Matrix4 s = Matrix4.CreateScale(scale);
        Matrix4 rx = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(transform.EulerAngles.X));
        Matrix4 ry = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(transform.EulerAngles.Y));
        Matrix4 rz = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(transform.EulerAngles.Z));
        Matrix4 t = Matrix4.CreateTranslation(transform.Position);
        return s * ry * rx * rz * t;
    }
}