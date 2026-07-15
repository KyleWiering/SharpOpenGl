using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Shared static nebula veil styling for world fog quads and minimap fog tiles.
/// Fog-of-war is a stationary veil — not animated particle emitters.
/// </summary>
public static class FogNebulaStyle
{
    public readonly record struct VeilLayer(
        Vector3 OffsetFactor,
        Vector2 SizeFactor,
        Vector3 Rgb,
        float Alpha);

    /// <summary>Layers for an unexplored fog cell (dense nebula).</summary>
    public static IReadOnlyList<VeilLayer> UnexploredLayers(int col, int row) =>
    [
        new(new Vector3(0.04f, 0f, 0.03f), new Vector2(0.92f, 0.92f),
            FogVisualPalette.UnexploredCore, FogVisualPalette.UnexploredAlpha),
        new(new Vector3(-0.03f, 0f, -0.02f), new Vector2(1.04f, 1.04f),
            FogVisualPalette.UnexploredVeil, FogVisualPalette.UnexploredAlpha * 0.75f),
        new(new Vector3(0.35f, 0f, 0.1f), new Vector2(0.55f, 0.45f),
            FogVisualPalette.UnexploredCore, FogVisualPalette.UnexploredAlpha * 0.55f),
    ];

    /// <summary>Layers for an explored memory-fog cell (lighter silhouette).</summary>
    public static IReadOnlyList<VeilLayer> ExploredLayers(int col, int row) =>
    [
        new(new Vector3(0.05f, 0f, 0.05f), new Vector2(0.88f, 0.88f),
            FogVisualPalette.ExploredCore, FogVisualPalette.ExploredAlpha),
        new(new Vector3(0.025f, 0f, 0.015f), new Vector2(0.96f, 0.96f),
            FogVisualPalette.ExploredVeil, FogVisualPalette.ExploredAlpha * 0.65f),
    ];

    public static Vector4 LayerColor(Vector3 rgb, float alpha, int col, int row, int layerIndex)
    {
        float noiseR = CellNoiseOffset(col, row, layerIndex, channel: 0);
        float noiseG = CellNoiseOffset(col, row, layerIndex, channel: 1);
        float noiseB = CellNoiseOffset(col, row, layerIndex, channel: 2);
        return new Vector4(
            Math.Clamp(rgb.X + noiseR, 0f, 1f),
            Math.Clamp(rgb.Y + noiseG, 0f, 1f),
            Math.Clamp(rgb.Z + noiseB, 0f, 1f),
            alpha);
    }

    /// <summary>Sample fog state for a minimap display tile from underlying grid cells.</summary>
    public static FogState SampleTileState(
        FogOfWar fog,
        int playerId,
        int startX,
        int startY,
        int sampleW,
        int sampleH,
        int gridWidth,
        int gridHeight)
    {
        bool anyVisible = false;
        bool anyExplored = false;

        int endX = Math.Min(gridWidth, startX + sampleW);
        int endY = Math.Min(gridHeight, startY + sampleH);

        for (int y = startY; y < endY; y++)
        for (int x = startX; x < endX; x++)
        {
            FogState state = fog.GetState(playerId, x, y);
            if (state == FogState.Visible) anyVisible = true;
            else if (state == FogState.Explored) anyExplored = true;
        }

        if (anyVisible) return FogState.Visible;
        if (anyExplored) return FogState.Explored;
        return FogState.Unexplored;
    }

    private static float CellNoiseOffset(int col, int row, int layer, int channel)
    {
        uint hash = unchecked((uint)(
            col * 73856093
            ^ row * 19349663
            ^ layer * 83492791
            ^ channel * 50331653));
        float normalized = (hash % 1000) / 1000f;
        return normalized * 0.08f - 0.04f;
    }
}