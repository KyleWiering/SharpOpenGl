using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Converts logical (reference) coordinates to physical screen pixels,
/// allowing the UI to look identical at any resolution.
/// </summary>
/// <remarks>
/// All widget positions and sizes are authored at the <em>reference resolution</em>
/// (default 1920 × 1080).  At runtime the scaler maps them to the actual
/// viewport so the layout remains proportional.
/// </remarks>
public sealed class UIScaler
{
    /// <summary>The design-time reference resolution (default 1920 × 1080).</summary>
    public static readonly Vector2 ReferenceSize = new(1920f, 1080f);

    private Vector2 _viewportSize;
    private Vector2 _scale;

    /// <param name="viewportSize">Initial physical viewport size in pixels.</param>
    public UIScaler(Vector2 viewportSize)
    {
        _viewportSize = viewportSize;
        RecalculateScale();
    }

    /// <summary>Current physical viewport size.</summary>
    public Vector2 ViewportSize => _viewportSize;

    /// <summary>
    /// Scale factor applied to all logical coordinates.
    /// A value of (1,1) means the viewport exactly matches the reference resolution.
    /// </summary>
    public Vector2 Scale => _scale;

    /// <summary>
    /// The uniform scale factor (minimum of X and Y scale) that keeps the UI
    /// aspect-ratio correct across resolutions.
    /// </summary>
    public float UniformScale => MathF.Min(_scale.X, _scale.Y);

    /// <summary>Update the viewport size (e.g. on window resize).</summary>
    public void Resize(Vector2 newViewportSize)
    {
        _viewportSize = newViewportSize;
        RecalculateScale();
    }

    /// <summary>Convert a logical pixel size to physical pixels.</summary>
    public Vector2 ScaleSize(Vector2 logicalSize) =>
        new(logicalSize.X * _scale.X, logicalSize.Y * _scale.Y);

    /// <summary>Convert a logical pixel position to physical pixels.</summary>
    public Vector2 ScalePosition(Vector2 logicalPosition) =>
        new(logicalPosition.X * _scale.X, logicalPosition.Y * _scale.Y);

    // ─────────────────────────────────────────────────────────────────────────
    private void RecalculateScale()
    {
        _scale = new Vector2(
            _viewportSize.X / ReferenceSize.X,
            _viewportSize.Y / ReferenceSize.Y);
    }
}
