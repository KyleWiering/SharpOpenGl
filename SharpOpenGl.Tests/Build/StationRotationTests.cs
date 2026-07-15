using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.Build;

public class StationRotationTests
{
    [Fact]
    public void Rotating_building_increases_yaw_each_tick()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new BuildingComponent { Rotates = true });
        world.AddComponent(entity, new TransformComponent { EulerAngles = new Vector3(0f, 10f, 0f) });

        const float deltaTime = 0.5f;
        StationRotationSystem.UpdateStationRotations(world, deltaTime);

        var transform = world.GetComponent<TransformComponent>(entity)!;
        float expectedYaw = 10f + StationRotationSystem.RotationSpeedDegreesPerSecond * deltaTime;
        Assert.Equal(expectedYaw, transform.EulerAngles.Y, precision: 4);
    }

    [Fact]
    public void Non_rotating_building_keeps_yaw_unchanged()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new BuildingComponent { Rotates = false });
        world.AddComponent(entity, new TransformComponent { EulerAngles = new Vector3(0f, 45f, 0f) });

        StationRotationSystem.UpdateStationRotations(world, 1f);

        var transform = world.GetComponent<TransformComponent>(entity)!;
        Assert.Equal(45f, transform.EulerAngles.Y, precision: 4);
    }

    [Fact]
    public void Multiple_ticks_accumulate_yaw_for_rotating_building()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new BuildingComponent { Rotates = true });
        world.AddComponent(entity, new TransformComponent());

        for (int i = 0; i < 3; i++)
            StationRotationSystem.UpdateStationRotations(world, 0.1f);

        var transform = world.GetComponent<TransformComponent>(entity)!;
        float expectedYaw = StationRotationSystem.RotationSpeedDegreesPerSecond * 0.3f;
        Assert.Equal(expectedYaw, transform.EulerAngles.Y, precision: 4);
    }
}