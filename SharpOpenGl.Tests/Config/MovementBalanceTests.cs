using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.Config;

public class MovementBalanceTests
{
    [Fact]
    public void Apply_reads_multipliers_from_config()
    {
        MovementBalance.ResetForTests();
        MovementBalance.Apply(new MovementConfig
        {
            GlobalSpeedMultiplier = 0.25f,
            GlobalAccelerationMultiplier = 0.3f,
        });

        Assert.Equal(0.25f, MovementBalance.SpeedMultiplier);
        Assert.Equal(0.3f, MovementBalance.AccelerationMultiplier);
    }

    [Fact]
    public void MovementSystem_scales_speed_by_balance_multiplier()
    {
        MovementBalance.ResetForTests();
        MovementBalance.Apply(new MovementConfig { GlobalSpeedMultiplier = 0.5f, GlobalAccelerationMultiplier = 1f });

        var world = new World();
        world.AddSystem(new MovementSystem());
        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(entity, new MovementComponent
        {
            Speed = 100f,
            Acceleration = 1000f,
            TurnRate = 360f,
            PathTarget = new Vector3(0f, 0f, 100f),
        });

        world.Update(1f);

        var movement = world.GetComponent<MovementComponent>(entity)!;
        Assert.True(movement.Velocity.Length <= 50f + 0.01f);
    }
}