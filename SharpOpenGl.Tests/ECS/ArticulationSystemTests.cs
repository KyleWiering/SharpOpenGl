using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ArticulationSystemTests
{
    [Fact]
    public void Update_slews_current_toward_target_within_slew_rate()
    {
        var world = new World();
        var system = new ArticulationSystem();

        Entity owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent { Position = Vector3.Zero });

        Entity partEntity = world.CreateEntity();
        world.AddComponent(partEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            YawMin = -90f,
            YawMax = 90f,
            PitchMin = -45f,
            PitchMax = 45f,
            CurrentYaw = 0f,
            CurrentPitch = 0f,
            TargetYaw = 90f,
            TargetPitch = 30f,
            HasAimTarget = true,
            SlewRateDegreesPerSecond = 90f,
        });

        system.Update(world, 0.5f);

        var part = world.GetComponent<ArticulatedPartComponent>(partEntity)!;
        Assert.Equal(45f, part.CurrentYaw, precision: 3);
        Assert.Equal(30f, part.CurrentPitch, precision: 3);
    }

    [Fact]
    public void Update_idle_sweep_oscillates_when_no_target()
    {
        var world = new World();
        var system = new ArticulationSystem();

        Entity owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent { Position = Vector3.Zero });

        Entity partEntity = world.CreateEntity();
        world.AddComponent(partEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            YawMin = -90f,
            YawMax = 90f,
            CurrentYaw = 0f,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 60f,
            HasAimTarget = false,
        });

        system.Update(world, 1f);

        var part = world.GetComponent<ArticulatedPartComponent>(partEntity)!;
        Assert.NotEqual(0f, part.CurrentYaw);
        Assert.InRange(part.CurrentYaw, -90f, 90f);
    }

    [Fact]
    public void Update_skips_angle_change_when_owner_beyond_lod_distance()
    {
        var world = new World();
        var system = new ArticulationSystem { CameraPosition = Vector3.Zero };

        Entity owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent { Position = new Vector3(500f, 0f, 0f) });
        world.AddComponent(owner, new MeshLodComponent
        {
            Lod = new MeshLod
            {
                SimpleDistance = 100f,
                IconDistance = 250f,
            },
        });

        Entity partEntity = world.CreateEntity();
        world.AddComponent(partEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            YawMin = -90f,
            YawMax = 90f,
            CurrentYaw = 0f,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 90f,
            HasAimTarget = false,
        });

        system.Update(world, 1f);

        var part = world.GetComponent<ArticulatedPartComponent>(partEntity)!;
        Assert.Equal(0f, part.CurrentYaw);
    }

    [Fact]
    public void Update_destroys_part_when_owner_destroyed()
    {
        var world = new World();
        var system = new ArticulationSystem();

        Entity owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent { Position = Vector3.Zero });

        Entity partEntity = world.CreateEntity();
        world.AddComponent(partEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 30f,
        });

        world.DestroyEntity(owner);
        system.Update(world, 0.016f);

        Assert.False(world.IsAlive(partEntity));
    }
}