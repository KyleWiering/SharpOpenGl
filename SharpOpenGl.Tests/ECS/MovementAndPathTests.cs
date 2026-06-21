using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class MovementAndPathTests
{
    private static (World world, GridSystem grid, Entity ship) SetupShip(
        int startX = 2, int startY = 2)
    {
        var grid = new GridSystem(16, 16);
        var world = new World();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(startX, startY)
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 10f,
            Acceleration = 50f,
            TurnRate = 360f
        });

        return (world, grid, ship);
    }

    [Fact]
    public void DestinationComponent_stores_target()
    {
        var dest = new DestinationComponent
        {
            Target = new Vector3(5, 0, 5),
            GridX = 5,
            GridY = 5
        };
        Assert.Equal(5, dest.GridX);
        Assert.Equal(5, dest.GridY);
    }

    [Fact]
    public void PathFollowingSystem_computes_path_and_sets_movement_target()
    {
        var (world, grid, ship) = SetupShip(0, 0);
        var system = new PathFollowingSystem(grid);

        world.AddComponent(ship, new DestinationComponent
        {
            Target = grid.GridToWorld(5, 0),
            GridX = 5,
            GridY = 0
        });

        system.Update(world, 0.016f);

        var movement = world.GetComponent<MovementComponent>(ship)!;
        Assert.NotNull(movement.PathTarget);

        world.Dispose();
    }

    [Fact]
    public void PathFollowingSystem_removes_destination_on_unreachable()
    {
        var grid = new GridSystem(5, 5);
        // Block all exits from (0,0)
        grid.GetCell(1, 0)!.Terrain = TerrainType.Impassable;
        grid.GetCell(0, 1)!.Terrain = TerrainType.Impassable;

        var world = new World();
        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(0, 0)
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 10f, Acceleration = 50f, TurnRate = 360f
        });
        world.AddComponent(ship, new DestinationComponent
        {
            Target = grid.GridToWorld(4, 4),
            GridX = 4,
            GridY = 4
        });

        var system = new PathFollowingSystem(grid);
        system.Update(world, 0.016f);

        Assert.False(world.HasComponent<DestinationComponent>(ship));

        world.Dispose();
    }

    [Fact]
    public void MovementSystem_moves_entity_toward_target()
    {
        var world = new World();
        var system = new MovementSystem();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = Vector3.Zero
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 100f,
            Acceleration = 200f,
            TurnRate = 360f,
            PathTarget = new Vector3(10, 0, 0)
        });

        // Simulate a few frames
        for (int i = 0; i < 10; i++)
            system.Update(world, 0.1f);

        var pos = world.GetComponent<TransformComponent>(ship)!.Position;
        Assert.True(pos.X > 0f, "Ship should have moved in X direction");

        world.Dispose();
    }

    [Fact]
    public void MovementSystem_stops_at_arrival()
    {
        var world = new World();
        var system = new MovementSystem();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = new Vector3(9.5f, 0, 0)
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 100f,
            Acceleration = 200f,
            TurnRate = 360f,
            PathTarget = new Vector3(10, 0, 0)
        });

        system.Update(world, 0.1f);

        var movement = world.GetComponent<MovementComponent>(ship)!;
        Assert.Null(movement.PathTarget);

        world.Dispose();
    }

    [Fact]
    public void MovementSystem_keeps_moving_while_route_managed_within_arrival_threshold()
    {
        var world = new World();
        var system = new MovementSystem();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = new Vector3(9.2f, 0, 0),
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 100f,
            Acceleration = 200f,
            TurnRate = 360f,
            PathTarget = new Vector3(10, 0, 0),
        });
        world.AddComponent(ship, new DestinationComponent
        {
            Target = new Vector3(10, 0, 0),
            GridX = 10,
            GridY = 0,
        });

        system.Update(world, 0.1f);

        var movement = world.GetComponent<MovementComponent>(ship)!;
        Assert.NotNull(movement.PathTarget);
        Assert.True(movement.Velocity.Length > 0.01f);

        world.Dispose();
    }

    [Fact]
    public void Full_route_pipeline_ship_reaches_destination()
    {
        var bus = new EventBus();
        var grid = new GridSystem(16, 16);
        var world = new World();
        world.AddSystem(new AutoMoveSystem(bus));
        world.AddSystem(new PathFollowingSystem(grid));
        world.AddSystem(new MovementSystem());

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

        RouteCommands.AssignDestination(world, ship, grid.GridToWorld(5, 0));

        for (int i = 0; i < 600; i++)
            world.Update(1f / 60f);

        var transform = world.GetComponent<TransformComponent>(ship)!;
        float dist = Vector3.Distance(transform.Position, grid.GridToWorld(5, 0));
        Assert.True(dist < 2f, $"Ship should reach destination, dist={dist}");

        world.Dispose();
    }

    [Fact]
    public void AutoMoveSystem_advances_through_waypoints()
    {
        var bus = new EventBus();
        var world = new World();
        var system = new AutoMoveSystem(bus);

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = Vector3.Zero
        });
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 10f, Acceleration = 50f, TurnRate = 360f
        });
        world.AddComponent(ship, new WaypointQueueComponent
        {
            Waypoints = new List<Vector3>
            {
                new(5, 0, 0),
                new(10, 0, 0)
            }
        });

        // First update should assign first waypoint as destination without advancing index
        system.Update(world, 0.016f);
        Assert.True(world.HasComponent<DestinationComponent>(ship));

        var dest = world.GetComponent<DestinationComponent>(ship)!;
        Assert.Equal(new Vector3(5, 0, 0), dest.Target);
        Assert.Equal(0, world.GetComponent<WaypointQueueComponent>(ship)!.CurrentIndex);

        world.Dispose();
    }

    [Fact]
    public void AutoMoveSystem_fires_arrival_event()
    {
        var bus = new EventBus();
        var world = new World();
        var system = new AutoMoveSystem(bus);
        bool arrived = false;
        bus.Subscribe<ShipArrivedEvent>(_ => arrived = true);

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent());
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 10f, Acceleration = 50f, TurnRate = 360f
        });
        world.AddComponent(ship, new WaypointQueueComponent
        {
            Waypoints = new List<Vector3> { new(5, 0, 0) }
        });

        // First update assigns destination
        system.Update(world, 0.016f);
        // Remove destination to simulate arrival
        world.RemoveComponent<DestinationComponent>(ship);
        // Next update should fire arrival event (queue exhausted)
        system.Update(world, 0.016f);

        Assert.True(arrived);

        world.Dispose();
    }

    [Fact]
    public void AutoMoveSystem_patrol_loops()
    {
        var bus = new EventBus();
        var world = new World();
        var system = new AutoMoveSystem(bus);

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent());
        world.AddComponent(ship, new MovementComponent
        {
            Speed = 10f, Acceleration = 50f, TurnRate = 360f
        });
        world.AddComponent(ship, new WaypointQueueComponent
        {
            Waypoints = new List<Vector3> { new(5, 0, 0), new(10, 0, 0) },
            Patrol = true
        });

        // Advance through both waypoints
        system.Update(world, 0.016f); // assigns wp 0
        world.RemoveComponent<DestinationComponent>(ship);
        system.Update(world, 0.016f); // assigns wp 1
        world.RemoveComponent<DestinationComponent>(ship);
        system.Update(world, 0.016f); // should loop to wp 0

        Assert.True(world.HasComponent<DestinationComponent>(ship));
        var dest = world.GetComponent<DestinationComponent>(ship)!;
        Assert.Equal(new Vector3(5, 0, 0), dest.Target);

        world.Dispose();
    }
}