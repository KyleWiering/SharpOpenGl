using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Each frame, updates each entity's <see cref="RenderComponent.MeshId"/> and
/// <see cref="RenderComponent.VertexCount"/> to the LOD mesh level that matches
/// the entity's distance from the camera.
/// Entities without <see cref="MeshLodComponent"/> are unaffected.
/// </summary>
public sealed class MeshLodSystem : GameSystem
{
    /// <summary>Camera world-space position used to compute entity distances.</summary>
    public Vector3 CameraPosition { get; set; } = Vector3.Zero;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, lodComp) in world.Query<MeshLodComponent>())
        {
            RenderComponent? render = world.GetComponent<RenderComponent>(entity);
            if (render == null) continue;

            TransformComponent? transform = world.GetComponent<TransformComponent>(entity);
            Vector3 entityPos = transform?.Position ?? Vector3.Zero;

            float dist = Vector3.Distance(entityPos, CameraPosition);
            var (vao, count) = lodComp.Lod.Select(dist);

            render.MeshId      = vao;
            render.VertexCount = count;
        }
    }
}
