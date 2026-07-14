using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Factory helpers that return pre-configured <see cref="ParticleEmitter"/> instances
/// for common visual effects.
/// </summary>
public static class ParticleEffects
{
    // ── Engine trail ──────────────────────────────────────────────────────────

    /// <summary>
    /// Continuous trail emitted from an engine nozzle.
    /// Particles stream backwards from the given origin.
    /// </summary>
    /// <param name="origin">Nozzle world-space position.</param>
    /// <param name="exhaustDir">Direction particles are ejected (typically -forward).</param>
    public static ParticleEmitter CreateEngineTrail(Vector3 origin, Vector3 exhaustDir, Vector3? tint = null)
    {
        var dir = Vector3.Normalize(exhaustDir);
        Vector4 startColor = ResolveTerranEngineColor(tint);
        return new ParticleEmitter(256)
        {
            Origin          = origin,
            EmitRate        = 60f,
            BaseVelocity    = dir * 3f,
            VelocitySpread  = 0.15f,
            ParticleLifetime = 0.6f,
            StartColor      = startColor,
            EndColor        = new Vector4(startColor.X * 0.18f, startColor.Y * 0.28f, startColor.Z * 0.55f, 0f),
        };
    }

    private static Vector4 ResolveTerranEngineColor(Vector3? tint)
    {
        if (tint.HasValue)
            return new Vector4(tint.Value, 1f);

        if (RaceVisualSchema.TryGetRace("terran", out var race) && race.Palette.Engine.Length >= 3)
        {
            return new Vector4(
                race.Palette.Engine[0],
                race.Palette.Engine[1],
                race.Palette.Engine[2],
                1f);
        }

        return new Vector4(0.55f, 0.72f, 0.92f, 1f);
    }

    // ── Explosion ─────────────────────────────────────────────────────────────

    /// <summary>
    /// One-shot explosion burst. Call <see cref="ParticleEmitter.IsEmitting"/> = false
    /// after one frame to stop spawning (burst style).
    /// </summary>
    /// <param name="origin">Centre of the explosion.</param>
    /// <param name="radius">Maximum outward velocity (controls size).</param>
    public static ParticleEmitter CreateExplosion(Vector3 origin, float radius = 2f)
    {
        return new ParticleEmitter(512)
        {
            Origin           = origin,
            EmitRate         = 1000f,   // burst — emit everything in one frame
            BaseVelocity     = Vector3.Zero,
            VelocitySpread   = radius,
            ParticleLifetime = 1.2f,
            StartColor       = new Vector4(1.0f, 0.6f, 0.1f, 1f),
            EndColor         = new Vector4(0.3f, 0.1f, 0.0f, 0f),
        };
    }

    /// <summary>Continuous low-rate ore sparks at a mining node surface (drone shuttle mode).</summary>
    public static ParticleEmitter CreateMiningNodeSurfaceEmitter(Vector3 origin, HarvestMode mode)
    {
        return mode switch
        {
            HarvestMode.Eva => new ParticleEmitter(96)
            {
                Origin = origin,
                EmitRate = 18f,
                BaseVelocity = Vector3.UnitY * 1.2f,
                VelocitySpread = 1.1f,
                ParticleLifetime = 0.7f,
                StartColor = new Vector4(0.75f, 0.62f, 0.45f, 1f),
                EndColor = new Vector4(0.35f, 0.28f, 0.2f, 0f),
            },
            HarvestMode.TractorBeam => new ParticleEmitter(64)
            {
                Origin = origin,
                EmitRate = 0f,
                IsEmitting = false,
            },
            _ => new ParticleEmitter(96)
            {
                Origin = origin,
                EmitRate = 14f,
                BaseVelocity = Vector3.UnitY * 0.9f,
                VelocitySpread = 0.9f,
                ParticleLifetime = 0.55f,
                StartColor = new Vector4(0.95f, 0.65f, 0.15f, 1f),
                EndColor = new Vector4(0.85f, 0.35f, 0.05f, 0f),
            },
        };
    }

    /// <summary>One-shot burst on node surface — orange chips with optional tractor shimmer.</summary>
    public static ParticleEmitter CreateMiningNodeBurst(Vector3 origin, HarvestMode mode)
    {
        if (mode == HarvestMode.TractorBeam)
        {
            return new ParticleEmitter(128)
            {
                Origin = origin,
                EmitRate = 900f,
                BaseVelocity = Vector3.UnitY * 1.4f,
                VelocitySpread = 1.6f,
                ParticleLifetime = 0.5f,
                StartColor = new Vector4(0.45f, 0.85f, 1f, 1f),
                EndColor = new Vector4(0.95f, 0.7f, 0.2f, 0f),
            };
        }

        if (mode == HarvestMode.Eva)
        {
            return new ParticleEmitter(96)
            {
                Origin = origin,
                EmitRate = 700f,
                BaseVelocity = Vector3.UnitY * 1f,
                VelocitySpread = 1.3f,
                ParticleLifetime = 0.6f,
                StartColor = new Vector4(0.7f, 0.55f, 0.38f, 1f),
                EndColor = new Vector4(0.4f, 0.3f, 0.22f, 0f),
            };
        }

        return new ParticleEmitter(96)
        {
            Origin = origin,
            EmitRate = 650f,
            BaseVelocity = Vector3.UnitY * 1.1f,
            VelocitySpread = 1.2f,
            ParticleLifetime = 0.55f,
            StartColor = new Vector4(0.95f, 0.68f, 0.18f, 1f),
            EndColor = new Vector4(0.85f, 0.3f, 0.05f, 0f),
        };
    }

    /// <summary>Short spark burst on weapon impact.</summary>
    public static ParticleEmitter CreateImpactBurst(Vector3 origin, float scale = 1f)
    {
        float radius = 1.4f * scale;
        return new ParticleEmitter(96)
        {
            Origin           = origin,
            EmitRate         = 1200f,
            BaseVelocity     = Vector3.UnitY * 0.8f,
            VelocitySpread   = radius,
            ParticleLifetime = 0.45f,
            StartColor       = new Vector4(1.0f, 0.85f, 0.35f, 1f),
            EndColor         = new Vector4(0.9f, 0.25f, 0.05f, 0f),
        };
    }

    /// <summary>Destruction burst for ships and small craft.</summary>
    public static ParticleEmitter CreateShipDeathExplosion(Vector3 origin, float scale = 1f)
    {
        float radius = 3.5f * scale;
        return new ParticleEmitter(384)
        {
            Origin           = origin,
            EmitRate         = 1400f,
            BaseVelocity     = Vector3.UnitY * 1.5f,
            VelocitySpread   = radius,
            ParticleLifetime = 1.0f,
            StartColor       = new Vector4(1.0f, 0.55f, 0.12f, 1f),
            EndColor         = new Vector4(0.25f, 0.08f, 0.02f, 0f),
        };
    }

    /// <summary>Large destruction burst for stations and bases.</summary>
    public static ParticleEmitter CreateStationDeathExplosion(Vector3 origin, float scale = 1f)
    {
        float radius = 7f * scale;
        return new ParticleEmitter(768)
        {
            Origin           = origin,
            EmitRate         = 1800f,
            BaseVelocity     = Vector3.UnitY * 2.5f,
            VelocitySpread   = radius,
            ParticleLifetime = 1.6f,
            StartColor       = new Vector4(1.0f, 0.7f, 0.2f, 1f),
            EndColor         = new Vector4(0.35f, 0.12f, 0.05f, 0f),
        };
    }

    // ── Shield bubble ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pulsing shield effect around a ship.
    /// Particles spawn on a sphere surface and drift outward.
    /// </summary>
    /// <param name="origin">Centre of the shielded entity.</param>
    /// <param name="shieldRadius">Sphere radius in world units.</param>
    public static ParticleEmitter CreateShieldBubble(Vector3 origin, float shieldRadius = 1.5f)
    {
        return new ParticleEmitter(128)
        {
            Origin           = origin,
            EmitRate         = 30f,
            BaseVelocity     = Vector3.UnitY * 0.3f,
            VelocitySpread   = shieldRadius * 0.5f,
            ParticleLifetime = 0.8f,
            StartColor       = new Vector4(0.2f, 0.6f, 1.0f, 0.8f),
            EndColor         = new Vector4(0.1f, 0.3f, 0.8f, 0f),
        };
    }

    // ── Weapon fire ───────────────────────────────────────────────────────────

    /// <summary>
    /// Continuous weapon-colored trail behind a live projectile (pooled, capped).
    /// </summary>
    public static ParticleEmitter CreateProjectileTrail(
        WeaponVisualKind visual, Vector3 origin, Vector3 direction, ProjectileType motion = ProjectileType.Linear)
    {
        var dir = Vector3.Normalize(direction);
        Vector4 tint = WeaponProfiles.TrailColor(visual, motion);
        float emitRate = visual switch
        {
            WeaponVisualKind.Beam => 90f,
            WeaponVisualKind.Rocket or WeaponVisualKind.Torpedo => motion is ProjectileType.Homing or ProjectileType.AoE ? 56f : 48f,
            WeaponVisualKind.Bomb or WeaponVisualKind.Wave => 36f,
            _ => 72f,
        };

        return new ParticleEmitter(48)
        {
            Origin = origin,
            EmitRate = emitRate,
            BaseVelocity = dir * -6f,
            VelocitySpread = 0.2f,
            ParticleLifetime = 0.35f,
            StartColor = tint,
            EndColor = tint with { W = 0f },
        };
    }

    /// <summary>Harvest beam shimmer pulled along collector→node link.</summary>
    public static ParticleEmitter CreateHarvestBeamShimmer(
        Vector3 origin, HarvestMode mode, Vector3 pullDir)
    {
        var dir = Vector3.Normalize(pullDir);
        Vector4 start = mode switch
        {
            HarvestMode.TractorBeam => new Vector4(0.38f, 0.84f, 1f, 0.92f),
            HarvestMode.Eva => new Vector4(0.88f, 0.58f, 0.28f, 0.86f),
            HarvestMode.Drones => new Vector4(0.48f, 0.96f, 0.64f, 0.88f),
            _ => new Vector4(0.98f, 0.72f, 0.18f, 0.9f),
        };

        return new ParticleEmitter(32)
        {
            Origin = origin,
            EmitRate = 28f,
            BaseVelocity = dir * 2.2f,
            VelocitySpread = 0.35f,
            ParticleLifetime = 0.45f,
            StartColor = start,
            EndColor = start with { W = 0f },
        };
    }

    /// <summary>
    /// Short-lived muzzle flash or laser streak effect.
    /// </summary>
    /// <param name="muzzlePos">World-space barrel tip position.</param>
    /// <param name="fireDir">Direction the weapon is firing.</param>
    /// <param name="isLaser">
    /// <c>true</c> for laser (fast, cyan); <c>false</c> for missile (slower, orange trail).
    /// </param>
    public static ParticleEmitter CreateWeaponFire(
        Vector3 muzzlePos, Vector3 fireDir, bool isLaser = true)
    {
        var dir = Vector3.Normalize(fireDir);

        if (isLaser)
        {
            return new ParticleEmitter(32)
            {
                Origin           = muzzlePos,
                EmitRate         = 100f,
                BaseVelocity     = dir * 20f,
                VelocitySpread   = 0.05f,
                ParticleLifetime = 0.15f,
                StartColor       = new Vector4(0.0f, 1.0f, 0.9f, 1f),
                EndColor         = new Vector4(0.0f, 0.5f, 0.4f, 0f),
            };
        }

        return new ParticleEmitter(64)
        {
            Origin           = muzzlePos,
            EmitRate         = 40f,
            BaseVelocity     = dir * 6f,
            VelocitySpread   = 0.3f,
            ParticleLifetime = 0.8f,
            StartColor       = new Vector4(1.0f, 0.5f, 0.1f, 1f),
            EndColor         = new Vector4(0.3f, 0.1f, 0.0f, 0f),
        };
    }
}
