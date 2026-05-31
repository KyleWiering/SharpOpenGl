using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Describes the physical form-factor / orientation of the display.
/// </summary>
public enum LayoutProfile
{
    /// <summary>Desktop monitor — keyboard + mouse, large viewport.</summary>
    Desktop,

    /// <summary>Tablet in landscape orientation — touch, medium viewport.</summary>
    TabletLandscape,

    /// <summary>Tablet in portrait orientation — touch, medium viewport.</summary>
    TabletPortrait,

    /// <summary>Phone in landscape orientation — touch, small viewport.</summary>
    PhoneLandscape,

    /// <summary>Phone in portrait orientation — touch, small viewport.</summary>
    PhonePortrait,
}

/// <summary>
/// Detects the appropriate <see cref="LayoutProfile"/> from viewport dimensions
/// and applies profile-specific UI layout rules.
/// </summary>
public static class AdaptiveLayout
{
    // Breakpoints (physical pixels)
    private const float PhoneMaxShortEdge   = 500f;
    private const float DesktopMinWidth     = 1280f;
    private const float DesktopMinHeight    = 720f;

    /// <summary>Minimum touch-target size in physical pixels (Apple HIG / Material Design guideline).</summary>
    public const float MinTouchTargetPx = 44f;

    /// <summary>
    /// Classify a viewport into a <see cref="LayoutProfile"/>.
    /// </summary>
    public static LayoutProfile Detect(Vector2 viewportSize)
    {
        float w = viewportSize.X;
        float h = viewportSize.Y;
        bool landscape = w >= h;
        float shortEdge = MathF.Min(w, h);

        if (w >= DesktopMinWidth && h >= DesktopMinHeight)
            return LayoutProfile.Desktop;

        if (shortEdge < PhoneMaxShortEdge)
            return landscape ? LayoutProfile.PhoneLandscape : LayoutProfile.PhonePortrait;

        return landscape ? LayoutProfile.TabletLandscape : LayoutProfile.TabletPortrait;
    }

    /// <summary>
    /// Returns <c>true</c> when the profile represents a touch-first device.
    /// </summary>
    public static bool IsTouchDevice(LayoutProfile profile) =>
        profile != LayoutProfile.Desktop;

    /// <summary>
    /// Returns <c>true</c> when edge-of-screen camera scroll should be disabled
    /// (replaced by two-finger drag on touch devices).
    /// </summary>
    public static bool DisableEdgeScroll(LayoutProfile profile) =>
        IsTouchDevice(profile);

    /// <summary>
    /// Recommended HUD button size for the given profile (logical pixels at 1920×1080 reference).
    /// </summary>
    public static Vector2 RecommendedButtonSize(LayoutProfile profile) => profile switch
    {
        LayoutProfile.PhonePortrait  => new Vector2(80f, 80f),
        LayoutProfile.PhoneLandscape => new Vector2(72f, 72f),
        LayoutProfile.TabletPortrait => new Vector2(64f, 64f),
        LayoutProfile.TabletLandscape => new Vector2(60f, 60f),
        _                            => new Vector2(40f, 32f), // Desktop
    };

    /// <summary>
    /// Recommended HUD button spacing for the given profile (logical pixels).
    /// </summary>
    public static float RecommendedButtonSpacing(LayoutProfile profile) => profile switch
    {
        LayoutProfile.PhonePortrait or LayoutProfile.PhoneLandscape => 12f,
        LayoutProfile.TabletPortrait or LayoutProfile.TabletLandscape => 10f,
        _ => 6f,
    };
}
