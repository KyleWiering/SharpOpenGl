using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Builds world-space polyline segments for queued waypoint route previews (shift+RMB queues).
/// </summary>
public static class RoutePreviewHelper
{
    public static readonly Vector3 DefaultLineColor = new(0.2f, 0.85f, 0.55f);
    public const float DefaultLineY = 0.3f;
    public static readonly Vector4 DefaultLineColorRgba = new(0.2f, 0.85f, 0.55f, 0.45f);

    /// <summary>
    /// Returns ordered XZ route vertices from the unit position through pending waypoints.
    /// Patrol routes close the loop back to the first waypoint.
    /// </summary>
    public static List<Vector3>? BuildSegments(Vector3 unitPosition, WaypointQueueComponent queue)
    {
        if (queue.Waypoints.Count == 0)
            return null;

        var segments = new List<Vector3> { unitPosition };
        for (int i = queue.CurrentIndex; i < queue.Waypoints.Count; i++)
            segments.Add(queue.Waypoints[i]);

        if (queue.Patrol && queue.Waypoints.Count > 0)
            segments.Add(queue.Waypoints[0]);

        return segments.Count >= 2 ? segments : null;
    }
}