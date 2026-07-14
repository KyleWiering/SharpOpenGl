using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class RoutePreviewHelperTests
{
    [Fact]
    public void BuildSegments_single_waypoint_starts_at_unit_position()
    {
        var queue = new WaypointQueueComponent
        {
            Waypoints = [new Vector3(10f, 0f, 5f)],
        };
        var unitPos = new Vector3(1f, 0f, 2f);

        var segments = RoutePreviewHelper.BuildSegments(unitPos, queue);

        Assert.NotNull(segments);
        Assert.Equal(2, segments!.Count);
        Assert.Equal(unitPos, segments[0]);
        Assert.Equal(queue.Waypoints[0], segments[1]);
    }

    [Fact]
    public void BuildSegments_shift_rmb_queue_draws_full_polyline()
    {
        var queue = new WaypointQueueComponent
        {
            Waypoints =
            [
                new Vector3(4f, 0f, 0f),
                new Vector3(8f, 0f, 4f),
                new Vector3(12f, 0f, 0f),
            ],
            CurrentIndex = 1,
            LegInProgress = true,
        };
        var unitPos = new Vector3(6f, 0f, 2f);

        var segments = RoutePreviewHelper.BuildSegments(unitPos, queue);

        Assert.NotNull(segments);
        Assert.Equal(3, segments!.Count);
        Assert.Equal(unitPos, segments[0]);
        Assert.Equal(queue.Waypoints[1], segments[1]);
        Assert.Equal(queue.Waypoints[2], segments[2]);
    }

    [Fact]
    public void BuildSegments_patrol_closes_loop_to_first_waypoint()
    {
        var first = new Vector3(2f, 0f, 2f);
        var second = new Vector3(6f, 0f, 2f);
        var queue = new WaypointQueueComponent
        {
            Waypoints = [first, second],
            Patrol = true,
        };

        var segments = RoutePreviewHelper.BuildSegments(Vector3.Zero, queue);

        Assert.NotNull(segments);
        Assert.Equal(4, segments!.Count);
        Assert.Equal(first, segments[^1]);
    }

    [Fact]
    public void BuildSegments_returns_null_when_no_waypoints()
    {
        var queue = new WaypointQueueComponent();
        Assert.Null(RoutePreviewHelper.BuildSegments(Vector3.Zero, queue));
    }
}