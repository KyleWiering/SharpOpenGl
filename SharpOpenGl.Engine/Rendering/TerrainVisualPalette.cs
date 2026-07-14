using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Ground tint palette for pathing-relevant terrain on fixed and sandbox maps.
/// Higher movement cost and impassable cells use stronger contrast for readability.
/// </summary>
public static class TerrainVisualPalette
{
    /// <summary>Returns RGBA tint for a terrain cell overlay, or <c>null</c> for open space.</summary>
    public static Vector4? ResolveTint(TerrainType terrain) => terrain switch
    {
        TerrainType.Impassable     => new Vector4(0.82f, 0.12f, 0.14f, 0.62f),
        TerrainType.AsteroidField  => new Vector4(0.62f, 0.42f, 0.24f, 0.40f),
        TerrainType.IonStorm       => new Vector4(0.55f, 0.22f, 0.95f, 0.48f),
        TerrainType.Nebula         => new Vector4(0.42f, 0.18f, 0.72f, 0.34f),
        TerrainType.Debris         => new Vector4(0.58f, 0.50f, 0.38f, 0.36f),
        TerrainType.WormholeRemnant => new Vector4(0.18f, 0.78f, 0.95f, 0.44f),
        TerrainType.Space          => null,
        _                          => null,
    };

    /// <summary>Whether this terrain type affects unit pathing (non-default movement cost or blocked).</summary>
    public static bool AffectsPathing(TerrainType terrain) =>
        terrain != TerrainType.Space;

    /// <summary>Category label for HUD/debug overlays.</summary>
    public static string DescribePathing(TerrainType terrain) => terrain switch
    {
        TerrainType.Impassable      => "Blocked",
        TerrainType.AsteroidField   => "Slow (×3)",
        TerrainType.IonStorm        => "Slow (×2.5)",
        TerrainType.Nebula          => "Slow (×2)",
        TerrainType.Debris          => "Slow (×1.5)",
        TerrainType.WormholeRemnant => "Slow (×2)",
        _                           => "Clear",
    };
}