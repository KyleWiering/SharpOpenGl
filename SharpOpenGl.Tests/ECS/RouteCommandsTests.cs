using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class RouteCommandsTests
{
    private static (World world, GridSystem grid, Entity ship) SetupShip(int startX = 0, int startY = 2)
    {
        var grid = new GridSystem(5, 5);
        var world = new World();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(startX, startY),
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 10f,
            Acceleration = 50f,
            TurnRate = 360f,
        });

        return (world, grid, ship);
    }

    [Fact]
    public void AssignDestination_replace_creates_single_waypoint_queue()
    {
        var (world, _, ship) = SetupShip();
        var target = new Vector3(4f, 0f, 2f);

        RouteCommands.AssignDestination(world, ship, target);

        var queue = world.GetComponent<WaypointQueueComponent>(ship)!;
        Assert.Single(queue.Waypoints);
        Assert.Equal(target, queue.Waypoints[0]);
        Assert.Equal(0, queue.CurrentIndex);
        Assert.False(queue.LegInProgress);
        Assert.False(queue.Patrol);

        world.Dispose();
    }

    [Fact]
    public void AssignDestination_append_adds_to_existing_queue()
    {
        var (world, _, ship) = SetupShip();
        var first = new Vector3(2f, 0f, 2f);
        var second = new Vector3(4f, 0f, 2f);

        RouteCommands.AssignDestination(world, ship, first);
        RouteCommands.AssignDestination(world, ship, second, append: true);

        var queue = world.GetComponent<WaypointQueueComponent>(ship)!;
        Assert.Equal(2, queue.Waypoints.Count);
        Assert.Equal(first, queue.Waypoints[0]);
        Assert.Equal(second, queue.Waypoints[1]);

        world.Dispose();
    }

    [Fact]
    public void AssignDestination_append_does_not_replace_in_progress_route()
    {
        var (world, _, ship) = SetupShip();
        var first = new Vector3(2f, 0f, 2f);
        var second = new Vector3(4f, 0f, 2f);

        RouteCommands.AssignDestination(world, ship, first);
        var queue = world.GetComponent<WaypointQueueComponent>(ship)!;
        queue.CurrentIndex = 0;
        queue.LegInProgress = true;

        RouteCommands.AssignDestination(world, ship, second, append: true);

        Assert.Equal(2, queue.Waypoints.Count);
        Assert.True(queue.LegInProgress);

        world.Dispose();
    }

    [Fact]
    public void Multi_waypoint_route_progresses_only_after_arrival()
    {
        var bus = new EventBus();
        var (world, grid, ship) = SetupShip();
        var autoMove = new AutoMoveSystem(bus);
        var pathFollowing = new PathFollowingSystem(grid);
        var movement = new MovementSystem();

        world.AddComponent(ship, new WaypointQueueComponent
        {
            Waypoints = [grid.GridToWorld(2, 2), grid.GridToWorld(4, 2)],
        });

        autoMove.Update(world, 0.016f);
        Assert.True(world.HasComponent<DestinationComponent>(ship));
        Assert.Equal(0, world.GetComponent<WaypointQueueComponent>(ship)!.CurrentIndex);

        world.RemoveComponent<DestinationComponent>(ship);
        world.RemoveComponent<PathComponent>(ship);

        autoMove.Update(world, 0.016f);
        var dest = world.GetComponent<DestinationComponent>(ship)!;
        Assert.Equal(grid.GridToWorld(4, 2), dest.Target);
        Assert.Equal(1, world.GetComponent<WaypointQueueComponent>(ship)!.CurrentIndex);

        world.Dispose();
    }

    [Fact]
    public void PathFollowingSystem_routes_around_impassable_terrain()
    {
        var grid = new GridSystem(5, 5);
        for (int y = 0; y <= 3; y++)
            grid.GetCell(2, y)!.Terrain = TerrainType.Impassable;

        var world = new World();
        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(0, 2),
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 10f,
            Acceleration = 50f,
            TurnRate = 360f,
        });
        world.AddComponent(ship, new DestinationComponent
        {
            Target = grid.GridToWorld(4, 2),
            GridX = 4,
            GridY = 2,
        });

        var system = new PathFollowingSystem(grid);
        system.Update(world, 0.016f);

        var path = world.GetComponent<PathComponent>(ship)!;
        Assert.True(path.Waypoints.Count > 2, "Path should detour around the wall");

        foreach (Vector3 waypoint in path.Waypoints)
        {
            if (!grid.WorldToGrid(waypoint, out int x, out int y)) continue;
            var cell = grid.GetCell(x, y);
            if (cell != null)
                Assert.True(cell.IsPassable);
        }

        world.Dispose();
    }

    [Fact]
    public void Patrol_loops_through_waypoints_with_pathfinding()
    {
        var bus = new EventBus();
        var (world, grid, ship) = SetupShip(0, 0);
        var autoMove = new AutoMoveSystem(bus);
        var pathFollowing = new PathFollowingSystem(grid);
        var transform = world.GetComponent<TransformComponent>(ship)!;

        RouteCommands.AssignPatrol(world, ship, grid.GridToWorld(4, 0), grid.GridToWorld(0, 0));

        autoMove.Update(world, 0.016f);
        pathFollowing.Update(world, 0.016f);
        Assert.True(world.HasComponent<PathComponent>(ship));

        transform.Position = grid.GridToWorld(4, 0);
        world.RemoveComponent<DestinationComponent>(ship);
        world.RemoveComponent<PathComponent>(ship);

        autoMove.Update(world, 0.016f);
        pathFollowing.Update(world, 0.016f);
        Assert.True(world.HasComponent<DestinationComponent>(ship));
        Assert.Equal(grid.GridToWorld(0, 0), world.GetComponent<DestinationComponent>(ship)!.Target);

        transform.Position = grid.GridToWorld(0, 0);
        world.RemoveComponent<DestinationComponent>(ship);
        world.RemoveComponent<PathComponent>(ship);

        autoMove.Update(world, 0.016f);
        var dest = world.GetComponent<DestinationComponent>(ship)!;
        Assert.Equal(grid.GridToWorld(4, 0), dest.Target);
        Assert.True(world.GetComponent<WaypointQueueComponent>(ship)!.Patrol);

        world.Dispose();
    }
}