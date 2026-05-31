using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Attaches a <see cref="Rendering.Material"/> to an entity.
/// Rendering systems read this component to determine surface appearance.
/// </summary>
public sealed class MaterialComponent
{
    /// <summary>Surface material (diffuse, emissive, opacity).</summary>
    public Material Material { get; set; } = new Material();
}
