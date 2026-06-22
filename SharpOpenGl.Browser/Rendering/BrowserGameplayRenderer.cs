using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Browser.Rendering;

/// <summary>
/// Mirrors desktop <c>EngineWindow.RenderGameplay</c> using the shared <see cref="IRenderer"/> pipeline.
/// </summary>
public sealed class BrowserGameplayRenderer
{
    private readonly WebGlRenderer _renderer;
    private readonly BrowserMeshLibrary _meshes;
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

    public void Render(World world, int viewportWidth, int viewportHeight, ExplosionVfxController? explosionVfx = null, float deltaTime = 0.016f)
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

        RenderTeamAuras(world);

        foreach (var (entity, render) in world.Query<RenderComponent>())
        {
            if (!render.Visible) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            if (render.MeshId < 0 && !string.IsNullOrEmpty(render.MeshKey)
                && _meshes.TryResolveProjectileMesh(render.MeshKey, out int meshId, out int vertCount))
            {
                render.MeshId = meshId;
                render.VertexCount = vertCount;
            }

            if (render.MeshId < 0) continue;

            bool isProjectile = world.HasComponent<ProjectileComponent>(entity);

            if (!isProjectile)
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
                        Vector4.Zero, GlPrimitive.Triangles);
                }
            }

            var projectile = world.GetComponent<ProjectileComponent>(entity);
            var visual = world.GetComponent<ProjectileVisualComponent>(entity);
            Matrix4 model = isProjectile
                ? BuildProjectileModel(transform, projectile, visual)
                : transform.GetModelMatrix();

            Vector4 color = render.RaceTextureIndex >= 0
                ? Vector4.Zero
                : isProjectile
                    ? render.Color
                    : world.HasComponent<ResourceNodeComponent>(entity) || world.HasComponent<AIControlledComponent>(entity)
                        ? render.Color
                        : Vector4.Zero;

            _renderer.DrawMesh(render.MeshId, render.VertexCount, model, color, render.PrimitiveType, render.RaceTextureIndex, render.TeamTint);
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