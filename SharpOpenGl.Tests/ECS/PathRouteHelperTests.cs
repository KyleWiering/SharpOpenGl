using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class PathRouteHelperTests
{
    [Fact]
    public void StringPull_removes_collinear_intermediate_waypoints()
    {
        var grid = new GridSystem(8, 8, cellSize: 10f);
        var waypoints = new List<Vector3>
        {
            grid.GridToWorld(0, 0),
            grid.GridToWorld(1, 0),
            grid.GridToWorld(2, 0),
            grid.GridToWorld(3, 0),
        };

        var pulled = PathRouteHelper.StringPull(waypoints, grid);

        Assert.Equal(2, pulled.Count);
        Assert.Equal(waypoints[0], pulled[0]);
        Assert.Equal(waypoints[^1], pulled[^1]);
    }

    [Fact]
    public void HasClearLine_returns_false_through_wall()
    {
        var grid = new GridSystem(5, 5, cellSize: 10f);
        for (int y = 0; y <= 4; y++)
            grid.GetCell(2, y)!.Terrain = TerrainType.Impassable;

        bool clear = PathRouteHelper.HasClearLine(
            grid.GridToWorld(0, 2),
            grid.GridToWorld(4, 2),
            grid);

        Assert.False(clear);
    }

    [Fact]
    public void PathFollowing_uses_direct_line_when_unobstructed()
    {
        var grid = new GridSystem(16, 16, cellSize: 10f);
        var world = new World();
        var system = new PathFollowingSystem(grid);

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(0, 0),
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 30f,
            Acceleration = 80f,
            TurnRate = 360f,
        });
        world.AddComponent(ship, new DestinationComponent
        {
            Target = grid.GridToWorld(5, 5),
            GridX = 5,
            GridY = 5,
        });

        system.Update(world, 0.016f);

        var path = world.GetComponent<PathComponent>(ship)!;
        Assert.Single(path.Waypoints);

        world.Dispose();
    }
}