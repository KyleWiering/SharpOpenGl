using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Draws every live entity that has both a <see cref="TransformComponent"/>
/// and a visible <see cref="RenderComponent"/>.
/// The system itself does not own the renderer; it is injected at construction.
/// </summary>
public sealed class RenderSystem : GameSystem
{
    private readonly IRenderer _renderer;

    /// <summary>
    /// Projection matrix used for the current frame.
    /// Set this before <see cref="World.Update"/> is called.
    /// </summary>
    public Matrix4 Projection { get; set; } = Matrix4.Identity;

    /// <summary>
    /// View (camera) matrix used for the current frame.
    /// Set this before <see cref="World.Update"/> is called.
    /// </summary>
    public Matrix4 View { get; set; } = Matrix4.Identity;

    /// <param name="renderer">Platform renderer (OpenGL desktop or WebGL2 browser).</param>
    public RenderSystem(IRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _renderer.BeginFrame(Projection, View);

        foreach (var (entity, render) in world.Query<RenderComponent>())
        {
            if (!render.Visible) continue;

            TransformComponent? transform = world.GetComponent<TransformComponent>(entity);
            Matrix4 model = transform?.GetModelMatrix() ?? Matrix4.Identity;

            _renderer.DrawMesh(render.MeshId, render.VertexCount, model, render.Color, render.PrimitiveType,
                render.RaceTextureIndex, render.TeamTint);
        }

        _renderer.EndFrame();
    }
}
