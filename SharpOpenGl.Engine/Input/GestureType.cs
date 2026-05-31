namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Logical gesture types recognised by <see cref="GestureRecognizer"/>.
/// </summary>
public enum GestureType
{
    /// <summary>A brief single-finger tap at one location.</summary>
    Tap,

    /// <summary>Two quick single-finger taps at roughly the same location.</summary>
    DoubleTap,

    /// <summary>A single finger held stationary for longer than the long-press threshold.</summary>
    LongPress,

    /// <summary>A single-finger slide across the screen.</summary>
    Drag,

    /// <summary>Two fingers moving in the same direction (pans the camera).</summary>
    TwoFingerDrag,

    /// <summary>Two fingers moving toward or away from each other (zooms the camera).</summary>
    Pinch,
}
