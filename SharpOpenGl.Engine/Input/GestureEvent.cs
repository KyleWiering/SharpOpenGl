using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Describes a completed or in-progress gesture recognised by <see cref="GestureRecognizer"/>.
/// </summary>
public sealed class GestureEvent
{
    /// <summary>Which gesture was recognised.</summary>
    public GestureType Type { get; init; }

    /// <summary>
    /// Primary screen-space position of the gesture.
    /// For two-finger gestures this is the midpoint of the two contacts.
    /// </summary>
    public Vector2 Position { get; init; }

    /// <summary>
    /// Incremental movement delta since the last frame (drag and two-finger drag).
    /// Zero for discrete gestures (tap, double-tap, long-press).
    /// </summary>
    public Vector2 Delta { get; init; }

    /// <summary>
    /// Scale factor change since the last frame (pinch only).
    /// Values > 1 mean the fingers moved apart (zoom in); values &lt; 1 mean closer together (zoom out).
    /// </summary>
    public float PinchScale { get; init; } = 1f;
}
