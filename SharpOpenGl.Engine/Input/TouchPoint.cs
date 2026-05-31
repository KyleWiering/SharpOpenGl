using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Input;

/// <summary>
/// A single contact point on a touch surface.
/// </summary>
public sealed class TouchPoint
{
    /// <summary>Platform-assigned identifier; stable for the duration of the contact.</summary>
    public int Id { get; set; }

    /// <summary>Current position in screen pixels.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Position recorded when this finger first touched the screen.</summary>
    public Vector2 StartPosition { get; set; }

    /// <summary>Elapsed time in seconds since this finger first touched the screen.</summary>
    public float ContactDuration { get; set; }

    /// <summary>Whether this point was active on the previous frame.</summary>
    public bool WasActive { get; set; }

    /// <summary>Whether this point is currently active (finger down).</summary>
    public bool IsActive { get; set; }
}
