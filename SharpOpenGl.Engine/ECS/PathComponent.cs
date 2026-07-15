using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Holds a precomputed path of grid cells for an entity to follow.
/// Used by <see cref="PathFollowingSystem"/> to move entities along waypoints.
/// </summary>
public sealed class PathComponent
{
    /// <summary>Ordered list of world-space positions from current to destination.</summary>
    public List<Vector3> Waypoints { get; set; } = new();

    /// <summary>Index of the next waypoint to move toward.</summary>
    public int CurrentWaypointIndex { get; set; }

    /// <summary>Seconds without meaningful progress toward the active waypoint.</summary>
    public float StuckSeconds { get; set; }

    /// <summary>Last recorded horizontal distance to the active path target.</summary>
    public float LastProgressDistance { get; set; } = float.MaxValue;

    /// <summary>Returns true when all waypoints have been reached.</summary>
    public bool IsComplete => CurrentWaypointIndex >= Waypoints.Count;
}
