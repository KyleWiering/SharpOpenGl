namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Chooses how many grid cells to skip between rendered lines based on camera height.
/// </summary>
public static class GridRenderLod
{
    /// <summary>
    /// Returns a line interval in cells (1 = every cell, 2 = every other, etc.).
    /// </summary>
    public static int ResolveLineStep(float cameraHeight, float minHeight, float maxHeight)
    {
        if (maxHeight <= minHeight) return 1;

        float span = maxHeight - minHeight;
        float low = minHeight + span * 0.1f;
        float mid = minHeight + span * 0.25f;
        float high = minHeight + span * 0.45f;
        float veryHigh = minHeight + span * 0.7f;

        if (cameraHeight >= veryHigh) return 20;
        if (cameraHeight >= high) return 10;
        if (cameraHeight >= mid) return 5;
        if (cameraHeight >= low) return 2;
        return 1;
    }
}