using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ShipEngineEmitterTests
{
    public ShipEngineEmitterTests() => RaceVisualSchema.Load();

    [Fact]
    public void ShipEngineEmitterSystem_updates_origin_from_owner_transform()
    {
        var world = new World();
        var system = new ShipEngineEmitterSystem();

        var ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = new Vector3(10f, 2f, -5f),
            EulerAngles = new Vector3(0f, 90f, 0f),
            Scale = Vector3.One,
        });
        world.AddComponent(ship, new MovementComponent { Velocity = Vector3.Zero });

        Vector3 localOffset = new Vector3(0.4f, 0.1f, -1.2f);
        var nozzleEntity = world.CreateEntity();
        var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        world.AddComponent(nozzleEntity, new ShipEngineNozzleComponent
        {
            Owner = ship,
            LocalOffset = localOffset,
            BaseEmitRate = emitter.EmitRate,
        });
        world.AddComponent(nozzleEntity, new ParticleEmitterComponent { Emitter = emitter });

        system.Update(world, 0.016f);

        Vector3 expected = ComputeExpectedOrigin(
            world.GetComponent<TransformComponent>(ship)!,
            localOffset,
            0f,
            0f);

        Assert.Equal(expected.X, emitter.Origin.X, precision: 3);
        Assert.Equal(expected.Y, emitter.Origin.Y, precision: 3);
        Assert.Equal(expected.Z, emitter.Origin.Z, precision: 3);
    }

    [Fact]
    public void ShipEngineEmitterSystem_gates_emitting_on_movement()
    {
        var world = new World();
        var system = new ShipEngineEmitterSystem();

        var ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(ship, new MovementComponent { Velocity = Vector3.Zero });

        var nozzleEntity = world.CreateEntity();
        var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        world.AddComponent(nozzleEntity, new ShipEngineNozzleComponent
        {
            Owner = ship,
            LocalOffset = new Vector3(0.3f, 0.1f, -1f),
            IdleGlowWhenStationary = false,
            BaseEmitRate = emitter.EmitRate,
        });
        world.AddComponent(nozzleEntity, new ParticleEmitterComponent { Emitter = emitter });

        system.Update(world, 0.016f);
        Assert.False(emitter.IsEmitting);

        world.GetComponent<MovementComponent>(ship)!.Velocity = new Vector3(1f, 0f, 0f);
        system.Update(world, 0.016f);
        Assert.True(emitter.IsEmitting);
    }

    [Fact]
    public void ShipEngineEmitterSystem_applies_gimbal_to_exhaust_direction_when_moving()
    {
        var world = new World();
        var system = new ShipEngineEmitterSystem();

        var ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        world.AddComponent(ship, new MovementComponent { Velocity = new Vector3(2f, 0f, 0f) });

        var nozzleEntity = world.CreateEntity();
        var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        world.AddComponent(nozzleEntity, new ShipEngineNozzleComponent
        {
            Owner = ship,
            LocalOffset = new Vector3(0.3f, 0.1f, -1f),
            GimbalPitch = 2f,
            BaseEmitRate = emitter.EmitRate,
        });
        world.AddComponent(nozzleEntity, new ParticleEmitterComponent { Emitter = emitter });

        system.Update(world, 0.016f);

        var zeroGimbalEmitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        zeroGimbalEmitter.BaseVelocity = Vector3.TransformNormal(-Vector3.UnitZ, Matrix4.Identity) * 3f;

        Assert.NotEqual(zeroGimbalEmitter.BaseVelocity, emitter.BaseVelocity);
        Assert.True(emitter.BaseVelocity.Y > 0f);
    }

    [Fact]
    public void ShipEngineEmitterSystem_gimbal_origin_shifts_with_pitch()
    {
        var world = new World();
        var system = new ShipEngineEmitterSystem();

        var ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = Vector3.Zero,
            EulerAngles = Vector3.Zero,
            Scale = Vector3.One,
        });
        world.AddComponent(ship, new MovementComponent { Velocity = new Vector3(1f, 0f, 0f) });

        Vector3 localOffset = new Vector3(0.4f, 0.1f, -1.2f);
        var nozzleEntity = world.CreateEntity();
        var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        world.AddComponent(nozzleEntity, new ShipEngineNozzleComponent
        {
            Owner = ship,
            LocalOffset = localOffset,
            GimbalPitch = 2f,
            BaseEmitRate = emitter.EmitRate,
        });
        world.AddComponent(nozzleEntity, new ParticleEmitterComponent { Emitter = emitter });

        system.Update(world, 0.016f);

        var ownerTf = world.GetComponent<TransformComponent>(ship)!;
        Vector3 baseline = ComputeExpectedOrigin(ownerTf, localOffset, 0f, 0f);
        Vector3 pitched = emitter.Origin;

        Assert.NotEqual(baseline, pitched);
        Assert.True(pitched.Y > baseline.Y);
    }

    [Fact]
    public void Gimbal_resets_when_stationary()
    {
        var world = new World();
        var system = new ShipEngineEmitterSystem();

        var ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero });
        var movement = new MovementComponent { Velocity = new Vector3(2f, 0f, 0f) };
        world.AddComponent(ship, movement);

        var nozzleEntity = world.CreateEntity();
        var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        var nozzle = new ShipEngineNozzleComponent
        {
            Owner = ship,
            LocalOffset = new Vector3(0.3f, 0.1f, -1f),
            GimbalNoiseSeed = 1.5f,
            BaseEmitRate = emitter.EmitRate,
        };
        world.AddComponent(nozzleEntity, nozzle);
        world.AddComponent(nozzleEntity, new ParticleEmitterComponent { Emitter = emitter });

        for (int i = 0; i < 30; i++)
            system.Update(world, 0.05f);

        Assert.True(MathF.Abs(nozzle.GimbalYaw) > 0.1f || MathF.Abs(nozzle.GimbalPitch) > 0.1f);

        movement.Velocity = Vector3.Zero;
        for (int i = 0; i < 60; i++)
            system.Update(world, 0.05f);

        Assert.Equal(0f, nozzle.GimbalYaw, precision: 1);
        Assert.Equal(0f, nozzle.GimbalPitch, precision: 1);
    }

    [Fact]
    public void Terran_ship_spawn_creates_nozzle_emitter_components()
    {
        var world = new World();
        var ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent());
        world.AddComponent(ship, new MovementComponent());

        var def = new EntityDefinition
        {
            Id = "fighter_basic",
            Category = "fighter",
        };

        int created = ShipEngineEmitterSpawner.AttachTerranEmitters(world, ship, def, "terran");
        Assert.Equal(2, created);
        Assert.Equal(2, ShipEngineEmitterSpawner.CountNozzlesForOwner(world, ship));

        int emitterCount = 0;
        foreach (var (entity, nozzle) in world.Query<ShipEngineNozzleComponent>())
        {
            if (nozzle.Owner != ship) continue;
            Assert.NotNull(world.GetComponent<ParticleEmitterComponent>(entity));
            emitterCount++;
        }

        Assert.Equal(2, emitterCount);
    }

    private static Vector3 ComputeExpectedOrigin(
        TransformComponent ownerTf,
        Vector3 localOffset,
        float gimbalYaw,
        float gimbalPitch)
    {
        var nozzle = new ShipEngineNozzleComponent
        {
            LocalOffset = localOffset,
            GimbalYaw = gimbalYaw,
            GimbalPitch = gimbalPitch,
        };
        Matrix4 rot = ShipEngineEmitterSystem.BuildNozzleRotation(ownerTf, nozzle);

        Vector3 scaledOffset = new Vector3(
            localOffset.X * ownerTf.Scale.X,
            localOffset.Y * ownerTf.Scale.Y,
            localOffset.Z * ownerTf.Scale.Z);
        Vector3 worldOffset = Vector3.TransformNormal(scaledOffset, rot);
        return ownerTf.Position + worldOffset;
    }
}