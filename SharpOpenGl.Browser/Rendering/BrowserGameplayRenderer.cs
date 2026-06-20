using OpenTK.Mathematics;
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

    public void Render(World world, int viewportWidth, int viewportHeight)
    {
        float aspect = viewportWidth / (float)Math.Max(1, viewportHeight);
        Matrix4 projection = _camera.GetProjectionMatrix(aspect);
        Matrix4 view = _camera.GetViewMatrix();

        _renderer.Clear(0f, 0f, 0.05f);
        _renderer.BeginFrame(projection, view);

        float halfGrid = BrowserMeshLibrary.MapWorldSize * 0.5f;
        Matrix4 gridModel = Matrix4.CreateTranslation(-halfGrid, -0.5f, -halfGrid);
        _renderer.DrawMesh(_meshes.Grid, _meshes.GridCount, gridModel, Vector4.Zero, GlPrimitive.Lines);

        foreach (var (entity, render) in world.Query<RenderComponent>())
        {
            if (!render.Visible) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

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

            Matrix4 model = transform.GetModelMatrix();
            bool useOverride = world.HasComponent<ResourceNodeComponent>(entity) ||
                               world.HasComponent<AIControlledComponent>(entity);
            _renderer.DrawMesh(render.MeshId, render.VertexCount, model,
                useOverride ? render.Color : Vector4.Zero, render.PrimitiveType);
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

        _renderer.EndFrame();
    }
}