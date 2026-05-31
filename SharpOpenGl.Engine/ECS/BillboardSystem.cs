using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Manages billboard (sprite fallback) visibility for distant entities.
/// For each entity with a <see cref="BillboardComponent"/>, the system:
/// <list type="bullet">
///   <item>Activates the billboard and hides the 3D mesh when beyond the threshold.</item>
///   <item>Deactivates the billboard and restores normal rendering when within range.</item>
/// </list>
/// Actual quad drawing is done by the platform renderer when
/// <see cref="BillboardComponent.IsActive"/> is true.
/// </summary>
public sealed class BillboardSystem : GameSystem
{
    /// <summary>Camera world-space position used to compute entity distances.</summary>
    public Vector3 CameraPosition { get; set; } = Vector3.Zero;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, billboard) in world.Query<BillboardComponent>())
        {
            TransformComponent? transform = world.GetComponent<TransformComponent>(entity);
            Vector3 entityPos = transform?.Position ?? Vector3.Zero;

            float dist = Vector3.Distance(entityPos, CameraPosition);
            bool shouldBeBillboard = dist > billboard.FarThreshold;

            billboard.IsActive = shouldBeBillboard;

            // Hide / show the 3D mesh when the billboard state changes
            RenderComponent? render = world.GetComponent<RenderComponent>(entity);
            if (render != null)
                render.Visible = !shouldBeBillboard;
        }
    }
}
