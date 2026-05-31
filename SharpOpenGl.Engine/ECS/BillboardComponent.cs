using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marks an entity as eligible for billboard (sprite) rendering when viewed from far away.
/// The <see cref="BillboardSystem"/> switches rendering to a flat quad once the entity
/// is beyond <see cref="FarThreshold"/> world units from the camera.
/// </summary>
public sealed class BillboardComponent
{
    /// <summary>Flat colour used for the billboard quad.</summary>
    public Vector4 Color { get; set; } = Vector4.One;

    /// <summary>World-space width of the billboard quad.</summary>
    public float Width { get; set; } = 1f;

    /// <summary>World-space height of the billboard quad.</summary>
    public float Height { get; set; } = 1f;

    /// <summary>
    /// Camera distance (world units) beyond which the billboard is active.
    /// Below this distance the entity renders normally via <see cref="RenderComponent"/>.
    /// </summary>
    public float FarThreshold { get; set; } = 400f;

    /// <summary>
    /// Whether the billboard is currently active (updated by <see cref="BillboardSystem"/>).
    /// </summary>
    public bool IsActive { get; internal set; }
}
