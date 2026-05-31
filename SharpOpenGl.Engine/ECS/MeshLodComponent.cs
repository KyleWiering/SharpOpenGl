using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Attaches a <see cref="MeshLod"/> set to an entity.
/// The <see cref="MeshLodSystem"/> updates <see cref="RenderComponent.MeshId"/>
/// each frame based on camera distance.
/// </summary>
public sealed class MeshLodComponent
{
    /// <summary>The LOD mesh set for this entity.</summary>
    public MeshLod Lod { get; set; } = new MeshLod();
}
