using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ParticleSystemTests
{
    // ── Particle lifecycle ────────────────────────────────────────────────────

    [Fact]
    public void Particle_starts_alive_and_ages()
    {
        var p = new Particle { IsAlive = true, MaxAge = 2f };
        p.Update(0.5f);

        Assert.True(p.IsAlive);
        Assert.Equal(0.5f, p.Age, precision: 5);
    }

    [Fact]
    public void Particle_dies_when_age_reaches_maxAge()
    {
        var p = new Particle { IsAlive = true, MaxAge = 1f };
        p.Update(1.0f);

        Assert.False(p.IsAlive);
    }

    [Fact]
    public void Particle_moves_with_velocity()
    {
        var p = new Particle
        {
            IsAlive  = true,
            MaxAge   = 10f,
            Position = Vector3.Zero,
            Velocity = new Vector3(1f, 0f, 0f),
        };
        p.Update(2f);

        Assert.Equal(2f, p.Position.X, precision: 4);
    }

    [Fact]
    public void Dead_particle_Update_is_no_op()
    {
        var p = new Particle { IsAlive = false, Age = 0f, MaxAge = 1f };
        p.Update(0.5f);

        Assert.Equal(0f, p.Age);
    }

    [Fact]
    public void LifeRatio_is_normalised_0_to_1()
    {
        var p = new Particle { IsAlive = true, MaxAge = 4f, Age = 2f };
        Assert.Equal(0.5f, p.LifeRatio, precision: 5);
    }

    // ── Emitter ───────────────────────────────────────────────────────────────

    [Fact]
    public void Emitter_spawns_particles_on_update()
    {
        var emitter = new ParticleEmitter(64)
        {
            EmitRate = 100f,
            ParticleLifetime = 5f,
        };

        emitter.Update(0.1f); // should emit ~10 particles
        Assert.True(emitter.LiveCount > 0);
    }

    [Fact]
    public void Emitter_does_not_exceed_capacity()
    {
        const int cap = 16;
        var emitter = new ParticleEmitter(cap)
        {
            EmitRate = 10000f,
            ParticleLifetime = 100f,
        };

        emitter.Update(1f); // attempt to emit far more than capacity
        Assert.True(emitter.LiveCount <= cap);
    }

    [Fact]
    public void Emitter_stops_spawning_when_IsEmitting_false()
    {
        var emitter = new ParticleEmitter(64)
        {
            EmitRate = 100f,
            ParticleLifetime = 10f,
            IsEmitting = false,
        };
        emitter.Update(1f);
        Assert.Equal(0, emitter.LiveCount);
    }

    [Fact]
    public void Emitter_live_count_decreases_after_particles_expire()
    {
        var emitter = new ParticleEmitter(64)
        {
            EmitRate = 100f,
            ParticleLifetime = 0.2f,
        };
        emitter.Update(0.1f); // emit some
        int before = emitter.LiveCount;

        emitter.IsEmitting = false;
        emitter.Update(0.5f); // let them expire
        Assert.True(emitter.LiveCount < before);
    }

    // ── BuildPointBuffer ──────────────────────────────────────────────────────

    [Fact]
    public void BuildPointBuffer_returns_live_particle_count()
    {
        var emitter = new ParticleEmitter(32)
        {
            EmitRate = 50f,
            ParticleLifetime = 10f,
        };
        emitter.Update(0.5f);

        int live = emitter.LiveCount;
        var buffer = new float[live * 3 + 9]; // a bit extra
        int reported = emitter.BuildPointBuffer(buffer);
        Assert.Equal(live, reported);
    }

    // ── ParticleSystem ECS ────────────────────────────────────────────────────

    [Fact]
    public void ParticleSystem_advances_all_emitters()
    {
        var world  = new World();
        var system = new SharpOpenGl.Engine.ECS.ParticleSystem();

        var entity = world.CreateEntity();
        var emitterComp = new ParticleEmitterComponent
        {
            Emitter = new ParticleEmitter(64)
            {
                EmitRate = 100f,
                ParticleLifetime = 10f,
            }
        };
        world.AddComponent(entity, emitterComp);

        system.Update(world, 0.1f);

        Assert.True(emitterComp.Emitter.LiveCount > 0);
    }

    // ── ParticleEffects presets ───────────────────────────────────────────────

    [Fact]
    public void EngineTrail_emitter_has_positive_emit_rate()
    {
        var e = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        Assert.True(e.EmitRate > 0f);
    }

    [Fact]
    public void Explosion_emitter_has_high_emit_rate_for_burst()
    {
        var e = ParticleEffects.CreateExplosion(Vector3.Zero, 2f);
        Assert.True(e.EmitRate >= 100f);
    }

    [Fact]
    public void ShieldBubble_start_color_has_partial_alpha()
    {
        var e = ParticleEffects.CreateShieldBubble(Vector3.Zero);
        Assert.True(e.StartColor.W < 1f); // translucent
    }

    [Fact]
    public void WeaponFire_laser_has_short_lifetime()
    {
        var laser   = ParticleEffects.CreateWeaponFire(Vector3.Zero, Vector3.UnitZ, isLaser: true);
        var missile = ParticleEffects.CreateWeaponFire(Vector3.Zero, Vector3.UnitZ, isLaser: false);
        Assert.True(laser.ParticleLifetime < missile.ParticleLifetime);
    }
}
