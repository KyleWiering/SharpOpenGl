using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Holds a queue of waypoints for sequential or patrol movement.
/// </summary>
public sealed class WaypointQueueComponent
{
    /// <summary>Ordered list of waypoint positions to visit.</summary>
    public List<Vector3> Waypoints { get; set; } = new();

    /// <summary>Index of the current waypoint being moved toward.</summary>
    public int CurrentIndex { get; set; }

    /// <summary>When true, loops back to the first waypoint after the last.</summary>
    public bool Patrol { get; set; }

    /// <summary>True while the entity is traveling toward <see cref="CurrentIndex"/>.</summary>
    public bool LegInProgress { get; set; }
}
