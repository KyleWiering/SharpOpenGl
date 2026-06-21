using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Helpers for assigning move routes through <see cref="WaypointQueueComponent"/>
/// and <see cref="DestinationComponent"/> so <see cref="PathFollowingSystem"/> can pathfind.
/// </summary>
public static class RouteCommands
{
    /// <summary>Clears queued routes, destinations, and movement targets for an entity.</summary>
    public static void ClearRoute(World world, Entity entity)
    {
        world.RemoveComponent<DestinationComponent>(entity);
        world.RemoveComponent<PathComponent>(entity);
        world.RemoveComponent<WaypointQueueComponent>(entity);

        var movement = world.GetComponent<MovementComponent>(entity);
        if (movement != null)
            movement.PathTarget = null;
    }

    /// <summary>
    /// Queues a destination for pathfinding. Replaces the route unless <paramref name="append"/>
    /// is true and the entity already has a non-patrol waypoint queue.
    /// </summary>
    public static void AssignDestination(
        World world,
        Entity entity,
        Vector3 destination,
        bool append = false,
        bool patrol = false)
    {
        var movement = world.GetComponent<MovementComponent>(entity);
        if (movement != null)
            movement.PathTarget = null;

        world.RemoveComponent<DestinationComponent>(entity);
        world.RemoveComponent<PathComponent>(entity);

        WaypointQueueComponent? queue = world.GetComponent<WaypointQueueComponent>(entity);
        if (append && queue != null && queue.Waypoints.Count > 0 && !queue.Patrol)
        {
            queue.Waypoints.Add(destination);
            return;
        }

        if (queue == null)
        {
            queue = new WaypointQueueComponent();
            world.AddComponent(entity, queue);
        }
        else
        {
            queue.Waypoints.Clear();
            queue.CurrentIndex = 0;
            queue.LegInProgress = false;
            queue.Patrol = false;
        }

        queue.Waypoints.Add(destination);
        queue.Patrol = patrol;
    }

    /// <summary>Assigns a looping patrol between <paramref name="first"/> and <paramref name="second"/>.</summary>
    public static void AssignPatrol(World world, Entity entity, Vector3 first, Vector3 second)
    {
        var movement = world.GetComponent<MovementComponent>(entity);
        if (movement != null)
        {
            movement.PathTarget = null;
            movement.Velocity = Vector3.Zero;
        }

        world.RemoveComponent<DestinationComponent>(entity);
        world.RemoveComponent<PathComponent>(entity);

        WaypointQueueComponent? queue = world.GetComponent<WaypointQueueComponent>(entity);
        if (queue == null)
        {
            queue = new WaypointQueueComponent();
            world.AddComponent(entity, queue);
        }

        queue.Waypoints.Clear();
        queue.Waypoints.Add(first);
        queue.Waypoints.Add(second);
        queue.CurrentIndex = 0;
        queue.LegInProgress = false;
        queue.Patrol = true;
    }

    /// <summary>Builds a destination component with grid coordinates when a grid is available.</summary>
    public static DestinationComponent CreateDestination(Vector3 target, GridSystem? grid = null)
    {
        int gridX = (int)target.X;
        int gridY = (int)target.Z;
        if (grid != null && grid.WorldToGrid(target, out int x, out int y))
        {
            gridX = x;
            gridY = y;
        }

        return new DestinationComponent
        {
            Target = target,
            GridX = gridX,
            GridY = gridY,
        };
    }
}