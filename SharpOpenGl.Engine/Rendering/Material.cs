using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Visual material properties for a mesh.
/// Applied via shader uniforms and procedural race surface textures.
/// </summary>
public sealed class Material
{
    /// <summary>Surface albedo colour (RGB, channels 0–1).</summary>
    public Vector3 DiffuseColor { get; set; } = Vector3.One;

    /// <summary>Self-illumination colour (RGB). Additive on top of diffuse.</summary>
    public Vector3 EmissiveColor { get; set; } = Vector3.Zero;

    /// <summary>Opacity: 1 = fully opaque, 0 = fully transparent.</summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>Convenience factory for an opaque solid colour.</summary>
    public static Material Solid(Vector3 diffuse) =>
        new() { DiffuseColor = diffuse };

    /// <summary>Convenience factory for a glowing (emissive) material.</summary>
    public static Material Emissive(Vector3 diffuse, Vector3 emissive) =>
        new() { DiffuseColor = diffuse, EmissiveColor = emissive };

    /// <summary>Convenience factory for a translucent material.</summary>
    public static Material Translucent(Vector3 diffuse, float opacity) =>
        new() { DiffuseColor = diffuse, Opacity = opacity };
}
