using OpenTK.Mathematics;

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
    public static ParticleEmitter CreateEngineTrail(Vector3 origin, Vector3 exhaustDir)
    {
        var dir = Vector3.Normalize(exhaustDir);
        return new ParticleEmitter(256)
        {
            Origin          = origin,
            EmitRate        = 60f,
            BaseVelocity    = dir * 3f,
            VelocitySpread  = 0.15f,
            ParticleLifetime = 0.6f,
            StartColor      = new Vector4(0.4f, 0.8f, 1.0f, 1f),
            EndColor        = new Vector4(0.1f, 0.2f, 0.5f, 0f),
        };
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
