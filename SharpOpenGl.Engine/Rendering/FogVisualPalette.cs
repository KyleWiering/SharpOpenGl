using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Shared fog-of-war color palette for world nebula particles and minimap fog overlay.
/// </summary>
public static class FogVisualPalette
{
    /// <summary>Deep purple/indigo nebula core for unexplored fog.</summary>
    public static readonly Vector3 UnexploredCore = new(0.35f, 0.08f, 0.55f);

    /// <summary>Dark edge fade for unexplored fog wisps.</summary>
    public static readonly Vector3 UnexploredVeil = new(0.08f, 0.02f, 0.14f);

    /// <summary>Cyan-purple silhouette core for explored fog.</summary>
    public static readonly Vector3 ExploredCore = new(0.42f, 0.22f, 0.72f);

    /// <summary>Soft fade for explored fog silhouettes.</summary>
    public static readonly Vector3 ExploredVeil = new(0.18f, 0.12f, 0.28f);

    /// <summary>Unexplored fog veil alpha (world quads + minimap tiles).</summary>
    public static float UnexploredAlpha => FogNebulaOverlay.Config.UnexploredVeilAlpha;

    /// <summary>Explored memory-fog alpha (world quads + minimap tiles).</summary>
    public static float ExploredAlpha => FogNebulaOverlay.Config.ExploredVeilAlpha;

    /// <summary>Builds an RGBA tint from a palette core/veil color and alpha.</summary>
    public static Vector4 ToColor(Vector3 rgb, float alpha) => new(rgb.X, rgb.Y, rgb.Z, alpha);
}