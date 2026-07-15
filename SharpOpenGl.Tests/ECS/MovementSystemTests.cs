using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

[Collection("MovementBalance")]
public class MovementSystemTests
{
    public MovementSystemTests()
    {
        MovementBalance.ResetForTests();
        MovementBalance.Apply(new MovementConfig
        {
            GlobalSpeedMultiplier = 1f,
            GlobalAccelerationMultiplier = 1f,
        });
    }

    [Fact]
    public void EntityMovesTowardTarget()
    {
        var world = new World();
        world.AddSystem(new MovementSystem());

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(entity, new MovementComponent
        {
            Speed = 10f,
            Acceleration = 100f,
            TurnRate = 360f,
            PathTarget = new Vector3(20f, 0f, 0f),
        });

        // Simulate several frames
        for (int i = 0; i < 60; i++)
            world.Update(1f / 60f);

        var transform = world.GetComponent<TransformComponent>(entity)!;
        // Should have moved toward target
        Assert.True(transform.Position.X > 5f,
            $"Ship should have moved toward X=20, but is at X={transform.Position.X}");
    }

    [Fact]
    public void EntityStopsAtTarget()
    {
        var world = new World();
        world.AddSystem(new MovementSystem());

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(entity, new MovementComponent
        {
            Speed = 50f,
            Acceleration = 200f,
            TurnRate = 360f,
            PathTarget = new Vector3(0f, 0f, 10f),
        });

        // Simulate many frames to ensure arrival
        for (int i = 0; i < 600; i++)
            world.Update(1f / 60f);

        var movement = world.GetComponent<MovementComponent>(entity)!;
        var transform = world.GetComponent<TransformComponent>(entity)!;

        Assert.Null(movement.PathTarget);
        Assert.True(Vector3.Distance(transform.Position, new Vector3(0f, 0f, 10f)) < 1f);
    }

    [Fact]
    public void EntityWithNoTargetDecelerates()
    {
        var world = new World();
        world.AddSystem(new MovementSystem());

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(entity, new MovementComponent
        {
            Speed = 10f,
            Acceleration = 50f,
            TurnRate = 360f,
            PathTarget = null,
            Velocity = new Vector3(10f, 0f, 0f),
        });

        // Simulate to decelerate
        for (int i = 0; i < 120; i++)
            world.Update(1f / 60f);

        var movement = world.GetComponent<MovementComponent>(entity)!;
        Assert.True(movement.Velocity.Length < 0.1f);
    }
}