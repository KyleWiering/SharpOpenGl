namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Defines which point on the parent container (or viewport) a widget's
/// origin is attached to.  The widget's <c>Position</c> is an additional
/// pixel offset applied after the anchor origin is resolved.
/// </summary>
public enum Anchor
{
    /// <summary>Anchors the widget to the top-left corner of its container.</summary>
    TopLeft,

    /// <summary>Horizontally centres the widget along the top edge.</summary>
    TopCenter,

    /// <summary>Anchors the widget to the top-right corner of its container.</summary>
    TopRight,

    /// <summary>Vertically centres the widget along the left edge.</summary>
    MiddleLeft,

    /// <summary>Centres the widget both horizontally and vertically.</summary>
    Center,

    /// <summary>Vertically centres the widget along the right edge.</summary>
    MiddleRight,

    /// <summary>Anchors the widget to the bottom-left corner of its container.</summary>
    BottomLeft,

    /// <summary>Horizontally centres the widget along the bottom edge.</summary>
    BottomCenter,

    /// <summary>Anchors the widget to the bottom-right corner of its container.</summary>
    BottomRight,

    /// <summary>
    /// Widget fills the entire container.  <c>Position</c> acts as inset padding
    /// and <c>Size</c> is ignored (computed from the container minus padding).
    /// </summary>
    Stretch,
}
